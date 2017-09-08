/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.Threading;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Standard;

    // Disambiguate Standard.Assert
    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class ContactManagerThreadTests
    {
        private const int WaitTimeout = 1000;
        private ManualResetEvent _threadEvent;
        private ContactManager _manager;

        [TestInitialize]
        public void TestInitialize()
        {
            _threadEvent = new ManualResetEvent(false);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Utility.SafeDispose(ref _manager);
        }

        [
            TestMethod,
            Description("Can't create a ContactManager from an MTA thread."),
        ]
        public void CreateOnMtaThreadTest()
        {
            bool passed = false;
            Thread t = new Thread((ThreadStart)delegate
            {
                try
                {
                    _manager = new ContactManager();
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
            Assert.IsNull(_manager);
        }

        [
            TestMethod,
            Description("ContactManagers shouldn't be able to be accessed from threads other than the one on which they were created.")
        ]
        public void AccessFromOtherThreadTest()
        {
            _manager = new ContactManager();

            foreach (ApartmentState state in new ApartmentState[] { ApartmentState.MTA, ApartmentState.STA })
            {
                bool passed = false;
                Thread t = new Thread((ThreadStart)delegate
                {
                    try
                    {
                        Contact contact = _manager.MeContact;
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
                Assert.IsNotNull(_manager);
            }
        }

        [
            TestMethod,
            Description("Methods on ContactManagers can be called from other threads if they go by way of the manager's Dispatcher property.")
        ]
        public void DispatchActionFromMtaThreadTest()
        {
            _manager = new ContactManager();
            Contact contact = null;
            try
            {
                Thread t = new Thread((ThreadStart)delegate
                {
                    ThreadStart ts = delegate { contact = _manager.CreateContact(); };
                    _manager.Dispatcher.Invoke(DispatcherPriority.Send, ts);
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
                Assert.IsNotNull(contact);
                Assert.IsNotNull(_manager);
            }
            finally
            {
                Utility.SafeDispose(ref contact);
            }
        }
    }
}
