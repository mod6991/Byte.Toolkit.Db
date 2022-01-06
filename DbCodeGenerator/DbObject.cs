using System;
using System.Collections.Generic;

namespace DbCodeGenerator
{
    internal class DbObject
    {
        public DbObject(string name, string tableName, string query)
        {
            Name = name;
            NameInstance = Name.Substring(0, 1).ToLower() + name.Substring(1);
            TableName = tableName;
            Query = query;
            Properties = new List<DbProperty>();
        }

        public List<DbProperty> Properties { get; set; }
        public string Name { get; set; }
        public string NameInstance { get; set; }
        public string TableName { get; set; }
        public string Query { get; set; }

        public void UpdateProperties(InputXml xml, Dictionary<string, Type> columns)
        {
            foreach (KeyValuePair<string, Type> col in columns)
                Properties.Add(new DbProperty(col.Key, col.Value, xml));
        }
    }
}
