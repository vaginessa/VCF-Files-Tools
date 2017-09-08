/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    using ColumnProperties = System.Collections.Generic.KeyValuePair<string, string>;

    public class ThumbnailView : ViewBase
    {
        protected override object DefaultStyleKey
        {
            get { return new ComponentResourceKey(typeof(ThumbnailView), "ThumbnailView"); }
        }

        protected override object ItemContainerDefaultStyleKey
        {
            get { return new ComponentResourceKey(typeof(ThumbnailView), "ThumbnailViewItem"); }
        }
    }

    public class DetailsView : GridView
    {
        private GridViewColumnHeader _sortedColumn;
        private ListSortDirection _direction = ListSortDirection.Ascending;

        private readonly ColumnProperties[] _columnProperties = new[]
        {
            new ColumnProperties("Name",   "Names.Default.FormattedName"),
            new ColumnProperties("E-mail", "EmailAddresses.Default.Address"),
            new ColumnProperties("Business Phone", "PhoneNumbers[" + PropertyLabels.Business + ", " + PhoneLabels.Voice + "].Number"),
            new ColumnProperties("Notes",  "Notes"),
        };

        private void header_Click(object sender, RoutedEventArgs e)
        {
            var h = e.OriginalSource as GridViewColumnHeader;

            if (null != h)
            {
                // If we're clicking on the column that's already sorted, flip the sort order.
                if (h == _sortedColumn)
                {
                    _direction = _direction == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;
                }
                // Otherwise change the sort column and sort ascending.
                else
                {
                    _sortedColumn = h;
                    _direction = ListSortDirection.Ascending;
                }

                _Sort();
            }
        }

        private void _Sort()
        {
            if (null != _sortedColumn)
            {
                //_list.Items.SortDescriptions.Clear();
                //_list.Items.SortDescriptions.Add(new SortDescription(_sortedColumn.Tag as string, _direction));
            }

        }

        public DetailsView()
        {
            GridViewColumn col;
            GridViewColumnHeader header;

            col = new BoundedGridViewColumn();
            ((BoundedGridViewColumn)col).MaxWidth = 128;
            ((BoundedGridViewColumn)col).MinWidth = 16;
            col.Width = 96;
            header = new GridViewColumnHeader
            {
                Content = "User Tile"
            };
            col.Header = header;
            var template = new DataTemplate();
            col.CellTemplate = template;
            var elFactory = new FrameworkElementFactory(typeof(Image));
            template.VisualTree = elFactory;
            var bind = new Binding("UserTile.Image");
            elFactory.SetBinding(Image.SourceProperty, bind);
            Columns.Add(col);

            foreach (ColumnProperties prop in _columnProperties)
            {
                col = new GridViewColumn();
                header = new GridViewColumnHeader();
                header.Click += header_Click;
                header.Tag = prop.Value;
                header.Content = prop.Key;
                col.Header = header;
                col.Width = 200;
                col.DisplayMemberBinding = new Binding(prop.Value);
                Columns.Add(col);
            }
        }
    }
}