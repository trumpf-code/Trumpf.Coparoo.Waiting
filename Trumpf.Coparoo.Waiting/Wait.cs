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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Trumpf.Coparoo.Waiting.Interfaces;

    /// <summary>
    /// Wait helper throwing on timeout.
    /// </summary>
    public static class Wait
    {
        /// <summary>
        /// Gets or sets the factory that creates waiter instances.
        /// Each wait operation calls this factory to get a fresh <see cref="IWaiter"/> instance.
        /// If not set, defaults to creating <see cref="SilentWaiter"/> instances.
        /// </summary>
        /// <remarks>
        /// This property is shared with <see cref="TryWait.DefaultWaiterFactory"/> through <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
        /// Setting this property affects both <see cref="Wait"/> and <see cref="TryWait"/> classes.
        /// For centralized configuration, prefer using <see cref="WaiterConfiguration.DefaultWaiterFactory"/> directly.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Use visual dialogs for interactive testing (fresh instance per wait)
        /// Wait.DefaultWaiterFactory = () => new ConditionDialogWaiter();
        /// 
        /// // Use silent waiter for CI/CD
        /// Wait.DefaultWaiterFactory = () => new SilentWaiter();
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
        public static void For(Func<bool> function)
        {
            For(function, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        public static void For<T>(Func<T> function, Predicate<T> condition)
        {
            For(function, condition, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        public static void For(Func<bool> function, TimeSpan timeout)
        {
            RetryUntilSuccessOrTimeout(function, e => e, timeout);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        public static void For<T>(Func<T> function, Predicate<T> condition, TimeSpan timeout)
        {
            RetryUntilSuccessOrTimeout(function, condition, timeout);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        public static void For<T>(Func<T> function, Predicate<T> condition, TimeSpan timeout, TimeSpan retryPause)
        {
            RetryUntilSuccessOrTimeout(function, condition, timeout, retryPause);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <remarks>
        /// Uses a default timeout of 20 seconds. For custom timeout, use <see cref="For(Func{bool}, string, TimeSpan)"/>.
        /// For full control over all parameters, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait with expectation text using default timeout
        /// Wait.For(() => element.IsVisible, "Element should be visible");
        /// </code>
        /// </example>
        public static void For(Func<bool> function, string expectationText)
        {
            For(function, expectationText, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="timeout">The maximum waiting time.</param>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait with expectation text for visual feedback
        /// Wait.For(() => element.IsVisible, "Element should be visible", TimeSpan.FromSeconds(10));
        /// </code>
        /// </example>
        public static void For(Func<bool> function, string expectationText, TimeSpan timeout)
        {
            WaiterConfiguration.CreateWaiter().GenericWaitFor(
                function,
                result => result,
                expectationText,
                timeout,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(100),
                true,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="negativeTimeout">The maximum time to wait for the condition to become true.</param>
        /// <param name="positiveTimeout">The time to wait after the condition becomes true before continuing.</param>
        /// <param name="pollingPeriod">The time between condition checks.</param>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait with full timeout control and expectation text
        /// Wait.For(
        ///     () => element.IsVisible, 
        ///     "Element should be visible", 
        ///     TimeSpan.FromSeconds(30),
        ///     TimeSpan.FromSeconds(2),
        ///     TimeSpan.FromMilliseconds(100));
        /// </code>
        /// </example>
        public static void For(Func<bool> function, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod)
        {
            WaiterConfiguration.CreateWaiter().GenericWaitFor(
                function,
                result => result,
                expectationText,
                negativeTimeout,
                positiveTimeout,
                pollingPeriod,
                true,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <remarks>
        /// Uses a default timeout of 20 seconds. For custom timeout, use <see cref="For{T}(Func{T}, Predicate{T}, string, TimeSpan)"/>.
        /// For full control over all parameters, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait for specific condition with expectation text using default timeout
        /// Wait.For(
        ///     () => GetStatus(), 
        ///     status => status == "Ready", 
        ///     "Status should be Ready");
        /// </code>
        /// </example>
        public static void For<T>(Func<T> function, Predicate<T> condition, string expectationText)
        {
            For(function, condition, expectationText, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="timeout">The maximum waiting time.</param>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait for specific condition with expectation text
        /// Wait.For(
        ///     () => GetStatus(), 
        ///     status => status == "Ready", 
        ///     "Status should be Ready", 
        ///     TimeSpan.FromSeconds(10));
        /// </code>
        /// </example>
        public static void For<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan timeout)
        {
            WaiterConfiguration.CreateWaiter().GenericWaitFor(
                function,
                condition,
                expectationText,
                timeout,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(100),
                true,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the function's return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation. Shown in visual feedback if the waiter supports it.</param>
        /// <param name="negativeTimeout">The maximum time to wait for the condition to become true.</param>
        /// <param name="positiveTimeout">The time to wait after the condition becomes true before continuing.</param>
        /// <param name="pollingPeriod">The time between condition checks.</param>
        /// <remarks>
        /// For full control over all parameters including clickThrough and actionText, use <see cref="GenericWaitFor{T}"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Wait for specific condition with full timeout control
        /// Wait.For(
        ///     () => GetStatus(), 
        ///     status => status == "Ready", 
        ///     "Status should be Ready", 
        ///     TimeSpan.FromSeconds(30),
        ///     TimeSpan.FromSeconds(2),
        ///     TimeSpan.FromMilliseconds(100));
        /// </code>
        /// </example>
        public static void For<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod)
        {
            WaiterConfiguration.CreateWaiter().GenericWaitFor(
                function,
                condition,
                expectationText,
                negativeTimeout,
                positiveTimeout,
                pollingPeriod,
                true,
                null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c> with full control over all parameters.
        /// Uses the centrally configured waiter from <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
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
        /// <example>
        /// <code>
        /// // Wait with custom expectation text for visual feedback
        /// Wait.GenericWaitFor(
        ///     () => element.IsVisible,
        ///     isVisible => isVisible,
        ///     "Waiting for element to become visible",
        ///     TimeSpan.FromSeconds(30),
        ///     TimeSpan.Zero,
        ///     TimeSpan.FromMilliseconds(100),
        ///     true,
        ///     null);
        /// 
        /// // Wait with action text for semi-automated testing
        /// Wait.GenericWaitFor(
        ///     () => loginSuccessful,
        ///     success => success,
        ///     "Login should succeed",
        ///     TimeSpan.FromMinutes(2),
        ///     TimeSpan.FromSeconds(2),
        ///     TimeSpan.FromMilliseconds(500),
        ///     true,
        ///     "Please enter credentials and click Login");
        /// </code>
        /// </example>
        public static void GenericWaitFor<T>(
            Func<T> function,
            Predicate<T> condition,
            string expectationText,
            TimeSpan negativeTimeout,
            TimeSpan positiveTimeout,
            TimeSpan pollingPeriod,
            bool clickThrough,
            string actionText)
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
        }

        /// <summary>
        /// Waits asynchronously until a function evaluates to <c>true</c> with full control over all parameters.
        /// Uses the centrally configured waiter from <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
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
        /// <example>
        /// <code>
        /// // Async wait with custom parameters
        /// await Wait.GenericWaitForAsync(
        ///     () => GetStatus(),
        ///     async status => await ValidateStatusAsync(status),
        ///     "Waiting for valid status",
        ///     TimeSpan.FromSeconds(60),
        ///     TimeSpan.Zero,
        ///     TimeSpan.FromMilliseconds(200),
        ///     true,
        ///     null);
        /// </code>
        /// </example>
        public static System.Threading.Tasks.Task GenericWaitForAsync<T>(
            Func<Task<T>> function,
            Predicate<T> condition,
            string expectationText,
            TimeSpan negativeTimeout,
            TimeSpan positiveTimeout,
            TimeSpan pollingPeriod,
            bool clickThrough,
            string actionText)
        {
            return WaiterConfiguration.CreateWaiter().GenericWaitForAsync(
                function,
                condition,
                expectationText,
                negativeTimeout,
                positiveTimeout,
                pollingPeriod,
                clickThrough,
                actionText);
        }

        /// <summary>
        /// Waits until a function's return value changes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <typeparam name="RetryException">The retry exception.</typeparam>
        /// <param name="a">Action to execute.</param>
        /// <param name="function">The function to test for stabilization.</param>
        public static void ActAndWaitForChange<T, RetryException>(Action a, Func<T> function)
            where RetryException : Exception
        {
            T before = function();
            a();

            For(() =>
            {
                try
                {
                    return Different(before, function());
                }
                catch (RetryException)
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        public static void UntilStable<T>(Func<T> function) where T : struct
        {
            UntilStableInternal(function, null, null);
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        public static void UntilStable<T>(Func<T> function, TimeSpan retryPause) where T : struct
        {
            UntilStableInternal(function, null, retryPause);
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        public static void UntilStable<T>(Func<T> function, TimeSpan timeout, TimeSpan retryPause) where T : struct
        {
            UntilStableInternal(function, timeout, retryPause);
        }

        /// <summary>
        /// Waits until a function stabilizes.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="function">The function to test for stabilization.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        internal static void UntilStableInternal<T>(Func<T> function, TimeSpan? timeout, TimeSpan? retryPause) where T : struct
        {
            T? last = null;
            Predicate<T> condition = arg =>
            {
                bool stable = last.HasValue && last.Value.Equals(arg);
                last = arg;
                return stable;
            };

            RetryUntilSuccessOrTimeout(function, condition, timeout, retryPause);
        }

        /// <summary>
        /// Determine if the values are different.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="before">The first value.</param>
        /// <param name="after">The second value.</param>
        /// <returns>Whether both values unequal.</returns>
        private static bool Different<T>(T before, T after)
        {
            return (before != null || after != null) && !(after != null ? after.Equals(before) : before.Equals(after));
        }

        /// <summary>
        /// Executes <paramref name="function"/> until its result is not <c>null</c>.
        /// </summary>
        /// <returns>The first result of <paramref name="function"/> which is not <c>null</c>.</returns>
        /// <param name="function">The function which is executed.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="timeout">The maximum waiting time in milliseconds.</param>
        /// <param name="retryPause">Timeout between the condition check rounds.</param>
        /// <typeparam name="T">The type of the variable.</typeparam>
        internal static T RetryUntilSuccessOrTimeout<T>(Func<T> function, Predicate<T> condition, TimeSpan? timeout = null, TimeSpan? retryPause = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(20);
            retryPause = retryPause ?? TimeSpan.FromMilliseconds(100);

            T result = default(T);
            List<T> results = new List<T>();
            
            try
            {
                WaiterConfiguration.CreateWaiter().GenericWaitFor(
                    function,
                    arg =>
                    {
                        result = arg;
                        results.Add(arg);
                        return condition(arg);
                    },
                    string.Empty,
                    timeout.Value,
                    TimeSpan.Zero,
                    retryPause.Value,
                    true,
                    null);
                
                return result;
            }
            catch (Exceptions.WaitForTimeoutException)
            {
                throw new TimeoutException(string.Format("Condition did not turn true within the maximum waiting time period of {0}s; polling results: {1}", timeout.Value.TotalSeconds, string.Join(", ", results.Select(e => e == null ? "null" : e.ToString()))));
            }
        }
    }
}