using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Rosreestr.Sessions
{
    partial class RosreestrSession
    {
        /// <summary>
        /// Форма поиска объектов недвижимости
        /// </summary>
        private class RosreestrRealEstateSearchSession : IRosreestrRealEstateSearchSession
        {
            readonly RosreestrSession _session;
            string _region;
            string _district;
            string _city;
            string _street;
            string _home;
            string _corp;
            string _flat;
            string _cadastralNumber;


            public string PID_Cadastral { get; private set; }

            public string PID_Region { get; private set; }

            public string PID_City { get; private set; }

            public string PID_Town { get; private set; }

            public string PID_Street { get; private set; }

            public string PID_Home { get; private set; }

            public string PID_Flat { get; private set; }

            public string SearchForm_PID { get; private set; }


            public RosreestrRealEstateSearchSession(RosreestrSession session)
            {
                _session = session;

                string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                              "true" + FIELD_SEPARATOR +
                              "PID35" + FIELD_SEPARATOR +
                              "disabledOnClick" + FIELD_SEPARATOR +
                              "b" + RECORD_SEPARATOR +
                              "true" + FIELD_SEPARATOR +
                              "PID35" + FIELD_SEPARATOR +
                              "state" + FIELD_SEPARATOR +
                              "b" + RECORD_SEPARATOR +
                              "1,347,37,false,false,false,false,1,101,13" + FIELD_SEPARATOR +
                              "PID35" + FIELD_SEPARATOR +
                              "mousedetails" + FIELD_SEPARATOR + "s";
                HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = _session.GetJson(s);
                    _session._actions.Add(new ActionInfo { Name = "Доступ к форме поиска объектов недвижимости", Request = body, Response = s });

                    ResetFormPIDs(json);
                }
            }

            internal void ResetFormPIDs(JObject json)
            {
                // абсолютные пути заданы специально
                if (json is null)
                    return;
                try
                {
                    PID_Cadastral = json["changes"][1][2][2][3][2][2][2][3][2][2][2][2][1]["id"].Value<string>();
                    PID_Region = json["changes"][1][2][2][3][2][2][2][3][4][2][2][2][2][1]["id"].Value<string>();
                    PID_City = json["changes"][1][2][2][3][2][2][2][3][4][2][3][2][2][1]["id"].Value<string>();
                    PID_Town = json["changes"][1][2][2][3][2][2][2][3][4][2][4][2][2][1]["id"].Value<string>();
                    PID_Street = json["changes"][1][2][2][3][2][2][2][3][4][4][3][2][2][1]["id"].Value<string>();
                    PID_Home = json["changes"][1][2][2][3][2][2][2][3][4][5][3][3][2][2][1]["id"].Value<string>();
                    PID_Flat = json["changes"][1][2][2][3][2][2][2][3][4][5][3][5][2][2][1]["id"].Value<string>();
                    SearchForm_PID = json["changes"][1][2][2][4][2][2][1]["id"].Value<string>();
                }
                catch
                {
                    ;
                }
            }

            public IRosreestrRealEstateSearchResultsSession SearchAddress(
                string region, string cadastralNumber)
            {
                if (_session.HasError)
                {
                    _session.OnErrorThrown("Ошибка сервиса росреестра. SearchAddress");
                    return null;
                }

                if (!string.IsNullOrEmpty(region))
                    region = region.ToLower();

                if (!string.IsNullOrEmpty(cadastralNumber))
                    cadastralNumber = cadastralNumber.ToLower();

                try
                {
                    if (_region != region)
                    {
                        EnterRegion(region);
                        _region = region;
                    }

                    if (_cadastralNumber != cadastralNumber)
                    {
                        EnterCadastralNumber(cadastralNumber);
                        _cadastralNumber = cadastralNumber;
                    }

                    if (_city != null || _district != null)
                    {
                        EnterCity(null);
                        _city = null;

                        if (_district != null)
                        {
                            EnterTown(null);
                            _district = null;
                        }
                    }

                    if (_street != null)
                    {
                        EnterStreet(null);
                        _street = null;
                    }

                    if (_home != null || _corp != null)
                    {
                        EnterHome(null, null);
                        _home = null;
                        _corp = null;
                    }

                    if (_flat != null)
                    {
                        EnterFlat(null);
                        _flat = null;
                    }
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Fill search address from");
                    return null;
                }

                try
                {
                    return new RosreestrRealEstateSearchResultsSession(_session, this);
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Search address results");
                }

                return null;
            }

            public IRosreestrRealEstateSearchResultsSession SearchAddress(
                string region, string district, string city, string street, string home, string corp, string flat)
            {
                if (_session.HasError)
                {
                    _session.OnErrorThrown("Ошибка сервиса росреестра. SearchAddress");
                    return null;
                }

                if (!string.IsNullOrEmpty(region))
                    region = region.ToLower();

                if (!string.IsNullOrEmpty(district))
                    district = district.ToLower();

                if (!string.IsNullOrEmpty(city))
                    city = city.ToLower();

                if (!string.IsNullOrEmpty(street))
                    street = street.ToLower();

                if (!string.IsNullOrEmpty(home))
                    home = home.ToLower();

                if (!string.IsNullOrEmpty(flat))
                    flat = flat.ToLower();

                try
                {
                    if (_region != region)
                    {
                        EnterRegion(region);
                        _region = region;
                    }

                    if (_district != district)
                    {
                        EnterCity(district);
                        _district = district;
                    }

                    if (_city != city)
                    {
                        if (_district != null)
                        {
                            EnterTown(city);
                        }
                        else
                        {
                            EnterCity(city);
                        }

                        _city = city;
                    }

                    if (_street != street)
                    {
                        EnterStreet(street);
                        _street = street;
                    }

                    if (_home != home || _corp != corp)
                    {
                        EnterHome(home, corp);
                        _corp = corp;
                        _home = home;
                    }

                    if (_flat != flat)
                    {
                        EnterFlat(flat);
                        _flat = flat;
                    }
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Fill search address from");
                    return null;
                }

                try
                {
                    return new RosreestrRealEstateSearchResultsSession(_session, this);
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Search address results");
                }

                return null;
            }

            /// <summary>
            /// Ввод и выбор региона для поиска
            /// </summary>
            /// <param name="region">Имя региона</param>
            private void EnterRegion(string region)
            {
                #region Ввод имени региона и получение идентификатора региона

                int regionKey = -1;
                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  region + FIELD_SEPARATOR +
                                  PID_Region + FIELD_SEPARATOR +
                                  "filter" + FIELD_SEPARATOR +
                                  "s" + RECORD_SEPARATOR + "0" + FIELD_SEPARATOR +
                                  PID_Region + FIELD_SEPARATOR +
                                  "page" + FIELD_SEPARATOR + "i";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Ввод названия региона", Request = body, Response = s });

                        JValue totalMatches = json.SelectToken("changes..totalMatches") as JValue;

                        if (totalMatches == null || totalMatches.Value<int>() != 1)
                        {
                            throw new InvalidOperationException("Ошибка поиска региона");
                        }

                        JValue keyToken = json.SelectToken("changes..key") as JValue;
                        if (keyToken == null)
                        {
                            throw new InvalidOperationException("Ошибка поиска региона");
                        }

                        regionKey = keyToken.Value<int>();
                    }
                }

                #endregion


                #region Выбор региона для поиска

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  regionKey.ToString() + FILE_SEPARATOR + FIELD_SEPARATOR +
                                  PID_Region + FIELD_SEPARATOR +
                                  "selected" + FIELD_SEPARATOR + "c";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Выбор региона по идентификатору", Request = body, Response = s });
                    }
                }

                #endregion
            }

            /// <summary>
            /// Ввод и выбор города для поиска
            /// </summary>
            /// <param name="city">Название города</param>
            private void EnterCity(string city)
            {
                #region Ввод названия города

                int cityKey = -1;
                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  city + FIELD_SEPARATOR +
                                  PID_City + FIELD_SEPARATOR +
                                  "filter" + FIELD_SEPARATOR +
                                  "s" + RECORD_SEPARATOR +
                                  "0" + FIELD_SEPARATOR +
                                  PID_City + FIELD_SEPARATOR +
                                  "page" + FIELD_SEPARATOR + "i";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Ввод названия города", Request = body, Response = s });

                        JValue totalMatches = json.SelectToken("changes..totalMatches") as JValue;
                        if (totalMatches == null || totalMatches.Value<int>() != 1)
                        {
                            throw new KeyNotFoundException("Ошибка поиска города");
                        }

                        JValue keyToken = json.SelectToken("changes..key") as JValue;
                        if (totalMatches == null || totalMatches.Value<int>() != 1)
                        {
                            throw new KeyNotFoundException("Ошибка поиска города");
                        }

                        cityKey = keyToken.Value<int>();
                    }
                }

                #endregion


                #region Выбор города из списка

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  cityKey.ToString() + FILE_SEPARATOR + FIELD_SEPARATOR +
                                  PID_City + FIELD_SEPARATOR +
                                  "selected" + FIELD_SEPARATOR + "c";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Выбор города", Request = body, Response = s });
                    }
                }

                #endregion
            }

            /// <summary>
            /// Ввод и выбор населенного пункта для поиска
            /// </summary>
            /// <param name="town">Название города</param>
            private void EnterTown(string town)
            {
                #region Ввод названия населенного пункта

                int townKey = -1;
                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  town + FIELD_SEPARATOR +
                                  PID_Town + FIELD_SEPARATOR +
                                  "filter" + FIELD_SEPARATOR +
                                  "s" + RECORD_SEPARATOR +
                                  "0" + FIELD_SEPARATOR +
                                  PID_Town + FIELD_SEPARATOR +
                                  "page" + FIELD_SEPARATOR + "i";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Ввод названия населённого пункта", Request = body, Response = s });

                        JValue totalMatches = json.SelectToken("changes..totalMatches") as JValue;
                        if (totalMatches == null || totalMatches.Value<int>() == 0)
                        {
                            throw new KeyNotFoundException("Ошибка поиска населенного пункта");
                        }

                        JValue keyToken = json.SelectTokens("changes..key").LastOrDefault() as JValue;
                        if (keyToken == null)
                        {
                            throw new KeyNotFoundException("Ошибка поиска населенного пункта");
                        }

                        townKey = keyToken.Value<int>();
                    }
                }

                #endregion


                #region Выбор населённого пункта из списка

                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  townKey.ToString() + FILE_SEPARATOR + FIELD_SEPARATOR +
                                  PID_Town + FIELD_SEPARATOR +
                                  "selected" + FIELD_SEPARATOR + "c";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Выбор населённого пункта", Request = body, Response = s });
                    }
                }

                #endregion
            }

            /// <summary>
            /// Ввод улицы для поиска
            /// </summary>
            /// <param name="street">Название улицы</param>
            private void EnterStreet(string street)
            {
                Regex regex = new Regex("[0-9]+(-я|я|ая|-ая|й|-й|ый|-ый|ой|-ой) ");
                if (regex.IsMatch(street))
                {
                    Match match = regex.Match(street);
                    string fullMatch = match.Groups[0].Value;
                    string suffix = match.Groups[1].Value;
                    street = street.Replace(fullMatch, fullMatch.Replace(suffix, ""));
                }

                string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                              street + FIELD_SEPARATOR +
                              PID_Street + FIELD_SEPARATOR +
                              "text" + FIELD_SEPARATOR +
                              "s" + RECORD_SEPARATOR +
                              "6" + FIELD_SEPARATOR +
                              PID_Street + FIELD_SEPARATOR +
                              "c" + FIELD_SEPARATOR + "i";

                HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = _session.GetJson(s);
                    _session._actions.Add(new ActionInfo { Name = "Ввод названия улицы", Request = body, Response = s });
                }
            }

            /// <summary>
            /// Ввод дома для поиска
            /// </summary>
            /// <param name="home">Номер дома</param>
            private void EnterHome(string home, string corp)
            {
                if (!string.IsNullOrEmpty(corp))
                    home = home + "/" + corp;

                string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                              home + FIELD_SEPARATOR +
                              PID_Home + FIELD_SEPARATOR +
                              "text" + FIELD_SEPARATOR +
                              "s" + RECORD_SEPARATOR +
                              "2" + FIELD_SEPARATOR +
                              PID_Home + FIELD_SEPARATOR +
                              "c" + FIELD_SEPARATOR + "i";

                HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = _session.GetJson(s);
                    _session._actions.Add(new ActionInfo { Name = "Ввод номера дома", Request = body, Response = s });
                }
            }

            /// <summary>
            /// Ввод квартиры для поиска
            /// </summary>
            /// <param name="flat">Номер квартиры</param>
            private void EnterFlat(string flat)
            {
                string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                              flat + FIELD_SEPARATOR +
                               PID_Flat + FIELD_SEPARATOR +
                              "text" + FIELD_SEPARATOR +
                              "s" + RECORD_SEPARATOR +
                              "2" + FIELD_SEPARATOR +
                               PID_Flat + FIELD_SEPARATOR +
                              "c" + FIELD_SEPARATOR + "i"; ;

                HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = _session.GetJson(s);
                    _session._actions.Add(new ActionInfo { Name = "Ввод номера квартиры", Request = body, Response = s });
                }
            }

            /// <summary>
            /// Ввод кадастрового номера
            /// </summary>
            /// <param name="number">Кадастровый номер</param>
            private void EnterCadastralNumber(string cadastralNumber)
            {
                string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                              "17" + FIELD_SEPARATOR +
                              PID_Cadastral + FIELD_SEPARATOR +
                              "c" + FIELD_SEPARATOR +
                              "i" + RECORD_SEPARATOR +
                              cadastralNumber + FIELD_SEPARATOR +
                              PID_Cadastral + FIELD_SEPARATOR +
                              "text" + FIELD_SEPARATOR + "s";

                HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string s = reader.ReadToEnd();
                    JObject json = _session.GetJson(s);
                    _session._actions.Add(new ActionInfo { Name = "Установка кадастрового номера", Request = body, Response = s });
                }
            }
        }
    }
}
