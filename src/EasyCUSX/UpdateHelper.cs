using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ExHandler;
using System.Security.Cryptography;

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
                string RecvStr = Encoding.ASCII.GetString(client.DownloadData("http://v2.api.cusx.net/version/" + tag));
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
            catch (Exception)
            {
                return CheckStatus.Failed;
            }
        }

        public static bool Download(string tag)
        {
            try
            {
                WebClient client = new WebClient();
                string newlink = Encoding.ASCII.GetString(client.DownloadData("http://v2.api.cusx.net/download/" + tag));
                client.DownloadFile(newlink, Application.StartupPath + @"\new.exe");
                string hash = Encoding.ASCII.GetString(client.DownloadData("http://v2.api.cusx.net/hash/" + tag));
                client.Dispose();

                if (checkHash(Application.StartupPath + @"\new.exe", hash))
                {
                    File.Move(Application.ExecutablePath, Application.StartupPath + @"\old.exe");
                    File.Move(Application.StartupPath + @"\new.exe", Application.StartupPath + @"\EasyCUSX.exe");
                    return true;
                }
                else
                {
                    File.Delete(Application.StartupPath + @"\new.exe");
                    return false;
                }
            }
            catch (WebException)
            {
                File.Delete(Application.StartupPath + @"\new.exe");
                return false;
            }
            catch (Exception ex)
            {
                File.Delete(Application.StartupPath + @"\new.exe");
                if (File.Exists(Application.StartupPath + @"\old.exe"))
                {
                    File.Delete(Application.StartupPath + @"\EasyCUSX.exe");
                    File.Move(Application.StartupPath + @"\old.exe", Application.StartupPath + @"\EasyCUSX.exe");
                }
                new ExceptionHandler(ex.ToString());
                return false;
            }
        }

        private static bool checkHash(string fileName, string md5target)
        {
            FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] bytesHash = md5.ComputeHash(file);
            md5.Clear();
            file.Dispose();
            string sTemp = "";
            for (int i = 0; i < bytesHash.Length; i++)
            {
                sTemp += bytesHash[i].ToString("X").PadLeft(2, '0');
            }
            return sTemp.ToLower() == md5target;
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