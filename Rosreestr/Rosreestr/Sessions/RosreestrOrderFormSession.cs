using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;
using Rosreestr.Sessions.Classes;
using Shared.Classes;

namespace Rosreestr.Sessions
{
	public partial class RosreestrSession
	{
		private class RosreestrOrderFormSession : IRosreestrOrderFormSession
		{
			private readonly RosreestrSession _session;
			private readonly AddressSearchInfo _address;
			private readonly RosreestrRealEstateSearchResultsSession _owner;
			private string _captchaUrl;
			private readonly bool _annulCanClose;

			public string ClosePID { get; }

			public string SendPID { get; }

			public string SignAndSendPID { get; }

			public string ChangeCaptchaPID { get; }

			public string CaptchaPID { get; }

			public string RegisteredPID { get; private set; }

			public string ContinuePID { get; private set; }

			public string RequestInfoPID { get; }

			public string RequestNumber { get; private set; }

			public bool HasSuccess { get; private set; }

			public bool HasTimeout { get; private set; }

			public bool CaptchaError { get; private set; }

			public bool IsAnnul { get; }

			public string ResolvedCaptcha { get; set; }

			public RosreestrOrderFormSession(
				RosreestrSession session, RosreestrRealEstateSearchResultsSession owner,
				AddressSearchInfo address, bool withCaptcha, bool isanul = false)
			{
				_session = session;
				_address = address;
				_owner = owner;

				string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
							  "977" + FIELD_SEPARATOR +
							  "PID0" + FIELD_SEPARATOR +
							  "height" + FIELD_SEPARATOR +
							  "i" + RECORD_SEPARATOR +
							  "755" + FIELD_SEPARATOR +
							  "PID0" + FIELD_SEPARATOR +
							  "width" + FIELD_SEPARATOR +
							  "i" + RECORD_SEPARATOR +
							  "1903" + FIELD_SEPARATOR +
							  "PID0" + FIELD_SEPARATOR +
							  "browserWidth" + FIELD_SEPARATOR +
							  "i" + RECORD_SEPARATOR +
							  "551" + FIELD_SEPARATOR +
							  "PID0" + FIELD_SEPARATOR +
							  "browserHeight" + FIELD_SEPARATOR +
							  "i" + RECORD_SEPARATOR +
							  _address.ID + FIELD_SEPARATOR +
							  _owner.SearchResultsPID + FIELD_SEPARATOR +
							  "clickedKey" + FIELD_SEPARATOR +
							  "s" + RECORD_SEPARATOR +
							  "3" + FIELD_SEPARATOR +
							  _owner.SearchResultsPID + FIELD_SEPARATOR +
							  "clickedColKey" + FIELD_SEPARATOR +
							  "s" + RECORD_SEPARATOR +
							  "1,938,273,false,false,false,false,8,-1,-1" + FIELD_SEPARATOR +
							  _owner.SearchResultsPID + FIELD_SEPARATOR +
							  "clickEvent" + FIELD_SEPARATOR +
							  "s" + RECORD_SEPARATOR + FIELD_SEPARATOR +
							  _owner.SearchResultsPID + FIELD_SEPARATOR +
							  "selected" + FIELD_SEPARATOR + "c";

				HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				using (StreamReader reader = new StreamReader(response.GetResponseStream()))
				{
					string s = reader.ReadToEnd();

					JObject json = _session.GetJson(s);
					_session._actions.Add(new ActionInfo { Name = "Получает доступ к форме заказа выписок", Request = body, Response = s });

					if (s.Contains("Запрос сведений по аннулированным объектам невозможен"))
					{
						IsAnnul = true;

						try
						{
							ClosePID = json["changes"][1][2][2][3][2][2][1]["id"].Value<string>();
							_annulCanClose = true;
						}
						catch
						{
							// Был случай, когда вместо формы было просто всплывающее уведомление об аннулированном объекте
							_annulCanClose = false;
						}

						return;
					}

					if (isanul) return;

					// ищем ссылку на капчу
					if (withCaptcha)
					{
						if (!(json.SelectToken("changes..[?(@.type == 'image' && @.mimetype == 'application/octet-stream')]") is JObject captchaInfo))
						{
							Thread.Sleep(5000);
							s = _session.EmptyRequest();
							json = _session.GetJson(s);
						}

						captchaInfo = json.SelectToken("changes..[?(@.type == 'image' && @.mimetype == 'application/octet-stream')]") as JObject;
						if (captchaInfo == null)
							throw new InvalidOperationException("Не удалось получить капчу");

						string relSrc = captchaInfo["src"].Value<string>();
						_captchaUrl = BASE_URL + "/" + relSrc.TrimStart('/');
					}

					ClosePID = json["changes"][1][2][2][5][2][4][1]["id"].Value<string>();
					SendPID = json["changes"][1][2][2][5][2][2][1]["id"].Value<string>();
					SignAndSendPID = json["changes"][1][2][2][5][2][3][1]["id"].Value<string>();
					CaptchaPID = json["changes"][1][2][2][4][2][4][2][2][1]["id"].Value<string>();
					ChangeCaptchaPID = json["changes"][1][2][2][4][2][4][3][1]["id"].Value<string>();
					RequestInfoPID = json["changes"][1][2][2][3][2][1]["id"].Value<string>();

					// Массив с доп. данными по адресу
					JArray additionDataArray = (JArray)json["changes"][1][2][2][2][3][2];
					foreach (JArray arr in additionDataArray.OfType<JArray>())
					{
						try
						{
							string dataName = arr[2][2].Value<string>();
							if (string.IsNullOrEmpty(dataName))
								continue;

							dataName = dataName.ToLower();
							if (dataName.Contains("статус"))
							{
								address.Status = arr[3][2].Value<string>();
							}
							else if (dataName.Contains("стоимость"))
							{
								address.CadastralCost = RosreestrSession.ParseDecimal(arr[3][2].Value<string>());
							}
							else if (dataName.Contains("дата") && dataName.Contains("стоимости"))
							{
								address.CadastralCostDate = RosreestrSession.ParseDate(arr[3][2].Value<string>());
							}
							else if (dataName.Contains("этажность"))
							{
								address.NumStoreys = arr[3][2].Value<string>();
							}
							else if (dataName.Contains("дата") && dataName.Contains("обновления"))
							{
								address.UpdateInfoDate = RosreestrSession.ParseDate(arr[3][2].Value<string>());
							}
							else if (dataName.Contains("литер") && dataName.Contains("бюро"))
							{
								address.LiterBTI = arr[3][2].Value<string>();
							}
						}
						catch
						{
							throw;
						}
					}
				}
			}

			public IRosreestrOrderFormSession AddCaptcha()
			{
				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. AddCaptcha");
					return null;
				}

				try
				{
					bool noCaptcha = false;
					using (WebClientEx downloader = new WebClientEx())
					{
						downloader.CookieContainer.Add(_session._cookieSession);

						_address.CaptchaBytes = downloader.DownloadData(_captchaUrl); // сохраняем капчу

						if (_address.CaptchaBytes == null || _address.CaptchaBytes.Length == 0)
						{
							noCaptcha = true;
						}

						ResolvedCaptcha = new MonsterCapService(_address.CaptchaBytes).GetResolveResult();
					}

					if (noCaptcha)
					{
						Thread.Sleep(5000);

						using (WebClientEx downloader = new WebClientEx())
						{
							downloader.CookieContainer.Add(_session._cookieSession);

							_address.CaptchaBytes = downloader.DownloadData(_captchaUrl); // сохраняем капчу

							if (_address.CaptchaBytes == null || _address.CaptchaBytes.Length == 0)
							{
								_session.OnErrorThrown("Ошибка сервиса росреестра. Не удалось получить капчу.");
								return null;
							}
						}
					}

					return this;
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Не удалось получить капчу.");
				}

				return null;
			}

			public IRosreestrOrderFormSession SaveCaptcha(string path)
			{
				File.WriteAllBytes(path, _address.CaptchaBytes);
				return this;
			}

			public IRosreestrRealEstateSearchResultsSession Close()
			{
				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. Close order form");
					return null;
				}

				if (IsAnnul && !_annulCanClose)
				{
					return _owner;
				}

				if (HasTimeout)
				{
					return CloseTimeoutPopup();
				}

				try
				{
					string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
								  "943" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "height" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "755" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "width" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "1903" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "browserWidth" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "551" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "browserHeight" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "true" + FIELD_SEPARATOR +
								  ClosePID + FIELD_SEPARATOR +
								  "state" + FIELD_SEPARATOR +
								  "b" + RECORD_SEPARATOR +
								  "1,1012,512,false,false,false,false,1,40,12" + FIELD_SEPARATOR +
								  ClosePID + FIELD_SEPARATOR +
								  "mousedetails" + FIELD_SEPARATOR + "s";

					HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						string s = reader.ReadToEnd();

						JObject json = _session.GetJson(s);
						_session._actions.Add(new ActionInfo { Name = "Закрывает форму заказа выписок", Request = body, Response = s });
					}

					return _owner;
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Close order form");
				}

				return null;
			}

			private IRosreestrRealEstateSearchResultsSession CloseTimeoutPopup()
			{
				try
				{
					string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
								  "388" + FIELD_SEPARATOR +
								  ClosePID + FIELD_SEPARATOR +
								  "positionx" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "1253" + FIELD_SEPARATOR +
								  ClosePID + FIELD_SEPARATOR +
								  "positiony" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "952" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "height" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "755" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "width" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "1423" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "browserWidth" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "339" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "browserHeight" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "true" + FIELD_SEPARATOR +
								  ClosePID + FIELD_SEPARATOR +
								  "close" + FIELD_SEPARATOR + "b";

					HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						string s = reader.ReadToEnd();
						JObject json = _session.GetJson(s);
						_session._actions.Add(new ActionInfo { Name = "Закрывает всплывающее сообщение о таймауте", Request = body, Response = s });
					}

					return _owner;
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. CloseTimeoutPopup");
				}

				return null;
			}

			public IRosreestrOrderFormSession ChangeCaptcha()
			{
				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. ChangeCaptcha");
					return null;
				}

				CaptchaError = false;

				try
				{
					string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
								  "true" + FIELD_SEPARATOR +
								  ChangeCaptchaPID + FIELD_SEPARATOR +
								  "state" + FIELD_SEPARATOR +
								  "b" + RECORD_SEPARATOR +
								  "1,639,167,false,false,false,false,1,55,8" + FIELD_SEPARATOR +
								  ChangeCaptchaPID + FIELD_SEPARATOR +
								  "mousedetails" + FIELD_SEPARATOR + "s";

					HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						string s = reader.ReadToEnd();

						JObject json = _session.GetJson(s);
						_session._actions.Add(new ActionInfo { Name = "Изменение капчи", Request = body, Response = s });

						string relSrc = json["changes"][0][2][1]["src"].Value<string>();
						_captchaUrl = BASE_URL + "/" + relSrc.TrimStart('/');
						_address.CaptchaBytes = null;
					}

					return AddCaptcha();
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. ChangeCaptcha");
				}

				return null;
			}

			public IRosreestrOrderFormSession EnterCaptcha(string value)
			{
				value = ResolvedCaptcha ?? value;

				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. EnterCaptcha");
					return null;
				}

				CaptchaError = false;

				try
				{
					{
						string body = _session._vaadinSecurityKey + GROUP_SEPARATOR + FIELD_SEPARATOR +
									  CaptchaPID + FIELD_SEPARATOR +
									  "focus" + FIELD_SEPARATOR + "s";

						HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						using (StreamReader reader = new StreamReader(response.GetResponseStream()))
						{
							string s = reader.ReadToEnd();
							JObject json = _session.GetJson(s);
							_session._actions.Add(new ActionInfo { Name = "Ввод капчи (focus)", Request = body, Response = s });
						}
					}

					{
						string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
									  value + FIELD_SEPARATOR +
									  CaptchaPID + FIELD_SEPARATOR +
									  "text" + FIELD_SEPARATOR +
									  "s" + RECORD_SEPARATOR +
									  "5" + FIELD_SEPARATOR +
									  CaptchaPID + FIELD_SEPARATOR +
									  "c" + FIELD_SEPARATOR + "i";

						HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						using (StreamReader reader = new StreamReader(response.GetResponseStream()))
						{
							string s = reader.ReadToEnd();
							JObject json = _session.GetJson(s);
							_session._actions.Add(new ActionInfo { Name = "Ввод капчи", Request = body, Response = s });
						}
					}

					{
						string body = _session._vaadinSecurityKey + GROUP_SEPARATOR + FIELD_SEPARATOR +
									  CaptchaPID + FIELD_SEPARATOR +
									  "blur" + FIELD_SEPARATOR + "s";

						HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						using (StreamReader reader = new StreamReader(response.GetResponseStream()))
						{
							string s = reader.ReadToEnd();
							JObject json = _session.GetJson(s);
							_session._actions.Add(new ActionInfo { Name = "Ввод капчи (blur)", Request = body, Response = s });
						}
					}

					return this;
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. EnterCaptcha");
				}

				return null;
			}

			public IRosreestrRealEstateSearchResultsSession Continue()
			{
				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. Continue ordering");
					return null;
				}

				try
				{
					string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
								  "388" + FIELD_SEPARATOR +
								  RegisteredPID + FIELD_SEPARATOR +
								  "positionx" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "1021" + FIELD_SEPARATOR +
								  RegisteredPID + FIELD_SEPARATOR +
								  "positiony" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "952" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "height" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "755" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "width" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "1423" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "browserWidth" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "412" + FIELD_SEPARATOR +
								  "PID0" + FIELD_SEPARATOR +
								  "browserHeight" + FIELD_SEPARATOR +
								  "i" + RECORD_SEPARATOR +
								  "true" + FIELD_SEPARATOR +
								  ContinuePID + FIELD_SEPARATOR +
								  "state" + FIELD_SEPARATOR +
								  "b" + RECORD_SEPARATOR +
								  "1,723,275,false,false,false,false,1,83,10" + FIELD_SEPARATOR +
								  ContinuePID + FIELD_SEPARATOR +
								  "mousedetails" + FIELD_SEPARATOR + "s";

					HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						string s = reader.ReadToEnd();
						JObject json = _session.GetJson(s);
						_session._actions.Add(new ActionInfo { Name = "Продолжить заказ выписок", Request = body, Response = s });
					}

					return _owner;
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Continue ordering");
				}

				return null;
			}

			public IRosreestrOrderFormSession Send()
			{
				HasSuccess = false;
				CaptchaError = false;
				HasTimeout = false;
				RequestNumber = null;

				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. Send order form");
					return null;
				}

				try
				{
					{
						string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
									  "true" + FIELD_SEPARATOR +
									  SendPID + FIELD_SEPARATOR +
									  "state" + FIELD_SEPARATOR +
									  "b" + RECORD_SEPARATOR +
									  "1,464,306,false,false,false,false,1,73,10" + FIELD_SEPARATOR +
									  SendPID + FIELD_SEPARATOR +
									  "mousedetails" + FIELD_SEPARATOR + "s";

						HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						using (StreamReader reader = new StreamReader(response.GetResponseStream()))
						{
							string s = reader.ReadToEnd();
							JObject json = _session.GetJson(s);
							_session._actions.Add(new ActionInfo { Name = "1) Отправка формы заказа выписок", Request = body, Response = s });

							if (s.Contains("Превышен интервал"))
							{
								HasTimeout = true;
								return this;
							}

							if (s.Contains("Ошибка ввода капчи"))
							{
								CaptchaError = true;
								return this;
							}

							if (s.Contains("Ошибка") || s.Contains("ошибка"))
							{
								string err = "";
								try
								{
									err = json["changes"][0][2][3][2][1]["message"].Value<string>();
								}
								catch
								{
									throw;
								}

								_session.OnErrorThrown("Ошибка сервиса росреестра. Send order form." + err);
								return null;
							}

							if (s.Contains("запрос зарегистрирован"))
							{
								RegisteredPID = json["changes"][0][2][3][1]["id"].Value<string>();
								ContinuePID = json.SelectToken("changes..[?(@.caption == 'Продолжить работу')]")["id"].Value<string>();

								JValue val = json.SelectTokens("changes..*[*]").
												  OfType<JValue>().
												  Where(x => x.Type == JTokenType.String).
												  FirstOrDefault(x => x.Value != null && x.Value<string>().Contains("Номер запроса"));
								string requestString = val.Value<string>();

								RequestNumber = ParseRequestNumber(requestString);
								HasSuccess = true;

								return this;
							}

							throw new InvalidOperationException("Не удалось обработать ответ после отправки формы");
						}
					}
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. Send order form");
				}

				return null;
			}

			public IRosreestrOrderFormSession CheckRequestObject(int num)
			{
				if (_session.HasError)
				{
					_session.OnErrorThrown("Ошибка сервиса росреестра. CheckRequestObject " + num);
					return null;
				}

				try
				{
					string body = _session._vaadinSecurityKey + GROUP_SEPARATOR +
								  num + FILE_SEPARATOR + FIELD_SEPARATOR +
								  RequestInfoPID + FIELD_SEPARATOR +
								  "selected" + FIELD_SEPARATOR +
								  "c";

					HttpWebRequest request = _session.CreatePostRequest(POST_REQUEST_URL, body);

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						string s = reader.ReadToEnd();
						JObject json = _session.GetJson(s);
						_session._actions.Add(new ActionInfo { Name = "Продолжить заказ выписок", Request = body, Response = s });
					}

					return this;
				}
				catch (Exception exc)
				{
					_session.OnErrorThrown(exc, "Ошибка сервиса росреестра. CheckRequestObject " + num);
				}

				return null;
			}

			private string ParseRequestNumber(string text)
			{
				Regex regex = new Regex("[0-9]+-[0-9]+");
				if (regex.IsMatch(text))
				{
					string number = regex.Match(text).Value;
					return number.Replace("-", "");
				}

				throw new InvalidOperationException("Не удалось прочитать номер запроса");
			}
		}
	}
}
