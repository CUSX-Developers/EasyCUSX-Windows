using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Forms; //trayIcon Control
using System.Windows.Input;
using System.Windows.Media.Effects;
//Class
using rasdialHelper;
using SocketHelper;
using UpdateHelper;

namespace EasyCUSX
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region init
        string ProgramName = "易·山传";
        string version = "2.0.7.3"; //Semver Standard

        //网络
        bool WANconnecting = false;
        bool WANconnected = false;
        string pppoeusername;
        string pppoepassword;

        bool detectNetworkStatus; //a switch for Detect Network Status Feature
        bool UpdateChecked = false;
        bool UpdateChecking = false;

        //UI
        BlurEffect blur = new BlurEffect();

        NotifyIcon notify = new System.Windows.Forms.NotifyIcon();

        //import class
        RasHelperMain d = new RasHelperMain();
        Ping ping = new Ping();
        UpdaterMain updater = new UpdaterMain();

        //HeartBeat Handle
        Thread hbt;

        //enums
        public enum WorkButtonFlag
        {
            NoFunction = 0,
            Back = 1,
            Disconnect = 2
        }

        public enum CurrectWorkStateFlag
        {
            Idle = 0,
            Connecting = 1,
            Connected = 2,
            ErrorMsgShowing = 3,
            Disconnecting = 4
        }

        public enum NotifyPopMsgFlag
        {
            Error = 0,
            Warning = 1,
            Info = 2
        }

        #endregion



        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWPFWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EasyCUSXInit();
        }

        #region Main Functions

        //Program
        public void EasyCUSXInit()
        {
            //清理自动更新文件
            updater.CleanUp();

            //踢出其他客户端
            KickOtherClient();

            //载入托盘图标
            notify.Text = ProgramName;
            notify.Icon = Properties.Resources.icon;
            notify.Visible = true;
            notify.Click += notify_Click;

            //载入版本号到UI
            Label_version.Content = ProgramName + " " + version;

            //载入当前设置状态到UI
            LoadConfig();

            //载入当前网络状态到UI并设置必要的状态参数
            SetCurrectWorkState(CurrectWorkStateFlag.Connecting);
            SetStateMsg("检测网络状态...");

            Thread temp = new Thread(() =>
            {
                string resultMsg;
                if (d.CheckNetwork(out resultMsg))
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        //Set for disconnect process
                        pppoeusername = TextBox_Username.Text;
                        pppoepassword = TextBox_Password.Password;
                        //设置是否检测网络波动
                        detectNetworkStatus = CheckBox_networkDetect.IsChecked.Value;

                        //检查参数是否对auth有效
                        if (!(pppoeusername != string.Empty && pppoepassword != string.Empty))
                        {
                            //无效时则表示用户已连接网络并第一次使用，断开连接防止心跳错误
                            Thread tempT = new Thread(new ThreadStart(WANDisconnect));
                            tempT.IsBackground = true;
                            tempT.Start();
                            return;
                        }
                    }));
                    //设置UI
                    SetCurrectWorkState(CurrectWorkStateFlag.Connected);
                    SetWindowVisibility(false);
                }
                else
                {
                    SetCurrectWorkState(CurrectWorkStateFlag.Idle);
                }
            });
            temp.IsBackground = true;
            temp.Start();

            //启动网络状态检查线程
            Thread t = new Thread(new ThreadStart(NetworkCheckLoop));
            t.IsBackground = true;
            t.Start();
        }

        private void NetworkCheckLoop()
        {
            try
            {
                while (true)
                {
                    if (WANconnected) //检查有线校园网是否保持着连接
                    {
                        string Result;
                        if (!d.CheckNetwork(out Result))
                        {
                            SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing, "网络已断开");
                            NotifyPopUp("有线网络已经断开", NotifyPopMsgFlag.Error);
                        }
                        else
                        {
                            //HeartBeat packet
                            //SendSocketAuth(pppoeusername, out Result);
                            if (hbt == null)
                            {
                                hbt = new Thread(() => HeartBeatHandler(true));
                                hbt.IsBackground = true;
                                hbt.Start();
                            }
                            else if (!hbt.IsAlive)
                            {
                                hbt = new Thread(() => HeartBeatHandler(true));
                                hbt.IsBackground = true;
                                hbt.Start();
                                if (detectNetworkStatus)
                                {
                                    SetStateMsg("网络不稳定");
                                    NotifyPopUp("校园网处于波动中...(TCP)", NotifyPopMsgFlag.Warning);
                                }

                            }
                        }
                    }
                    else
                    {
                        shutdownHeartBeatThread();
                    }

                    if (WANconnected && detectNetworkStatus) //检测校园网络波动
                    {
                        PingReply pingReply = ping.Send("172.18.4.3");
                        if (pingReply.Status != IPStatus.Success)
                        {
                            SetStateMsg("网络不稳定");
                            NotifyPopUp("校园网处于波动中...(ICMP)", NotifyPopMsgFlag.Warning);
                        }
                    }

                    if (!UpdateChecked)
                    {
                        EasyCUSX_Update();
                    }
                    Thread.Sleep(7000);
                }
            }
            catch (Exception)
            {
            }
        }

        private void KickOtherClient()
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process otherclient in procs)
                {
                    if (otherclient.ProcessName.ToLower().Contains("pppoelogin"))
                    {
                        otherclient.Kill();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        //Network
        private void WANConnect(string u, string p)
        {
            string Result;

            //设置到连接中状态
            SetCurrectWorkState(CurrectWorkStateFlag.Connecting);
            Thread.Sleep(500);

            //创建Entry
            SetStateMsg("正在检查设备...");
            if (!d.CreateEntry("EasyCUSX", out Result))
            {
                SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing, Result);
                return;
            }

            //开始拨号
            SetStateMsg("正在连接中...");
            if (!d.DialUp(u, p, "EasyCUSX", out Result))
            {
                SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing, Result);
                return;
            }

            //SocketAuth part
            SetStateMsg("正在验证中...");
            if (!SendSocketAuth(u, out Result))
            {
                SetStateMsg("正在重试...");
                //retry once
                if (!SendSocketAuth(u, out Result))
                {
                    WANDisconnect();
                    SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing, "网络不稳定,请重试");
                    return;
                }
            }

            SetCurrectWorkState(CurrectWorkStateFlag.Connected);

            Thread.Sleep(1000);
            SetWindowVisibility(false);

            Thread.Sleep(2000);
            //检查更新
            EasyCUSX_Update();
        }

        private void WANDisconnect()
        {
            try
            {
                SetCurrectWorkState(CurrectWorkStateFlag.Disconnecting);
                shutdownHeartBeatThread();

                string Result;
                SendDisconnectAuth(pppoeusername, d.getCurrentIP(), out Result);

                if (d.HangUp("EasyCUSX", out Result))
                {
                    SetCurrectWorkState(CurrectWorkStateFlag.Idle);
                }
                else
                {
                    SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing, Result);
                }
            }
            catch (Exception)
            {
            }

        }

        private bool SendSocketAuth(string u, out string _inResult)
        {
            if (u == string.Empty)
            {
                _inResult = "ParamInvalid";
                return true;
            }
            SocketHelperMain s = new SocketHelperMain();
            string[] datas = {
                             "AUTH 33ss333asasasc3ddsd5434fsdasas5\r\n",
                             //"GET clientver\r\n",
                             string.Format("*3\r\n$7\r\npublish\r\n$11\r\nclientcheck\r\n${0}\r\ncheckuser:{1}\r\n", (u.Length + 10).ToString(), u),
                             //"GET openurl\r\n",
                             string.Format("*3\r\n$4\r\nSADD\r\n$7\r\nalluser\r\n${0}\r\n{1}\r\n", u.Length.ToString(), u),
                             string.Format("*3\r\n$6\r\nCLIENT\r\n$7\r\nSETNAME\r\n${0}\r\n{1}\r\n", u.Length.ToString(), u),
                             string.Format("*3\r\n$9\r\nsubscribe\r\n$12\r\ncheckprocess\r\n${0}\r\n@{1}\r\n", (u.Length + 1).ToString(), u)
                             };
            if (s.SocketConnect("172.18.4.3", 6379, out _inResult))
            {
                foreach (string data in datas)
                {
                    if (!s.Send4Recv(data, out _inResult))
                    {
                        s.SocketClose();
                        return false;
                    }
                }
                //s.JustSend("QUIT\r\n", out _inResult);
                //s.SocketClose();

                //go HeartBeat
                hbt = new Thread(() => HeartBeatHandler(false, s));
                hbt.IsBackground = true;
                hbt.Start();

                _inResult = "AuthCompleted";
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// HeartBeat Thread Handler
        /// </summary>
        /// <param name="preAuth">set if pre auth is needed</param>
        /// <param name="s">if pre auth is needed, than this is not needed.</param>
        private void HeartBeatHandler(bool preAuth, SocketHelperMain s = null)
        {
            try
            {
                string _inResult;
                if (preAuth)
                {
                    while (true)
                    {
                        if (SendSocketAuth(pppoeusername, out _inResult))
                        {
                            break;
                        }
                    }
                    return;
                }

                while (true)
                {
                    string recvStr = "";
                    if (s.JustRecv(out recvStr))
                    {
                        Console.WriteLine("HeartBeat Thread recieve:", recvStr);
                    }
                    else
                    {
                        Console.WriteLine("HeartBeat Thread Failed once time. Try to restart HeartBeat");
                        break;
                    }
                }
            }
            catch (ThreadAbortException)
            {
                string _inResult;
                if (s != null)
                {
                    s.JustSend("QUIT\r\n", out _inResult);
                    s.SocketClose();
                }
            }
            catch (Exception)
            {
            }
        }

        private void shutdownHeartBeatThread()
        {
            try
            {
                if (hbt != null)
                {
                    if (hbt.IsAlive)
                    {
                        hbt.Abort();
                    }
                }
            }
            catch (Exception)
            {
            }

        }

        private bool SendDisconnectAuth(string u, string IP, out string _inResult)
        {
            if (u == string.Empty || IP == string.Empty)
            {
                _inResult = "ParamInvalid";
                return true;
            }
            SocketHelperMain s = new SocketHelperMain();
            string[] datas = {
                             "AUTH 33ss333asasasc3ddsd5434fsdasas5\r\n",
                             string.Format("*3\r\n$7\r\npublish\r\n$11\r\nclientcheck\r\n${0}\r\ndownline:{1}:{2}\r\n", (u.Length + IP.Length + 10).ToString(), u, IP)
                             };
            if (s.SocketConnect("172.18.4.3", 6379, out _inResult))
            {
                foreach (string data in datas)
                {
                    if (!s.Send4Recv(data, out _inResult))
                    {
                        s.SocketClose();
                        return false;
                    }
                }
                s.JustSend("QUIT\r\n", out _inResult);
                s.SocketClose();
                _inResult = "AuthCompleted";
                return true;
            }
            else
            {
                return false;
            }
        }

        private void EasyCUSX_Update()
        {
            if (UpdateChecked || UpdateChecking)
            {
                return;
            }
            UpdateChecking = true;
            Thread temp = new Thread(() =>
            {
                UpdaterMain.CheckStatu status = updater.Check(version);
                if (status == UpdaterMain.CheckStatu.newVersion)
                {
                    UpdateChecked = true;
                    NotifyPopUp("发现了新版本，开始更新！", NotifyPopMsgFlag.Info);
                    if (updater.Download())
                    {
                        NotifyPopUp("更新下载完成！下次启动易·山传时更新将完成", NotifyPopMsgFlag.Info);
                    }
                    else
                    {
                        NotifyPopUp("更新下载失败！", NotifyPopMsgFlag.Error);
                    }
                }
                else if (status == UpdaterMain.CheckStatu.noNewVersion)
                {
                    Console.WriteLine("No New Version");
                    //NotifyPopUp("未发现新版本", NotifyPopMsgFlag.Error);
                    UpdateChecked = true;
                }
                else
                {
                    Console.WriteLine("Check Update Failed");
                    //NotifyPopUp("检查更新失败", NotifyPopMsgFlag.Error);
                }

                UpdateChecking = false;
            });
            temp.IsBackground = true;
            temp.Start();

        }
        #endregion

        #region UI

        //Entrys

        private void SetCurrectWorkState(CurrectWorkStateFlag Flag, string ErrorMsg = "")
        {
            switch (Flag)
            {
                case CurrectWorkStateFlag.Idle: //idle 0
                    SetBlurBackground(false);
                    SetStateMsgVisbility(false);
                    SetStateMsg("Idle");
                    SetWorkButton(WorkButtonFlag.NoFunction);
                    WANconnecting = false;
                    WANconnected = false;
                    break;

                case CurrectWorkStateFlag.Connecting: //connecting 1
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    SetStateMsg("正在连接中");
                    SetWorkButton(WorkButtonFlag.NoFunction);
                    WANconnecting = true;
                    WANconnected = false;
                    break;

                case CurrectWorkStateFlag.Connected: //connected 2
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    SetStateMsg("网络已连接");
                    SetWorkButton(WorkButtonFlag.Disconnect);
                    WANconnecting = false;
                    WANconnected = true;
                    break;
                case CurrectWorkStateFlag.ErrorMsgShowing: //errorMsgShowing 3
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    SetStateMsg(ErrorMsg);
                    SetWorkButton(WorkButtonFlag.Back);
                    WANconnecting = false;
                    WANconnected = false;
                    break;
                case CurrectWorkStateFlag.Disconnecting: //disconnecting 4
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    SetStateMsg("正在断开中");
                    SetWorkButton(WorkButtonFlag.NoFunction);
                    WANconnecting = false;
                    WANconnected = false;
                    break;
            }
        }

        private void NotifyPopUp(string Msg, NotifyPopMsgFlag Flag = NotifyPopMsgFlag.Info, int Duration = 5000)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                switch (Flag)
                {
                    case NotifyPopMsgFlag.Info:
                        notify.ShowBalloonTip(Duration, "提示", Msg, ToolTipIcon.Info);
                        break;
                    case NotifyPopMsgFlag.Warning:
                        notify.ShowBalloonTip(Duration, "警告", Msg, ToolTipIcon.Warning);
                        break;
                    case NotifyPopMsgFlag.Error:
                        notify.ShowBalloonTip(Duration, "错误", Msg, ToolTipIcon.Error);
                        break;
                }
            }));

        }

        //Sub
        private void SetBlurBackground(bool on)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (on == true)
                {
                    blur.Radius = 100;
                    blur.RenderingBias = RenderingBias.Performance;
                    Main_Content.Effect = blur;
                    Main_Content.IsEnabled = false;
                    Collapsed(1);
                    WorkMsg.Visibility = Visibility.Visible;
                }
                else
                {
                    blur.Radius = 0;
                    Main_Content.Effect = blur;
                    Main_Content.IsEnabled = true;
                }
            }));
        }

        private void SetWorkButton(WorkButtonFlag Flag)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                switch (Flag)
                {
                    case WorkButtonFlag.NoFunction://hide(idle/processing) 1
                        WorkButton.Visibility = Visibility.Hidden;
                        WorkButton.Content = "noContent";
                        break;
                    case WorkButtonFlag.Back://ProcessStop(ShowErrorMsg/WaitForUserBack) 2
                        WorkButton.Visibility = Visibility.Visible;
                        WorkButton.Content = "返 回";
                        break;
                    case WorkButtonFlag.Disconnect://success(DisconnectButton) 3
                        WorkButton.Visibility = Visibility.Visible;
                        WorkButton.Content = "断 开 连 接";
                        break;

                }
            }));
        }

        private void SetStateMsgVisbility(bool on)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (on == true)
                {
                    WorkMsg.Visibility = Visibility.Visible;
                }
                else
                {
                    WorkMsg.Visibility = Visibility.Hidden;
                }
            }));
        }

        private void SetStateMsg(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                WorkMsg.Text = msg;
            }));
        }

        private void SetWindowVisibility(bool visible)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (visible == true)
                {
                    this.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Visibility = Visibility.Hidden;
                    NotifyPopUp("易·山传正在后台运行中...\r\n点击托盘图标可 显示/隐藏 窗口");
                }
            }));
        }

        #endregion

        #region Events

        //UI
        private void AdvancedButton_Expanded(object sender, RoutedEventArgs e)
        {
            Expanded();
        }

        private void AdvancedButton_Collapsed(object sender, RoutedEventArgs e)
        {
            Collapsed(0);
        }

        private void Expanded()
        {
            MainWPFWindow.Width = 740;
            ExpandedBorder.Visibility = Visibility.Visible;
        }

        private void Collapsed(int extraControl)
        {
            MainWPFWindow.Width = 550;
            ExpandedBorder.Visibility = Visibility.Hidden;
            if (extraControl == 1)
            {
                AdvancedButton.IsExpanded = false;
            }
        }

        private void notify_Click(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                SetWindowVisibility(false);
            }
            else
            {
                SetWindowVisibility(true);
            }
        }

        private void MainWPFWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        //Functions
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            //保存选项
            SaveConfig();

            //拨号部分
            //Set for disconnect
            pppoeusername = TextBox_Username.Text;
            pppoepassword = TextBox_Password.Password;
            //设置是否检测网络波动
            detectNetworkStatus = CheckBox_networkDetect.IsChecked.Value;
            //开始拨号
            Thread t = new Thread(() => WANConnect(pppoeusername, pppoepassword));
            t.IsBackground = true;
            t.Start();
        }

        private void WorkButton_Click(object sender, RoutedEventArgs e)
        {
            if (WANconnecting == false && WANconnected == false)
            {
                SetCurrectWorkState(CurrectWorkStateFlag.Idle); //BackButton
            }
            if (WANconnected == true)
            {
                Thread t = new Thread(() => WANDisconnect()); //WANDisconnectButton
                t.IsBackground = true;
                t.Start();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //因为已连接时界面被禁用,所以不做网络连接判断
            notify.Visible = false;
            MainWPFWindow.Visibility = Visibility.Hidden;
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWPFWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WANconnecting == true || WANconnected == true)
            {
                e.Cancel = true;
                System.Windows.Forms.MessageBox.Show("请先断开连接!");
            }
        }

        private void FixEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("这么做将会删除你所创建的所有连接，确定要继续吗？", "警告", System.Windows.Forms.MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                if (d.FlushAllEntry())
                {
                    System.Windows.Forms.MessageBox.Show("重置成功.");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("重置失败!\r\n权限可能不足");
                }
            }
        }

        #endregion

        #region Data

        private void SaveConfig()
        {
            Properties.Settings.Default.username = TextBox_Username.Text;
            if (CheckBox_REMpass.IsChecked.Value)
            {
                Properties.Settings.Default.password = TextBox_Password.Password;
            }
            else
            {
                Properties.Settings.Default.password = "";
            }
            Properties.Settings.Default.REMpass = CheckBox_REMpass.IsChecked.Value;
            Properties.Settings.Default.NetworkDetect = CheckBox_networkDetect.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void LoadConfig()
        {
            TextBox_Username.Text = Properties.Settings.Default.username;
            TextBox_Password.Password = Properties.Settings.Default.password;
            CheckBox_REMpass.IsChecked = Properties.Settings.Default.REMpass;
            CheckBox_networkDetect.IsChecked = Properties.Settings.Default.NetworkDetect;
        }

        #endregion
    }
}