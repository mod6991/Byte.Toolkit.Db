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
                //DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
                //DbManager db = new DbManager(@"Data Source=test.db", "System.Data.SQLite");
                //db.RegisterDbObject(typeof(Group));
                //db.RegisterDbObject(typeof(Client));
                //db.Open();

                //Client? client = db.FillObject<Client>("select * from client where");
                //Dictionary<string, Type> dic = db.GetQueryColumnsTypes("select id_client, client_acr, client_name, count(*) from client");

                //db.Close();

                DbLayer db = new DbLayer();
                db.DbManager.Open();

                Client? client = db.ClientLayer.GetClientById(3);
                List<Client> clients = db.ClientLayer.GetAllClients();

                List<Group> groups = db.GroupLayer.GetAllGroups();

                db.DbManager.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    [DbObject]
    public class Client
    {
        [DbColumn("ID_CLIENT")]
        public Int64? IdClient { get; set; }

        [DbColumn("CLIENT_ACR")]
        public string? Acr { get; set; }

        [DbColumn("CLIENT_NAME")]
        public string? Name { get; set; }

        [DbColumn("count(*)")]
        public Int64? Count { get; set; }
    }

    [DbObject]
    public class Group
    {
        [DbColumn("ID_GROUP")]
        public Int64? Id { get; set; }

        [DbColumn("GROUP_NAME")]
        public string? Name { get; set; }
    }

    public class DbLayer
    {
        public DbLayer()
        {
            DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
            DbManager = new DbManager(@"Data Source=test.db", "System.Data.SQLite");
            
            DbManager.RegisterDbObject(typeof(Group));
            DbManager.RegisterDbObject(typeof(Client));

            DbManager.AddQueries(typeof(Group), new Dictionary<string, string>()
            {
                { "GetAllGroups", "select * from client_group" }
            });

            DbManager.AddQueries(typeof(Client), new Dictionary<string, string>()
            {
                { "GetAllClients", "select * from client" },
                { "GetClientById", "select * from client where id_client = @idClient" }
            });

            GroupLayer = new GroupLayer(DbManager);
            ClientLayer = new ClientLayer(DbManager);
        }

        public DbManager DbManager { get; }
        public GroupLayer GroupLayer { get; }
        public ClientLayer ClientLayer { get; }
    }

    public class GroupLayer : DbObjectLayer<Group>
    {
        public GroupLayer(DbManager db)
            : base(db) { }

        public List<Group> GetAllGroups() => Db.FillObjects<Group>(Queries[nameof(GetAllGroups)]);
    }

    public class ClientLayer : DbObjectLayer<Client>
    {
        public ClientLayer(DbManager db)
            : base(db) { }

        public Client? GetClientById(Int64 id)
        {
            List<DbParameter> parameters = new List<DbParameter>();
            parameters.Add(Db.CreateParameter("idClient", id));
            return Db.FillSingleObject<Client>(Queries[nameof(GetClientById)], CommandType.Text, parameters);
        }

        public List<Client> GetAllClients() => Db.FillObjects<Client>(Queries[nameof(GetAllClients)]);
    }
}
