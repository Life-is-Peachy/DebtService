using DAL;
using Shared.Classes;
using static System.Threading.Thread;
using Rosreestr.Sessions;
using Pipelines.PreparePipeline;
using NLog;

namespace Pipelines.PreparingPipeline
{
    public class PreparingPipeline : Pipeline<UnpreparedOrder>
    {
        protected override bool OutOfOrders => Repository.CheckUnpreparedQueue();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override void Invoke()
        {
            CurrentThread.Name = "Preparer";

            while (true)
            {
                if (PrepareWorker.OutOfCapacity)
                {
                    Logger.Info("Все потоки заняты. Отдыхаю 20 с.");
                    Sleep(20_000);
                    continue;
                }

                if (OutOfOrders)
                {
                    Logger.Info("В базе данных отсутствуют подходящие записи для подготовки. Отдыхаю 20 с.");
                    Sleep(20_000);
                    continue;
                }

                try
                {
                    _session = new RosreestrSession(Repository.GetUnpreparedSessionCredentials());
                    _order = Repository.GetUnpreparedOrder();

                    Logger.Info($"Взял [{_order.Address}] ID = [{_order.ID}] от [{_order.Source}] [{_session.LoginKey}]");

                    _worker = new PrepareWorker(_session, _order);
                    _worker.RunAsync();
                }
                catch (OrderingFailedException ex)
                {
                    Logger.Error(ex, $"Возникла ошибка на этапе подготовки для [ID = {ex.OrderID}, ID_REQUEST = {_order.ID_Request}]");
                    Sleep(10_000);
                }
            }
        }
    }
}