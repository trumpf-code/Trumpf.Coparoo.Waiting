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

    /// <summary>
    /// Result of a single polling iteration.
    /// </summary>
    internal struct PollResult
    {
        /// <summary>
        /// Gets or sets the truth value of the condition evaluation.
        /// </summary>
        public bool Truth { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during evaluation, or null if successful.
        /// </summary>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Callbacks for polling engine consumers to react to evaluation changes.
    /// </summary>
    internal class PollingCallbacks
    {
        /// <summary>
        /// Called when the evaluated value changes (as string representation).
        /// </summary>
        public Action<string> OnValueChanged { get; set; }

        /// <summary>
        /// Called when the truth value changes.
        /// </summary>
        public Action<bool> OnTruthChanged { get; set; }

        /// <summary>
        /// Called after each poll iteration with the result. Return true to stop polling.
        /// </summary>
        public Func<PollResult, bool> OnPollComplete { get; set; }
    }

    /// <summary>
    /// Shared polling engine used by both <see cref="SilentWaiter"/> and ConditionDialogWaiter.
    /// Evaluates a function and condition in a loop, notifying consumers of changes.
    /// Runs on the calling thread — no thread pool or background thread involvement.
    /// </summary>
    internal static class PollingEngine
    {
        /// <summary>
        /// Runs the polling loop synchronously on the calling thread.
        /// </summary>
        /// <typeparam name="T">The type returned by the function.</typeparam>
        /// <param name="function">The function to evaluate. May be null.</param>
        /// <param name="condition">The condition to evaluate on the function's return value. May be null (uses Convert.ToBoolean).</param>
        /// <param name="pollingPeriod">The polling interval.</param>
        /// <param name="cancellationToken">Token to cancel the polling loop.</param>
        /// <param name="callbacks">Optional callbacks for value/truth changes and poll completion.</param>
        /// <returns>The last <see cref="PollResult"/> before the loop ended.</returns>
        public static PollResult Run<T>(
            Func<T> function,
            Predicate<T> condition,
            TimeSpan pollingPeriod,
            CancellationToken cancellationToken,
            PollingCallbacks callbacks = null)
        {
            var effectivePollingPeriod = pollingPeriod > TimeSpan.Zero ? pollingPeriod : TimeSpan.FromMilliseconds(100);
            var stopwatch = new Stopwatch();

            bool first = true;
            T lastValue = default;
            bool lastTruth = default;
            var lastResult = new PollResult();

            while (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Restart();

                T value = default;
                bool truth = false;
                Exception exception = null;

                try
                {
                    // Evaluate function if provided
                    if (function != null)
                    {
                        value = function();
                        if (first || !object.Equals(value, lastValue))
                        {
                            callbacks?.OnValueChanged?.Invoke(value?.ToString() ?? "null");
                            lastValue = value;
                        }
                    }

                    // Evaluate condition
                    truth = condition == null ? Convert.ToBoolean(value) : condition(value);
                }
                catch (Exception ex)
                {
                    // Treat exception as false, store it, and notify
                    exception = ex;
                    truth = false;
                    callbacks?.OnValueChanged?.Invoke($"Exception: {ex.Message}");
                }

                // Notify truth change
                if (first || !truth.Equals(lastTruth))
                {
                    callbacks?.OnTruthChanged?.Invoke(truth);
                    lastTruth = truth;
                }

                lastResult = new PollResult { Truth = truth, Exception = exception };

                // Let consumer decide whether to stop
                if (callbacks?.OnPollComplete != null && callbacks.OnPollComplete(lastResult))
                {
                    break;
                }

                // Sleep for remaining polling period, cancellable
                var remaining = effectivePollingPeriod - stopwatch.Elapsed;
                Sleep(remaining, cancellationToken);

                first = false;
            }

            return lastResult;
        }

        /// <summary>
        /// Runs the polling loop synchronously on the calling thread, with an async condition.
        /// The async condition is awaited using GetAwaiter().GetResult() to keep execution on the calling thread.
        /// </summary>
        /// <typeparam name="T">The type returned by the function.</typeparam>
        /// <param name="function">The function to evaluate. May be null.</param>
        /// <param name="condition">The async condition to evaluate on the function's return value. May be null (uses Convert.ToBoolean).</param>
        /// <param name="pollingPeriod">The polling interval.</param>
        /// <param name="cancellationToken">Token to cancel the polling loop.</param>
        /// <param name="callbacks">Optional callbacks for value/truth changes and poll completion.</param>
        /// <returns>The last <see cref="PollResult"/> before the loop ended.</returns>
        public static PollResult RunWithAsyncCondition<T>(
            Func<T> function,
            Func<T, Task<bool>> condition,
            TimeSpan pollingPeriod,
            CancellationToken cancellationToken,
            PollingCallbacks callbacks = null)
        {
            var effectivePollingPeriod = pollingPeriod > TimeSpan.Zero ? pollingPeriod : TimeSpan.FromMilliseconds(100);
            var stopwatch = new Stopwatch();

            bool first = true;
            T lastValue = default;
            bool lastTruth = default;
            var lastResult = new PollResult();

            while (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Restart();

                T value = default;
                bool truth = false;
                Exception exception = null;

                try
                {
                    // Evaluate function if provided
                    if (function != null)
                    {
                        value = function();
                        if (first || !object.Equals(value, lastValue))
                        {
                            callbacks?.OnValueChanged?.Invoke(value?.ToString() ?? "null");
                            lastValue = value;
                        }
                    }

                    // Evaluate async condition synchronously to stay on calling thread
                    truth = condition == null
                        ? Convert.ToBoolean(value)
                        : condition(value).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // Treat exception as false, store it, and notify
                    exception = ex;
                    truth = false;
                    callbacks?.OnValueChanged?.Invoke($"Exception: {ex.Message}");
                }

                // Notify truth change
                if (first || !truth.Equals(lastTruth))
                {
                    callbacks?.OnTruthChanged?.Invoke(truth);
                    lastTruth = truth;
                }

                lastResult = new PollResult { Truth = truth, Exception = exception };

                // Let consumer decide whether to stop
                if (callbacks?.OnPollComplete != null && callbacks.OnPollComplete(lastResult))
                {
                    break;
                }

                // Sleep for remaining polling period, cancellable
                var remaining = effectivePollingPeriod - stopwatch.Elapsed;
                Sleep(remaining, cancellationToken);

                first = false;
            }

            return lastResult;
        }

        /// <summary>
        /// Cancellable sleep without thread pool involvement.
        /// </summary>
        /// <param name="duration">The duration to sleep.</param>
        /// <param name="cancellationToken">Token to cancel the sleep.</param>
        internal static void Sleep(TimeSpan duration, CancellationToken cancellationToken)
        {
            if (duration <= TimeSpan.Zero)
            {
                return;
            }

            cancellationToken.WaitHandle.WaitOne(duration);
        }
    }
}
