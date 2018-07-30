using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using TestService;

namespace UserProcess
{
    /// <summary>
    /// https://www.codeproject.com/Articles/36581/Interaction-between-services-and-applications-at-u
    /// </summary>
    public class ProcessStarter
    {
        public enum ActiveSessionRetrieavalMethod
        {
            WTSGetActiveConsoleSessionId,
            WTSEnumerateSessions
        }

        private ILogger _logger;

        #region Import Section

        [Flags]
        enum CreateProcessFlags : uint
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }

        private static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private static uint TOKEN_DUPLICATE = 0x0002;
        private static uint TOKEN_IMPERSONATE = 0x0004;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_QUERY_SOURCE = 0x0010;
        private static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private static uint TOKEN_ADJUST_GROUPS = 0x0040;
        private static uint TOKEN_ADJUST_DEFAULT = 0x0080;
        private static uint TOKEN_ADJUST_SESSIONID = 0x0100;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        private static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);

        
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WTSQueryUserToken(int sessionId, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out IntPtr newToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateProcessAsUser(IntPtr token, string applicationName, string commandLine, ref SECURITY_ATTRIBUTES processAttributes, ref SECURITY_ATTRIBUTES threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetLastError();

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int WTSEnumerateSessions(System.IntPtr hServer, int Reserved, int Version, ref System.IntPtr ppSessionInfo, ref int pCount);

        [DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = false)]
        private static extern void WTSFreeMemory(IntPtr memory);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        #endregion

        public ProcessStarter()
        {

        }

        public ProcessStarter(ILogger logger)
        {
            _logger = logger;
        }
        
        public static IntPtr GetCurrentUserToken(ActiveSessionRetrieavalMethod sessionRetrieavalMethod)
        {
            IntPtr currentToken = IntPtr.Zero;
            IntPtr primaryToken = IntPtr.Zero;

            int dwSessionId = sessionRetrieavalMethod == ActiveSessionRetrieavalMethod.WTSEnumerateSessions
                ? GetActiveSessionIdByEnumeration()
                : (int) WTSGetActiveConsoleSessionId();

            bool bRet = WTSQueryUserToken(dwSessionId, out currentToken);
            if (bRet == false)
            {
                return IntPtr.Zero;
            }

            bRet = DuplicateTokenEx(currentToken, TOKEN_ASSIGN_PRIMARY | TOKEN_ALL_ACCESS, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out primaryToken);
            if (bRet == false)
            {
                return IntPtr.Zero;
            }

            return primaryToken;
        }

        private static int GetActiveSessionIdByEnumeration()
        {
            int dwSessionId = 0;

            IntPtr pSessionInfo = IntPtr.Zero;
            int dwCount = 0;

            WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref pSessionInfo, ref dwCount);

            Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

            Int32 current = (int) pSessionInfo;
            for (int i = 0; i < dwCount; i++)
            {
                WTS_SESSION_INFO si =
                    (WTS_SESSION_INFO) Marshal.PtrToStructure((System.IntPtr) current, typeof(WTS_SESSION_INFO));
                if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                {
                    dwSessionId = si.SessionID;
                    break;
                }

                current += dataSize;
            }

            WTSFreeMemory(pSessionInfo);
            return dwSessionId;
        }

        public void Run(string processPath, string arguments, ActiveSessionRetrieavalMethod sessionRetrieavalMethod)
        {
            IntPtr primaryToken = GetCurrentUserToken(sessionRetrieavalMethod);
            if (primaryToken == IntPtr.Zero)
            {
                return;
            }
            STARTUPINFO StartupInfo = new STARTUPINFO();
            _processInfo = new PROCESS_INFORMATION();
            StartupInfo.cb = Marshal.SizeOf(StartupInfo);

            SECURITY_ATTRIBUTES Security1 = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES Security2 = new SECURITY_ATTRIBUTES();

            string command = "\"" + processPath + "\"";
            if (!string.IsNullOrEmpty(arguments))
            {
                command += " " + arguments;
            }

            IntPtr lpEnvironment = IntPtr.Zero;
            bool resultEnv = CreateEnvironmentBlock(out lpEnvironment, primaryToken, false);
            if (resultEnv != true)
            {
                int nError = GetLastError();
            }

            CreateProcessFlags flags = CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT;

            bool createProcessResult = CreateProcessAsUser(primaryToken, null, command, ref Security1, ref Security2,
                false, (uint)flags, lpEnvironment, null,
                ref StartupInfo, out _processInfo);

            if (!createProcessResult)
            {
                _logger.Log("CreateProcessAsUser failed!");
                _logger.Log("GetLastError: " + GetLastError());
            }


            DestroyEnvironmentBlock(lpEnvironment);
            CloseHandle(primaryToken);
        }

        private PROCESS_INFORMATION _processInfo;
    }
}
