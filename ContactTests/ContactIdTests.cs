/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard.Tests;

    [TestClass]
    public class ContactIdTests
    {
        [
            TestMethod,
            Description("Verify result when requesting a token that's not in the Id")
        ]
        public void FindMissingTokenTest()
        {
            Assert.AreEqual(string.Empty, ContactId.TokenizeContactId("/PATH:\"C:\\test.contact\"", ContactId.Token.Guid));
        }

        [
            TestMethod,
            Description("Verify result when requesting a token from a token with no value."),
        ]
        public void MissingValueTest()
        {
            UTVerify.ExpectException<FormatException>(() => ContactId.TokenizeContactId("/PATH:", ContactId.Token.Path));
        }

        [
            TestMethod,
            Description("Verify correct value returned for a valid contact id")
        ]
        public void FindTokenTest()
        {
            string guid = Guid.NewGuid().ToString();
            string path = "C:\\test.contact";
            string contactId = "/GUID:\"" + guid + "\" /PATH:\"" + path + "\"";

            Assert.AreEqual(ContactId.TokenizeContactId(contactId, ContactId.Token.Path), path);
            Assert.AreEqual(ContactId.TokenizeContactId(contactId, ContactId.Token.Guid), guid);
        }

        [
            TestMethod,
            Description("Verify result when token's value has unbalanced quotes."),
            ExpectedException(typeof(FormatException))
        ]
        public void UnbalancedParenthesesTest()
        {
            ContactId.TokenizeContactId("/GUID:\"", ContactId.Token.Guid);
        }

        [
            TestMethod,
            Description("Verify that tokens enclosed in values of other tokens are properly ignored")
        ]
        public void TokensWithinValuesTest()
        {
            Assert.AreEqual("Real", ContactId.TokenizeContactId("/GUID:\"/PATH:Fake\" /PATH:\"Real\"", ContactId.Token.Path));
        }
    }
}