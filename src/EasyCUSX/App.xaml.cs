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
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        static void application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("发生了一个严重错误，易山传将尝试继续工作\r\n但仍建议您重新打开易山传以避免一些未知问题.");
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
