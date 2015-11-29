using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
//Other
using DotRas;


namespace rasdialHelper
{
    class RasHelperMain
    {

        public bool DialUp(string _Username, string _Password, string _EntryName, out string _ResultMsg)
        {
            if (_Username == "" || _Password == "")
            {
                _ResultMsg = "请填写用户名密码";
                return false;
            }

            try
            {
                RasDialer dialer = new RasDialer();
                dialer.EntryName = _EntryName;
                dialer.PhoneNumber = "";
                dialer.AllowUseStoredCredentials = true;
                dialer.PhoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);
                dialer.Credentials = new NetworkCredential(_Username, _Password);
                dialer.Timeout = 1000;
                RasHandle myras = dialer.Dial();
                int i = 0;
                while (myras.IsInvalid)
                {
                    if (i == 3)
                    {
                        break;
                    }
                    i++;
                    myras = dialer.Dial();
                    Thread.Sleep(1000);
                }
                if (!myras.IsInvalid)
                {
                    _ResultMsg = "拨号成功";
                    return true;
                }
                else
                {
                    _ResultMsg = "拨号失败";
                    return false;
                }
            }
            catch (RasException ex)
            {
                switch (ex.ErrorCode.ToString())
                {
                    case "651":   //no response
                        _ResultMsg = "网线/网口/驱动故障";
                        break;
                    case "691":  //user pass error
                        _ResultMsg = "账号/密码错误/设备数达上限";
                        break;
                    case "623":   //no phone book
                        _ResultMsg = "请尝试重置网络连接";
                        break;
                    case "756":
                        _ResultMsg = "设备错误,请尝试重启电脑";
                        break;
                    default:
                        _ResultMsg = "错误" + ex.ErrorCode.ToString();
                        break;
                }
                return false;
            }
            /*
             * Old way to Dailup
             * 
             try {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("rasdial {0} {1} {2}", EntryName, username, password);
                p.StandardInput.WriteLine("exit");
                string strRst = p.StandardOutput.ReadToEnd();

                if (strRst.Contains("已连接 " + EntryName + "。\n命令已完成。")) {
                    resultMsg = "拨号成功";
                    return true;
                }
                if (strRst.Contains("你已经连接到 " + EntryName + "。\n命令已完成。")) {
                    resultMsg = "已经连接了";
                    return true;
                }
                if (strRst.Contains("远程访问错误")) {
                    string ErrorCode = strRst.Substring(strRst.IndexOf("远程访问错误 ") + 7, 3);
                    switch (ErrorCode) {
                        case "651":   //no response
                            resultMsg = "请检查网线/网卡/入户设备";
                            break;
                        case "691":  //user pass error
                            resultMsg = "账号/密码错误/多设备登录";
                            break;
                        case "623":   //no phone book
                            resultMsg = "请尝试重置网络连接(高级选项中)";
                            break;
                        default:
                            resultMsg = "未解析的远程错误 " + ErrorCode;
                            break;
                    }
                    return false;
                }

                System.Windows.Forms.MessageBox.Show("RasdialHelper_DialUp:未解析的错误，请加群342586878向我们报告以下信息，以帮助我们解决这个错误！\r\n" + strRst);
                resultMsg = "未解析的错误";
                return false;
            }
            catch (Exception ex) {
                resultMsg = "程序异常";
                return false;
            }*/
        }

        public bool HangUp(string _EntryName, out string _ResultMsg)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("rasdial {0} /DISCONNECT", _EntryName);
                p.StandardInput.WriteLine("rasdial PPPOE /DISCONNECT");
                p.StandardInput.WriteLine("rasdial /DISCONNECT");
                p.StandardInput.WriteLine("exit");
                string strRst = p.StandardOutput.ReadToEnd();
                _ResultMsg = "成功";
                return true;
            }
            catch (Exception)
            {
                _ResultMsg = "程序异常";
                return false;
            }
        }

        public bool CreateEntry(string EntryName, out string resultMsg)
        {
            try
            {
                RasPhoneBook UserPhoneBook = new RasPhoneBook();
                string path = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);
                UserPhoneBook.Open(path);

                // if the EntryName exist,then ignore.
                if (UserPhoneBook.Entries.Contains(EntryName))
                {
                    //UserPhoneBook.Entries[EntryName].PhoneNumber = "";
                    //UserPhoneBook.Entries[EntryName].DnsAddress = IPAddress.Loopback;
                    //UserPhoneBook.Entries[EntryName].DnsAddress = null;
                    // 不管当前PPPOE是否连接，服务器地址的更新总能成功，如果正在连接，则需要PPPOE重启后才能起作用
                    //UserPhoneBook.Entries[EntryName].Update();
                    resultMsg = "设备已就绪";
                    return true;
                }

                ReadOnlyCollection<RasDevice> readOnlyCollection = RasDevice.GetDevices();
                //                foreach (var col in readOnlyCollection)
                //                {
                //                    adds += col.Name + ":" + col.DeviceType.ToString() + "|||";
                //                }
                //                _log.Info("Devices are : " + adds);
                // Find the device that will be used to dial the connection.

                RasDevice device = RasDevice.GetDevices().Where(o => o.DeviceType == RasDeviceType.PPPoE).First();
                RasEntry NewEntry = RasEntry.CreateBroadbandEntry(EntryName, device);    //建立PPPoE Entry
                NewEntry.PhoneNumber = "";
                //NewEntry.DnsAddress = IPAddress.Loopback;
                NewEntry.DnsAddress = null;
                UserPhoneBook.Entries.Add(NewEntry);
                resultMsg = "设备创建成功";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                resultMsg = "系统虚拟设备异常";
                return false;
            }




            /*
             * Old way to Create Entry
             * 
            string pbk_string;

            if (DNSon == false) {
                pbk_string = EasyCUSX.Properties.Resources.pbk_normal;
            } else {
                pbk_string = EasyCUSX.Properties.Resources.pbk_ChinaDNS;
            }

            try {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk");
                FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk", FileMode.Append);
                byte[] data = new UTF8Encoding().GetBytes(pbk_string);
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Close();
                resultMsg = "预置成功";
                return true;
            } catch (Exception ex) {
                resultMsg = "程序异常";
                exh.save(ex);
                return false;
            }*/
        }

        public bool CheckNetwork(out string resultMsg)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("rasdial");
                p.StandardInput.WriteLine("exit");
                string strRst = p.StandardOutput.ReadToEnd();
                strRst = strRst.Replace(Environment.CurrentDirectory, "2333333");
                if (strRst.Contains("EasyCUSX"))
                {
                    resultMsg = "网络已连接";
                    return true;
                }
                else if (strRst.Contains("PPPOE"))
                {
                    resultMsg = "通过学校客户端连接";
                    return true;
                }
                else if (strRst.Contains("Connected") || strRst.Contains("已连接"))
                {
                    resultMsg = "通过其他方式连接";
                    return true;
                }
                else
                {
                    resultMsg = "网络未连接";
                    return false;
                }
            }
            catch (Exception)
            {
                resultMsg = "程序异常";
                return false;
            }

        }

        public bool FlushAllEntry()
        {
            try
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Network\Connections\Pbk\rasphone.pbk");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public string getCurrentIP()
        {
            foreach (RasConnection connection in RasConnection.GetActiveConnections())
            {
                if (connection.EntryName == "EasyCUSX" || connection.EntryName == "PPPOE")
                {
                    RasIPInfo ipAddresses = (RasIPInfo)connection.GetProjectionInfo(RasProjectionType.IP);
                    Console.WriteLine("I Found IP:" + ipAddresses.IPAddress.ToString());
                    return ipAddresses.IPAddress.ToString();
                }
            }
            return "0.0.0.0";
        }

    }
}
