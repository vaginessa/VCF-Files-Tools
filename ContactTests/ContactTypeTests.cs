/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using Microsoft.Communications.Contacts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    using TypeExtensionPair = System.Collections.Generic.KeyValuePair<ContactTypes, string>;
    using Standard.Tests;

    [TestClass]
    public class ContactTypeTests
    {
        const string _contactFileName = "Alexei Karamazov.contact";

        [ClassInitialize]
        public static void InitializeContactTypeTests(TestContext testContext)
        {
            // Write the test contact file from the test's resources.
            using (StreamWriter sw = File.CreateText(_contactFileName))
            {
                sw.Write(Resources.Alexei_Karamazov);
            }
        }

        [ClassCleanup]
        public static void TeardownContactTypeTests()
        {
            File.Delete(_contactFileName);
        }

        [TestMethod]
        public void CreateStreamWithExplicitType()
        {
            using (FileStream fstream = new FileStream(_contactFileName, FileMode.Open))
            {
                using (Contact contact = new Contact(fstream, ContactTypes.Organization))
                {
                    Assert.AreEqual(ContactTypes.Organization, contact.ContactType);
                }
            }
        }

        [TestMethod]
        public void CreateStreamWithUnknownContactType()
        {
            using (FileStream fstream = new FileStream(_contactFileName, FileMode.Open))
            {
                using (Contact contact = new Contact(fstream, ContactTypes.Organization))
                {
                    Assert.AreEqual(ContactTypes.Organization, contact.ContactType);
                }
                // None ContactType is only OK for the Stream constructor.
                using (Contact contact = new Contact(fstream, ContactTypes.None))
                {
                    Assert.AreEqual(ContactTypes.None, contact.ContactType);
                }
            }
        }

        [TestMethod]
        public void CreateStreamWithInvalidTypes()
        {
            ContactTypes[] invalidTypes = new ContactTypes[]
            {
                ContactTypes.Organization | ContactTypes.Contact,
                (ContactTypes)(1 << 30),
            };
            foreach (ContactTypes type in invalidTypes)
            {
                try
                {
                    using (FileStream fstream = new FileStream(_contactFileName, FileMode.Open))
                    {
                        using (Contact contact = new Contact(fstream, type))
                        {
                            Assert.Fail();
                        }
                    }
                }
                catch (ArgumentException)
                { }
            }
        }

        [TestMethod]
        public void GetExtensionsFromTypes()
        {
            TypeExtensionPair[] typesToExtensions = new TypeExtensionPair[]
            {
                new TypeExtensionPair(ContactTypes.Organization, ".Organization"),
                new TypeExtensionPair(ContactTypes.Contact, ".Contact"),
                new TypeExtensionPair(ContactTypes.Group, ".Group"),
                new TypeExtensionPair(ContactTypes.Contact | ContactTypes.Group, ".Contact|.group"),
                new TypeExtensionPair(ContactTypes.All, ".contact|.group|.organization"),
            };

            foreach(TypeExtensionPair pair in typesToExtensions)
            {
                string[] fromTypes = Contact.GetExtensionsFromType(pair.Key).Split('|');
                string[] fromStrings = pair.Value.Split('|');
                Array.Sort<string>(fromTypes);
                Array.Sort<string>(fromStrings);
                for (int i = 0; i < fromStrings.Length; ++i)
                {
                    Assert.AreEqual(fromStrings[i].ToLowerInvariant(), fromTypes[i].ToLowerInvariant());
                }
            }
        }

        [TestMethod]
        public void GetTypesFromExtensions()
        {
            TypeExtensionPair[] typesFromExtensions = new TypeExtensionPair[]
            {
                new TypeExtensionPair(ContactTypes.Organization, ".Organization"),
                new TypeExtensionPair(ContactTypes.Contact, ".Contact"),
                new TypeExtensionPair(ContactTypes.Group, ".Group"),
                new TypeExtensionPair(ContactTypes.None, ".unknown"),
                new TypeExtensionPair(ContactTypes.None, @"Q:\test.txt"),
                new TypeExtensionPair(ContactTypes.None, "noextension"),
                new TypeExtensionPair(ContactTypes.Contact, @"..\relativePath\group.contact"),
            };

            foreach (TypeExtensionPair pair in typesFromExtensions)
            {
                Assert.AreEqual(pair.Key, Contact.GetTypeFromExtension(pair.Value));
            }
        }

        [TestMethod]
        public void GetTypeFromEmptyExtension()
        {
            UTVerify.ExpectException<ArgumentException>(() => Contact.GetTypeFromExtension(string.Empty));
        }

        [TestMethod]
        public void GetTypeFromNullExtension()
        {
            UTVerify.ExpectException<ArgumentNullException>(() => Contact.GetTypeFromExtension(null));
        }

        [TestMethod]
        public void GetTypeFromInvalidPath()
        {
            UTVerify.ExpectException<ArgumentException>(() => Contact.GetTypeFromExtension("|"));
        }

        [TestMethod]
        public void GetExtensionFromUnknownType()
        {
            UTVerify.ExpectException<ArgumentException>(() => Contact.GetExtensionsFromType(ContactTypes.None));
        }

        [TestMethod]
        public void GetExtensionFromInvalidType()
        {
            // Contains no valid bits
            UTVerify.ExpectException<ArgumentException>(() => Contact.GetExtensionsFromType((ContactTypes)999 ^ ContactTypes.All));
        }

        [TestMethod]
        public void GetExtensionFromInvalidPlusValidType()
        {
            ContactTypes invalid = (ContactTypes)999;
            Assert.AreNotEqual(0, invalid & ContactTypes.All);
            // Contains some valid bits.
            UTVerify.ExpectException<ArgumentException>(() => Contact.GetExtensionsFromType(invalid));
        }

        [
            TestMethod,
            Description("Shouldn't be able to create a contact of type None from a ContactManager.")
        ]
        public void CreateUnknownContactFromManager()
        {
            using (ContactManager manager = new ContactManager())
            {
                UTVerify.ExpectException<ArgumentException>(() => manager.CreateContact(ContactTypes.None));
            }
        }

        [
            TestMethod,
            Description("Ensure that ContactType is persisted through the ContactManager")
        ]
        public void GetTypeFromContactManagerLoadedContact()
        {
            using (ContactManager manager = new ContactManager())
            {
                string contactId = null;
                string groupId = null;
                Contact contact = null;
                Contact group = null;
                try
                {
                    contact = manager.CreateContact(ContactTypes.Contact);
                    contact.Names.Default = new Name("ContactType::Contact");
                    contact.CommitChanges();
                    contactId = contact.Id;
                    group = manager.CreateContact(ContactTypes.Group);
                    group.Names.Default = new Name("ContactType::Group");
                    group.CommitChanges();
                    groupId = group.Id;

                    Assert.AreEqual(ContactTypes.Contact, contact.ContactType);
                    Utility.SafeDispose(ref contact);
                    Assert.AreEqual(ContactTypes.Group, group.ContactType);
                    Utility.SafeDispose(ref group);

                    contact = manager.GetContact(contactId);
                    Assert.AreEqual(ContactTypes.Contact, contact.ContactType);
                    group = manager.GetContact(groupId);
                    Assert.AreEqual(ContactTypes.Group, group.ContactType);
                }
                finally
                {
                    Utility.SafeDispose(ref contact);
                    Utility.SafeDispose(ref group);
                    manager.Remove(contactId ?? "");
                    manager.Remove(groupId ?? "");
                }
            }
        }
    }
}
