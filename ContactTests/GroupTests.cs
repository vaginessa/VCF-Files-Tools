using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Tests;

namespace Microsoft.Communications.Contacts.Tests
{
    /// <summary>
    /// Summary description for GroupTests
    /// </summary>
    [TestClass]
    public class GroupTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void AddMembers()
        {
            using (Contact c = new Contact(ContactTypes.Group))
            {
                using (GroupView group = new GroupView(c))
                {
                    c.Names.Default = "Whoville";
                    c.People.Add(new Person("Thing 1", null, "thing@seuss.com", null), PersonLabels.Agent);

                    Assert.AreEqual(0, group.Members.Count);

                    c.People.Add(new Person("Cat"), PersonLabels.Member);

                    Assert.AreEqual(1, group.Members.Count);

                    group.Members.Add(new Person("Sam I Am"), PersonLabels.Manager);

                    Assert.AreEqual(2, group.Members.Count);
                    Assert.AreEqual(3, c.People.Count);
                    Assert.AreEqual(1, group.Members.IndexOfLabels(PersonLabels.Manager));
                    Assert.AreEqual(0, group.Members.IndexOfLabels(PersonLabels.Member));
                }
            }
        }

        [TestMethod]
        public void GetMembers()
        {
            using (Contact c = new Contact(ContactTypes.Group))
            {
                Person member = new Person("Foo");

                using (GroupView group = new GroupView(c))
                {
                    group.Members.Add(member);
                }

                using (GroupView group = new GroupView(c))
                {
                    Assert.AreEqual(member, group.Members[0]);
                }
            }
        }

        [TestMethod]
        public void GetEmail()
        {
            using (Contact c = new Contact(ContactTypes.Group))
            {
                using (GroupView group = new GroupView(c))
                {
                    c.EmailAddresses.Add("noone@nowhere.com");

                    Assert.AreEqual(0, group.ExpandEmailAddresses().Count);

                    group.Members.Add(new Person("Andy Warhol", null, "Andy@15minutes.com", null));

                    Assert.IsTrue(group.ExpandEmailAddresses().Contains("Andy@15minutes.com"));
                }
            }
        }

        [TestMethod]
        public void GetContactEmail()
        {
            try
            {
                using (ContactManager cm = new ContactManager("*\\UnitTests"))
                {
                    using (Contact member = new Contact())
                    {
                        member.EmailAddresses.Add("joe@cast.com");
                        member.EmailAddresses.Add("cast@joe.com", PropertyLabels.Preferred);
                        cm.AddContact(member);
                        using (Contact c = cm.CreateContact(ContactTypes.Group))
                        {
                            using (GroupView group = new GroupView(c))
                            {
                                group.Members.Add(new Person(member));
                                Assert.IsTrue(group.ExpandEmailAddresses().Contains(member.EmailAddresses.Default.Address));
                                Assert.AreEqual(1, group.ExpandEmailAddresses().Count);
                            }
                        }
                    }
                }
            }
            finally
            {
                TestUtil.PurgeContactManager("*\\UnitTests");
            }
        }

        [TestMethod]
        public void CreateNonGroupView()
        {
            UTVerify.ExpectException<ArgumentException>(() => new GroupView(new Contact()));
        }

        [TestMethod]
        [Ignore] // Still thinking about this case....
        public void RecurseGroupEmailAddresses()
        {
            try
            {
                using (ContactManager cm = new ContactManager("*\\UnitTests"))
                {
                    using (Contact member = cm.CreateContact())
                    {
                        member.EmailAddresses.Add("joe@cast.com");
                        member.CommitChanges();

                        using (Contact c1 = cm.CreateContact(ContactTypes.Group))
                        {
                            c1.EmailAddresses.Add("group@54.com");
                            using (GroupView group = new GroupView(c1))
                            {
                                group.Members.Add(new Person(member));
                                Assert.IsTrue(group.ExpandEmailAddresses().Contains(member.EmailAddresses.Default.Address));
                                Assert.AreEqual(1, group.ExpandEmailAddresses().Count);
                            }

                            using (Contact c2 = cm.CreateContact(ContactTypes.Group))
                            {
                                using (GroupView group2 = new GroupView(c2))
                                {
                                    group2.Members.Add(new Person(c1));
                                    var emails = group2.ExpandEmailAddresses();
                                    Assert.IsTrue(emails.Contains(member.EmailAddresses.Default.Address));
                                    Assert.IsFalse(emails.Contains(c1.EmailAddresses.Default.Address));
                                    Assert.AreEqual(1, emails.Count);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                TestUtil.PurgeContactManager("*\\UnitTests");
            }
        }
    }
}
