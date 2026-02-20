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

namespace Trumpf.Coparoo.Waiting
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Exceptions;
    using Interfaces;

    /// <summary>
    /// Silent waiter class that performs waiting without showing a dialog.
    /// </summary>
    public class SilentWaiter : IWaiter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SilentWaiter"/> class.
        /// </summary>
        public SilentWaiter()
        {
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Does not show a dialog.
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
            ValidateParameters(actionText, positiveTimeout, negativeTimeout, function, condition);

            var cts = new CancellationTokenSource();
            var timeoutHelper = new TimeoutHelper(negativeTimeout, positiveTimeout);
            bool success = false;
            Exception lastException = null;

            var callbacks = new PollingCallbacks
            {
                OnPollComplete = result =>
                {
                    lastException = result.Exception;
                    return timeoutHelper.ProcessPollResult(result.Truth, result.Exception != null, expectationText, cts, ref success, ref lastException);
                }
            };

            PollingEngine.Run(function, condition, pollingPeriod, cts.Token, callbacks);

            if (success)
            {
                return;
            }

            // Timeout or cancellation reached
            if (lastException != null)
            {
                throw lastException;
            }

            throw new WaitForTimeoutException(expectationText, timeoutHelper.EffectiveNegativeTimeout);
        }

        /// <summary>
        /// Waits until an async function evaluates to <c>true</c>.
        /// Does not show a dialog.
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
            ValidateParameters(actionText, positiveTimeout, negativeTimeout, function, condition);

            var cts = new CancellationTokenSource();
            var timeoutHelper = new TimeoutHelper(negativeTimeout, positiveTimeout);
            bool success = false;
            Exception lastException = null;

            var callbacks = new PollingCallbacks
            {
                OnPollComplete = result =>
                {
                    lastException = result.Exception;
                    return timeoutHelper.ProcessPollResult(result.Truth, result.Exception != null, expectationText, cts, ref success, ref lastException);
                }
            };

            PollingEngine.RunWithAsyncCondition(function, condition, pollingPeriod, cts.Token, callbacks);

            if (success)
            {
                return Task.CompletedTask;
            }

            // Timeout or cancellation reached
            if (lastException != null)
            {
                throw lastException;
            }

            throw new WaitForTimeoutException(expectationText, timeoutHelper.EffectiveNegativeTimeout);
        }

        /// <summary>
        /// Validates parameters that SilentWaiter cannot support.
        /// </summary>
        private static void ValidateParameters<T>(string actionText, TimeSpan positiveTimeout, TimeSpan negativeTimeout, Func<T> function, object condition)
        {
            // Throw exception if action text is provided (requires human interaction)
            if (!string.IsNullOrEmpty(actionText))
            {
                throw new InvalidOperationException("SilentWaiter does not support action text as it requires human interaction.");
            }

            // Throw exception if positive timeout is MaxValue (would wait forever without user interaction)
            if (positiveTimeout == TimeSpan.MaxValue)
            {
                throw new InvalidOperationException("SilentWaiter does not support infinite positive timeout (TimeSpan.MaxValue) as it would wait forever without user interaction.");
            }

            // Throw exception if both function and condition are null with infinite timeout (requires manual acknowledgment)
            if (function == null && condition == null && negativeTimeout == TimeSpan.MaxValue)
            {
                throw new InvalidOperationException("SilentWaiter does not support manual acknowledgment mode (null function and condition with infinite timeout).");
            }
        }
    }

    /// <summary>
    /// Manages timeout logic for polling-based waiters.
    /// Tracks negative (overall) and positive (condition-must-stay-true) timeouts using stopwatch-based timing.
    /// </summary>
    internal class TimeoutHelper
    {
        private readonly Stopwatch negativeStopwatch = Stopwatch.StartNew();
        private Stopwatch positiveStopwatch;
        private bool wasInGoodState;

        /// <summary>
        /// Gets the effective negative timeout.
        /// </summary>
        public TimeSpan EffectiveNegativeTimeout { get; }

        /// <summary>
        /// Gets the effective positive timeout.
        /// </summary>
        public TimeSpan EffectivePositiveTimeout { get; }

        /// <summary>
        /// Gets whether the negative timeout is infinite.
        /// </summary>
        public bool IsInfiniteNegativeTimeout { get; }

        /// <summary>
        /// Gets whether the negative timeout has elapsed.
        /// </summary>
        public bool IsNegativeTimeoutElapsed =>
            !IsInfiniteNegativeTimeout && negativeStopwatch.Elapsed >= EffectiveNegativeTimeout;

        /// <summary>
        /// Gets the remaining negative timeout.
        /// </summary>
        public TimeSpan NegativeRemaining =>
            IsInfiniteNegativeTimeout ? TimeSpan.MaxValue : EffectiveNegativeTimeout - negativeStopwatch.Elapsed;

        /// <summary>
        /// Gets the remaining positive timeout, or MaxValue if not in good state.
        /// </summary>
        public TimeSpan PositiveRemaining =>
            wasInGoodState && positiveStopwatch != null ? EffectivePositiveTimeout - positiveStopwatch.Elapsed : TimeSpan.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutHelper"/> class.
        /// </summary>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        public TimeoutHelper(TimeSpan negativeTimeout, TimeSpan positiveTimeout)
        {
            EffectiveNegativeTimeout = negativeTimeout < TimeSpan.Zero ? TimeSpan.Zero : negativeTimeout;
            EffectivePositiveTimeout = positiveTimeout < TimeSpan.Zero ? TimeSpan.Zero : positiveTimeout;
            IsInfiniteNegativeTimeout = EffectiveNegativeTimeout == TimeSpan.MaxValue;
        }

        /// <summary>
        /// Processes a poll result and determines whether polling should stop.
        /// Manages positive/negative timeout state transitions.
        /// </summary>
        /// <param name="truth">Whether the condition is true.</param>
        /// <param name="hasException">Whether an exception occurred during evaluation.</param>
        /// <param name="expectationText">The expectation text for timeout exceptions.</param>
        /// <param name="cts">The cancellation token source to cancel on decision.</param>
        /// <param name="success">Set to true if the wait completed successfully.</param>
        /// <param name="lastException">Updated with exception state.</param>
        /// <returns>True if polling should stop.</returns>
        public bool ProcessPollResult(bool truth, bool hasException, string expectationText, CancellationTokenSource cts, ref bool success, ref Exception lastException)
        {
            if (hasException)
            {
                // Exception treated as false
                if (wasInGoodState)
                {
                    positiveStopwatch = null;
                    wasInGoodState = false;
                }
            }
            else if (truth)
            {
                // Clear exception on success
                lastException = null;

                if (!wasInGoodState)
                {
                    // Transition from bad to good state - start positive timeout
                    positiveStopwatch = Stopwatch.StartNew();
                    wasInGoodState = true;
                }

                // Check if positive timeout is satisfied
                if (EffectivePositiveTimeout == TimeSpan.Zero ||
                    positiveStopwatch.Elapsed >= EffectivePositiveTimeout)
                {
                    success = true;
                    cts.Cancel();
                    return true;
                }
            }
            else
            {
                // Condition is false, no exception
                lastException = null;

                if (wasInGoodState)
                {
                    // Transition from good to bad state - reset positive timeout
                    positiveStopwatch = null;
                    wasInGoodState = false;
                }
            }

            // Check negative timeout (only when not in positive phase;
            // once the condition is true we only care about the positive countdown)
            if (!wasInGoodState && IsNegativeTimeoutElapsed)
            {
                cts.Cancel();
                return true;
            }

            return false;
        }
    }
}
