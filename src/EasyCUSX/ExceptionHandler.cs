using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ExceptionHandler {
    class ExHandlerMain {

        public event EventHandler ExceptionSaved;
        public event EventHandler ExceptionUploaded;

        public void save(Exception _Exception) {
            try {
                FileStream fs = new FileStream(Application.StartupPath + @"\Exception.txt", FileMode.Append);
                byte[] data = new UTF8Encoding().GetBytes(_Exception.ToString() + "\r\n\r\n");
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Close();
                this.OnExceptionSaved(new EventArgs());
            } catch (Exception ex) {
                MessageBox.Show("ExceptionHandler throw a exception while save exception to local storage.\r\n\r\n-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n" + ex.ToString());
            }
        }

        public void Upload() {
            if (File.Exists(Application.StartupPath + @"\Exception.txt")) {
                try { 
                    //TODO:上传错误信息到服务器
                    //upload
                    //File.Delete(Application.StartupPath + @"\Exception.txt");
                    //this.OnExceptionUploaded(new EventArgs());
                } catch (Exception ex) {
                    MessageBox.Show("ExceptionHandler throw a exception while upload exception to server.\r\n\r\n-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\r\n" + ex.ToString());
                }
            }
        }

        private void OnExceptionSaved(EventArgs eventArgs) {
            if (this.ExceptionSaved != null)
            {
                this.ExceptionSaved(this, eventArgs);
            } 
        }

        private void OnExceptionUploaded(EventArgs eventArgs) {
            if (this.ExceptionUploaded != null)
            {
                this.ExceptionUploaded(this, eventArgs);
            } 
        }
    }
}
