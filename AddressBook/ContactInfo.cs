/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Widgets
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using System.ComponentModel;
    using Standard;

    public class ContactInfo : ContactView, INotifyPropertyChanged
    {
        private NameBuilder _nameBuilder;
        private PhysicalAddressBuilder _workAddressBuilder;
        private PhysicalAddressBuilder _homeAddressBuilder;
        private EmailAddressBuilder _email;
        private PositionBuilder _job;
        private PhoneNumberBuilder _homePhone;
        private PhoneNumberBuilder _homeFax;
        private PhoneNumberBuilder _workPhone;
        private PhoneNumberBuilder _workFax;
        private PhoneNumberBuilder _cellPhone;
        private PhoneNumberBuilder _pager;
        private PhotoBuilder _userTile;
        private string _notes;
        private string _workWebsite;
        private string _personalWebsite;
        private string _webFeed;

        private void _OnPropertyChanged(string propertyName)
        {
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public PhoneNumberBuilder HomePhone
        {
            get { return _homePhone; }
        }

        public PhoneNumberBuilder HomeFax
        {
            get { return _homeFax; }
        }

        public PhoneNumberBuilder WorkPhone
        {
            get { return _workPhone; }
        }

        public PhoneNumberBuilder WorkFax
        {
            get { return _workFax; }
        }

        public PhoneNumberBuilder Cellular
        {
            get { return _cellPhone; }
        }

        public PhoneNumberBuilder Pager
        {
            get { return _pager; }
        }

        public NameBuilder Name
        {
            get { return _nameBuilder; }
        }

        public PositionBuilder Job
        {
            get { return _job; }
        }

        public PhysicalAddressBuilder WorkAddress
        {
            get { return _workAddressBuilder; }
        }

        public PhysicalAddressBuilder HomeAddress
        {
            get { return _homeAddressBuilder; }
        }

        public EmailAddressBuilder Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public PhotoBuilder UserTile
        {
            get { return _userTile; }
        }

        public string WorkWebsite
        {
            get { return _workWebsite; }
            set
            {
                if (_workWebsite != value)
                {
                    _workWebsite = value;
                    _OnPropertyChanged("WorkWebsite");
                }
            }
        }

        public string PersonalWebsite
        {
            get { return _personalWebsite; }
            set
            {
                if (_personalWebsite != value)
                {
                    _personalWebsite = value;
                    _OnPropertyChanged("PersonalWebsite");
                }
            }
        }

        public string WebFeed
        {
            get { return _webFeed; }
            set
            {
                if (_webFeed != value)
                {
                    _webFeed = value;
                    _OnPropertyChanged("WebFeed");
                }
            }
        }

        public string Notes
        {
            get { return _notes; }
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    _OnPropertyChanged("Notes");
                }
            }
        }

        public ContactInfo(Contact contact)
            : base(contact)
        {
            _nameBuilder = new NameBuilder(Source.Names.Default);
            _homeAddressBuilder = new PhysicalAddressBuilder(Source.Addresses[PropertyLabels.Personal]);
            _workAddressBuilder = new PhysicalAddressBuilder(Source.Addresses[PropertyLabels.Business]);
            _job = new PositionBuilder(Source.Positions[PropertyLabels.Business]);
            _homePhone = new PhoneNumberBuilder(Source.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Voice]);
            _workPhone = new PhoneNumberBuilder(Source.PhoneNumbers[PropertyLabels.Business, PhoneLabels.Voice]);
            _homeFax = new PhoneNumberBuilder(Source.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Fax]);
            _workFax = new PhoneNumberBuilder(Source.PhoneNumbers[PropertyLabels.Business, PhoneLabels.Fax]);
            _pager = new PhoneNumberBuilder(Source.PhoneNumbers[PhoneLabels.Pager]);
            _cellPhone = new PhoneNumberBuilder(Source.PhoneNumbers[PhoneLabels.Cellular]);
            _email = new EmailAddressBuilder(Source.EmailAddresses.Default);
            _notes = Source.Notes;
            _userTile = new PhotoBuilder(Source.Photos[PhotoLabels.UserTile]);
            _userTile.PropertyChanged += _OnUserTileChanged;

            Uri uri = Source.Urls[PropertyLabels.Business];
            if (null != uri)
            {
                _workWebsite = uri.ToString();
            }
            uri = Source.Urls[PropertyLabels.Personal];
            if (null != uri)
            {
                _personalWebsite = uri.ToString();
            }
            uri = Source.Urls[UrlLabels.Rss];
            if (null != uri)
            {
                _webFeed = uri.ToString();
            }
        }

        private void _OnUserTileChanged(object sender, PropertyChangedEventArgs e)
        {
            // If any property in the user tile changes then notify changes by the UserTile name.
            _OnPropertyChanged("UserTile");
        }

        public void SaveToSource()
        {
            if (string.IsNullOrEmpty(Name.FormattedName))
            {
                Name.FormattedName = Microsoft.Communications.Contacts.Name.FormatName(Name.GivenName, Name.MiddleName, Name.FamilyName, NameCatenationOrder.GivenMiddleFamily);
            }

            Source.Names.Default = _nameBuilder;
            Source.Addresses[PropertyLabels.Business] = _workAddressBuilder;
            Source.Addresses[PropertyLabels.Personal] = _homeAddressBuilder;
            Source.Positions[PropertyLabels.Business] = _job;
            Source.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Voice] = _homePhone;
            Source.PhoneNumbers[PropertyLabels.Business, PhoneLabels.Voice] = _workPhone;
            Source.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Fax] = _homeFax;
            Source.PhoneNumbers[PropertyLabels.Business, PhoneLabels.Fax] = _workFax;
            Source.PhoneNumbers[PropertyLabels.Personal, PhoneLabels.Voice] = _homePhone;
            Source.PhoneNumbers[PhoneLabels.Pager] = _pager;
            Source.PhoneNumbers[PhoneLabels.Cellular] = _cellPhone;
            Source.EmailAddresses.Default = Email;
            Source.Notes = Notes;
            Source.Photos[PhotoLabels.UserTile] = UserTile;
            
            Uri uri;
            Uri.TryCreate(WorkWebsite, UriKind.RelativeOrAbsolute, out uri);
            Source.Urls[PropertyLabels.Business] = uri;
            Uri.TryCreate(PersonalWebsite, UriKind.RelativeOrAbsolute, out uri);
            Source.Urls[PropertyLabels.Personal] = uri;
            Uri.TryCreate(WebFeed, UriKind.RelativeOrAbsolute, out uri);
            Source.Urls[UrlLabels.Rss] = uri;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
