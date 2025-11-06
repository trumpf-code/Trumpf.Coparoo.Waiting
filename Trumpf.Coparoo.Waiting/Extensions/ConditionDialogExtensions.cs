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

namespace Trumpf.Coparoo.Waiting.Extensions
{
    using System;

    using Interfaces;

    /// <summary>
    /// Extension methods for IWaiter.
    /// </summary>
    public static class ConditionDialogExtensions
    {
        private static readonly TimeSpan timerPeriod = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan negativeWaitTime = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan positiveWaitTime = TimeSpan.FromSeconds(0);
        private static readonly TimeSpan positiveWaitTimeWithAction = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        public static void WaitFor(this IWaiter waiter, Func<bool> function, string expectationText)
        {
            waiter.WaitFor(function, expectationText, negativeWaitTime);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        public static void WaitFor<T>(this IWaiter waiter, Func<T> function, Predicate<T> condition, string expectationText)
        {
            waiter.WaitFor(function, condition, expectationText, negativeWaitTime);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="timeout">The timeout.</param>
        public static void WaitFor(this IWaiter waiter, Func<bool> function, string expectationText, TimeSpan timeout)
        {
            waiter.WaitFor(function, expectationText, timeout, positiveWaitTime, timerPeriod, false);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="timeout">The timeout.</param>
        public static void WaitFor<T>(this IWaiter waiter, Func<T> function, Predicate<T> condition, string expectationText, TimeSpan timeout)
        {
            waiter.WaitFor(function, condition, expectationText, timeout, positiveWaitTime, timerPeriod, false, null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        public static void WaitFor(this IWaiter waiter, Func<bool> function, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod)
        {
            waiter.WaitFor(function, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, false);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        public static void WaitFor<T>(this IWaiter waiter, Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod)
        {
            waiter.WaitFor(function, condition, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, false, null);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        public static void WaitFor<T>(this IWaiter waiter, Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout)
        {
            waiter.WaitFor(function, condition, expectationText, negativeTimeout, positiveTimeout, timerPeriod, false, null);
        }

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
        public static void WaitForAction<T>(this IWaiter waiter, string actionText, Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout)
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
        public static void WaitForAction<T>(this IWaiter waiter, string actionText, Func<T> function, Predicate<T> condition, string expectationText)
        {
            waiter.WaitFor(function, condition, expectationText, TimeSpan.MaxValue, positiveWaitTimeWithAction, timerPeriod, false, actionText);
        }

        /// <summary>
        /// Shows the action and expectation text. Requires manual acknowledgment.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="actionText">The action to display.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        public static void WaitForAction(this IWaiter waiter, string actionText, string expectationText)
        {
            waiter.WaitFor<object>(null, null, expectationText, TimeSpan.MaxValue, TimeSpan.Zero, timerPeriod, false, actionText);
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        public static void WaitFor(this IWaiter waiter, Func<bool> function, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough)
        {
            if (function == null)
            {
                waiter.WaitFor<object>(null, null, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, null);
            }
            else
            {
                waiter.WaitFor<object>(null, _ => function(), expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, null);
            }
        }

        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog including the current value.
        /// </summary>
        /// <param name="waiter">The waiter instance.</param>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        /// <param name="actionText">The action text.</param>
        public static void WaitFor<T>(this IWaiter waiter, Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText)
        {
            waiter.Forr(function, condition, expectationText, negativeTimeout, positiveTimeout, pollingPeriod, clickThrough, actionText);
        }
    }
}
