using System.Threading.Tasks;

namespace Pipelines
{
    public interface IWorker
    {
        /// <summary>
        /// Определяет как будет себя вести конкретный обработчик
        /// </summary>
        Task RunAsync();
    }
}