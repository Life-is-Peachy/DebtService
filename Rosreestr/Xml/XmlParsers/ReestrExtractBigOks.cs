using System.Collections.Generic;
using System.Xml;

namespace Rosreestr.Xml
{
    public partial class XmlParserFactory
    {
        private class ReestrExtractBigOks : ReestrExtractBig
        {
            public override ICollection<XmlPerson> Persons { get; protected set; }

            public override ICollection<XmlGovernance> Governances { get; protected set; }

            public override ICollection<XmlOrganization> Organizations { get; protected set; }


            internal ReestrExtractBigOks(XmlDocument doc, string href)
                : base(doc, href)
            {

            }
        }
    }
}
