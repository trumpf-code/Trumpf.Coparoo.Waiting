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

namespace Trumpf.Coparoo.Waiting.Extensions.ManualInteraction
{
    using System;

    using Interfaces;

    /// <summary>
    /// Extension methods for IWaiter manual interaction operations.
    /// </summary>
    public static class ManualInteractionWaitExtensions
    {
        private static readonly TimeSpan timerPeriod = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan positiveWaitTimeWithAction = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>. Shows an action text.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="actionText">The action to display.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        public static void WaitForUserAction<T>(this IWaiter waiter, string actionText, Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout)
        {
            waiter.WaitFor(function, condition, expectationText, negativeTimeout, positiveTimeout, timerPeriod, false, actionText);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>. Shows an action text.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="actionText">The action to display.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        public static void WaitForUserAction<T>(this IWaiter waiter, string actionText, Func<T> function, Predicate<T> condition, string expectationText)
        {
            waiter.WaitFor(function, condition, expectationText, TimeSpan.MaxValue, positiveWaitTimeWithAction, timerPeriod, false, actionText);
        }

        /// <summary>
        /// Shows the action and expectation text. Requires manual acknowledgment.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="actionText">The action to display.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        public static void WaitForUserAction(this IWaiter waiter, string actionText, string expectationText)
        {
            waiter.WaitFor<object>(null, null, expectationText, TimeSpan.MaxValue, TimeSpan.Zero, timerPeriod, false, actionText);
        }
    }
}
