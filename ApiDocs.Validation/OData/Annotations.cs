using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDocs.Validation.OData
{
    [XmlRoot("Annotations", Namespace = ODataParser.EdmNamespace)]
    public class Annotations
    {
        [XmlElement("Target", Namespace = ODataParser.EdmNamespace), DefaultValue(null)]
        public string Target { get; set; }
    }
}
