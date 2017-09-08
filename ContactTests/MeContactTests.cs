/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
    using Standard.Tests;

    [TestClass]
    public class MeContactTests
    {
        private const int _WaitTimeout = 5000;
        private const int _FastWaitTimeout = 500;

        private const string _RootTestFolder = @"*\MeContactTests";
        private static string[] _contactIds;
        private ContactManager _manager;

        // Use events to wait for changes to get registered.
        private AutoResetEvent _changeEvent = new AutoResetEvent(false);
        private int _meChangeCount;

        private void _MeContactChangeHandler(object sender, PropertyChangedEventArgs e)
        {
            // Right now this is the only property triggered for ContactManager.
            Assert.AreEqual("MeContact", e.PropertyName);

            ++_meChangeCount;
            _changeEvent.Set();
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            TestUtil.PurgeContactManager(_RootTestFolder);
            using (ContactManager cm = new ContactManager(_RootTestFolder))
            {
                _contactIds = new string[4];
                for (int i = 0; i < _contactIds.Length; ++i)
                {
                    using (Contact contact = cm.CreateContact())
                    {
                        contact.Names.Add(new Name("Me #" + i.ToString()));
                        contact.CommitChanges();
                        _contactIds[i] = contact.Id;
                    }
                }
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestUtil.PurgeContactManager(_RootTestFolder);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestUtil.SetMeRegistryValue(_RootTestFolder, "");
            _manager = new ContactManager(_RootTestFolder);
            _meChangeCount = 0;
            _changeEvent.Reset();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Utility.SafeDispose(ref _manager);
        }

        private void _PushManagersDispatcher()
        {
            // Try to process other threads to queue up whatever they need to this dispatcher's thread.
            System.Windows.Forms.Application.DoEvents();

            // Push the Dispatcher's queue.
            DispatcherFrame frame = new DispatcherFrame(true);
            ThreadStart ts = delegate { frame.Continue = false; };
            // Put this delegate into (probably, at least effectively) the back of the queue.
            // All other pending changes should be processed before this gets hit.
            _manager.Dispatcher.BeginInvoke(DispatcherPriority.Normal, ts);
            Dispatcher.PushFrame(frame);
        }

        private void _WaitOnChange(bool isFailureExpected)
        {
            // Don't wait as long if we're not expecting the change to get hit.
            int wait = isFailureExpected ? _FastWaitTimeout : _WaitTimeout;

            int waitRet = WaitHandle.WaitAny(new[] { _changeEvent }, wait, true);
            // If this didn't succeed then the return should be WaitTimeout
            Standard.Assert.Implies(0 != waitRet, WaitHandle.WaitTimeout == waitRet);

            if (!isFailureExpected && waitRet == WaitHandle.WaitTimeout)
            {
                _PushManagersDispatcher();
                waitRet = WaitHandle.WaitAny(new[] { _changeEvent }, wait, true);
                Standard.Assert.Implies(0 != waitRet, WaitHandle.WaitTimeout == waitRet);
            }

            Assert.AreEqual(isFailureExpected ? WaitHandle.WaitTimeout : 0, waitRet);
        }


        [
            TestMethod,
            Description("Add a Me contact when there was none before.")
        ]
        public void AddNewMeContact()
        {
            Assert.IsNull(_manager.MeContact);

            _manager.PropertyChanged += _MeContactChangeHandler;

            using (Contact contact = _manager.GetContact(_contactIds[0]))
            {
                Assert.AreEqual(contact.Id, _contactIds[0]);

                _manager.MeContact = contact;

                // Setting the MeContact when there was none
                // should have triggered the change event.
                //
                // Need to pump messages for the change to be seen.
                _WaitOnChange(false);

                Assert.AreEqual(1, _meChangeCount);

                using (Contact meContact1 = _manager.MeContact)
                {
                    Assert.AreEqual(meContact1.Id, contact.Id);
                }
            }

            // Get me again after disposing the Contact used to set the Me contact.
            using (Contact meContact2 = _manager.MeContact)
            {
                Assert.AreEqual(meContact2.Id, _contactIds[0]);
            }

            // Nothing else we did should have incremented the change count.
            _WaitOnChange(true);

            Assert.AreEqual(1, _meChangeCount);
        }

        [
            TestMethod,
            Description("Add a Me contact when there's an Id in the registry but it's no longer valid.")
        ]
        public void ReplaceInvalidMeContact()
        {
            // A new guid isn't going to match an existing contact.
            string missingContactId = "/GUID:\"" + new Guid().ToString() + "\"";

            TestUtil.SetMeRegistryValue(_RootTestFolder, missingContactId);

            using (Contact contact = _manager.GetContact(_contactIds[1]))
            {
                _manager.MeContact = contact;
                Assert.AreEqual(contact.Id, _manager.MeContact.Id);
                Assert.AreEqual(TestUtil.GetMeRegistryValue(_RootTestFolder), contact.Id);
            }
        }

        [
            TestMethod,
            Description("Trying to get a Me contact that's invalid shouldn't result in a change notification.")
        ]
        public void SeeNoChangeFromGettingMissingMeContact()
        {
            _manager.PropertyChanged += _MeContactChangeHandler;

            // A new guid isn't going to match an existing contact.
            string missingContactId = "/GUID:\"" + new Guid().ToString() + "\"";

            TestUtil.SetMeRegistryValue(_RootTestFolder, missingContactId);

            _WaitOnChange(false);

            Assert.AreEqual(1, _meChangeCount);

            using (Contact c = _manager.MeContact)
            {
                Assert.IsNull(c);
            }

            // Pass true for isFailureExpected because we're not expecting this to get hit.
            _WaitOnChange(true);

            Assert.AreEqual(1, _meChangeCount);
        }

        [
            TestMethod,
            Description("Change the registry key for the Me contact to something invalid.")
        ]
        public void SeeNoChangeFromGettingInvalidMeContact()
        {
            _manager.PropertyChanged += _MeContactChangeHandler;

            // Set the MeContact's Id to be something that can't resolve to a contact.
            TestUtil.SetMeRegistryValue(_RootTestFolder, "/GUID:\"Badly-Formed Id");

            _WaitOnChange(false);

            Assert.AreEqual(1, _meChangeCount);

            using (Contact c = _manager.MeContact)
            {
                Assert.IsNull(c);
            }

            // Pass true for isFailureExpected because we're not expecting this to get hit.
            _WaitOnChange(true);

            Assert.AreEqual(1, _meChangeCount);
        }

        [
            TestMethod,
            Description("Delete the root registry key for the Me contact.")
        ]
        public void SetMeInformationToNull()
        {
            TestUtil.SetMeRegistryValue(_RootTestFolder, "/GUID:\"" + new Guid().ToString() + "\"");
            _manager.PropertyChanged += _MeContactChangeHandler;

            var meKeys = TestUtil.BackupAndPurgeMeRegistryKeys();

            _WaitOnChange(false);
            Assert.AreEqual(1, _meChangeCount);

            TestUtil.RestoreMeRegistryKeys(meKeys);

            _WaitOnChange(false);
            Assert.AreEqual(2, _meChangeCount);

            Assert.IsFalse(string.IsNullOrEmpty(TestUtil.GetMeRegistryValue(_RootTestFolder)));
        }

        [
            TestMethod,
            Description("Set the Me contact to the contact that's already Me.")
        ]
        public void SetMeToMe()
        {
            using (Contact contact = _manager.GetContact(_contactIds[0]))
            {
                _manager.MeContact = contact;
                _manager.PropertyChanged += _MeContactChangeHandler;
                // Shouldn't mess anything up, but also shouldn't signal a change.
                _manager.MeContact = contact;

                // Pass true for isFailureExpected because we're not expecting this to get hit.
                _WaitOnChange(true);

                Assert.AreEqual(0, _meChangeCount);

                using (Contact meContact = _manager.MeContact)
                {
                    Assert.AreEqual(meContact.Id, contact.Id);
                }
            }
        }

        [
            TestMethod,
            Description("Set the Me contact to NULL.")
        ]
        public void SetMeContactToNull()
        {
            // Shouldn't be able to se the Me contact to null,
            //   even when there is no Me contact.
            try
            {
                _manager.MeContact = null;
                Assert.Fail();
            }
            catch (ArgumentNullException)
            { }

            TestUtil.SetMeRegistryValue(_RootTestFolder, _contactIds[0]);

            try
            {
                _manager.MeContact = null;
                Assert.Fail();
            }
            catch (ArgumentNullException)
            { }
        }

        [
            TestMethod,
            Description("Set the Me contact to a contact that hasn't been committed.")
        ]
        public void SetMeToUncommittedContact()
        {
            using (Contact contact = _manager.CreateContact())
            {
                try
                {
                    _manager.MeContact = contact;
                    Assert.Fail();
                }
                catch (UnreachableContactException)
                { }
            }
        }

        [
            TestMethod,
            Description("Make changes to the Me contact.")
        ]
        public void CommitChangesToMe()
        {
            using (Contact newContact = _manager.CreateContact())
            {
                newContact.Names.Add(new Name("Wil be Me"));
                newContact.CommitChanges();

                try
                {
                    TestUtil.SetMeRegistryValue(_RootTestFolder, newContact.Id);
                    _manager.PropertyChanged += _MeContactChangeHandler;

                    newContact.PhoneNumbers.Add(new PhoneNumber("555-1221"));
                    newContact.CommitChanges();

                    _PushManagersDispatcher();

                    _WaitOnChange(false);
                    Assert.AreEqual(1, _meChangeCount);

                    newContact.EmailAddresses.Add(new EmailAddress("invalid email"));
                    newContact.CommitChanges();

                    _WaitOnChange(false);
                    Assert.AreEqual(2, _meChangeCount);
                }
                finally
                {
                    _manager.Remove(newContact.Id);
                }
            }
        }

        [
            TestMethod,
            Description("Commit changes to a contact other than Me.  Shouldn't see it as a change to the Me contact.")
        ]
        public void CommitNonMeContactChanges()
        {
            TestUtil.SetMeRegistryValue(_RootTestFolder, _contactIds[0]);

            _manager.PropertyChanged += _MeContactChangeHandler;

            using (Contact contact = _manager.CreateContact())
            {
                try
                {
                    contact.Names.Add(new Name("I am not myself"));
                    contact.CommitChanges();
                    contact.PhoneNumbers.Add(new PhoneNumber("411"));
                    contact.CommitChanges();
                    contact.Names.Default = new Name("Still not me.");

                    _PushManagersDispatcher();

                    _WaitOnChange(true);

                    Assert.AreEqual(0, _meChangeCount);
                }
                finally 
                {
                    _manager.Remove(contact.Id);
                }
            }
        }

        [
            TestMethod,
            Description("Make changes to the Me contact that result in a rename of the backing file.")
        ]
        public void RenameMe()
        {
            using (Contact newContact = _manager.CreateContact())
            {
                newContact.Names.Add(new Name("Will be Me"));
                newContact.CommitChanges();

                try
                {
                    TestUtil.SetMeRegistryValue(_RootTestFolder, newContact.Id);
                    _manager.PropertyChanged += _MeContactChangeHandler;

                    // This property is interesting because it causes the file to be renamed.
                    newContact.Names.Default = new Name("This is Me");
                    newContact.CommitChanges();

                    _PushManagersDispatcher();

                    _WaitOnChange(false);
                    Assert.AreEqual(2, _meChangeCount);
                }
                finally
                {
                    _manager.Remove(newContact.Id);
                }
            }
        }

        [
            TestMethod,
            Description("Delete the Me contact.")
        ]
        public void DeleteMe()
        {
            using (Contact contact = _manager.CreateContact())
            {
                contact.CommitChanges();
                try
                {
                    _manager.MeContact = contact;

                    _manager.PropertyChanged += _MeContactChangeHandler;
                    _manager.Remove(contact.Id);

                    // Need to process the manager's changes.
                    _PushManagersDispatcher();

                    _WaitOnChange(false);
                    Assert.AreEqual(1, _meChangeCount);

                    using (Contact shouldBeNull = _manager.MeContact)
                    {
                        Assert.IsNull(shouldBeNull);
                    }
                }
                finally
                {
                    _manager.Remove(contact.Id);
                }
            }
        }

        [
            TestMethod,
            Description("Verify that the Me Contact for the root folder doesn't use a named registry value.")
        ]
        public void VerifyDefaultManagerSharesWindowsMe()
        {
            using (ContactManager defaultManager = new ContactManager())
            {
                Assert.AreEqual("", defaultManager.MeManager.RegistryValue);
            }
        }

        [
            TestMethod,
            Description("Verify that the Me Contact for the root folder doesn't use a named registry value.")
        ]
        public void VerifyNonDefaultManagerDoesntShareWindowsMe()
        {
            Assert.AreEqual(_manager.RootDirectory, _manager.MeManager.RegistryValue);
        }

        [
            TestMethod,
            Description("Set Me contact to different types of contacts.")
        ]
        public void SetNonContactTypeMe()
        {
            using (Contact business = _manager.CreateContact(ContactTypes.Organization))
            {
                business.CommitChanges();
                _manager.MeContact = business;

                using (Contact meContact = _manager.MeContact)
                {
                    Assert.AreEqual(meContact.Id, business.Id);
                }
                _manager.PropertyChanged += _MeContactChangeHandler;

                business.Addresses.Add(new PhysicalAddress(null, "1 Microsoft Way", "Redmond", "WA", "98052", "USA", null, null));
                business.CommitChanges();

                _PushManagersDispatcher();
                _WaitOnChange(false);
                Assert.AreEqual(1, _meChangeCount);
             }
        }

        [TestMethod]
        public void PeerContactManagersDontShareMeContacts()
        {
            ContactManager manager1 = null;
            ContactManager manager2 = null;
            string folder1 = null;
            string folder2 = null;

            DateTime dt = new DateTime(1766, 12, 1);

            try
            {
                manager1 = new ContactManager("*\\MeUnitTest1");
                folder1 = manager1.RootDirectory;
                manager2 = new ContactManager("*\\MeUnitTest2");
                folder2 = manager2.RootDirectory;

                using (Contact c = manager1.CreateContact())
                {
                    c.Names.Default = new Name("Cogito ergo sum");
                    c.Dates[DateLabels.Birthday] = dt;
                    c.CommitChanges();
                    manager1.MeContact = c;
                }
                using (Contact c = manager2.CreateContact())
                {
                    c.Names.Default = new Name("Que sera sera");
                    c.Dates[DateLabels.Birthday] = dt;
                    c.CommitChanges();
                    manager2.MeContact = c;
                }

                Assert.AreNotEqual(manager2.MeContact.Names.Default, manager1.MeContact.Names.Default);
                Assert.AreEqual(manager2.MeContact.Dates[DateLabels.Birthday], manager1.MeContact.Dates[DateLabels.Birthday]);
            }
            finally
            {
                Utility.SafeDispose(ref manager1);
                Utility.SafeDispose(ref manager2);
                TestUtil.PurgeContactManager(folder1);
                TestUtil.PurgeContactManager(folder2);
            }
        }

        [
            TestMethod,
            Description("Try to set the Me contact to a contact that's outside of the address book."),
        ]
        public void SetMeOutsideContacts()
        {
            Contact contact = null;
            string path = null;

            // This manager should be a subfolder of the root contacts folder.
            Assert.IsFalse(ContactUtil.GetContactsFolder().StartsWith(_manager.RootDirectory));

            try
            {
                path = Path.Combine(ContactUtil.GetContactsFolder(), "Me 1111111.Contact");
                contact = new Contact();
                contact.Save(path);
                contact.Dispose();

                // reload the contact to get the fullest Id.
                contact = new Contact(path);

                UTVerify.ExpectException<UnreachableContactException>(() => _manager.MeContact = contact);
            }
            finally
            {
                Utility.SafeDispose(ref contact);
                Utility.SafeDeleteFile(path);
            }
        }

        [
            TestMethod,
            Description("Change which contact is the me contact.")
        ]
        public void SwitchMe()
        {
            TestUtil.SetMeRegistryValue(_RootTestFolder, _contactIds[0]);

            _manager.PropertyChanged += _MeContactChangeHandler;

            for (int i = 1; i < _contactIds.Length; ++i)
            {
                using (Contact contact = _manager.GetContact(_contactIds[i]))
                {
                    _manager.MeContact = contact;

                    _WaitOnChange(false);
                    Assert.AreEqual(i, _meChangeCount);
                }
            }
        }

        [
            TestMethod,
            Description("Ignore changes made to another Me contact.")
        ]
        public void IgnoreOtherManagersMeChanges()
        {
            TestUtil.SetMeRegistryValue(_RootTestFolder, _contactIds[0]);
            _manager.PropertyChanged += _MeContactChangeHandler;

            try
            {
                using (ContactManager otherManager = new ContactManager(_RootTestFolder + "2"))
                {
                    using (Contact otherMe = otherManager.CreateContact())
                    {
                        otherMe.Names.Add(new Name("Maybe in another life"));
                        otherMe.CommitChanges();

                        otherManager.MeContact = otherMe;

                        _WaitOnChange(true);
                        Assert.AreEqual(0, _meChangeCount);
                    }
                }
            }
            finally
            {
                TestUtil.PurgeContactManager(_RootTestFolder + "2");
            }
        }
    }
}
