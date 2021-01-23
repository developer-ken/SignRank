using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SignRank
{
    class CPDailyAPI
    {
        public const int SCHOOL_CODE = 11276;
        public const string ACCESS_TOKEN = "5e5b7d74e7b43fc22a851e615fd2792f";
        public const string APPID = "amp-ios-11276";
        public const string UA = "Mozilla/5.0 (Linux; Android 10; Mi 10 Build/QKQ1.191117.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/83.0.4103.101 Mobile Safari/537.36  cpdaily/8.2.17 wisedu/8.2.17 okhttp/3.12.4";

        public struct SignInfo
        {
            public DateTime sendTime;
            public DateTime deadLine;
            public bool handled;
        }
        public JObject HTTP_POST(string url, JObject payload)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";

            req.Headers.Add("accessToken", ACCESS_TOKEN);
            req.Headers.Add("appId", APPID);
            req.UserAgent = UA;

            byte[] data = Encoding.UTF8.GetBytes(payload.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            if (resp.ContentType.IndexOf("json") >= 0)
            {
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return (JObject)JsonConvert.DeserializeObject(result);
            }
            else return null;
        }

        public SignInfo[] GetSignListOf(int studentid)
        {
            var list = new List<SignInfo>();
            int start = 0;
            int step = 200;
            int lastupdate = 0;
            do
            {
                var payload = new JObject();
                payload.Add("userId", studentid.ToString());
                payload.Add("schoolCode", SCHOOL_CODE.ToString());
                payload.Add("sign", MD5Encrypt(ACCESS_TOKEN + SCHOOL_CODE.ToString() + studentid.ToString()));
                payload.Add("timestamp", lastupdate.ToString());

                var page = new JObject();
                page.Add("start", start);
                page.Add("size", step);
                page.Add("total", "");
                payload.Add("page", page);

                var result = HTTP_POST("http://messageapi.campusphere.net/message_pocket_web/V2/mp/restful/mobile/message/extend/get", payload);
                if (result.Value<int>("status") != 200) throw new Exception(result.Value<string>("msg"));
                try
                {
                    if (result["page"] == null || ((JArray)result["msgsNew"]).Count == 0) break;
                }
                catch
                {
                    break;
                }
                foreach (JObject jb in (JArray)result["msgsNew"])
                {
                    list.Add(new SignInfo() {
                        handled = jb.Value<bool>("isHandled"),
                        sendTime = ConvertToDateTime(jb.Value<string>("sendTime")),

                    });
                }
                if (result["page"].Value<int>("size") < step) break;
            } while (true);
            return list.ToArray();
        }

        public static DateTime ConvertToDateTime(string timestamp)
        {
            DateTime time = DateTime.MinValue;
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            if (timestamp.Length == 10)        //精确到秒
            {
                time = startTime.AddSeconds(double.Parse(timestamp));
            }
            else if (timestamp.Length == 13)   //精确到毫秒
            {
                time = startTime.AddMilliseconds(double.Parse(timestamp));
            }
            return time;
        }

        public static string MD5Encrypt(string password)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder tmp = new StringBuilder();
            foreach (byte i in hashedDataBytes)
            {
                tmp.Append(i.ToString("x2"));
            }
            return tmp.ToString();
        }
    }
}
