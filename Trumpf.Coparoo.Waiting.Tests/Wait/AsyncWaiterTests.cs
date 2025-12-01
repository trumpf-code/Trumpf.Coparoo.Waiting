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
    using System.Threading.Tasks;

    using NUnit.Framework;
    using AwesomeAssertions;
    using Trumpf.Coparoo.Waiting.Exceptions;
    using Trumpf.Coparoo.Waiting.Interfaces;
    using Trumpf.Coparoo.Waiting.WinForms;

    /// <summary>
    /// Async wait tests for GenericWaitForAsync method
    /// </summary>
    [TestFixture]
    public abstract class AsyncWaiterTestBase
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
        /// Positive case - async condition returns true
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionIsTrue_ThenNoExceptionIsThrown()
        {
            await waiter.GenericWaitForAsync(
                () => true,
                async (value) => { await Task.Delay(10); return value; },
                "Async condition is true",
                @long,
                GetPositiveTimeout(),
                @short,
                false,
                null);
        }

        /// <summary>
        /// Positive case - async condition eventually returns true
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionEventuallyBecomesTrue_ThenNoExceptionIsThrown()
        {
            int counter = 0;
            await waiter.GenericWaitForAsync(
                () => counter++,
                async (value) => { await Task.Delay(10); return value > 3; },
                "Counter reaches threshold",
                @long,
                GetPositiveTimeout(),
                @short,
                false,
                null);
        }

        /// <summary>
        /// Negative case - async condition returns false and times out
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionIsFalse_ThenTimeout()
        {
            try
            {
                await waiter.GenericWaitForAsync(
                    () => false,
                    async (value) => { await Task.Delay(10); return value; },
                    "Async condition is false",
                    @long,
                    GetPositiveTimeout(),
                    @short,
                    false,
                    null);
                
                Assert.Fail("Expected WaitForTimeoutException was not thrown");
            }
            catch (WaitForTimeoutException ex)
            {
                ex.Message.Should().Contain("Timeout of " + @long.TotalSeconds.ToString("0.00") + " seconds exceeded when waiting for 'Async condition is false'");
            }
        }

        /// <summary>
        /// Test async condition with complex async operation
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionWithComplexOperation_ThenNoExceptionIsThrown()
        {
            await waiter.GenericWaitForAsync(
                () => "test",
                async (value) => 
                { 
                    await Task.Delay(50);
                    var result = await Task.Run(() => value == "test");
                    return result;
                },
                "Complex async operation",
                @long,
                GetPositiveTimeout(),
                @short,
                false,
                null);
        }

        /// <summary>
        /// Test async condition with generic type
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionWithGenericType_ThenNoExceptionIsThrown()
        {
            await waiter.GenericWaitForAsync(
                () => 42,
                async (value) => 
                { 
                    await Task.Delay(10);
                    return value == 42;
                },
                "Integer value matches",
                @long,
                GetPositiveTimeout(),
                @short,
                false,
                null);
        }

        /// <summary>
        /// Test async condition that throws exception
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionThrowsException_ThenTimeout()
        {
            try
            {
                await waiter.GenericWaitForAsync(
                    () => "test",
                    async (value) => 
                    { 
                        await Task.Delay(10);
                        throw new InvalidOperationException("Test exception");
                    },
                    "Async condition throws",
                    @long,
                    GetPositiveTimeout(),
                    @short,
                    false,
                    null);
                
                Assert.Fail("Expected WaitForTimeoutException was not thrown");
            }
            catch (WaitForTimeoutException)
            {
                // Expected
            }
        }

        /// <summary>
        /// Test async condition with null function
        /// </summary>
        [Test]
        public virtual async Task IfTheFunctionIsNull_AndAsyncConditionIsFalse_ThenTimeout()
        {
            try
            {
                await waiter.GenericWaitForAsync<object>(
                    null,
                    async (value) => 
                    { 
                        await Task.Delay(10);
                        return false;
                    },
                    "Null function with async condition",
                    @long,
                    GetPositiveTimeout(),
                    @short,
                    false,
                    null);
                
                Assert.Fail("Expected WaitForTimeoutException was not thrown");
            }
            catch (WaitForTimeoutException)
            {
                // Expected
            }
        }

        /// <summary>
        /// Test async condition with fast polling
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionWithFastPolling_ThenNoExceptionIsThrown()
        {
            int counter = 0;
            await waiter.GenericWaitForAsync(
                () => counter++,
                async (value) => 
                { 
                    await Task.Delay(5);
                    return value > 2;
                },
                "Fast polling",
                @long,
                GetPositiveTimeout(),
                TimeSpan.FromMilliseconds(50),
                false,
                null);
        }

        /// <summary>
        /// Test async condition with slow polling
        /// </summary>
        [Test]
        public virtual async Task IfTheAsyncConditionWithSlowPolling_ThenNoExceptionIsThrown()
        {
            await waiter.GenericWaitForAsync(
                () => true,
                async (value) => 
                { 
                    await Task.Delay(10);
                    return value;
                },
                "Slow polling",
                @long,
                GetPositiveTimeout(),
                TimeSpan.FromMilliseconds(500),
                false,
                null);
        }

        /// <summary>
        /// Test that async condition is not blocked
        /// </summary>
        [Test]
        public virtual async Task AsyncConditionDoesNotDeadlock()
        {
            var task = waiter.GenericWaitForAsync(
                () => true,
                async (value) => 
                { 
                    // Simulate an async operation that needs to return to context
                    await Task.Delay(100).ConfigureAwait(false);
                    await Task.Yield();
                    return value;
                },
                "No deadlock test",
                @long,
                GetPositiveTimeout(),
                @short,
                false,
                null);

            // Should complete without deadlock
            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the positive timeout for the waiter type
        /// Override in derived classes if needed
        /// </summary>
        protected virtual TimeSpan GetPositiveTimeout()
        {
            return medium;
        }
    }

    /// <summary>
    /// Async tests for SilentWaiter
    /// </summary>
    [TestFixture]
    public class SilentWaiterAsyncTests : AsyncWaiterTestBase
    {
        protected override IWaiter CreateWaiter()
        {
            return new SilentWaiter();
        }

        protected override TimeSpan GetPositiveTimeout()
        {
            // SilentWaiter only supports TimeSpan.Zero or TimeSpan.MaxValue for positive timeout
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Test that SilentWaiter throws exception when action text is provided
        /// </summary>
        [Test]
        public async Task IfActionTextIsProvided_ThenInvalidOperationException()
        {
            try
            {
                await waiter.GenericWaitForAsync(
                    () => true,
                    async (value) => { await Task.Delay(10); return value; },
                    "Async condition with action text",
                    @long,
                    TimeSpan.Zero,
                    @short,
                    false,
                    "Do something");
                
                Assert.Fail("Expected InvalidOperationException was not thrown");
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.Should().Contain("SilentWaiter does not support action text as it requires human interaction.");
            }
        }

        /// <summary>
        /// Test that SilentWaiter throws exception when positive timeout requires interaction
        /// </summary>
        [Test]
        public async Task IfPositiveTimeoutRequiresInteraction_ThenInvalidOperationException()
        {
            try
            {
                await waiter.GenericWaitForAsync(
                    () => true,
                    async (value) => { await Task.Delay(10); return value; },
                    "Async condition with positive timeout",
                    @long,
                    medium,
                    @short,
                    false,
                    null);
                
                Assert.Fail("Expected InvalidOperationException was not thrown");
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.Should().Contain("SilentWaiter does not support positive timeout as it requires human interaction.");
            }
        }
    }

    /// <summary>
    /// Async tests for ConditionDialogWaiter
    /// Note: These tests show UI dialogs and should be run explicitly, not in automated test runs
    /// </summary>
    [TestFixture]
    [Explicit("These tests show UI dialogs and may hang in automated test environments")]
    public class ConditionDialogWaiterAsyncTests : AsyncWaiterTestBase
    {
        protected override IWaiter CreateWaiter()
        {
            return new ConditionDialogWaiter();
        }

        protected override TimeSpan GetPositiveTimeout()
        {
            // Use very short timeout to allow automatic closing
            return TimeSpan.FromMilliseconds(200);
        }

        /// <summary>
        /// Test async condition with click-through mode
        /// </summary>
        [Test]
        public async Task IfTheAsyncConditionIsTrue_WithClickThrough_ThenNoExceptionIsThrown()
        {
            await waiter.GenericWaitForAsync(
                () => true,
                async (value) => { await Task.Delay(10); return value; },
                "Async condition with click-through",
                @long,
                TimeSpan.FromMilliseconds(200), // Very short positive timeout for auto-close
                @short,
                true,
                null);
        }
    }
}
