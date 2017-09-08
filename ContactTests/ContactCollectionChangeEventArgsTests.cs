
namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard.Tests;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class ContactCollectionChangeEventArgsTests
    {
        [TestMethod]
        public void CreateNoopChangeTest()
        {
            UTVerify.ExpectException<ArgumentException>(()=>
                new ContactCollectionChangedEventArgs(ContactCollectionChangeType.NoChange, "null"));
        }

        [TestMethod]
        public void BadMoveChangeTest()
        {
            UTVerify.ExpectException<ArgumentException>(()=>
                new ContactCollectionChangedEventArgs(ContactCollectionChangeType.Moved, "there's no matching string"));
        }

        [TestMethod]
        public void BadAddChangeTest()
        {
            UTVerify.ExpectException<ArgumentException>(()=>
                new ContactCollectionChangedEventArgs(ContactCollectionChangeType.Added, "what's the old one", "new contactId"));
        }

        [TestMethod]
        public void BadContactIdTest()
        {
            UTVerify.ExpectException<ArgumentException>(()=>
                new ContactCollectionChangedEventArgs(ContactCollectionChangeType.Removed, ""));
        }

        [TestMethod]
        public void AddChangeTest()
        {
            ContactCollectionChangedEventArgs e = new ContactCollectionChangedEventArgs(ContactCollectionChangeType.Added, "New");
            Assert.AreEqual(ContactCollectionChangeType.Added, e.Change);
            Assert.IsNull(e.OldId);
            Assert.AreEqual("New", e.NewId);
        }

        [TestMethod]
        public void RemoveChangeTest()
        {
            ContactCollectionChangedEventArgs e = new ContactCollectionChangedEventArgs(ContactCollectionChangeType.Removed, "Old");
            Assert.AreEqual(ContactCollectionChangeType.Removed, e.Change);
            Assert.IsNull(e.NewId);
            Assert.AreEqual("Old", e.OldId);
        }

        [TestMethod]
        public void UpdatedChangeTest()
        {
            ContactCollectionChangedEventArgs e = new ContactCollectionChangedEventArgs(ContactCollectionChangeType.Updated, "Old", "New");
            Assert.AreEqual(ContactCollectionChangeType.Updated, e.Change);
            Assert.AreEqual("Old", e.OldId);
            Assert.AreEqual("New", e.NewId);
        }
    }
}
