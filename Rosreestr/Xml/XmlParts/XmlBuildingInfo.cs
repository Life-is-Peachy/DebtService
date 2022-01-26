using System;

namespace Rosreestr.Xml
{
    public class XmlBuildingInfo
    {
        /// <summary>
        /// Сооружение
        /// </summary>
        public const string TYPE_1 = "002001004000";

        /// <summary>
        /// Линейное сооружение
        /// </summary>
        public const string TYPE_2 = "002001004001";

        /// <summary>
        /// Условная часть линейного сооружения
        /// </summary>
        public const string TYPE_3 = "002001004002";

        /// <summary>
        /// Объект незавершенного строительства
        /// </summary>
        public const string TYPE_4 = "002001005000";

        /// <summary>
        /// Помещение
        /// </summary>
        public const string TYPE_5_1 = "002001003000";

        /// <summary>
        /// Помещение
        /// </summary>
        public const string TYPE_5_2 = "002002002000";

        /// <summary>
        /// Здание
        /// </summary>
        public const string TYPE_6 = "002001002000";

        /// <summary>
        /// Земельный участок
        /// </summary>
        public const string TYPE_7 = "ZU_01";



        public string CadastralNum { get; set; }

        public string Type { get; set; }

        public string Area { get; set; }

        public string Name
        {
            get
            {
                switch (Type)
                {
                    case TYPE_1: return "Сооружение";
                    case TYPE_2: return "Линейное сооружение";
                    case TYPE_3: return "Условная часть линейного сооружения";
                    case TYPE_4: return "Объект незавершенного строительства";
                    case TYPE_5_1:
                    case TYPE_5_2: return "Помещение";
                    case TYPE_6: return "Здание";
                    case TYPE_7: return "Земельный участок";
                }

                throw new InvalidOperationException("Неизвестный тип объекта " + Type + " (XmlBuildingInfo)");
            }
        }

        public string City { get; set; }

        public string CityType { get; set; }

        public string Street { get; set; }

        public string StreetType { get; set; }

        public string Home { get; set; }

        public string HomeType { get; set; }

        public string Flat { get; set; }

        public string FlatType { get; set; }

        public string Address
        {
			get
            {
                string fullAddr = string.Empty;
                if (!string.IsNullOrWhiteSpace(City))
                    fullAddr += CityType + ". " + City;

                if (!string.IsNullOrWhiteSpace(Street))
                    fullAddr += ", " + StreetType + ". " + Street;

                if (!string.IsNullOrWhiteSpace(Home))
                    fullAddr += ", " + HomeType + ". " + Home;

                if (!string.IsNullOrWhiteSpace(Flat))
                    fullAddr += ", " + FlatType + ". " + Flat;

                return fullAddr;
            }
        }
    }
}
