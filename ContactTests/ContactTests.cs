/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

//
// TODO: Tests to add:
//  Try bizarre simple extension namespaces, e.g. "123", "A:B", "A,B"
//  Set string on a simple extension property that was already created as an array node.
//  Create a simple extension array node on a property that was already used for a string property.
//

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Microsoft.Communications.Contacts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;
    using Standard.Tests;

    // Disambiguate between Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    // Disambiguate between System.ComponentModel.DescriptionAttribute
    using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;


    [TestClass]
    public class ContactThreadTests
    {
        private const int WaitTimeout = 1000;
        private ManualResetEvent _threadEvent;
        private Contact _contact;

        [TestInitialize]
        public void TestInitialize()
        {
            _threadEvent = new ManualResetEvent(false);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Utility.SafeDispose(ref _contact);
        }

        [
            TestMethod,
            Description("Can't create a Contact from an MTA thread."),
        ]
        public void CreateOnMtaThreadTest()
        {
            bool passed = false;
            Thread t = new Thread((ThreadStart)delegate
                {
                    try
                    {
                        _contact = new Contact();
                    }
                    catch (InvalidOperationException)
                    {
                        passed = true;
                        _threadEvent.Set();
                    }
                });
            t.SetApartmentState(ApartmentState.MTA);
            // Wait for the expected exception's throw to signal this event.
            _threadEvent.Reset();

            t.Start();
            Assert.IsTrue(_threadEvent.WaitOne(WaitTimeout, false), "Didn't catch the expected exception in a reasonable amount of time");
            Assert.IsTrue(passed);
            Assert.IsNull(_contact);
        }

        [
            TestMethod,
            Description("Contacts shouldn't be able to be accessed from threads other than the one on which they were created.")
        ]
        public void AccessFromOtherThreadTest()
        {
            _contact = new Contact();

            foreach (ApartmentState state in new ApartmentState[] { ApartmentState.MTA, ApartmentState.STA })
            {
                bool passed = false;
                Thread t = new Thread((ThreadStart)delegate
                    {
                        try
                        {
                            _contact.Notes = "Foo";
                        }
                        catch (InvalidOperationException)
                        {
                            passed = true;
                            _threadEvent.Set();
                        }
                    });
                t.SetApartmentState(ApartmentState.MTA);
                // Wait for the expected exception's throw to signal this event.
                _threadEvent.Reset();

                t.Start();
                Assert.IsTrue(_threadEvent.WaitOne(WaitTimeout, false), "Didn't catch the expected exception in a reasonable amount of time");
                Assert.IsTrue(passed);
                Assert.IsNotNull(_contact);
            }
        }

        [
            TestMethod,
            Description("Methods and properties on Contacts can be called from other threads if they go by way of the manager's Dispatcher property.")
        ]
        public void DispatchActionFromMtaThreadTest()
        {
            _contact = new Contact();

            Thread t = new Thread((ThreadStart)delegate
            {
                ThreadStart ts = delegate { _contact.Notes = "Foo"; };
                _contact.Dispatcher.Invoke(DispatcherPriority.Send, ts);
                _threadEvent.Set();
            });
            t.Name = "Dispatch for CreateContact";
            t.SetApartmentState(ApartmentState.MTA);
            // Wait for the expected exception's throw to signal this event.
            _threadEvent.Reset();

            t.Start();
            bool tryWait = false;
            // Avoid deadlocks... If I just do WaitOne, then this thread blocks
            // while the Dispatcher is trying to Invoke the method.
            for (int i = 0; i < 5 && !tryWait; ++i)
            {
                System.Windows.Forms.Application.DoEvents();
                tryWait = _threadEvent.WaitOne(WaitTimeout, false);
            }
            Assert.IsTrue(tryWait, "Didn't get signaled from the marshalled call in a reasonable amount of time");
            Assert.IsNotNull(_contact);
        }
    }

    /// <summary>
    /// Summary description for ContactTests
    /// </summary>
    [TestClass]
    public class ContactTests
    {
        private const string _contactFileName = "Alexei Karamazov.contact";
        private Contact _contact;
        private string _changedProperty;
        bool _notified;

        private ContactPropertyChangeType _changedType;

        [ClassInitialize()]
        public static void InitializeContactTests(TestContext testContext)
        {
            // Write the test contact file from the test's resources.
            StreamWriter sw = File.CreateText(_contactFileName);
            sw.Write(Resources.Alexei_Karamazov);
            sw.Close();
        }

        [ClassCleanup()]
        public static void TeardownContactTests()
        {
            File.Delete(_contactFileName);
        }

        [TestInitialize]
        public void PreTestInitialize()
        {
            Assert.IsNull(_contact);
            _contact = new Contact();
            _changedProperty = null;
        }

        [TestCleanup]
        public void PostTestCleanup()
        {
            Utility.SafeDispose(ref _contact);
        }

        private void _OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _notified = true;
            ContactPropertyChangedEventArgs cpc = (ContactPropertyChangedEventArgs)e;

            _changedProperty = cpc.PropertyName;
            _changedType = cpc.ChangeType;
        }

        [
            TestMethod,
            Description("Get and set some notes on the contact.")
        ]
        public void NotesTest()
        {
            string phrase = "Who knows what evil lurks in the hearts of men?  The Shadow knows!";
            Assert.AreEqual<string>("", _contact.Notes);

            // Register for change notifications.
            _notified = false;
            _contact.PropertyChanged += _OnPropertyChanged;

            // Setting a non-existent property to NULL (e.g. DeleteProperty) should still succeed.
            _contact.Notes = null;
            // Since that was a no-op, it shouldn't have triggered an event.
            Assert.IsFalse(_notified);

            _contact.Notes = phrase;
            Assert.AreEqual<string>(phrase, _contact.Notes);
            Assert.AreEqual<string>(PropertyNames.Notes, _changedProperty);
            Assert.AreEqual(ContactPropertyChangeType.PropertySet, _changedType);

            _contact.Notes = "";
            Assert.AreEqual("", _contact.Notes);
            Assert.AreEqual<string>(PropertyNames.Notes, _changedProperty);
            Assert.AreEqual(ContactPropertyChangeType.PropertyRemoved, _changedType);

            _contact.Notes = null;
            Assert.AreEqual("", _contact.Notes);
            Assert.AreEqual<string>(PropertyNames.Notes, _changedProperty);
            Assert.AreEqual(ContactPropertyChangeType.PropertyRemoved, _changedType);
        }

        [
            TestMethod,
            Description("Get and set the mailer on the contact.")
        ]
        public void MailerTest()
        {
            // Register for change notifications.
            _contact.PropertyChanged += _OnPropertyChanged;
            string program = "Windows Mail";
            Assert.AreEqual<string>("", _contact.Mailer);
            _contact.Mailer = program;
            Assert.AreEqual<string>(program, _contact.Mailer);
            Assert.AreEqual<string>(PropertyNames.Mailer, _changedProperty);
            _changedProperty = null;
            _contact.Mailer = null;
            Assert.AreEqual<string>("", _contact.Mailer);
            Assert.AreEqual<string>(PropertyNames.Mailer, _changedProperty);
            Assert.AreEqual(ContactPropertyChangeType.PropertyRemoved, _changedType);
        }

        [
            TestMethod,
            Description("Get and set the Gender on the conact.  Ensure invalid enumeration values are guarded against.")
        ]
        public void GenderTest()
        {
            Assert.AreEqual<Gender>(Gender.Unspecified, _contact.Gender);
            _contact.Gender = Gender.Female;
            Assert.AreEqual<Gender>(Gender.Female, _contact.Gender);
            _contact.Gender = Gender.Unspecified;
            Assert.AreEqual<Gender>(Gender.Unspecified, _contact.Gender);
            _contact.PropertyChanged += _OnPropertyChanged;
            _contact.Gender = Gender.Male;
            Assert.AreEqual<Gender>(Gender.Male, _contact.Gender);
            Assert.AreEqual<string>(PropertyNames.Gender, _changedProperty);
            Assert.AreEqual(ContactPropertyChangeType.PropertySet, _changedType);
            _changedProperty = null;

            Gender badGender = (Gender)6;
            Assert.IsFalse(Enum.IsDefined(typeof(Gender), badGender));
            try
            {
                _contact.Gender = (Gender)6;
                Assert.Fail("Shouldn't have been able to set the gender to an invalid enumeration value.");
            }
            catch (SchemaException)
            {
            }

            // Since the last set failed, we shouldn't have received a property change notification.
            Assert.IsNull(_changedProperty);
        }

        [
            TestMethod,
            Description("Get the creation date for the contact.  Compare it against other modification times.")
        ]
        public void GetCreationDateTest()
        {
            using (Contact contact = new Contact(_contactFileName))
            {
                Assert.IsTrue(contact.CreationDate.CompareTo(DateTime.Now) < 0);

                bool foundNotes = false;
                bool foundId = false;
                contact.Notes = "Notes";

                // Compare the creation time against the time for notes.  It should be earlier.
                // Compare the creation time against the first of the ContactID values.  It should be the same.
                string contactID = string.Format(PropertyNames.ContactIdValueFormat, 1);

                foreach (ContactProperty property in contact.GetPropertyCollection())
                {
                    if (property.Name == PropertyNames.Notes)
                    {
                        Assert.IsTrue(property.ModificationDate.CompareTo(contact.CreationDate) > 0);
                        foundNotes = true;
                    }
                    else if (property.Name == contactID)
                    {
                        Assert.AreEqual(property.ModificationDate, contact.CreationDate);
                        foundId = true;
                    }
                }
                Assert.IsTrue(foundNotes && foundId);
            }
        }

        [
            TestMethod,
            Description("Verify that UTC offsets are appropriately handled on read and write")
        ]
        public void SetLocalAndUtcDates()
        {
            using (Contact contact = new Contact(_contactFileName))
            {
                DateTime localTime = new DateTime(2007, 12, 25, 9, 0, 0, DateTimeKind.Local);
                contact.Dates.Default = localTime;
                DateTime getTime = contact.Dates.Default.Value;
                // Getting the date should retrieve the UTC version of this
                Assert.AreEqual(DateTimeKind.Utc, getTime.Kind);
                Assert.AreNotEqual(localTime.Ticks, getTime.Ticks);

                // UTC times should roundtrip exactly.
                DateTime universalTime = new DateTime(2006, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                contact.Dates.Add(universalTime);
                getTime = contact.Dates[contact.Dates.Count - 1].Value;
                Assert.AreEqual(DateTimeKind.Utc, getTime.Kind);
                Assert.AreEqual(universalTime.Ticks, getTime.Ticks);

                // If the kind isn't specified, Contacts should treat it like UTC.
                DateTime unspecifiedTime = new DateTime(1999, 10, 31);
                Assert.AreEqual(DateTimeKind.Unspecified, unspecifiedTime.Kind);
                contact.Dates.Add(unspecifiedTime);
                getTime = contact.Dates[contact.Dates.Count - 1].Value;
                Assert.AreEqual(DateTimeKind.Utc, getTime.Kind);
                Assert.AreEqual(unspecifiedTime.Ticks, getTime.Ticks);
            }
        }

        private const string guidToken = "/GUID:\"";
        private const string pathToken = "/PATH:\"";

        private void _ValidateContactId(string id, string expectedPath)
        {
            Assert.IsTrue(id.Contains(guidToken));

            // Expecting that the GUID is enclosed in quotes.
            int guidStart = id.IndexOf(guidToken) + guidToken.Length;
            int guidLength = id.IndexOf('\"', guidStart) - guidStart;
            // Guid's constructor should be able to deal with the substring.
            Guid guidPart = new Guid(id.Substring(guidStart, guidLength));

            if (null == expectedPath)
            {
                Assert.IsFalse(id.Contains(pathToken));
            }
            else
            {
                Assert.IsTrue(id.Contains(pathToken));

                // Expecting that the path is enclosed in quotes.
                int pathStart = id.IndexOf(pathToken) + pathToken.Length;
                int pathLength = id.IndexOf('\"', pathStart) - pathStart;
                Assert.AreEqual(expectedPath, id.Substring(pathStart, pathLength));
            }
        }

        [TestMethod]
        public void GetIdFromNewContactTest()
        {
            using (Contact contact = new Contact())
            {
                string id = contact.Id;
                _ValidateContactId(id, null);
            }
        }

        [TestMethod]
        public void GetIdFromFileTest()
        {
            using (Contact contact = new Contact(_contactFileName))
            {
                string id = contact.Id;
                string fullPath = Path.GetFullPath(_contactFileName);
                _ValidateContactId(id, fullPath);
            }
        }

        [
            TestMethod,
            Description("Use a PropertyCollection to walk simple extension properties on a modified contact.")
        ]
        public void GetSimpleExtensionsByPropertyCollectionTest()
        {
            using (Contact contact = new Contact())
            {
                contact.SetStringProperty("[ext]SomeString", "aardvark");
                string node1 = contact.AddNode("[ext:SomeNode]SomeCollection", true);
                contact.SetStringProperty(node1 + "/First", "first");
                contact.SetStringProperty(node1 + "/Second", "second");
                string node2 = contact.AddNode("[ext:SomeNode]SomeCollection", true);
                contact.SetStringProperty(node2 + "/First", "other first");

                List<string> expected = new List<string>(new string[] {
                    "[ext]SomeString",
                    "[ext]SomeCollection/SomeNode[1]",
                    "[ext]SomeCollection/SomeNode[1]/First",
                    "[ext]SomeCollection/SomeNode[1]/Second",
                    "[ext]SomeCollection/SomeNode[2]",
                    "[ext]SomeCollection/SomeNode[2]/First",
                });
                foreach (ContactProperty prop in contact.GetPropertyCollection())
                {
                    if (expected.Contains(prop.Name))
                    {
                        expected.Remove(prop.Name);
                    }
                }
                if (0 != expected.Count)
                {
                    Assert.Fail("Didn't find " + expected[0]);
                }
            }
        }

        [TestMethod]
        public void AddNodesAppendPrepend()
        {
            using (Contact contact = new Contact())
            {
                string schemaFormat = PropertyNames.NameCollection + PropertyNames.NameArrayNode + "[{0}]";
                string nodeName;

                nodeName = contact.AddNode(PropertyNames.NameCollection, true);
                Assert.AreEqual(string.Format(schemaFormat, 1), nodeName);

                // Could put a value so we can track the node.
                // Don't because we're really just verifying array node creation functionality.
                // contact.SetStringProperty(nodeName + PropertyNames.FormattedName, "Original name");
                
                nodeName = contact.AddNode(PropertyNames.NameCollection, true);
                Assert.AreEqual(string.Format(schemaFormat, 2), nodeName);
                Assert.AreEqual("", contact.GetStringProperty(nodeName + PropertyNames.FormattedName));

                nodeName = contact.AddNode(PropertyNames.NameCollection, false);
                Assert.AreEqual(string.Format(schemaFormat, 1), nodeName);
                //Assert.AreEqual("Original name", contact.GetStringProperty(string.Format(PropertyNames.NameFormattedNameFormat, 1)));

                int nodeCount = 0;
                foreach (ContactProperty prop in contact.GetPropertyCollection(PropertyNames.NameCollection, null, true))
                {
                    Assert.AreEqual(prop.PropertyType, ContactPropertyType.ArrayNode);
                    ++nodeCount;
                }
                Assert.AreEqual(3, nodeCount);
            }
        }

        [TestMethod]
        public void AddInvalidNode()
        {
            UTVerify.ExpectException<SchemaException>(
                () => _contact.AddNode("NameCollection2", true));
            // Contacts are still usable after that exception.
            string nodeName = _contact.AddNode(PropertyNames.CertificateCollection, true);
            Assert.AreEqual(PropertyNames.CertificateCollection + PropertyNames.CertificateArrayNode + "[1]", nodeName);
        }

        [TestMethod]
        public void AddSimpleExtensionNodesAppendPrepend()
        {
            using (Contact contact = new Contact())
            {
                string schemaFormat = "[UnitTests]NameCollection2/Name[{0}]";
                string nodeCreateString = "[UnitTests:Name]NameCollection2";
                string nodeName;

                nodeName = contact.AddNode(nodeCreateString, true);
                Assert.AreEqual(string.Format(schemaFormat, 1), nodeName);

                // Could put a value so we can track the node.
                // Don't because we're really just verifying array node creation functionality.
                // contact.SetStringProperty(nodeName + PropertyNames.FormattedName, "Original name");

                nodeName = contact.AddNode(nodeCreateString, true);
                Assert.AreEqual(string.Format(schemaFormat, 2), nodeName);
                Assert.AreEqual("", contact.GetStringProperty(nodeName + PropertyNames.FormattedName));

                nodeName = contact.AddNode(nodeCreateString, false);
                Assert.AreEqual(string.Format(schemaFormat, 1), nodeName);
                //Assert.AreEqual("Original name", contact.GetStringProperty(string.Format(PropertyNames.NameFormattedNameFormat, 1)));

                int nodeCount = 0;
                foreach (ContactProperty prop in contact.GetPropertyCollection("[UnitTests]NameCollection2", null, true))
                {
                    Assert.AreEqual(prop.PropertyType, ContactPropertyType.ArrayNode);
                    ++nodeCount;
                }
                Assert.AreEqual(3, nodeCount);
            }
        }

        [TestMethod]
        public void DisposeContactDuringPropertyEnumerationTest()
        {
            using (IEnumerator<ContactProperty> propEnum = _contact.GetPropertyCollection().GetEnumerator())
            {
                Assert.IsTrue(propEnum.MoveNext());
                _contact.Dispose();
                UTVerify.ExpectException<ObjectDisposedException>(() => propEnum.MoveNext());
            }
        }

        [TestMethod]
        public void ReadonlyContactTest()
        {
            try
            {
                _contact.Save("Readonly.Contact");
                Assert.IsFalse(_contact.IsReadOnly, "There's no file associated with this contact, so it shouldn't be readonly.");
                File.SetAttributes("Readonly.Contact", FileAttributes.ReadOnly);
                Assert.IsFalse(_contact.IsReadOnly, "The contact is still not associated with the file, despite that it's been saved there.  It should not pick up the attribute.");
                _contact.Dispose();

                _contact = new Contact("Readonly.Contact");
                Assert.IsTrue(_contact.IsReadOnly);
                // The collections in the contact aren't readonly, though.
                Assert.IsFalse(_contact.ContactIds.IsReadOnly);

                // Label collection should also be not-readonly
                string node1 = _contact.AddNode(PropertyNames.NameCollection, true);
                Assert.IsFalse(_contact.GetLabelCollection(node1).IsReadOnly);

                // Set some properties on the contact, the readonly-ness of it should only impact saving.
                _contact.Notes = "Some Text";

                File.SetAttributes("Readonly.Contact", FileAttributes.Normal);
                Assert.IsFalse(_contact.IsReadOnly, "This property shouldn't be cached.");
                _contact.Dispose();
            }
            finally
            {
                File.Delete("Readonly.Contact");
            }
        }

        [
            TestMethod,
            Description("Adds and removes simple extension array nodes on the contact.")
        ]
        public void AddRemoveNodesTest()
        {
            // Create a couple nodes to work with.
            // (So long as appendNode=true, don't need to worry about the name changing under the test.)
            string node1 = _contact.AddNode("[ns:Node]Collection", true);
            _contact.SetStringProperty(node1 + "/First", "First Value");
            _contact.SetStringProperty(node1 + "/Second", "Second Value");
            _contact.GetLabelCollection(node1).Add(PropertyLabels.Business);

            // Ensure that adding the label a second time results in the label only being present once.
            _contact.GetLabelCollection(node1).Add(PropertyLabels.Business);
            int labelCount = 0;
            foreach(string label in _contact.GetLabelCollection(node1))
            {
                if (PropertyLabels.Business == label)
                {
                    ++labelCount;
                }
            }
            Assert.AreEqual(1, labelCount);

            string node2 = _contact.AddNode("[ns:Node]Collection", true);
            _contact.SetStringProperty(node2 + "/First", "Third Value");
            _contact.SetStringProperty(node2 + "/Second", "Fourth Value");
            _contact.GetLabelCollection(node2).Add(PropertyLabels.Business);

            // Now have two name nodes: both have some data, one has a label.

            // Deleting the first node shouldn't affect the second.
            _contact.RemoveNode(node1);

            string node3 = _contact.AddNode("[ns:Node]Collection", true);

            // TODO: REMOVE THIS.
            // There's a bug in the underlying Contact implemenations where this won't work
            // without reloading the contact...
            _contact.Save("ArrayNode.contact");
            _contact.Dispose();
            _contact = new Contact("ArrayNode.contact");

            // Walk through the properties and make sure the expected values are present on the appropriate nodes
            // And the deleted values are not present.
            KeyValuePair<string, string>[] expectToFind =
            {
                new KeyValuePair<string, string>(node1, ""),
                new KeyValuePair<string, string>(node2, ""),
                // The order of these two isn't guaranteed... and actually it's the opposite of what you'd expect.
                // This order is different than if these aren't simple extensions (see the analagous NameCollection
                // test in the InteropTests), but the order doesn't matter so long as it's consistent.
                new KeyValuePair<string, string>(node2 + "/Second", "Fourth Value"),
                new KeyValuePair<string, string>(node2 + "/First", "Third Value"),
                new KeyValuePair<string, string>(node3, ""),
            };

            int index = 0;
            foreach(ContactProperty property in _contact.GetPropertyCollection("[ns]Collection", null, false))
            {
                KeyValuePair<string, string> expected = expectToFind[index];
                ++index;

                Assert.AreEqual(expected.Key, property.Name);
                Assert.AreEqual(_contact.GetStringProperty(property.Name), expected.Value);
            }
            Assert.AreEqual(index, expectToFind.Length);
        }

        [TestMethod]
        public void GetLevel2PropertyCollection()
        {
            string node = _contact.AddNode(PropertyNames.EmailAddressCollection, true);
            _contact.SetStringProperty(node + PropertyNames.Address, "fyodor@dostoevsky.com");

            // Shouldn't need to move it before the exception will get thrown.
            UTVerify.ExpectException<ArgumentException>(() => _contact.GetPropertyCollection(node, null, false));
        }

        [TestMethod]
        public void LabeledNodeTest()
        {
            string node1 = _contact.AddNode(PropertyNames.NameCollection, true);
            ILabelCollection labels = _contact.GetLabelCollection(node1);

            Assert.AreEqual(0, labels.Count);
            labels.Add(PropertyLabels.Business);
            Assert.AreEqual(1, labels.Count);
            // Adding the property again shouldn't actualy modify the collection.
            labels.Add(PropertyLabels.Business);
            Assert.AreEqual(1, labels.Count);
            Assert.IsTrue(labels.Contains(PropertyLabels.Business.ToLower()));

            Assert.IsFalse(labels.Add(PropertyLabels.Business.ToLower()));

            // Removing this node should just cause the labelCollection to empty, but not invalidate it.
            _contact.RemoveNode(node1);
            Assert.AreEqual(0, labels.Count);
            labels.Add(PropertyLabels.Personal);
            Assert.AreEqual(1, labels.Count);

            // Prepending labels should change the property name, appending them shouldn't.
            Assert.AreEqual(labels.PropertyName, node1);
            string nodeBefore = _contact.AddNode(PropertyNames.NameCollection, false);
            Assert.AreNotEqual(labels.PropertyName, node1);
            node1 = labels.PropertyName;
            string nodeAfter = _contact.AddNode(PropertyNames.NameCollection, true);
            Assert.AreEqual(labels.PropertyName, node1);

            // It should still map to the node that has the labels set.
            Assert.IsTrue(labels.Contains(PropertyLabels.Personal));
            labels.Add(PropertyLabels.Preferred);
            Assert.IsTrue(labels.Remove(PropertyLabels.Personal.ToUpper()));
            // This was already removed...
            Assert.IsFalse(labels.Remove(PropertyLabels.Personal));

            string[] copy = new string[5];
            labels.CopyTo(copy, 2);
            Assert.IsTrue(labels.Contains(PropertyLabels.Preferred));
            Assert.AreEqual(1, labels.Count);
            Assert.AreEqual(copy[2], PropertyLabels.Preferred);
            labels.Clear();

            IEnumerator iter = ((IEnumerable)labels).GetEnumerator();
            Assert.IsFalse(iter.MoveNext());
        }

        [TestMethod]
        public void ValidateNullNodeLabelCollectionTest()
        {
            UTVerify.ExpectException<ArgumentNullException>(() => _contact.GetLabelCollection(null));
        }

        [TestMethod]
        public void ValidateNonNodeLabelCollectionTest()
        {
            UTVerify.ExpectException<SchemaException>(() => _contact.GetLabelCollection(PropertyNames.Notes));
        }

        [TestMethod]
        public void ValidateBadNodeLabelCollectionTest()
        {
            UTVerify.ExpectException<SchemaException>(() => _contact.GetLabelCollection("[ns]Collection/Node[Blue]"));
        }

        [TestMethod]
        public void ValidateBadNodeLabelCollectionTest2()
        {
            UTVerify.ExpectException<SchemaException>(
                () => _contact.GetLabelCollection(string.Format(PropertyNames.NameFamilyNameFormat, "1")));
        }

        [TestMethod]
        public void ValidateNonExistentNodeLabelCollectionTest()
        {
            UTVerify.ExpectException<PropertyNotFoundException>(() => 
                _contact.GetLabelCollection(PropertyNames.NameCollection + PropertyNames.NameArrayNode + "[1]"));
        }

        [TestMethod]
        public void GetDefaultEmptyNamesTest()
        {
            Assert.AreEqual(default(Name), _contact.Names.Default);
        }

        [TestMethod]
        public void GetDefaultPreferredNamesTest()
        {
            Name businessName = new Name("BusinessName");
            Name personalName = new Name("PersonalName");

            ILabeledPropertyCollection<Name> names = _contact.Names;
            names[PropertyLabels.Business] = businessName;
            Assert.AreEqual(businessName, names.Default);
            names[PropertyLabels.Personal] = personalName;
            Assert.AreEqual(businessName, names.Default);
            names.GetLabelsAt(names.IndexOfLabels(PropertyLabels.Personal)).Add(PropertyLabels.Preferred);
            Assert.AreEqual(personalName, names.Default);
        }

        [TestMethod]
        public void GetAndSetDatesTest()
        {
            DateTime dt = new DateTime(2006, 10, 31);
            // No settable top-level date properties, so working with a simple extension.
            _contact.SetDateProperty("[Third]Halloween", dt);
            DateTime? result = _contact.GetDateProperty("[Third]Halloween");
            Assert.IsNotNull(result);
            Assert.AreEqual(dt, result);

            _changedProperty = null;
            _contact.PropertyChanged += _OnPropertyChanged;
            result = _contact.GetDateProperty("[Fourth]AllSaintsDay");
            Assert.IsNull(result);
            Assert.IsNull(_changedProperty);
            dt.AddDays(1);
            _contact.SetDateProperty("[Fourth]AllSaintsDay", dt);
            Assert.AreEqual("[Fourth]AllSaintsDay", _changedProperty);
            result = _contact.GetDateProperty("[Fourth]AllSaintsDay");
            Assert.AreEqual(dt, result);
        }

        [TestMethod]
        public void SetStringNullOrEmptyTest()
        {
            UTVerify.ExpectException<FormatException>(() => _contact.SetStringProperty("[Third]Foo", ""));
        }

        [TestMethod]
        public void UseDisposedContactException()
        {
            ILabeledPropertyCollection<Name> nameCollection = _contact.Names;
            _contact.Dispose();
            // Disposing a second time should still be okay.
            _contact.Dispose();

            // Should throw because the object's been disposed.
            Name n;
            UTVerify.ExpectException<ObjectDisposedException>(() => n = nameCollection.Default);
        }

        [
            TestMethod,
            Description("Contacts restrict ContactIDs to registry-style GUID strings."),
        ]
        public void SetInvalidContactIdTest()
        {
            string id = _contact.GetStringProperty(string.Format(PropertyNames.ContactIdValueFormat, 1));
            Assert.IsNotNull(id);
            // It should also be parsable as a guid, or else this will throw an exception.
            Guid guid = new Guid(id);
            UTVerify.ExpectException<SchemaException>(() => 
                _contact.SetStringProperty(string.Format(PropertyNames.ContactIdValueFormat, 1), "Not a Guid"));
        }

        [TestMethod]
        public void GetAndSetContactIdsTest()
        {
            // Contact IDs are special in that zero aren't allowed.
            // The default ContactID should be the first, even though there is no preferred label.
            int i = _contact.ContactIds.DefaultIndex;
            if (i != 0)
            {
                Assert.Inconclusive("This test can be rewritten without this requirement.");
            }
            Assert.IsFalse(_contact.ContactIds.GetLabelsAt(0).Contains(PropertyLabels.Preferred));
            Guid guid = Guid.NewGuid();
            _contact.ContactIds.Add(guid);
            Assert.AreEqual(guid, _contact.ContactIds[1]);
            _contact.ContactIds.RemoveAt(0);
            // Even though the guid has been removed, it should still be default because the one
            // with the value doesn't have a label set.
            // It doesn't seem right to modify unrelated label sets in this scenario.
            Assert.AreEqual(0, _contact.ContactIds.DefaultIndex);
            _contact.ContactIds.Default = Guid.NewGuid();
            // Now that it's been set, the default should still be 0 but it should also have the preferred label
            Assert.AreEqual(0, _contact.ContactIds.DefaultIndex);
            Assert.IsTrue(_contact.ContactIds.GetLabelsAt(0).Contains(PropertyLabels.Preferred));
            // Setting the value to null will leave the label present.
            _contact.ContactIds[0] = null;
            Assert.AreEqual("", _contact.GetStringProperty(string.Format(PropertyNames.ContactIdValueFormat, 1)));
            Assert.AreEqual(null, _contact.ContactIds[0]);
            Assert.IsTrue(_contact.ContactIds.GetLabelsAt(0).Contains(PropertyLabels.Preferred));
            // Removing the property won't leave the labels.
            _contact.ContactIds.Remove(null);
            Assert.IsFalse(_contact.ContactIds.GetLabelsAt(0).Contains(PropertyLabels.Preferred));
        }

        [TestMethod]
        public void GetAndSetPositionTest()
        {
            Position pos = new Position("Org", "Role", "MS", "Windows Live", "12345", "Gofer", "Prof");
            _contact.Positions.Default = pos;
            _contact.Positions.GetLabelsAt(_contact.Positions.DefaultIndex).Add(PropertyLabels.Personal);
            _contact.Positions.GetLabelsAt(_contact.Positions.DefaultIndex).Add(PropertyLabels.Business);
            Assert.AreEqual(_contact.Positions[PropertyLabels.Business], _contact.Positions[PropertyLabels.Personal]);
            Assert.AreEqual(-1, _contact.Positions.IndexOf(default(Position)));
        }

        [TestMethod]
        public void ThrowConflictingChangesTest()
        {
            ContactManager manager = new ContactManager();
            Contact contact = manager.CreateContact();
            string path = null;
            try
            {
                contact.Names.Default = new Name("Conflictable");
                contact.CommitChanges();
                path = contact.Path;

                contact.PhoneNumbers.Default = new PhoneNumber("555-1212");
                contact.Save(contact.Path);
                UTVerify.ExpectException<IncompatibleChangesException>(() => contact.CommitChanges());
            }
            finally
            {
                Utility.SafeDeleteFile(path);
            }
        }

        [TestMethod]
        public void ThrowMissingFileTest()
        {
            ContactManager manager = new ContactManager();
            Contact contact = manager.CreateContact();
            contact.Names.Default = new Name("Conflictable");
            contact.CommitChanges();
            contact.PhoneNumbers.Default = new PhoneNumber("555-1212");
            File.Delete(contact.Path);
            UTVerify.ExpectException<IncompatibleChangesException>(() => contact.CommitChanges());
        }

        [TestMethod]
        public void SwallowConflictingChangesTest()
        {
            ContactManager manager = new ContactManager();
            Contact contact = manager.CreateContact();
            contact.Names.Default = new Name("Conflictable", "Swallow", null, null, null, null, null, null, null, null);
            contact.CommitChanges();

            contact.PhoneNumbers.Default = new PhoneNumber("555-1212");
            contact.Save(contact.Path);
            contact.CommitChanges(ContactCommitOptions.IgnoreChangeConflicts);

            File.Delete(contact.Path);
            contact.CommitChanges(ContactCommitOptions.IgnoreChangeConflicts);
            Assert.IsTrue(File.Exists(contact.Path));
            File.Delete(contact.Path);
        }

        [TestMethod]
        public void SaveAndRenameTest()
        {
            string startName = Path.Combine(TestUtil.GetContactsFolder(), "First Name.contact");
            string endName = Path.Combine(TestUtil.GetContactsFolder(), "Second Name.contact");

            // Ensure these aren't present, because this doesn't do the regex comparison
            File.Delete(startName);
            File.Delete(endName);

            _contact.Names.Default = new Name("First Name");
            _contact.Save(startName);
            
            _contact.Dispose();
            _contact = new Contact(startName);
            Assert.AreEqual(_contact.Path, startName);
            _contact.Names.Default = new Name("Second Name");
            _contact.CommitChanges(ContactCommitOptions.SyncStorageWithFormattedName);
            Assert.AreEqual(_contact.Path, endName);
            File.Delete(_contact.Path);
        }

        [TestMethod]
        public void EnsureLabeledCollectionInsertFailsTest()
        {
            UTVerify.ExpectException<NotSupportedException>(() => _contact.Names.Insert(0, default(Name)));
        }

        [TestMethod]
        public void SetBinaryPropertiesEmptyStreamTest()
        {
            string nodeName1 = _contact.AddNode("[simple:Photo]Binary", true);
            UTVerify.ExpectException<ArgumentException>(() => _contact.SetBinaryProperty(nodeName1 + PropertyNames.Value, new MemoryStream(), "Blank"));
        }

        [TestMethod]
        public void SetPropertyOnNodeTest()
        {
            string nodeName1 = _contact.AddNode("[simple:Node]Collection", true);
            // Setting the property on the node name should throw a schema exception.
            UTVerify.ExpectException<SchemaException>(() => _contact.SetStringProperty(nodeName1, "Foo"));
        }

        [TestMethod]
        public void SetBinaryPropertiesTest()
        {
            string nodeName1 = _contact.AddNode("[simple:Photo]Binary", true);
            Stream memStream = new MemoryStream();
            memStream.WriteByte(13);
            _contact.SetBinaryProperty(nodeName1 + PropertyNames.Value, memStream, "AlmostBlank");
            _contact.GetLabelCollection(nodeName1).Add(PropertyLabels.Business);
            string nodeName2 = _contact.AddNode("[simple:Photo]Binary", true);
            MemoryStream imgStream = new MemoryStream();
            Resources.IcarusFalling.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
            _contact.SetBinaryProperty(nodeName2 + PropertyNames.Value, imgStream, "image/png");
            _contact.GetLabelCollection(nodeName2).AddRange(PropertyLabels.Personal, PropertyLabels.Preferred);
            StringBuilder sb = new StringBuilder();
            Stream results = _contact.GetBinaryProperty(nodeName1 + PropertyNames.Value, sb);
            Assert.IsTrue(Utility.AreStreamsEqual(results, memStream));
            Assert.AreEqual(sb.ToString(), "AlmostBlank");
            _contact.GetStringProperty(nodeName1);
        }

        [TestMethod]
        public void CommitNewContactTest()
        {
            _contact.Names.Default = new Name("Fileless");
            UTVerify.ExpectException<FileNotFoundException>(() => _contact.CommitChanges());
        }

        [TestMethod]
        public void SetDateInvalidNodeTest()
        {
            UTVerify.ExpectException<SchemaException>(() => _contact.SetDateProperty(PropertyNames.DateCollection + PropertyNames.DateArrayNode + "[1]", DateTime.Now));
        }

        [TestMethod]
        public void SetBinaryInvalidNodeTest()
        {
            using (MemoryStream mstm = new MemoryStream())
            {
                UTVerify.ExpectException<SchemaException>(() => _contact.SetBinaryProperty(PropertyNames.PhotoCollection + PropertyNames.PhotoArrayNode + "[1]", mstm, null));
            }
        }

        [TestMethod]
        public void SaveNewContactToStream()
        {
            using (MemoryStream mstm = new MemoryStream())
            {
                _contact.Save(mstm);
                using (Contact contact2 = new Contact(mstm, ContactTypes.None))
                {
                    Assert.AreEqual(_contact.Id, contact2.Id);
                }
            }
        }

        [TestMethod]
        public void GetNonExistentBinaryProperty()
        {
            StringBuilder sbType = new StringBuilder();
            Stream stm = _contact.GetBinaryProperty(string.Format(PropertyNames.PhotoValueFormat, 1), sbType);
            Assert.IsNull(stm);
            Assert.AreEqual("", sbType.ToString());
        }

        [TestMethod]
        public void RemoveNonExistentArrayNode()
        {
            string node = _contact.AddNode(PropertyNames.PhotoCollection, true);
            StringAssert.Contains(node, "[1]");
            Assert.AreEqual(1, _contact.Photos.Count);

            _contact.RemoveNode(node.Replace("[1]", "[2]"));
            Assert.AreEqual(1, _contact.Photos.Count);
        }

        [TestMethod]
        public void SetAndRemoveDates()
        {
            _contact.Dates.Clear();
            DateTime vistaConsumerReleaseDate = new DateTime(2007, 1, 31);
            DateTime vistaBusinessReleaseDate = new DateTime(2006, 11, 30);

            _contact.Dates[DateLabels.Birthday, PropertyLabels.Business] = vistaBusinessReleaseDate;
            _contact.Dates.Add(vistaConsumerReleaseDate, DateLabels.Birthday, PropertyLabels.Personal, PropertyLabels.Preferred);
            Assert.AreEqual(2, _contact.Dates.Count);
            Assert.AreEqual(vistaConsumerReleaseDate, _contact.Dates[DateLabels.Birthday].Value);
            _contact.Dates.Remove(vistaConsumerReleaseDate);
            Assert.AreEqual(vistaBusinessReleaseDate, _contact.Dates[DateLabels.Birthday].Value);
            _contact.Dates[DateLabels.Birthday] = null;
            Assert.IsFalse(_contact.Dates[DateLabels.Birthday].HasValue);
        }

        [TestMethod]
        public void SetAndRemoveAddresses()
        {
            PhysicalAddress address = new PhysicalAddress(null, "1 Microsoft Way", "Redmond", "WA", "98052", "United States", null, null);
            _contact.Addresses[PropertyLabels.Business] = address;
            Assert.AreEqual(_contact.Addresses[0], address);
            Assert.AreEqual(_contact.Addresses[PropertyLabels.Business].State, "WA");
            _contact.Addresses.Clear();
            Assert.AreNotEqual(_contact.Addresses[PropertyLabels.Business].State, "WA");
        }

        [TestMethod]
        public void SetAndRemovePhoneNumbers()
        {
            PhoneNumber number = new PhoneNumber("555-1212");
            _contact.PhoneNumbers[PropertyLabels.Business] = number;
            Assert.AreEqual(_contact.PhoneNumbers[0], number);
            Assert.AreEqual(_contact.PhoneNumbers[PropertyLabels.Business].Alternate, "");
            _contact.PhoneNumbers[0] = default(PhoneNumber);
            Assert.AreNotEqual(_contact.PhoneNumbers[0], number);

            // Note that the voice label is required for the phone number to show up in the Vista UI.
            number = new PhoneNumber("(123) 456 - 7890", "555-1212");
            _contact.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Voice] = number;
            Assert.AreEqual(_contact.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Voice].Number, number.Number);
            Assert.AreEqual(_contact.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Voice].Alternate, number.Alternate);
        }

        [TestMethod]
        public void LoadFromFile()
        {
            using (Contact contact = new Contact(Path.Combine(Environment.CurrentDirectory, _contactFileName)))
            {
                Assert.AreEqual("Fyodor Karamazov", contact.People[PersonLabels.Parent].Name);
                Assert.AreEqual(-1, contact.People.IndexOf(new Person("Susan")));
                string [] brothers = new string[] { "Dmitri", "Ivan" };
                foreach (ContactProperty prop in contact.GetPropertyCollection(PropertyNames.PersonCollection, new string[] { PersonLabels.Sibling }, true))
                {
                    string name = contact.GetStringProperty(prop.Name + PropertyNames.FormattedName);
                    bool found = false;
                    for (int i = 0; i < brothers.Length; ++i)
                    {
                        if (name.StartsWith(brothers[i]))
                        {
                            brothers[i] = "XXXXXXXX";
                            found = true;
                            break;
                        }
                    }
                    Assert.IsTrue(found);
                }
            }
        }

        [
            TestMethod,
            Description("Trying to load a contact from a non-existent file should throw the appropriate exception."),
        ]
        public void LoadContactFromMissingFile()
        {
            UTVerify.ExpectException<FileNotFoundException>(() => new Contact("This file doesnt exist.Contact"));
        }

        [
            TestMethod,
            Description("Trying to load a contact from a non-XML stream should throw the appropriate exception."),
        ]
        public void LoadContactFromInvalidStream()
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                memstream.Write(bytes, 0, bytes.Length);
                UTVerify.ExpectException<InvalidDataException>(() => new Contact(memstream, ContactTypes.None));
            }
        }

        [
            TestMethod,
            Description("Trying to load a contact from a non-XML stream should throw the appropriate exception."),
        ]
        public void LoadContactFromInvalidFile()
        {
            string path = "SomeFileThatDoesntExist.Contact";
            try
            {
                using (FileStream fstream = new FileStream(path, FileMode.CreateNew))
                {
                    byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                    fstream.Write(bytes, 0, bytes.Length);
                    fstream.Close();
                    UTVerify.ExpectException<InvalidDataException>(() => new Contact(path));
                }
            }
            finally
            {
                Utility.SafeDeleteFile(path);
            }
        }

        [TestMethod]
        public void GetAndSetUrls()
        {
            Uri url = new Uri(@"http://www.test.com");
            _contact.Urls.Add(url, PropertyLabels.Personal);
            Assert.AreEqual(url, _contact.Urls[PropertyLabels.Personal]);

            // try it again with URLs that aren't "Well formed", e.g. file system URLs.
            string unwell = "C:\\test\\foo.txt";
            Assert.IsFalse(Uri.IsWellFormedUriString(unwell, UriKind.Absolute));

            url = new Uri(unwell);
            _contact.Urls[PropertyLabels.Personal] = url;
            Assert.AreEqual(url, _contact.Urls[PropertyLabels.Personal]);

            // This should still work with Urls that are explicitly set through SetString

            // Clear all the URLS (I know there's one) and then set it to the native unwell URL.
            _contact.Urls.Clear();
            _contact.SetStringProperty(string.Format(PropertyNames.UrlValueFormat, 1), unwell);
            // Retrieving this URL from the schematized type should work.
            Assert.AreEqual(url, _contact.Urls[0]);

            _contact.Urls[0] = null;
            Assert.AreEqual(default(Uri), _contact.Urls[0]);
        }

        [
            TestMethod,
            Description("Verify that the UserTile property on a contact isn't null when there's not a labeled photo.")
        ]
        public void EmptyUserTileIsNotNull()
        {
            Assert.AreEqual<Photo>(default(Photo), _contact.Photos[PhotoLabels.UserTile]);
            ImageSource defaultUserTileImage = UserTile.GetFramedPhoto(default(Photo), 96);
            Assert.IsNotNull(defaultUserTileImage);
            Assert.IsTrue(Utility.AreImageSourcesEqual(defaultUserTileImage, _contact.UserTile.Image));
        }

        [
            TestMethod,
            Description("Verify that the UserTile property on a contact picks up the correctly labeled photo.")
        ]
        public void VerifyUserTileMatchesLabel()
        {
            // Assuming that the contact isn't starting off with a user tile.
            Assert.AreEqual<Photo>(default(Photo), _contact.Photos[PhotoLabels.UserTile]);

            // Add an image to the contact.
            using (MemoryStream imgStream = new MemoryStream())
            {
                Resources.IcarusFalling.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
                Photo icarus = new Photo(imgStream, "image/png");
                ImageSource icarusTile = UserTile.GetFramedPhoto(icarus, 96);
                _contact.Photos.Add(icarus);

                Assert.IsFalse(
                    Utility.AreImageSourcesEqual(
                        icarusTile,
                        _contact.UserTile.Image));
                Assert.IsTrue(
                    Utility.AreImageSourcesEqual(
                        icarusTile,
                        UserTile.GetFramedPhoto(_contact.Photos.Default, 96)));

                _contact.Photos.GetLabelsAt(_contact.Photos.DefaultIndex).Add(PhotoLabels.UserTile);
                Assert.IsTrue(
                    Utility.AreImageSourcesEqual(
                        icarusTile,
                        _contact.UserTile.Image));
            }
        }

        [
            TestMethod,
            Description("Attributes such as property type should be queryable even after a property has been deleted.")
        ]
        public void GetPropertyAttributesForDeletedProperty()
        {
            // Should work for schematized types,
            _contact.Dates.Add(DateTime.UtcNow, "Today");
            // As well as simple extensions.
            _contact.SetDateProperty("[Da]Te", DateTime.UtcNow);

            foreach (string propName in new[] { _contact.Dates.GetNameAt("Today") + PropertyNames.Value, "[Da]Te" })
            {
                ContactProperty originalProps = _contact.GetAttributes(propName);

                Assert.AreEqual(ContactPropertyType.DateTime, originalProps.PropertyType);
                Assert.IsFalse(originalProps.Removed);

                _contact.RemoveProperty(propName);

                ContactProperty modifiedProps = _contact.GetAttributes(propName);

                Assert.IsTrue(modifiedProps.Removed);
                Assert.AreEqual(ContactPropertyType.DateTime, modifiedProps.PropertyType);
            }
        }

        [
            TestMethod,
            Description("UserTile gotten from a URL should be the same as if loaded from a stream.")
        ]
        public void FindUserTileFromPhotoUrl()
        { 
            string imageUrl = "wljewel.gif";
            try
            {
                using (MemoryStream imgStream = new MemoryStream())
                {
                    Resources.WindowsLiveJewel.Save(imgStream, System.Drawing.Imaging.ImageFormat.Gif);
                    Photo streamPhoto = new Photo(imgStream, "image/gif");

                    Resources.WindowsLiveJewel.Save(imageUrl, System.Drawing.Imaging.ImageFormat.Gif);

                    // Bug in Windows Contacts.
                    // Windows Contacts doesn't like "file://" for representing user tile URLs.
                    // Since I'm using Windows Contacts for generating the user tile I need to
                    // bypass the Photo struct.
                    _contact.Photos[PhotoLabels.UserTile] = new Photo();
                    int userTileIndex = _contact.Photos.IndexOfLabels(PhotoLabels.UserTile);
                    _contact.SetStringProperty(string.Format(PropertyNames.PhotoUrlFormat, userTileIndex + 1), Path.GetFullPath(imageUrl));

                    Assert.IsTrue(
                        Utility.AreImageSourcesEqual(
                            UserTile.GetFramedPhoto(streamPhoto, 96),
                            _contact.UserTile.Image));
                }
            }
            finally
            {
                Utility.SafeDeleteFile(imageUrl);
            }
        }

        [
            TestMethod,
            Description("All types of URLs should work for usertiles, e.g. \"file://\".")
        ]
        public void FindUserTileFromFileUrl()
        {
            string imageUrl = Path.Combine(Environment.CurrentDirectory, "wljewel.gif");

            try
            {
                using (FileStream imgStream = File.Create(imageUrl))
                {
                    Resources.WindowsLiveJewel.Save(imgStream, System.Drawing.Imaging.ImageFormat.Gif);
                    _contact.Photos["wab:Reference"] = new Photo(imgStream, "image/gif");
                }
                
                _contact.Photos[PhotoLabels.UserTile] = new Photo(new Uri(imageUrl, UriKind.Absolute));

                Assert.IsTrue(
                    Utility.AreImageSourcesEqual(
                        UserTile.GetFramedPhoto(_contact.Photos["wab:Reference"], 96),
                        _contact.UserTile.Image));
            }
            finally
            {
                Utility.SafeDeleteFile(imageUrl);
            }
        }

        [TestMethod]
        public void SetTypelessBinaryProperty()
        {
            // Contact should silently swallow this.
            _contact.SetBinaryProperty("[Bin]Ary", new MemoryStream(new byte[] { 1, 2, 3 }), null);
            StringBuilder sbType = new StringBuilder();
            _contact.GetBinaryProperty("[Bin]Ary", sbType);
            Assert.AreEqual("binary", sbType.ToString());
        }
        
        [TestMethod]
        public void SetRelativeUri()
        {
            string relativeUri = "www.microsoft.com";
            Uri uriSet = new Uri(relativeUri, UriKind.RelativeOrAbsolute);
            Assert.IsFalse(uriSet.IsAbsoluteUri);

            _contact.Urls[PropertyLabels.Preferred] = uriSet;

            Uri uriGet = _contact.Urls[PropertyLabels.Preferred];
            Assert.IsNotNull(uriGet);
            Assert.IsFalse(uriGet.IsAbsoluteUri);
            Assert.AreEqual(uriSet, uriGet);
        }

        [TestMethod]
        public void SetAbsoluteUri()
        {
            string absoluteUri = "http://www.microsoft.com";
            Uri uriSet = new Uri(absoluteUri, UriKind.RelativeOrAbsolute);
            Assert.IsTrue(uriSet.IsAbsoluteUri);

            _contact.Urls[PropertyLabels.Preferred] = uriSet;

            Uri uriGet = _contact.Urls[PropertyLabels.Preferred];
            Assert.IsNotNull(uriGet);
            Assert.IsTrue(uriGet.IsAbsoluteUri);
            Assert.AreEqual(uriSet, uriGet);
        }

        [TestMethod]
        public void GetPropertyAttributes()
        {
            // Get property data for an element that should certainly exist.
            ContactProperty metadata = _contact.GetAttributes(string.Format(PropertyNames.ContactIdValueFormat, "1"));
            Assert.IsFalse(metadata.Removed);
            Assert.IsTrue(metadata.Version >= 1);
            Assert.AreEqual(default(Guid), metadata.ElementId);

            metadata = _contact.GetAttributes(PropertyNames.ContactIdCollection + PropertyNames.ContactIdArrayNode + "[1]");
            Assert.IsFalse(metadata.Removed);
            Assert.IsTrue(metadata.Version >= 1);
            Assert.AreNotEqual(default(Guid), metadata.ElementId);

            // Get the property data for an element that should probably not exist.
            UTVerify.ExpectException<PropertyNotFoundException>(
                () => _contact.GetAttributes(string.Format(PropertyNames.ContactIdValueFormat, "1000")));

            // Getting the data from the property collection should be the same as GetAttribute.
            string nodeName = _contact.AddNode(PropertyNames.PositionCollection, true);
            var collProps = from prop in _contact.GetPropertyCollection()
                            where prop.Name == nodeName
                            select prop;
            metadata = default(ContactProperty);
            foreach (ContactProperty p in collProps)
            {
                metadata = p;
                break;
            }
            Assert.AreNotEqual(default(ContactProperty), metadata);
            Assert.AreEqual(metadata, _contact.GetAttributes(nodeName));

            string propName = nodeName + PropertyNames.Organization;
            // Set the property a few times to rev the version number.
            _contact.SetStringProperty(propName, "Foo");
            _contact.SetStringProperty(propName, "Foo1");
            _contact.SetStringProperty(propName, "Bar");
            _contact.SetStringProperty(propName, "Foo");

            collProps = from prop in _contact.GetPropertyCollection()
                        where prop.Name == propName
                        select prop;
            metadata = default(ContactProperty);
            foreach (ContactProperty p in collProps)
            {
                metadata = p;
                break;
            }
            Assert.AreNotEqual(default(ContactProperty), metadata);
            Assert.AreEqual(metadata, _contact.GetAttributes(propName));
            Assert.AreEqual(4, metadata.Version);
        }

        [TestMethod]
        public void GetDeletedPropertyAttributes()
        {
            Assert.IsTrue(string.IsNullOrEmpty(_contact.Notes));

            UTVerify.ExpectException<PropertyNotFoundException>(
                () => _contact.GetAttributes(PropertyNames.Notes));

            _contact.Notes = "Who's who in the 20th century.";

            ContactProperty metadata = _contact.GetAttributes(PropertyNames.Notes);
            Assert.AreEqual(PropertyNames.Notes, metadata.Name);
            Assert.AreEqual(ContactPropertyType.String, metadata.PropertyType);
            Assert.AreEqual(default(Guid), metadata.ElementId);
            Assert.IsTrue(metadata.ModificationDate <= DateTime.UtcNow);
            Assert.IsFalse(metadata.Removed);

            _contact.Notes = null;

            metadata = _contact.GetAttributes(PropertyNames.Notes);
            Assert.AreEqual(PropertyNames.Notes, metadata.Name);
            Assert.AreEqual(ContactPropertyType.String, metadata.PropertyType);
            Assert.AreEqual(default(Guid), metadata.ElementId);
            Assert.IsTrue(metadata.ModificationDate <= DateTime.UtcNow);
            Assert.IsTrue(metadata.Removed);
        }
    }
}
