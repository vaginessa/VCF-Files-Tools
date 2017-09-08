/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

#if NEED_TO_MIGRATE_TO_CONTACT_TESTS
namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.Drawing;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Communications.Contacts.Interop;
    using System.Runtime.InteropServices.ComTypes;
    using System.Runtime.InteropServices;
    using System.IO;
    using System.Threading;
    using Standard;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    
    [TestClass]
    public class InteropTests
    {
        private const string contactFileName = "Alexei Karamazov.contact";

        private ContactRcw _contact = null;

        [ClassInitialize]
        public static void InitializeInteropTests(TestContext testContext)
        {
            // Write the test contact file from the test's resources.
            StreamWriter sw = File.CreateText(contactFileName);
            sw.Write(Resources.Alexei_Karamazov);
            sw.Close();
        }

        [ClassCleanup]
        public static void TeardownInteropTests()
        {
            File.Delete(contactFileName);
        }

        [TestInitialize]
        public void PreTestInitialize()
        {
            _contact = new ContactRcw();
        }

        [TestCleanup]
        public void PostTestCleanup()
        {
            if (null != _contact)
            {
                Marshal.ReleaseComObject(_contact);
                _contact = null;
            }
        }

        [TestMethod]
        public void DeleteArrayNodeTest()
        {
            // Initialize to empty data
            _contact.InitNew();

            // Create a couple nodes to work with.
            // (So long as appendNode=true, don't need to worry about the name changing under the test.)
            string node1;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node1).ThrowIfFailed();
            ContactUtil.SetString(_contact, node1 + PropertyNames.GivenName, "First Name").ThrowIfFailed();
            ContactUtil.SetString(_contact, node1 + PropertyNames.FamilyName, "First Last Name").ThrowIfFailed();
            ContactUtil.SetLabels(_contact, node1, new string[] { PropertyLabels.Business }).ThrowIfFailed();

            string node2;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node2).ThrowIfFailed();
            ContactUtil.SetString(_contact, node2 + PropertyNames.GivenName, "Second Name").ThrowIfFailed();
            ContactUtil.SetString(_contact, node2 + PropertyNames.FamilyName, "Last Name").ThrowIfFailed();

            // Now have two name nodes: both have some data, one has a label.
            
            // Deleting the first node shouldn't affect the second.
            ContactUtil.DeleteArrayNode(_contact, node1).ThrowIfFailed();

            string node3;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node3).ThrowIfFailed();

            IContactPropertyCollection propertyCollection = null;
            // Walk through the properties and make sure the expected values are present on the appropriate nodes
            // And the deleted values are not present.
            KeyValuePair<string, string>[] expectToFind =
            {
                new KeyValuePair<string, string>(node1, null),
                new KeyValuePair<string, string>(node2, null),
                // The order of these two isn't guaranteed...
                new KeyValuePair<string, string>(node2 + PropertyNames.FamilyName, "Last Name"),
                new KeyValuePair<string, string>(node2 + PropertyNames.GivenName, "Second Name"),
                new KeyValuePair<string, string>(node3, null),
            };

            HRESULT hr = HRESULT.S_OK;
            try
            {
                ContactUtil.GetPropertyCollection(_contact, PropertyNames.NameCollection, null, false, out propertyCollection).ThrowIfFailed();

                foreach(KeyValuePair<string, string> expected in expectToFind)
                {
                    hr = propertyCollection.Next();
                    if (HRESULT.S_OK != hr)
                    {
                        break;
                    }

                    string propertyName;
                    ContactUtil.GetPropertyName(propertyCollection, out propertyName).ThrowIfFailed();
                    Assert.AreEqual<string>(expected.Key, propertyName);

                    string propertyValue;
                    hr = ContactUtil.GetString(_contact, propertyName, false, out propertyValue);
                    if (null == expected.Value)
                    {
                        Assert.AreEqual<HRESULT>(HRESULT.S_FALSE, hr);
                    }
                    else
                    {
                        Assert.AreEqual<HRESULT>(HRESULT.S_OK, hr);
                        Assert.AreEqual<string>(expected.Value, propertyValue);
                    }
                }
                
                // Should have hit the last property in this collection.
                hr = propertyCollection.Next();
                Assert.AreEqual<HRESULT>(HRESULT.S_FALSE, hr);
            }
            finally
            {
                Utility.SafeRelease(ref propertyCollection);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeleteArrayNodeNullContactTest()
        {
            ContactUtil.DeleteArrayNode(null, string.Format(PropertyNames.NameArrayNode + "[1]"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeleteArrayNodeNullNodetest()
        {
            // Uninitialized contact. Should fail for the null arg before anything else.
            ContactUtil.DeleteArrayNode(_contact, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeletePropertyNullContactTest()
        {
            ContactUtil.DeleteProperty(null, PropertyNames.Notes);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeletePropertyNullPropertyTest()
        {
            ContactUtil.DeleteProperty(_contact, null);
        }

        [TestMethod]
        public void GetLabeledNodeTest()
        {
            _contact.InitNew();

            string foundNode;
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetLabeledNode(_contact, PropertyNames.NameCollection, null, out foundNode));

            string node1;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node1).ThrowIfFailed();
            ContactUtil.SetLabels(_contact, node1, new string[] { PropertyLabels.Business }).ThrowIfFailed();

            string node2;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node2).ThrowIfFailed();
            ContactUtil.SetLabels(_contact, node2, new string[] { PropertyLabels.Business, PropertyLabels.Preferred }).ThrowIfFailed();

            string node3;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node3).ThrowIfFailed();
            ContactUtil.SetLabels(_contact, node3, new string[] { PropertyLabels.Business, PropertyLabels.Personal }).ThrowIfFailed();

            ContactUtil.GetLabeledNode(_contact, PropertyNames.NameCollection, new string[] { PropertyLabels.Business }, out foundNode).ThrowIfFailed();
            Assert.AreEqual(node2, foundNode);

            ContactUtil.GetLabeledNode(_contact, PropertyNames.NameCollection, new string[] { PropertyLabels.Personal }, out foundNode).ThrowIfFailed();
            Assert.AreEqual(node3, foundNode);

            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetLabeledNode(_contact, PropertyNames.NameCollection, new string[] { PropertyLabels.Personal, PropertyLabels.Preferred }, out foundNode));

            // Passing NULL in the labels is also an easy way to get the standard preferred node.
            ContactUtil.GetLabeledNode(_contact, PropertyNames.NameCollection, null, out foundNode).ThrowIfFailed();
            Assert.AreEqual(node2, foundNode);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetLabeledNodeNullContactTest()
        {
            string node;
            ContactUtil.GetLabeledNode(null, PropertyNames.NameCollection, null, out node);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetLabeledNodeNullNodeTest()
        {
            string node;
            ContactUtil.GetLabeledNode(_contact, null, null, out node);
        }

        [TestMethod]
        public void GetLabelsTest()
        {
            string[] sampleSingleLabel = new string[] { PropertyLabels.Preferred };
            string[] sampleMultiLabels = new string[] { PropertyLabels.Personal, PropertyLabels.Business };
            string[] sampleThirdPartyLabels = new string[] { PropertyLabels.Preferred, "test:label" };

            List<string> results = null;

            // Initialize the contact to empty data.
            _contact.InitNew();

            // New top level property.  This can't have labels.
            ContactUtil.SetString(_contact, PropertyNames.Notes, "these are some notes").ThrowIfFailed();

            Assert.AreEqual<HRESULT>(Win32Error.ERROR_INVALID_DATATYPE, ContactUtil.SetLabels(_contact, PropertyNames.Notes, sampleSingleLabel));

            string nodeName = null;
            foreach (string[] sampleSet in new string[][] { sampleSingleLabel, sampleThirdPartyLabels, sampleMultiLabels })
            {
                ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out nodeName).ThrowIfFailed();
                // No data has yet been set on this node.
                ContactUtil.SetLabels(_contact, nodeName, sampleSet).ThrowIfFailed();
                ContactUtil.GetLabels(_contact, nodeName, out results).ThrowIfFailed();

                Assert.AreEqual(sampleSet.Length, results.Count);
                foreach (string s in sampleSet)
                {
                    Assert.IsTrue(results.Contains(s));
                }
            }

            // Set a value on this node to validate that the emptiness doesn't matter.
            ContactUtil.SetString(_contact, nodeName + PropertyNames.GivenName, "Fyodor").ThrowIfFailed();

            // Ensure that clearing the labels works.
            ContactUtil.DeleteLabels(_contact, nodeName).ThrowIfFailed();
            ContactUtil.GetLabels(_contact, nodeName, out results).ThrowIfFailed();
            Assert.AreEqual(0, results.Count);

            // Add multiple label sets into the node and validate results.
            // sampleSingleLabel and sampleMultiLabels don't have overlapping content.
            ContactUtil.SetLabels(_contact, nodeName, sampleSingleLabel).ThrowIfFailed();
            ContactUtil.SetLabels(_contact, nodeName, sampleMultiLabels).ThrowIfFailed();
            ContactUtil.GetLabels(_contact, nodeName, out results).ThrowIfFailed();

            Assert.AreEqual(sampleSingleLabel.Length + sampleMultiLabels.Length, results.Count);
            foreach (string s in sampleSingleLabel)
            {
                Assert.IsTrue(results.Contains(s));
            }
            foreach (string s in sampleMultiLabels)
            {
                Assert.IsTrue(results.Contains(s));
            }
        }

        [
            TestMethod,
            Ignore
        ]
        public void GetLabelsNonExistentPropertiesTest()
        {
            HRESULT hr = HRESULT.S_OK;
            _contact.InitNew();
            
            List<string> labels;
            hr = ContactUtil.GetLabels(_contact, PropertyNames.NameCollection + PropertyNames.NameArrayNode + "[1]", out labels);
            Assert.AreEqual((HRESULT)Win32Error.ERROR_PATH_NOT_FOUND, hr);
            Assert.IsNull(labels);

            hr = ContactUtil.GetLabels(_contact, PropertyNames.Notes, out labels);
            Assert.AreEqual((HRESULT)Win32Error.ERROR_INVALID_DATATYPE, hr);
            Assert.IsNull(labels);

            // Shouldn't matter whether there are notes there.
            ContactUtil.SetString(_contact, PropertyNames.Notes, "Some notes").ThrowIfFailed();
            
            hr = ContactUtil.GetLabels(_contact, PropertyNames.Notes, out labels);
            Assert.AreEqual((HRESULT)Win32Error.ERROR_INVALID_DATATYPE, hr);
            Assert.IsNull(labels);

            hr = ContactUtil.GetLabels(_contact, "[ns]Collection/Node[1]", out labels);
            Assert.AreEqual((HRESULT)Win32Error.ERROR_PATH_NOT_FOUND, hr);
            Assert.IsNull(labels);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeleteLabelsNullArgTest()
        {
            ContactUtil.DeleteLabels(null, PropertyNames.NameArrayNode + "[1]");
        }

        [TestMethod]
        public void GetDateTest()
        {
            DateTime beginning = DateTime.UtcNow;
            _contact.InitNew();
            DateTime created;
            ContactUtil.GetDate(_contact, PropertyNames.CreationDate, true, out created).ThrowIfFailed();
            
            // Want to make sure that timezones appear to be respected.
            // This test shouldn't have taken longer than a minute, so that's a fair range to check for.

            // The resolution here might be a bit fuzzy.  Negative values for the timespan are reasonable.
            TimeSpan ts = created - beginning;
            Assert.IsTrue(1 > Math.Abs(ts.TotalMinutes));

            string node;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.DateCollection, true, out node).ThrowIfFailed();

            string propName = node + PropertyNames.Value;
            ContactUtil.SetDate(_contact, propName, new DateTime(1980, 11, 30)).ThrowIfFailed();

            DateTime birthday;
            ContactUtil.GetDate(_contact, propName, true, out birthday).ThrowIfFailed();
            Assert.AreEqual<DateTime>(new DateTime(1980, 11, 30), birthday);

            ContactUtil.DeleteProperty(_contact, propName).ThrowIfFailed();

            // Verify that the property is no longer there, but that the api still knows it once was.
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetDate(_contact, propName, false, out birthday));
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetDate(_contact, propName, true, out birthday));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDateNullContactTest()
        {
            DateTime dt;
            ContactUtil.GetDate(null, string.Format(PropertyNames.DateValueFormat, 1), true, out dt);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDateNullPropertyTest()
        {
            DateTime dt;
            ContactUtil.GetDate(_contact, null, true, out dt);
        }

        [TestMethod]
        [ExpectedException(typeof(SchemaException))]
        public void SetLabelsEmptyStringsTest()
        {
            // try and set a label set that contains an empty string
            _contact.InitNew();

            string nodeName;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, false, out nodeName).ThrowIfFailed();
            // Don't expect this function to return.
            ContactUtil.SetLabels(_contact, nodeName, new string[]{ PropertyLabels.Preferred, "", PropertyLabels.Business });
        }

        [TestMethod]
        [ExpectedException(typeof(SchemaException))]
        public void SetLabelsNullStringsTest()
        {
            // try and set a label set that contains an empty string
            _contact.InitNew();

            string nodeName;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, false, out nodeName).ThrowIfFailed();
            // Don't expect this function to return.
            ContactUtil.SetLabels(_contact, nodeName, new string[] { PropertyLabels.Preferred, null });
        }

        [TestMethod]
        public void SetLabelsDuplicateStringsTest()
        {
            _contact.InitNew();

            string nodeName;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, false, out nodeName).ThrowIfFailed();
            ContactUtil.SetLabels(_contact, nodeName, new string[] { PropertyLabels.Personal, PropertyLabels.Personal, PropertyLabels.Personal }).ThrowIfFailed();
            List<string> labels;
            ContactUtil.GetLabels(_contact, nodeName, out labels).ThrowIfFailed();
            Assert.AreEqual(3, labels.Count);
            foreach (string s in labels)
            {
                Assert.AreEqual(s, PropertyLabels.Personal);
            }
        }

        [TestMethod]
        public void MarshalableLabelCollectionNullArgTest()
        {
            MarshalableLabelCollection mlc = new MarshalableLabelCollection(null);
            Assert.AreEqual<UInt32>(0, mlc.Count);
            Assert.AreEqual<IntPtr>(IntPtr.Zero, mlc.MarshaledLabels);
        }

        [TestMethod]
        public void MarshalableLabelCollectionZeroLengthTest()
        {
            MarshalableLabelCollection mlc = new MarshalableLabelCollection(new string[0]);
            Assert.AreEqual<uint>(0, mlc.Count);
            Assert.AreEqual(IntPtr.Zero, mlc.MarshaledLabels);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MarshalableDoubleNullStringBadSizeTest()
        {
            new MarshalableDoubleNullString(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateArrayNodeNullContactTest()
        {
            string s;
            ContactUtil.CreateArrayNode(null, "doesn't matter", false, out s);
        }

        [TestMethod]
        public void CreateArrayNodeAppendPrependTest()
        {
            const string nodeTemplate = "[ns:arrayNode]collection";
            const string nodePrefix = "[ns]collection/arrayNode[";
            string nodeNameBefore;
            string nodeNameAfter;
            int iBefore;
            int iAfter;

            _contact.InitNew();

            // Create a random third-party array node name
            ContactUtil.CreateArrayNode(_contact, nodeTemplate, false, out nodeNameBefore).ThrowIfFailed();
            Assert.IsTrue(nodeNameBefore.StartsWith(nodePrefix));

            // Append another node.
            iBefore = int.Parse(nodeNameBefore.Substring(nodePrefix.Length, nodeNameBefore.IndexOf(']', nodePrefix.Length) - nodePrefix.Length));
            ContactUtil.CreateArrayNode(_contact, nodeTemplate, true, out nodeNameAfter).ThrowIfFailed();
            
            iAfter = int.Parse(nodeNameAfter.Substring(nodePrefix.Length, nodeNameAfter.IndexOf(']', nodePrefix.Length) - nodePrefix.Length));
            Assert.IsTrue(iBefore < iAfter, "Contact array-nodes were created in the wrong relative order");

            ContactUtil.CreateArrayNode(_contact, nodeTemplate, false, out nodeNameBefore).ThrowIfFailed();
            
            iBefore = int.Parse(nodeNameBefore.Substring(nodePrefix.Length, nodeNameBefore.IndexOf(']', nodePrefix.Length) - nodePrefix.Length));
            Assert.IsTrue(iBefore < iAfter, "Contact array-nodes were created in the wrong relative order");
        }

        [TestMethod]
        public void CreateArrayNodeForceReallocTest()
        {
            const string nodeTemplate = "[ns:{0}]collection";
            const string nodePrefix = "[ns]collection/";
            string longNodeName = new string('A', (int)Win32Value.MAX_PATH);

            _contact.InitNew();
            string nodeName = string.Format(nodeTemplate, longNodeName);
            ContactUtil.CreateArrayNode(_contact, nodeName, false, out nodeName).ThrowIfFailed();
            Assert.IsTrue(nodeName.StartsWith(nodePrefix + longNodeName));
        }

        [TestMethod]
        public void CreateArrayNodeBadExtensionTest()
        {
            HRESULT hr = HRESULT.S_OK;
            _contact.InitNew();
            string nodeName;
            hr = ContactUtil.CreateArrayNode(_contact, "[ns:]collection", false, out nodeName);
            Assert.AreEqual(HRESULT.E_INVALIDARG, hr);
        }

        private struct DoubleNullStringTable
        {
            public string doubleNull;
            public string[] results;

            public DoubleNullStringTable(string doubleNull, params string[] results)
            {
                this.doubleNull = doubleNull;
                this.results = results;
            }
        }

        private DoubleNullStringTable[] samples = 
        {
            // Just get a single string.
            new DoubleNullStringTable("test\0", "test"),
            // Try again with multiple strings.
            new DoubleNullStringTable("string1\0string two\0", "string1", "string two"),
            // An empty list should also be returned if a valid string is passed.
            new DoubleNullStringTable("\0", new string[0]),
            // An empty single-null terminated string should be the same as a empty double-null string
            new DoubleNullStringTable("", new string[0]),
        };

        [TestMethod]
        public void ParseDoubleNullStringTest()
        {
            foreach (DoubleNullStringTable test in samples)
            {
                List<string> results = null;

                IntPtr psz = IntPtr.Zero;
                try
                {
                    psz = Marshal.StringToCoTaskMemUni(test.doubleNull);
                    results = ContactUtil.ParseDoubleNullString(psz);
                    Assert.AreEqual<int>(test.results.Length, results.Count, "Bad count for a double-null string.");

                    int i = 0;
                    foreach (string elt in test.results)
                    {
                        Assert.AreEqual<string>(elt, results[i], "Parsed string collection has bad values.");
                        ++i;
                    }
                }
                finally
                {
                    if (IntPtr.Zero != psz)
                    {
                        Marshal.FreeCoTaskMem(psz);
                        psz = IntPtr.Zero;
                    }
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ParseDoubleNullStringNullArgTest()
        {
            ContactUtil.ParseDoubleNullString(IntPtr.Zero);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetLabelsNullArgTest()
        {
            List<string> labels;
            ContactUtil.GetLabels(null, "whatever", out labels);
        }

        [TestMethod]
        public void GetLabelsForceRealloc()
        {
            string[] sourceLabels = new string[] { PropertyLabels.Business, "wab:" + new string('7', (int)Win32Value.MAX_PATH) };

            _contact.InitNew();

            string nodeName;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out nodeName).ThrowIfFailed();
            ContactUtil.SetLabels(_contact, nodeName, sourceLabels).ThrowIfFailed();

            List<string> labels;
            ContactUtil.GetLabels(_contact, nodeName, out labels).ThrowIfFailed();
            Assert.AreEqual(labels.Count, sourceLabels.Length);
            foreach (string s in sourceLabels)
            {
                Assert.IsTrue(labels.Contains(s));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetLabelsNullArgTest()
        {
            ContactUtil.SetLabels(null, "whatever", new string[] { PropertyLabels.Personal });
        }

        [TestMethod]
        public void GetPathFromUninitializedContactTest()
        {
            string path;
            Assert.AreEqual(HRESULT.E_UNEXPECTED, ContactUtil.GetPath(_contact, out path));
        }

        [TestMethod]
        public void GetPathFromNewContactTest()
        {
            _contact.InitNew();

            string path;
            Assert.AreEqual(HRESULT.E_UNEXPECTED, ContactUtil.GetPath(_contact, out path));
        }

        [TestMethod]
        public void GetPathFromLoadedContactTest()
        {
            _contact.Load(contactFileName, STGM.DIRECT);

            string path;
            ContactUtil.GetPath(_contact, out path).ThrowIfFailed();
            Assert.AreEqual(contactFileName, path);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPathNullArg()
        {
            string path;
            ContactUtil.GetPath(null, out path);
        }

        [TestMethod]
        public void GetContactsFolderTest()
        {
            string path = ContactUtil.GetContactsFolder();
            if (path != Environment.GetEnvironmentVariable("USERPROFILE") + "\\Contacts")
            {
                Assert.Inconclusive(
                    "GetContactsFoler succeeded with "
                    + path
                    + ", but it's not a standard location so I'm not sure if this passed.");
            }
        }

        [TestMethod]
        public void GetStringTest()
        {
            _contact.Load(contactFileName, STGM.DIRECT);

            string reallyLongName = new string('<', (int)(Win32Value.MAX_PATH * 2));
            string nickname;
            ContactUtil.GetString(_contact, string.Format(PropertyNames.NameNickNameFormat, 1), false, out nickname).ThrowIfFailed();
            ContactUtil.SetString(_contact, string.Format(PropertyNames.NameFamilyNameFormat, 1), reallyLongName).ThrowIfFailed();
            
            string familyName;
            ContactUtil.GetString(_contact, string.Format(PropertyNames.NameFamilyNameFormat, 1), false, out familyName).ThrowIfFailed();
            Assert.AreEqual(reallyLongName, familyName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetStringNullContactTest()
        {
            string value;
            ContactUtil.GetString(null, PropertyNames.Notes, false, out value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetStringNullPropertyTest()
        {
            string value;
            ContactUtil.GetString(_contact, null, false, out value);
        }

        [
            TestMethod,
            Description("Ensure that retrieving deleted strings returns correct values and error codes.")
        ]
        public void GetStringDeletedPropertyTest()
        {
            _contact.InitNew();

            // Set the property we're going to work against.
            ContactUtil.SetString(_contact, PropertyNames.Notes, "Hello").ThrowIfFailed();

            // Delete it, so that it has the old record of it.
            ContactUtil.DeleteProperty(_contact, PropertyNames.Notes).ThrowIfFailed();

            // Deleting a deleted propety should still be S_OK;
            ContactUtil.DeleteProperty(_contact, PropertyNames.Notes).ThrowIfFailed();

            // Try to retrieve it with the utility that ignores the S_FALSE retrurn.
            string value;
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetString(_contact, PropertyNames.Notes, true, out value));
            Assert.IsNull(value);

            // Try to retrieve it again but look for the S_FALSE.  Ensure that the out parameter is still null.
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, PropertyNames.Notes, false, out value));
            Assert.IsNull(value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetStringNullContactTest()
        {
            ContactUtil.SetString(null, PropertyNames.Notes, "Notes");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetStringNullPropertyTest()
        {
            ContactUtil.SetString(_contact, null, "Notes");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetDateNullContactTest()
        {
            ContactUtil.SetDate(null, PropertyNames.CreationDate, default(DateTime));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetDateNullPropertyTest()
        {
            ContactUtil.SetDate(_contact, null, default(DateTime));
        }

        [TestMethod]
        public void SetDateCreationDateTest()
        {
            _contact.InitNew();

            DateTime date;
            ContactUtil.GetDate(_contact, PropertyNames.CreationDate, true, out date).ThrowIfFailed();

            // Surprisingly, you can set the creation date.
            ContactUtil.SetDate(_contact, PropertyNames.CreationDate, DateTime.Now).ThrowIfFailed();
            
            // But you can't delete it.
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_INVALID_DATATYPE, ContactUtil.DeleteProperty(_contact, PropertyNames.CreationDate));
        }

        [TestMethod]
        public void PropertyCollectionTest()
        {
            _contact.Load(contactFileName, STGM.DIRECT);

            IContactPropertyCollection propertyCollection = null;
            string propertyName = null;
            int propertyVersion = -1;
            DateTime date = default(DateTime);
            uint type = ContactValue.CGD_UNKNOWN_PROPERTY;
            Guid propertyID = default(Guid);

            // Do a basic walk across names and dates.
            foreach(string collectionType in new string[] { PropertyNames.NameCollection, PropertyNames.DateCollection })
            {
                try
                {
                    ContactUtil.GetPropertyCollection(_contact, collectionType, null, true, out propertyCollection).ThrowIfFailed();

                    HRESULT hr = propertyCollection.Next();
                    while (HRESULT.S_OK == hr)
                    {
                        ContactUtil.GetPropertyName(propertyCollection, out propertyName).ThrowIfFailed();
                        ContactUtil.GetPropertyType(propertyCollection, out type).ThrowIfFailed();
                        ContactUtil.GetPropertyVersion(propertyCollection, out propertyVersion).ThrowIfFailed();
                        ContactUtil.GetPropertyModificationDate(propertyCollection, out date).ThrowIfFailed();

                        hr = ContactUtil.GetPropertyID(propertyCollection, out propertyID);
                        Assert.IsTrue(ContactValue.CGD_ARRAY_NODE != type || hr.Succeeded());

                        hr = propertyCollection.Next();
                    }
                    Assert.AreEqual<HRESULT>(HRESULT.S_FALSE, hr);
                }
                finally
                {
                    Utility.SafeRelease(ref propertyCollection);
                }
            }
        }

        [TestMethod ]
        public void PropertyTypeTest()
        {
            IContactPropertyCollection propertyCollection = null;
            int propertyVersion = -1;
            uint type = ContactValue.CGD_UNKNOWN_PROPERTY;

            _contact.InitNew();

            // Add an image, a date, and a string to get the basic property types.
            string photoNode;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.PhotoCollection, true, out photoNode).ThrowIfFailed();
            Bitmap bmp = Resources.IcarusFalling;
            File.Delete("Matisse.png");
            bmp.Save("Matisse.png");
            bmp.Dispose();
            FileStream fstream = new FileStream("Matisse.png", FileMode.Open);
            ContactUtil.SetBinary(_contact, photoNode + PropertyNames.Value, "image/png", fstream).ThrowIfFailed();
            fstream.Close();

            string dateNode;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.DateCollection, true, out dateNode).ThrowIfFailed();
            ContactUtil.SetDate(_contact, dateNode + PropertyNames.Value, DateTime.Now).ThrowIfFailed();
            ContactUtil.SetString(_contact, PropertyNames.Notes, "Some string").ThrowIfFailed();

            bool foundString = false;
            bool foundDate = false;
            bool foundArrayNode = false;
            bool foundBinary = false;
            try
            {
                ContactUtil.GetPropertyCollection(_contact, null, null, true, out propertyCollection).ThrowIfFailed();

                HRESULT hr = propertyCollection.Next();
                while (HRESULT.S_OK == hr)
                {
                    ContactUtil.GetPropertyType(propertyCollection, out type).ThrowIfFailed();
                    switch (type)
                    {
                        case ContactValue.CGD_ARRAY_NODE:
                            foundArrayNode = true;
                            break;
                        case ContactValue.CGD_BINARY_PROPERTY:
                            foundBinary = true;
                            break;
                        case ContactValue.CGD_DATE_PROPERTY:
                            foundDate = true;
                            break;
                        case ContactValue.CGD_STRING_PROPERTY:
                            foundString = true;
                            break;
                        default:
                            Assert.Fail("Invalid type to have found");
                            break;
                    }

                    ContactUtil.GetPropertyVersion(propertyCollection, out propertyVersion).ThrowIfFailed();
                    // These are all new properties, so they should only be 1.
                    Assert.AreEqual(1, propertyVersion);

                    hr = propertyCollection.Next();
                }
                Assert.AreEqual(HRESULT.S_FALSE, hr);
            }
            finally
            {
                Utility.SafeRelease(ref propertyCollection);
            }
            Assert.IsTrue(foundArrayNode);
            Assert.IsTrue(foundBinary);
            Assert.IsTrue(foundDate);
            Assert.IsTrue(foundString);
        }

        [
            TestMethod,
            Description(
@"Test setting and getting hierarchical simple extension properties.
NOTE: this works on a contact that hasn't been modified.
The variation of this test doesn't work because of a bug in Windows Contacts.")
        ]
        public void HierarchicalSimpleExtensionTest()
        {
            _contact.InitNew();

            string extendedCollectionNode = string.Format("[ns:reallyLongSecondLevel{0}]TopLevel", new string('a', (int)Win32Value.MAX_PATH));
            string extendedCollection = "[ns]TopLevel";

            string extendedStringProp = "/StringProp";
            string extendedDateProp = "/DateProp";

            string extendedValue = "Some text.";
            DateTime extendedDate = new DateTime(2006, 10, 31);

            // Try this for some third party extensions also.
            string node1;
            ContactUtil.CreateArrayNode(_contact, extendedCollectionNode, true, out node1).ThrowIfFailed();

            extendedDateProp = node1 + extendedDateProp;
            extendedStringProp = node1 + extendedStringProp;

            ContactUtil.SetString(_contact, extendedStringProp, extendedValue).ThrowIfFailed();
            ContactUtil.SetDate(_contact, extendedDateProp, extendedDate).ThrowIfFailed();

            string propertyValue;
            ContactUtil.GetString(_contact, extendedStringProp, true, out propertyValue).ThrowIfFailed();
            Assert.AreEqual(extendedValue, propertyValue);

            DateTime dateValue;
            ContactUtil.GetDate(_contact, extendedDateProp, true, out dateValue).ThrowIfFailed();
            Assert.AreEqual<DateTime>(extendedDate, dateValue);

            IContactPropertyCollection propertyCollection = null;

            // These are some properties that were explicitly set.  Ensure that they're present.
            string[] expectToFind = new string[] { extendedDateProp, extendedStringProp, node1 };

            string propertyName;
            uint type;
            int propertyVersion;
            DateTime date;
            Guid propertyID;

            // [Windows Bug] Make a copy of the contact before doing the walk.
            using (MemoryStream memstream = new MemoryStream())
            {
                using (ManagedIStream istream = new ManagedIStream(memstream))
                {
                    _contact.Save(istream, true);
                    memstream.Seek(0, SeekOrigin.Begin);

                    ContactRcw readonlyContact = new ContactRcw();
                    try
                    {
                        readonlyContact.Load(istream);

                        try
                        {
                            ContactUtil.GetPropertyCollection(readonlyContact, extendedCollection, null, true, out propertyCollection).ThrowIfFailed();

                            HRESULT hr = propertyCollection.Next();
                            while (HRESULT.S_OK == hr)
                            {
                                ContactUtil.GetPropertyName(propertyCollection, out propertyName).ThrowIfFailed();
                                for (int i = 0; i < expectToFind.Length; ++i)
                                {
                                    if (expectToFind[i] == propertyName)
                                    {
                                        expectToFind[i] = null;
                                        break;
                                    }
                                }
                                ContactUtil.GetPropertyType(propertyCollection, out type).ThrowIfFailed();
                                ContactUtil.GetPropertyVersion(propertyCollection, out propertyVersion).ThrowIfFailed();
                                ContactUtil.GetPropertyModificationDate(propertyCollection, out date).ThrowIfFailed();

                                hr = ContactUtil.GetPropertyID(propertyCollection, out propertyID);
                                Assert.IsTrue(ContactValue.CGD_ARRAY_NODE != type || hr.Succeeded());

                                hr = propertyCollection.Next();
                            }
                            Assert.AreEqual(HRESULT.S_FALSE, hr);
                        }
                        finally
                        {
                            Utility.SafeRelease(ref propertyCollection);
                        }
                    }
                    finally
                    {
                        Utility.SafeRelease(ref readonlyContact);
                    }
                }
            }

            foreach (string found in expectToFind)
            {
                Assert.IsNull(found, found + " was not found when enumerating the Contact properties.");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPropertyCollectionNullContactTest()
        {
            IContactPropertyCollection propertyCollection;
            ContactUtil.GetPropertyCollection(null, null, null, false, out propertyCollection);
        }

        [
            TestMethod,
            Description("Property enumeration can be filtered by specifying label sets.")
        ]
        public void GetLabeledPropertyCollectionTest()
        {
            IContactPropertyCollection propertyCollection = null;

            _contact.InitNew();

            string node;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node).ThrowIfFailed();
            ContactUtil.SetString(_contact, node + PropertyNames.FormattedName, "Name 1").ThrowIfFailed();
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node).ThrowIfFailed();
            ContactUtil.SetString(_contact, node + PropertyNames.FormattedName, "Name 2").ThrowIfFailed();
            try
            {
                ContactUtil.GetPropertyCollection(_contact,
                                                  PropertyNames.NameCollection,
                                                  new string[] { PropertyLabels.Business },
                                                  true,
                                                  out propertyCollection)
                    .ThrowIfFailed();
                // There should be no business name on this contact.
                Assert.AreEqual<HRESULT>(HRESULT.S_FALSE, propertyCollection.Next());

                ContactUtil.SetLabels(_contact, node, new string[] { PropertyLabels.Business }).ThrowIfFailed();

                Utility.SafeRelease(ref propertyCollection);
                ContactUtil.GetPropertyCollection(_contact,
                                                  PropertyNames.NameCollection,
                                                  new string[] { PropertyLabels.Business },
                                                  true,
                                                  out propertyCollection)
                    .ThrowIfFailed();

                // There should be one business name on this contact.
                Assert.AreEqual<HRESULT>(HRESULT.S_OK, propertyCollection.Next());

                // The second node should now be labeled business, and it's the only thing labeled.
                string name;
                ContactUtil.GetPropertyName(propertyCollection, out name).ThrowIfFailed();
                Assert.AreEqual(name, node);

                // There should be no business name on this contact.
                Assert.AreEqual<HRESULT>(HRESULT.S_FALSE, propertyCollection.Next());
            }
            finally
            {
                Utility.SafeRelease(ref propertyCollection);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPropertyModificationDateNullCollectionTest()
        {
            DateTime date;
            ContactUtil.GetPropertyModificationDate(null, out date);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPropertyIdNullCollectionTest()
        {
            Guid id;
            ContactUtil.GetPropertyID(null, out id);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPropertyTypeNullCollectionTest()
        {
            uint type;
            ContactUtil.GetPropertyType(null, out type);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPropertyVersionNullCollectionTest()
        {
            int version;
            ContactUtil.GetPropertyVersion(null, out version);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPropertyNameNullCollectionTest()
        {
            string name;
            ContactUtil.GetPropertyName(null, out name);
        }

        [
            TestMethod,
            Description("Set an image to a contact from a file and retrieve it")
        ]
        public void GetImageTest()
        {
            // Save the image file to disk.
            Bitmap bmp = Resources.IcarusFalling;
            File.Delete("Matisse.png");
            bmp.Save("Matisse.png");
            bmp.Dispose();

            _contact.InitNew();
            string photoNode;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.PhotoCollection, true, out photoNode).ThrowIfFailed();
            FileStream fstream = new FileStream("Matisse.png", FileMode.Open);
            ContactUtil.SetBinary(_contact, photoNode + PropertyNames.Value, "image/png", fstream).ThrowIfFailed();
            fstream.Close();

            string type;
            Stream cstream = null;
            try
            {
                ContactUtil.GetBinary(_contact, photoNode + PropertyNames.Value, true, out type, out cstream).ThrowIfFailed();
                fstream = new FileStream("Matisse.png", FileMode.Open);
                Assert.IsTrue(Utility.AreStreamsEqual(fstream, cstream));
                fstream.Close();
            }
            finally
            {
                Utility.SafeDispose(ref cstream);
            }
        }

        [
            TestMethod,
            Description("Get and set a binary on a very long simple extension property")
        ]
        public void GetBinaryForceReallocTest()
        {
            string longProperty = string.Format("[ns]Binary{0}", new string('A', (int)Win32Value.MAX_PATH));
            string longType = string.Format("image/{0}", new string('A', (int)Win32Value.MAX_PATH));

            // Save the image file to disk.  Use the hashcode to ensure that this works.
            Bitmap bmp = Resources.IcarusFalling;
            File.Delete("Matisse.png");
            bmp.Save("Matisse.png");
            bmp.Dispose();

            _contact.InitNew();
            FileStream fstream = new FileStream("Matisse.png", FileMode.Open);
            ContactUtil.SetBinary(_contact, longProperty, longType, fstream).ThrowIfFailed();
            fstream.Close();

            string type;
            Stream cstream = null;
            try
            {
                ContactUtil.GetBinary(_contact, longProperty, true, out type, out cstream).ThrowIfFailed();
                Assert.AreEqual(longType, type);

                fstream = new FileStream("Matisse.png", FileMode.Open);
                Assert.IsTrue(Utility.AreStreamsEqual(fstream, cstream));
                fstream.Close();
            }
            finally
            {
                Utility.SafeDispose(ref cstream);
            }

            ContactUtil.DeleteProperty(_contact, longProperty).ThrowIfFailed();
            
            // Retrieve the really long property that has been deleted.
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetBinary(_contact, longProperty, true, out type, out cstream));
            Assert.IsNull(cstream);
            Assert.IsNull(type);

            // Do it again, but this time check for the S_FALSE.
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetBinary(_contact, longProperty, false, out type, out cstream));
            Assert.IsNull(cstream);
            Assert.IsNull(type);
        }

        [TestMethod]
        public void SetBinaryNullTypeTest()
        {
            byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0 };
            using (MemoryStream ms = new MemoryStream())
            {
                _contact.InitNew();

                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                Assert.AreEqual(HRESULT.E_INVALIDARG, ContactUtil.SetBinary(_contact, "[Random]Bin", null, ms));

                string type;
                Stream value;
                Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetBinary(_contact, "[Random]Bin", false, out type, out value));
                Assert.IsNull(value);
                Assert.IsNull(type);

                // The stream needs to be sought to the beginning for COM.
                // ContactUtil should wrap that.
                ms.Seek(5, SeekOrigin.Begin);

                ContactUtil.SetBinary(_contact, "[Random]Bin", "bytes", ms).ThrowIfFailed();
                Assert.AreEqual(HRESULT.E_INVALIDARG, ContactUtil.SetBinary(_contact, "[Random]Bin", "", ms));
            }
        }

        [
            TestMethod,
            Description("GetString should have specific behavior when working with array arrayNode names.")
        ]
        public void GetStringOnNodePropertyTest()
        {
            _contact.InitNew();

            string node;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node).ThrowIfFailed();

            string value;
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, node, false, out value));
            Assert.IsNull(value);

            ContactUtil.SetString(_contact, node + PropertyNames.FormattedName, "Formatted Name").ThrowIfFailed();

            // Should get the same results, even if there are properties under the node.
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, node, false, out value));
            Assert.IsNull(value);

            // Try GetString on a non-existent property.
            Assert.AreEqual<HRESULT>(
                Win32Error.ERROR_PATH_NOT_FOUND,
                ContactUtil.GetString(_contact, PropertyNames.NameCollection + PropertyNames.NameArrayNode + "[2]", false, out value));
            Assert.IsNull(value);
        }

        [
            Ignore,
            TestMethod,
            Description("GetString should have specific behavior when working with array arrayNode names; it should have the same behavior when working with simple extensions also.")
        ]
        public void GetStringOnSimpleExtensionNodePropertiesTest()
        {
            _contact.InitNew();

            string node;
            ContactUtil.CreateArrayNode(_contact, "[ns:Node]Collection", true, out node).ThrowIfFailed();

            string value;
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, node, false, out value));
            Assert.IsNull(value);

            ContactUtil.SetString(_contact, node + "/Value", "Some Value").ThrowIfFailed();

            // Should get the same results, even if there are properties under the node.
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, node, false, out value));
            Assert.IsNull(value);

            // Try GetString on a non-existent property.
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetString(_contact, "[ns]Collection/Node[2]", false, out value));
            Assert.IsNull(value);

            // Try SetString on the array node also.  It should return ERROR_INVALID_DATA_TYPE
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_INVALID_DATATYPE, ContactUtil.SetString(_contact, node, "Foo"));
        }

        [TestMethod]
        public void DoesPropertyExistTest()
        {
            _contact.InitNew();

            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, ""));
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, "[ns:]BadArg"));
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, "BadPropName"));
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, PropertyNames.NameCollection + PropertyNames.NameArrayNode + "[1]"));
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, "[ns]Collection/Node[1]"));

            string nameNode;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out nameNode).ThrowIfFailed();
            ContactUtil.SetString(_contact, nameNode + PropertyNames.NickName, "Nick").ThrowIfFailed();
            
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, nameNode));
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, nameNode + PropertyNames.NickName));
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, nameNode + PropertyNames.FormattedName));

            ContactUtil.DeleteProperty(_contact, nameNode + PropertyNames.NickName).ThrowIfFailed();
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, nameNode + PropertyNames.NickName));
            
            // unlike deleting a value, deleting the node should still leave it.
            ContactUtil.DeleteArrayNode(_contact, nameNode).ThrowIfFailed();
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, nameNode));

            // Similar tests to above, but with simple extension properties.
            string extNode;
            ContactUtil.CreateArrayNode(_contact, "[ns:Node]Collection", true, out extNode).ThrowIfFailed();
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, extNode));

            ContactUtil.SetString(_contact, extNode + "/Value1", "Val").ThrowIfFailed();
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, extNode));
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, extNode + "/Value1"));
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, extNode + "/Value2"));

            ContactUtil.DeleteProperty(_contact, extNode + "/Value1").ThrowIfFailed();
            Assert.IsFalse(ContactUtil.DoesPropertyExist(_contact, extNode + "/Value1"));

            // unlike deleting a value, deleting the node should still leave it.
            ContactUtil.DeleteArrayNode(_contact, extNode).ThrowIfFailed();
            Assert.IsTrue(ContactUtil.DoesPropertyExist(_contact, extNode));
        }

        [TestMethod]
        public void DeleteArrayNodeAsPropertyTest()
        {
            HRESULT hr = HRESULT.S_OK;
            _contact.InitNew();

            string node;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.NameCollection, true, out node).ThrowIfFailed();
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_INVALID_DATATYPE, ContactUtil.DeleteProperty(_contact, node));
            ContactUtil.SetString(_contact, node + PropertyNames.FormattedName, "FN").ThrowIfFailed();
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_INVALID_DATATYPE, ContactUtil.DeleteArrayNode(_contact, node + PropertyNames.FormattedName));
        }

        [TestMethod]
        public void DeleteNonExistentArrayNode()
        {
            _contact.InitNew();
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.DeleteArrayNode(_contact, PropertyNames.NameCollection + PropertyNames.NameArrayNode + "[1]"));
        }
        
        [TestMethod]
        public void VerifyNilBehaviorTest()
        {
            HRESULT hr = HRESULT.S_OK;
            string emailArrayFormat = PropertyNames.EmailAddressCollection + PropertyNames.EmailAddressArrayNode + "[{0}]";

            _contact.Load(contactFileName, STGM.DIRECT);

            // Find the last set index.
            int index = 1;
            while (ContactUtil.DoesPropertyExist(_contact, string.Format(emailArrayFormat, index)))
            {
                ++index;
            }

            // Verify that get[index] is unavailable.
            string value;
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetString(_contact, string.Format(emailArrayFormat, index), false, out value));

            // and the address
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetString(_contact, string.Format(emailArrayFormat, index) + PropertyNames.Address, false, out value));

            string node;
            ContactUtil.CreateArrayNode(_contact, PropertyNames.EmailAddressCollection, true, out node).ThrowIfFailed();
            Assert.AreEqual(string.Format(emailArrayFormat, index), node);

            ContactUtil.SetString(_contact, node + PropertyNames.Address, "test_number_1_@test.com").ThrowIfFailed();

            // delete the new email address property node.
            ContactUtil.DeleteArrayNode(_contact, node).ThrowIfFailed();
            
            // verify that the node is discoverable
            Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, node, false, out value));

            // verify /Address fails with PATH_NOT_FOUND
            Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetString(_contact, node + PropertyNames.Address, false, out value));
        }

        [
            TestMethod,
            Description("Use string format concatenation to walk hierarchical properties")
        ]
        public void WalkHeirarchicalProperties()
        {
            HRESULT hr = HRESULT.S_OK;

            _contact.Load(contactFileName, STGM.DIRECT);

            for (int index = 1; hr.Succeeded(); ++index)
            {
                string prop = string.Format(PropertyNames.EmailAddressAddressFormat, index);
                
                string value;
                hr = ContactUtil.GetString(_contact, prop, false, out value);
                if (hr.Failed())
                {
                    Assert.AreEqual((HRESULT)Win32Error.ERROR_PATH_NOT_FOUND, hr);

                    string nextNode;
                    ContactUtil.CreateArrayNode(_contact, PropertyNames.EmailAddressCollection, true, out nextNode).ThrowIfFailed();

                    // Verify that the get for any data at this node fails.
                    Assert.AreEqual<HRESULT>(Win32Error.ERROR_PATH_NOT_FOUND, ContactUtil.GetString(_contact, nextNode + PropertyNames.Address, false, out value));

                    // And set the value for the [nth] element...
                    // add a new e-mail address entry at the location that just failed a get.
                    ContactUtil.SetString(_contact, nextNode + PropertyNames.Address, "joe@test.com").ThrowIfFailed();

                    // and re-get it!
                    ContactUtil.GetString(_contact, nextNode + PropertyNames.Address, false, out value).ThrowIfFailed();
                    Assert.AreEqual("joe@test.com", value);

                    // and delete the change
                    ContactUtil.DeleteProperty(_contact, nextNode + PropertyNames.Address).ThrowIfFailed();

                    // and verify the delete succeeded
                    Assert.AreEqual(HRESULT.S_FALSE, ContactUtil.GetString(_contact, nextNode + PropertyNames.Address, false, out value));

                    break;
                }
            }
        }

        [
            TestMethod,
            Description("Connect to the contact as a connection point.")
        ]
        public void AdviseAndUnadviseTest()
        {
            PropertyNotifySink sink = new PropertyNotifySink();
            IConnectionPoint pcp = null;
            uint dwCookie = 0;
            Guid riid = new Guid(PropertyNotifySink.Iid);

            _contact.InitNew();
            PropertyNotifySink.ConnectToConnectionPoint(sink, ref riid, Win32Value.TRUE, _contact, ref dwCookie, out pcp);
            pcp.Unadvise((int)dwCookie);
            Utility.SafeRelease(ref pcp);
        }

        [TestMethod]
        public void ChangeNotificationTest()
        {
            PropertyNotifySink sink = new PropertyNotifySink();
            IConnectionPoint pcp = null;
            uint dwCookie = 0;
            Guid riid = new Guid(PropertyNotifySink.Iid);

            // Make a copy of the file.  I want to call CommitChanges but not mess with other tests.
            File.Delete("ChangeTest.contact");
            File.Copy(contactFileName, "ChangeTest.contact");
            _contact.Load("ChangeTest.contact", STGM.DIRECT);

            // Clear any pending messages.
            System.Windows.Forms.Application.DoEvents();

            PropertyNotifySink.ConnectToConnectionPoint(sink, ref riid, Win32Value.TRUE, _contact, ref dwCookie, out pcp);

            // set a couple properties and count the change notifications.
            ContactUtil.SetString(_contact, string.Format(PropertyNames.NameFormattedNameFormat, 1), "new formatted name").ThrowIfFailed();
            ContactUtil.SetString(_contact, string.Format(PropertyNames.NameNickNameFormat, 1), "new nick name").ThrowIfFailed();

            Assert.AreEqual(2, sink._OnChangedCount);

            // Committing the contact also raises a change notification, but it requires pumping messages.
            System.Windows.Forms.Application.DoEvents();

            ContactUtil.CommitContact(_contact, false).ThrowIfFailed();

            bool fChanged = false;
            for (int retries = 0; retries < 50; ++retries)
            {
                if (sink._OnChangedCount >= 3)
                {
                    fChanged = true;
                    pcp.Unadvise((int)dwCookie);
                    Utility.SafeRelease(ref pcp);
                    break;
                }
                // If you're going to wait on the file system change notification you need to pump messages.
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1000);
            }
            Assert.IsTrue(fChanged);

            Assert.AreEqual(0, sink._OnRequestEditCount);
            File.Delete("ChangeTest.contact");
        }
    }
}
#endif