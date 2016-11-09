using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Shell;
using ExHandler;

namespace EasyCUSX
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string Unique = "EasyCUSX2_Mutex_as456a8fc41as9d84af1f5f4f9g8s9f4"; //EasyCUSX2_Mutex_as456a8fc41as9d84af1f5f4f9g8s9f4
        private const string errMsg = "易·山传发生了一个严重错误，程序将尝试忽略这个错误并继续工作，但有可能出现更多的未知问题，我们建议您立即重新启动易山传。\r\n如果该问题反复出现，请访问官方网站 https://cusx.net 下载安装包重新安装。\r\n如有更多疑问，可以反馈到易山传QQ群：512985336";

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();
                application.InitializeComponent();
                application.DispatcherUnhandledException += application_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(errMsg);
            new ExceptionHandler(e.ExceptionObject.ToString());
        }

        static void application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(errMsg);
            new ExceptionHandler(e.Exception.ToString());
            e.Handled = true;
        }

        #region ISingleInstanceApp Members
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // Handle command line arguments of second instance
            return true;
        }
        #endregion
    }
}
