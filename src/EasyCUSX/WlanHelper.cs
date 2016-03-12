using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;

namespace WlanHelper
{
    /// <summary >
    /// This class encapsulates basic functions required
    /// for enumerating wireless adapaters in the system. This class consumes 
    /// wifi api developed and supported from Microsoft Vista onwards
    /// http://www.codeproject.com/Articles/35329/How-to-access-wireless-network-parameters-using-na
    /// </summary >
    class WlanHelperMain
    {
        public class CookieWebClient : WebClient
        {
            public CookieContainer CookieContainer { get; private set; }

            /// <summary>
            /// This will instanciate an internal CookieContainer.
            /// </summary>
            public CookieWebClient()
            {
                this.CookieContainer = new CookieContainer();
            }

            /// <summary>
            /// Use this if you want to control the CookieContainer outside this class.
            /// </summary>
            public CookieWebClient(CookieContainer cookieContainer)
            {
                this.CookieContainer = cookieContainer;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address) as HttpWebRequest;
                if (request == null) return base.GetWebRequest(address);
                request.CookieContainer = CookieContainer;
                return request;
            }
        }
        public static int AUTH_SUCCESS = 0;
        public static int AUTH_ALREADY = 1;

        public static int AUTH_IDENTIFYERROR = 2;
        public static int AUTH_FREEZE = 3;
        public static int AUTH_OVERDEVICE = 4;

        public static int AUTH_UNREACHABLE = 5;
        public static int AUTH_TOKEN_MATCH_FAILED = 6;

        public static int AUTH_UNKNOWN_TYPE = 7;
        public static int AUTH_EXCEPTION = 8;


        public static int DEAUTH_SUCCESS = 0;
        public static int DEAUTH_ALREADY = 1;
        public static int DEAUTH_IDENTIFYERROR = 2;

        public static int DEAUTH_UNREACHABLE = 3;
        public static int DEAUTH_TOKEN_MATCH_FAILED = 4;

        public static int DEAUTH_UNKNOWN_TYPE = 5;
        public static int DEAUTH_EXCEPTION = 6;


        public static bool checkGateway(out string Result)
        {
            string rst;
            Ping ping = new Ping();
            PingReply pingReply = ping.Send("172.18.4.3");
            bool ok = (pingReply.Status == IPStatus.Success);
            if (!ok)
            {
                rst = "请先连接到 \"CUSX\" 无线接入点";
            }
            else
            {
                rst = "OK!";
            }
            Result = rst;
            return ok;
        }

        public static int auth(string username, string password)
        {
            try
            {
                string token;
                //get cookie and token
                var client = new CookieWebClient();
                string html = Encoding.UTF8.GetString(client.DownloadData("http://172.18.4.3/login.php"));
                Regex re = new Regex("<input.+?name=\"token\".+?value=\"(.+?)\">");
                Match m = re.Match(html);
                if (!m.Success)
                {
                    return AUTH_TOKEN_MATCH_FAILED;
                }
                token = m.Groups[1].Value;

                html = Encoding.UTF8.GetString(client.UploadValues("http://172.18.4.3/login.php?action=login", "POST",
                    new System.Collections.Specialized.NameValueCollection{
                {"token", token},
                {"username", username},
                {"password", password},
                {"type", ""}
                }));

                if (html.Contains("成功登陆，祝您冲浪愉快"))
                {
                    return AUTH_SUCCESS;
                }
                else if (html.Contains("您的计算机已经登陆"))
                {
                    return AUTH_ALREADY;
                }
                else if (html.Contains("您的帐号已经登录,或者登录数超过限制"))
                {
                    return AUTH_OVERDEVICE;
                }
                else if (html.Contains("您的帐号已经被冻结"))
                {
                    return AUTH_FREEZE;
                }
                else if (html.Contains("不正确的用户名或密码"))
                {
                    return AUTH_IDENTIFYERROR;
                }
                else
                {
                    return AUTH_UNKNOWN_TYPE;
                }
            }
            catch (Exception)
            {
                return AUTH_EXCEPTION;
            }
        }

        public static int deAuth(string username, string password)
        {
            try
            {
                string token;
                //get cookie and token
                var client = new CookieWebClient();
                string html = Encoding.UTF8.GetString(client.DownloadData("http://172.18.4.3/login.php"));
                Regex re = new Regex("<input.+?name=\"token\".+?value=\"(.+?)\">");
                Match m = re.Match(html);
                if (!m.Success)
                {
                    return DEAUTH_TOKEN_MATCH_FAILED;
                }
                token = m.Groups[1].Value;

                html = Encoding.UTF8.GetString(client.UploadValues("http://172.18.4.3/login.php?action=logout", "POST",
                    new System.Collections.Specialized.NameValueCollection{
                {"token", token},
                {"username", username},
                {"password", password},
                {"type", ""}
                }));

                if (html.Contains("您下网了"))
                {
                    return DEAUTH_SUCCESS;
                }
                else if (html.Contains("您不在网上"))
                {
                    return DEAUTH_ALREADY;
                }
                else if (html.Contains("不正确的用户名和密码"))
                {
                    return DEAUTH_IDENTIFYERROR;
                }
                else
                {
                    return AUTH_UNKNOWN_TYPE;
                }
            }
            catch (Exception)
            {
                return AUTH_EXCEPTION;
            }
        }




























        //************************************************************
        #region declarations

        private const int WLAN_API_VERSION_2_0 = 2; //Windows Vista WiFi API Version
        private const int ERROR_SUCCESS = 0;

        /// <summary >
        /// Opens a connection to the server
        /// </summary >
        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern UInt32 WlanOpenHandle(UInt32 dwClientVersion,
                IntPtr pReserved, out UInt32 pdwNegotiatedVersion,
                out IntPtr phClientHandle);

        /// <summary >
        /// Closes a connection to the server
        /// </summary >
        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern UInt32 WlanCloseHandle(IntPtr hClientHandle,
                                                     IntPtr pReserved);

        /// <summary >
        /// Enumerates all wireless interfaces in the laptop
        /// </summary >
        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern UInt32 WlanEnumInterfaces(IntPtr hClientHandle,
                       IntPtr pReserved, out IntPtr ppInterfaceList);

        /// <summary >
        /// Frees memory returned by native WiFi functions
        /// </summary >
        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern void WlanFreeMemory(IntPtr pmemory);

        /// <summary >
        /// Interface state enums
        /// </summary >
        public enum WLAN_INTERFACE_STATE : int
        {
            wlan_interface_state_not_ready = 0,
            wlan_interface_state_connected,
            wlan_interface_state_ad_hoc_network_formed,
            wlan_interface_state_disconnecting,
            wlan_interface_state_disconnected,
            wlan_interface_state_associating,
            wlan_interface_state_discovering,
            wlan_interface_state_authenticating
        };

        /// <summary >
        /// Stores interface info
        /// </summary >
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            /// GUID->_GUID
            public Guid InterfaceGuid;

            /// WCHAR[256]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;

            /// WLAN_INTERFACE_STATE->_WLAN_INTERFACE_STATE
            public WLAN_INTERFACE_STATE isState;
        }
        /// <summary >
        /// This structure Contains an array of NIC information
        /// </summary >
        [StructLayout(LayoutKind.Sequential)]
        public struct WLAN_INTERFACE_INFO_LIST
        {
            public Int32 dwNumberofItems;
            public Int32 dwIndex;
            public WLAN_INTERFACE_INFO[] InterfaceInfo;

            public WLAN_INTERFACE_INFO_LIST(IntPtr pList)
            {
                // The first 4 bytes are the number of WLAN_INTERFACE_INFO structures.
                dwNumberofItems = Marshal.ReadInt32(pList, 0);

                // The next 4 bytes are the index of the current item in the unmanaged API.
                dwIndex = Marshal.ReadInt32(pList, 4);

                // Construct the array of WLAN_INTERFACE_INFO structures.
                InterfaceInfo = new WLAN_INTERFACE_INFO[dwNumberofItems];

                for (int i = 0; i < dwNumberofItems; i++)
                {
                    // The offset of the array of structures is 8 bytes past the beginning.
                    // Then, take the index and multiply it by the number of bytes in the
                    // structure.
                    // the length of the WLAN_INTERFACE_INFO structure is 532 bytes - this
                    // was determined by doing a sizeof(WLAN_INTERFACE_INFO) in an
                    // unmanaged C++ app.
                    IntPtr pItemList = new IntPtr(pList.ToInt32() + (i * 532) + 8);

                    // Construct the WLAN_INTERFACE_INFO structure, marshal the unmanaged
                    // structure into it, then copy it to the array of structures.
                    WLAN_INTERFACE_INFO wii = new WLAN_INTERFACE_INFO();
                    wii = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(pItemList,
                        typeof(WLAN_INTERFACE_INFO));
                    InterfaceInfo[i] = wii;
                }
            }
        }

        #endregion

        //************************************************************
        #region Private Functions
        /// <summary >
        ///get NIC state  
        /// </summary >
        private string getStateDescription(WLAN_INTERFACE_STATE state)
        {
            string stateDescription = string.Empty;
            switch (state)
            {
                case WLAN_INTERFACE_STATE.wlan_interface_state_not_ready:
                    stateDescription = "not ready to operate";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_connected:
                    stateDescription = "connected";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_ad_hoc_network_formed:
                    stateDescription = "first node in an adhoc network";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_disconnecting:
                    stateDescription = "disconnecting";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_disconnected:
                    stateDescription = "disconnected";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_associating:
                    stateDescription = "associating";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_discovering:
                    stateDescription = "discovering";
                    break;
                case WLAN_INTERFACE_STATE.wlan_interface_state_authenticating:
                    stateDescription = "authenticating";
                    break;
            }

            return stateDescription;
        }
        #endregion

        //************************************************************
        #region Public Functions

        /// <summary >
        /// enumerate wireless network adapters using wifi api
        /// </summary >
        public void EnumerateNICs()
        {
            uint serviceVersion = 0;
            IntPtr handle = IntPtr.Zero;
            if (WlanOpenHandle(WLAN_API_VERSION_2_0, IntPtr.Zero,
                out serviceVersion, out handle) == ERROR_SUCCESS)
            {
                IntPtr ppInterfaceList = IntPtr.Zero;
                WLAN_INTERFACE_INFO_LIST interfaceList;

                if (WlanEnumInterfaces(handle, IntPtr.Zero,
                              out ppInterfaceList) == ERROR_SUCCESS)
                {
                    //Tranfer all values from IntPtr to WLAN_INTERFACE_INFO_LIST structure 
                    interfaceList = new WLAN_INTERFACE_INFO_LIST(ppInterfaceList);

                    Console.WriteLine("Enumerating Wireless Network Adapters...");
                    for (int i = 0; i < interfaceList.dwNumberofItems; i++)
                        Console.WriteLine("{0}-->{1}",
                          interfaceList.InterfaceInfo[i].strInterfaceDescription,
                          getStateDescription(interfaceList.InterfaceInfo[i].isState));

                    //frees memory
                    if (ppInterfaceList != IntPtr.Zero)
                        WlanFreeMemory(ppInterfaceList);
                }
                //close handle
                WlanCloseHandle(handle, IntPtr.Zero);
            }
        }
        #endregion
    }
}