/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets.TypeControls
{
    using Standard;
    using System;
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
    using System.IO;

    public partial class NameControl : UserControl
    {
        public NameControl()
        {
            InitializeComponent();
        }

        private void _OnUserTileClick(object source, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofn = new System.Windows.Forms.OpenFileDialog();
            ofn.Filter = "All Picture Files|*.BMP;*.GIF;*.JPEG;*.JPG;*.JPE;*.JFIF;*.PNG;*.TIF;*.TIFF;*.ICO|Bitmap Files (*.BMP)|*.BMP|GIF (*.GIF)|*.GIF|JPEG (*.JPEG;*.JPG;*.JPE;*.JFIF)|*.JPEG;*.JPG;*.JPE;*.JFIF|PNG (*.PNG)|*.PNG|TIFF (*.TIF;*.TIFF)|*.TIF;*.TIFF|ICO (*.ICO)|*.ICO|All Files|*";
            ofn.Title = "Select User Tile";
            ofn.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            ofn.Multiselect = false;
            if (ofn.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ContactInfo view = (ContactInfo)DataContext;
                Stream disposable = view.UserTile.Value;
                Utility.SafeDispose(ref disposable);
                view.UserTile.Value = ofn.OpenFile();
            }
        }

        private void _OnUserTileClear(object source, RoutedEventArgs e)
        {
            ContactInfo view = (ContactInfo)DataContext;
            Stream disposable = view.UserTile.Value;
            Utility.SafeDispose(ref disposable);
            view.UserTile.Clear();
        }


    }
}