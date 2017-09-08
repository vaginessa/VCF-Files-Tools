/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Windows;
    using System.Windows.Data;
    using System.Xml;
    using Standard;

    internal class FrameUserTileConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return UserTile.GetFramedPhoto((Photo)(PhotoBuilder)value, 96);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class IsUserTilePresentConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((Photo)(PhotoBuilder)value).Equals(default(Photo));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class HideableTextConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != value)
            {
                var str = ((string)value).Trim();
                if (str.Length != 0)
                {
                    str += " (" + (string)parameter + ")";
                    return str;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class RssTitleFromUriConverter : IValueConverter
    {
        #region IValueConverter Members

        private static readonly HideableTextConverter _secondPassConverter = new HideableTextConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = (string)value;
            // Worst case we'll just give back the URL again.
            string retTitle = url;
            if (!string.IsNullOrEmpty(url))
            {
                // Value should be a URI.
                var xmlDocument = new XmlDocument();
                try
                {
                    using (var webClient = new WebClient())
                    {
                        using (Stream rssStream = webClient.OpenRead(url))
                        {
                            xmlDocument.Load(new XmlTextReader(new StreamReader(rssStream)));
                        }
                    }
                }
                catch (WebException)
                {
                    return _secondPassConverter.Convert(retTitle, typeof(string), parameter, culture);
                }
                catch (XmlException)
                {
                    return _secondPassConverter.Convert(retTitle, typeof(string), parameter, culture);
                }

                // Have some XML.  Find the <channel> node under the <rss> node
                XmlNode rssNode = xmlDocument.SelectSingleNode("rss");
                if (null != rssNode)
                {
                    XmlNode channelNode = rssNode.SelectSingleNode("channel");
                    if (null != channelNode)
                    {
                        // Far enough along to cache the Feed's Title.
                        XmlNode channelTitle = channelNode.SelectSingleNode("title");
                        if (null != channelTitle && !string.IsNullOrEmpty(channelTitle.InnerText))
                        {
                            retTitle = channelTitle.InnerText;
                        }

                        // Get the most recent <item> node.
                        XmlNode itemNode = channelNode.SelectSingleNode("item");
                        if (null != itemNode)
                        {
                            XmlNode itemTitle = itemNode.SelectSingleNode("title");
                            if (null != itemTitle && !string.IsNullOrEmpty(itemTitle.InnerText))
                            {
                                retTitle = itemTitle.InnerText;
                            }
                        }
                    }
                }
            }
            return _secondPassConverter.Convert(retTitle, typeof(string), parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class AdjustDoubleConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Assert.AreEqual(typeof(string), parameter.GetType());
            var originalValue = (double)value;
            var delta = double.Parse((string)parameter);
            return originalValue + delta;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}