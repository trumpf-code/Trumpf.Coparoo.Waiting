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
        private static Func<IWaiter> defaultWaiterFactory;

        /// <summary>
        /// Gets or sets the factory that creates waiter instances used by <see cref="Wait"/>, <see cref="TryWait"/>, and <see cref="Waiter"/>.
        /// Each call to <see cref="CreateWaiter"/> invokes this factory to produce a fresh <see cref="IWaiter"/> instance,
        /// ensuring that concurrent or nested waits do not share mutable state.
        /// If not set, defaults to creating <see cref="SilentWaiter"/> instances.
        /// </summary>
        /// <remarks>
        /// This is a shared configuration. Setting this property affects all waiting operations
        /// in <see cref="Wait"/>, <see cref="TryWait"/>, and <see cref="Waiter"/> classes.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Use visual dialogs for interactive testing (each wait gets a fresh instance)
        /// WaiterConfiguration.DefaultWaiterFactory = () => new ConditionDialogWaiter();
        /// 
        /// // Use silent waiter for CI/CD
        /// WaiterConfiguration.DefaultWaiterFactory = () => new SilentWaiter();
        /// 
        /// // Now both Wait and TryWait create fresh waiters per call
        /// Wait.For(() => condition);
        /// TryWait.For(() => condition);
        /// </code>
        /// </example>
        public static Func<IWaiter> DefaultWaiterFactory
        {
            get => defaultWaiterFactory ?? (defaultWaiterFactory = () => new SilentWaiter());
            set => defaultWaiterFactory = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Creates a new waiter instance using the configured <see cref="DefaultWaiterFactory"/>.
        /// Each call returns a fresh instance, making it safe for nested/reentrant wait scenarios.
        /// </summary>
        /// <returns>A new <see cref="IWaiter"/> instance.</returns>
        public static IWaiter CreateWaiter() => DefaultWaiterFactory();
    }
}
