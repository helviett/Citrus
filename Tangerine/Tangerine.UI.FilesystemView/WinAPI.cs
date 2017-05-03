using System;
using System.Runtime.InteropServices;

namespace Tangerine.UI.FilesystemView
{
	class WinAPI
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct SHFILEINFO
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		};

		public const uint SHGFI_ICON = 0x100;
		public const uint SHGFI_LARGEICON = 0x0;	// 'Large icon
		public const uint SHGFI_SMALLICON = 0x1;	// 'Small icon
		public const uint SHGFI_ADDOVERLAYS = 0x000000020;

		[DllImport("shell32.dll")]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

		[DllImport("user32")]
		public static extern int DestroyIcon(IntPtr hIcon);
	}
}