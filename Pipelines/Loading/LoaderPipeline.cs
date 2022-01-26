using DAL;
using NLog;
using Rosreestr.Sessions;
using Shared.Classes;
using static System.Threading.Thread;

namespace Pipelines.LoadingPipeline
{
    public class LoaderPipeline : Pipeline<LoadOrder>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected override bool OutOfOrders => Repository.CheckLoadQueue();

		public override void Invoke()
		{
			CurrentThread.Name = "Loader";

			while (true)
			{
				if (LoadWorker.OutOfCapacity)
				{
					Logger.Info("Все потоки заняты. Отдыхаю 20 с.");
					Sleep(20_000);
					continue;
				}

				if (OutOfOrders)
				{
					Logger.Info("В базе данных отсутствуют подходящие выписки для скачивания. Отдыхаю 20 с.");
					Sleep(20_000);
					continue;
				}

				try
				{
					_order = Repository.GetLoadableOrder();
					_session = new RosreestrSession(_order.SessionKey);
					Logger.Info($"Взял [{_order.NumRequest}] ID = [{_order.ID}] от [{_order.Source}] [{_order.SessionKey}]");

					_worker = new LoadWorker(_session, _order);
					_worker.RunAsync();
				}
				catch (OrderingFailedException ex)
				{
					Logger.Error(ex, $"Возникла ошибка на этапе загрузки для [ID = {ex.OrderID}");
					Sleep(10_000);
				}
			}
		}
	}
}