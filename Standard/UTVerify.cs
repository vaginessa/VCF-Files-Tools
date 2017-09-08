/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Standard.Tests
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Interop;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// A UnitTest class to supplement Visual Studio's Assert facilities.
    /// </summary>
    /// <remarks>
    /// As this becomes more complete it may replace some uses of the VS Assert class.  Some
    /// aspects of the VS test framework, such as ExpectedExceptionAttribute, have deep limitations
    /// that this tries to address.  So this class can be used as a drop-in replacement for
    /// Assert it generally throws AssertFailedExceptions which are understood by the MSTest harness.
    /// </remarks>
    internal static class UTVerify
    {
        public delegate void ExceptionableAction();

        public static void ExpectException<TException>(ExceptionableAction action) where TException : Exception
        {
            ExpectException<TException>(action, true);
        }

        public static void ExpectException<TException>(ExceptionableAction action, bool supportSubclasses) where TException : Exception
        {
            // Throw the ArgumentException if action is null.  Don't want this to get caught in our try block.
            Verify.IsNotNull(action, "action");

            try
            {
                action();
            }
            catch (TException e)
            {
                // If the caller specified that they want exactly the TException type thrown then don't accept derived exceptions.
                if (!supportSubclasses && (e.GetType() != typeof(TException)))
                {
                    throw;
                }
                // Caught the expected exception type.
                // If code past the catch block gets executed then the action didn't throw.
                return;
            }
            throw new AssertFailedException(
                string.Format(CultureInfo.InvariantCulture, "Expected an exception of type {0}{1} to be thrown but the operation completed without raising one.",
                    typeof(TException),
                    supportSubclasses ? " (or a derived exception type)" : ""));
        }

        public static void ExpectComException(ExceptionableAction action, HRESULT expectedErrorCode)
        {
            // Throw the ArgumentException if action is null.  Don't want this to get caught in our try block.
            Verify.IsNotNull(action, "action");

            try
            {
                action();
            }
            catch (COMException e)
            {
                // Only catch this if it maps to the expected HRESULT.
                if (!expectedErrorCode.Equals(e))
                {
                    throw;
                }

                // Caught the expected exception type.
                // If code past the catch block gets executed then the action didn't throw.
                return;
            }
            throw new AssertFailedException(
                string.Format(CultureInfo.InvariantCulture, "Expected a COMException with error code {0} to be thrown but the operation completed without raising one.",
                    expectedErrorCode));
        }
    }
}
