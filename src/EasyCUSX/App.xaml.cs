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
            MessageBox.Show("易·山传发生了一个严重错误，程序将尝试忽略这个错误并继续工作，并有可能出现一些未知问题。\r\n如果该问题反复出现，请反馈至易山传QQ群：512985336");
            new ExceptionHandler(e.ExceptionObject.ToString());
        }

        static void application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("易·山传发生了一个严重错误，程序将尝试忽略这个错误并继续工作，并有可能出现一些未知问题。\r\n如果该问题反复出现，请反馈至易山传QQ群：512985336");
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
