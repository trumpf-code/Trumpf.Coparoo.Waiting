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

namespace Trumpf.Coparoo.Waiting.Tests.Base
{
    using System;
    using System.Linq;

    using NUnit.Framework;
    using AwesomeAssertions;
    using Trumpf.Coparoo.Waiting.Exceptions;
    using Trumpf.Coparoo.Waiting.Extensions;
    using Trumpf.Coparoo.Waiting.Interfaces;

    /// <summary>
    /// Abstract base class for waiter tests
    /// </summary>
    public abstract class WaiterTestBase
    {
        protected readonly TimeSpan @long = TimeSpan.FromSeconds(2);
        protected readonly TimeSpan medium = TimeSpan.FromSeconds(1);
        protected readonly TimeSpan none = TimeSpan.FromSeconds(0);
        protected readonly TimeSpan @short = TimeSpan.FromMilliseconds(100);
        protected IWaiter waiter;

        /// <summary>
        /// Abstract method to create the waiter instance
        /// </summary>
        protected abstract IWaiter CreateWaiter();

        /// <summary>
        /// Setup method to initialize the waiter
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            waiter = CreateWaiter();
        }

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrue_ThenNoExceptionIsThrown_FastContinue()
            => waiter.WaitFor(() => true, "Empty", @long, TimeSpan.FromSeconds(0), @short);

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrue_ThenNoExceptionIsThrown_ClickThrough()
            => waiter.WaitFor(() => true, "Door is closed", @long, @long, @short, true);

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrue_ThenNoExceptionIsThrown()
            => waiter.WaitFor(() => true, "Empty", @long, medium, @short);

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public virtual void LongExpectationText_IsShownInTwoLines()
            => waiter.WaitFor(() => true, "We wait until the expected condition turns to true, so we can continue with the test.", @long, medium, @short);

        /// <summary>
        /// If the condition is false, exception is thrown with exception message.
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFalse_ExceptionIsThrown_WithExceptionMessage()
        {
            Action act = () => waiter.WaitFor(() => false, "Order list is empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>()
                .WithMessage("Timeout of " + @long.TotalSeconds.ToString("0.00") + " seconds exceeded when waiting for 'Order list is empty'");
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsNull_ThenTimeout()
        {
            Action act = () => waiter.WaitFor(null, "Empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFalse_ThenTimeout()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFalse_ThenTimeout_LazyCondition()
        {
            int i = 0;
            Action act = () => waiter.WaitFor(() => { System.Threading.Thread.Sleep(1000); return i++; }, j => j == 2, "Empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFalse_ThenTimeout_ZeroPollingTime()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", @long, medium, TimeSpan.Zero);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFalseAndMaxPositiveTimeout_ThenTimeout()
        {
            // no negative timeout should be displayed on the UI
            Action act = () => waiter.WaitFor(() => false, "Empty", @long, TimeSpan.MaxValue, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrueAndMaxNegativeTimeout_ThenNoExceptionIsThrown()
            // no negative timeout should be displayed on the UI
            => waiter.WaitFor(() => true, "Empty", TimeSpan.MaxValue, @long, @short);

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrue_ThenNoExceptionIsThrown_TimeoutBeforeFirstPoll()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrue_ThenNoExceptionIsThrown_ZeroTimes()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsTrue_ThenNoExceptionIsThrown_NegativeTimes()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(-10));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public virtual void Repeat_IfTheConditionIsTrue_ThenNoExceptionIsThrown()
            => Enumerable.Range(0, 50).ToList().ForEach(_ => waiter.WaitFor(() => true, "Empty", @long, none, @short));

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFlipsBad_ThenTimeout()
        {
            bool b = false;
            Action act = () => waiter.WaitFor(() => b = !b, "Empty", @long, medium, TimeSpan.FromMilliseconds(500));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public virtual void IfTheConditionIsFlipsBad_ThenTimeout_ShowCurrentValue()
        {
            int i = 0;
            Action act = () => waiter.WaitFor(() => ++i, v => v % 2 == 0, "Even", medium, medium, @short, false, null);
            act.Should().Throw<WaitForTimeoutException>();
        }
    }
}
