using System.Collections.Generic;
using System.Xml;

namespace Rosreestr.Xml
{
    public partial class XmlParserFactory
    {
        private class ReestrExtractBigRoom : ReestrExtractBig
        {
            public override ICollection<XmlPerson> Persons { get; protected set; }

            public override ICollection<XmlGovernance> Governances { get; protected set; }

            public override ICollection<XmlOrganization> Organizations { get; protected set; }


            internal ReestrExtractBigRoom(XmlDocument doc, string href)
                : base(doc, href)
            {
                
            }

            internal override void SetBuildingInfo()
            {
                XmlElement buildingXml = (XmlElement)_document.DocumentElement.SelectSingleNode("//x:Realty/x:Flat", _nsManager);
                if (buildingXml != null)
                {
                    BuildingInfo = new XmlBuildingInfo();
                    BuildingInfo.CadastralNum = buildingXml.Attributes["CadastralNumber"].Value;

                    XmlNode objType = buildingXml.SelectSingleNode("x:ObjectType", _nsManager);
                    BuildingInfo.Type = objType.InnerText;

                    XmlNode area = buildingXml.SelectSingleNode("x:Area", _nsManager);
                    BuildingInfo.Area = area.InnerText;

                    XmlNode addr = buildingXml.SelectSingleNode("x:Address", _nsManager);
                    if (addr != null)
                    {
                        XmlNode city = addr.SelectSingleNode("adrs:City", _nsManager);
                        if (city != null)
                        {
                            BuildingInfo.City = city.Attributes["Name"].Value;
                            BuildingInfo.CityType = city.Attributes["Type"].Value;
                        }

                        XmlNode street = addr.SelectSingleNode("adrs:Street", _nsManager);
                        if (street != null)
                        {
                            BuildingInfo.Street = street.Attributes["Name"].Value;
                            BuildingInfo.StreetType = street.Attributes["Type"].Value;
                        }

                        XmlNode home = addr.SelectSingleNode("adrs:Level1", _nsManager);
                        if (home != null)
                        {
                            BuildingInfo.Home = home.Attributes["Value"].Value;
                            BuildingInfo.HomeType = home.Attributes["Type"].Value;
                        }

                        XmlNode flat = addr.SelectSingleNode("adrs:Apartment", _nsManager);
                        if (flat != null)
                        {
                            BuildingInfo.Flat = flat.Attributes["Value"].Value;
                            BuildingInfo.FlatType = flat.Attributes["Type"].Value;
                        }
                    }
                }
            }
        }
    }
}
