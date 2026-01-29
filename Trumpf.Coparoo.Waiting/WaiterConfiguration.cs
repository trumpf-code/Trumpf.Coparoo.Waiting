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
    using Trumpf.Coparoo.Waiting.Interfaces;

    /// <summary>
    /// Central configuration for waiter implementations.
    /// This configuration is shared by <see cref="Wait"/> and <see cref="TryWait"/> classes.
    /// </summary>
    public static class WaiterConfiguration
    {
        private static IWaiter defaultWaiter;

        /// <summary>
        /// Gets or sets the default waiter implementation used by <see cref="Wait"/>, <see cref="TryWait"/>, and <see cref="Waiter"/>.
        /// If not set, defaults to <see cref="SilentWaiter"/>.
        /// </summary>
        /// <remarks>
        /// This is a shared configuration. Setting this property affects all waiting operations
        /// in <see cref="Wait"/>, <see cref="TryWait"/>, and <see cref="Waiter"/> classes.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Use visual dialogs for interactive testing
        /// WaiterConfiguration.DefaultWaiter = new ConditionDialogWaiter();
        /// 
        /// // Use silent waiter for CI/CD
        /// WaiterConfiguration.DefaultWaiter = new SilentWaiter();
        /// 
        /// // Now both Wait and TryWait use the configured waiter
        /// Wait.For(() => condition);
        /// TryWait.For(() => condition);
        /// </code>
        /// </example>
        public static IWaiter DefaultWaiter
        {
            get => defaultWaiter ?? (defaultWaiter = new SilentWaiter());
            set => defaultWaiter = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
