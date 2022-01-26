using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rosreestr.Sessions.Classes;
using Shared.Utills;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Rosreestr.Sessions
{
    public partial class RosreestrSession
    {
        private const string ROSR_EGRN = "https://rosreestr.gov.ru/wps/portal/p/cc_present/ir_egrn";
        private const string BASE_URL = ROSR_EGRN + "/!ut/p/z1/jY_BCsIwEEQ_KbNurV63NaQlaIgQrLlIThLQ6kH8frW3gtbubeA9ZlZF1anYp2c-p0e-9enyzsdYnkCNkBQLi3ZTQarG6XpfMgB1GANrtkvIVjvPvGMEVnGOb3yhqS7IGk8rSCuWmAzgaJ4_AQz-j5OPH8cVXz4YgKmJ_0ru1xA65PYFAlPB1Q!!";
        private const string POST_INIT_URL = BASE_URL + "/p0/IZ7_01HA1A42KODT90AR30VLN22003=CZ6_01HA1A42K0IDB0ABHOECR63000=NJUIDL=/?repaintAll=1&sh=900&sw=1440&cw=1423&ch=378&vw=753&vh=1&fr=&tzo=-180&rtzo=-180&dstd=0&dston=false&curdate=1576220966359&wsver=6.8.17";
        private const string POST_REQUEST_URL = BASE_URL + "/p0/IZ7_01HA1A42KODT90AR30VLN22003=CZ6_01HA1A42K0IDB0ABHOECR63000=NJUIDL=/?windowName=1";
        private const string APP_ERROR = "appError";
        private const char FILE_SEPARATOR = (char)0x1C;   // FS
        private const char GROUP_SEPARATOR = (char)0x1D;  // GS
        private const char RECORD_SEPARATOR = (char)0x1E; // RS
        private const char FIELD_SEPARATOR = (char)0x1F;  // US

        private CookieContainer _cookieContainer;
        private readonly List<ActionInfo> _actions = new List<ActionInfo>();
        private readonly DateTime _creationTime = DateTime.Now;
        private readonly string _key;
        private Cookie _cookieSession;
        private string _vaadinSecurityKey;

        /// <summary>
        /// Пустой ответ на запрос
        /// </summary>
        public const string EMPTY_RESPONSE = "for(;;);[{\"changes\":[], \"meta\" : {}, \"resources\" : {}, \"locales\":[]}]";

        /// <summary>
        /// Происходит при возникновении ошибки на любом этапе работы
        /// </summary>
        public event EventHandler<RosreestrEventArgs> ErrorThrown;

        /// <summary>
        /// Происходит при изменении статуса (подключается/подключён)
        /// </summary>
        public event EventHandler<EventArgs> StatusChanged;

        /// <summary>
        /// Произвольная отметка
        /// </summary>
        public bool IsMarked { get; set; }

        /// <summary>
        /// Произвольное имя ключа учётной записи
        /// </summary>
        public string LoginKey { get; }

        /// <summary>
        /// Признак ошибки при работе с росреестром.
        /// В связи с нестабильностью сайта и его сервисов будем считать наличие любой ошибки
        /// неустранимой и требующей пересоздание объекта.
        /// </summary>
        public bool HasError { get; private set; }

        public bool Success { get; set; }

        /// <summary>
        /// Признак процесса подключения
        /// </summary>
        public bool Connecting { get; private set; }

        /// <summary>
        /// Признак подключения
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Открытое подключение
        /// </summary>
        public IRosreestrInitSession InitSession { get; private set; }

        public RosreestrSession(string key)
        {
			_key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public RosreestrSession(ObjPair<string, string> pair)
		{
            _key = pair.Key;
            LoginKey = pair.Value;
		}

        /// <summary>
        /// Подключение к сайту росреестра
        /// </summary>
        public IRosreestrInitSession Init()
        {
            _cookieContainer = new CookieContainer();
            _cookieSession = null;
            _vaadinSecurityKey = null;
            InitSession = null;
            HasError = false;
            Connecting = true;
            Connected = false;

            try
            {
                OnStatusChanged();

                InitSession = new RosreestrInitSession(this);

                _actions.Add(new ActionInfo
                {
                    Name = "Инициализация работы с сайтом",
                    Request = "Init()",
                    Response = "Подключение создано"
                });

                Connecting = false;
                Connected = true;

                OnStatusChanged();
            }
            catch (Exception exc)
            {
                Connecting = false;
                Connected = false;

                _actions.Add(new ActionInfo
                {
                    Name = "Инициализация работы с сайтом",
                    Request = "Init()",
                    Response = exc.ToString()
                });

                OnErrorThrown(exc, "Ошибка при инициализации подключения к росреестру");
            }

            return InitSession;
        }

        /// <summary>
        /// Выполняет пустой запрос. Это нужно, например, для ожидания получения ответа
        /// от предыдущего запроса.
        /// </summary>
        public string EmptyRequest()
        {
            try
            {
                string body = _vaadinSecurityKey + GROUP_SEPARATOR;
                HttpWebRequest request = CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = GetJson(s);
                    _actions.Add(new ActionInfo { Name = "EmptyRequest", Request = body, Response = s });
                    return s;
                }
            }
            catch (Exception exc)
            {
                OnErrorThrown(exc, "Ошибка сервиса росреестра. EmptyRequest");
            }

            return null;
        }

        /// <summary>
        /// Деактивирует объект для возможности повторного подключения к росреестру
        /// </summary>
        public void Deactivate()
        {
            InitSession = null;
            IsMarked = false;
            Connecting = false;
            Connected = false;

            OnStatusChanged();
        }

        internal static string ParseCadastral(string num)
        {
            if (string.IsNullOrEmpty(num))
                return null;

            Regex r = new Regex(">[0-9:]*<");
            string res = string.Empty;

            foreach (Match m in r.Matches(num))
                res += m.Value.TrimStart('>').TrimEnd('<');

            return string.IsNullOrEmpty(res) ? null : res;
        }

        internal static DateTime? ParseDate(string text)
        {
            Regex regex = new Regex("[0-9]{2}.[0-9]{2}.[0-9]{4}");
            if (regex.IsMatch(text))
            {
                string s = regex.Match(text).Value;
                return DateTime.ParseExact(s, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            }

            return null;
        }

        internal static int? ParseInt(string text)
        {
            Regex regex = new Regex("\\b[0-9]+");
            string matches = string.Empty;
            foreach (Match m in regex.Matches(text))
            {
                matches += m.Value;
            }

            if (!string.IsNullOrEmpty(matches))
                return int.Parse(matches, CultureInfo.InvariantCulture);

            return null;
        }

        internal static decimal? ParseDecimal(string text)
        {
            Regex regex = new Regex("\\b[0-9]+[.0-9]*");
            string matches = string.Empty;
            foreach (Match m in regex.Matches(text))
            {
                matches += m.Value;
            }

            if (!string.IsNullOrEmpty(matches))
                return decimal.Parse(matches, CultureInfo.InvariantCulture);

            return null;
        }

        private HttpWebRequest CreatePostRequest(string url, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = WebRequestMethods.Http.Post;
            request.CookieContainer = _cookieContainer;
            request.Accept = "*/*";
            request.UserAgent = "MobileApp"; //"Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:78.0) Gecko/20100101 Firefox/78.0";
            request.Host = "rosreestr.gov.ru";
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.ContentType = "text/plain;charset=utf-8";
            request.ContentLength = bodyBytes.Length;
            request.Headers.Add("Origin", "https://rosreestr.gov.ru");
            request.Headers.Add("DNT", "1");
            request.KeepAlive = true;
            request.Referer = ROSR_EGRN;
            request.Headers.Add("Cookie", string.Format("{0}={1}", _cookieSession.Name, _cookieSession.Value));
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 4 * 60 * 1000;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            return request;
        }

        private JObject GetJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Contains(APP_ERROR))
            {
                OnErrorThrown("Ошибка сервера: " + text);
                return null;
            }

            JObject json = null;
            try
            {
                int startIndex = text.IndexOf('{');
                text = text.Substring(startIndex).TrimEnd(']');
                json = (JObject)JsonConvert.DeserializeObject(text);
            }
            catch (Exception exc)
            {
                OnErrorThrown(exc, "Ошибка при создании json-объекта: " + text);
            }

            return json;
        }

        public void LogActions(string logDir)
        {
            string s = Path.Combine(logDir, (LoginKey ?? GetHashCode().ToString()) + "  " + _creationTime.ToString("dd.MM.yyyy HH_mm_ss_fff"));
            if (!Directory.Exists(s))
                Directory.CreateDirectory(s);

            string time = DateTime.Now.ToString("HH_mm_ss_fff");
            string filePath = Path.Combine(s, time + ".json");
            File.WriteAllLines(filePath, _actions.Select(x => x.ToString()).ToArray());
        }

        internal void OnStatusChanged()
        {
            var tmp = StatusChanged;
            if (tmp != null)
                StatusChanged(this, EventArgs.Empty);
        }

        internal void OnErrorThrown(string message)
        {
            OnErrorThrown(null, message);
        }

        internal void OnErrorThrown(Exception exc, string message)
        {
            _actions.Add(new ActionInfo
            {
                Name = "message",
                Request = "",
                Response = exc == null ? "" : exc.ToString()
            });

            HasError = true;
            Connected = false;
            Connecting = false;

            var tmp = ErrorThrown;
            if (tmp != null)
                ErrorThrown(this, new RosreestrEventArgs(exc, message));
        }

        [Serializable]
        private class ActionInfo
        {
            public DateTime Date;

            public string Name;

            public string Response;

            public string Request;

            public ActionInfo()
            {
                Date = DateTime.Now;
            }

            public override string ToString()
            {
                return $"[{Date}, '{Name:dd.MM.yyyy HH:mm:ss}']\n[{Request}]\n[{Response}]\n";
            }
        }
    }
}
