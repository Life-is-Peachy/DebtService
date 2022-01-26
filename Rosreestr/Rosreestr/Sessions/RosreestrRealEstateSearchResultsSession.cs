using Newtonsoft.Json.Linq;
using Shared.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Rosreestr.Sessions
{
    partial class RosreestrSession
    {
        private class RosreestrRealEstateSearchResultsSession : IRosreestrRealEstateSearchResultsSession
        {
            private readonly RosreestrSession _session;
            private readonly RosreestrRealEstateSearchSession _searchForm;


            public IList<AddressSearchInfo> Addresses { get; }

            public string SearchResultsPID { get; }

            public string ChangePID { get; }

            public bool NotFound { get; }

            public RosreestrRealEstateSearchResultsSession(RosreestrSession session, RosreestrRealEstateSearchSession searchForm)
            {
                _session = session;
                _searchForm = searchForm;
                Addresses = new List<AddressSearchInfo>();

                string pid = null;
                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "true" + FIELD_SEPARATOR +
                                  _searchForm.SearchForm_PID + FIELD_SEPARATOR +
                                  "state" + FIELD_SEPARATOR +
                                  "b" + RECORD_SEPARATOR +
                                  "1,561,224,false,false,false,false,1,22,14" + FIELD_SEPARATOR +
                                  _searchForm.SearchForm_PID + FIELD_SEPARATOR +
                                  "mousedetails" + FIELD_SEPARATOR + "s";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        JObject json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "1) Запрос на поиск", Request = body, Response = s });

                        // Поиск объектов недвижимости
                        pid = json["changes"][0][2][4][1]["id"].Value<string>();
                    }
                }

                // попытка прочитать первую часть результатов
                int totalRows = 0;
                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "591" + FIELD_SEPARATOR +
                                  pid + FIELD_SEPARATOR +
                                  "positionx" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "964" + FIELD_SEPARATOR +
                                  pid + FIELD_SEPARATOR +
                                  "positiony" + FIELD_SEPARATOR + "i";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    JObject json = null;
                    string jsonText = null;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        jsonText = reader.ReadToEnd();
                        json = _session.GetJson(jsonText);
                        _session._actions.Add(new ActionInfo { Name = "2) Запрос на поиск", Request = body, Response = jsonText });
                    }

                    int times = 0;
                    List<JArray> rows = new List<JArray>();
                    while (!jsonText.Contains("Не найдены") && times++ < 150) // ожидаем результатов запроса
                    {
                        // получаем все найденные строки по адресу
                        rows = json.SelectTokens("changes..*").
                                    Where(x => x.HasValues && (x.First is JValue) && x.First.Value<string>() == "tr").
                                    OfType<JArray>().
                                    ToList();

                        if (rows.Count == 0)
                        {
                            Thread.Sleep(1000);
                            jsonText = _session.EmptyRequest();
                            json = _session.GetJson(jsonText);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (jsonText.Contains("Не удалось получить доступ"))
                    {
                        throw new InvalidOperationException("Не удалось получить доступ к информационному ресурсу");
                    }

                    foreach (JArray arr in rows)
                    {
                        Addresses.Add(new AddressSearchInfo
                        {
                            ID = arr[1]["key"].Value<int>(),
                            CadastralNumber = ParseCadastral(arr[2][2][2][2].Value<string>()),
                            FullAddress = arr[3][2].Value<string>(),
                            ObjType = arr[4].Value<string>(),
                            Square = arr[5].Value<string>(),
                            SteadCategory = arr[6].Value<string>(),
                            SteadKind = arr[7].Value<string>(),
                            FuncName = arr[8].Value<string>(),
                        });
                    }

                    try
                    {
                        // объект с информацией о кол-ве результатов поиска
                        JObject obj = (JObject)json["changes"][1][2][2][6][1];
                        totalRows = obj["totalrows"].Value<int>();
                    }
                    catch
                    {
                        NotFound = true;
                        return;
                    }

                    // Если есть результат, то открывается форма со списком,
                    // поэтому нужно обновить некоторые идентификаторы для возможности повторного поиска
                    if (Addresses.Count > 0)
                    {
                        JObject objSummary = (JObject)json["changes"][1][2][2][5][1];
                        if (!objSummary.ContainsKey("totalrows"))
                        {
                            objSummary = (JObject)json["changes"][1][2][2][6][1];
                        }

                        SearchResultsPID = objSummary["id"].Value<string>();
                        ChangePID = json["changes"][1][2][2][3][2][3][1]["id"].Value<string>();
                    }
                    else
                    {
                        SearchResultsPID = null;
                        ChangePID = null;
                    }
                }

                // чтение оставшихся строк с результатами (если они есть)
                if (totalRows > Addresses.Count && Addresses.Count > 0)
                {
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "10" + FIELD_SEPARATOR +
                                  SearchResultsPID + FIELD_SEPARATOR +
                                  "pagelength" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "0" + FIELD_SEPARATOR +
                                  SearchResultsPID + FIELD_SEPARATOR +
                                  "firstToBeRendered" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "82" + FIELD_SEPARATOR +
                                  SearchResultsPID + FIELD_SEPARATOR +
                                  "lastToBeRendered" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "0" + FIELD_SEPARATOR +
                                  SearchResultsPID + FIELD_SEPARATOR +
                                  "firstvisible" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "15" + FIELD_SEPARATOR +
                                  SearchResultsPID + FIELD_SEPARATOR +
                                  "reqfirstrow" + FIELD_SEPARATOR +
                                  "i" + RECORD_SEPARATOR +
                                  "68" + FIELD_SEPARATOR +
                                  SearchResultsPID + FIELD_SEPARATOR +
                                  "reqrows" + FIELD_SEPARATOR + "i";

                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    JObject json = null;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Завершение запроса на поиск", Request = body, Response = s });
                    }

                    // получаем все найденные строки по адресу
                    List<JArray> rows = json.SelectTokens("changes..*").
                                        Where(x => x.HasValues && (x.First is JValue) && x.First.Value<string>() == "tr").
                                        OfType<JArray>().
                                        ToList();

                    foreach (JArray arr in rows)
                    {
                        Addresses.Add(new AddressSearchInfo
                        {
                            ID = arr[1]["key"].Value<int>(),
                            CadastralNumber = ParseCadastral(arr[2][2][2][2].Value<string>()),
                            FullAddress = arr[3][2].Value<string>(),
                            ObjType = arr[4].Value<string>(),
                            Square = arr[5].Value<string>(),
                            SteadCategory = arr[6].Value<string>(),
                            SteadKind = arr[7].Value<string>(),
                            FuncName = arr[8].Value<string>(),
                        });
                    }
                }
            }

            public IRosreestrOrderFormSession OpenOrderForm(int addressIndex, bool withCaptcha, bool isanul = false)
            {
                if (_session.HasError)
                {
                    _session.OnErrorThrown("Ошибка сервиса росреестра. OpenOrderForm");
                    return null;
                }

                try
                {
                    return new RosreestrOrderFormSession(_session, this, Addresses[addressIndex], withCaptcha, isanul);
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. OpenOrderForm");
                }

                return null;
            }

            public IRosreestrRealEstateSearchSession ChangeSearchParameters()
            {
                if (_session.HasError)
                {
                    _session.OnErrorThrown("Ошибка сервиса росреестра. ChangeSearchParameters");
                    return null;
                }

                try
                {
                    JObject json = null;
                    string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
                                  "true" + FIELD_SEPARATOR +
                                  ChangePID + FIELD_SEPARATOR +
                                  "state" + FIELD_SEPARATOR +
                                  "b" + RECORD_SEPARATOR +
                                  "1,369,114,false,false,false,false,1,20,18" + FIELD_SEPARATOR +
                                  ChangePID + FIELD_SEPARATOR +
                                  "mousedetails" + FIELD_SEPARATOR + "s";
                    HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string s = reader.ReadToEnd();
                        json = _session.GetJson(s);
                        _session._actions.Add(new ActionInfo { Name = "Изменить параметры поиска", Request = body, Response = s });
                    }

                    int times = 0;
                    while (times++ < 10)
                    {
                        try
                        {
                            _searchForm.ResetFormPIDs(json);
                            break;
                        }
                        catch
                        {
                            ; // данные могут прийти не сразу
                        }

                        Thread.Sleep(250);
                        string jsonText = _session.EmptyRequest();
                        json = _session.GetJson(jsonText);
                    }

                    return _searchForm;
                }
                catch (Exception exc)
                {
                    _session.OnErrorThrown(exc, "Ошибка сервиса росреестра. ChangeSearchParameters");
                }

                return null;
            }
        }
    }
}
