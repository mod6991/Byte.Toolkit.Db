namespace Byte.Toolkit.Db
{
    /// <summary>
    /// DbObject layer abstract class
    /// </summary>
    /// <typeparam name="T">DbObject type</typeparam>
    public abstract class DbObjectLayer<T>
    {
        public DbObjectLayer(DbManager db)
        {
            Db = db;
        }

        /// <summary>
        /// DbManager reference
        /// </summary>
        public DbManager Db { get; }

        /// <summary>
        /// DbObject queries reference
        /// </summary>
        public Dictionary<string, string> Queries
        {
            get => Db.Queries[typeof(T)];
        }
    }
}
