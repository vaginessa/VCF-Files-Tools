/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using Microsoft.Communications.Contacts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class PersonTests
    {
        [
            TestMethod,
            Description("When a Person is instantiated from a contact it should act as a snapshot view over that contact.")
        ]
        public void ViewContactThroughPerson()
        {
            Person p;
            Name name = new Name("Person");
            EmailAddress address = new EmailAddress("cogito@ergo.sum");

            using (Contact contact = new Contact())
            {
                contact.Names.Add(name);
                contact.EmailAddresses.Add(address);

                p = new Person(contact);

                // The person should reflect the data from the contact when it was made.
                Assert.AreEqual(name.FormattedName, p.Name);
                Assert.AreEqual(address.Address, p.Email);

                contact.Names.RemoveAt(contact.Names.DefaultIndex);
                Assert.AreEqual(name.FormattedName, p.Name);
                Assert.AreEqual(default(Name), contact.Names.Default);
            }
        }

        [
            TestMethod,
            Description("Persons aren't affected by a backing contact no longer being in memory.")
        ]
        public void ViewDisposedContactThroughPerson()
        {
            Person p = default(Person);
            Name name = new Name("Disposable");

            using (Contact contact = new Contact())
            {
                contact.Names.Add(name);
                p = new Person(contact, null, "phone", null, null);
            }

            // Backing contact has now been disposed.

            Assert.AreEqual(name.FormattedName, p.Name);
            Assert.AreEqual("phone", p.Phone);
            Assert.AreEqual("", p.Email);
        }

        [
            TestMethod,
            Description("Persons aren't affected by a backing contact no longer being in memory.")
        ]
        public void ExplicitPropertiesOverrideImplicitProperties()
        {
            Person p = default(Person);
            Name name = new Name("throw away");
            PhoneNumber number = new PhoneNumber("206-555-1212");

            using (Contact contact = new Contact())
            {
                contact.Names.Add(name);
                contact.PhoneNumbers.Add(number, PhoneLabels.Cellular, PropertyLabels.Business);

                p = new Person(contact, null, "phone", null, null);

                Assert.AreEqual(name.FormattedName, p.Name);

                Assert.AreEqual("phone", p.Phone);
                Assert.AreEqual("", p.Email);
            }
        }

        [TestMethod]
        public void SetPersonOnContact()
        {
            using (Contact member = new Contact())
            {
 
            }
        }

        [TestMethod]
        public void BuildPersonFromBuilder()
        {
            using (Contact contact = new Contact())
            {
                PersonBuilder builder = new PersonBuilder("Person", contact.Id);
                contact.EmailAddresses.Default = new EmailAddress("Email");

                // Explicitly set phone, it should round-trip.
                builder.Phone = "Phone";
                Assert.AreEqual("Phone", builder.Phone);

                // E-mail is implicitly set,
                // it should only be inferred when a Person backed by a committed contact.
                Assert.IsNull(builder.Email);

                Person p = builder;
                Assert.AreEqual(builder.Phone, p.Phone);
                Assert.AreEqual("", p.Email);
            }
        }

        [TestMethod]
        public void InferPropertiesFromId()
        {
            // Tokens are case sensitive.  User's should never really be generating these though.
            string singlePhone = "206-555-1212";
            string singleEmail = "Eml@microsoft.com";
            string goodId = "/PHONE:\"" + singlePhone + "\" /IGNORE:\"OMG! PONIES!!1!\" /EMAIL:\"" + singleEmail + "\"";

            Person p = new Person(null, goodId, null);
            Assert.AreEqual(ContactTypes.None, p.ContactType);
            Assert.AreEqual(singleEmail, p.Email);
            Assert.AreEqual(singlePhone, p.Phone);
            Assert.AreEqual("", p.Name);

            string badId = "/GUID:\"not really a guid\"";
            p = new Person("Name", badId, null);
            Assert.AreEqual(ContactTypes.None, p.ContactType);
            Assert.AreEqual("", p.Email);
            Assert.AreEqual("", p.Phone);
            Assert.AreEqual("Name", p.Name);
        }
    }
}
