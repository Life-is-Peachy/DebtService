using System;
using System.Collections.Generic;

namespace Rosreestr.Xml
{
    public interface IXmlReestrParser
    {
        string XslHref { get; }

        string RequeryNumber { get; }

        DateTime RequeryDate { get; }

        ICollection<XmlPerson> Persons { get; }

        ICollection<XmlGovernance> Governances { get; }

        ICollection<XmlOrganization> Organizations { get; }

        XmlBuildingInfo BuildingInfo { get; }

        string GetHtmlText();

        int DebugMark { get; }
    }
}
