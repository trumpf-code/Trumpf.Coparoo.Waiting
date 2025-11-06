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

namespace Trumpf.Coparoo.Waiting.Tests.Wait
{
    using System;
    using System.Linq;

    using NUnit.Framework;
    using AwesomeAssertions;
    using Trumpf.Coparoo.Waiting.Extensions;
    using Trumpf.Coparoo.Waiting.Interfaces;
    using Trumpf.Coparoo.Waiting.Tests.Base;

    /// <summary>
    /// Silent wait tests using SilentWaiter
    /// </summary>
    [TestFixture]
    public class SilentWaiterTests : WaiterTestBase
    {
        /// <summary>
        /// Creates a SilentWaiter instance
        /// </summary>
        protected override IWaiter CreateWaiter()
        {
            return new SilentWaiter();
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrue_ThenNoExceptionIsThrown_ClickThrough()
        {
            Action act = () => waiter.WaitFor(() => true, "Door is closed", @long, @long, @short, true);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrue_ThenNoExceptionIsThrown()
        {
            Action act = () => waiter.WaitFor(() => true, "Empty", @long, medium, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void LongExpectationText_IsShownInTwoLines()
        {
            Action act = () => waiter.WaitFor(() => true, "We wait until the expected condition turns to true, so we can continue with the test.", @long, medium, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsFalse_ExceptionIsThrown_WithExceptionMessage()
        {
            Action act = () => waiter.WaitFor(() => false, "Order list is empty", @long, medium, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsNull_ThenTimeout()
        {
            Action act = () => waiter.WaitFor(null, "Empty", @long, medium, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsFalse_ThenTimeout()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", @long, medium, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsFalse_ThenTimeout_LazyCondition()
        {
            int i = 0;
            Action act = () => waiter.WaitFor(() => { System.Threading.Thread.Sleep(1000); return i++; }, j => j == 2, "Empty", @long, medium, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsFalse_ThenTimeout_ZeroPollingTime()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", @long, medium, TimeSpan.Zero);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrue_ThenNoExceptionIsThrown_TimeoutBeforeFirstPoll()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsFlipsBad_ThenTimeout()
        {
            bool b = false;
            Action act = () => waiter.WaitFor(() => b = !b, "Empty", @long, medium, TimeSpan.FromMilliseconds(500));
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsFlipsBad_ThenTimeout_ShowCurrentValue()
        {
            int i = 0;
            Action act = () => waiter.WaitFor(() => ++i, v => v % 2 == 0, "Even", medium, medium, @short, false, null);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: SilentWaiter does not support positive timeout (requires human interaction)
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrue_ThenNoExceptionIsThrown_NegativeTimes()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(-10));
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support positive timeout as it requires human interaction.");
        }

        /// <summary>
        /// Override: Adjusted for SilentWaiter - uses TimeSpan.Zero for positive timeout
        /// </summary>
        [Test]
        public override void Repeat_IfTheConditionIsTrue_ThenNoExceptionIsThrown()
            => Enumerable.Range(0, 50).ToList().ForEach(_ => waiter.WaitFor(() => true, "Empty", @long, none, @short));

        /// <summary>
        /// Override: Adjusted for SilentWaiter - uses TimeSpan.Zero for positive timeout
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrue_ThenNoExceptionIsThrown_FastContinue()
            => waiter.WaitFor(() => true, "Empty", @long, TimeSpan.FromSeconds(0), @short);

        /// <summary>
        /// Override: Adjusted for SilentWaiter - tests with TimeSpan.MaxValue and zero positive timeout
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrueAndMaxNegativeTimeout_ThenNoExceptionIsThrown()
            // no negative timeout should be displayed on the UI - use TimeSpan.Zero for positive timeout in SilentWaiter
            => waiter.WaitFor(() => true, "Empty", TimeSpan.MaxValue, TimeSpan.Zero, @short);

        /// <summary>
        /// Test that SilentWaiter throws exception when action text is provided
        /// </summary>
        [Test]
        public void IfActionTextIsProvided_ThenInvalidOperationException()
        {
            Action act = () => waiter.WaitForAction("do something", () => 1, v => v == 1, "value is 1");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support action text as it requires human interaction.");
        }

        /// <summary>
        /// Test specific to SilentWaiter - nested wait
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
                            new SilentWaiter().WaitFor(() => true, "sub wait", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(100));
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
                TimeSpan.FromSeconds(0),
                TimeSpan.FromMilliseconds(1000));
        }
    }
}
