namespace Byte.Toolkit.Db
{
    public interface IDbObject
    {
        /// <summary>
        /// Returns the mapping of column name (key) and object property name (value)
        /// </summary>
        Dictionary<string, string> GetMapping();
    }
}
