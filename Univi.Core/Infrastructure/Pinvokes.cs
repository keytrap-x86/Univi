using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Univi.Core.Infrastructure;
public partial class PInvokes
{
    [LibraryImport("Wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WTSQuerySessionInformationA(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);
    [LibraryImport("Wtsapi32.dll")]
    private static partial void WTSFreeMemory(IntPtr pointer);

    public enum WtsInfoClass
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType,
        WTSIdleTime,
        WTSLogonTime,
        WTSIncomingBytes,
        WTSOutgoingBytes,
        WTSIncomingFrames,
        WTSOutgoingFrames,
        WTSClientInfo,
        WTSSessionInfo,
    }

    [LibraryImport("netapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int NetWkstaUserEnum(
        string servername,
        int level,
        out IntPtr bufptr,
        int prefmaxlen,
        out int entriesread,
        out int totalentries,
        ref int resume_handle);


    public static bool TryGetUsernameBySessionId(int sessionId, bool prependDomain, [NotNullWhen(returnValue: true)] out string? username)
    {
        username = "SYSTEM";
        if (WTSQuerySessionInformationA(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out IntPtr buffer, out int strLen) && strLen > 1)
        {
            username = Marshal.PtrToStringAnsi(buffer) ?? "SYSTEM";
            WTSFreeMemory(buffer);
            if (prependDomain)
            {
                if (WTSQuerySessionInformationA(IntPtr.Zero, sessionId, WtsInfoClass.WTSDomainName, out buffer, out strLen) && strLen > 1)
                {
                    username = Marshal.PtrToStringAnsi(buffer) + "\\" + username;
                    WTSFreeMemory(buffer);
                }
            }
            return true;
        }
        return false;
    }
}
