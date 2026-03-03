using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.FormattableString;

namespace Trumpf.Coparoo.Waiting.Tests.Base
{
    public static class ConditionalDialogWaitHelper
    {
        /// <summary>
        /// Executes the function only one time and checks the result. Polls during function execution will be skipped.
        /// </summary>
        /// <param name="function">The function to be executed.</param>
        /// <param name="expectationText">The expectation text.</param>
        /// <param name="timeSpan">The time to wait for the function to return to true.</param>
        /// <returns>The result of the called function.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching exception turns dialog into red.")]
        public static bool WaitForFuncIsExecutedAndReturnsTrue(Func<bool> function, string expectationText, TimeSpan timeSpan)
        {
            bool isExecuting = false;
            bool result = false;

            Trumpf.Coparoo.Waiting.Wait.For(
                () =>
                {
                    if (!isExecuting && !result)
                    {
                        isExecuting = true;
                        try
                        {
                            result = function();
                        }
                        catch (Exception ex)
                        {
                            string message = Invariant($"{ex.GetType().FullName} occurred in {nameof(WaitForFuncIsExecutedAndReturnsTrue)}, Message = '{ex.Message}'.");
                            Trace.TraceError(message);
                        }

                        isExecuting = false;
                    }

                    return result;
                },
                expectationText,
                timeSpan);
            return result;
        }

        /// <summary>
        /// Executes the function only one time and checks the result. Polls during function execution will be skipped. Timeout is 20s.
        /// </summary>
        /// <param name="function">The function to be executed.</param>
        /// <param name="expectationText">The expectation text.</param>
        /// <returns>The result of the called function.</returns>
        public static bool WaitForFuncIsExecutedAndReturnsTrue(Func<bool> function, string expectationText)
        {
            return WaitForFuncIsExecutedAndReturnsTrue(function, expectationText, TimeSpan.FromSeconds(20));
        }

        public static bool ExecuteFuncAndActOnException(Func<bool> function, Action<Exception> actionOnException, string expectationText, TimeSpan timeSpan)
        {
            bool isExecuting = false;
            bool result = false;

            Trumpf.Coparoo.Waiting.Wait.For(
                () =>
                {
                    if (!isExecuting && !result)
                    {
                        isExecuting = true;
                        try
                        {
                            result = function();
                        }
                        catch (Exception ex)
                        {
                            string message = Invariant($"{ex.GetType().FullName} occurred in {nameof(ExecuteFuncAndActOnException)}, Message = '{ex.Message}'.");
                            Trace.TraceError(message);
                            actionOnException(ex);
                        }
                        finally
                        {
                            isExecuting = false;
                        }
                    }

                    return result;
                },
                expectationText,
                timeSpan);
            return result;
        }

        /// <summary>
        /// Executes the function only one time and checks the result. Polls during function execution will be skipped.
        /// </summary>
        /// <param name="function">The function to be executed.</param>
        /// <param name="timeSpan">The time to wait for the function to return to true.</param>
        /// <param name="expectationText">The expectation text.</param>
        /// <returns>The result of the called function.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching exception turns dialog into red.")]
        public static bool TryWaitForResult(Func<bool> function, TimeSpan timeSpan, string expectationText)
        {
            bool result = false;

            try
            {
                result = WaitForFuncIsExecutedAndReturnsTrue(function, expectationText, timeSpan);
            }
            catch (Exception ex)
            {
                // The exception is not relevant.
                // The result is the desired value.
                string message = Invariant($"{ex.GetType().FullName} occurred in {nameof(TryWaitForResult)}, Message = '{ex.Message}'.");
                Trace.TraceError(message);
            }

            return result;
        }
    }
}
