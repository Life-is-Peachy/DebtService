using Shared.Utills;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Rosreestr.Xml
{
    public partial class XmlParserFactory
    {
        private class ReestrExtractList07 : XmlReestr
        {
            public override ICollection<XmlPerson> Persons { get; protected set; }

            public override ICollection<XmlGovernance> Governances { get; protected set; }

            public override ICollection<XmlOrganization> Organizations { get; protected set; }

            public override XmlBuildingInfo BuildingInfo { get; protected set; }


            internal ReestrExtractList07(XmlDocument doc, string href)
                : base(doc, href)
            {
                Persons = new List<XmlPerson>();
                Governances = new List<XmlGovernance>();
                Organizations = new List<XmlOrganization>();

                foreach (XmlElement elem in _document.DocumentElement.SelectNodes(
                    "//x:ReestrExtract/x:ExtractObjectRight/x:ExtractObject/x:Owner", _nsManager))
                {
                    string ownerNumber = elem.Attributes["OwnerNumber"].Value;
                    XmlNodeList xPersons = elem.SelectNodes("x:Person", _nsManager);
                    XmlNodeList xGovernances = elem.SelectNodes("x:Governance", _nsManager);
                    XmlNodeList xOrganizations = elem.SelectNodes("x:Organization", _nsManager);

                    XmlNode xRegistr = _document.DocumentElement.SelectSingleNode(
                        "//x:ReestrExtract/x:ExtractObjectRight/x:ExtractObject/" +
                        "x:Registration[@RegistrNumber = '" + ownerNumber + "']", _nsManager);

                    XmlNode regType = null;
                    XmlNode regShare = null;
                    XmlNode regDate = null;
                    XmlNode regName = null;

                    if (xRegistr != null)
                    {
                        regDate = xRegistr.SelectSingleNode("x:RegDate", _nsManager);
                        regShare = xRegistr.SelectSingleNode("x:Share", _nsManager);
                        regType = xRegistr.SelectSingleNode("x:Type", _nsManager);
                        regName = xRegistr.SelectSingleNode("x:Name", _nsManager);
                    }

                    foreach (XmlNode node in xPersons)
                    {
                        XmlNode content = node.SelectSingleNode("x:Content", _nsManager);
                        XmlPerson tmp = new XmlPerson
                        {
                            FullName = content.InnerText,
                            RegDate = DateTime.ParseExact(regDate.InnerText, "dd.MM.yyyy", CultureInfo.InvariantCulture),
                            RegName = regName != null ? regName.InnerText : null,
                            RegType = regType != null ? regType.InnerText : null,
                        };

                        // Доли собственности
                        int numerator = 1;
                        int denominator = 1;

                        if (regType.Value == XmlParserFactory.COMMON_OWNERSHIP_TYPE) // Совместная собственность
                            denominator = xPersons.Count;

                        if (regShare != null)
                        {
                            numerator = Convert.ToInt32(regShare.Attributes["Numerator"].Value);
                            denominator = Convert.ToInt32(regShare.Attributes["Denominator"].Value);
                        }

                        tmp.Fraction = new Fraction(numerator, denominator);

                        Persons.Add(tmp);
                    }

                    foreach (XmlNode node in xGovernances)
                    {
                        XmlNode name = node.SelectSingleNode("x:Name", _nsManager);

                        XmlGovernance tmp = new XmlGovernance
                        {
                            Name = name != null ? name.InnerText : null,
                            RegType = regType != null ? regType.InnerText : null,
                        };

                        Governances.Add(tmp);
                    }

                    foreach (XmlNode node in xOrganizations)
                    {
                        XmlNode name = node.SelectSingleNode("x:Name", _nsManager);

                        XmlOrganization tmp = new XmlOrganization
                        {
                            Name = name != null ? name.InnerText : null,
                            RegType = regType != null ? regType.InnerText : null,
                        };

                        Organizations.Add(tmp);
                    }
                }

                SetBuildingInfo();
            }

            internal virtual void SetBuildingInfo()
            {
                XmlElement buildingXml = (XmlElement)_document.DocumentElement.SelectSingleNode("descendant::x:ExtractObject/x:ObjectDesc", _nsManager);
                if (buildingXml != null)
                {
                    BuildingInfo = new XmlBuildingInfo();

                    XmlNode cadastral = buildingXml.SelectSingleNode("x:CadastralNumber", _nsManager);
                    BuildingInfo.CadastralNum = cadastral.InnerText;

                    XmlNode objType = buildingXml.SelectSingleNode("x:ObjectType", _nsManager);
                    BuildingInfo.Type = objType.InnerText;

                    XmlNode area = buildingXml.SelectSingleNode("x:Area", _nsManager);
                    if (area != null)
                    {
                        area = area.SelectSingleNode("x:Area", _nsManager);
                        BuildingInfo.Area = area.InnerText;
                    }

                    XmlNode addr = buildingXml.SelectSingleNode("x:Address", _nsManager);
                    if (addr != null)
                    {
                        XmlNode city = addr.SelectSingleNode("x:City", _nsManager);
                        if (city != null)
                        {
                            BuildingInfo.City = city.Attributes["Name"].Value;
                            BuildingInfo.CityType = city.Attributes["Type"].Value;
                        }

                        XmlNode street = addr.SelectSingleNode("x:Street", _nsManager);
                        if (street != null)
                        {
                            BuildingInfo.Street = street.Attributes["Name"].Value;
                            BuildingInfo.StreetType = street.Attributes["Type"].Value;
                        }

                        XmlNode home = addr.SelectSingleNode("x:Level1", _nsManager);
                        if (home != null)
                        {
                            BuildingInfo.Home = home.Attributes["Name"].Value;
                            BuildingInfo.HomeType = home.Attributes["Type"].Value;
                        }

                        XmlNode flat = addr.SelectSingleNode("x:Apartment", _nsManager);
                        if (flat != null)
                        {
                            BuildingInfo.Flat = flat.Attributes["Name"].Value;
                            BuildingInfo.FlatType = flat.Attributes["Type"].Value;
                        }
                    }
                }
            }
        }
    }
}
