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
        private Exception lastException; // Store last exception from evaluation

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

            var negativeStopwatch = Stopwatch.StartNew();
            Stopwatch positiveStopwatch = null;
            var effectivePollingPeriod = pollingPeriod > TimeSpan.Zero ? pollingPeriod : TimeSpan.FromMilliseconds(100);
            var effectiveNegativeTimeout = negativeTimeout < TimeSpan.Zero ? TimeSpan.Zero : negativeTimeout;
            var effectivePositiveTimeout = positiveTimeout < TimeSpan.Zero ? TimeSpan.Zero : positiveTimeout;

            bool isInfiniteNegativeTimeout = effectiveNegativeTimeout == TimeSpan.MaxValue;
            bool wasInGoodState = false;
            lastException = null; // Reset exception tracking

            // Main waiting loop
            while (isInfiniteNegativeTimeout || negativeStopwatch.Elapsed < effectiveNegativeTimeout)
            {
                try
                {
                    T result = default(T);

                    // Evaluate function if provided
                    if (function != null)
                    {
                        result = function();
                    }

                    // Evaluate condition
                    bool conditionMet = condition == null ? Convert.ToBoolean(result) : condition(result);

                    // Clear exception on successful evaluation
                    lastException = null;

                    // Clear exception on successful evaluation
                    lastException = null;

                    if (conditionMet)
                    {
                        // Condition is true
                        if (!wasInGoodState)
                        {
                            // Transition from bad to good state - start positive timeout
                            positiveStopwatch = Stopwatch.StartNew();
                            wasInGoodState = true;
                        }

                        // Check if positive timeout is satisfied
                        if (effectivePositiveTimeout == TimeSpan.Zero ||
                            positiveStopwatch.Elapsed >= effectivePositiveTimeout)
                        {
                            // Positive timeout satisfied, success
                            return;
                        }

                        // Continue polling during positive timeout
                    }
                    else
                    {
                        // Condition is false
                        if (wasInGoodState)
                        {
                            // Transition from good to bad state - reset positive timeout
                            positiveStopwatch = null;
                            wasInGoodState = false;
                        }

                        // For infinite timeout with false condition after first evaluation, throw
                        if (isInfiniteNegativeTimeout)
                        {
                            throw new WaitForTimeoutException(expectationText, TimeSpan.Zero);
                        }
                    }

                    // Condition not met yet (or still in positive timeout), sleep and retry if we have time left
                    if (negativeStopwatch.Elapsed + effectivePollingPeriod < effectiveNegativeTimeout)
                    {
                        Thread.Sleep(effectivePollingPeriod);
                    }
                }
                catch (WaitForTimeoutException)
                {
                    // Re-throw timeout exceptions
                    throw;
                }
                catch (Exception ex)
                {
                    // Store exception and treat as condition not met (retry on next poll)
                    lastException = ex;
                    
                    if (wasInGoodState)
                    {
                        // Transition from good to bad state - reset positive timeout
                        positiveStopwatch = null;
                        wasInGoodState = false;
                    }

                    if (isInfiniteNegativeTimeout)
                    {
                        // For infinite timeout with exception, throw the exception immediately
                        throw;
                    }

                    if (negativeStopwatch.Elapsed + effectivePollingPeriod < effectiveNegativeTimeout)
                    {
                        Thread.Sleep(effectivePollingPeriod);
                    }
                }
            }

            // Timeout reached - throw stored exception if available, otherwise timeout exception
            if (lastException != null)
            {
                var ex = lastException;
                lastException = null;
                throw ex;
            }
            throw new WaitForTimeoutException(expectationText, effectiveNegativeTimeout);
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
        public async Task GenericWaitForAsync<T>(Func<Task<T>> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText)
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

            var negativeStopwatch = Stopwatch.StartNew();
            Stopwatch positiveStopwatch = null;
            var effectivePollingPeriod = pollingPeriod > TimeSpan.Zero ? pollingPeriod : TimeSpan.FromMilliseconds(100);
            var effectiveNegativeTimeout = negativeTimeout < TimeSpan.Zero ? TimeSpan.Zero : negativeTimeout;
            var effectivePositiveTimeout = positiveTimeout < TimeSpan.Zero ? TimeSpan.Zero : positiveTimeout;

            bool isInfiniteNegativeTimeout = effectiveNegativeTimeout == TimeSpan.MaxValue;
            bool wasInGoodState = false;
            lastException = null; // Reset exception tracking

            // Main waiting loop
            while (isInfiniteNegativeTimeout || negativeStopwatch.Elapsed < effectiveNegativeTimeout)
            {
                try
                {
                    T result = default(T);

                    // Evaluate function if provided
                    if (function != null)
                    {
                        result = await function().ConfigureAwait(false);
                    }

                    // Evaluate condition
                    bool conditionMet = condition == null ? Convert.ToBoolean(result) : condition(result);

                    // Clear exception on successful evaluation
                    lastException = null;

                    // Clear exception on successful evaluation
                    lastException = null;

                    if (conditionMet)
                    {
                        // Condition is true
                        if (!wasInGoodState)
                        {
                            // Transition from bad to good state - start positive timeout
                            positiveStopwatch = Stopwatch.StartNew();
                            wasInGoodState = true;
                        }

                        // Check if positive timeout is satisfied
                        if (effectivePositiveTimeout == TimeSpan.Zero ||
                            positiveStopwatch.Elapsed >= effectivePositiveTimeout)
                        {
                            // Positive timeout satisfied, success
                            return;
                        }

                        // Continue polling during positive timeout
                    }
                    else
                    {
                        // Condition is false
                        if (wasInGoodState)
                        {
                            // Transition from good to bad state - reset positive timeout
                            positiveStopwatch = null;
                            wasInGoodState = false;
                        }

                        // For infinite timeout with false condition after first evaluation, throw
                        if (isInfiniteNegativeTimeout)
                        {
                            throw new WaitForTimeoutException(expectationText, TimeSpan.Zero);
                        }
                    }

                    // Condition not met yet (or still in positive timeout), sleep and retry if we have time left
                    if (negativeStopwatch.Elapsed + effectivePollingPeriod < effectiveNegativeTimeout)
                    {
                        await Task.Delay(effectivePollingPeriod).ConfigureAwait(false);
                    }
                }
                catch (WaitForTimeoutException)
                {
                    // Re-throw timeout exceptions
                    throw;
                }
                catch (Exception ex)
                {
                    // Store exception and treat as condition not met (retry on next poll)
                    lastException = ex;
                    
                    if (wasInGoodState)
                    {
                        // Transition from good to bad state - reset positive timeout
                        positiveStopwatch = null;
                        wasInGoodState = false;
                    }

                    if (isInfiniteNegativeTimeout)
                    {
                        // For infinite timeout with exception, throw the exception immediately
                        throw;
                    }

                    if (negativeStopwatch.Elapsed + effectivePollingPeriod < effectiveNegativeTimeout)
                    {
                        await Task.Delay(effectivePollingPeriod).ConfigureAwait(false);
                    }
                }
            }

            // Timeout reached - throw stored exception if available, otherwise timeout exception
            if (lastException != null)
            {
                var ex = lastException;
                lastException = null;
                throw ex;
            }
            throw new WaitForTimeoutException(expectationText, effectiveNegativeTimeout);
        }
    }
}
