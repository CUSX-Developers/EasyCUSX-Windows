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

namespace EasyCUSX
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {


        #region init

        //网络
        bool WANconnecting = false;
        bool WANconnected = false;
        string pppoeusername;
        string pppoepassword;

        bool detectNetworkStatus;

        //UI
        BlurEffect blur = new BlurEffect();

        NotifyIcon notify = new System.Windows.Forms.NotifyIcon();

        //import class
        RasHelperMain d = new RasHelperMain();
        Ping ping = new Ping();

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
            //踢出其他客户端
            KickOtherClient();

            //载入托盘图标
            notify.Text = "易·山传 OpenSource";
            notify.Icon = Properties.Resources.icon;
            notify.Visible = true;
            notify.Click += notify_Click;

            //载入设置到UI
            LoadConfig();

            //载入当前网络状态到UI并设置必要的状态参数
            SetCurrectWorkState(CurrectWorkStateFlag.Connecting);
            DisplayStateMsg("检测网络状态...");

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
                }

                if (WANconnected && detectNetworkStatus) //检测校园网络波动
                {
                    PingReply pingReply = ping.Send("172.18.4.3");
                    if (pingReply.Status != IPStatus.Success)
                    {
                        DisplayStateMsg("网络不稳定");
                        NotifyPopUp("校园网处于波动中...", NotifyPopMsgFlag.Warning);
                    }
                }
                Thread.Sleep(7000);
            }
        }

        private void KickOtherClient()
        {
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process otherclient in procs)
                {
                    if (otherclient.ProcessName.Contains("PPPOELogin"))
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
            DisplayStateMsg("正在检查设备...");
            if (!d.CreateEntry("EasyCUSX", out Result))
            {
                DisplayStateMsg(Result);
                SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing);
                return;
            }

            //开始拨号
            DisplayStateMsg("正在连接中...");
            if (!d.DialUp(u, p, "EasyCUSX", out Result))
            {
                SetCurrectWorkState(CurrectWorkStateFlag.ErrorMsgShowing, Result);
                return;
            }

            //SocketAuth part
            DisplayStateMsg("正在验证中...");
            if (!SendSocketAuth(u, out Result))
            {
                DisplayStateMsg("正在重试...");
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
        }

        private void WANDisconnect()
        {
            SetCurrectWorkState(CurrectWorkStateFlag.Disconnecting);
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

        private bool SendSocketAuth(string u, out string _inResult)
        {
            string Result;
            SocketHelperMain s = new SocketHelperMain();
            if (s.SocketConnect("172.18.4.3", 6379, out Result))
            {
                if (!s.Send4Recv("AUTH 33ss333asasasc3ddsd5434fsdasas5\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.Send4Recv("GET clientver\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("*3\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.Send4Recv(string.Format("$7\r\npublish\r\n$11\r\nclientcheck\r\n$22\r\ncheckuser:{0}\r\n", u), out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.Send4Recv("GET openurl\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("*3\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.Send4Recv(string.Format("$4\r\nSADD\r\n$7\r\nalluser\r\n$12\r\n{0}\r\n", u), out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("*3\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.Send4Recv(string.Format("$6\r\nCLIENT\r\n$7\r\nSETNAME\r\n$12\r\n{0}\r\n", u), out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.Send4Recv(string.Format("subscribe checkprocess @{0}\r\n\r\n", u), out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("QUIT\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                s.SocketClose();  //auth complete 
            }
            else
            {
                _inResult = Result;
                return false;
            }
            _inResult = "AuthSuccess";
            return true;
        }

        private bool SendDisconnectAuth(string u, string IP, out string _inResult)
        {
            string Result;
            SocketHelperMain s = new SocketHelperMain();
            if (s.SocketConnect("172.18.4.3", 6379, out Result))
            {
                if (!s.Send4Recv("AUTH 33ss333asasasc3ddsd5434fsdasas5\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("*3\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend(string.Format("$7\r\npublish\r\n$11\r\nclientcheck\r\n$33\r\ndownline:{0}:{1}\r\n", u, IP), out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend(string.Format("$7\r\npublish\r\n$11\r\nclientcheck\r\n$33\r\ndownline:{0}:{1}\r\n", u, IP), out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("QUIT\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                if (!s.JustSend("QUIT\r\n", out Result))
                {
                    s.SocketClose();
                    _inResult = Result;
                    return false;
                }
                s.SocketClose();  //complete
            }
            else
            {
                _inResult = Result;
                return false;
            }
            _inResult = "AuthSuccess";
            return true;
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
                    DisplayStateMsg("Idle");
                    SetWorkButton(WorkButtonFlag.NoFunction);
                    WANconnecting = false;
                    WANconnected = false;
                    break;

                case CurrectWorkStateFlag.Connecting: //connecting 1
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    DisplayStateMsg("正在连接中");
                    SetWorkButton(WorkButtonFlag.NoFunction);
                    WANconnecting = true;
                    WANconnected = false;
                    break;

                case CurrectWorkStateFlag.Connected: //connected 2
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    DisplayStateMsg("网络已连接");
                    SetWorkButton(WorkButtonFlag.Disconnect);
                    WANconnecting = false;
                    WANconnected = true;
                    break;
                case CurrectWorkStateFlag.ErrorMsgShowing: //errorMsgShowing 3
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    DisplayStateMsg(ErrorMsg);
                    SetWorkButton(WorkButtonFlag.Back);
                    WANconnecting = false;
                    WANconnected = false;
                    break;
                case CurrectWorkStateFlag.Disconnecting: //disconnecting 4
                    SetBlurBackground(true);
                    SetStateMsgVisbility(true);
                    DisplayStateMsg("正在断开中");
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

        private void DisplayStateMsg(string msg)
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