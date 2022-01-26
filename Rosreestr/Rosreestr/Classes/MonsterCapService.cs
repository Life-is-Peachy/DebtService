using Newtonsoft.Json;
using System.Net;
using System.Threading;
using System.IO;
using System;

namespace Rosreestr.Sessions.Classes
{
    public class MonsterCapService
    {
        public byte[] CaptchaBytes { get; set; }

        public MonsterCapService(byte[] bytes)
        {
            CaptchaBytes = bytes;
        }

        public string GetResolveResult()
        {
            int TaskID = SendCaptchaToResolvingService();

            JSonRequester SendRequestForResult = new JSonRequester()
            {
                clientKey = "****",
                TaskId = TaskID
            };

            var JTask = JsonConvert.SerializeObject(SendRequestForResult);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.capmonster.cloud/getTaskResult/");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = JTask.Length;

            Thread.Sleep(2500);

            using (var StreamWriter = new StreamWriter(request.GetRequestStream()))
            {
                StreamWriter.Write(JTask);
            }

            var response = (HttpWebResponse)request.GetResponse();
            using (var StreamReader = new StreamReader(response.GetResponseStream()))
            {
                return JsonConvert.DeserializeObject<JSonResult>(StreamReader.ReadToEnd()).Solution.Text.ToString();
            }
        }

        private int SendCaptchaToResolvingService()
        {
            var TextImgToSend = Convert.ToBase64String(CaptchaBytes);

            JSonTask JTask = new JSonTask()
            {
                ClientKey = "****",
                Task = new JTask()
                {
                    Type = "ImageToTextTask",
                    Body = TextImgToSend,
                    Case = false,
                    Numeric = 1,
                    Math = false
                }
            };

            var JString = JsonConvert.SerializeObject(JTask);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.capmonster.cloud/createTask");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = JString.Length;

            using (var StreamWriter = new StreamWriter(request.GetRequestStream()))
            {
                StreamWriter.Write(JString);
            }

            var response = (HttpWebResponse)request.GetResponse();
            using (var StreamReader = new StreamReader(response.GetResponseStream()))
            {
                return JsonConvert.DeserializeObject<JSonAnswer>(StreamReader.ReadToEnd()).TaskId;
            }
        }
    }
}
