using FastMember;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Xml;

namespace Byte.Toolkit.Db
{
    public class DbManager : IDisposable
    {
        private bool _disposed;

        #region Constructors

        public DbManager(string connectionString, string provider)
        {
            Factory = DbProviderFactories.GetFactory(provider);
            Connection = Factory.CreateConnection();
            Connection.ConnectionString = connectionString;

            TypeAccessors = new Dictionary<Type, TypeAccessor>();
            TypeColumnPropMappings = new Dictionary<Type, Dictionary<string, string>>();
            Queries = new Dictionary<Type, Dictionary<string, string>>();
        }

        ~DbManager()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        public bool Disposed { get => _disposed; }
        public DbProviderFactory Factory { get; private set; }
        public DbConnection Connection { get; private set; }
        public DbTransaction? Transaction { get; private set; }
        private Dictionary<Type, TypeAccessor> TypeAccessors { get; set; }
        private Dictionary<Type, Dictionary<string, string>> TypeColumnPropMappings { get; set; }
        public Dictionary<Type, Dictionary<string, string>> Queries { get; private set; }

        #endregion

        /// <summary>
        /// Open the connection to the database
        /// </summary>
        public void Open()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            Connection.Open();
        }

        /// <summary>
        /// Close the connection to the database
        /// </summary>
        public void Close()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            Connection.Close();
        }

        /// <summary>
        /// Start a database transaction
        /// </summary>
        public void BeginTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            Transaction = Connection.BeginTransaction();
        }

        /// <summary>
        /// End a database transaction.
        /// </summary>
        /// <param name="commit">Commits the transaction</param>
        public void EndTransaction(bool commit)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (commit)
                Transaction?.Commit();
            else
                Transaction?.Rollback();

            Transaction?.Dispose();
        }

        /// <summary>
        /// Return a new parameter
        /// </summary>
        public DbParameter CreateParameter()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            return Factory.CreateParameter();
        }

        /// <summary>
        /// Return a new parameter
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <param name="paramDirection">Parameter direction</param>
        public DbParameter CreateParameter(string name, object value, ParameterDirection paramDirection = ParameterDirection.Input)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            DbParameter param = Factory.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            param.Direction = paramDirection;
            return param;
        }

        /// <summary>
        /// Register a DbObject
        /// </summary>
        /// <param name="t">Object type</param>
        public void RegisterDbObject(Type t)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (Attribute.GetCustomAttribute(t, typeof(DbObjectAttribute)) == null)
                throw new InvalidDbObjectException(t);

            if (TypeAccessors.ContainsKey(t) || TypeColumnPropMappings.ContainsKey(t))
                throw new DbObjectAlreadyRegisteredException(t);

            TypeAccessors.Add(t, TypeAccessor.Create(t));
            TypeColumnPropMappings.Add(t, new Dictionary<string, string>());
            
            foreach(PropertyInfo prop in t.GetProperties())
            {
                DbColumnAttribute? attr = prop.GetCustomAttribute(typeof(DbColumnAttribute)) as DbColumnAttribute;

                if (attr != null)
                    TypeColumnPropMappings[t].Add(attr.ColumnName, prop.Name);
            }
        }

        public void FillDataTable(DataTable table, string commandText, CommandType commandType = CommandType.Text, List<DbParameter>? parameters = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = Connection.CreateCommand())
            {
                command.CommandType = commandType;
                command.CommandText = commandText;

                if (Transaction != null)
                    command.Transaction = Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataAdapter adapter = Factory.CreateDataAdapter())
                {
                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }
            }
        }

        public T? FillObject<T>(string commandText, CommandType commandType = CommandType.Text, List<DbParameter>? parameters = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            Type t = typeof(T);

            if (Attribute.GetCustomAttribute(t, typeof(DbObjectAttribute)) == null)
                throw new InvalidDbObjectException(t);

            if (!TypeColumnPropMappings.ContainsKey(t) || !TypeAccessors.ContainsKey(t))
                throw new ObjectNotRegisteredException(t);

            T obj = (T)Activator.CreateInstance(t);
            TypeAccessor ta = TypeAccessors[t];

            using (DbCommand command = Connection.CreateCommand())
            {
                command.CommandType = commandType;
                command.CommandText = commandText;

                if (Transaction != null)
                    command.Transaction = Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string fieldName = reader.GetName(i);

                            if (TypeColumnPropMappings[t].ContainsKey(fieldName))
                            {
                                object readerValue = reader[fieldName];
                                if (!(readerValue is DBNull))
                                    ta[obj, TypeColumnPropMappings[t][fieldName]] = readerValue;
                            }
                        }
                    }
                    else
                    {
                        return default(T);
                    }
                    reader.Close();
                }
            }

            return obj;
        }

        public List<T> FillObjects<T>(string commandText, CommandType commandType = CommandType.Text, List<DbParameter>? parameters = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            Type t = typeof(T);

            if (Attribute.GetCustomAttribute(t, typeof(DbObjectAttribute)) == null)
                throw new InvalidDbObjectException(t);

            if (!TypeColumnPropMappings.ContainsKey(t) || !TypeAccessors.ContainsKey(t))
                throw new ObjectNotRegisteredException(t);

            List<T> list = new List<T>();
            TypeAccessor ta = TypeAccessors[t];

            using (DbCommand command = Connection.CreateCommand())
            {
                command.CommandType = commandType;
                command.CommandText = commandText;

                if (Transaction != null)
                    command.Transaction = Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T obj = (T)Activator.CreateInstance(t);

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string fieldName = reader.GetName(i);

                            if (TypeColumnPropMappings[t].ContainsKey(fieldName))
                            {
                                object readerValue = reader[fieldName];
                                if (!(readerValue is DBNull))
                                    ta[obj, TypeColumnPropMappings[t][fieldName]] = readerValue;
                            }
                        }

                        list.Add(obj);
                    }

                    reader.Close();
                }
            }

            return list;
        }

        public int ExecuteNonQuery(string commandText, CommandType commandType = CommandType.Text, List<DbParameter>? parameters = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = Connection.CreateCommand())
            {
                command.CommandType = commandType;
                command.CommandText = commandText;

                if (Transaction != null)
                    command.Transaction = Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteNonQuery();
            }
        }

        public object ExecuteScalarWithRequest(string commandText, CommandType commandType = CommandType.Text, List<DbParameter>? parameters = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = Connection.CreateCommand())
            {
                command.CommandType = commandType;
                command.CommandText = commandText;

                if (Transaction != null)
                    command.Transaction = Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteScalar();
            }
        }

        public Dictionary<string, Type> GetQueryColumnsTypes(string commandText, CommandType commandType = CommandType.Text, List<DbParameter>? parameters = null)
        {
            Dictionary<string, Type> dic = new Dictionary<string, Type>();

            DataTable dt = new DataTable();
            FillDataTable(dt, commandText, commandType, parameters);

            foreach (DataColumn col in dt.Columns)
                dic.Add(col.ColumnName, col.DataType);

            return dic;
        }

        public void AddQueries(Type t, Dictionary<string, string> queries)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (Attribute.GetCustomAttribute(t, typeof(DbObjectAttribute)) == null)
                throw new InvalidDbObjectException(t);

            if (!Queries.ContainsKey(t))
                Queries.Add(t, queries);
            else
                Queries[t] = queries;
        }
        
        public void AddQueriesFile(Type t, string file)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (Attribute.GetCustomAttribute(t, typeof(DbObjectAttribute)) == null)
                throw new InvalidDbObjectException(t);

            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            Queries.Add(t, new Dictionary<string, string>());

            XmlNodeList requestList = doc.SelectNodes("/Queries/Query");

            foreach (XmlNode queryNode in requestList)
            {
                XmlAttribute nameAttr = queryNode.Attributes["Name"];

                if (nameAttr == null)
                    throw new InvalidQueriesFileException("Name attribute missing on a Query node");

                Queries[t].Add(nameAttr.Value, queryNode.InnerText);
            }
        }

        #region IDisposable members

        /// <summary>
        /// Releases all resources used
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Connection?.Dispose();
                Transaction?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
