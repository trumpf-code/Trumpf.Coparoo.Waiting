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
    using Trumpf.Coparoo.Waiting.Interfaces;

    /// <summary>
    /// Wait helper returning on timeout.
    /// </summary>
    public static class TryWait
    {
        /// <summary>
        /// Gets or sets the factory that creates waiter instances.
        /// Each wait operation calls this factory to get a fresh <see cref="IWaiter"/> instance.
        /// If not set, defaults to creating <see cref="SilentWaiter"/> instances.
        /// </summary>
        /// <remarks>
        /// This property is shared with <see cref="Wait.DefaultWaiterFactory"/> through <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
        /// Setting this property affects both <see cref="Wait"/> and <see cref="TryWait"/> classes.
        /// For centralized configuration, prefer using <see cref="WaiterConfiguration.DefaultWaiterFactory"/> directly.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Use visual dialogs for interactive testing (fresh instance per wait)
        /// TryWait.DefaultWaiterFactory = () => new ConditionDialogWaiter();
        /// 
        /// // Use silent waiter for CI/CD
        /// TryWait.DefaultWaiterFactory = () => new SilentWaiter();
        /// 
        /// // Or configure centrally (affects both Wait and TryWait)
        /// WaiterConfiguration.DefaultWaiterFactory = () => new ConditionDialogWaiter();
        /// </code>
        /// </example>
        public static Func<IWaiter> DefaultWaiterFactory
        {
            get => WaiterConfiguration.DefaultWaiterFactory;
            set => WaiterConfiguration.DefaultWaiterFactory = value;
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool For(Func<bool> function)
        {
            return For(function, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool For<T>(Func<T> function, Predicate<T> condition)
        {
            return For(function, condition, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool For(Func<bool> function, TimeSpan timeout)
        {
            return InternalWait(function, e => e, timeout);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool For<T>(Func<T> function, Predicate<T> condition, TimeSpan timeout)
        {
            return InternalWait(function, condition, timeout);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool For<T>(Func<T> function, Predicate<T> condition, TimeSpan timeout, TimeSpan retryPause)
        {
            return InternalWait(function, condition, timeout, retryPause);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        /// <remarks>
        /// Uses a default timeout of 20 seconds. For custom timeout, use <see cref="For(Func{bool}, string, TimeSpan)"/>.
        /// For full control over all parameters, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Try wait with expectation text using default timeout
        /// if (TryWait.For(() => element.IsVisible, "Element should be visible"))
        /// {
        ///     // Element became visible
        /// }
        /// </code>
        /// </example>
        public static bool For(Func<bool> function, string expectationText)
        {
            return For(function, expectationText, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="timeout">The maximum waiting time.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Try wait with expectation text
        /// if (TryWait.For(() => element.IsVisible, "Element should be visible", TimeSpan.FromSeconds(10)))
        /// {
        ///     // Element became visible
        /// }
        /// </code>
        /// </example>
        public static bool For(Func<bool> function, string expectationText, TimeSpan timeout)
        {
            return GenericWaitFor(
                function != null ? (Func<object>)(() => function() ? (object)true : null) : null,
                result => result != null && (bool)result,
                expectationText,
                timeout,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(100),
                false,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="negativeTimeout">The maximum time to wait for the condition to become true.</param>
        /// <param name="positiveTimeout">The time to wait after the condition becomes true before continuing.</param>
        /// <param name="pollingPeriod">The time between condition checks.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Try wait with full timeout control
        /// if (TryWait.For(
        ///     () => element.IsVisible, 
        ///     "Element should be visible", 
        ///     TimeSpan.FromSeconds(30),
        ///     TimeSpan.FromSeconds(2),
        ///     TimeSpan.FromMilliseconds(100)))
        /// {
        ///     // Element became visible and was stable
        /// }
        /// </code>
        /// </example>
        public static bool For(Func<bool> function, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod)
        {
            return GenericWaitFor(
                function != null ? (Func<object>)(() => function() ? (object)true : null) : null,
                result => result != null && (bool)result,
                expectationText,
                negativeTimeout,
                positiveTimeout,
                pollingPeriod,
                false,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        /// <remarks>
        /// Uses a default timeout of 20 seconds. For custom timeout, use <see cref="For{T}(Func{T}, Predicate{T}, string, TimeSpan)"/>.
        /// For full control over all parameters, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Try wait for specific condition using default timeout
        /// if (TryWait.For(
        ///     () => GetStatus(), 
        ///     status => status == "Ready", 
        ///     "Status should be Ready"))
        /// {
        ///     // Status is ready
        /// }
        /// </code>
        /// </example>
        public static bool For<T>(Func<T> function, Predicate<T> condition, string expectationText)
        {
            return For(function, condition, expectationText, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="timeout">The maximum waiting time.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Try wait for specific condition
        /// if (TryWait.For(
        ///     () => GetStatus(), 
        ///     status => status == "Ready", 
        ///     "Status should be Ready", 
        ///     TimeSpan.FromSeconds(10)))
        /// {
        ///     // Status is ready
        /// }
        /// </code>
        /// </example>
        public static bool For<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan timeout)
        {
            return GenericWaitFor(
                function,
                condition,
                expectationText,
                timeout,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(100),
                false,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="negativeTimeout">The maximum time to wait for the condition to become true.</param>
        /// <param name="positiveTimeout">The time to wait after the condition becomes true before continuing.</param>
        /// <param name="pollingPeriod">The time between condition checks.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Try wait with full timeout control
        /// if (TryWait.For(
        ///     () => GetStatus(), 
        ///     status => status == "Ready", 
        ///     "Status should be Ready", 
        ///     TimeSpan.FromSeconds(30),
        ///     TimeSpan.FromSeconds(2),
        ///     TimeSpan.FromMilliseconds(100)))
        /// {
        ///     // Status is ready and stable
        /// }
        /// </code>
        /// </example>
        public static bool For<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod)
        {
            return GenericWaitFor(
                function,
                condition,
                expectationText,
                negativeTimeout,
                positiveTimeout,
                pollingPeriod,
                false,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c> with full control over all parameters.
        /// Uses the centrally configured waiter from <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual dialogs.</param>
        /// <param name="negativeTimeout">The maximum time to wait for the condition to become true.</param>
        /// <param name="positiveTimeout">The time to wait after the condition becomes true before continuing.</param>
        /// <param name="pollingPeriod">The time between condition checks.</param>
        /// <param name="clickThrough">Whether to enable click-through mode for dialogs.</param>
        /// <param name="actionText">Optional action text for manual interaction scenarios.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        public static bool GenericWaitFor<T>(
            Func<T> function,
            Predicate<T> condition,
            string expectationText,
            TimeSpan negativeTimeout,
            TimeSpan positiveTimeout,
            TimeSpan pollingPeriod,
            bool clickThrough,
            string actionText)
        {
            try
            {
                WaiterConfiguration.CreateWaiter().GenericWaitFor(
                    function,
                    condition,
                    expectationText,
                    negativeTimeout,
                    positiveTimeout,
                    pollingPeriod,
                    clickThrough,
                    actionText);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits asynchronously until a function evaluates to <c>true</c> with full control over all parameters.
        /// Uses the centrally configured waiter from <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
        /// Returns <c>true</c> if the condition is met, <c>false</c> on timeout (does not throw exceptions).
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The async condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual dialogs.</param>
        /// <param name="negativeTimeout">The maximum time to wait for the condition to become true.</param>
        /// <param name="positiveTimeout">The time to wait after the condition becomes true before continuing.</param>
        /// <param name="pollingPeriod">The time between condition checks.</param>
        /// <param name="clickThrough">Whether to enable click-through mode for dialogs.</param>
        /// <param name="actionText">Optional action text for manual interaction scenarios.</param>
        /// <returns>True if the condition was met within the timeout, false otherwise.</returns>
        public static async System.Threading.Tasks.Task<bool> GenericWaitForAsync<T>(
            Func<Task<T>> function,
            Predicate<T> condition,
            string expectationText,
            TimeSpan negativeTimeout,
            TimeSpan positiveTimeout,
            TimeSpan pollingPeriod,
            bool clickThrough,
            string actionText)
        {
            try
            {
                await WaiterConfiguration.CreateWaiter().GenericWaitForAsync(
                    function,
                    condition,
                    expectationText,
                    negativeTimeout,
                    positiveTimeout,
                    pollingPeriod,
                    clickThrough,
                    actionText).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        /// <returns>If the condition turned true.</returns>
        private static bool InternalWait<T>(Func<T> function, Predicate<T> condition, TimeSpan? timeout = null, TimeSpan? retryPause = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(20);
            retryPause = retryPause ?? TimeSpan.FromMilliseconds(100);

            try
            {
                WaiterConfiguration.CreateWaiter().GenericWaitFor(
                    function,
                    condition,
                    string.Empty,
                    timeout.Value,
                    TimeSpan.Zero,
                    retryPause.Value,
                    false,
                    null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Wait until the specified property equals the specified value for defined time period or until the specified time limit is reached.
        /// </summary>
        /// <param name="function">Condition to evaluate in loop.</param>
        /// <param name="timeout">The time limit for the whole operation.</param>
        /// <param name="stableTime">The time the condition should evaluate true.</param>
        /// <returns>True if the specified property equals the specified value; else, false.</returns>
        public static bool UntilStableFor(Func<bool> function, TimeSpan timeout, TimeSpan stableTime)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (watch.Elapsed < timeout)
            {
                if (!function())
                {
                    Thread.Sleep(100);
                    continue;
                }

                TimeSpan elapsedBeforeSucceeded = watch.Elapsed;
                if (elapsedBeforeSucceeded + stableTime > timeout)
                {
                    return false;
                }

                //wait for cooldown period
                while (watch.Elapsed - elapsedBeforeSucceeded < stableTime)
                {
                    if (!function())
                    {
                        return UntilStableFor(function, timeout - watch.Elapsed, stableTime);
                    }

                    Thread.Sleep(100);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool UntilStable<T>(Func<T> function) where T : struct
        {
            return UntilStableInternal(function, null, null);
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool UntilStable<T>(Func<T> function, TimeSpan retryPause) where T : struct
        {
            return UntilStableInternal(function, null, retryPause);
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        /// <returns>If the condition turned true.</returns>
        public static bool UntilStable<T>(Func<T> function, TimeSpan timeout, TimeSpan retryPause) where T : struct
        {
            return UntilStableInternal(function, timeout, retryPause);
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        /// <returns>If the condition turned true.</returns>
        private static bool UntilStableInternal<T>(Func<T> function, TimeSpan? timeout, TimeSpan? retryPause) where T : struct
        {
            try
            {
                Wait.UntilStableInternal(function, timeout, retryPause);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}