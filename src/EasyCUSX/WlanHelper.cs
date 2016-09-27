using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace WlanHelper
{
    class WlanHelperMain
    {
        public class CookieAwareWebClient : WebClient
        {
            private  CookieContainer container = new CookieContainer();
            public bool lockCookie = false;

            protected override WebRequest GetWebRequest(Uri address)
            {
                var servicePoint = ServicePointManager.FindServicePoint(address);
                servicePoint.Expect100Continue = false;
                WebRequest request = base.GetWebRequest(address);
                HttpWebRequest webRequest = request as HttpWebRequest;
                if (webRequest != null)
                {
                    webRequest.KeepAlive = true;
                    webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                    webRequest.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.87 Safari/537.36";
                    webRequest.CookieContainer = container;
                }
                return request;
            }
            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                WebResponse response = base.GetWebResponse(request, result);
                ReadCookies(response);
                return response;
            }
            protected override WebResponse GetWebResponse(WebRequest request)
            {
                WebResponse response = base.GetWebResponse(request);
                ReadCookies(response);
                return response;
            }
            private void ReadCookies(WebResponse r)
            {
                var response = r as HttpWebResponse;
                if (response != null)
                {
                    CookieCollection cookies = response.Cookies;
                    if (!lockCookie)
                    {
                        container.Add(cookies);
                    }
                }
            }
        }
        public static int AUTH_SUCCESS = 0;
        public static int AUTH_ALREADY = 1;

        public static int AUTH_IDENTIFYERROR = 2;
        public static int AUTH_FREEZE = 3;
        public static int AUTH_OVERDEVICE = 4;

        public static int AUTH_UNREACHABLE = 5;
        public static int AUTH_TOKEN_MATCH_FAILED = 6;

        public static int AUTH_UNKNOWN_TYPE = 7;
        public static int AUTH_EXCEPTION = 8;


        public static int DEAUTH_SUCCESS = 0;
        public static int DEAUTH_ALREADY = 1;
        public static int DEAUTH_IDENTIFYERROR = 2;

        public static int DEAUTH_UNREACHABLE = 3;
        public static int DEAUTH_TOKEN_MATCH_FAILED = 4;

        public static int DEAUTH_UNKNOWN_TYPE = 5;
        public static int DEAUTH_EXCEPTION = 6;

        public static int AUTH_GENERAL_NOIDENTIFY = 100;


        public static bool checkGateway(bool backup, out string Result)
        {
            try
            {
                string ip = "172.18.4.11";
                if (backup) {
                    ip = "172.18.4.14";
                }
                string rst;
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(ip);
                bool ok = (pingReply.Status == IPStatus.Success);
                if (!ok)
                {
                    rst = "请先连接到 \"CUSX\" 无线接入点";
                }
                else
                {
                    rst = ip;
                }
                Result = rst;
                return ok; 
            }
            catch (Exception)
            {
                Result = "请先连接到 \"CUSX\" 无线接入点";
                return false;
            }
            
        }

        public static int auth(string ip, string username, string password)
        {
            if (username == string.Empty || password == string.Empty)
            {
                return AUTH_GENERAL_NOIDENTIFY;
            }

            try
            {
                var client = new CookieAwareWebClient();
                client.Headers.Add(HttpRequestHeader.Host, ip);
                client.Headers.Add(HttpRequestHeader.AcceptLanguage, "zh-CN,zh;q=0.8");
                client.Headers.Add(HttpRequestHeader.Referer, "http://" + ip + "/login.php");
                
                //get cookie and token
                string html = client.DownloadString("http://" + ip + "/login.php");

                Regex re = new Regex("<input.+?name=\"token\".+?value=\"(.+?)\">");
                Match m = re.Match(html);
                if (!m.Success)
                {
                    return AUTH_TOKEN_MATCH_FAILED;
                }
                string token = m.Groups[1].Value;
                client.lockCookie = true;

                //login
                html = Encoding.UTF8.GetString(client.UploadValues("http://" + ip + "/login.php?action=login", "POST",
                    new System.Collections.Specialized.NameValueCollection{
                {"token", token},
                {"username", username},
                {"password", password},
                {"type", ""}
                }));
                //client.Headers.Set(HttpRequestHeader.Referer, "http://" + ip + "/index.php");
                //html = Encoding.UTF8.GetString(client.DownloadData("http://" + ip + "/RADACCTlist.php"));
                
                if (html.Contains("成功登陆，祝您冲浪愉快"))
                {
                    return AUTH_SUCCESS;
                }
                else if (html.Contains("您的计算机已经登陆"))
                {
                    return AUTH_ALREADY;
                }
                else if (html.Contains("您的帐号已经登录,或者登录数超过限制"))
                {
                    return AUTH_OVERDEVICE;
                }
                else if (html.Contains("您的帐号已经被冻结"))
                {
                    return AUTH_FREEZE;
                }
                else if (html.Contains("不正确的用户名或密码"))
                {
                    return AUTH_IDENTIFYERROR;
                }
                else
                {
                    return AUTH_UNKNOWN_TYPE;
                }
            }
            catch (Exception)
            {
                return AUTH_EXCEPTION;
            }
        }

        public static int deAuth(string ip, string username, string password)
        {
            if (username == string.Empty || password == string.Empty)
            {
                return AUTH_GENERAL_NOIDENTIFY;
            }

            try
            {
                var client = new CookieAwareWebClient();
                client.Headers.Add(HttpRequestHeader.Host, ip);
                client.Headers.Add(HttpRequestHeader.AcceptLanguage, "zh-CN,zh;q=0.8");
                client.Headers.Add(HttpRequestHeader.Referer, "http://" + ip + "/login.php");

                //get cookie and token
                string html = client.DownloadString("http://" + ip + "/login.php");

                Regex re = new Regex("<input.+?name=\"token\".+?value=\"(.+?)\">");
                Match m = re.Match(html);
                if (!m.Success)
                {
                    return AUTH_TOKEN_MATCH_FAILED;
                }
                string token = m.Groups[1].Value;
                client.lockCookie = true;

                //logout
                html = Encoding.UTF8.GetString(client.UploadValues("http://" + ip + "/login.php?action=logout", "POST",
                    new System.Collections.Specialized.NameValueCollection{
                {"token", token},
                {"username", username},
                {"password", password},
                {"type", ""}
                }));
                //client.Headers.Set(HttpRequestHeader.Referer, "http://" + ip + "/index.php");
                //html = Encoding.UTF8.GetString(client.DownloadData("http://" + ip + "/RADACCTlist.php"));

                if (html.Contains("您下网了"))
                {
                    return DEAUTH_SUCCESS;
                }
                else if (html.Contains("您不在网上"))
                {
                    return DEAUTH_ALREADY;
                }
                else if (html.Contains("不正确的用户名和密码"))
                {
                    return DEAUTH_IDENTIFYERROR;
                }
                else
                {
                    return AUTH_UNKNOWN_TYPE;
                }
            }
            catch (Exception)
            {
                return AUTH_EXCEPTION;
            }
        }
    }
}