namespace DbCodeGenerator
{
    internal static class StringsHelper
    {
        public static string SnakeCaseToCamelCase(string columnName)
        {
            string result = string.Empty;
            string[] items = columnName.Split('_');
            
            foreach(string item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    result += item.Substring(0, 1).ToUpper();
                    result += item.Substring(1).ToLower();
                }
            }

            return result;
        }

        public static string FirstLetterUpper(string columnName)
        {
            return columnName[0].ToString().ToUpper()
                   + columnName.Substring(1);
        }
    }
}
