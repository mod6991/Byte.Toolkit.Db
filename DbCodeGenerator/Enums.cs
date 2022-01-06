using System;

namespace DbCodeGenerator
{
    public enum NameType
    {
        SnakeCaseToCamelCase,
        FirstLetterUpper
    }

    public enum PropertyType
    {
        GetSet,
        Notify
    }

    internal static class EnumHelper
    {
        public static T GetEnumValue<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }
    }
}
