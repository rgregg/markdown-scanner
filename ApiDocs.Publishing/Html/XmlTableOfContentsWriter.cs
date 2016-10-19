using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ApiDocs.Publishing.Html
{
    public static class XmlTableOfContentsWriter
    {
        /// <summary>
        /// Generate an XML representation of the Table of Contents
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        internal static string GenerateXml(List<TocItem> tree)
        {
            XmlDocument doc = new XmlDocument();

            XmlElement menu = doc.CreateElement("menu");
            doc.AppendChild(menu);

            foreach (var item in tree)
            {
                CreateTocElement(item, menu);
            }

            StringWriter stringWriter = new StringWriter();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }

        private static void CreateTocElement(TocItem item, XmlElement parent)
        {
            XmlElement element = parent.OwnerDocument.CreateElement("item");

            element.SetAttribute("text", item.Title);
            if (!string.IsNullOrEmpty(item.Url))
                element.SetAttribute("url", item.Url);
            element.SetAttribute("SEOKeyword", item.Keywords ?? "");
            element.SetAttribute("SEODescription", item.PageDescription ?? "");

            if (item.NextLevel.Any())
            {
                foreach(var child in item.NextLevel)
                {
                    CreateTocElement(child, element);
                }
            }
            parent.AppendChild(element);
        }
    }
}
