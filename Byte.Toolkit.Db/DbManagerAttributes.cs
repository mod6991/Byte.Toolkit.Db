namespace Byte.Toolkit.Db
{
    /// <summary>
    /// DbObject class attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DbObjectAttribute : Attribute
    { }

    /// <summary>
    /// DbObject property attribute defining the corresponding column name
    /// </summary>
    public class DbColumnAttribute : Attribute
    {
        public DbColumnAttribute(string columnName) => ColumnName = columnName;

        public string ColumnName { get; }
    }
}
