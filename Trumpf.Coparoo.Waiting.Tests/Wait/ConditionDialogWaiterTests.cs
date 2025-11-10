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

namespace Trumpf.Coparoo.Waiting.Tests.Wait
{
    using System;

    using NUnit.Framework;
    using Trumpf.Coparoo.Waiting.Extensions;
    using Trumpf.Coparoo.Waiting.Extensions.ManualInteraction;
    using Trumpf.Coparoo.Waiting.Interfaces;
    using Trumpf.Coparoo.Waiting.Tests.Base;

    /// <summary>
    /// Dialog wait for tests using ConditionDialogWaiter
    /// </summary>
    [TestFixture]
    public class ConditionDialogWaiterTests : WaiterTestBase
    {
        /// <summary>
        /// Creates a ConditionDialogWaiter instance
        /// </summary>
        protected override IWaiter CreateWaiter()
        {
            return new ConditionDialogWaiter();
        }

        /// <summary>
        /// Nested wait
        /// </summary>
        [Test]
        public void IfTheNestedConditionsAreTrue_ThenNoExceptionIsThrown()
        {
            waiter.WaitFor(
                () =>
                {
                    try
                    {
                        Action a = () =>
                        {
                            new ConditionDialogWaiter().WaitFor(() => true, "sub wait", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(100));
                        };
                        System.Threading.Thread.Sleep(250);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                },
                "main wait",
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(1000));
        }

        /// <summary>
        /// Wait with action text.
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_WithActionText()
        {
            int exp = 10;
            int i = 0;
            waiter.WaitForUserAction("don't do anything", () => i != exp ? i++ : i, value => value == exp, $"value is {exp}");
        }
    }
}
