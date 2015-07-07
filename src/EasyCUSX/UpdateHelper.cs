using System;
using System.Text;
using System.Net;
using System.IO;
using System.Windows.Forms;
//------------
using ExceptionHandler;

namespace UpdateHelper {
    class UpdaterMain {

        ExHandlerMain exh = new ExHandlerMain();

        public enum CheckStatu {
            noNewVersion = 0,
            newVersion = 1,
            Failed = 2,
        }

        public CheckStatu Check(string _currentVersion) {
            WebClient client = new WebClient();
            try {
                string RecvStr = Encoding.ASCII.GetString(client.DownloadData("http://api.cusx.net/api/EasyCUSX/getinfo.php?edition=pc&request=latestversion"));
                client.Dispose();
                if (_currentVersion == RecvStr) {
                    return CheckStatu.noNewVersion;
                }
                else {
                    return CheckStatu.newVersion;
                }
            }
            catch (Exception ex) {
                exh.save(ex);
                return CheckStatu.Failed;
            }
        }

        public bool Download()
        {
            try {
                File.Move(Application.ExecutablePath, Application.StartupPath + @"\old.exe");
                try {
                    WebClient client = new WebClient();
                    string newlink = Encoding.ASCII.GetString(client.DownloadData("http://api.cusx.net/api/EasyCUSX/getinfo.php?edition=pc&request=link"));
                    client.DownloadFile(newlink, Application.StartupPath + @"\EasyCUSX.exe");
                    client.Dispose();
                    return true;
                }
                catch (Exception ex) {
                    if (File.Exists(Application.StartupPath + @"\EasyCUSX.exe")) {
                        File.Delete(Application.StartupPath + @"\EasyCUSX.exe");
                    }
                    File.Move(Application.StartupPath + @"\old.exe", Application.StartupPath + @"\EasyCUSX.exe");
                    exh.save(ex);
                    return false;
                }
            }
            catch (Exception ex) {
                exh.save(ex);
                return false; 
            }

        }

        public void CleanUp() {
            try {
                if (File.Exists(Application.StartupPath + @"\old.exe")) {
                    File.Delete(Application.StartupPath + @"\old.exe");
                }
            }
            catch (Exception ex) {
                exh.save(ex);
            }
        }
    }
}
