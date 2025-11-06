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
    using Trumpf.Coparoo.Waiting.Exceptions;
    using Trumpf.Coparoo.Waiting.Extensions;

    /// <summary>
    /// Dialog wait for tests
    /// </summary>
    [TestFixture]
    public class ConditionDialogWaiterTests
    {
        private readonly TimeSpan @long = TimeSpan.FromSeconds(2);
        private readonly TimeSpan medium = TimeSpan.FromSeconds(1);
        private readonly TimeSpan none = TimeSpan.FromSeconds(0);
        private readonly TimeSpan @short = TimeSpan.FromMilliseconds(100);
        private readonly ConditionDialogWaiter dialog = new ConditionDialogWaiter();

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_FastContinue()
            => dialog.WaitFor(() => true, "Empty", @long, TimeSpan.FromSeconds(0), @short);

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_ClickThrough()
            => dialog.WaitFor(() => true, "Door is closed", @long, @long, @short, true);

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown()
            => dialog.WaitFor(() => true, "Empty", @long, medium, @short);

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public void LongExpectationText_IsShownInTwoLines()
            => dialog.WaitFor(() => true, "We wait until the expected condition turns to true, so we can continue with the test.", @long, medium, @short);

        /// <summary>
        /// If the condition is false, exception is thrown with exception message.
        /// </summary>
        [Test]
        public void IfTheConditionIsFalse_ExceptionIsThrown_WithExceptionMessage()
        {
            Action act = () => dialog.WaitFor(() => false, "Order list is empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>()
                .WithMessage("Timeout of " + @long.TotalSeconds.ToString("0.00") + " seconds exceeded when waiting for 'Order list is empty'");
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsNull_ThenTimeout()
        {
            Action act = () => dialog.WaitFor(null, "Empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsFalse_ThenTimeout()
        {
            Action act = () => dialog.WaitFor(() => false, "Empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsFalse_ThenTimeout_LazyCondition()
        {
            int i = 0;
            Action act = () => dialog.WaitFor(() => { System.Threading.Thread.Sleep(1000); return i++; }, j => j == 2, "Empty", @long, medium, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsFalse_ThenTimeout_ZeroPollingTime()
        {
            Action act = () => dialog.WaitFor(() => false, "Empty", @long, medium, TimeSpan.Zero);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsFalseAndMaxPositiveTimeout_ThenTimeout()
        {
            // no negative timeout should be displayed on the UI
            Action act = () => dialog.WaitFor(() => false, "Empty", @long, TimeSpan.MaxValue, @short);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrueAndMaxNegativeTimeout_ThenNoExceptionIsThrown()
            // no negative timeout should be displayed on the UI
            => dialog.WaitFor(() => true, "Empty", TimeSpan.MaxValue, @long, @short);

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_TimeoutBeforeFirstPoll()
        {
            Action act = () => dialog.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_ZeroTimes()
        {
            Action act = () => dialog.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_NegativeTimes()
        {
            Action act = () => dialog.WaitFor(() => false, "Empty", TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(-10));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Positive case
        /// </summary>
        [Test]
        public void Repeat_IfTheConditionIsTrue_ThenNoExceptionIsThrown()
            => Enumerable.Range(0, 50).ToList().ForEach(_ => dialog.WaitFor(() => true, "Empty", @long, none, @short));

        /// <summary>
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsFlipsBad_ThenTimeout()
        {
            bool b = false;
            Action act = () => dialog.WaitFor(() => b = !b, "Empty", @long, medium, TimeSpan.FromMilliseconds(500));
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Nested wait
        /// </summary>
        [Test]
        public void IfTheNestedConditionsAreTrue_ThenNoExceptionIsThrown()
        {
            dialog.WaitFor(
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
        /// Negative case
        /// </summary>
        [Test]
        public void IfTheConditionIsFlipsBad_ThenTimeout_ShowCurrentValue()
        {
            int i = 0;
            Action act = () => dialog.WaitFor(() => ++i, v => v % 2 == 0, "Even", medium, medium, @short, false, null);
            act.Should().Throw<WaitForTimeoutException>();
        }

        /// <summary>
        /// Wait with action text.
        /// </summary>
        [Test]
        public void IfTheConditionIsTrue_ThenNoExceptionIsThrown_WithActionText()
        {
            int exp = 10;
            int i = 0;
            dialog.WaitForAction("don't do anything", () => i != exp ? i++ : i, value => value == exp, $"value is {exp}");
        }
    }
}
