using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ExHandler;

namespace UpdateHelper
{
    class Updater
    {
        public enum CheckStatus
        {
            noNewVersion = 0,
            newVersion = 1,
            Failed = 2,
        }

        public static CheckStatus Check(string _currentVersion, string username, string tag)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add(HttpRequestHeader.UserAgent, string.Format("{0}:{1}:{2}:EasyCUSX_Statistics", username, tag, _currentVersion));
                string RecvStr = Encoding.ASCII.GetString(client.DownloadData("http://api.cusx.net/v1/get.php?appname=easycusx_win&request=version"));
                client.Dispose();
                if (_currentVersion == RecvStr)
                {
                    return CheckStatus.noNewVersion;
                }
                else
                {
                    return CheckStatus.newVersion;
                }
            }
            catch (Exception ex)
            {
                return CheckStatus.Failed;
            }
        }

        public static bool Download()
        {
            try
            {
                WebClient client = new WebClient();
                string newlink = Encoding.ASCII.GetString(client.DownloadData("http://api.cusx.net/v1/get.php?appname=easycusx_win&request=downloadlink"));
                client.DownloadFile(newlink, Application.StartupPath + @"\new.exe");
                client.Dispose();
                File.Move(Application.ExecutablePath, Application.StartupPath + @"\old.exe");
                File.Move(Application.StartupPath + @"\new.exe", Application.StartupPath + @"\EasyCUSX.exe");
                return true;
            }
            catch (WebException)
            {
                File.Delete(Application.StartupPath + @"\new.exe");
                return false;
            }
            catch (Exception ex)
            {
                new ExceptionHandler(ex.ToString());
                return false;
            }
        }

        public static void CleanUp()
        {
            try
            {
                File.Delete(Application.StartupPath + @"\old.exe");
            }
            catch (Exception ex)
            {
                new ExceptionHandler(ex.ToString());
            }
        }
    }
}