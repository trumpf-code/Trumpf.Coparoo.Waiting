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

namespace Trumpf.Coparoo.Waiting.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for waiting functionality.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Do not call methods on this interface directly unless implementing custom waiter behavior.</b>
    /// <para>
    /// For normal usage, prefer:
    /// <list type="bullet">
    /// <item><description><see cref="Wait.For(Func{bool})"/> or <see cref="TryWait.For(Func{bool})"/> - for simple waiting scenarios</description></item>
    /// <item><description><see cref="Waiter.GenericWaitFor"/> or <see cref="Waiter.GenericWaitForAsync"/> - for advanced parameters</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// These static methods use the centrally configured waiter from <see cref="WaiterConfiguration.DefaultWaiter"/>,
    /// allowing you to switch between <see cref="SilentWaiter"/> and visual waiters (like ConditionDialogWaiter)
    /// without changing your code.
    /// </para>
    /// </remarks>
    public interface IWaiter
    {
        /// <summary>
        /// Waits until a function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        /// <param name="actionText">The action text.</param>
        void GenericWaitFor<T>(Func<T> function, Predicate<T> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText);

        /// <summary>
        /// Waits until an async function evaluates to <c>true</c>.
        /// Shows a dialog.
        /// </summary>
        /// <param name="function">The function to evaluate.</param>
        /// <param name="condition">The async condition to evaluate on the functions return value.</param>
        /// <param name="expectationText">Text that explains the function's expectation.</param>
        /// <param name="negativeTimeout">The negative timeout.</param>
        /// <param name="positiveTimeout">The positive timeout.</param>
        /// <param name="pollingPeriod">The polling time.</param>
        /// <param name="clickThrough">Whether to enable click-through mode.</param>
        /// <param name="actionText">The action text.</param>
        Task GenericWaitForAsync<T>(Func<T> function, Func<T, Task<bool>> condition, string expectationText, TimeSpan negativeTimeout, TimeSpan positiveTimeout, TimeSpan pollingPeriod, bool clickThrough, string actionText);
    }
}
