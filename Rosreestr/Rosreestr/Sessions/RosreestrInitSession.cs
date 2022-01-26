using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace Rosreestr.Sessions
{
    public partial class RosreestrSession
    {
        /// <summary>
        /// Инициализация начала работы с сайтом росреестра
        /// </summary>
        private class RosreestrInitSession : IRosreestrInitSession
        {
            private readonly RosreestrSession _session;

            public RosreestrInitSession(RosreestrSession session)
            {
                _session = session;

                #region Подключение к сайту росреестра

                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(ROSR_EGRN);
                    request.ProtocolVersion = HttpVersion.Version11;
                    request.Method = WebRequestMethods.Http.Get;
                    request.CookieContainer = _session._cookieContainer;
                    request.Timeout = 3 * 60 * 1000;

                    // Получение кукисов для работы с сайтом
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader respStream = new StreamReader(response.GetResponseStream()))
                    {
                        string s = respStream.ReadToEnd();
                    }
                }

                // кукис для поддержания сессии между запросами
                _session._cookieSession = _session._cookieContainer.GetCookies(new Uri(POST_REQUEST_URL))["JSESSIONID_8"];
                if (_session._cookieSession == null)
                    throw new ArgumentNullException("Не удалось создать сессию для сайта rosreestr");

                #endregion

                #region Инициализация работы с сайтом

                {
                    string body = "init" + GROUP_SEPARATOR;
                    HttpWebRequest request = _session.CreatePostRequest(POST_INIT_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "init", Request = body, Response = s });

                        try
                        {
                            _session._vaadinSecurityKey = json["Vaadin-Security-Key"].Value<string>();
                        }
                        catch
                        {
                            throw new InvalidOperationException("Не удалось получить код безопасности для сайта rosreestr");
                        }
                    }
                }

                #endregion

                #region Focus

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "focus" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "init", Request = body, Response = s });
                    }
                }

                #endregion

                #region Вход на сайт

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "36" + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "c" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  _session._key + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "curText" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Login", Request = body, Response = s });
                    }
                }

                #endregion

                #region Blur

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "8" + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "c" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR + FIELD_SEPARATOR +
                                  "PID12" + FIELD_SEPARATOR +
                                  "blur" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "blur", Request = body, Response = s });
                    }
                }

                #endregion

                #region Доступ к форме выбора "Мои счета" - "Мои заявки" - "Поиск объектов"

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "true" + FIELD_SEPARATOR +
                                  "PID30" + FIELD_SEPARATOR +
                                  "state" + FIELD_SEPARATOR +
                                  "b" + RECORD_SEPARATOR +
                                  "1,565,217,false,false,false,false,1,26,13" + FIELD_SEPARATOR +
                                  "PID30" + FIELD_SEPARATOR +
                                  "mousedetails" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo
                        {
                            Name = "Доступ к форме выбора Мои счета - Мои заявки - Поиск объектов",
                            Request = body,
                            Response = s
                        });
                    }
                }

                #endregion
            }

            /// <summary>
            /// Открывает форму поиска объектов недвижимости
            /// </summary>
            public IRosreestrRealEstateSearchSession OpenRealEstateSearchForm()
            {
                if (_session.HasError)
                {
                    _session.OnErrorThrown("Ошибка сервиса росреестра. OpenRealEstateSearchForm");
                    return null;
                }

                try
                {
                    return new RosreestrRealEstateSearchSession(_session);
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. OpenRealEstateSearchForm");
                }

                return null;
            }

            /// <summary>
            /// Открывает форму поиска заказанных выписок
            /// </summary>
            public IRosreestrNumberSearchSession OpenNumberSearchFrom()
            {
                if (_session.HasError)
                {
                    _session.OnErrorThrown("Ошибка сервиса росреестра. OpenNumberSearchFrom");
                    return null;
                }

                try
                {
                    return new RosreestrNumberSearchSession(_session);
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. OpenRealEstateSearchForm");
                }

                return null;
            }
        }
    }
}
