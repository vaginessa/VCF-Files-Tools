/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using Standard;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Globalization;
    using System.Diagnostics;

    public class MailtoConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty((string)value))
            {
                return "mailto:" + (string)value;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public partial class ContactHeader : UserControl
    {
        public ContactHeader()
        {
            InitializeComponent();
        }

        private void _OnClickLink(object sender, RoutedEventArgs e)
        {
            string link = ((Hyperlink)sender).NavigateUri.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(link));

            if (!Uri.IsWellFormedUriString(link, UriKind.Absolute))
            {
                // try prefixing http:// to it.
                link = "http://" + link;
            }

            Process.Start(new ProcessStartInfo(link));
            e.Handled = true;
        }

    }
}
