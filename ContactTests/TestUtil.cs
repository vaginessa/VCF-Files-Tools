/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Communications.Contacts.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Win32;
    using Standard;

    public static class TestUtil
    {
        /// <summary>
        /// CAUTION: Potentially results in data loss!
        /// Completely remove all remains of a ContactManager in the root.
        /// </summary>
        /// <param name="rootDirectory">The root of the ContactManager to purge.</param>
        /// <remarks>
        /// Removes all files, contacts or not, under the folder.
        /// Removes the folder.
        /// Removes the registry keys associated with the ContactManager.
        /// </remarks>
        public static void PurgeContactManager(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory))
            {
                return;
            }

            // Do not call this on the root directory.
            rootDirectory = ContactUtil.ExpandRootDirectory(rootDirectory);

            // Because of the nature of this do not call this for the user's root Contacts folder.
            Assert.AreNotEqual(ContactUtil.GetContactsFolder(), rootDirectory);

            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, true);
            }

            SetMeRegistryValue(rootDirectory, "");
        }

        private static string _contactsFolder;

        public static string GetContactsFolder()
        {
            if (null == _contactsFolder)
            {
                using (ContactManager cm = new ContactManager())
                {
                    _contactsFolder = cm.RootDirectory;
                }
            }
            return _contactsFolder;
        }

        private const string _MeRegKey = @"Software\Microsoft\WAB\Me";

        public static void SetMeRegistryValue(string rootDirectory, string value)
        {
            string key = ContactUtil.ExpandRootDirectory(rootDirectory);
            // If this is the root Contacts folder then we share the Me contact with Windows.
            if (key.Equals(ContactUtil.GetContactsFolder(), StringComparison.OrdinalIgnoreCase))
            {
                key = "";
            }

            // Open the key for write.  CreateSubKey tends to return null on failure due to a missing key.
            RegistryKey hKey = Registry.CurrentUser.CreateSubKey(_MeRegKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (null != hKey)
            {
                try
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        hKey.DeleteValue(key, false);
                    }
                    else
                    {
                        hKey.SetValue(key, value, RegistryValueKind.String);
                    }
                }
                finally
                {
                    hKey.Close();
                }
            }
        }

        public static string GetMeRegistryValue(string rootDirectory)
        {
            string key = ContactUtil.ExpandRootDirectory(rootDirectory);
            // If this is the root Contacts folder then we share the Me contact with Windows.
            if (key.Equals(ContactUtil.GetContactsFolder(), StringComparison.OrdinalIgnoreCase))
            {
                key = "";
            }

            // Open the key read-only.  If it doesn't exist OpenSubKey tends to return null.
            RegistryKey hKey = Registry.CurrentUser.OpenSubKey(_MeRegKey, false);
            if (null != hKey)
            {
                try
                {
                    return hKey.GetValue(key, "") as string;
                }
                finally
                {
                    hKey.Close();
                }
            }
            return "";
        }

        public static Dictionary<string, string> BackupAndPurgeMeRegistryKeys()
        {
            RegistryKey hKey = Registry.CurrentUser.OpenSubKey(_MeRegKey, false);
            // If the key isn't present just return null.
            if (null == hKey)
            {
                return null;
            }

            Dictionary<string, string> valuePairs = new Dictionary<string, string>();
            try
            {
                foreach (string name in hKey.GetValueNames())
                {
                    string value = hKey.GetValue(name) as string;
                    if (null != value)
                    {
                        valuePairs.Add(name, value);
                    }
                }
            }
            finally
            {
                hKey.Close();
            }

            Registry.CurrentUser.DeleteSubKey(_MeRegKey);

            return valuePairs;
        }

        public static void RestoreMeRegistryKeys(Dictionary<string, string> valuePairs)
        {
            foreach (KeyValuePair<string, string> pair in valuePairs)
            {
                string root = pair.Key;
                if (string.IsNullOrEmpty(pair.Key))
                {
                    root = "*";
                }
                SetMeRegistryValue(root, pair.Value);
            }
        }
    }
}
