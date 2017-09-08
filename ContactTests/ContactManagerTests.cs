/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Communications.Contacts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;
    using Standard.Tests;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class ContactManagerTests
    {
        private const string _MultiTypeRoot = @"*\MultiTypeTests";

        /// <summary>
        /// Create a folder with one contact of each type in a dedicated folder.
        /// </summary>
        private static void _InitializeMultiTypeFolder()
        {
            TestUtil.PurgeContactManager(_MultiTypeRoot);
            using (ContactManager cm = new ContactManager(_MultiTypeRoot))
            {
                foreach (ContactTypes type in new [] { ContactTypes.Organization, ContactTypes.Contact, ContactTypes.Group })
                {
                    using (Contact c = cm.CreateContact(type))
                    {
                        c.Names.Default = new Name(type.ToString());
                        c.CommitChanges();
                    }
                }
            }
        }

        private ContactManager _contactManager = null;
        private static Dictionary<string, string> _myContactIds;

        // Explicitly set the Me contact to a dummy.
        // Don't move it in case we fail resetting it.
        // DO NOT otherwise mess with the user's existing address book as part of these tests.
        [ClassInitialize]
        public static void BackupMeContact(TestContext tc)
        {
            _myContactIds = TestUtil.BackupAndPurgeMeRegistryKeys();
        }

        [ClassCleanup]
        public static void RestoreMeContact()
        {
            TestUtil.RestoreMeRegistryKeys(_myContactIds);
        }

        [TestInitialize]
        public void InitializeManager()
        {
            _contactManager = new ContactManager();
         }

        [TestCleanup]
        public void DisposeOfManager()
        {
            Utility.SafeDispose(ref _contactManager);
        }

        [TestMethod]
        public void GetContactOutsideContacts()
        {
            Contact contact = null;
            string path = null;
            string id = null;

            if (Environment.GetFolderPath(Environment.SpecialFolder.Personal).StartsWith(ContactUtil.GetContactsFolder()))
            {
                Assert.Inconclusive("This test assumes that the personal folder isn't a subfolder of Contacts.");
            }

            try
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Any.Contact");
                contact = new Contact();
                contact.Save(path);
                contact.Dispose();

                // reload the contact to get the fullest Id.
                contact = new Contact(path);
                id = contact.Id;

                Utility.SafeDispose(ref contact);
                Assert.IsFalse(_contactManager.TryGetContact(id, out contact));
                Assert.IsNull(contact);

                UTVerify.ExpectException<UnreachableContactException>(() => _contactManager.GetContact(id));
            }
            finally
            {
                Utility.SafeDispose(ref contact);
                Utility.SafeDeleteFile(path);
            }
        }

        [
            TestMethod,
            Description("Dispose a ContactManager")
        ]
        public void ReuseDisposedContactManager()
        {
            ContactManager manager = new ContactManager();
            manager.Dispose();
            // Disposing a second time should still work.
            manager.Dispose();

            // Throw the ObjectDisposedException.
            Contact contact;
            UTVerify.ExpectException<ObjectDisposedException>(() => contact = manager.MeContact);
        }

        [TestMethod]
        public void SimpleEnumeration()
        {
            _InitializeMultiTypeFolder();
            using (ContactManager cm = new ContactManager(_MultiTypeRoot))
            {
                // There's already one contact of ContactTypes.Contact.  Create a few more.
                for (int i = 0; i < 5; ++i)
                {
                    cm.CreateContact().CommitChanges();
                }
                int count = 0;
                // GetContactCollection with no parameters only returns 
                foreach (Contact contact in cm.GetContactCollection())
                {
                    Assert.IsNotNull(contact);
                    contact.Dispose();
                    ++count;
                }
                Assert.AreEqual(6, count);
            }
            TestUtil.PurgeContactManager(_MultiTypeRoot);
        }

        [TestMethod]
        public void SimpleTypeEnumeration()
        {
            _InitializeMultiTypeFolder();
            using (ContactManager cm = new ContactManager(_MultiTypeRoot))
            {
                foreach (Contact wontGetHit in cm.GetContactCollection(ContactTypes.None))
                {
                    Assert.Fail("ContactTypes.None shouldn't have enumerated any values.");
                }

                // For any of the single types it should find the right one and only one in the enumeration.
                foreach (ContactTypes type in new[] { ContactTypes.Organization, ContactTypes.Contact, ContactTypes.Group })
                {
                    using (IEnumerator<Contact> enumerator = cm.GetContactCollection(type).GetEnumerator())
                    {
                        Assert.IsTrue(enumerator.MoveNext());
                        using (Contact contact = enumerator.Current)
                        {
                            Assert.AreEqual(type.ToString(), contact.Names.Default.FormattedName);
                        }
                        // Shouldn't find a second value here.
                        Assert.IsFalse(enumerator.MoveNext());
                    }
                }
            }
            TestUtil.PurgeContactManager(_MultiTypeRoot);
        }

        [TestMethod]
        public void MultipleTypesEnumeration()
        {
            _InitializeMultiTypeFolder();

            ContactTypes[] singleTypes = new [] { ContactTypes.Organization, ContactTypes.Contact, ContactTypes.Group };
            ContactTypes[] combinationTypes = new []
            {
                ContactTypes.Organization | ContactTypes.Group,
                ContactTypes.Contact | ContactTypes.Organization, 
                ContactTypes.Group | ContactTypes.Contact,
                ContactTypes.All
            };

            using (ContactManager cm = new ContactManager(_MultiTypeRoot))
            {
                // Can pair up types and should get multiple results.
                foreach (ContactTypes types in combinationTypes)
                {
                    List<string> names = new List<string>();
                    foreach (Contact contact in cm.GetContactCollection(types))
                    {
                        names.Add(contact.Names.Default.FormattedName);
                    }

                    foreach (ContactTypes type in singleTypes)
                    {
                        if (Utility.IsFlagSet((int)types, (int)type))
                        {
                            Assert.IsTrue(names.Contains(type.ToString()));
                            names.Remove(type.ToString());
                        }
                        else
                        {
                            Assert.IsFalse(names.Contains(type.ToString()));
                        }
                    }
                    // Should have found and removed all the names.
                    Assert.AreEqual(0, names.Count);
                }
            }
            TestUtil.PurgeContactManager(_MultiTypeRoot);
        }

        [
            TestMethod,
            Description("Contacts created from a manager can be Saved despite not being backed initially by a file.")
        ]
        public void SaveCreatedContactTest()
        {
            Contact newContact = _contactManager.CreateContact();
            Assert.IsNull(newContact.Path);
            Assert.IsNotNull(newContact.Id);

            newContact.Names.Default = new Name("New Contact");
            newContact.CommitChanges();

            Assert.AreEqual(Path.GetDirectoryName(newContact.Path), _contactManager.RootDirectory);
            Assert.IsTrue(Path.GetFileNameWithoutExtension(newContact.Path).StartsWith("New Contact", StringComparison.InvariantCultureIgnoreCase));
            Assert.AreEqual(Path.GetExtension(newContact.Path), ".contact", true);

            string originalPath = newContact.Path;
            DateTime originalSaveTime = File.GetLastWriteTime(newContact.Path);

            newContact.PhoneNumbers.Default = new PhoneNumber("555-1212");
            newContact.CommitChanges();

            Assert.AreEqual(newContact.Path, originalPath);
            Assert.IsTrue(TimeSpan.Zero < File.GetLastWriteTime(newContact.Path) - originalSaveTime);

            // Saving the contact should rename it since it's associated with the manager.
            newContact.Names.Default = new Name("|nv@l:d*Ch/\\r@|<ters*");
            newContact.CommitChanges();
            Assert.AreEqual(Path.GetDirectoryName(newContact.Path), _contactManager.RootDirectory);
            Assert.IsTrue(Path.GetFileNameWithoutExtension(newContact.Path).StartsWith("_nv@l_d_Ch__r@__ters_", StringComparison.InvariantCultureIgnoreCase));
            Assert.AreEqual(Path.GetExtension(newContact.Path), ".contact", true);

            Assert.IsTrue(_contactManager.Remove(newContact.Id));
            Assert.IsFalse(File.Exists(newContact.Path));
        }

        [
            TestMethod,
            Description("Remove should make an attempt to find the contact to remove, if the contact isn't bound to a file.")
        ]
        public void RemoveContactNotLoadedFromFileTest()
        {
            using (Contact newContact = new Contact())
            {
                newContact.Names.Default = new Name("Unstored Contact");

                // The contact doesn't exist in the manager.
                Assert.IsFalse(_contactManager.Remove(newContact.Id));

                newContact.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Foo.contact"));
                Assert.IsFalse(_contactManager.Remove(newContact.Id));
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Foo.contact"));

                newContact.Save(Path.Combine(_contactManager.RootDirectory, "Foo.contact"));
                Assert.IsTrue(_contactManager.Remove(newContact.Id));
                Assert.IsFalse(File.Exists(Path.Combine(_contactManager.RootDirectory, "Foo.contact")));
            }
        }

        [
            TestMethod,
            Description("Remove should return whether the collection is modified as a result of the call.  Removing the same contact twice should result in the second call being a no-op")
        ]
        public void RemoveContactTwice()
        {
            using (Contact contact = _contactManager.CreateContact())
            {
                // Contact hasn't yet been committed, so it shouldn't affect the manager.
                Assert.IsFalse(_contactManager.Remove(contact.Id));
                contact.CommitChanges();
                Assert.IsTrue(_contactManager.Remove(contact.Id));
                Assert.IsFalse(_contactManager.Remove(contact.Id));
            }
        }

        [TestMethod]
        public void GetNonExistentContact()
        {
            using (Contact newContact = _contactManager.CreateContact())
            {
                newContact.Names.Default = new Name("Unsaved Contact Test");
                Contact loadedContact;

                Assert.IsFalse(_contactManager.TryGetContact(newContact.Id, out loadedContact));

                newContact.CommitChanges();
                Assert.IsTrue(_contactManager.TryGetContact(newContact.Id, out loadedContact));
                Assert.IsTrue(_contactManager.Remove(loadedContact.Id));
                loadedContact.Dispose();
                UTVerify.ExpectException<UnreachableContactException>(() => _contactManager.GetContact(newContact.Id));
            }
        }

        [TestMethod]
        public void GetContactTest()
        {
            string path = null;
            try
            {
                using (Contact newContact = _contactManager.CreateContact())
                {
                    newContact.Names.Default = new Name("ContactLoadTest");
                    newContact.CommitChanges();
                    path = newContact.Path;

                    using (Contact loadedContact = _contactManager.GetContact(newContact.Id))
                    {
                        Assert.AreEqual(loadedContact.ContactIds.Default, newContact.ContactIds.Default);
                    }
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void CreateManyContactsSameNameTest()
        {
            List<string> fileNames = new List<string>();

            try
            {
                for (int i = 0; i < 60; ++i)
                {
                    Contact contact = _contactManager.CreateContact();
                    contact.Names.Default = new Name("Duplicate");
                    contact.CommitChanges();
                    Assert.IsFalse(fileNames.Contains(contact.Path));
                    fileNames.Add(contact.Path);
                }
            }
            finally
            {
                foreach (string file in fileNames)
                {
                    File.Delete(file);
                }
            }
        }
    }
}
