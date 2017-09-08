/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

#if USE_VISTA_WRITER
namespace Microsoft.Communications.Contacts.Interop
{
    /// <summary>
    /// Defines the GUIDs for the COM interfaces used in this assembly.
    /// </summary>
    internal static class IIDGuid
    {
        /// <summary>IID_IPersist</summary>
        public const string IPersist                    = "0000010C-0000-0000-C000-000000000046";
        /// <summary>IID_IPersistStream</summary>
        public const string IPersistStream              = "00000109-0000-0000-C000-000000000046";
        /// <summary>IID_IContactProperties</summary>
        public const string IContactProperties          = "70DD27DD-5CBD-46E8-BEF0-23B6B346288F";
        /// <summary>IID_IContactPropertyCollection</summary>
        public const string IContactPropertyCollection  = "FFD3ADF8-FA64-4328-B1B6-2E0DB509CB3C";
    }

    /// <summary>
    /// Defines the GUIDs for the CLSIDs of COM objects used in this assembly.
    /// </summary>
    internal static class CLSIDGuid
    {
        /// <summary>CLSID_Contact</summary>
        public const string Contact                     = "61B68808-8EEE-4FD1-ACB8-3D804C8DB056";
    }
}
#endif