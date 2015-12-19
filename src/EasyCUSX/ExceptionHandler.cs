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
                File.AppendAllText(Application.StartupPath + @"\Exception.txt", string.Format("\r\n====异常信息====\r\n时间:{0}\r\n====错误堆栈====\r\n{1}\r\n====错误堆栈结束====\r\n====异常信息结束====\r\n", DateTime.Now.ToLocalTime().ToString(), exStack));
                Console.WriteLine(string.Format("[{0}] Exception Handler捕获了一个异常:\r\n{1}", DateTime.Now.ToLocalTime().ToString(), exStack));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[{0}] Exception Handler错误:\r\n{1}", DateTime.Now.ToLocalTime().ToString(), ex.ToString()));
            }
        }
    }
}
