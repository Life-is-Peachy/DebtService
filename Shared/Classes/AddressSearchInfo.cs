using System;

namespace Shared.Classes
{
    public sealed class AddressSearchInfo
    {
        /// <summary>
        /// Идентификатор для запроса
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Кадастровый номер
        /// </summary>
        public string CadastralNumber { get; set; }

        /// <summary>
        /// Полный адрес
        /// </summary>
        public string FullAddress { get; set; }

        /// <summary>
        /// Тип объекта
        /// </summary>
        public string ObjType { get; set; }

        /// <summary>
        /// Площадь
        /// </summary>
        public string Square { get; set; }

        /// <summary>
        /// Категория ЗУ
        /// </summary>
        public string SteadCategory { get; set; }

        /// <summary>
        /// Вид разрешённого использования ЗУ
        /// </summary>
        public string SteadKind { get; set; }

        /// <summary>
        /// Назначение здания или помещения
        /// </summary>
        public string FuncName { get; set; }

        /// <summary>
        /// Байты картинки с капчей
        /// </summary>
        public byte[] CaptchaBytes { get; set; }

        // **
        // ** Эти поля заполняются при открытии формы заказа выписки
        // **

        /// <summary>
        /// Кадастровая стоимость
        /// </summary>
        public decimal? CadastralCost { get; set; }

        /// <summary>
        /// Статус объекта
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Дата обновления информации
        /// </summary>
        public DateTime? UpdateInfoDate { get; set; }

        /// <summary>
        /// Этажность
        /// </summary>
        public string NumStoreys { get; set; }

        /// <summary>
        /// Литер БТИ
        /// </summary>
        public string LiterBTI { get; set; }

        /// <summary>
        /// Аннулирован
        /// </summary>
        public bool ChkAnnul { get; set; }

        /// <summary>
        /// Дата внесения кадастровой стоимости
        /// </summary>
        public DateTime? CadastralCostDate { get; set; }
    }
}
