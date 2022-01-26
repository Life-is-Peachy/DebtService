using System.Threading.Tasks;
using System;
using System.Threading;
using System.Data.SqlClient;
using DAL;
using Shared;
using Pipelines.OrderingPipeline;
using Pipelines.PreparingPipeline;
using Pipelines.LoadingPipeline;
using Pipelines;

namespace WcfDebtService.App_Code
{
    /// <summary>
    /// Класс с которого начнётся исполнение сервиса
    /// Здесь мы асинхронно запускаем все конвееры
    /// </summary>
    public static class Initializer
    {
        private static readonly Action InvokeService;
        private static Pipeline _pipeline;

        static Initializer()
        {
            InvokeService = Repository.SetFreeOnStart;  // Обнуляем все временные столбцы таблиц БД если не успели обнулиться перед перезапуском сервера
            InvokeService += InvokePreparingAsync;
            InvokeService += InvokeOrderingAsync;
            InvokeService += InvokeLoaderAsync;
        }

        /// <summary>
        /// Проверяем, успела ли запуститься база данных
        /// После ежесуточного перезапуска сервера
        /// </summary>
        public static void AppInitialize()
        {
            while (CanGetConnection() == false)
                Thread.Sleep(60 * 1000);                // Подождём минутку..

            InvokeService.Invoke();
        }

        private static async void InvokePreparingAsync()
        {
            _pipeline = new PreparingPipeline();
            await Task.Run(() => _pipeline.Invoke());
        }

        private static async void InvokeOrderingAsync()
        {
            _pipeline = new OrderingPipeline();
            await Task.Run(() => _pipeline.Invoke());
        }

        private static async void InvokeLoaderAsync()
        {
            _pipeline = new LoaderPipeline();
            await Task.Run(() => _pipeline.Invoke());
        }

        private static bool CanGetConnection()
        {
            using (SqlConnection connection = new SqlConnection(SETTINGS.PIPELINE_DB_CONNECTION))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}