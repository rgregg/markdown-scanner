﻿namespace ApiDocs.Validation.OData
{
    using System;
    using System.Xml.Linq;

    public class EntitySet
    {
        public string Name { get; set; }
        public string EntityType { get; set; }

        internal static EntitySet FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not an EntitySet element");
            var obj = new EntitySet
            {
                Name = xml.AttributeValue("Name"),
                EntityType = xml.AttributeValue("EntityType")
            };
            return obj;
        }

        public static string ElementName { get { return "EntitySet"; } }
    }
}