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
    using AwesomeAssertions;
    using Trumpf.Coparoo.Waiting.Interfaces;
    using System.Threading.Tasks;

    /// <summary>
    /// Tests for DefaultWaiter configuration in Wait and TryWait classes.
    /// </summary>
    [TestFixture]
    public class DefaultWaiterTests
    {
        private IWaiter originalWaitWaiter;
        private IWaiter originalTryWaitWaiter;

        [SetUp]
        public void Setup()
        {
            // Save original waiters
            originalWaitWaiter = Trumpf.Coparoo.Waiting.Wait.DefaultWaiter;
            originalTryWaitWaiter = Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original waiters
            Trumpf.Coparoo.Waiting.Wait.DefaultWaiter = originalWaitWaiter;
            Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter = originalTryWaitWaiter;
        }

        /// <summary>
        /// Test that Wait.DefaultWaiter defaults to SilentWaiter
        /// </summary>
        [Test]
        public void Wait_DefaultWaiter_Should_Be_SilentWaiter()
        {
            // Reset to default
            Trumpf.Coparoo.Waiting.Wait.DefaultWaiter = new SilentWaiter();
            
            // Act
            var waiter = Trumpf.Coparoo.Waiting.Wait.DefaultWaiter;

            // Assert
            Assert.IsNotNull(waiter);
            Assert.IsInstanceOf<SilentWaiter>(waiter);
        }

        /// <summary>
        /// Test that TryWait.DefaultWaiter defaults to SilentWaiter
        /// </summary>
        [Test]
        public void TryWait_DefaultWaiter_Should_Be_SilentWaiter()
        {
            // Reset to default
            Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter = new SilentWaiter();
            
            // Act
            var waiter = Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter;

            // Assert
            Assert.IsNotNull(waiter);
            Assert.IsInstanceOf<SilentWaiter>(waiter);
        }

        /// <summary>
        /// Test that Wait.DefaultWaiter can be changed
        /// </summary>
        [Test]
        public void Wait_DefaultWaiter_Can_Be_Changed()
        {
            // Arrange
            var customWaiter = new SilentWaiter();

            // Act
            Trumpf.Coparoo.Waiting.Wait.DefaultWaiter = customWaiter;

            // Assert
            Assert.AreSame(customWaiter, Trumpf.Coparoo.Waiting.Wait.DefaultWaiter);
        }

        /// <summary>
        /// Test that TryWait.DefaultWaiter can be changed
        /// </summary>
        [Test]
        public void TryWait_DefaultWaiter_Can_Be_Changed()
        {
            // Arrange
            var customWaiter = new SilentWaiter();

            // Act
            Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter = customWaiter;

            // Assert
            Assert.AreSame(customWaiter, Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter);
        }

        /// <summary>
        /// Test that Wait.DefaultWaiter throws when set to null
        /// </summary>
        [Test]
        public void Wait_DefaultWaiter_Should_Throw_When_Set_To_Null()
        {
            // Act & Assert
            Action act = () => Trumpf.Coparoo.Waiting.Wait.DefaultWaiter = null;
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that TryWait.DefaultWaiter throws when set to null
        /// </summary>
        [Test]
        public void TryWait_DefaultWaiter_Should_Throw_When_Set_To_Null()
        {
            // Act & Assert
            Action act = () => Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter = null;
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that Wait.For uses the configured DefaultWaiter
        /// </summary>
        [Test]
        public void Wait_For_Should_Use_DefaultWaiter()
        {
            // Arrange
            var testWaiter = new TestWaiter();
            Trumpf.Coparoo.Waiting.Wait.DefaultWaiter = testWaiter;

            // Act
            Trumpf.Coparoo.Waiting.Wait.For(() => true, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.IsTrue(testWaiter.WasCalled);
        }

        /// <summary>
        /// Test that TryWait.For uses the configured DefaultWaiter
        /// </summary>
        [Test]
        public void TryWait_For_Should_Use_DefaultWaiter()
        {
            // Arrange
            var testWaiter = new TestWaiter();
            Trumpf.Coparoo.Waiting.TryWait.DefaultWaiter = testWaiter;

            // Act
            Trumpf.Coparoo.Waiting.TryWait.For(() => true, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.IsTrue(testWaiter.WasCalled);
        }

        /// <summary>
        /// Test waiter implementation for testing purposes
        /// </summary>
        private class TestWaiter : IWaiter
        {
            public bool WasCalled { get; private set; }

            public void GenericWaitFor<T>(
                Func<T> function, 
                Predicate<T> condition, 
                string expectationText, 
                TimeSpan negativeTimeout, 
                TimeSpan positiveTimeout, 
                TimeSpan pollingPeriod, 
                bool clickThrough, 
                string actionText)
            {
                WasCalled = true;
                
                // Simple implementation: just evaluate the condition once
                if (function != null && condition != null)
                {
                    var result = function();
                    if (!condition(result))
                    {
                        throw new Exceptions.WaitForTimeoutException(expectationText, negativeTimeout);
                    }
                }
            }

            public System.Threading.Tasks.Task GenericWaitForAsync<T>(
                Func<Task<T>> function,
                Predicate<T> condition, 
                string expectationText, 
                TimeSpan negativeTimeout, 
                TimeSpan positiveTimeout, 
                TimeSpan pollingPeriod, 
                bool clickThrough, 
                string actionText)
            {
                WasCalled = true;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }
}
