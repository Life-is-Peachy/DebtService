using DAL;
using NLog;
using Pipelines.PorderingPipeline;
using Rosreestr.Sessions;
using Shared.Classes;
using static System.Threading.Thread;

namespace Pipelines.OrderingPipeline
{
	public class OrderingPipeline : Pipeline<PreparedOrder>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected override bool OutOfOrders => Repository.CheckPreparedQueue();

		public override void Invoke()
		{
			CurrentThread.Name = "Preparer";

			while (true)
			{
				if (OrderWorker.OutOfCapacity)
				{
					Logger.Info("Все потоки заняты. Отдыхаю 20 с.");
					Sleep(20_000);
					continue;
				}

				if (OutOfOrders)
				{
					Logger.Info("В базе данных отсутствуют подходящие записи для заказа. Отдыхаю 20 с.");
					Sleep(20_000);
					continue;
				}

				try
				{
					_session = new RosreestrSession(Repository.GetPreparedSessionCredentials());
					_order = Repository.GetPreparedOrder();
					Logger.Info($"Взял [{_order.CadastralNumber}] ID = [{_order.ID}] от [{_order.Source}] [{_session.LoginKey}]");

					_worker = new OrderWorker(_session, _order);
					_worker.RunAsync();
				}
				catch (OrderingFailedException ex)
				{
					Logger.Error(ex, $"Возникла ошибка на этапе заказа для [ID = {ex.OrderID}");
					Sleep(10_000);
				}
			}
		}
	}
}