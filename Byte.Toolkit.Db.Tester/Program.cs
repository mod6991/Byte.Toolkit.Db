using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Byte.Toolkit.Db.Tester
{
    public class Program
    {
        static void Main()
        {
            try
            {
                DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
                DbManager db = new DbManager(@"Data Source=test.db", "System.Data.SQLite");
                db.RegisterDbObject(typeof(Client));
                db.Open();

                Client? client = db.FillObject<Client>("select * from client");

                db.Close();
            }
            catch(Exception ex)
            {

            }
        }
    }

    [DbObject]
    public class Client
    {
        [DbColumn("ID_CLIENT")]
        public Int64 IdClient { get; set; }

        [DbColumn("CLIENT_ACR")]
        public string? Acr { get; set; }

        [DbColumn("CLIENT_NAME")]
        public string? Name { get; set; }
    }
}
