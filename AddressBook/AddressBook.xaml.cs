/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
    using Interop;
    using Standard;
    using System.Reflection;
    using System.Diagnostics;

    public partial class AddressBook
    {
        private class FadeAnimator
        {
            private readonly DoubleAnimation _fadeIn;
            private readonly DoubleAnimation _fadeOut;
            private readonly DoubleAnimation _scaleIn;
            private readonly DoubleAnimation _scaleOut;

            public FadeAnimator(Duration duration)
            {
                _fadeIn = new DoubleAnimation
                {
                    Duration = duration,
                    FillBehavior = FillBehavior.Stop,
                    From = 0,
                    To = 1,
                };
                _fadeOut = new DoubleAnimation
                {
                    Duration = duration,
                    FillBehavior = FillBehavior.Stop,
                    From = 1,
                    To = 0,
                };
                _scaleIn = new DoubleAnimation
                {
                    Duration = duration,
                    FillBehavior = FillBehavior.Stop,
                    From = 0,
                    To = 1,
                };
                _scaleOut = new DoubleAnimation
                {
                    Duration = duration,
                    FillBehavior = FillBehavior.Stop,
                    From = 1,
                    To = 0,
                };
            }
            public void FadeOut(FrameworkElement element)
            {
                DoubleAnimation scaleClone = _scaleOut.Clone();
                scaleClone.From = element.ActualHeight;

                // Once the animation is complete need to collapse the item so the panel will relayout.
                scaleClone.Completed += (sender, e) => element.Visibility = Visibility.Collapsed;

                element.BeginAnimation(OpacityProperty, _fadeOut);
                element.BeginAnimation(HeightProperty, scaleClone);
            }

            public void FadeIn(UIElement element)
            { 
                element.Visibility = Visibility.Visible;

                DoubleAnimation scaleClone = _scaleIn.Clone();
                element.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                scaleClone.To = element.DesiredSize.Height;

                element.BeginAnimation(OpacityProperty, _fadeIn);
                element.BeginAnimation(HeightProperty, scaleClone);
            }

        }
        private ContactManager _contactManager;

        private void _OnSwitchContext(object sender, RoutedEventArgs e)
        {
            ThreadStart ts = delegate
            {
                string newRoot = ShellProvider.SelectFolder("Choose the new Root", _contactManager.RootDirectory);
                if (Directory.Exists(newRoot))
                {
                    _contactManager.Dispose();
                    _contactManager = new ContactManager(newRoot);
                    _contactManager.CollectionChanged += delegate { _RefreshList(); };

                    _RefreshList();
                }
            };
            Dispatcher.Invoke(DispatcherPriority.Send, ts);
        }

        private void _OnNewContact(object sender, RoutedEventArgs e)
        {
            ThreadStart ts = delegate
            {
                Contact contact = _contactManager.CreateContact();
                var ui = new ContactDisplay 
                {
                    SourceContact = contact
                };
                if (true == ui.ShowDialog())
                {
                    contact.CommitChanges();
                }
            };
            _contactManager.Dispatcher.Invoke(DispatcherPriority.Send, ts);
        }

        private void _OnDelete(object sender, RoutedEventArgs e)
        {
            var contact = _contactPanel.SelectedItem as Contact;
            if (null != contact)
            {
                ThreadStart ts = () => _contactManager.Remove(contact.Id);
                _contactManager.Dispatcher.Invoke(DispatcherPriority.Send, ts);
            }
        }

        private void _OnExportVcf(object sender, RoutedEventArgs e)
        {
            _contactPanel.SelectionMode = SelectionMode.Extended;
            _contactPanel.SelectAll();
            List<Contact> list = new List<Contact>();
            for (int i = 0; i < _contactPanel.Items.Count; i++)
            {
                var cont = _contactPanel.SelectedItems[i] as Contact;
                if (null != cont)
                {
                    list.Add(cont);
                }
               
            }
            var sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Vcf files (*.vcf)|*.vcf|All files (*.*)|*.*";
            sfd.RestoreDirectory = true;
            sfd.Title = "Save Vcf File";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ThreadStart ts = () => Contact.SaveToVCard21(list, sfd.FileName);
                _contactManager.Dispatcher.Invoke(DispatcherPriority.Send, ts);
            }           
        }

        private void _OnImportVcf(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ThreadStart ts = delegate
                {
                    using (var sr = new StreamReader(ofd.FileName))
                    {
                        ICollection<Contact> contacts = Contact.CreateFromVCard(sr);
                        try
                        {
                            foreach (Contact contact in contacts)
                            {
                                _contactManager.AddContact(contact);
                            }
                        }
                        finally
                        {
                            foreach (Contact contact in contacts)
                            {
                                contact.Dispose();
                            }
                        }
                    }
                };
                _contactManager.Dispatcher.Invoke(DispatcherPriority.Send, ts);
            }
        }
        private void _About(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coded By Mahdi Hosseini\nmahdidvb72@gmail.com","About",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        public AddressBook()
        {
            InitializeComponent();

            Photo.SupportNonLocalUrls = true;
            _contactManager = new ContactManager();
            _contactManager.CollectionChanged += (sender, e) => _RefreshList();

            _RefreshList();


            Closing += (sender, e) => Utility.SafeDispose(ref _contactManager);
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            this.Title += "  " + version;
            //_sortedColumn = (GridViewColumnHeader)_view.Columns[1].Header;
            //_Sort();
        }

        private void _OpenContact(object sender, MouseButtonEventArgs e)
        {
            var contact = _contactPanel.SelectedItem as Contact;
            if (null != contact)
            {
                var ui = new ContactDisplay
                {
                    SourceContact = contact
                };
                if (true == ui.ShowDialog())
                {
                    contact.CommitChanges();
                }
            }
        }

        private void _RefreshList()
        {
            // I think that this is the correct way to bind data to the list view, but it has odd side effects
            // that cause the process to hang and not allow the list to be sorted...
            // list.ItemsSource = new ContactManager().GetContactCollection();
            _contactPanel.Items.Clear();
            foreach (Contact contact in _contactManager.GetContactCollection())
            {
                _contactPanel.Items.Add(contact);
            }
            //_Sort();
        }

        /// <summary>Handle changes to the wordwheel textbox.  Filters the contact list appropriately.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterWordwheel(object sender, TextChangedEventArgs e)
        {
            var animator = new FadeAnimator(new Duration(TimeSpan.FromMilliseconds(200)));
            // Not using the Filter property on the ItemCollection as it doesn't facilitate animations
            // This isn't an ideal way to do this (e.g. it's not a very generic solution),
            // but it serves its task.

            string filterText = _wordwheel.Text;
            foreach (Contact c in _contactPanel.Items)
            {
                var item = _contactPanel.ItemContainerGenerator.ContainerFromItem(c) as ListBoxItem;
                if (null == item)
                {
                    continue;
                }

                // Does the current item match the filter?
                bool fade = !(c.Names.Default.FormattedName.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) || c.EmailAddresses.Default.Address.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) || c.PhoneNumbers[PropertyLabels.Business, PhoneLabels.Voice].Number.StartsWith(filterText, StringComparison.OrdinalIgnoreCase));

                if (!fade && item.Visibility == Visibility.Visible)
                {
                    // Item's already visible, nothing to do.
                    continue;
                }

                // Apply the appropriate animation, hide or show.
                if (fade)
                {
                    // Ensure we don't leave the user with an invisible selection.
                    item.IsSelected = false;

                    animator.FadeOut(item);
                }
                else
                {
                    animator.FadeIn(item);
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.F) && Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.F) && Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Keyboard.Focus(_wordwheel);
                e.Handled = true;
            }
            
        }
    }
}