using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Publishing.CSDL
{
    internal class CsdlWriterConfigFile: Validation.Config.ConfigFile
    {
        [JsonProperty("csdlWriter")]
        public CsdlWriterSettings CsdlWriterSettings { get; set; }


        public override bool IsValid
        {
            get { return null != this.CsdlWriterSettings; }
        }
    }


    internal class CsdlWriterSettings
    {

        public CsdlWriterSettings()
        {
            this.ExcludedNamespaces = new string[0];
            this.IncludeXmlDeclaration = true;
            this.OutputFilename = "metadata.csdl";
            this.IndentXml = true;
        }

        [JsonProperty("excludedNamespaces")]
        public string[] ExcludedNamespaces { get; set; }


        [JsonProperty("includeXmlDeclaration")]
        public bool IncludeXmlDeclaration { get; set; }

        [JsonProperty("indentXml")]

        public bool IndentXml { get; internal set; }
        [JsonProperty("outputFilename")]
        public string OutputFilename { get; set; }

    }
}
