
namespace Microsoft.Communications.Contacts
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using Standard;
    using Standard.Interop;

    internal static class ContactUtil
    {
        private static string _assemblyName;

        public static string ExpandRootDirectory(string rootDirectory)
        {
            if (rootDirectory.StartsWith("*", StringComparison.Ordinal))
            {
                rootDirectory = Path.Combine(GetContactsFolder(), rootDirectory.Substring(1).TrimStart('\\', '/'));
                Assert.IsTrue(Path.IsPathRooted(rootDirectory));
            }

            // This might throw, but I want that exception to propagate out.
            return Path.GetFullPath(rootDirectory).TrimEnd('\\', '/');
        }

        // Ideally the Environment class should be able to do this, but Contacts in Vista
        // is newer than the last rev of these .Net APIs.  Maybe next time...
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static string GetContactsFolder()
        {
            bool vistaOrNewer = Environment.OSVersion.Version.Major >= 6;

            if (vistaOrNewer)
            {
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    const int KF_FLAG_CREATE = 0x00008000;
                    var FOLDERID_Contacts = new Guid("56784854-C6CB-462b-8169-88E350ACB882");
                    NativeMethods.SHGetKnownFolderPath(ref FOLDERID_Contacts, KF_FLAG_CREATE, IntPtr.Zero, out ptr);
                    return Marshal.PtrToStringUni(ptr);
                }
                catch (EntryPointNotFoundException)
                {
                    // Funny... Don't fail this function.  Fallback to the legacy implementation.
                    Assert.Fail();
                }
                finally
                {
                    Utility.SafeCoTaskMemFree(ref ptr);
                }
            }

            // The folder name "Contacts" doesn't get localized, even on Vista.  Just the display of it does.
            // Since we're presumably not on Vista, don't need to worry about folder redirection.  Just put it
            // under the user's profile folder (which, again, isn't exposed to .Net).

            // On XP it would be more appropriate to put it under the "My Documents" directory, or even an AppData.
            // Not putting it in My Documents on principle.
            // Keeping the same folder hierarchy as Vista for migration purposes.

            // CONSIDER: Caching this in the registry so users can override the default location
            // when folder redirection isn't available.
            var sb = new StringBuilder((int)Win32Value.MAX_PATH);

            const int CSIDL_PROFILE = 0x0028;
            const int SHGFP_TYPE_CURRENT = 0;
            NativeMethods.SHGetFolderPath(IntPtr.Zero, CSIDL_PROFILE, IntPtr.Zero, SHGFP_TYPE_CURRENT, sb);

            return Path.Combine(sb.ToString(), "Contacts");
        }

        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "app")]
        public static Uri GetResourceUri(string resourceName)
        {
            Assert.IsNeitherNullNorEmpty(resourceName);

            if (null == _assemblyName)
            {
                // WPF Dlls need to be loaded for the pack: uri syntax to work.
                var app = System.Windows.Application.Current;

                _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            }

            return new Uri(@"pack://application:,,,/" + _assemblyName + @";Component/Files/" + resourceName);
        }
    }
}
