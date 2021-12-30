using FastMember;
using System.Data;
using System.Data.Common;

namespace Byte.Toolkit.Db
{
    public class RequestFileManager
    {
        private DbManager _dbManager;
        private Dictionary<string, string> _requests;

        #region Constructor

        public RequestFileManager(DbManager dbManager, string name)
        {
            _dbManager = dbManager;

            if (!_dbManager.Requests.ContainsKey(name))
                throw new RequestsNotFoundException($"Requests not found for name '{name}'");

            _requests = _dbManager.Requests[name];
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Execute a SQL request by its name and stores the results in a DataTable
        /// </summary>
        /// <param name="requestName">Request name</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="table">DataTable</param>
        public void FillDataTable(string requestName, List<DbParameter> parameters, DataTable table)
        {
            if (!_requests.ContainsKey(requestName))
                throw new RequestNotFoundException($"Request '{requestName}' not found");

            using (DbCommand command = _dbManager.Connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = _requests[requestName];

                if (_dbManager.Transaction != null)
                    command.Transaction = _dbManager.Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                using (DbDataAdapter adapter = _dbManager.Factory.CreateDataAdapter())
                {
                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }
            }
        }

        /// <summary>
        /// Execute a SQL request by its name and stores the results in a list of objects
        /// </summary>
        /// <typeparam name="T">Type of objects to return</typeparam>
        /// <param name="requestName">Request name</param>
        /// <param name="parameters">Parameters</param>
        public List<T> FillObjects<T>(string requestName, List<DbParameter> parameters)
        {
            if (!_requests.ContainsKey(requestName))
                throw new RequestNotFoundException($"Request '{requestName}' not found");
            if (!_dbManager.TypeMappings.ContainsKey(typeof(T)) || !_dbManager.TypeAccessors.ContainsKey(typeof(T)))
                throw new ObjectNotRegisteredException($"Type '{typeof(T).FullName}' not registered! Use DbManager RegisterDbObject method");

            List<T> list = new List<T>();
            Dictionary<string, string> mappingList = _dbManager.TypeMappings[typeof(T)];
            TypeAccessor ta = _dbManager.TypeAccessors[typeof(T)];

            using (DbCommand command = _dbManager.Connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = _requests[requestName];

                if (_dbManager.Transaction != null)
                    command.Transaction = _dbManager.Transaction;

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
        /// Execute a SQL request by its name
        /// </summary>
        /// <param name="requestName">Request name</param>
        /// <param name="parameters">Parameters</param>
        public int ExecuteNonQuery(string requestName, List<DbParameter> parameters)
        {
            if (!_requests.ContainsKey(requestName))
                throw new RequestNotFoundException($"Request '{requestName}' not found");

            using (DbCommand command = _dbManager.Connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = _requests[requestName];

                if (_dbManager.Transaction != null)
                    command.Transaction = _dbManager.Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Execute a SQL request by its name and returns the single result
        /// </summary>
        /// <param name="requestName">Request name</param>
        /// <param name="parameters">Parameters</param>
        public object ExecuteScalar(string requestName, List<DbParameter> parameters)
        {
            if (!_requests.ContainsKey(requestName))
                throw new RequestNotFoundException($"Request '{requestName}' not found");

            using (DbCommand command = _dbManager.Connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = _requests[requestName];

                if (_dbManager.Transaction != null)
                    command.Transaction = _dbManager.Transaction;

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (DbParameter param in parameters)
                        command.Parameters.Add(param);
                }

                return command.ExecuteScalar();
            }
        }

        #endregion
    }
}
