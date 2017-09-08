/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using Standard.Tests;
    using Standard.Interop;

    [TestClass]
    public class ContactManagerAlternateRootTests
    {
        [
            TestMethod,
            Description("Initializing the ContactManager to a non-existent root folder should cause the folder to be created")
        ]
        public void InitializeToNonExistentFolder()
        {
            string subfolder = Path.Combine(Environment.CurrentDirectory, new Random().NextDouble().ToString());
            Assert.IsFalse(Directory.Exists(subfolder));

            try
            {
                // Creating a manager rooted in a non-existent path should work.
                using (ContactManager manager = new ContactManager(subfolder))
                {
                    Assert.IsTrue(Directory.Exists(subfolder));
                    Assert.AreEqual(manager.RootDirectory, subfolder);

                    using (Contact contact = manager.CreateContact())
                    {
                        contact.Names.Default = new Name("SubfolderContact");
                        contact.CommitChanges();
                        Assert.AreEqual(subfolder, Path.GetDirectoryName(contact.Path));
                        Assert.IsTrue(File.Exists(contact.Path));
                        manager.Remove(contact.Id);
                    }
                }
            }
            finally
            {
                Directory.Delete(subfolder);
            }
        }

        [
            TestMethod,
            Description("ContactManagers can't be created if they're anchored to an uncreateable folder"),
        ]
        public void InitializeToUncreateableFolder()
        {
            // A corollary to this test is anchoring against a network directory.
            string invalidRoot = "Q:\\";
            if (Directory.Exists(invalidRoot))
            {
                Assert.Inconclusive("Didn't expect the user to have a Q: drive...");
            }
            UTVerify.ExpectException<DirectoryNotFoundException>(() => new ContactManager(invalidRoot));
        }

        [
            TestMethod,
            Description("A ContactManager rooted with the \"*\" relative syntax should be the same as a ContactManager initialized without an explicit root.")
        ]
        public void InitializeToContactsFolder()
        {
            using (ContactManager implicitManager = new ContactManager())
            {
                using (ContactManager explicitManager = new ContactManager("*"))
                {
                    Assert.AreEqual(implicitManager.RootDirectory, explicitManager.RootDirectory);
                }

                // This is an oddly formed rootManager, but it should also work.
                using (ContactManager explicitManager = new ContactManager("*\\/"))
                {
                    Assert.AreEqual(implicitManager.RootDirectory, explicitManager.RootDirectory);
                }
            }
        }

        [
            TestMethod,
            Description("Initializing a contact manager with a path rooted with \"*\" should cause a subfolder under the user's contacts folder.")
        ]
        public void InitializeToNonexistentContactsSubfolder()
        {
            string subfolder = new Random().Next().ToString();
            string fullPath = Path.Combine(ContactUtil.GetContactsFolder(), subfolder);
            Assert.IsFalse(Directory.Exists(fullPath));
            using (ContactManager explicitManager = new ContactManager("*\\" + subfolder))
            {
                Assert.AreEqual(fullPath, explicitManager.RootDirectory);
                Directory.Delete(explicitManager.RootDirectory);
            }
        }

        [TestMethod]
        public void InitializeExcessivelyLongPath()
        {
            UTVerify.ExpectException<PathTooLongException>(()=>
                new ContactManager("*\\" + new string('A', (int)Win32Value.MAX_PATH)));
        }

        [TestMethod]
        public void InitializeInvalidPath()
        {
            UTVerify.ExpectException<ArgumentException>(() => new ContactManager("*\\!!*"));
        }

        [TestMethod]
        public void ContainedContactsFromOtherManagers()
        {
            const string outerFolder = "*\\UnitTest1";
            const string innerFolder = outerFolder + "\\UnitTestSubfolder";

            TestUtil.PurgeContactManager(outerFolder);

            ContactManager rootManager = null;
            ContactManager subManager = null;
            try
            {
                rootManager = new ContactManager(outerFolder);
                subManager = new ContactManager(innerFolder);

                using (Contact innerContact = subManager.CreateContact())
                {
                    innerContact.Names.Default = new Name("Inner");
                    innerContact.CommitChanges();

                    // A contact created in a subManager-contct manager should be able to be loaded by a rootManager manager.
                    using (Contact c = rootManager.GetContact(innerContact.Id))
                    {
                        Assert.AreEqual(innerContact.Names.Default, c.Names.Default);
                    }
                }
            }
            finally
            {
                Utility.SafeDispose(ref rootManager);
                Utility.SafeDispose(ref subManager);
                // Covers subfolders...
                TestUtil.PurgeContactManager(outerFolder);
            }
        }


        [
            TestMethod,
            Description("ContactManagers shouldn't hold locks on their directories, and it should continue to work with a missing directory but some operations should fail.")
        ]
        public void DeleteContactManagerFolder()
        {
            if (Directory.Exists(Path.Combine(ContactUtil.GetContactsFolder(), "DeletableUnitTestFolder")))
            {
                Directory.Delete(Path.Combine(ContactUtil.GetContactsFolder(), "DeletableUnitTestFolder"));
            }
            using (ContactManager manager = new ContactManager("*\\DeletableUnitTestFolder"))
            {
                Assert.IsTrue(Directory.Exists(manager.RootDirectory));
                Directory.Delete(manager.RootDirectory);
                Assert.IsFalse(Directory.Exists(manager.RootDirectory));

                using (Contact c = manager.CreateContact())
                {
                    c.Names.Default = new Name("Won't be able to save");
                    UTVerify.ExpectException<DirectoryNotFoundException>(() => c.CommitChanges());
                }
            }
        }
    }
}