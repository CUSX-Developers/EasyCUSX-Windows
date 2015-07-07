﻿using System;
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
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
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