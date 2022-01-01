using NUnit.Framework;
using System.Collections.Generic;

namespace DbToolkitUnitTests
{
    public class DbManagerTests
    {
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
                    Assert.That(groups.Count > 0);
                    Assert.That(users.Count > 0);
                    Assert.That(user?.Name == "Jimi Hendrix");
                });
            }
        }
    }
}