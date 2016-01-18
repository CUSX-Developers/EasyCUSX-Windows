using System;
using System.IO;
using System.Windows.Forms;

namespace ExHandler
{
    class ExceptionHandler
    {
        public ExceptionHandler(string exStack)
        {
            try
            {
                File.AppendAllText(Application.StartupPath + @"\Exception.txt", string.Format("\r\n====Exception====\r\nTime:{0}\r\n====Stack====\r\n{1}\r\n====Stack End====\r\n====Exception End====\r\n", DateTime.Now.ToLocalTime().ToString(), exStack));
                Console.WriteLine(string.Format("[{0}] Exception Handler收到了一个异常:\r\n{1}", DateTime.Now.ToLocalTime().ToString(), exStack));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[{0}] Exception Handler错误:\r\n{1}", DateTime.Now.ToLocalTime().ToString(), ex.ToString()));
            }
        }
    }
}
