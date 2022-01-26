using DAL;
using Ionic.Zip;
using Rosreestr.Sessions;
using Rosreestr.Sessions.Classes;
using Rosreestr.Xml;
using Shared;
using Shared.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.Threading.Thread;
using System.Threading.Tasks;
using System.Xml;
using NLog;

namespace Pipelines.LoadingPipeline
{
    public class LoadWorker : IWorker
	{
		private readonly RosreestrSession _session;
		private readonly LoadOrder _order;
		private RequestDownloadInfo _downloadInfo;
		private IRosreestrNumberSearchSession _searchForm;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly string _savePath = SETTINGS.XML_FOLDER;
		private static int _activeThreadsCount;

		public static bool OutOfCapacity => _activeThreadsCount == SETTINGS.UPLOAD_XML_THREAD_COUNT;

		public LoadWorker(RosreestrSession session, LoadOrder order)
		{
			_session = session;
			_order = order;

			_activeThreadsCount++;
			Repository.SetBusyLoader(order.SessionKey, order.NumRequest);
			Logger.Info($"Начинаю загрузку. Текущее число занятых потоков = {_activeThreadsCount}");
		}

		public async Task RunAsync()
		{
			await Task.Run(() => Start());
		}

		private void Start()
		{
			CurrentThread.Name = "LoadWorker";

			try
			{
				while (!_session.Success)
				{
					_session.Deactivate();
					_session.Init();

					if ((_searchForm = _session.InitSession?.OpenNumberSearchFrom()) == null)
						continue;

					_downloadInfo = _searchForm.DownloadRequest(_order.NumRequest, _savePath);

					if (_downloadInfo.NoLink) // запрос есть, но еще не обработан
					{
						Logger.Warn($"{_order}. Запрос ещё не обработан Росреестром. Повторю загрузку этой выписки позже");
						break;
					}

					if (_downloadInfo.NoRequest) // случалось что Росреестр терял реквесты выписок. Если такое произошло - откатываем до подготовленного и попробуем ещё раз
					{
						RollBackToPrepared();
						break;
					}
					else
					{
						Logger.Info($"{_order}. Архив успешно скачан.");
						_searchForm.Found = true;
					}

					if (_searchForm.Found)
					{
						BeginXmlProcessing();
						break;
					}
				}
			}
			catch
			{
				throw new OrderingFailedException(_order.ID);
			}
			finally
			{
				Dispose();
			}
		}

		private void BeginXmlProcessing()
		{
			string destDirectory = Path.Combine(_savePath, "unzipped");
			FileInfo fileInfo = new FileInfo(_downloadInfo.FilePath);
			UnzipFile(fileInfo, destDirectory);
		}

		private void UnzipFile(FileInfo fileInfo, string destDirectory)
		{
			Logger.Info($"Начинаю разархивацию для потока {CurrentThread.ManagedThreadId}");

			try
			{
				if (!Directory.Exists(destDirectory))
					Directory.CreateDirectory(destDirectory);

				using (ZipFile zip = ZipFile.Read(fileInfo.FullName)) // основной zip-файл
				using (MemoryStream nestedZipMemory = new MemoryStream())
				using (MemoryStream xmlMemory = new MemoryStream())
				using (StreamReader xmlMemoryReader = new StreamReader(xmlMemory, Encoding.UTF8))
				{
					ZipEntry entry = zip.Entries.FirstOrDefault(x => x.FileName.EndsWith(".zip"));

					entry.Extract(nestedZipMemory);

					nestedZipMemory.Position = 0;
					using (ZipFile nestedZip = ZipFile.Read(nestedZipMemory)) // вложенный zip-файл
					{
						ZipEntry xmlEntry = nestedZip.Entries.FirstOrDefault(x => x.FileName.EndsWith(".xml"));
						xmlEntry.Extract(xmlMemory);
						xmlMemory.Position = 0;
					}

					string xmlString = xmlMemoryReader.ReadToEnd();
					XmlDocument doc = new XmlDocument();
					doc.LoadXml(xmlString);

					XmlParserFactory xmlReestrParser = new XmlParserFactory();
					IXmlReestrParser parser = xmlReestrParser.GetReestrParser(doc);

					string tmp = Path.Combine(destDirectory, parser.RequeryNumber + ".xml");
					doc.Save(tmp);

					string htmlText = parser.GetHtmlText();

					File.WriteAllText(Path.Combine(destDirectory, parser.RequeryNumber + ".html"), htmlText);

					Logger.Info($"Разархивированно успешно для потока {CurrentThread.ManagedThreadId}");

					UploadXmlData(Path.Combine(destDirectory, _order.NumRequest + ".xml"), _order.ID);
					DeleteUnnecessaryFiles(Path.Combine(destDirectory, _order.NumRequest));
				}

				if (File.Exists(_downloadInfo.FilePath))
					File.Delete(fileInfo.FullName);
			}
			catch
			{
				throw new OrderingFailedException(_order.ID);
			}
		}

		private void UploadXmlData(string fileName, int id)
		{
			Logger.Info($"Загружаю файлы в базу данных для потока {CurrentThread.ManagedThreadId}");

			try
			{
				string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
				string dirPath = Path.GetDirectoryName(fileName);

				// Загружаем xml-файл
				XmlDocument doc = new XmlDocument { PreserveWhitespace = false };
				doc.Load(fileName);

				XmlParserFactory xmlReestrParser = new XmlParserFactory();
				IXmlReestrParser parser = xmlReestrParser.GetReestrParser(doc);

				string htmlText = null;
				string htmlPath = Path.Combine(dirPath, fileNameNoExt + ".html");
				if (File.Exists(htmlPath))
					htmlText = File.ReadAllText(htmlPath);

				AddXmlInfo(doc, htmlText, id);
			}
			catch
			{
				throw new OrderingFailedException(_order.ID);
			}
		}

		private void AddXmlInfo(XmlDocument xml, string html, int id)
		{
			try
			{
				XmlParserFactory xmlReestrParser = new XmlParserFactory();
				IXmlReestrParser reestr = xmlReestrParser.GetReestrParser(xml);

				string xmlData = xml.InnerXml;
				string htmlData = html;
				string xslHref = reestr.XslHref;

				Repository.AddXmlData(id, xmlData, htmlData, xslHref);

				if (reestr.Persons.Count > 0)
					CreateEGRP(reestr.Persons, id);

				if (reestr.Governances.Count > 0)
					Repository.SetGovResult(id);

				if (reestr.Organizations.Count > 0)
					Repository.SetOrganizationResult(id);

				if (reestr.Persons.Count == 0 && reestr.Governances.Count == 0 && reestr.Organizations.Count == 0)
					Repository.SetNoXmlData(id);
			}
			catch
			{
				throw new OrderingFailedException(_order.ID);
			}
		}

		private void CreateEGRP(IEnumerable<XmlPerson> persons, int id)
		{
			Logger.Info($"Создаю ЕГРП для потока {CurrentThread.ManagedThreadId}");

			IEnumerable<EGRP> egrpList = persons.Select(person => new EGRP
			{
				ID_Pipeline = id,
				FIO = person.FullName.Trim(),
				DateReg = person.RegDate,
				Fraction = person.Fraction,
				FullFraction = person.FullFraction
			});

			Repository.CreateEGRP(egrpList);
		}

		private void DeleteUnnecessaryFiles(string file)
		{
			Logger.Info($"Удаляю ненужные файлы для потока {CurrentThread.ManagedThreadId}");

			string xml = file + ".xml";
			string html = file + ".html";

			if (File.Exists(xml))
				File.Delete(xml);

			if (File.Exists(html))
				File.Delete(html);
		}

		private void RollBackToPrepared()
		{
			Logger.Error($"{_order}. Росреестр потерял выписку???. Откатываю до подготовленного");
			Repository.RollBackToPrepared(_order.ID);
		}

		public void Dispose()
		{
			Repository.UpdateLastUploadAttempt(_order.ID);
			Repository.SetFreeLoader(_order.SessionKey);
			Logger.Info($"Освободил поток для {_order}. Текущее количество = {_activeThreadsCount}");
			_activeThreadsCount--;
		}
	}
}