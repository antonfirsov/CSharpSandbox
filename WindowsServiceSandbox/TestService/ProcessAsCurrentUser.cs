using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace TestService
{
    public class ProcessAsCurrentUser
    {
        private ILogger _logger;

        public ProcessAsCurrentUser(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Connection state of a session.
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>
            /// A user is logged on to the session.
            /// </summary>
            Active,
            /// <summary>
            /// A client is connected to the session.
            /// </summary>
            Connected,
            /// <summary>
            /// The session is in the process of connecting to a client.
            /// </summary>
            ConnectQuery,
            /// <summary>
            /// This session is shadowing another session.
            /// </summary>
            Shadowing,
            /// <summary>
            /// The session is active, but the client has disconnected from it.
            /// </summary>
            Disconnected,
            /// <summary>
            /// The session is waiting for a client to connect.
            /// </summary>
            Idle,
            /// <summary>
            /// The session is listening for connections.
            /// </summary>
            Listening,
            /// <summary>
            /// The session is being reset.
            /// </summary>
            Reset,
            /// <summary>
            /// The session is down due to an error.
            /// </summary>
            Down,
            /// <summary>
            /// The session is initializing.
            /// </summary>
            Initializing
        }


        [StructLayout(LayoutKind.Sequential)]
        class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
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

        enum LOGON_TYPE
        {
            LOGON32_LOGON_INTERACTIVE = 2,
            LOGON32_LOGON_NETWORK,
            LOGON32_LOGON_BATCH,
            LOGON32_LOGON_SERVICE,
            LOGON32_LOGON_UNLOCK = 7,
            LOGON32_LOGON_NETWORK_CLEARTEXT,
            LOGON32_LOGON_NEW_CREDENTIALS
        }

        enum LOGON_PROVIDER
        {
            LOGON32_PROVIDER_DEFAULT,
            LOGON32_PROVIDER_WINNT35,
            LOGON32_PROVIDER_WINNT40,
            LOGON32_PROVIDER_WINNT50
        }

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

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public int SessionID;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string WinStationName;
            public ConnectionState State;
        }

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Int32 WTSEnumerateSessions(IntPtr hServer, int reserved, int version,
            ref IntPtr sessionInfo, ref int count);


        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUserW", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            UInt32 dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr memory);

        [DllImport("kernel32.dll")]
        private static extern UInt32 WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern int WTSQueryUserToken(UInt32 sessionId, out IntPtr Token);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int ImpersonationLevel,
            int TokenType,
            out IntPtr phNewToken);

        private const int TokenImpersonation = 2;
        private const int SecurityIdentification = 1;
        private const int MAXIMUM_ALLOWED = 0x2000000;
        private const int TOKEN_DUPLICATE = 0x2;
        private const int TOKEN_QUERY = 0x00000008;

        /// <summary>
        /// Launches a process for the current logged on user if there are any.
        /// If none, return false as well as in case of 
        /// 
        /// ##### !!! BEWARE !!! ####  ------------------------------------------
        /// This code will only work when running in a windows service (where it is really needed)
        /// so in case you need to test it, it needs to run in the service. Reason
        /// is a security privileg which only services have (SE_??? something, cant remember)!
        /// </summary>
        /// <param name="processExe"></param>
        /// <returns></returns>
        public bool CreateProcessAsCurrentUser(string processExe)
        {

            IntPtr duplicate = new IntPtr();
            STARTUPINFO info = new STARTUPINFO();
            PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();

            _logger.Log(string.Format("CreateProcessAsCurrentUser. processExe: " + processExe));

            IntPtr p = GetCurrentUserToken();

            bool result = DuplicateTokenEx(p, MAXIMUM_ALLOWED | TOKEN_QUERY | TOKEN_DUPLICATE, IntPtr.Zero, SecurityIdentification, SecurityIdentification, out duplicate);
            _logger.Log(string.Format("DuplicateTokenEx result: {0}", result));
            _logger.Log(string.Format("duplicate: {0}", duplicate));


            if (result)
            {
                IntPtr env = IntPtr.Zero;
                if (CreateEnvironmentBlock(out env, duplicate, false))
                {
                    _logger.Log("Env: " + env);

                    result = CreateProcessAsUser(duplicate, processExe, null,
                        IntPtr.Zero, IntPtr.Zero, false, (UInt32)CreateProcessFlags.CREATE_NEW_CONSOLE, env, null,
                        ref info, out procInfo);
                    _logger.Log(string.Format("CreateProcessAsUser result: {0}", result));
                }
                else
                {
                    _logger.Log("CreateEnvironmentBlock() failed");
                }
            }

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.Log("Marshal.GetLastWin32Error(): " + error);
            }
            

            if (p.ToInt32() != 0)
            {
                Marshal.Release(p);
                _logger.Log(string.Format("Released handle p: {0}", p));
            }


            if (duplicate.ToInt32() != 0)
            {
                Marshal.Release(duplicate);
                _logger.Log(string.Format("Released handle duplicate: {0}", duplicate));
            }



            return result;
        }

        public int GetCurrentSessionId()
        {
            uint sessionId = WTSGetActiveConsoleSessionId();
            _logger.Log(string.Format("sessionId: {0}", sessionId));

            if (sessionId == 0xFFFFFFFF)
                return -1;
            else
                return (int)sessionId;
        }

        public bool IsUserLoggedOn()
        {
            List<WTS_SESSION_INFO> wtsSessionInfos = ListSessions();
            _logger.Log(string.Format("Number of sessions: {0}", wtsSessionInfos.Count));
            return wtsSessionInfos.Where(x => x.State == ConnectionState.Active).Count() > 0;
        }

        private IntPtr GetCurrentUserToken()
        {
            List<WTS_SESSION_INFO> wtsSessionInfos = ListSessions();
            //int sessionId = wtsSessionInfos.Where(x => x.State == ConnectionState.Active).FirstOrDefault().SessionID;
            int sessionId = GetCurrentSessionId();

            _logger.Log(string.Format("sessionId: {0}", sessionId));
            if (sessionId == int.MaxValue)
            {
                return IntPtr.Zero;
            }
            else
            {
                IntPtr p = new IntPtr();
                int result = WTSQueryUserToken((UInt32)sessionId, out p);
                _logger.Log(string.Format("WTSQueryUserToken result: {0}", result));
                _logger.Log(string.Format("WTSQueryUserToken p: {0}", p));

                return p;
            }
        }

        [DllImport("userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        public List<WTS_SESSION_INFO> ListSessions()
        {
            IntPtr server = IntPtr.Zero;
            List<WTS_SESSION_INFO> ret = new List<WTS_SESSION_INFO>();

            try
            {
                IntPtr ppSessionInfo = IntPtr.Zero;

                Int32 count = 0;
                Int32 retval = WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref ppSessionInfo, ref count);
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

                Int64 current = (int)ppSessionInfo;

                if (retval != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
                        current += dataSize;

                        ret.Add(si);
                    }

                    WTSFreeMemory(ppSessionInfo);
                }
            }
            catch (Exception exception)
            {
                _logger.Log(exception.ToString());
            }

            return ret;
        }

    }
}