namespace Byte.Toolkit.Db
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DbObjectAttribute : Attribute
    { }

    public class DbColumnAttribute : Attribute
    {
        public DbColumnAttribute(string columnName) => ColumnName = columnName;

        public string ColumnName { get; }
    }
}
