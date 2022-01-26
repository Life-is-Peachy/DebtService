using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Xsl;

namespace Rosreestr.Xml
{
    public partial class XmlParserFactory
    {
        /// <summary>
        /// Совместная собственность
        /// </summary>
        internal const string COMMON_OWNERSHIP_TYPE = "001003000000";

        /// <summary>
        /// Долевая собственность
        /// </summary>
        internal const string PARTIAL_OWNERSHIP_TYPE = "001002000000";

        /// <summary>
        /// Собственность
        /// </summary>
        internal const string PERSONAL_OWNERSHIP_TYPE = "001001000000";

        /// <summary>
        /// Оперативное управление
        /// </summary>
        internal const string OPERATING_MANAGEMENT = "001005000000";

        private static readonly Dictionary<string, byte[]> _xslDataCache = new Dictionary<string, byte[]>();

        private abstract class XmlReestr : IXmlReestrParser
        {
            protected readonly XmlDocument _document;
            protected readonly XmlNamespaceManager _nsManager;
            protected readonly XmlElement _declaration;
            protected readonly string _href;

            public string XslHref { get { return _href; } }

            public virtual string RequeryNumber
            {
                get
                {
                    return _declaration.Attributes["RequeryNumber"].Value;
                }
            }

            public virtual DateTime RequeryDate
            {
                get
                {
                    return DateTime.ParseExact(
                        _declaration.Attributes["RequeryDate"].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                }
            }

            public abstract ICollection<XmlPerson> Persons { get; protected set; }

            public abstract ICollection<XmlGovernance> Governances { get; protected set; }

            public abstract ICollection<XmlOrganization> Organizations { get; protected set; }

            public abstract XmlBuildingInfo BuildingInfo { get; protected set; }

            public virtual int DebugMark { get; protected set; }

            internal XmlReestr(XmlDocument doc, string href)
            {
                _document = doc;
                _href = href;
                _nsManager = new XmlNamespaceManager(doc.NameTable);
                _nsManager.AddNamespace("x", doc.DocumentElement.NamespaceURI);
                _nsManager.AddNamespace("adrs", doc.DocumentElement.GetNamespaceOfPrefix("adrs"));

                _declaration = (XmlElement)_document.SelectSingleNode("//x:DeclarAttribute", _nsManager);
            }

            public string GetHtmlText()
            {
                string _localHref = null;
                try
                {
                    using (MemoryStream xmlStream = new MemoryStream())
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        _document.Save(xmlStream);

                        byte[] xslData = null;
                        if (_xslDataCache.ContainsKey(_href))
                        {
                            xslData = _xslDataCache[_href];
                        }
                        else
                        {
                            using (WebClient client = new WebClient())
                            {
                                // Загружаем таблицу xsl
                                try
                                {
                                    xslData = client.DownloadData(_href);
                                }
                                catch
                                {
                                    _localHref = _href.Replace("https://portal.rosreestr.ru/",  Directory.GetCurrentDirectory().Replace("\\", "/") +"/LocalXsl/");
                                    xslData = client.DownloadData(_localHref);
                                 }
                                _xslDataCache[_href] = xslData;
                            }
                        }

                        XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                        {
                            ConformanceLevel = ConformanceLevel.Document,
                            DtdProcessing = DtdProcessing.Parse,
                            CloseInput = true,
                            IgnoreWhitespace = true,
                            IgnoreComments = true,
                        };

                        xmlStream.Seek(0, SeekOrigin.Begin);
                        using (MemoryStream xslStream = new MemoryStream(xslData))
                        using (XmlReader xslReader = XmlReader.Create(xslStream, xmlReaderSettings, _localHref ?? _href))
                        using (XmlReader xmlReader = XmlReader.Create(xmlStream, xmlReaderSettings))
                        {
                            XslCompiledTransform xslt = new XslCompiledTransform();
                            xslt.Load(xslReader, new XsltSettings(true, true), new XmlUrlResolver());

                            xslt.Transform(xmlReader, null, outputStream);
                        }

                        outputStream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(outputStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        internal static void ClearCache()
        {
            _xslDataCache.Clear();
        }
    }
}
