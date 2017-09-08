/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Controls;
    using System.Windows;

    public class BoundedGridViewColumn : GridViewColumn
    {
        public static readonly DependencyProperty MinWidthProperty;
        public static readonly DependencyProperty MaxWidthProperty;

        static BoundedGridViewColumn()
        {
            WidthProperty.OverrideMetadata(typeof(BoundedGridViewColumn),
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(_ConstrainWidth)));

            MinWidthProperty = DependencyProperty.Register(
                "MinWidth",
                typeof(double),
                typeof(BoundedGridViewColumn),
                new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(_OnBoundedWidthChanged)));

            MaxWidthProperty = DependencyProperty.Register(
                "MaxWidth",
                typeof(double),
                typeof(BoundedGridViewColumn),
                new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(_OnBoundedWidthChanged)));
        }

        private static void _OnBoundedWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BoundedGridViewColumn constraint = o as BoundedGridViewColumn;
            if (null != constraint)
            {
                constraint.CoerceValue(WidthProperty);
            }
        }

        private static object _ConstrainWidth(DependencyObject o, object baseValue)
        {
            // TODO: Validate.IsTrue(MinWidth <= MaxWidth);
            BoundedGridViewColumn constraint = o as BoundedGridViewColumn;
            double width = (double)baseValue;
            if (null != constraint)
            {
                width = Math.Max(constraint.MinWidth, width);
                width = Math.Min(constraint.MaxWidth, width);
            }
            return width;
        }

        public double MinWidth
        {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        public double MaxWidth
        {
            get { return (double)GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }
    }
}
