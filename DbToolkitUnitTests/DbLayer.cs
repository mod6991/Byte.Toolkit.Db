using Byte.Toolkit.Db;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DbToolkitUnitTests
{
    internal class DbLayer
    {
        public DbLayer()
        {
            DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
            DbManager = new DbManager(@"Data Source=data\testdb.sqlite", "System.Data.SQLite");

            DbManager.RegisterDbObject(typeof(UserGroup));
            DbManager.AddQueriesFile(typeof(UserGroup), @"data\UserGroupQueries.xml");
            UserGroup = new UserGroupLayer(DbManager);

            DbManager.RegisterDbObject(typeof(User));
            DbManager.AddQueriesFile(typeof(User), @"data\UserQueries.xml");
            User = new UserLayer(DbManager);
        }

        public DbManager DbManager { get; set; }
        public UserGroupLayer UserGroup { get; set; }
        public UserLayer User { get; set; }
    }

    internal class UserGroupLayer : DbObjectLayer<UserGroup>
    {
        public UserGroupLayer(DbManager db)
            : base(db) { }

        public List<UserGroup> GetAllGroups() => DbManager.FillObjects<UserGroup>(Queries["GetAllGroups"]);
    }

    internal class UserLayer : DbObjectLayer<User>
    {
        public UserLayer(DbManager db)
            : base(db) { }

        public List<User> GetAllUsers() => DbManager.FillObjects<User>(Queries["GetAllUsers"]);

        public User? GetUserById(Int64 id)
        {
            List<DbParameter> parameters = new List<DbParameter>();
            parameters.Add(DbManager.CreateParameter("id", id));
            return DbManager.FillSingleObject<User>(Queries["GetUserById"], CommandType.Text, parameters);
        }
    }
}
