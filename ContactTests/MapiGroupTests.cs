
namespace Microsoft.Communications.Contacts.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Communications.Contacts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    /// <summary>
    /// Summary description for MapiGroupTests
    /// </summary>
    [TestClass]
    public class MapiGroupTests
    {
        private const string _groupFileName = "MapiGroup.group";
        private const string _contactIdKey = "[MSWABMAPI]PropTag0x66001102";
        private const string _nameEmailKey = "[MSWABMAPI]PropTag0x80091102";

        private Contact _contact;
        private MapiGroupView _groupView;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            using (StreamWriter sw = File.CreateText(_groupFileName))
            {
                sw.Write(Resources.MapiGroup);
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            File.Delete(_groupFileName);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _contact = new Contact(_groupFileName);
            _groupView = new MapiGroupView(_contact);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Utility.SafeDispose(ref _contact);
            Utility.SafeDispose(ref _groupView);
        }

        [TestMethod]
        public void LoadMapiGroupAsContact()
        {
            Contact contact = new Contact(_groupFileName);
            Assert.AreEqual("Group", contact.Names.Default.FormattedName);
        }

        [TestMethod]
        public void ViewContactAsMapiGroup()
        {
            using (Contact contact = new Contact())
            {
                using (MapiGroupView groupView = new MapiGroupView(contact))
                {
                    Assert.AreEqual(0, groupView.MemberIds.Count);
                    Assert.AreEqual(0, groupView.OneOffMembers.Count);
                }
            }
        }

        [
            TestMethod,
            Description("Check that a contact is correctly matched for containing MAPI group properties.")
        ]
        public void DetectMapiGroup()
        {
            Assert.IsTrue(MapiGroupView.HasMapiProperties(_contact));
            _contact.RemoveProperty(_contactIdKey);
            Assert.IsTrue(MapiGroupView.HasMapiProperties(_contact));
            _contact.RemoveProperty(_nameEmailKey);
            Assert.IsFalse(MapiGroupView.HasMapiProperties(_contact));
        }

        [TestMethod]
        public void GetContactMembers()
        {
            List<string> ids = new List<string>();
            ids.Add("/GUID:\"5ac2d943-e93d-492f-b852-b0dc94a62f76\" /PATH:\"C:\\Users\\JoeCast\\Contacts\\Alexei Karamazov.contact\"");
            ids.Add("/GUID:\"1f6f9929-c0d7-49f6-8660-5a7ab50c12ac\" /PATH:\"C:\\Users\\JoeCast\\Contacts\\Group.group\"");

            Assert.AreEqual(2, _groupView.MemberIds.Count);
            foreach (string id in _groupView.MemberIds)
            {
                Assert.IsTrue(ids.Contains(id));
                ids.Remove(id);
            }
            Assert.AreEqual(0, ids.Count);
        }

        [TestMethod]
        public void GetContactOneOffs()
        {
            List<Person> oneOffs = new List<Person>();
            oneOffs.Add(new Person("Prince Myshkin", null, "and.hold.the.relish@pinks", null));
            oneOffs.Add(new Person("Fyodor", null, "webmaster@dostoevsky.ru", null));

            Assert.AreEqual(2, _groupView.OneOffMembers.Count);
            foreach (Person person in _groupView.OneOffMembers)
            {
                Assert.IsTrue(oneOffs.Contains(person));
                oneOffs.Remove(person);
            }
            Assert.AreEqual(0, oneOffs.Count);
        }
    }
}
