using System;
using System.Threading.Tasks;
using static System.Threading.Thread;
using Shared;
using Shared.Classes;
using Rosreestr.Sessions;
using DAL;
using NLog;

namespace Pipelines.PorderingPipeline
{
	public class OrderWorker : IWorker
	{
		private readonly RosreestrSession _session;
		private readonly PreparedOrder _order;
		private IRosreestrRealEstateSearchSession _searchForm;
		private IRosreestrRealEstateSearchResultsSession _resultForm;
		private IRosreestrOrderFormSession _orderForm;

		private const int MAX_ATTEMPT_COUNT = 5;
		private int _rosrFailedResponseCount = 0;

		private static int _activeThreadsCount;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static bool OutOfCapacity => _activeThreadsCount == SETTINGS.ORDER_PIPELINE_THREAD_COUNT;

		public OrderWorker(RosreestrSession session, PreparedOrder order)
		{
			_session = session;
			_order = order;

			_activeThreadsCount++;
			Logger.Info($"Начинаю заказ. Текущее число занятых потоков = {_activeThreadsCount}");
			Repository.SetBusyOrder(_session.LoginKey, _order.CadastralNumber);
		}

		public async Task RunAsync()
		{
			await Task.Run(() => Start());
		}

		private void Start()
		{
			CurrentThread.Name = "OrderWorker";

			try
			{
				while (!_session.Success)
				{
					_session.Deactivate();
					_session.Init();

					if ((_searchForm = _session.InitSession?.OpenRealEstateSearchForm()) == null)
						continue;

					Logger.Info("Начинаю поиск");

					_resultForm = _searchForm.SearchAddress("Ростовская область", _order.CadastralNumber);

					if (_resultForm == null)
					{
						if (_rosrFailedResponseCount != MAX_ATTEMPT_COUNT)
						{
							Logger.Warn($"NULL от Росреестра. Попытка {_rosrFailedResponseCount} из 5");
							_rosrFailedResponseCount++;
							continue;
						}

						CancelOrdering();
						break;
					}

					if (_resultForm.Addresses.Count == 0)
					{
						SetNoAddressesFound();
						break;
					}

					if (_resultForm.Addresses.Count > 1)
					{
						SetMoreThanOneAddressFound();
						break;
					}

					Logger.Info("Адреса успешно получены");

					_orderForm = _resultForm.OpenOrderForm(0, true);

					if (_orderForm == null)
						continue;

					if (_orderForm.IsAnnul)
					{
						SetAnnulOrder();
						break;
					}

					if ((_orderForm = _orderForm.AddCaptcha()) == null || _session.HasError)
					{
						Logger.Error("Сбой ввода капчи");
						continue;
					}

					ProcessOrderForm();
					break;
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

		private void ProcessOrderForm()
		{
			_orderForm.EnterCaptcha(_orderForm.ResolvedCaptcha);

			if (_session.HasError)
				return;

			_orderForm.Send();

			if (_orderForm.HasTimeout)
			{
				Logger.Info("Таймаут Росреестра. Отдыхаю 5 минут");
				Sleep(300_000);
			}

			if (_orderForm.HasSuccess)
			{
				Logger.Info($"Номер {_orderForm.RequestNumber} успешно получен. Отдыхаю 5 минут");
				Repository.SetAsPrepared(_order, DateTime.Now, _orderForm.RequestNumber, _session.LoginKey);
				Sleep(300_000);
			}
		}

		private void Dispose()
		{
			Repository.SetFreeOrder(_session.LoginKey);
			_activeThreadsCount--;
			Logger.Info($"Освободил поток. Текущее количество = {_activeThreadsCount}");
		}

		private void SetAnnulOrder()
		{
			Repository.SetAnul(_order);
			Logger.Warn("АНУЛИРОВАН");
		}

		private void CancelOrdering()
		{
			Logger.Error("Ни одна из 5 попыток не получила адресс");
			Repository.SetIncorrect(_order, _session.LoginKey);
		}

		private void SetNoAddressesFound()
			=> Repository.SetNoAddressesFound(_order, _session.LoginKey);

		private void SetMoreThanOneAddressFound()
			=> Repository.SetMoreThanOneAddressesFound(_order, _session.LoginKey);
	}
}