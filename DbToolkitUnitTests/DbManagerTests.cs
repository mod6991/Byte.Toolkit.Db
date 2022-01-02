using Byte.Toolkit.Db;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DbToolkitUnitTests
{
    public class DbManagerTests
    {
        static DbManager GetDbManagerSqlite()
        {
            DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
            return new DbManager(@"Data Source=data\testdb.sqlite", "System.Data.SQLite");
        }

        [Test]
        public void TestDbLayer()
        {
            DbLayer db = new DbLayer();
            using (db.DbManager)
            {
                db.DbManager.Open();

                List<UserGroup> groups = db.UserGroup.GetAllGroups();

                List<User> users = db.User.GetAllUsers();
                User? user = db.User.GetUserById(2);

                db.DbManager.Close();

                Assert.Multiple(() =>
                {
                    Assert.That(groups.Count == 2);
                    Assert.That(users.Count == 3);
                    Assert.That(user?.Name == "Jimi Hendrix");
                });
            }
        }

        [Test]
        public void TestTransactions()
        {
            using (DbManager db = GetDbManagerSqlite())
            {
                db.RegisterDbObject(typeof(UserGroup));

                List<DbParameter> parameters = new List<DbParameter>();
                parameters.Add(db.CreateParameter("groupId", 3));
                parameters.Add(db.CreateParameter("groupName", "TestGroup1"));

                db.Open();
                db.BeginTransaction();
                db.ExecuteNonQuery("insert into user_group (group_id, group_name) values (@groupId, @groupName)", parameters: parameters);
                db.EndTransaction(true);
                UserGroup? group = db.FillSingleObject<UserGroup>("select * from user_group where group_id = 3");
                Assert.AreEqual("TestGroup1", group?.Name);

                db.BeginTransaction();
                parameters = new List<DbParameter>();
                parameters.Add(db.CreateParameter("groupId", 4));
                parameters.Add(db.CreateParameter("groupName", "TestGroup2"));

                db.ExecuteNonQuery("insert into user_group (group_id, group_name) values (@groupId, @groupName)", parameters: parameters);
                db.EndTransaction(false);
                group = db.FillSingleObject<UserGroup>("select * from user_group where group_id = 4");
                Assert.That(group == null);
            }
        }

        [Test]
        public void TestRegisterDbObjects()
        {
            using (DbManager db = GetDbManagerSqlite())
            {
                Assert.Multiple(() =>
                {
                    Assert.Throws<InvalidDbObjectException>(() =>
                    {
                        db.RegisterDbObject(typeof(TestObject));
                    });

                    Assert.DoesNotThrow(() =>
                    {
                        db.RegisterDbObject(typeof(User));
                    });
                });
            }
        }

        [Test]
        public void TestUnregisteredObject()
        {
            using (DbManager db = GetDbManagerSqlite())
            {
                db.Open();

                Assert.Throws<ObjectNotRegisteredException>(() =>
                {
                    User? user = db.FillSingleObject<User>("select * from user");
                });

                Assert.Throws<ObjectNotRegisteredException>(() =>
                {
                    List<User> users = db.FillObjects<User>("select * from user");
                });
            }
        }

        //test fill datatable
        //test fill single object
        //test fill object list
        //test non query
        //test scalar
        //test get columns names types

    }
}