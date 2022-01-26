
using Rosreestr.Sessions;
using Shared.Classes;

namespace Pipelines
{
    public abstract class Pipeline
	{
        /// <summary>
        /// Запускает конкретный обработчик работы с Росреестром
        /// </summary>
        protected IWorker _worker;

        /// <summary>
        /// Сессия работы с Росреестром
        /// Посылает все запросы на получения данных
        /// </summary>
        protected RosreestrSession _session;

        /// <summary>
        /// Индикатор того, закончились ли в базе данных
        /// ордеры конкретного типа для обработки.
        /// Если закончились - можем уйти в небольшой таймаут
        /// </summary>
        protected abstract bool OutOfOrders { get; }

        /// <summary>
        /// Определяет как именно будет вести себя конвеер
        /// </summary>
        public abstract void Invoke();
    }

    /// <summary>
    /// Основа для конвееров
    /// </summary>
    /// <typeparam name="T">Закрываем нужным ордером</typeparam>
    public abstract class Pipeline<T> : Pipeline where T : OrderBase
    {
        /// <summary>
        /// Конкретный тип ордера
        /// </summary>
        protected T _order;
    }
}