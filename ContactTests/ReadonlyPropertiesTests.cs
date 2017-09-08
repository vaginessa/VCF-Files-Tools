
namespace Microsoft.Communications.Contacts.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard.Tests;
    using System;

    [TestClass]
    public class ReadonlyPropertiesTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void UnsupportedOperations()
        {
            // Default contact.
            using (ReadonlyContactProperties roProp = new ReadonlyContactProperties(new WriteableContactProperties().SaveToStream()))
            {
                // For the sake of accessing the explicit method implementations.
                IContactProperties prop = roProp;
                UTVerify.ExpectException<NotSupportedException>(() => prop.AddLabels(null, null));
                UTVerify.ExpectException<NotSupportedException>(() => prop.ClearLabels(null));
                UTVerify.ExpectException<NotSupportedException>(() => prop.CreateArrayNode(null, false));
                UTVerify.ExpectException<NotSupportedException>(() => prop.DeleteArrayNode(null));
                UTVerify.ExpectException<NotSupportedException>(() => prop.DeleteProperty(null));
                UTVerify.ExpectException<NotSupportedException>(() => prop.RemoveLabel(null, null));
                UTVerify.ExpectException<NotSupportedException>(() => prop.SetBinary(null, null, null));
                UTVerify.ExpectException<NotSupportedException>(() => prop.SetDate(null, default(DateTime)));
                UTVerify.ExpectException<NotSupportedException>(() => prop.SetString(null, null));

                Assert.IsTrue(roProp.IsUnchanged);
            }
        }

        [TestMethod]
        public void GetBadAttributes()
        {
            // Default contact.
            using (ReadonlyContactProperties roProp = new ReadonlyContactProperties(new WriteableContactProperties().SaveToStream()))
            {
                UTVerify.ExpectException<PropertyNotFoundException>(() =>
                    roProp.GetAttributes(string.Format(PropertyNames.NameFormattedNameFormat, "1")));
                UTVerify.ExpectException<SchemaException>(() =>
                    roProp.GetAttributes(PropertyNames.ContactIdCollection));
            }
        }
    }
}
