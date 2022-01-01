namespace Byte.Toolkit.Db
{
    public abstract class DbObjectLayer<T>
    {
        public DbObjectLayer(DbManager db)
        {
            Db = db;
        }

        public DbManager Db { get; }

        public Dictionary<string, string> Queries
        {
            get => Db.Queries[typeof(T)];
        }
    }
}
