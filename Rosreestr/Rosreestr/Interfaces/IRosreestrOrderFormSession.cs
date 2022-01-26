namespace Rosreestr.Sessions
{
    /// <summary>
    /// Форма заказа выписки + доп. информация по адресу
    /// </summary>
    public interface IRosreestrOrderFormSession
    {
        string ResolvedCaptcha { get; set; }

        string RequestNumber { get; }

        bool HasSuccess { get; }

        bool CaptchaError { get; }

        bool HasTimeout { get; }

        bool IsAnnul { get; }

        IRosreestrOrderFormSession AddCaptcha();

        IRosreestrOrderFormSession SaveCaptcha(string path);

        IRosreestrOrderFormSession Send();

        IRosreestrRealEstateSearchResultsSession Close();

        IRosreestrRealEstateSearchResultsSession Continue();

        IRosreestrOrderFormSession ChangeCaptcha();

        /// <summary>
        /// Ввод капчи
        /// </summary>
        /// <param name="value">Значение капчи</param>
        /// <returns></returns>
        IRosreestrOrderFormSession EnterCaptcha(string value);

        /// <summary>
        /// Запросить сведения
        /// </summary>
        /// <param name="num">
        /// 1 - Запросить сведения об объекте;
        /// 2 - Запросить сведения о переходе прав на объект</param>
        /// <returns></returns>
        IRosreestrOrderFormSession CheckRequestObject(int num);
    }
}
