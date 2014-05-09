﻿using System;
using System.Runtime.InteropServices;

namespace FastColoredTextBoxNS
{
    public static class PlatformType
    {
        private const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        private const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
/*
        private const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;
*/

        [DllImport("kernel32.dll")]
        private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        /// <summary>
        ///     Gets the Operating System Platform
        /// </summary>
        /// <returns></returns>
        public static Platform GetOperationSystemPlatform()
        {
            var sysInfo = new SYSTEM_INFO();

            // WinXP and older - use GetNativeSystemInfo
            if (Environment.OSVersion.Version.Major > 5 ||
                (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1))
            {
                GetNativeSystemInfo(ref sysInfo);
            }
                // else use GetSystemInfo
            else
            {
                GetSystemInfo(ref sysInfo);
            }

            switch (sysInfo.wProcessorArchitecture)
            {
                case PROCESSOR_ARCHITECTURE_IA64:
                case PROCESSOR_ARCHITECTURE_AMD64:
                    return Platform.X64;

                case PROCESSOR_ARCHITECTURE_INTEL:
                    return Platform.X86;

                default:
                    return Platform.Unknown;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public readonly ushort wProcessorArchitecture;
            private readonly ushort wReserved;
            private readonly uint dwPageSize;
            private readonly IntPtr lpMinimumApplicationAddress;
            private readonly IntPtr lpMaximumApplicationAddress;
            private readonly UIntPtr dwActiveProcessorMask;
            private readonly uint dwNumberOfProcessors;
            private readonly uint dwProcessorType;
            private readonly uint dwAllocationGranularity;
            private readonly ushort wProcessorLevel;
            private readonly ushort wProcessorRevision;
        };
    }

    public enum Platform
    {
        X86,
        X64,
        Unknown
    }
}