using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Steamworks
{
	internal static class Platform
    {
		public const int StructPlatformPackSize = 8;
		public const string LibraryName = "steam_api64";
		public const CallingConvention CC = CallingConvention.Cdecl;
		public const int StructPackSize = 4;
	}
}
