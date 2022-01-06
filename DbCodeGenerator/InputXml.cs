using System;
using System.Collections.Generic;
using System.Xml;

namespace DbCodeGenerator
{
    public class InvalidInputXml : Exception
    {
        public InvalidInputXml(string message) : base(message) { }
        public InvalidInputXml(string xpath, string file) : base($"xpath '{xpath}' is missing in file '{file}'") { }
    }

    internal sealed class InputXml
    {
        private XmlDocument _doc;
        private string _file;

        public InputXml(string file)
        {
            _file = file;
            _doc = new XmlDocument();
            _doc.Load(file);

            Objects = new List<DbObject>();
            ConnectionString = GetTagValue("/DbCodeGen/DbConnection/ConnectionString");
            ProviderName = GetTagValue("/DbCodeGen/DbConnection/ProviderName");
            FactoryType = GetTagValue("/DbCodeGen/DbConnection/FactoryType");

            Output = GetTagValue("/DbCodeGen/Settings/Output");
            ObjectsNamespace = GetTagValue("/DbCodeGen/Settings/ObjectsNamespace");
            LayersNamespace = GetTagValue("/DbCodeGen/Settings/LayersNamespace");
            ParameterChar = GetTagValue("/DbCodeGen/Settings/ParameterChar");
            NameType = EnumHelper.GetEnumValue<NameType>(GetTagValue("/DbCodeGen/Settings/NameType"));
            PropertyType = EnumHelper.GetEnumValue<PropertyType>(GetTagValue("/DbCodeGen/Settings/PropertyType"));
            PropertySetTemplate = GetTagValue("/DbCodeGen/Settings/PropertySetTemplate");
            NullableTypes = bool.Parse(GetTagValue("/DbCodeGen/Settings/NullableTypes"));

            XmlNodeList objectsNodes = _doc.SelectNodes("/DbCodeGen/Objects/Object");

            if (objectsNodes == null || objectsNodes.Count == 0)
                throw new InvalidInputXml($"No object found for xpath '/DbCodeGen/Objects/Object' in file '{_file}'");

            foreach (XmlNode? objNode in objectsNodes)
            {
                if (objNode == null)
                    throw new InvalidInputXml("Object node is null");

                string name = GetAttributeValue(objNode, "Name");
                string tableName = GetAttributeValue(objNode, "TableName");
                
                XmlNode? queryNode = objNode.SelectSingleNode("ColumnsSelectionQuery");

                if (queryNode == null)
                    throw new InvalidInputXml($"Object sub node 'ColumnsSelectionQuery' is missing in file '{_file}'");

                string query = queryNode.InnerText;

                Objects.Add(new DbObject(name, tableName, query));
            }
        }

        public List<DbObject> Objects { get; set; }
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
        public string FactoryType { get; set; }

        public string Output { get; set; }
        public string ObjectsNamespace { get; set; }
        public string LayersNamespace { get; set; }
        public string ParameterChar { get; set; }
        public NameType NameType { get; set; }
        public PropertyType PropertyType { get; set; }
        public string PropertySetTemplate { get; set; }
        public bool NullableTypes { get; set; }

        private string GetTagValue(string xpath)
        {
            XmlNode node = _doc.SelectSingleNode(xpath);
            return node?.InnerText ?? throw new InvalidInputXml(xpath, _file);
        }

        private string GetAttributeValue(XmlNode node, string attributeName)
        {
            XmlAttribute attr = node.Attributes[attributeName];

            if (attr == null)
                throw new InvalidInputXml($"Object node is missing attribute '{attributeName}' in file '{_file}'");

            return attr.Value;
        }
    }
}
