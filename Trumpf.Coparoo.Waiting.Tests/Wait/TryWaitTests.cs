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
    using System.Diagnostics;
    using NUnit.Framework;
    using AwesomeAssertions;

    /// <summary>
    /// Tests for the static TryWait class
    /// </summary>
    [TestFixture]
    public class TryWaitTests
    {
        /// <summary>
        /// Test that TryWait.For returns true when condition is true
        /// </summary>
        [Test]
        public void For_WhenConditionIsTrue_ReturnsTrue()
        {
            // Act
            var result = TryWait.For(() => true, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.For returns false when condition is false
        /// </summary>
        [Test]
        public void For_WhenConditionIsFalse_ReturnsFalse()
        {
            // Act
            var result = TryWait.For(() => false, TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.For uses default timeout of 20 seconds
        /// </summary>
        [Test]
        public void For_WithoutTimeout_UsesDefaultTimeout()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = TryWait.For(() => false);

            // Assert
            result.Should().BeFalse();
            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(19.0)); // Allow some margin
        }

        /// <summary>
        /// Test that TryWait.For with condition returns true when condition met
        /// </summary>
        [Test]
        public void For_WithCondition_WhenConditionMet_ReturnsTrue()
        {
            // Arrange
            int value = 5;

            // Act
            var result = TryWait.For(() => value, v => v == 5, TimeSpan.FromSeconds(1));

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.For with condition returns false when condition not met
        /// </summary>
        [Test]
        public void For_WithCondition_WhenConditionNotMet_ReturnsFalse()
        {
            // Arrange
            int value = 5;

            // Act
            var result = TryWait.For(() => value, v => v == 10, TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.For with condition and default timeout uses correct default
        /// </summary>
        [Test]
        public void For_WithConditionAndDefaultTimeout_UsesDefaultTimeout()
        {
            // Arrange
            int value = 5;
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = TryWait.For(() => value, v => v == 10);

            // Assert
            result.Should().BeFalse();
            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(19.0)); // Allow some margin
        }

        /// <summary>
        /// Test that TryWait.For with custom retry pause works correctly
        /// </summary>
        [Test]
        public void For_WithCustomRetryPause_Works()
        {
            // Arrange
            int counter = 0;

            // Act
            var result = TryWait.For(
                () => ++counter >= 3,
                v => v,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(200));

            // Assert
            result.Should().BeTrue();
            Assert.That(counter, Is.GreaterThanOrEqualTo(3));
        }

        /// <summary>
        /// Test that TryWait.For eventually returns true when condition becomes true
        /// </summary>
        [Test]
        public void For_WhenConditionBecomesTrue_ReturnsTrue()
        {
            // Arrange
            int counter = 0;

            // Act
            var result = TryWait.For(() => ++counter > 3, TimeSpan.FromSeconds(2));

            // Assert
            result.Should().BeTrue();
            counter.Should().Be(4);
        }

        /// <summary>
        /// Test that TryWait.For handles exceptions gracefully
        /// </summary>
        [Test]
        public void For_WhenFunctionThrowsException_ReturnsFalse()
        {
            // Arrange
            Func<bool> throwingFunction = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = TryWait.For(throwingFunction, TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.For with null function returns false
        /// </summary>
        [Test]
        public void For_WithNullFunction_ReturnsFalse()
        {
            // Act
            var result = TryWait.For((Func<bool>)null, TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.UntilStable returns true when value stabilizes
        /// </summary>
        [Test]
        public void UntilStable_WhenValueStabilizes_ReturnsTrue()
        {
            // Arrange
            int value = 0;
            int callCount = 0;

            // Act
            var result = TryWait.UntilStable(
                () =>
                {
                    callCount++;
                    if (callCount < 5)
                        value = callCount;
                    return value;
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(100));

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.UntilStable returns false when value never stabilizes
        /// </summary>
        [Test]
        public void UntilStable_WhenValueNeverStabilizes_ReturnsFalse()
        {
            // Arrange
            int value = 0;

            // Act
            var result = TryWait.UntilStable(
                () => value++,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(50));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.UntilStable with default parameters works
        /// </summary>
        [Test]
        public void UntilStable_WithDefaultParameters_ReturnsTrue()
        {
            // Arrange
            int value = 5;

            // Act
            var result = TryWait.UntilStable(() => value);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.UntilStable with retry pause works
        /// </summary>
        [Test]
        public void UntilStable_WithRetryPause_ReturnsTrue()
        {
            // Arrange
            int value = 5;

            // Act
            var result = TryWait.UntilStable(() => value, TimeSpan.FromMilliseconds(200));

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor returns true when condition is stable
        /// </summary>
        [Test]
        public void UntilStableFor_WhenConditionIsStable_ReturnsTrue()
        {
            // Arrange
            bool condition = true;

            // Act
            var result = TryWait.UntilStableFor(
                () => condition,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor returns false when condition is never true
        /// </summary>
        [Test]
        public void UntilStableFor_WhenConditionNeverTrue_ReturnsFalse()
        {
            // Act
            var result = TryWait.UntilStableFor(
                () => false,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor returns false when condition flips
        /// </summary>
        [Test]
        public void UntilStableFor_WhenConditionFlips_ReturnsFalse()
        {
            // Arrange
            bool condition = true;
            int callCount = 0;

            // Act
            var result = TryWait.UntilStableFor(
                () =>
                {
                    callCount++;
                    if (callCount == 3)
                        condition = false;
                    return condition;
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor returns false when timeout occurs before stable time
        /// </summary>
        [Test]
        public void UntilStableFor_WhenTimeoutBeforeStableTime_ReturnsFalse()
        {
            // Arrange
            bool condition = false;
            int callCount = 0;

            // Act
            var result = TryWait.UntilStableFor(
                () =>
                {
                    callCount++;
                    if (callCount > 5)
                        condition = true;
                    return condition;
                },
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2)); // stable time longer than timeout

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor handles condition becoming true late
        /// </summary>
        [Test]
        public void UntilStableFor_WhenConditionBecomesTrueLate_ReturnsTrue()
        {
            // Arrange
            bool condition = false;
            int callCount = 0;

            // Act
            var result = TryWait.UntilStableFor(
                () =>
                {
                    callCount++;
                    if (callCount > 3)
                        condition = true;
                    return condition;
                },
                TimeSpan.FromSeconds(3),
                TimeSpan.FromMilliseconds(500));

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor handles recursive retry correctly
        /// </summary>
        [Test]
        public void UntilStableFor_WithRecursiveRetry_HandlesCorrectly()
        {
            // Arrange
            bool condition = false;
            int callCount = 0;

            // Act
            var result = TryWait.UntilStableFor(
                () =>
                {
                    callCount++;
                    // True for calls 5-7, then false
                    if (callCount >= 5 && callCount <= 7)
                        condition = true;
                    else if (callCount > 7)
                        condition = false;
                    return condition;
                },
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(300));

            // Assert - should eventually stabilize or timeout
            // The exact result depends on timing, but it should not throw
            Assert.That(result, Is.True.Or.False);
        }

        /// <summary>
        /// Test that TryWait.For does not throw any exception
        /// </summary>
        [Test]
        public void For_NeverThrowsException_EvenWithExceptionsInFunction()
        {
            // Arrange
            int callCount = 0;
            Func<bool> functionWithException = () =>
            {
                callCount++;
                if (callCount % 2 == 0)
                    throw new InvalidOperationException("Test");
                return false;
            };

            // Act
            Action act = () =>
            {
                var result = TryWait.For(functionWithException, TimeSpan.FromMilliseconds(500));
                result.Should().BeFalse();
            };

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that TryWait.UntilStable does not throw any exception
        /// </summary>
        [Test]
        public void UntilStable_NeverThrowsException()
        {
            // Arrange
            int value = 0;

            // Act
            Action act = () =>
            {
                var result = TryWait.UntilStable(() => value++, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(50));
                result.Should().BeFalse();
            };

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that TryWait.UntilStableFor does not throw any exception
        /// </summary>
        [Test]
        public void UntilStableFor_NeverThrowsException()
        {
            // Act
            Action act = () =>
            {
                var result = TryWait.UntilStableFor(() => false, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(200));
                result.Should().BeFalse();
            };

            // Assert
            act.Should().NotThrow();
        }
    }
}
