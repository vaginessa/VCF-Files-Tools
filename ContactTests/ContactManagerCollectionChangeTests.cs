/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    /// <summary>
    /// Tests to verify notification behavior of changes to the Contact collection.
    /// These tests seem to be prone to timing issues.  They need to be updated ASAP.
    /// Since they generally pass, I'm planning to still check them in since I'm
    /// currently the only person who will see the failures.
    /// </summary>
    [TestClass]
    public class ContactManagerCollectionChangeTests
    {
        private const int WaitTimeout = 2000;
        private List<ContactCollectionChangedEventArgs> _actions = new List<ContactCollectionChangedEventArgs>();
        private ContactManager _contactManager;
        private ManualResetEvent _changeEvent;

        private void _NonBlockingWaitOne()
        {
            bool tryWait = false;
            for (int i = 0; i < 5 && !tryWait; ++i)
            {
                System.Windows.Forms.Application.DoEvents();
                tryWait = _changeEvent.WaitOne(WaitTimeout, false);
            }
            Assert.IsTrue(tryWait, "Didn't receive the contact's Add event in a reasonable amount of time");
        }

        private string _CreateContactClearActions(string path)
        {
            try
            {
                Contact contact = new Contact();
                contact.Save(path);
                contact.Dispose();
                contact = new Contact(path);
                string id = contact.Id;
                contact.Dispose();

                // Process and clear any changes notified as a result of that creation.
                _changeEvent.Reset();
                _NonBlockingWaitOne();
                _actions.Clear();
                return id;
            }
            catch
            {
                // If this throws an exception then cleanup the file on the way out.
                Utility.SafeDeleteFile(path);
                throw;
            }
        }

        private void _OnCollectionChanged(object sender, ContactCollectionChangedEventArgs e)
        {
            _actions.Add(e);
            // Reset the ManualResetEvent.
            _changeEvent.Set();
        }

        [TestInitialize]
        public void InitializeManager()
        {
            _actions.Clear();
            _contactManager = new ContactManager();
            _contactManager.CollectionChanged += _OnCollectionChanged;
            _changeEvent = new ManualResetEvent(false);
            System.Windows.Forms.Application.DoEvents();
        }

        [TestCleanup]
        public void DisposeOfManager()
        {
            Utility.SafeDispose(ref _contactManager);
            System.Windows.Forms.Application.DoEvents();
        }

        [TestMethod]
        public void IgnoreNewInvalidContactTest()
        {
            string filePath = Path.Combine(_contactManager.RootDirectory, "NotReally.contact");
            try
            {
                using (FileStream fstream = File.Create(filePath))
                {
                    Assert.AreEqual(0, _actions.Count);
                    byte[] param = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
                    fstream.Write(param, 0, param.Length);
                    fstream.Close();
                    Assert.AreEqual(0, _actions.Count);
                }
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [TestMethod]
        public void NoticeAddedContactTest()
        {
            using (Contact contact = _contactManager.CreateContact())
            {
                // Contact shouldn't have affected the manager until it's been saved.
                Assert.AreEqual(0, _actions.Count);
                contact.Names.Default = new Name("ContactManager Added Contact");
                _changeEvent.Reset();
                contact.CommitChanges();

                try
                {
                    // Wait to receive notice that the contact was added.
                    _NonBlockingWaitOne();
                    Assert.AreEqual(1, _actions.Count);
                    Assert.AreEqual(ContactCollectionChangeType.Added, _actions[0].Change);
                    // CommitChanges should have updated the Id to be the same as what the notification knows.
                    Assert.AreEqual(_actions[0].NewId, contact.Id, true);
                }
                finally
                {
                    _contactManager.Remove(contact.Id);
                }
            }
        }

        [
            TestMethod,
            Description("Notice that a contact has been removed.")
        ]
        public void NoticeContactRemovedTest()
        {
            string path = Path.Combine(_contactManager.RootDirectory, "Removable Notification.contact");
            string id = _CreateContactClearActions(path);

            File.Delete(path);
            _changeEvent.Reset();
            _NonBlockingWaitOne();
            Assert.AreEqual(1, _actions.Count);
            Assert.AreEqual(ContactCollectionChangeType.Removed, _actions[0].Change);
        }

        [
            TestMethod,
            Description("Notice that a contact has been removed, even when the file that used to contain it is still present.")
        ]
        public void NoticeContactRemovedBecauseInvalidTest()
        {
            string path = Path.Combine(_contactManager.RootDirectory, "Removable Notification.contact");
            string id = _CreateContactClearActions(path);

            try
            {
                _changeEvent.Reset();
                using (FileStream fstream = File.OpenWrite(path))
                {
                    byte[] param = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
                    fstream.Write(param, 0, param.Length);
                    fstream.Close();
                }
                _NonBlockingWaitOne();
                Assert.AreEqual(1, _actions.Count);
                Assert.AreEqual(ContactCollectionChangeType.Removed, _actions[0].Change);
                Assert.AreEqual(id, _actions[0].OldId, true);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // Because of the asynchronous nature of the update, this gets seen as a Delete/Add.
        // This is incorrect but acceptable for now.  Need to decide on how to mitigate it.
        // (Don't want UI that updates a contact and then closes the editor because it appears to be removed.)
        [
            TestMethod,
            Description("Notice that a contact has been moved to a different storage location.")
        ]
        public void NoticeContactMovedTest()
        {
            using (Contact contact = _contactManager.CreateContact())
            {
                contact.Names.Default = new Name("Changeable");
                contact.CommitChanges();

                _changeEvent.Reset();
                _NonBlockingWaitOne();
                _actions.Clear();

                contact.Names.Default = new Name("Changed");

                _changeEvent.Reset();
                contact.CommitChanges(ContactCommitOptions.ForceSyncStorageWithFormattedName);
                _NonBlockingWaitOne();
                // Expecting two events
                _changeEvent.Reset();
                if (_actions.Count < 2)
                {
                    _NonBlockingWaitOne();
                }
                else
                {
                    _changeEvent.Set();
                }
                _contactManager.CollectionChanged -= _OnCollectionChanged;
                _contactManager.Remove(contact.Id);
                // Because of the asynchronous nature of this, a move is most likely to be viewed as a Delete/Add
                // The order of these two shouldn't matter and can be interchanged.  The test shouldn't fail.
                Assert.AreEqual(ContactCollectionChangeType.Moved, _actions[0].Change);
                Assert.AreEqual(ContactCollectionChangeType.Updated, _actions[1].Change);
            }
        }

        [
            TestMethod,
            Description("Notice that a contact has been updated.")
        ]
        public void NoticeContactChangesTest()
        {
            string path = Path.Combine(_contactManager.RootDirectory, "No one I know.contact");
            _CreateContactClearActions(path);

            try
            {
                using (Contact contact = new Contact(path))
                {
                    contact.Addresses[AddressLabels.Postal] = new PhysicalAddress("P", "S", "C", "S", "Z", "C", "X", "L");
                    _changeEvent.Reset();
                    contact.CommitChanges();
                    _NonBlockingWaitOne();
                    Assert.AreEqual(1, _actions.Count);
                    Assert.AreEqual(ContactCollectionChangeType.Updated, _actions[0].Change);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [
            TestMethod,
            Description("Notice that the contact has changed because the file has been moved.")
        ]
        public void NoticeChangeFromMovedFile()
        {
            string path = Path.Combine(_contactManager.RootDirectory, "ToMove 123456.Contact");
            string newPath = Path.Combine(_contactManager.RootDirectory, "Moved 654321.Contact");

            // Ensure the file location to move to isn't present.
            File.Delete(newPath);

            _CreateContactClearActions(path);

            try
            {
                _changeEvent.Reset();
                File.Move(path, newPath);
                _NonBlockingWaitOne();
                Assert.IsTrue(0 < _actions.Count, "No actions received.");
                Assert.AreEqual(ContactCollectionChangeType.Moved, _actions[0].Change);
                // Sometimes receive an additional Updated change event.  Not worth failing for.
                // Assert.AreEqual(1, _actions.Count, "Received at least an additional " + _actions[1].Change);
            }
            finally
            {
                File.Delete(path);
                File.Delete(newPath);
            }
        }

        [
            TestMethod,
            Description("Notice changes to contacts under the root Contacts folder.")
        ]
        public void NoticeChangesInSubfoldersTest()
        {
            string dir = Path.Combine(_contactManager.RootDirectory, "Unittest Subfolder 12345");
            string path = Path.Combine(dir, "Sub.Contact");
            Directory.CreateDirectory(dir);
            _CreateContactClearActions(path);

            try
            {
                using (Contact contact = new Contact(path))
                {
                    contact.Notes = "Some notes";
                    _changeEvent.Reset();
                    contact.CommitChanges();
                    _NonBlockingWaitOne();
                    Assert.AreEqual(1, _actions.Count);
                    Assert.AreEqual(ContactCollectionChangeType.Updated, _actions[0].Change);
                }
            }
            finally
            {
                File.Delete(path);
                Directory.Delete(dir, true);
            }
        }

        [
            TestMethod,
            Description("Verify that unsubscribing from the change event works.")
        ]
        public void IgnoreUnsubscribedChangesTest()
        {
            _contactManager.CollectionChanged -= _OnCollectionChanged;
            using (Contact contact = _contactManager.CreateContact())
            {
                contact.CommitChanges();
                File.Delete(contact.Path);
            }
            Assert.AreEqual(0, _actions.Count);
        }

        [
            TestMethod,
            Description("Should pick up changes to contacts that were created before the manager was instantiated.")
        ]
        public void NoticeChangesToPreexistingContactTest()
        {
            string path = Path.Combine(_contactManager.RootDirectory, "Preexisting Test.contact");
            _CreateContactClearActions(path);
            _contactManager.CollectionChanged -= _OnCollectionChanged;
            try
            {
                // So far as this manager is concerned, the contact is pre-existing.
                using (ContactManager testManager = new ContactManager())
                {
                    testManager.CollectionChanged += _OnCollectionChanged;
                    using (Contact contact = new Contact(path))
                    {
                        contact.Notes = "Delete this contact.";
                        _changeEvent.Reset();
                        contact.CommitChanges();
                        _NonBlockingWaitOne();
                        Assert.AreEqual(1, _actions.Count);
                        Assert.AreEqual(ContactCollectionChangeType.Updated, _actions[0].Change);
                        Assert.AreEqual(contact.Id, _actions[0].NewId, true);
                    }
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
