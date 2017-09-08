namespace Standard.Tests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Interop;

    /// <summary>
    /// Tests for HRESULT and Win32Error utilities in Contacts.
    /// </summary>
    [TestClass]
    public class ErrorCodeTests
    {
        [TestMethod]
        public void SuccessHresultsTest()
        {
            HRESULT hr = HRESULT.S_OK;

            Assert.IsTrue(hr.Succeeded());
            Assert.IsFalse(hr.Failed());

            // ToString should properly enumerate the public, static fields on the class.
            Assert.AreEqual<string>(hr.ToString(), "S_OK");

            // S_FALSE (1) is the other well-known SUCCEEDED hresult
            hr = new HRESULT(1);

            // Even though it wasn't created from the static field, this still works.
            Assert.IsTrue(hr.Succeeded());
            Assert.AreEqual<string>(hr.ToString(), "S_FALSE");

            // random SUCCEEDED hresult, but not really valid...
            hr = new HRESULT(10);
            Assert.IsTrue(hr.Succeeded());
            // Since this is a random value, it shouldn't have a known string associated with it. 
            Assert.AreEqual(hr.ToString().Substring(0, 2), "0x");
            
            HRESULT hrTemp = new HRESULT(UInt32.Parse(hr.ToString().Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo));
            Assert.AreEqual(hr, hrTemp);
        }

        /// <summary>
        /// Some random small tests focused on FAILED HRESULTs.
        /// Most HRESULT functionality is tested in SuccessHresultTests.
        /// </summary>
        [TestMethod]
        public void FailedHresultsTest()
        {
            HRESULT hr = HRESULT.E_FAIL;

            Assert.IsTrue(hr != HRESULT.E_ACCESSDENIED);
            Assert.IsTrue(hr.Failed());
            Assert.IsFalse(hr.Succeeded());
            Assert.IsFalse(hr.Equals("E_FAIL"));

            // Win32Error codes are failed HRESULTs too!
            hr = (HRESULT)Win32Error.ERROR_TOO_MANY_OPEN_FILES;
            Assert.AreEqual(hr.ToString(), "HRESULT_FROM_WIN32(ERROR_TOO_MANY_OPEN_FILES)");

            // Some Win32Errors are also HRESULTs!
            Assert.AreEqual(HRESULT.E_OUTOFMEMORY, (HRESULT)Win32Error.ERROR_OUTOFMEMORY);
            // Just want to make sure that hashcodes of equal objects are also equal.
            Assert.AreEqual(HRESULT.E_OUTOFMEMORY.GetHashCode(), ((HRESULT)Win32Error.ERROR_OUTOFMEMORY).GetHashCode());

            Assert.AreNotEqual(HRESULT.E_OUTOFMEMORY.GetHashCode(), HRESULT.E_NOINTERFACE.GetHashCode());
        }

        [
            TestMethod,
            ExpectedException(typeof(ArgumentException))
        ]
        public void HresultToExceptionTest()
        {
            HRESULT hr = HRESULT.E_INVALIDARG;
            string message = "message";
            try
            {
                hr.ThrowIfFailed(message);
            }
            catch (ArgumentException e)
            {
                // Debug builds of Contact append the underlying hex value onto the message for easier debugging.
                Assert.IsTrue(e.Message.StartsWith(message));
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void HresultToExceptionNoMessageTest()
        {
            HRESULT hr = HRESULT.E_NOTIMPL;

            try
            {
                hr.ThrowIfFailed();
            }
            catch (NotImplementedException e)
            {
                Assert.AreEqual(hr.ToString(), e.Message);
                throw;
            }
        }

        [TestMethod]
        public void CompareWin32ErrorCodes()
        {
            // ERROR_OUTOFMEMORY == 14
            const int iError = 14;
            Win32Error error = Win32Error.ERROR_OUTOFMEMORY;
            // Win32Errors should be implicitly convertable to equivalent HRESULTs.
            Assert.AreEqual<HRESULT>(HRESULT.E_OUTOFMEMORY, error);

            Assert.IsTrue(error.Equals(new Win32Error(iError)));
            Assert.AreEqual(error.GetHashCode(), new Win32Error(iError).GetHashCode());

            // Try comparing to a non-Win32Error.  There's not a back-cast from HRESULTs.
            Assert.IsFalse(error.Equals(HRESULT.E_OUTOFMEMORY));
        }
    }
}
