# Byte.Toolkit.Db
Database management library using DbFactory and generic NET classes

## Initialisation

### Example with .NET Framework

In App.config:

```xml
<configuration>
  <system.data>
    <DbProviderFactories>
      <remove invariant="System.Data.SQLite"/>
      <add name="SQLite Data Provider" invariant="System.Data.SQLite"
           description=".Net Framework Data Provider for SQLite"
           type="System.Data.SQLite.SQLiteFactory, System.Data.SQLite" />
    </DbProviderFactories>
  </system.data>
</configuration>
```

Then in code:

```C#
DbManager db = new DbManager(@"Data Source=my_db.sqlite", "System.Data.SQLite");
```

### Example with .NET Core

```C#
DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
DbManager db = new DbManager(@"Data Source=my_db.sqlite", "System.Data.SQLite");
```

## DbObject

```C#
[DbObject]
internal class User
{
    [DbColumn("USER_ID")]
    public Int64? Id { get; set; }

    [DbColumn("GROUP_ID")]
    public Int64? GroupId { get; set; }

    [DbColumn("USERNAME")]
    public string? Username { get; set; }

    [DbColumn("PASSWORD")]
    public string? Password { get; set; }

    [DbColumn("FULL_NAME")]
    public string? Name { get; set; }
}
```

## DbObject XML queries file

```XML
<?xml version="1.0" encoding="utf-8" ?>
<Queries>
	<Query Name="GetAllUsers">
		select * from user
	</Query>
	<Query Name="GetUserById">
		select * from user where user_id = @id
	</Query>
</Queries>
```

## Db layers

```C#
internal class MyDbLayer
{
    public MyDbLayer()
    {
        DbProviderFactories.RegisterFactory("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
        DbManager = new DbManager(@"Data Source=data\testdb.sqlite", "System.Data.SQLite");

        DbManager.RegisterDbObject(typeof(User));
        DbManager.AddQueriesFile(typeof(User), @"data\UserQueries.xml");
        User = new UserLayer(DbManager);
    }

    public DbManager DbManager { get; set; }
    public UserLayer User { get; set; }
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
```

## Usage

```C#
MyDbLayer db = new MyDbLayer();
using (db.DbManager)
{
    db.DbManager.Open();

    List<User> users = db.User.GetAllUsers();
    User? user = db.User.GetUserById(2);

    db.DbManager.Close();
}
```
