using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Effects;
//Class
using rasdialHelper;
using SocketHelper;
using UpdateHelper;
using ExHandler;
using WlanHelper;

namespace EasyCUSX
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Defines

        string ProgramName = "易·山传";
        string ProgramTag = "easycusx_win";
        string version = "2.1.1";

        //Network
        bool WanConnecting = false;
        bool WanConnected = false;
        bool WlanConnecting = false;
        bool WlanConnected = false;
        bool WanDisconnecting = false;
        bool WlanDiconnecting = false;

        string pppoeusername;
        string pppoepassword;

        //Updater
        bool UpdateChecked = false;
        bool UpdateChecking = false;

        //UI
        BlurEffect blur = new BlurEffect();
        NotifyIcon notify = new System.Windows.Forms.NotifyIcon();

        //import class
        RasHelperMain d = new RasHelperMain();
        WlanHelperMain wifi = new WlanHelperMain();

        //HeartBeat Thread
        Thread WanHeartBeatDeamonThread;

        //enums
        private enum UIStatusOptions
        {
            Idle = 0,
            Working = 1,
            Deamon = 2,
            Error = 3,
        }
        private enum NotifyTypeOptions
        {
            Error = 0,
            Warning = 1,
            Info = 2
        }
        private enum StatusPageButtonOptions
        {
            NoFunction = 0,
            Back = 1,
            Disconnect = 2
        }

        #endregion


        #region Main

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWPFWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EasyCUSXInit();
        }

        private void EasyCUSXInit()
        {
            //禁用UI交互
            SetUIStatus(UIStatusOptions.Working, "请稍后", "正在检查网络状态");

            //初始化UI
            //托盘
            notify.Text = ProgramName;
            notify.Icon = Properties.Resources.icon;
            notify.Visible = true;
            notify.Click += notify_Click;
            //sidebar版本号
            Label_version.Content = Label_version.Content + " " + version;
            //数据填充至UI
            LoadConfig();
            //Blur性能选项
            blur.RenderingBias = RenderingBias.Performance;

            Thread Do = new Thread(() =>
            {
                //预清理
                Updater.CleanUp();
                KickOtherClient();

                string outMsg;
                if (d.CheckNetwork(out outMsg))
                {
                    PreSaveCertificate();
                    //参数为空时Redis Auth服务器可能会无响应数据 && 说明用户是第一次使用
                    if (!(pppoeusername != string.Empty && pppoepassword != string.Empty))
                    {
                        //断开连接防止心跳错误
                        if (d.HangUp(out outMsg))
                        {
                            SetUIStatus(UIStatusOptions.Idle);
                        }
                        else
                        {
                            SetUIStatus(UIStatusOptions.Error, "出错了!", "初始化时出现错误,请重启电脑再试");
                        }
                        return;
                    }
                    else
                    {
                        //已连接的状态
                        WanConnected = true;
                        SetUIStatus(UIStatusOptions.Deamon, "网络已连接", outMsg);
                        SetWindowVisibility(false);
                        NotifyPopUp("网络已经处于连接状态\r\n点击托盘图标可 显示/隐藏 窗口");
                    }
                }
                else
                {
                    //未连接
                    SetUIStatus(UIStatusOptions.Idle);
                }
            });
            Do.IsBackground = true;
            Do.Start();

            //启动主守护线程
            Thread MainDeamonThread = new Thread(() => MainDeamon());
            MainDeamonThread.IsBackground = true;
            MainDeamonThread.Start();
        }

        private void MainDeamon()
        {
            try
            {
                while (true)
                {
                    if (WanConnected && !WanDisconnecting) //有线网
                    {
                        string Result;
                        if (!d.CheckNetwork(out Result))
                        {
                            WanConnected = false;
                            SetUIStatus(UIStatusOptions.Error, "有线网络已断开", "请检查网线是否连接正常");
                            NotifyPopUp("有线网络已经断开", NotifyTypeOptions.Error);
                        }
                        else
                        {
                            //Check WAN HeartBeat Deamon Thread
                            if (WanHeartBeatDeamonThread == null || (!WanHeartBeatDeamonThread.IsAlive))
                            {
                                WanHeartBeatDeamonThread = new Thread(() => WanHeartBeatDeamon(true));
                                WanHeartBeatDeamonThread.IsBackground = true;
                                WanHeartBeatDeamonThread.Start();
                            }
                        }
                    }
                    else
                    {
                        ShutdownWanHeartBeatDeamon();
                    }

                    if (WlanConnected)
                    {
                        //TODO:Wlan part in Main Deamon
                    }

                    if (!UpdateChecked && !UpdateChecking && (WanConnected || WlanConnected))
                    {
                        EasyCUSX_Update();
                    }
                    Thread.Sleep(7000);
                }
            }
            catch (Exception ex)
            {
                new ExceptionHandler(ex.ToString());
            }
        }

        private void KickOtherClient()
        {
            Process[] procs = Process.GetProcesses();
            foreach (Process otherclient in procs)
            {
                try
                {
                    if (otherclient.ProcessName.ToLower().Contains("pppoelogin"))
                    {
                        otherclient.Kill();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void EasyCUSX_Update()
        {
            UpdateChecking = true;
            Thread temp = new Thread(() =>
            {
                Updater.CheckStatus status = Updater.Check(version, pppoeusername, ProgramTag);
                if (status == Updater.CheckStatus.newVersion)
                {
                    UpdateChecked = true;
                    NotifyPopUp("发现了新版本，开始更新！", NotifyTypeOptions.Info);
                    if (Updater.Download())
                    {
                        NotifyPopUp("更新下载完成！下次启动易·山传时更新将完成", NotifyTypeOptions.Info);
                    }
                    else
                    {
                        NotifyPopUp("更新下载失败！", NotifyTypeOptions.Error);
                    }
                }
                else if (status == Updater.CheckStatus.noNewVersion)
                {
                    UpdateChecked = true;
                }

                UpdateChecking = false;
            });
            temp.IsBackground = true;
            temp.Start();
        }

        #endregion


        #region Wan Part

        private void WanConnect(string u, string p)
        {
            string Result;

            //设置到工作状态
            SetUIStatus(UIStatusOptions.Working, "请稍后", "即将开始连接有线网络");
            WanConnecting = true;
            Thread.Sleep(500);

            //创建Entry
            SetStatusMsg("请稍后", "正在检查虚拟网络设备");
            if (!d.CreateEntry("EasyCUSX", out Result))
            {
                SetUIStatus(UIStatusOptions.Error, "出错了!", Result);
                WanConnecting = false;
                return;
            }

            //开始拨号
            SetStatusMsg("请稍后", "正在连接至校园网");
            if (!d.DialUp(u, p, "EasyCUSX", out Result))
            {
                SetUIStatus(UIStatusOptions.Error, "出错了!", Result);
                WanConnecting = false;
                return;
            }

            //SocketAuth part
            SetStatusMsg("请稍后", "正在验证账户");
            if (!SendWanAuth(u, out Result))
            {
                SetStatusMsg("请稍后", "验证账户失败 正在重试");
                //retry once
                if (!SendWanAuth(u, out Result))
                {
                    WanDisconnect();
                    SetUIStatus(UIStatusOptions.Error, "网络不稳定 请尝试重新连接");
                    WanConnecting = false;
                    return;
                }
            }

            WanConnecting = false;
            WanConnected = true;
            SetUIStatus(UIStatusOptions.Deamon, "网络已连接", "感谢使用易·山传");

            Thread.Sleep(1000);
            SetWindowVisibility(false);
            NotifyPopUp("易·山传正在后台运行中...\r\n点击托盘图标可 显示/隐藏 窗口");
        }

        private void WanDisconnect()
        {
            string Result;

            try
            {
                SetUIStatus(UIStatusOptions.Working, "请稍后", "正在断开连接中");
                WanDisconnecting = true;
                ShutdownWanHeartBeatDeamon();

                SendWanDeAuth(pppoeusername, d.getCurrentIP(), out Result);

                if (d.HangUp(out Result))
                {
                    SetUIStatus(UIStatusOptions.Idle);
                }
                else
                {
                    SetUIStatus(UIStatusOptions.Error, "出错了!", "断开连接时出现了一些问题 可能没有正常断开");
                }

                WanConnected = false;
                WanDisconnecting = false;
            }
            catch (Exception)
            {
            }
        }

        private bool SendWanAuth(string u, out string _inResult)
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

                //启动心跳守护
                WanHeartBeatDeamonThread = new Thread(() => WanHeartBeatDeamon(false, s));
                WanHeartBeatDeamonThread.IsBackground = true;
                WanHeartBeatDeamonThread.Start();

                _inResult = "完成";
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool SendWanDeAuth(string u, string IP, out string _inResult)
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
                _inResult = "完成";
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 用于有线网络的心跳守护线程
        /// </summary>
        /// <param name="preAuth">设置是否需要预验证</param>
        /// <param name="s">当不使用预验证时则需要在这里传入用于复用的Socket对象(基于SocketHelper)</param>
        private void WanHeartBeatDeamon(bool preAuth, SocketHelperMain s = null)
        {
            try
            {
                string _inResult;
                if (preAuth)
                {
                    while (true)
                    {
                        if (SendWanAuth(pppoeusername, out _inResult))
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
                        Console.WriteLine("HeartBeat recieve:", recvStr);
                    }
                    else
                    {
                        Console.WriteLine("HeartBeat Failed once time. Try to restart HeartBeat");
                        break;
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("HeartBeat Thread recvive Abort signal");
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

        private void ShutdownWanHeartBeatDeamon()
        {
            try
            {
                if (WanHeartBeatDeamonThread != null)
                {
                    if (WanHeartBeatDeamonThread.IsAlive)
                    {
                        WanHeartBeatDeamonThread.Abort();
                    }
                    WanHeartBeatDeamonThread = null;
                }
            }
            catch (Exception)
            {
            }

        }

        #endregion


        #region Wlan Part

        private void WlanConnect(string u, string p)
        {
            WlanConnecting = true;
            SetUIStatus(UIStatusOptions.Working, "请稍后", "正在扫描无线网络");
            Thread.Sleep(1500);
            SetUIStatus(UIStatusOptions.Error, "抱歉", "由于无线校园网的接口不完善\r\n本功能仍在测试 暂未开放");
            WlanConnecting = false;
        }

        #endregion


        #region UI Part
        /// <summary>
        /// 更新UI的主入口点
        /// </summary>
        /// <param name="Option">指定的UI类型</param>
        /// <param name="StatusMsgAbove">上部提示信息，仅非Idle时有效</param>
        /// <param name="StatusMsgBottom">下部提示信息，仅非Idle时有效</param>
        private void SetUIStatus(UIStatusOptions Option, string StatusMsgAbove = "", string StatusMsgBottom = "")
        {
            switch (Option)
            {
                case UIStatusOptions.Idle:
                    SetStatusMsg("Idle", "Idle Description");
                    SetLoadAnimeVisibility(false);
                    SetBlurBackground(false);
                    SetStatusPageButton(StatusPageButtonOptions.NoFunction);
                    break;

                case UIStatusOptions.Working:
                    SetStatusMsg(StatusMsgAbove, StatusMsgBottom);
                    SetLoadAnimeVisibility(true);
                    SetBlurBackground(true);
                    SetStatusPageButton(StatusPageButtonOptions.NoFunction);
                    break;

                case UIStatusOptions.Deamon:
                    SetStatusMsg(StatusMsgAbove, StatusMsgBottom);
                    SetLoadAnimeVisibility(false);
                    SetBlurBackground(true);
                    SetStatusPageButton(StatusPageButtonOptions.Disconnect);
                    break;
                case UIStatusOptions.Error:
                    SetStatusMsg(StatusMsgAbove, StatusMsgBottom);
                    SetLoadAnimeVisibility(false);
                    SetBlurBackground(true);
                    SetStatusPageButton(StatusPageButtonOptions.Back);
                    break;
            }
        }
        /// <summary>
        /// 设置程序界面的可视性
        /// </summary>
        /// <param name="visible">是否可视</param>
        private void SetWindowVisibility(bool visible)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            }));
        }
        /// <summary>
        /// 弹出托盘信息
        /// </summary>
        /// <param name="Msg">信息内容</param>
        /// <param name="Flag">信息类型，默认为Info</param>
        /// <param name="Duration">信息显示时间，默认为5000(ms)</param>
        private void NotifyPopUp(string Msg, NotifyTypeOptions Flag = NotifyTypeOptions.Info, int Duration = 5000)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                switch (Flag)
                {
                    case NotifyTypeOptions.Info:
                        notify.ShowBalloonTip(Duration, "提示", Msg, ToolTipIcon.Info);
                        break;
                    case NotifyTypeOptions.Warning:
                        notify.ShowBalloonTip(Duration, "警告", Msg, ToolTipIcon.Warning);
                        break;
                    case NotifyTypeOptions.Error:
                        notify.ShowBalloonTip(Duration, "错误", Msg, ToolTipIcon.Error);
                        break;
                }
            }));

        }

        //=============== UI AUX Functions ===============
        private void SetBlurBackground(bool on)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (on)
                {
                    blur.Radius = 100;
                    Main_Content.Effect = blur;
                    Main_Content.IsEnabled = false;
                    Collapsed(1);
                    StatusMsgBottom.Visibility = Visibility.Visible;
                    StatusMsgAbove.Visibility = Visibility.Visible;
                }
                else
                {
                    blur.Radius = 0;
                    Main_Content.Effect = blur;
                    Main_Content.IsEnabled = true;
                    StatusMsgBottom.Visibility = Visibility.Hidden;
                    StatusMsgAbove.Visibility = Visibility.Hidden;
                }
            }));
        }
        private void SetStatusPageButton(StatusPageButtonOptions Flag)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                switch (Flag)
                {
                    case StatusPageButtonOptions.NoFunction: //hide(idle/processing) 1
                        WorkButton.Visibility = Visibility.Hidden;
                        WorkButton.Content = "noContent";
                        break;
                    case StatusPageButtonOptions.Back: //ProcessStop(ShowErrorMsg/WaitForUserBack) 2
                        WorkButton.Visibility = Visibility.Visible;
                        WorkButton.Content = "返 回";
                        break;
                    case StatusPageButtonOptions.Disconnect: //success(DisconnectButton) 3
                        WorkButton.Visibility = Visibility.Visible;
                        WorkButton.Content = "断 开 连 接";
                        break;

                }
            }));
        }
        private void SetStatusMsg(string above, string bottom)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                StatusMsgAbove.Text = above;
                StatusMsgBottom.Text = bottom;
            }));
        }
        private void SetLoadAnimeVisibility(bool on)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                LoadingAnimation.Visibility = on ? Visibility.Visible : Visibility.Hidden;
            }));
        }
        #endregion


        #region UI Events

        //Functions
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            //保存选项
            SaveConfig();

            //连接有线部分
            PreSaveCertificate();

            //开始拨号
            Thread t = new Thread(() => WanConnect(pppoeusername, pppoepassword));
            t.IsBackground = true;
            t.Start();
        }

        private void WLANLoginButton_Click(object sender, RoutedEventArgs e)
        {
            //保存选项
            SaveConfig();

            //连接无线部分
            PreSaveCertificate();

            //开始连接
            Thread t = new Thread(() => WlanConnect(pppoeusername, pppoepassword));
            t.IsBackground = true;
            t.Start();
        }

        private void WorkButton_Click(object sender, RoutedEventArgs e)
        {
            if (WanConnected == false)
            {
                SetUIStatus(UIStatusOptions.Idle); //Back Button
            }
            if (WanConnected == true)
            {
                Thread t = new Thread(() => WanDisconnect()); //Wan Disconnect Button
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

        private void MainWPFWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WanConnected)
            {
                WanDisconnect();
            }
            if (WlanConnected)
            {
                //Code
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
            Properties.Settings.Default.Save();
        }
        private void LoadConfig()
        {
            TextBox_Username.Text = Properties.Settings.Default.username;
            TextBox_Password.Password = Properties.Settings.Default.password;
            CheckBox_REMpass.IsChecked = Properties.Settings.Default.REMpass;
        }
        private void PreSaveCertificate()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                pppoeusername = TextBox_Username.Text;
                pppoepassword = TextBox_Password.Password;
            }));
        }

        #endregion
    }
}