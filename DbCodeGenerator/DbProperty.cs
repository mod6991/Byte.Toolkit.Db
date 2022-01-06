using System;

namespace DbCodeGenerator
{
    internal sealed class DbProperty
    {
        public DbProperty(string columnName, Type type, InputXml xml)
        {
            ColumnName = columnName;

            if (xml.NameType == NameType.SnakeCaseToCamelCase)
                PropertyName = StringsHelper.SnakeCaseToCamelCase(columnName);
            else
                PropertyName = StringsHelper.FirstLetterUpper(columnName);

            FieldName = "_" +
                        PropertyName.Substring(0, 1).ToLower() +
                        PropertyName.Substring(1);

            ParameterName = FieldName.Substring(1);
            ParameterNameWithChar = xml.ParameterChar + ParameterName;
            TypeName = type.Name + (xml.NullableTypes ? "?" : "");
            TypeNameWithoutNullable = type.Name;
        }

        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
        public string FieldName { get; set; }
        public string TypeName { get; set; }
        public string TypeNameWithoutNullable { get; set; }
        public string ParameterName { get; set; }
        public string ParameterNameWithChar { get; set; }
    }
}
