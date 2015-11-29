using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketHelper
{
    class SocketHelperMain
    {

        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public bool SocketConnect(string IP, int Port, out string Result)
        {
            IPAddress remoteIP = IPAddress.Parse(IP);
            IPEndPoint ipe = new IPEndPoint(remoteIP, Port);
            try
            {
                s.Connect(ipe);
                Result = "连接成功";
                Console.WriteLine("Connected: {0}:{1}", IP, Port.ToString());
                return true;
            }
            catch (Exception)
            {
                Result = "套接字失败";
                return false;
            }
        }

        public bool Send4Recv(string SendStr, out string RecvStr)
        {
            Console.WriteLine("Send: {0}", SendStr);
            byte[] bytesSendStr = new byte[1024];
            bytesSendStr = Encoding.ASCII.GetBytes(SendStr);
            try
            {
                s.Send(bytesSendStr, bytesSendStr.Length, 0);
                byte[] RecvBytes = new byte[1024];
                int bytes = 0;
                bytes = s.Receive(RecvBytes, RecvBytes.Length, 0);
                RecvStr = Encoding.ASCII.GetString(RecvBytes, 0, bytes);
                Console.WriteLine("Recv: {0}", RecvStr);
                return true;
            }
            catch (Exception)
            {
                RecvStr = "发送失败";
                return false;
            }
        }

        public bool JustSend(string SendStr, out string ResultMsg)
        {
            Console.WriteLine("Send: {0}", SendStr);
            byte[] bytesSendStr = new byte[1024];
            bytesSendStr = Encoding.ASCII.GetBytes(SendStr);
            try
            {
                s.Send(bytesSendStr, bytesSendStr.Length, 0);
                ResultMsg = "发送成功";
                return true;
            }
            catch (Exception)
            {
                ResultMsg = "发送失败";
                return false;
            }
        }

        public void SocketClose()
        {
            s.Close();
            s.Dispose();
        }
    }
}
