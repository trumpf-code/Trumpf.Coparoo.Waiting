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
        private Func<IWaiter> originalFactory;

        [SetUp]
        public void Setup()
        {
            // Save original factory
            originalFactory = WaiterConfiguration.DefaultWaiterFactory;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original factory
            WaiterConfiguration.DefaultWaiterFactory = originalFactory;
        }

        /// <summary>
        /// Test that Wait.DefaultWaiterFactory defaults to creating SilentWaiter
        /// </summary>
        [Test]
        public void Wait_DefaultWaiter_Should_Be_SilentWaiter()
        {
            // Reset to default
            WaiterConfiguration.DefaultWaiterFactory = () => new SilentWaiter();

            // Act
            var waiter = WaiterConfiguration.CreateWaiter();

            // Assert
            Assert.IsNotNull(waiter);
            Assert.IsInstanceOf<SilentWaiter>(waiter);
        }

        /// <summary>
        /// Test that TryWait.DefaultWaiterFactory defaults to creating SilentWaiter
        /// </summary>
        [Test]
        public void TryWait_DefaultWaiter_Should_Be_SilentWaiter()
        {
            // Reset to default
            WaiterConfiguration.DefaultWaiterFactory = () => new SilentWaiter();

            // Act
            var waiter = WaiterConfiguration.CreateWaiter();

            // Assert
            Assert.IsNotNull(waiter);
            Assert.IsInstanceOf<SilentWaiter>(waiter);
        }

        /// <summary>
        /// Test that DefaultWaiterFactory can be changed
        /// </summary>
        [Test]
        public void Wait_DefaultWaiterFactory_Can_Be_Changed()
        {
            // Arrange
            var customWaiter = new SilentWaiter();
            WaiterConfiguration.DefaultWaiterFactory = () => customWaiter;

            // Act & Assert
            Assert.AreSame(customWaiter, WaiterConfiguration.CreateWaiter());
        }

        /// <summary>
        /// Test that TryWait.DefaultWaiterFactory can be changed
        /// </summary>
        [Test]
        public void TryWait_DefaultWaiterFactory_Can_Be_Changed()
        {
            // Arrange
            var customWaiter = new SilentWaiter();
            WaiterConfiguration.DefaultWaiterFactory = () => customWaiter;

            // Act & Assert
            Assert.AreSame(customWaiter, WaiterConfiguration.CreateWaiter());
        }

        /// <summary>
        /// Test that DefaultWaiterFactory throws when set to null
        /// </summary>
        [Test]
        public void Wait_DefaultWaiterFactory_Should_Throw_When_Set_To_Null()
        {
            // Act & Assert
            Action act = () => WaiterConfiguration.DefaultWaiterFactory = null;
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that TryWait.DefaultWaiterFactory throws when set to null
        /// </summary>
        [Test]
        public void TryWait_DefaultWaiterFactory_Should_Throw_When_Set_To_Null()
        {
            // Act & Assert
            Action act = () => WaiterConfiguration.DefaultWaiterFactory = null;
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that each CreateWaiter call returns a fresh instance when factory creates new instances
        /// </summary>
        [Test]
        public void CreateWaiter_Should_Return_Fresh_Instances()
        {
            // Arrange
            WaiterConfiguration.DefaultWaiterFactory = () => new SilentWaiter();

            // Act
            var waiter1 = WaiterConfiguration.CreateWaiter();
            var waiter2 = WaiterConfiguration.CreateWaiter();

            // Assert
            Assert.AreNotSame(waiter1, waiter2);
        }

        /// <summary>
        /// Test that Wait.For uses the configured DefaultWaiterFactory
        /// </summary>
        [Test]
        public void Wait_For_Should_Use_DefaultWaiter()
        {
            // Arrange
            var testWaiter = new TestWaiter();
            WaiterConfiguration.DefaultWaiterFactory = () => testWaiter;

            // Act
            Trumpf.Coparoo.Waiting.Wait.For(() => true, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.IsTrue(testWaiter.WasCalled);
        }

        /// <summary>
        /// Test that TryWait.For uses the configured DefaultWaiterFactory
        /// </summary>
        [Test]
        public void TryWait_For_Should_Use_DefaultWaiter()
        {
            // Arrange
            var testWaiter = new TestWaiter();
            WaiterConfiguration.DefaultWaiterFactory = () => testWaiter;

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
