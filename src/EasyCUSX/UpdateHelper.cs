using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace UpdateHelper
{
    class UpdaterMain
    {
        public enum CheckStatu
        {
            noNewVersion = 0,
            newVersion = 1,
            Failed = 2,
        }

        public CheckStatu Check(string _currentVersion)
        {
            try
            {
                WebClient client = new WebClient();
                string RecvStr = Encoding.ASCII.GetString(client.DownloadData("http://api.cusx.net/v1/get.php?appname=easycusx_win&request=version"));
                client.Dispose();
                if (_currentVersion == RecvStr)
                {
                    return CheckStatu.noNewVersion;
                }
                else
                {
                    return CheckStatu.newVersion;
                }
            }
            catch (Exception)
            {
                return CheckStatu.Failed;
            }
        }

        public bool Download()
        {
            try
            {
                File.Move(Application.ExecutablePath, Application.StartupPath + @"\old.exe");
                try
                {
                    WebClient client = new WebClient();
                    string newlink = Encoding.ASCII.GetString(client.DownloadData("http://api.cusx.net/v1/get.php?appname=easycusx_win&request=downloadlink"));
                    client.DownloadFile(newlink, Application.StartupPath + @"\EasyCUSX.exe");
                    client.Dispose();
                    return true;
                }
                catch (Exception)
                {
                    if (File.Exists(Application.StartupPath + @"\EasyCUSX.exe"))
                    {
                        File.Delete(Application.StartupPath + @"\EasyCUSX.exe");
                    }
                    File.Move(Application.StartupPath + @"\old.exe", Application.StartupPath + @"\EasyCUSX.exe");
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        public void CleanUp()
        {
            try
            {
                if (File.Exists(Application.StartupPath + @"\old.exe"))
                {
                    File.Delete(Application.StartupPath + @"\old.exe");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}