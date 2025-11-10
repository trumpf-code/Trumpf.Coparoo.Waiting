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

namespace Trumpf.Coparoo.Waiting.UI
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    /// <summary>
    /// Manages the layout and styling of the waiting dialog UI.
    /// Separates presentation concerns from business logic.
    /// </summary>
    internal class WaitingDialogLayout
    {
        private readonly Form dialog;
        private readonly Panel contentPanel;
        private readonly Panel statusBar;
        private readonly Panel topStatusBar;
        private Label expectationHeaderLabel;
        private Label expectationTextLabel;
        private Label actionHeaderLabel;
        private Label actionTextLabel;
        private Label valueLabel;
        private Label autoActionGoodLabel;
        private Label autoActionBadLabel;
        private ModernButton positiveButton;
        private ModernButton negativeButton;

        private DialogState currentState;
        private Timer transitionTimer;
        private Color targetBackColor;
        private Color currentBackColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitingDialogLayout"/> class.
        /// </summary>
        /// <param name="dialog">The form dialog to style and layout.</param>
        /// <param name="showButtons">Whether to show interactive buttons.</param>
        /// <param name="showAutoActionGoodLabel">Whether to show the auto action good label.</param>
        /// <param name="showAutoActionBadLabel">Whether to show the auto action bad label.</param>
        /// <param name="showCurrentValue">Whether to show the current value.</param>
        /// <param name="hasActionText">Whether action text will be displayed.</param>
        /// <param name="expectationLines">The number of lines in the expectation text.</param>
        public WaitingDialogLayout(
            Form dialog,
            bool showButtons,
            bool showAutoActionGoodLabel,
            bool showAutoActionBadLabel,
            bool showCurrentValue,
            bool hasActionText,
            int expectationLines)
        {
            this.dialog = dialog;
            currentState = DialogState.Unknown;
            currentBackColor = ModernTheme.GetStateColor(DialogState.Unknown);

            // Initialize animation timer
            transitionTimer = new Timer { Interval = 16 }; // ~60fps
            transitionTimer.Tick += TransitionTimer_Tick;

            // Configure main dialog
            ConfigureDialog();

            // Create top status bar
            topStatusBar = CreateTopStatusBar();
            dialog.Controls.Add(topStatusBar);

            // Create status bar (bottom)
            statusBar = CreateStatusBar();
            dialog.Controls.Add(statusBar);

            // Create content panel
            contentPanel = CreateContentPanel();
            dialog.Controls.Add(contentPanel);

            int top = ModernTheme.Layout.Padding;

            // Create buttons if needed
            if (showButtons)
            {
                CreateButtons();
                contentPanel.Controls.Add(positiveButton);
                contentPanel.Controls.Add(negativeButton);
                top += ModernTheme.Layout.ButtonHeight + ModernTheme.Layout.Spacing;
            }

            // Create expectation section
            expectationHeaderLabel = CreateHeaderLabel("Expectation", top);
            contentPanel.Controls.Add(expectationHeaderLabel);
            top += expectationHeaderLabel.Height + ModernTheme.Layout.SpacingSmall;

            expectationTextLabel = CreateBodyLabel(top, expectationLines);
            contentPanel.Controls.Add(expectationTextLabel);
            top += expectationTextLabel.Height + ModernTheme.Layout.Spacing;

            // Create action section if needed
            if (hasActionText)
            {
                actionHeaderLabel = CreateHeaderLabel("Action", top);
                contentPanel.Controls.Add(actionHeaderLabel);
                top += actionHeaderLabel.Height + ModernTheme.Layout.SpacingSmall;

                actionTextLabel = CreateBodyLabel(top, 1);
                contentPanel.Controls.Add(actionTextLabel);
                top += actionTextLabel.Height + ModernTheme.Layout.Spacing;
            }

            // Create value label if needed
            if (showCurrentValue)
            {
                valueLabel = CreateBodyLabel(top, 1);
                valueLabel.ForeColor = ModernTheme.Colors.TextSecondary;
                contentPanel.Controls.Add(valueLabel);
                top += valueLabel.Height + ModernTheme.Layout.Spacing;
            }

            // Create timer labels
            if (showAutoActionGoodLabel)
            {
                autoActionGoodLabel = CreateTimerLabel(top);
                contentPanel.Controls.Add(autoActionGoodLabel);
                top += autoActionGoodLabel.Height + ModernTheme.Layout.SpacingSmall;
            }

            if (showAutoActionBadLabel)
            {
                autoActionBadLabel = CreateTimerLabel(top);
                contentPanel.Controls.Add(autoActionBadLabel);
                top += autoActionBadLabel.Height + ModernTheme.Layout.SpacingSmall;
            }

            // Set final dialog size
            int totalHeight = top + ModernTheme.Layout.Padding + statusBar.Height;
            dialog.Height = Math.Max(totalHeight, ModernTheme.Layout.MinDialogHeight);
            contentPanel.Height = dialog.Height - statusBar.Height;
        }

        /// <summary>
        /// Gets the expectation text label.
        /// </summary>
        public Label ExpectationTextLabel => expectationTextLabel;

        /// <summary>
        /// Gets the action text label.
        /// </summary>
        public Label ActionTextLabel => actionTextLabel;

        /// <summary>
        /// Gets the value label.
        /// </summary>
        public Label ValueLabel => valueLabel;

        /// <summary>
        /// Gets the auto action good label.
        /// </summary>
        public Label AutoActionGoodLabel => autoActionGoodLabel;

        /// <summary>
        /// Gets the auto action bad label.
        /// </summary>
        public Label AutoActionBadLabel => autoActionBadLabel;

        /// <summary>
        /// Gets the positive button.
        /// </summary>
        public ModernButton PositiveButton => positiveButton;

        /// <summary>
        /// Gets the negative button.
        /// </summary>
        public ModernButton NegativeButton => negativeButton;

        /// <summary>
        /// Sets the dialog state and updates visual appearance.
        /// </summary>
        public void SetState(DialogState state)
        {
            if (currentState == state) return;

            currentState = state;
            targetBackColor = ModernTheme.GetStateColor(state);
            transitionTimer.Start();

            // Update top status bar with new state color
            topStatusBar.BackColor = ModernTheme.GetStateLightColor(state);
        }

        /// <summary>
        /// Configures the main dialog form.
        /// </summary>
        private void ConfigureDialog()
        {
            dialog.FormBorderStyle = FormBorderStyle.None;
            dialog.ShowInTaskbar = false;
            dialog.StartPosition = FormStartPosition.Manual;
            dialog.BackColor = ModernTheme.Colors.Background;
            dialog.Width = ModernTheme.Layout.DialogWidth;

            // Enable double buffering for smooth rendering
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dialog, true);

            // Custom paint for rounded corners and shadow
            dialog.Paint += Dialog_Paint;

            // Set dialog region for rounded corners
            UpdateDialogRegion();
            dialog.Resize += (s, e) => UpdateDialogRegion();

            // Position at top-right of screen
            dialog.Location = new Point(
                Screen.PrimaryScreen.Bounds.Width - dialog.Width - 20,
                20);
        }

        /// <summary>
        /// Updates the dialog region for rounded corners.
        /// </summary>
        private void UpdateDialogRegion()
        {
                    // Use slightly inset rectangle to avoid edge aliasing artifacts on transparent backgrounds.
                    int inset = 1;
                    var rect = new Rectangle(inset, inset, dialog.Width - inset * 2, dialog.Height - inset * 2);
                    using (var path = ModernTheme.CreateRoundedRectangle(rect, ModernTheme.Layout.CornerRadius * 2))
                    {
                        dialog.Region = new Region(path);
                    }
        }

        /// <summary>
        /// Custom paint handler for dialog shadow and border.
        /// </summary>
        private void Dialog_Paint(object sender, PaintEventArgs e)
        {
            // Supersampled border rendering to reduce pixelation on translucent backgrounds.
            int scale = 2;
            int w = dialog.Width * scale;
            int h = dialog.Height * scale;
            using (var buffer = new Bitmap(w, h))
            using (var g = Graphics.FromImage(buffer))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(dialog.BackColor);

                // Outer shadow path (slightly larger radius)
                var outerRect = new Rectangle(0, 0, w - scale, h - scale);
                using (var shadowPath = ModernTheme.CreateRoundedRectangle(outerRect, ModernTheme.Layout.CornerRadius * scale * 2))
                using (var shadowPen = new Pen(Color.FromArgb(28, 0, 0, 0), 2 * scale))
                {
                    g.DrawPath(shadowPen, shadowPath);
                }

                // Inner highlight border
                var innerRect = new Rectangle(scale * 2, scale * 2, w - scale * 4, h - scale * 4);
                using (var innerPath = ModernTheme.CreateRoundedRectangle(innerRect, ModernTheme.Layout.CornerRadius * scale * 2))
                using (var innerPen = new Pen(Color.FromArgb(60, Color.White), scale))
                {
                    g.DrawPath(innerPen, innerPath);
                }

                // Downscale to screen
                var target = e.Graphics;
                target.InterpolationMode = InterpolationMode.HighQualityBicubic;
                target.PixelOffsetMode = PixelOffsetMode.HighQuality;
                target.SmoothingMode = SmoothingMode.HighQuality;
                target.DrawImage(buffer, new Rectangle(0, 0, dialog.Width, dialog.Height));
            }
        }

        /// <summary>
        /// Creates the top status bar.
        /// </summary>
        private Panel CreateTopStatusBar()
        {
            var bar = new Panel
            {
                Height = 8,
                Dock = DockStyle.Top,
                BackColor = ModernTheme.GetStateLightColor(DialogState.Unknown)
            };

            return bar;
        }

        /// <summary>
        /// Creates the status bar at the bottom of the dialog.
        /// </summary>
        private Panel CreateStatusBar()
        {
            var bar = new Panel
            {
                Height = 8,
                Dock = DockStyle.Bottom,
                BackColor = currentBackColor
            };

            return bar;
        }

        /// <summary>
        /// Creates the main content panel.
        /// </summary>
        private Panel CreateContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Colors.Background,
                Padding = new Padding(ModernTheme.Layout.Padding)
            };

            return panel;
        }

        /// <summary>
        /// Creates the interactive buttons.
        /// </summary>
        private void CreateButtons()
        {
            int availableWidth = contentPanel.Width - ModernTheme.Layout.Padding * 2;
            int buttonWidth = (availableWidth - ModernTheme.Layout.Spacing) / 2;
            int top = ModernTheme.Layout.Padding;

            positiveButton = new ModernButton
            {
                Width = buttonWidth,
                Height = ModernTheme.Layout.ButtonHeight,
                Location = new Point(
                    ModernTheme.Layout.Padding + buttonWidth + ModernTheme.Layout.Spacing,
                    top),
                Text = "Continue",
                NormalColor = ModernTheme.Colors.ButtonPositive,
                HoverColor = ModernTheme.Colors.ButtonPositiveHover,
                DisabledColor = ModernTheme.Colors.ButtonDisabled,
                TabStop = false,
                BackColor = Color.Transparent
            };

            negativeButton = new ModernButton
            {
                Width = buttonWidth,
                Height = ModernTheme.Layout.ButtonHeight,
                Location = new Point(ModernTheme.Layout.Padding, top),
                Text = "Abort",
                NormalColor = ModernTheme.Colors.ButtonNegative,
                HoverColor = ModernTheme.Colors.ButtonNegativeHover,
                DisabledColor = ModernTheme.Colors.ButtonDisabled,
                TabStop = false,
                BackColor = Color.Transparent
            };
        }

        /// <summary>
        /// Creates a header label.
        /// </summary>
        private Label CreateHeaderLabel(string text, int top)
        {
            var label = new Label
            {
                Text = text,
                Font = ModernTheme.Typography.GetHeaderFont(),
                ForeColor = ModernTheme.Colors.TextPrimary,
                AutoSize = true,
                Location = new Point(ModernTheme.Layout.Padding, top),
                Width = contentPanel.Width - ModernTheme.Layout.Padding * 2
            };

            return label;
        }

        /// <summary>
        /// Creates a body text label.
        /// </summary>
        private Label CreateBodyLabel(int top, int lines)
        {
            var label = new Label
            {
                Font = ModernTheme.Typography.GetBodyFont(),
                ForeColor = ModernTheme.Colors.TextPrimary,
                AutoSize = false,
                Location = new Point(ModernTheme.Layout.Padding, top),
                Width = contentPanel.Width - ModernTheme.Layout.Padding * 2,
                Height = (int)(ModernTheme.Typography.GetBodyFont().GetHeight() * lines * 1.4)
            };

            return label;
        }

        /// <summary>
        /// Creates a timer label.
        /// </summary>
        private Label CreateTimerLabel(int top)
        {
            var label = new Label
            {
                Font = ModernTheme.Typography.GetTimerFont(),
                ForeColor = ModernTheme.Colors.TextSecondary,
                AutoSize = false,
                Location = new Point(ModernTheme.Layout.Padding, top),
                Width = contentPanel.Width - ModernTheme.Layout.Padding * 2,
                Height = (int)(ModernTheme.Typography.GetTimerFont().GetHeight() * 1.4)
            };

            return label;
        }

        /// <summary>
        /// Handles smooth color transitions.
        /// </summary>
        private void TransitionTimer_Tick(object sender, EventArgs e)
        {
            int r = (int)(currentBackColor.R + (targetBackColor.R - currentBackColor.R) * 0.15f);
            int g = (int)(currentBackColor.G + (targetBackColor.G - currentBackColor.G) * 0.15f);
            int b = (int)(currentBackColor.B + (targetBackColor.B - currentBackColor.B) * 0.15f);

            currentBackColor = Color.FromArgb(r, g, b);
            statusBar.BackColor = currentBackColor;

            if (Math.Abs(currentBackColor.R - targetBackColor.R) < 2 &&
                Math.Abs(currentBackColor.G - targetBackColor.G) < 2 &&
                Math.Abs(currentBackColor.B - targetBackColor.B) < 2)
            {
                currentBackColor = targetBackColor;
                statusBar.BackColor = targetBackColor;
                transitionTimer.Stop();
            }
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            transitionTimer?.Dispose();
        }
    }
}
