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
    /// Tests for the static Wait class
    /// </summary>
    [TestFixture]
    public class WaitTests
    {
        /// <summary>
        /// Test that Wait.For succeeds when condition is true
        /// </summary>
        [Test]
        public void For_WhenConditionIsTrue_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Action act = () => Waiting.Wait.For(() => true, TimeSpan.FromSeconds(1));
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that Wait.For throws TimeoutException when condition is false
        /// </summary>
        [Test]
        public void For_WhenConditionIsFalse_ThrowsTimeoutException()
        {
            // Arrange & Act
            Action act = () => Waiting.Wait.For(() => false, TimeSpan.FromMilliseconds(500));

            // Assert
            act.Should().Throw<TimeoutException>();
        }

        /// <summary>
        /// Test that Wait.For uses default timeout of 20 seconds
        /// </summary>
        [Test]
        public void For_WithoutTimeout_UsesDefaultTimeout()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            try
            {
                Waiting.Wait.For(() => false);
            }
            catch (TimeoutException)
            {
                // Expected
            }

            // Assert
            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(19.0)); // Allow some margin
        }

        /// <summary>
        /// Test that Wait.For with condition evaluates correctly
        /// </summary>
        [Test]
        public void For_WithCondition_WhenConditionMet_DoesNotThrow()
        {
            // Arrange
            int value = 5;

            // Act & Assert
            Action act = () => Waiting.Wait.For(() => value, v => v == 5, TimeSpan.FromSeconds(1));
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that Wait.For with condition throws when condition not met
        /// </summary>
        [Test]
        public void For_WithCondition_WhenConditionNotMet_ThrowsTimeoutException()
        {
            // Arrange
            int value = 5;

            // Act
            Action act = () => Waiting.Wait.For(() => value, v => v == 10, TimeSpan.FromMilliseconds(500));

            // Assert
            act.Should().Throw<TimeoutException>();
        }

        /// <summary>
        /// Test that Wait.For with condition and default timeout uses correct default
        /// </summary>
        [Test]
        public void For_WithConditionAndDefaultTimeout_ThrowsOnTimeout()
        {
            // Arrange
            int value = 5;
            var stopwatch = Stopwatch.StartNew();

            // Act
            try
            {
                Waiting.Wait.For(() => value, v => v == 10);
            }
            catch (TimeoutException)
            {
                // Expected
            }

            // Assert
            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(19.0)); // Allow some margin
        }

        /// <summary>
        /// Test that Wait.For with custom retry pause works correctly
        /// </summary>
        [Test]
        public void For_WithCustomRetryPause_Works()
        {
            // Arrange
            int counter = 0;

            // Act
            Action act = () => Waiting.Wait.For(
                () => ++counter >= 3,
                v => v,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(200));

            // Assert
            act.Should().NotThrow();
            Assert.That(counter, Is.GreaterThanOrEqualTo(3));
        }

        /// <summary>
        /// Test that Wait.UntilStable succeeds when value stabilizes
        /// </summary>
        [Test]
        public void UntilStable_WhenValueStabilizes_Succeeds()
        {
            // Arrange
            int value = 0;
            int callCount = 0;

            // Act & Assert
            Action act = () => Waiting.Wait.UntilStable(
                () =>
                {
                    callCount++;
                    if (callCount < 5)
                        value = callCount;
                    return value;
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(100));

            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that Wait.UntilStable throws when value never stabilizes
        /// </summary>
        [Test]
        public void UntilStable_WhenValueNeverStabilizes_ThrowsTimeoutException()
        {
            // Arrange
            int value = 0;

            // Act
            Action act = () => Waiting.Wait.UntilStable(
                () => value++,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(50));

            // Assert
            act.Should().Throw<TimeoutException>();
        }

        /// <summary>
        /// Test that Wait.UntilStable with default parameters works
        /// </summary>
        [Test]
        public void UntilStable_WithDefaultParameters_Works()
        {
            // Arrange
            int value = 5;

            // Act & Assert
            Action act = () => Waiting.Wait.UntilStable(() => value);
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that Wait.UntilStable with retry pause works
        /// </summary>
        [Test]
        public void UntilStable_WithRetryPause_Works()
        {
            // Arrange
            int value = 5;

            // Act & Assert
            Action act = () => Waiting.Wait.UntilStable(() => value, TimeSpan.FromMilliseconds(200));
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that Wait.ActAndWaitForChange detects value change
        /// </summary>
        [Test]
        public void ActAndWaitForChange_WhenValueChanges_Succeeds()
        {
            // Arrange
            int value = 5;
            Action changeValue = () => value = 10;

            // Act & Assert
            Action act = () => Waiting.Wait.ActAndWaitForChange<int, Exception>(changeValue, () => value);
            act.Should().NotThrow();
        }

        /// <summary>
        /// Test that Wait.ActAndWaitForChange throws when value doesn't change
        /// </summary>
        [Test]
        public void ActAndWaitForChange_WhenValueDoesNotChange_ThrowsTimeoutException()
        {
            // Arrange
            int value = 5;
            Action doNothing = () => { };

            // Act
            Action act = () => Waiting.Wait.ActAndWaitForChange<int, Exception>(doNothing, () => value);

            // Assert
            act.Should().Throw<TimeoutException>();
        }

        /// <summary>
        /// Test that Wait.ActAndWaitForChange handles retry exceptions
        /// </summary>
        [Test]
        public void ActAndWaitForChange_WithRetryException_RetriesOnException()
        {
            // Arrange
            int value = 5;
            int callCount = 0;
            Action changeValue = () => value = 10;

            Func<int> functionWithException = () =>
            {
                callCount++;
                if (callCount < 5 && value != 5) // Only throw if value changed and count < 5
                    throw new InvalidOperationException("Retry me");
                return value;
            };

            // Act & Assert
            Action act = () => Waiting.Wait.ActAndWaitForChange<int, InvalidOperationException>(
                changeValue,
                functionWithException);

            act.Should().NotThrow();
            Assert.That(callCount, Is.GreaterThanOrEqualTo(1));
        }

        /// <summary>
        /// Test that Wait.For handles exceptions from function
        /// </summary>
        [Test]
        public void For_WhenFunctionThrowsException_ThrowsTimeoutException()
        {
            // Arrange
            Func<bool> throwingFunction = () => throw new InvalidOperationException("Test exception");

            // Act
            Action act = () => Waiting.Wait.For(throwingFunction, TimeSpan.FromMilliseconds(500));

            // Assert
            act.Should().Throw<TimeoutException>();
        }

        /// <summary>
        /// Test that Wait.For with null function throws
        /// </summary>
        [Test]
        public void For_WithNullFunction_ThrowsException()
        {
            // Act & Assert - Expect either TimeoutException or NullReferenceException
            Action act = () => Waiting.Wait.For((Func<bool>)null, TimeSpan.FromMilliseconds(500));
            act.Should().Throw<Exception>();
        }

        /// <summary>
        /// Test that Wait.For eventually succeeds when condition becomes true
        /// </summary>
        [Test]
        public void For_WhenConditionBecomesTrue_Succeeds()
        {
            // Arrange
            int counter = 0;

            // Act & Assert
            Action act = () => Waiting.Wait.For(() => ++counter > 3, TimeSpan.FromSeconds(2));
            act.Should().NotThrow();
            counter.Should().Be(4);
        }
    }
}
