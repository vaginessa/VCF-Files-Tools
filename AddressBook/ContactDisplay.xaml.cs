/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using Standard;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for ContactDisplay.xaml
    /// </summary>
    public partial class ContactDisplay : Window
    {
        private ContactInfo _contactView;
        private Contact _contact;

        public ContactDisplay()
        {
            InitializeComponent();
        }

        public Contact SourceContact
        {
            set
            {
                Assert.IsNull(_contact);
                Assert.IsNotNull(value);
                _contact = value;
                _contactView = new ContactInfo(value);
                DataContext = _contactView;
            }
            get
            {
                return _contact;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            GlassHelper.ExtendGlassFrameComplete(this);
            GlassHelper.SetWindowThemeAttribute(this, false, false);
        }

        private void _OnMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            this.DragMove();
        }

        private void _OnSaveChanges(object source, RoutedEventArgs e)
        {
            ((Button)source).Focus();
            _contactView.SaveToSource();
            _contact.CommitChanges();
            this.Close();
        }
    }
}