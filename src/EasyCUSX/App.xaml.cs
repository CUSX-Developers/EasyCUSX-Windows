using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Shell;

namespace EasyCUSX
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string Unique = "EasyCUSX2_Mutex_as456a8fc41as9d84af1f5f4f9g8s9f4"; //EasyCUSX2_Mutex_as456a8fc41as9d84af1f5f4f9g8s9f4

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
            MessageBox.Show("易·山传在运行过程中发生了一个严重错误，程序将尝试忽略这个错误并继续工作\r\n但仍建议您重新打开易·山传以避免一些未知的问题.\r\n希望您能将下面的错误信息报告给开发者以帮助解决这个问题，不胜感激！反馈QQ群：512985336\r\n错误堆栈：" + e.ExceptionObject.ToString());
        }

        static void application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("易·山传在运行过程中发生了一个严重错误，程序将尝试忽略这个错误并继续工作\r\n但仍建议您重新打开易·山传以避免一些未知的问题.\r\n希望您能将下面的错误信息报告给开发者以帮助解决这个问题，不胜感激！反馈QQ群：512985336\r\n错误堆栈：" + e.Exception.ToString());
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
