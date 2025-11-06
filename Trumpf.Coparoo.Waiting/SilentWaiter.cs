// Copyright 2016 - 2023 TRUMPF Werkzeugmaschinen GmbH + Co. KG.
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
        public void Forr<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText)
        {
            // Throw exception if action text is provided (requires human interaction)
            if (!string.IsNullOrEmpty(actionText))
            {
                throw new InvalidOperationException("SilentWaiter does not support action text as it requires human interaction.");
            }

            // Throw exception if positive timeout is set to a value that requires user interaction
            // TimeSpan.Zero means no positive timeout (immediate continue)
            // TimeSpan.MaxValue means infinite positive timeout (not requiring interaction)
            if (positiveTimeout != TimeSpan.Zero && positiveTimeout != TimeSpan.MaxValue)
            {
                throw new InvalidOperationException("SilentWaiter does not support positive timeout as it requires human interaction.");
            }

            // Throw exception if both function and condition are null with infinite timeout (requires manual acknowledgment)
            if (function == null && condition == null && negativeTimeout == TimeSpan.MaxValue)
            {
                throw new InvalidOperationException("SilentWaiter does not support manual acknowledgment mode (null function and condition with infinite timeout).");
            }

            var stopwatch = Stopwatch.StartNew();
            var effectivePollingPeriod = pollingPeriod > TimeSpan.Zero ? pollingPeriod : TimeSpan.FromMilliseconds(100);
            var effectiveNegativeTimeout = negativeTimeout < TimeSpan.Zero ? TimeSpan.Zero : negativeTimeout;
            
            bool isInfiniteTimeout = effectiveNegativeTimeout == TimeSpan.MaxValue;

            // Main waiting loop
            while (isInfiniteTimeout || stopwatch.Elapsed < effectiveNegativeTimeout)
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

                    if (conditionMet)
                    {
                        // Condition is true, success
                        return;
                    }

                    // For infinite timeout with false condition after first evaluation, throw
                    if (isInfiniteTimeout)
                    {
                        throw new WaitForTimeoutException(expectationText, TimeSpan.Zero);
                    }

                    // Condition not met yet, sleep and retry if we have time left
                    if (stopwatch.Elapsed + effectivePollingPeriod < effectiveNegativeTimeout)
                    {
                        Thread.Sleep(effectivePollingPeriod);
                    }
                }
                catch (WaitForTimeoutException)
                {
                    // Re-throw timeout exceptions
                    throw;
                }
                catch (Exception)
                {
                    // If function throws, treat as condition not met
                    if (isInfiniteTimeout)
                    {
                        throw new WaitForTimeoutException(expectationText, TimeSpan.Zero);
                    }
                    
                    if (stopwatch.Elapsed + effectivePollingPeriod < effectiveNegativeTimeout)
                    {
                        Thread.Sleep(effectivePollingPeriod);
                    }
                }
            }

            // Timeout reached
            throw new WaitForTimeoutException(expectationText, effectiveNegativeTimeout);
        }
    }
}
