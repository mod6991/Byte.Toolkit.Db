using System.Collections.Generic;

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
            DbManager = db;
        }

        /// <summary>
        /// DbManager reference
        /// </summary>
        public DbManager DbManager { get; }

        /// <summary>
        /// DbObject queries reference
        /// </summary>
        public Dictionary<string, string> Queries
        {
            get => DbManager.Queries[typeof(T)];
        }
    }
}
