namespace Microsoft.Communications.Contacts.Widgets
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using Standard;

    // Adapted from Kevin Moore's Bag o' Tricks:
    //     http://wpf.netfx3.com/files/folders/controls/entry10297.aspx
    // BUGBUG: Keyboard navigation when this panel is used to back a ListBox doesn't work correctly.
    //     Up-down is fine, left-right tends to go vertical as well if the panel has been resized.
    // BUGBUG: This implementation doesn't respect Margin dependency property.
    public class AnimatingTilePanel : Panel
    {
        private class Data
        {
            public Point ChildTarget;
            public Point ChildLocation;
            public Vector Velocity = new Vector(0, 0);
            public bool IsNew = true;
            public readonly double Random = s_random.NextDouble();

            private static readonly Random s_random = new Random();
        }

        private class CompositionTargetRenderingListener
        {
            public void StartListening()
            {
                if (!m_isListening)
                {
                    m_isListening = true;
                    CompositionTarget.Rendering += _Rendering;
                }
            }

            public void StopListening()
            {
                if (m_isListening)
                {
                    m_isListening = false;
                    CompositionTarget.Rendering -= _Rendering;
                }
            }

            public event EventHandler Rendering;

            protected virtual void OnRendering(EventArgs args)
            {
                EventHandler handler = Rendering;
                if (handler != null)
                {
                    handler(this, args);
                }
            }

            #region Implementation

            private void _Rendering(object sender, EventArgs e)
            {
                OnRendering(e);
            }

            private bool m_isListening;

            #endregion

        }

        public AnimatingTilePanel()
        {
            _listener.Rendering += compositionTarget_Rendering;

            Loaded += delegate { _listener.StartListening(); };
            Unloaded += delegate { _listener.StopListening(); };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _OnPreApplyTemplate();

            var theChildSize = new Size(ItemWidth, ItemHeight);

            int displayableItems = 0;
            foreach (UIElement child in Children)
            {
                if (child.Visibility != Visibility.Collapsed)
                {
                    child.Measure(theChildSize);
                    ++displayableItems;
                }
            }

            int childrenPerRow = Math.Max(displayableItems, 1);

            // Figure out how many children fit on each row
            if (availableSize.Width != Double.PositiveInfinity)
            {
                childrenPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / ItemWidth));
            }

            // Calculate the width and height this results in
            double width = childrenPerRow * ItemWidth;
            double height = ItemHeight * Math.Ceiling((double)displayableItems / childrenPerRow);

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _listener.StartListening();

            // Calculate how many children fit on each row
            int childrenPerRow = Math.Max(1, (int)Math.Floor(finalSize.Width / ItemWidth));

            bool animateNewItem = AnimateNewItem;

            var theChildSize = new Size(ItemWidth, ItemHeight);
            int visibleIndex = 0;
            for (int i = 0; i < Children.Count; ++i, ++visibleIndex)
            {
                UIElement child = Children[i];

                if (child.Visibility == Visibility.Collapsed)
                {
                    --visibleIndex;
                    continue;
                }

                // Figure out where the child goes
                Point newOffset = _CalculateChildOffset(visibleIndex, childrenPerRow, ItemWidth, ItemHeight, finalSize.Width, Children.Count);

                var data = child.GetValue(DataProperty) as Data;
                if (data == null)
                {
                    data = new Data();
                    child.SetValue(DataProperty, data);
                }

                //set the location attached DP
                data.ChildTarget = newOffset;

                if (data.IsNew) // first time I've seen this...
                {
                    data.IsNew = false;

                    if (animateNewItem)
                    {
                        newOffset.X -= theChildSize.Width;
                    }

                    data.ChildLocation = newOffset;
                    child.Arrange(new Rect(newOffset, theChildSize));
                }
                else
                {
                    Point currentOffset = data.ChildLocation;
                    // Position the child and set its size
                    child.Arrange(new Rect(currentOffset, theChildSize));
                }
            }
            return finalSize;
        }

        #region Public Properties

        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        public static readonly DependencyProperty ItemWidthProperty =
            CreateDoubleDP("ItemWidth", 50, FrameworkPropertyMetadataOptions.AffectsMeasure, 0, double.PositiveInfinity, true);

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        public static readonly DependencyProperty ItemHeightProperty =
            CreateDoubleDP("ItemHeight", 50, FrameworkPropertyMetadataOptions.AffectsMeasure, 0, double.PositiveInfinity, true);

        public double Dampening
        {
            get { return (double)GetValue(DampeningProperty); }
            set { SetValue(DampeningProperty, value); }
        }

        public static readonly DependencyProperty DampeningProperty =
            CreateDoubleDP("Dampening", 0.2, FrameworkPropertyMetadataOptions.None, 0, 1, false);

        public double Attraction
        {
            get { return (double)GetValue(AttractionProperty); }
            set { SetValue(AttractionProperty, value); }
        }

        public static readonly DependencyProperty AttractionProperty =
            CreateDoubleDP("Attraction", 2, FrameworkPropertyMetadataOptions.None, 0, double.PositiveInfinity, false);

        public bool AnimateNewItem
        {
            get { return (bool)GetValue(AnimateNewItemProperty); }
            set { SetValue(AnimateNewItemProperty, value); }
        }

        public static readonly DependencyProperty AnimateNewItemProperty =
            DependencyProperty.Register("AnimateNewItem", typeof(bool), typeof(AnimatingTilePanel), new FrameworkPropertyMetadata(false));

        public double Variation
        {
            get { return (double)GetValue(VariationProperty); }
            set { SetValue(VariationProperty, value); }
        }

        public static readonly DependencyProperty VariationProperty =
            CreateDoubleDP("Variation", 1, FrameworkPropertyMetadataOptions.None, 0, true, 1, true, false);

        #endregion

        #region Implementation

        #region private methods

        private void _BindToParentItemsControl(DependencyProperty property, DependencyObject source)
        {
            if (DependencyPropertyHelper.GetValueSource(this, property).BaseValueSource == BaseValueSource.Default)
            {
                var binding = new Binding
                {
                    Source = source,
                    Path = new PropertyPath(property)
                };
                SetBinding(property, binding);
            }
        }

        private void _OnPreApplyTemplate()
        {
            if (!m_appliedTemplate)
            {
                m_appliedTemplate = true;

                DependencyObject source = TemplatedParent;

                var itemsPresenter = source as ItemsPresenter;
                if (null != itemsPresenter)
                {
                    source = _LookForItemsControl(itemsPresenter);
                }

                if (null != source)
                {
                    _BindToParentItemsControl(ItemHeightProperty, source);
                    _BindToParentItemsControl(ItemWidthProperty, source);
                }
            }
        }

        private static ItemsControl _LookForItemsControl(DependencyObject element)
        {
            if (null == element)
            {
                return null;
            }

            var itemsControl = element as ItemsControl;
            if (null != itemsControl)
            {
                return itemsControl;
            }

            var parent = VisualTreeHelper.GetParent(element) as FrameworkElement;
            return _LookForItemsControl(parent);
        }

        private void compositionTarget_Rendering(object sender, EventArgs e)
        {
            double dampening = Dampening;
            double attractionFactor = Attraction;
            double variation = Variation;

            bool shouldChange = false;
            for (int i = 0; i < Children.Count; i++)
            {
                shouldChange = updateElement(
                    (Data)Children[i].GetValue(DataProperty),
                    dampening,
                    attractionFactor,
                    variation) || shouldChange;
            }

            if (shouldChange)
            {
                InvalidateArrange();
            }
            else
            {
                _listener.StopListening();
            }
        }

        private static bool updateElement(Data data, double dampening, double attractionFactor, double variation)
        {
            Assert.IsTrue(dampening > 0 && dampening < 1);
            Assert.IsTrue(attractionFactor > 0 && !double.IsInfinity(attractionFactor));

            Point current = data.ChildLocation;
            Point target = data.ChildTarget;
            Vector velocity = data.Velocity;

            Vector diff = target - current;

            if (diff.Length > Diff || velocity.Length > Diff)
            {
                // Apply dampening
                velocity.X *= (1 - dampening);
                velocity.Y *= (1 - dampening);

                // yeild a value within 1 +/- (variation/2)
                double itemVariation = 1 + (variation * (data.Random - .5));

                // Apply force
                velocity += diff * attractionFactor * itemVariation * .01;

                // Limit velocity to 'terminal velocity'
                velocity *= (velocity.Length > TerminalVelocity) ? (TerminalVelocity / velocity.Length) : 1;

                // Apply velocity to location
                current += velocity;

                data.Velocity = velocity;
                data.ChildLocation = current;
                return true;
            }

            data.ChildLocation = data.ChildTarget;
            data.Velocity = new Vector();
            return false;
        }

        // Given a child index, child size and children per row, figure out where the child goes
        private static Point _CalculateChildOffset(
            int index,
            int childrenPerRow,
            double itemWidth,
            double itemHeight,
            double panelWidth,
            int totalChildren)
        {
            double fudge = 0;
            if (totalChildren > childrenPerRow)
            {
                fudge = (panelWidth - childrenPerRow * itemWidth) / childrenPerRow;
                Assert.IsTrue(fudge >= 0);
            }

            int row = index / childrenPerRow;
            int column = index % childrenPerRow;
            return new Point(.5 * fudge + column * (itemWidth + fudge), row * itemHeight);
        }

        private static DependencyProperty CreateDoubleDP(
            string name,
            double defaultValue,
            FrameworkPropertyMetadataOptions metadataOptions,
            double minValue,
            double maxValue,
            bool attached)
        {
            return CreateDoubleDP(name, defaultValue, metadataOptions, minValue, false, maxValue, false, attached);
        }

        private static DependencyProperty CreateDoubleDP(
            string name,
            double defaultValue,
            FrameworkPropertyMetadataOptions metadataOptions,
            double minValue,
            bool includeMin,
            double maxValue,
            bool includeMax,
            bool attached)
        {
            if (double.IsNaN(minValue))
            {
                Verify.IsTrue(double.IsNaN(maxValue), "maxValue", "If minValue is NaN, then maxValue must be.");

                if (attached)
                {
                    return DependencyProperty.RegisterAttached(
                        name,
                        typeof(double),
                        typeof(AnimatingTilePanel),
                        new FrameworkPropertyMetadata(defaultValue, metadataOptions));
                }

                return DependencyProperty.Register(
                    name,
                    typeof(double),
                    typeof(AnimatingTilePanel),
                    new FrameworkPropertyMetadata(defaultValue, metadataOptions));
            }

            Verify.IsTrue(!double.IsNaN(maxValue), "maxValue", "maxValue is not a number.");
            Verify.IsTrue(maxValue >= minValue, "maxValue", "minValue is not less than maxValue.");

            ValidateValueCallback validateValueCallback = objValue =>
            {
                var value = (double) objValue;
                if (includeMin)
                {
                    if (value < minValue)
                    {
                        return false;
                    }
                }
                else
                {
                    if (value <= minValue)
                    {
                        return false;
                    }
                }
                if (includeMax)
                {
                    if (value > maxValue)
                    {
                        return false;
                    }
                }
                else
                {
                    if (value >= maxValue)
                    {
                        return false;
                    }
                }
                return true;
            };

            if (attached)
            {
                return DependencyProperty.RegisterAttached(
                    name,
                    typeof(double),
                    typeof(AnimatingTilePanel),
                    new FrameworkPropertyMetadata(defaultValue, metadataOptions), validateValueCallback);
            }
            
            return DependencyProperty.Register(
                name,
                typeof(double),
                typeof(AnimatingTilePanel),
                new FrameworkPropertyMetadata(defaultValue, metadataOptions), validateValueCallback);
        }

        #endregion

        private bool m_appliedTemplate;

        private readonly CompositionTargetRenderingListener _listener = new CompositionTargetRenderingListener();

        private static readonly DependencyProperty DataProperty =
            DependencyProperty.RegisterAttached("Data", typeof(Data), typeof(AnimatingTilePanel));

        private const double Diff = 0.1;
        private const double TerminalVelocity = 10000;

        #endregion

    }
}