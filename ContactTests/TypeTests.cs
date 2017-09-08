using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Tests;

namespace Microsoft.Communications.Contacts.Tests
{
    /// <summary>
    /// Summary description for TypeTests
    /// </summary>
    [TestClass]
    public class NameTests
    {
        private struct FmlName
        {
            public FmlName(string formatted, string first, string middle, string last, NameCatenationOrder order)
            {
                Formatted = formatted;
                First = first;
                Middle = middle;
                Last = last;
                Order = order;
            }
            public readonly string Formatted;
            public readonly string First;
            public readonly string Middle;
            public readonly string Last;
            public readonly NameCatenationOrder Order;
        }

        [TestMethod]
        public void FormatNameTests()
        { 
            FmlName[] expectedNameTable = new FmlName[]
            {
                new FmlName("",                   null,    null,     null,   NameCatenationOrder.None),
                new FmlName("",                   "First", null,     "Last", NameCatenationOrder.None),
                new FmlName("First Last",         "First", null,     "Last", NameCatenationOrder.GivenFamily),
                new FmlName("Last First",         "First", "Middle", "Last", NameCatenationOrder.FamilyGiven),
                new FmlName("Last First",         "First", null,     "Last", NameCatenationOrder.FamilyGivenMiddle),
                new FmlName("Last First Middle",  "First", "Middle", "Last", NameCatenationOrder.FamilyGivenMiddle),
                new FmlName("First Last",         "First", null,     "Last", NameCatenationOrder.GivenMiddleFamily),
                new FmlName("First Middle Last",  "First", "Middle", "Last", NameCatenationOrder.GivenMiddleFamily),
                new FmlName("Last, First Middle", "First", "Middle", "Last", NameCatenationOrder.FamilyCommaGivenMiddle),
                new FmlName("Last, Middle",       null,    "Middle", "Last", NameCatenationOrder.FamilyCommaGivenMiddle),
                new FmlName("Last, First",        "First", "Middle", "Last", NameCatenationOrder.FamilyCommaGiven),
                new FmlName("First Middle",       "First", "Middle", null,   NameCatenationOrder.FamilyCommaGivenMiddle),
                new FmlName("Middle",             "",      "Middle", "",     NameCatenationOrder.FamilyCommaGivenMiddle),
            };

            foreach (FmlName fml in expectedNameTable)
            {
                Assert.AreEqual(fml.Formatted, Name.FormatName(fml.First, fml.Middle, fml.Last, fml.Order));
                Name n = new Name(fml.First, fml.Middle, fml.Last, fml.Order);
                Assert.AreEqual(fml.First ?? "", n.GivenName);
                Assert.AreEqual(fml.Middle ?? "", n.MiddleName);
                Assert.AreEqual(fml.Last ?? "", n.FamilyName);
                Assert.AreEqual(fml.Formatted ?? "", n.FormattedName);
            }
        }

        [TestMethod]
        public void FormatNameBadEnumTest()
        {
            UTVerify.ExpectException<ArgumentException>(() => Name.FormatName("First", "Middle", "Last", (NameCatenationOrder)(-1)));
        }

        [TestMethod]
        public void DefaultNameTest()
        {
            Name n = default(Name);

            Assert.AreEqual("", n.FamilyName);
            Assert.AreEqual("", n.FormattedName);
            Assert.AreEqual("", n.Generation);
            Assert.AreEqual("", n.GivenName);
            Assert.AreEqual("", n.MiddleName);
            Assert.AreEqual("", n.Nickname);
            Assert.AreEqual("", n.PersonalTitle);
            Assert.AreEqual("", n.Phonetic);
            Assert.AreEqual("", n.Prefix);
            Assert.AreEqual("", n.Suffix);

            Assert.AreEqual(default(Name), new Name(null, null, null, NameCatenationOrder.FamilyGiven));
        }

        [TestMethod]
        public void ExplicitNameTest()
        {
            Name n = "FormattedName";

            Assert.AreEqual("FormattedName", n.FormattedName);
            Assert.IsTrue(string.IsNullOrEmpty(n.FamilyName));
            Assert.IsTrue(string.IsNullOrEmpty(n.Generation));
            Assert.IsTrue(string.IsNullOrEmpty(n.GivenName));
            Assert.IsTrue(string.IsNullOrEmpty(n.MiddleName));
            Assert.IsTrue(string.IsNullOrEmpty(n.Nickname));
            Assert.IsTrue(string.IsNullOrEmpty(n.PersonalTitle));
            Assert.IsTrue(string.IsNullOrEmpty(n.Phonetic));
            Assert.IsTrue(string.IsNullOrEmpty(n.Prefix));
            Assert.IsTrue(string.IsNullOrEmpty(n.Suffix));

            // This is kindof weird.  There's no implicit cast from object to Name,
            //     so it resolves to the standard .Equals method.
            Assert.IsFalse(n.Equals((object)"FormattedName"));
            // This will do the implicit cast from a string to Name
            Assert.IsTrue(n.Equals("FormattedName"));

            n = new Name("", "", "", "", "", "", "", "", "", "");
            Assert.AreEqual(default(Name), n);

            n = new Name("FN", "P", "PR", "T", "G", "M", "F", "Gen", "X", "NN");
            Assert.AreEqual("F", n.FamilyName);
            Assert.AreEqual("Gen", n.Generation);
            Assert.AreEqual("G", n.GivenName);
            Assert.AreEqual("M", n.MiddleName);
            Assert.AreEqual("NN", n.Nickname);
            Assert.AreEqual("T", n.PersonalTitle);
            Assert.AreEqual("P", n.Phonetic);
            Assert.AreEqual("PR", n.Prefix);
            Assert.AreEqual("X", n.Suffix);
        }
    }
}
