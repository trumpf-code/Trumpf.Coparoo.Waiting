// Copyright 2016 - 2025 TRUMPF Werkzeugmaschinen GmbH + Co. KG.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Trumpf.Coparoo.Waiting.WinForms
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Trumpf.Coparoo.Waiting.Exceptions;
    using Trumpf.Coparoo.Waiting.Interfaces;

    /// <summary>
    /// Condition dialog class.
    /// Shows a visual dialog during waits with color-coded status feedback.
    /// The polling loop runs on the calling thread; only the dialog UI runs on a background thread.
    /// </summary>
    public class ConditionDialogWaiter : IWaiter
    {
        // AsyncLocal works across async/await and Task boundaries for reentrancy detection
        private static readonly AsyncLocal<bool> isInsideWait = new AsyncLocal<bool>();
        private static readonly SilentWaiter silentWaiter = new SilentWaiter();

        private readonly object m = new object();
        private DialogView uic;
        private volatile bool dialogReady;
        private volatile int userAction; // 0=none, 1=positive, -1=negative
        private static readonly TimeSpan timerPeriod = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan negativeWaitTime = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan positiveWaitTime = TimeSpan.FromSeconds(0);
        private static readonly TimeSpan positiveWaitTimeWithAction = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionDialogWaiter"/> class.
        /// </summary>
        public ConditionDialogWaiter()
        {
        }

        /// <summary>
        /// Windows enum for enabling click-through.
        /// </summary>
        private enum GWL
        {
            /// <summary>
            /// A value.
            /// </summary>
            ExStyle = -20
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr wnd, GWL index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr wnd, GWL index, int newLong);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr wnd, int msg, int param1, int param2);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hwnd, int wmsg, bool wparam, int lparam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        /// <summary>
        /// Starts the dialog on a background thread and waits until it is loaded.
        /// </summary>
        /// <param name="expectationText">Text describing the expected condition.</param>
        /// <param name="actionText">The action text.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="cts">Cancellation token source to cancel the polling loop on user action.</param>
        /// <returns>The background dialog thread.</returns>
        private Thread StartDialog(string expectationText, string actionText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, CancellationTokenSource cts)
        {
            dialogReady = false;
            userAction = 0;

            var dialogThread = new Thread(() =>
            {
                uic.RunUI(
                    dialogLoad: () =>
                    {
                        lock (m)
                        {
                            uic.ExpectationText = expectationText;
                            if (actionText != null)
                            {
                                uic.ActionText = actionText;
                            }

                            uic.GoodButtonEnabled = null;
                            uic.AutoActionGoodText = positiveTimeout;
                            uic.AutoActionBadText = negativeTimeout;
                            uic.Value = "unknown";
                            uic.Show();
                            dialogReady = true;
                        }
                    },
                    badClick: () =>
                    {
                        lock (m)
                        {
                            userAction = -1;
                            uic.Close();
                            cts.Cancel();
                        }
                    },
                    goodClick: () =>
                    {
                        lock (m)
                        {
                            userAction = 1;
                            uic.Close();
                            cts.Cancel();
                        }
                    });
            });
            dialogThread.IsBackground = true;
            dialogThread.Start();

            // Wait for dialog to be ready before polling starts
            SpinWait.SpinUntil(() => dialogReady);

            return dialogThread;
        }

        /// <summary>
        /// Closes the dialog and waits for the dialog thread to finish.
        /// </summary>
        /// <param name="dialogThread">The dialog thread.</param>
        private void CloseDialogAndJoin(Thread dialogThread)
        {
            lock (m)
            {
                if (uic != null)
                {
                    uic.Close();
                }
            }

            dialogThread.Join(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Core wait logic shared by sync and async variants.
        /// Runs the polling engine on the calling thread with a dialog overlay on a background thread.
        /// </summary>
        /// <typeparam name="T">The type returned by the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The sync condition to evaluate.</param>
        /// <param name="asyncCondition">The async condition to evaluate (mutually exclusive with condition).</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        /// <param name="actionText">The action text.</param>
        private void WaitWithDialog<T>(Func<T> function, Predicate<T> condition, Func<T, Task<bool>> asyncCondition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText)
        {
            // Detect nested waits and delegate to SilentWaiter to prevent multiple dialogs
            if (isInsideWait.Value)
            {
                if (asyncCondition != null)
                {
                    silentWaiter.GenericWaitForAsync(function, asyncCondition, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, actionText).GetAwaiter().GetResult();
                }
                else
                {
                    silentWaiter.GenericWaitFor(function, condition, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, actionText);
                }

                return;
            }

            isInsideWait.Value = true;
            try
            {
                uic = new DialogView(negativeTimeout != TimeSpan.MaxValue, positiveTimeout != TimeSpan.MaxValue && positiveTimeout != TimeSpan.Zero, clickThrough, function != null, actionText, expectationText.Split('\n').Count());

                var cts = new CancellationTokenSource();
                var timeoutHelper = new TimeoutHelper(negativeTimeout, positiveTimeout);
                var dialogThread = StartDialog(expectationText, actionText, negativeTimeout, positiveTimeout, cts);

                bool success = false;
                Exception lastException = null;
                var timerStopwatch = Stopwatch.StartNew();

                // Handle manual interaction mode (null function/condition)
                if (function == null && condition == null && asyncCondition == null)
                {
                    // Pure manual mode: just wait for user action or timeout
                    while (!cts.IsCancellationRequested)
                    {
                        // Update countdown display
                        UpdateCountdownDisplay(timeoutHelper, false);

                        // Check negative timeout
                        if (timeoutHelper.IsNegativeTimeoutElapsed)
                        {
                            break;
                        }

                        PollingEngine.Sleep(timerPeriod, cts.Token);
                    }
                }
                else
                {
                    var callbacks = new PollingCallbacks
                    {
                        OnValueChanged = value =>
                        {
                            lock (m)
                            {
                                if (dialogReady && userAction == 0)
                                {
                                    uic.Value = value;
                                }
                            }
                        },
                        OnTruthChanged = truth =>
                        {
                            lock (m)
                            {
                                if (dialogReady && userAction == 0)
                                {
                                    uic.SuspendDrawing();
                                    uic.GoodButtonEnabled = truth;
                                    uic.ResumeDrawing();
                                }
                            }
                        },
                        OnPollComplete = result =>
                        {
                            lastException = result.Exception;

                            // Update countdown display periodically
                            if (timerStopwatch.Elapsed >= timerPeriod)
                            {
                                UpdateCountdownDisplay(timeoutHelper, result.Truth);
                                timerStopwatch.Restart();
                            }

                            return timeoutHelper.ProcessPollResult(result.Truth, result.Exception != null, expectationText, cts, ref success, ref lastException);
                        }
                    };

                    if (asyncCondition != null)
                    {
                        PollingEngine.RunWithAsyncCondition(function, asyncCondition, pollingPeriod, cts.Token, callbacks);
                    }
                    else
                    {
                        PollingEngine.Run(function, condition, pollingPeriod, cts.Token, callbacks);
                    }
                }

                // Close dialog and wait for dialog thread
                CloseDialogAndJoin(dialogThread);

                // Determine result
                if (success)
                {
                    return;
                }

                if (userAction == 1)
                {
                    // User clicked positive
                    return;
                }

                if (userAction == -1)
                {
                    // User clicked negative
                    throw new WaitForAbortedException(expectationText);
                }

                // Timeout
                if (lastException != null)
                {
                    throw lastException;
                }

                throw new WaitForTimeoutException(expectationText, timeoutHelper.EffectiveNegativeTimeout);
            }
            finally
            {
                isInsideWait.Value = false;
            }
        }

        /// <summary>
        /// Updates the countdown labels on the dialog.
        /// </summary>
        /// <param name="timeoutHelper">The timeout helper.</param>
        /// <param name="isGood">Whether the condition is currently true.</param>
        private void UpdateCountdownDisplay(TimeoutHelper timeoutHelper, bool isGood)
        {
            lock (m)
            {
                if (!dialogReady || userAction != 0)
                {
                    return;
                }

                if (!isGood)
                {
                    var negRemaining = timeoutHelper.NegativeRemaining;
                    if (negRemaining != TimeSpan.MaxValue)
                    {
                        uic.AutoActionBadText = negRemaining > TimeSpan.Zero ? negRemaining : TimeSpan.Zero;
                    }
                }
                else
                {
                    var posRemaining = timeoutHelper.PositiveRemaining;
                    if (posRemaining != TimeSpan.MaxValue)
                    {
                        uic.AutoActionGoodText = posRemaining > TimeSpan.Zero ? posRemaining : TimeSpan.Zero;
                    }
                }
            }
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        /// <param name="actionText">The action text.</param>
        public void GenericWaitFor<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText)
        {
            WaitWithDialog(function, condition, null, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, actionText);
        }

        /// <summary>
        /// Waits until an async function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The async condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        /// <param name="actionText">The action text.</param>
        public Task GenericWaitForAsync<T>(Func<T> function, Func<T, Task<bool>> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText)
        {
            WaitWithDialog(function, null, condition, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, actionText);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The view class.
        /// </summary>
        private class DialogView
        {
            // create and add dialog components
            private readonly Label expectedHeaderLabel;
            private readonly Label expectedTextLabel;
            private readonly Label actionHeaderLabel;
            private readonly Label actionTextLabel;
            private readonly Label valueLabel;
            private readonly Label autoActionGoodLabel;
            private readonly Label autoActionBadLabel;
            private readonly Button positiveButton;
            private readonly Button negativeButton;
            private readonly Form dialog;
            private readonly bool clickThrough;
            private readonly bool showCurrentValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="DialogView"/> class.
            /// </summary>
            /// <param name="showAutoActionBadLabel">Whether to show the auto action bad label.</param>
            /// <param name="showAutoActionGoodLabel">Whether to show the auto action good label.</param>
            /// <param name="clickThrough">Whether to enable click-through mode.</param>
            /// <param name="showCurrentValue">Whether to show the current value.</param>
            /// <param name="actionText">The action text.</param>
            /// <param name="expectationLines">The expectation lines.</param>
            public DialogView(bool showAutoActionBadLabel, bool showAutoActionGoodLabel, bool clickThrough, bool showCurrentValue, string actionText, int expectationLines)
            {
                this.clickThrough = clickThrough;
                this.showCurrentValue = showCurrentValue;

                const int WIDTH = 500;
                const int SPACE = 10;
                const int BUTTON_HEIGHT = 60;
                const int ITOP = 20;
                const int LEFT = 30;
                int top = ITOP;

                var font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
                var headerFont = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold);

                // dialog
                dialog = new Form() { Width = LEFT + WIDTH, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, Opacity = 0.8, StartPosition = FormStartPosition.Manual, Visible = false, TopMost = false };

                if (!clickThrough)
                {
                    var dialogWidth = dialog.Width;

                    top += SPACE;

                    // positive button
                    dialog.Controls.Add(positiveButton = new Button() { Left = dialogWidth / 2, Height = BUTTON_HEIGHT, Width = dialogWidth / 2, Top = top, DialogResult = DialogResult.OK, Text = "Positive", FlatStyle = FlatStyle.Flat, Font = font });

                    // negative button
                    dialog.Controls.Add(negativeButton = new Button() { Left = 0, Height = BUTTON_HEIGHT, Width = dialogWidth / 2, Top = top, DialogResult = DialogResult.Cancel, Text = "Negative", FlatStyle = FlatStyle.Flat, BackColor = Color.Red, Font = font });

                    top += BUTTON_HEIGHT;
                    top += SPACE;
                }

                // expected header
                dialog.Controls.Add(expectedHeaderLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = headerFont, Height = headerFont.Height, Text = "Expectation" });
                top += expectedHeaderLabel.Height;

                // expected text
                dialog.Controls.Add(expectedTextLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = font, Height = font.Height });
                top += expectationLines * expectedTextLabel.Height;
                expectedTextLabel.Height = expectationLines * expectedTextLabel.Height;
                expectedTextLabel.AutoSize = true;

                if (actionText != null)
                {
                    top += SPACE;

                    // action header
                    dialog.Controls.Add(actionHeaderLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = headerFont, Height = headerFont.Height, Text = "Action" });
                    top += actionHeaderLabel.Height;

                    // action text
                    dialog.Controls.Add(actionTextLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = font, Height = font.Height });
                    var actionLines = actionText.Split('\n').Count();
                    top += actionLines * actionTextLabel.Height;
                    actionTextLabel.Height = actionLines * actionTextLabel.Height;
                    actionTextLabel.AutoSize = true;
                }

                if (showCurrentValue)
                {
                    top += SPACE;
                    dialog.Controls.Add(valueLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = font, Height = font.Height });
                    top += valueLabel.Height;
                }

                // good timeout label
                if (showAutoActionGoodLabel)
                {
                    top += SPACE;
                    dialog.Controls.Add(autoActionGoodLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = font, Height = font.Height });
                    top += autoActionGoodLabel.Height;
                }

                // bad timeout label
                if (showAutoActionBadLabel)
                {
                    top += SPACE;
                    dialog.Controls.Add(autoActionBadLabel = new Label() { Left = LEFT, Top = top, Width = WIDTH, Font = font, Height = font.Height });
                    top += autoActionBadLabel.Height;
                }

                // set dialog size
                dialog.Height = top + ITOP;
                dialog.Location = new Point(Screen.PrimaryScreen.Bounds.Width - dialog.Width, 0);
            }

            /// <summary>
            /// Sets the auto action good text.
            /// </summary>
            public TimeSpan AutoActionGoodText
            {
                set { Invoke(() => autoActionGoodLabel.Text = "Continue in " + value.TotalSeconds.ToString("0.0") + " seconds", autoActionGoodLabel != null); }
            }

            /// <summary>
            /// Sets the auto action bad text.
            /// </summary>
            public TimeSpan AutoActionBadText
            {
                set { Invoke(() => autoActionBadLabel.Text = "Abort in " + value.TotalSeconds.ToString("0.0") + " seconds", autoActionBadLabel != null); }
            }

            /// <summary>
            /// Sets the expected text.
            /// </summary>
            public string ExpectationText
            {
                set { Invoke(() => expectedTextLabel.Text = value); }
            }

            /// <summary>
            /// Sets the action text.
            /// </summary>
            public string ActionText
            {
                set { Invoke(() => actionTextLabel.Text = value); }
            }

            /// <summary>
            /// Sets the good button text.
            /// </summary>
            public string GoodButtonText
            {
                set { Invoke(() => positiveButton.Text = value); }
            }

            /// <summary>
            /// Sets the bad button text.
            /// </summary>
            public string BadButtonText
            {
                set { Invoke(() => negativeButton.Text = value); }
            }

            /// <summary>
            /// Sets the current value.
            /// </summary>
            public string Value
            {
                set { Invoke(() => valueLabel.Text = "Observed value: " + value, showCurrentValue); }
            }

            /// <summary>
            /// Sets a value indicating whether the good button is enabled.
            /// </summary>
            public bool? GoodButtonEnabled
            {
                set
                {
                    Invoke(() =>
                    {
                        if (positiveButton != null)
                        {
                            positiveButton.Enabled = !(value == false);
                        }

                        dialog.BackColor = !value.HasValue ? Color.Gray : (value == true ? Color.Green : Color.Red);
                        dialog.Invalidate();
                    });
                }
            }

            /// <summary>
            /// Invoke an action.
            /// </summary>
            /// <param name="a">action.</param>
            /// <param name="condition">Whether to execute the action.</param>
            private void Invoke(Action a, bool condition = true)
            {
                if (condition)
                {
                    if (dialog.InvokeRequired && dialog.IsHandleCreated && !dialog.IsDisposed)
                    {
                        dialog.BeginInvoke((MethodInvoker)(() => a()));
                    }
                    else
                    {
                        a();
                    }
                }
            }

            /// <summary>
            /// Run the dialog synchronously on current thread.
            /// </summary>
            /// <param name="dialogLoad">The dialog load action.</param>
            /// <param name="badClick">The bad click action.</param>
            /// <param name="goodClick">The good click action.</param>
            public void RunUI(Action dialogLoad, Action badClick, Action goodClick)
            {
                dialog.Load += (s, o) => dialogLoad();
                dialog.FormClosed += (s, o) => Application.ExitThread();
                
                if (negativeButton != null)
                {
                    negativeButton.Click += (s, o) => badClick();
                }

                if (positiveButton != null)
                {
                    positiveButton.Click += (s, o) => goodClick();
                }

                if (!clickThrough)
                {
                    dialog.MouseMove += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            ReleaseCapture();
                            SendMessage(dialog.Handle, 0xA1, 0x2, 0);
                        }
                    };
                }

                dialog.Show();
                Application.Run(); // Blocks until ExitThread
            }

            /// <summary>
            /// Close the dialog.
            /// </summary>
            public void Close()
            {
                Invoke(() => {
                    dialog.Close();
                    dialog.Dispose();
                });
            }

            /// <summary>
            /// Sets a value indicating whether the view is visible.
            /// </summary>
            public void Show()
            {
                Invoke(() =>
                {
                    dialog.Visible = true;
                    dialog.BringToFront();
                    dialog.TopMost = true;
                    dialog.TopLevel = true;
                    if (clickThrough)
                    {
                        MakeClickThrough();
                    }
                });
            }

            /// <summary>
            /// Suspend drawing.
            /// </summary>
            public void SuspendDrawing()
            {
                Invoke(() => SendMessage(dialog.Handle, 11, false, 0));
            }

            /// <summary>
            /// Resume drawing.
            /// </summary>
            public void ResumeDrawing()
            {
                Invoke(() =>
                {
                    SendMessage(dialog.Handle, 11, true, 0);
                    dialog.Refresh();
                });
            }

            /// <summary>
            /// Make dialog click-through.
            /// </summary>
            private void MakeClickThrough()
            {
                int wl = GetWindowLong(dialog.Handle, GWL.ExStyle);
                wl = wl | 0x80000 | 0x20;
                SetWindowLong(dialog.Handle, GWL.ExStyle, wl);
            }
        }
    }
}
