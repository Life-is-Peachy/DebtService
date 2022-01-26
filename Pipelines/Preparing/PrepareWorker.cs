using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Threading.Thread;
using DAL;
using Rosreestr.Sessions;
using Shared;
using Shared.Classes;
using NLog;

namespace Pipelines.PreparePipeline
{
	public class PrepareWorker : IWorker
	{
		private readonly RosreestrSession _session;
		private readonly UnpreparedOrder _order;
		private IRosreestrRealEstateSearchSession _searchForm;
		private IRosreestrRealEstateSearchResultsSession _resultForm;

		/// <summary>
		/// Т.к Росреестр реагирует на регистры, точки и сокращения улиц
		/// Только по понятной ему логике - иногда придётся отправить ему
		/// Разные вариации написания одной и той же улицы
		/// </summary>
		private readonly Queue<string> _correctStreets;

		/// <summary>
		/// В связи с нестабильной работой сервисов Росреестра
		/// Будем досрочно прерывать работу обработчика после
		/// Некоторого количества неудавшихся попыток
		/// </summary>
		private const int MAX_ATTEMPT_COUNT = 5;
		private int _rosrFailedResponseCount = 0;

		/// <summary>
		/// Общее количество асинхронных задач конкретного обработчика
		/// Не должно быть больше заданного в настройках
		/// </summary>
		private static uint _activeThreadsCount;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static bool OutOfCapacity => _activeThreadsCount == SETTINGS.PREPARE_PIPELINE_THREAD_COUNT;

		public PrepareWorker(RosreestrSession session, UnpreparedOrder order)
		{
			_session = session;
			_order = order;
			_correctStreets = new Queue<string>(Repository.TryGetCorrectStreet(order.Street));

			_activeThreadsCount++;
			Logger.Info($"Начинаю заказ. Текущее число занятых потоков = {_activeThreadsCount}");
		}

		public async Task RunAsync()
		{
			await Task.Run(() => Start());
		}

		private void Start()
		{
			CurrentThread.Name = "PrepareWorker";

			try
			{
				while (!_session.Success)
				{
					_session.Deactivate();
					_session.Init();

					if ((_searchForm = _session.InitSession?.OpenRealEstateSearchForm()) == null)
						continue;

					Logger.Info("Связь с Росреестром установлена");

					TryFindAddress();

					if (_resultForm == null)
					{
						if (_rosrFailedResponseCount >= MAX_ATTEMPT_COUNT)
						{
							_rosrFailedResponseCount++;
							Logger.Warn($"NULL от Росреестра. Попытка {_rosrFailedResponseCount} из 5");
							continue;
						}

						CancelOrdering();
						break;
					}

					if (_resultForm.NotFound && _correctStreets.Count != 0)
					{
						_order.Street = _correctStreets.Dequeue();
						continue;
					}

					if (_resultForm.NotFound)
					{
						SetNotFoundData();
						break;
					}

					for (int i = 0; i < _resultForm.Addresses.Count; ++i)
					{
						var form = _resultForm.OpenOrderForm(i, false);
						if (form == null)
							return;

						_resultForm.Addresses[i].ChkAnnul = form.IsAnnul;

						if (form.Close() == null)
							return;
					}

					SetSuccess();
					break;
				}
			}
			catch
			{
				throw new OrderingFailedException(_order.ID, _order.ID_Request);
			}
			finally
			{
				Dispose();
			}
		}

		private void TryFindAddress()
		{
			if (_order.Town?.ToLower() == "верхнетемерницкий")
			{
				_resultForm = _searchForm.SearchAddress("Ростовская область",
											_order.District,
											null,
											_order.Street,
											_order.Home?.ToString(),
											_order.Corp?.ToString(),
											_order.Flat?.ToString());
			}
			else
			{
				_resultForm = _searchForm.SearchAddress("Ростовская область",
											_order.District,
											string.IsNullOrEmpty(_order.City) ? _order.Town : _order.City,
											_order.Street,
											_order.Home?.ToString(),
											_order.Corp?.ToString(),
											_order.Flat?.ToString());
			}
		}

		private void SetSuccess()
		{
			Logger.Info($"Найденно {_resultForm.Addresses.Count} адресов");
			Repository.AddPreparedData(_order, _resultForm.Addresses);
		}

		private void CancelOrdering()
		{
			Logger.Error("Ни одна из 5 попыток не получила адресс. Попробую позже");
			Repository.SetAddressNotFound(_order);
		}

		private void SetNotFoundData()
		{
			Logger.Info("Адреса не найдены");
			Repository.SetNotFoundData(_order);
		}

		private void Dispose()
		{
			_activeThreadsCount--;
			Logger.Info($"Освободил поток. Текущее количество = {_activeThreadsCount}");
		}
	}
}