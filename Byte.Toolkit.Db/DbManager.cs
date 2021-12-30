using FastMember;
using System.Data;
using System.Data.Common;
using System.Xml;

namespace Byte.Toolkit.Db
{
    public class DbManager : IDisposable
    {
        private bool _disposed;
        private DbConnection _connection;
        private DbProviderFactory _factory;
        private DbTransaction _transaction;
        private string _connectionString;
        private string _provider;
        private Dictionary<Type, TypeAccessor> _typeAccessors;
        private Dictionary<Type, Dictionary<string, string>> _typeMappings;
        private Dictionary<string, RequestFileManager> _requestFileManagers;
        private Dictionary<string, Dictionary<string, string>> _requests;

        #region Constructors

        /// <summary>
        /// Create a new instance of DbManager with ConnectionString and Provider
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="provider">Provider name</param>
        public DbManager(string connectionString, string provider)
        {
            _connectionString = connectionString;
            _provider = provider;
            _factory = DbProviderFactories.GetFactory(_provider);
            _connection = _factory.CreateConnection();
            _connection.ConnectionString = _connectionString;

            _typeAccessors = new Dictionary<Type, TypeAccessor>();
            _typeMappings = new Dictionary<Type, Dictionary<string, string>>();
            _requestFileManagers = new Dictionary<string, RequestFileManager>();
            _requests = new Dictionary<string, Dictionary<string, string>>();
        }

        ~DbManager()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        public bool Disposed
        {
            get { return _disposed; }
        }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

        public string Provider
        {
            get { return _provider; }
        }

        public DbProviderFactory Factory
        {
            get { return _factory; }
        }

        public DbConnection Connection
        {
            get { return _connection; }
        }

        public DbTransaction Transaction
        {
            get { return _transaction; }
        }

        public Dictionary<Type, TypeAccessor> TypeAccessors
        {
            get { return _typeAccessors; }
        }

        public Dictionary<Type, Dictionary<string, string>> TypeMappings
        {
            get { return _typeMappings; }
        }

        public Dictionary<string, Dictionary<string, string>> Requests
        {
            get { return _requests; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Open the connection to the database
        /// </summary>
        public void Open()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            _connection.Open();
        }

        /// <summary>
        /// Close the connection to the database
        /// </summary>
        public void Close()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            _connection.Close();
        }

        /// <summary>
        /// Start a database transaction
        /// </summary>
        public void BeginTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            _transaction = _connection.BeginTransaction();
        }

        /// <summary>
        /// End a database transaction.
        /// </summary>
        /// <param name="commit">Commits the transaction</param>
        public void EndTransaction(bool commit)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (_transaction != null)
            {
                if (commit)
                    _transaction.Commit();
                else
                    _transaction.Rollback();

                _transaction = null;
            }
        }

        /// <summary>
        /// Return a new parameter
        /// </summary>
        public DbParameter CreateParameter()
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            return _factory.CreateParameter();
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

            DbParameter param = _factory.CreateParameter();
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

            _typeAccessors.Add(t, TypeAccessor.Create(t));
            IDbObject obj = (IDbObject)Activator.CreateInstance(t);
            _typeMappings.Add(t, obj.GetMapping());
        }

        /// <summary>
        /// Execute a SQL request and stores the results in a DataTable
        /// </summary>
        /// <param name="request">SQL request</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="table">DataTable</param>
        public void FillDataTableWithRequest(string request, List<DbParameter> parameters, DataTable table)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = request;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataAdapter adapter = _factory.CreateDataAdapter())
                {
                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }
            }
        }

        /// <summary>
        /// Execute a stored procedure and stores the results in a DataTable
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="table">DataTable</param>
        public void FillDataTableWithProcedure(string procedureName, List<DbParameter> parameters, DataTable table)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procedureName;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataAdapter adapter = _factory.CreateDataAdapter())
                {
                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }
            }
        }

        /// <summary>
        /// Execute a SQL request and stores the results in a list of objects
        /// </summary>
        /// <typeparam name="T">Type of objects to return</typeparam>
        /// <param name="request">SQL request</param>
        /// <param name="parameters">Parameters</param>
        public List<T> FillObjectsWithRequest<T>(string request, List<DbParameter> parameters)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (!_typeMappings.ContainsKey(typeof(T)) || !_typeAccessors.ContainsKey(typeof(T)))
                throw new ObjectNotRegisteredException($"Type '{typeof(T).FullName}' not registered! Use DbManager RegisterDbObject method");

            List<T> list = new List<T>();
            Dictionary<string, string> mappingList = _typeMappings[typeof(T)];
            TypeAccessor ta = _typeAccessors[typeof(T)];

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = request;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T obj = (T)Activator.CreateInstance(typeof(T));

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string fieldName = reader.GetName(i);

                            if (mappingList.ContainsKey(fieldName))
                            {
                                object readerValue = reader[fieldName];
                                if (!(readerValue is DBNull))
                                    ta[obj, mappingList[fieldName]] = readerValue;
                            }
                        }

                        list.Add(obj);
                    }

                    reader.Close();
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a stored procedure and stores the results in a list of objects
        /// </summary>
        /// <typeparam name="T">Type of objects to return</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Parameters</param>
        public List<T> FillObjectsWithProcedure<T>(string procedureName, List<DbParameter> parameters)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (!_typeMappings.ContainsKey(typeof(T)) || !_typeAccessors.ContainsKey(typeof(T)))
                throw new ObjectNotRegisteredException($"Type '{typeof(T).FullName}' not registered. Use DbManager RegisterDbObject method first");

            List<T> list = new List<T>();
            Dictionary<string, string> mappingList = _typeMappings[typeof(T)];
            TypeAccessor ta = _typeAccessors[typeof(T)];

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procedureName;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T obj = (T)Activator.CreateInstance(typeof(T));

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string fieldName = reader.GetName(i);

                            if (mappingList.ContainsKey(fieldName))
                            {
                                object readerValue = reader[fieldName];
                                if (!(readerValue is DBNull))
                                    ta[obj, mappingList[fieldName]] = readerValue;
                            }
                        }

                        list.Add(obj);
                    }

                    reader.Close();
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a SQL request
        /// </summary>
        /// <param name="request">SQL request</param>
        /// <param name="parameters">Parameters</param>
        public int ExecuteNonQueryWithRequest(string request, List<DbParameter> parameters)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = request;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Execute a stored procedure
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Parameters</param>
        public int ExecuteNonQueryWithProcedure(string procedureName, List<DbParameter> parameters)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procedureName;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Execute a SQL request and returns the single result
        /// </summary>
        /// <param name="request">SQL request</param>
        /// <param name="parameters">Parameters</param>
        public object ExecuteScalarWithRequest(string request, List<DbParameter> parameters)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = request;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Execute a stored procedure and returns the single result
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Parameters</param>
        public object ExecuteScalarWithProcedure(string procedureName, List<DbParameter> parameters)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procedureName;

                if (_transaction != null)
                    command.Transaction = _transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteScalar();
            }
        }

        #endregion

        #region RequestFileManager

        /// <summary>
        /// Add a request file
        /// </summary>
        /// <param name="name">Name of the request file</param>
        /// <param name="requestFile">Path of the file</param>
        public void AddRequestFile(string name, string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(DbManager).FullName);

            if (_requests.ContainsKey(name))
                throw new RequestFileAlreadyAddedException($"Name '{name}' already added");

            _requests.Add(name, new Dictionary<string, string>());

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNodeList requestList = doc.SelectNodes("/Requests/Request");

            foreach (XmlNode requestNode in requestList)
            {
                XmlAttribute nameAttr = requestNode.Attributes["Name"];

                if (nameAttr == null)
                    throw new InvalidRequestFileException("Name attribute missing on a Request node");

                _requests[name].Add(nameAttr.Value, requestNode.InnerText);
            }

            RequestFileManager manager = new RequestFileManager(this, name);
            _requestFileManagers.Add(name, manager);
        }

        /// <summary>
        /// Indexer for the request files
        /// </summary>
        /// <param name="name">Request file name</param>
        public RequestFileManager this[string name]
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(typeof(DbManager).FullName);

                if (!_requestFileManagers.ContainsKey(name))
                    throw new RequestFileNotFoundException($"Request file '{name}' not found");
                return _requestFileManagers[name];
            }
        }

        #endregion

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
                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }

                _transaction = null;
                _factory = null;
                _connectionString = null;
                _provider = null;
            }

            _disposed = true;
        }

        #endregion
    }
}
