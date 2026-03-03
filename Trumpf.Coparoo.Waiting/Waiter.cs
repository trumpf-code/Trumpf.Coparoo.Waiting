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
    using System.Threading.Tasks;

    /// <summary>
    /// Provides static access to waiter functionality with full control over all parameters.
    /// Uses the centrally configured waiter from <see cref="WaiterConfiguration.DefaultWaiterFactory"/>.
    /// </summary>
    /// <remarks>
    /// Use this class when you need access to advanced parameters like <c>expectationText</c>, 
    /// <c>positiveTimeout</c>, <c>clickThrough</c>, or <c>actionText</c> that are not exposed 
    /// by <see cref="Wait"/> or <see cref="TryWait"/> classes.
    /// </remarks>
    public static class Waiter
    {
        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
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
        /// Waiter.GenericWaitFor(
        ///     () => element.IsVisible,
        ///     isVisible => isVisible,
        ///     "Waiting for element to become visible",
        ///     TimeSpan.FromSeconds(30),
        ///     TimeSpan.Zero,
        ///     TimeSpan.FromMilliseconds(100),
        ///     false,
        ///     null);
        /// 
        /// // Wait with action text for semi-automated testing
        /// Waiter.GenericWaitFor(
        ///     () => loginSuccessful,
        ///     success => success,
        ///     "Login should succeed",
        ///     TimeSpan.FromMinutes(2),
        ///     TimeSpan.FromSeconds(2),
        ///     TimeSpan.FromMilliseconds(500),
        ///     false,
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
        /// Waits asynchronously until a function evaluates to <c>true</c>.
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
        /// await Waiter.GenericWaitForAsync(
        ///     () => GetStatus(),
        ///     async status => await ValidateStatusAsync(status),
        ///     "Waiting for valid status",
        ///     TimeSpan.FromSeconds(60),
        ///     TimeSpan.Zero,
        ///     TimeSpan.FromMilliseconds(200),
        ///     false,
        ///     null);
        /// </code>
        /// </example>
        public static Task GenericWaitForAsync<T>(
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
    }
}
