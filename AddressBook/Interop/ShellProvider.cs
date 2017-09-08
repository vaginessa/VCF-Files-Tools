namespace Microsoft.Communications.Contacts.Widgets.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class ShellProvider
    {
        // Browsing for directory.
        private const uint BIF_RETURNONLYFSDIRS = 0x0001;  // For finding a folder to start document searching
        private const uint BIF_DONTGOBELOWDOMAIN = 0x0002;  // For starting the Find Computer
        private const uint BIF_STATUSTEXT = 0x0004;  // Top of the dialog has 2 lines of text for BROWSEINFO.lpszTitle and one line if
        // this flag is set.  Passing the message BFFM_SETSTATUSTEXTA to the hwnd can set the
        // rest of the text.  This is not used with BIF_USENEWUI and BROWSEINFO.lpszTitle gets
        // all three lines of text.
        private const uint BIF_RETURNFSANCESTORS = 0x0008;
        private const uint BIF_EDITBOX = 0x0010;   // Add an editbox to the dialog
        private const uint BIF_VALIDATE = 0x0020;   // insist on valid result (or CANCEL)

        private const uint BIF_NEWDIALOGSTYLE = 0x0040;   // Use the new dialog layout with the ability to resize
        // Caller needs to call OleInitialize() before using this API
        private const uint BIF_USENEWUI = 0x0040 + 0x0010; //(BIF_NEWDIALOGSTYLE | BIF_EDITBOX);

        private const uint BIF_BROWSEINCLUDEURLS = 0x0080;   // Allow URLs to be displayed or entered. (Requires BIF_USENEWUI)
        private const uint BIF_UAHINT = 0x0100;   // Add a UA hint to the dialog, in place of the edit box. May not be combined with BIF_EDITBOX
        private const uint BIF_NONEWFOLDERBUTTON = 0x0200;   // Do not add the "New Folder" button to the dialog.  Only applicable with BIF_NEWDIALOGSTYLE.
        private const uint BIF_NOTRANSLATETARGETS = 0x0400;  // don't traverse target as shortcut

        private const uint BIF_BROWSEFORCOMPUTER = 0x1000;  // Browsing for Computers.
        private const uint BIF_BROWSEFORPRINTER = 0x2000;// Browsing for Printers
        private const uint BIF_BROWSEINCLUDEFILES = 0x4000; // Browsing for Everything
        private const uint BIF_SHAREABLE = 0x8000;  // sharable resources displayed (remote shares, requires BIF_USENEWUI)

        [DllImport("shell32.dll")]
        private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHGetPathFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);

        private delegate int BrowseCallBackProc(IntPtr hwnd, int msg, IntPtr lp, IntPtr wp);

        private struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string pszDisplayName;
            public string lpszTitle;
            public uint ulFlags;
            public BrowseCallBackProc lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        public static string SelectFolder(string caption, string initialPath)
        {
            IntPtr pidl = IntPtr.Zero;
            try
            {
                BROWSEINFO bi;
                bi.hwndOwner = IntPtr.Zero;
                bi.pidlRoot = IntPtr.Zero;
                bi.pszDisplayName = initialPath;
                bi.lpszTitle = caption;
                bi.ulFlags = BIF_NEWDIALOGSTYLE | BIF_SHAREABLE;
                bi.lpfn = null;
                bi.lParam = IntPtr.Zero;
                bi.iImage = 0;
                pidl = SHBrowseForFolder(ref bi);
                if (IntPtr.Zero == pidl)
                {
                    // Can assume the user canceled the dialog.
                    return null;
                }
                StringBuilder sb = new StringBuilder(256);
                if (0 == SHGetPathFromIDList(pidl, sb))
                {
                    throw new Exception("Failed to get the FolderName from the PIDL");
                }
                return sb.ToString();
            }
            finally
            {
                Marshal.FreeCoTaskMem(pidl);
            }
        }
    }
}
