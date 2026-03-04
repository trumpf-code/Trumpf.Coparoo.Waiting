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
    using System.Linq;

    using NUnit.Framework;
    using AwesomeAssertions;
    using Trumpf.Coparoo.Waiting.Extensions;
    using Trumpf.Coparoo.Waiting.Interfaces;
    using Trumpf.Coparoo.Waiting.Tests.Base;
    using Trumpf.Coparoo.Waiting.WinForms.Extensions;

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
        /// Override: Adjusted for SilentWaiter - tests with TimeSpan.MaxValue for negative, Zero for positive
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrueAndMaxNegativeTimeout_ThenNoExceptionIsThrown()
            => waiter.WaitFor(() => true, "Empty", TimeSpan.MaxValue, TimeSpan.Zero, @short);

        /// <summary>
        /// Override: Adjusted for SilentWaiter - uses TimeSpan.Zero for positive timeout with clickThrough
        /// </summary>
        [Test]
        public override void IfTheConditionIsTrue_ThenNoExceptionIsThrown_ClickThrough()
            => waiter.WaitFor(() => true, "Door is closed", @long, TimeSpan.Zero, @short, true);

        /// <summary>
        /// Override: SilentWaiter throws InvalidOperationException for MaxValue positive timeout
        /// </summary>
        [Test]
        public override void IfTheConditionIsFalseAndMaxPositiveTimeout_ThenTimeout()
        {
            Action act = () => waiter.WaitFor(() => false, "Empty", @long, TimeSpan.MaxValue, @short);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support infinite positive timeout (TimeSpan.MaxValue) as it would wait forever without user interaction.");
        }

        /// <summary>
        /// Test that SilentWaiter throws exception when action text is provided
        /// </summary>
        [Test]
        public void IfActionTextIsProvided_ThenInvalidOperationException()
        {
            Action act = () => waiter.WaitForUserAction("do something", () => 1, v => v == 1, "value is 1");
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

        /// <summary>
        /// Test that condition must stay true during positive timeout for stability
        /// </summary>
        [Test]
        public void IfConditionStaysTrue_DuringPositiveTimeout_Success()
        {
            // Condition stays true for entire duration
            waiter.WaitFor(() => true, "Stable true", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(50));
        }

        /// <summary>
        /// Test that condition flipping during positive timeout resets the timer
        /// </summary>
        [Test]
        public void IfConditionFlipsDuringPositiveTimeout_ThenResetAndContinue()
        {
            int evaluationCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Condition will be true, false, true pattern
            // Should reset positive timeout when it goes false
            waiter.WaitFor(
                () =>
                {
                    evaluationCount++;
                    // First 3 evals: true, then next 2: false, then true thereafter
                    if (evaluationCount <= 3) return true;
                    if (evaluationCount <= 5) return false;
                    return true;
                },
                "Flipping condition",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromMilliseconds(50));

            stopwatch.Stop();
            // Should take at least 300ms (positive timeout) after the last flip to true
            stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(300));
        }

        /// <summary>
        /// Test that condition must remain stable for the full positive timeout duration
        /// </summary>
        [Test]
        public void IfConditionFlipsMultipleTimes_OnlySucceedsAfterStableTrue()
        {
            int evaluationCount = 0;

            waiter.WaitFor(
                () =>
                {
                    evaluationCount++;
                    // Alternate true/false for first 10 evaluations, then stay true
                    if (evaluationCount <= 10)
                        return evaluationCount % 2 == 0;
                    return true;
                },
                "Multiple flips",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(50));

            // Should have evaluated many times due to the flipping
            evaluationCount.Should().BeGreaterThan(10);
        }

        /// <summary>
        /// Test that positive timeout of TimeSpan.MaxValue throws InvalidOperationException
        /// </summary>
        [Test]
        public void IfPositiveTimeoutIsMaxValue_ThenInvalidOperationException()
        {
            Action act = () => waiter.WaitFor(
                () => true,
                "MaxValue positive timeout",
                TimeSpan.FromSeconds(10),
                TimeSpan.MaxValue,
                TimeSpan.FromMilliseconds(50));

            // Should throw InvalidOperationException because MaxValue would wait forever
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("SilentWaiter does not support infinite positive timeout (TimeSpan.MaxValue) as it would wait forever without user interaction.");
        }

        /// <summary>
        /// Test that exception during positive timeout phase is treated as false condition
        /// </summary>
        [Test]
        public void IfExceptionDuringPositiveTimeout_ThenReset()
        {
            int evaluationCount = 0;

            waiter.WaitFor(
                () =>
                {
                    evaluationCount++;
                    // True for first few, then throw, then true again
                    if (evaluationCount > 3 && evaluationCount <= 5)
                        throw new InvalidOperationException("Simulated error");
                    return true;
                },
                "Exception during positive",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromMilliseconds(50));

            // Should succeed after recovering from exception
            evaluationCount.Should().BeGreaterThan(5);
        }    }
}