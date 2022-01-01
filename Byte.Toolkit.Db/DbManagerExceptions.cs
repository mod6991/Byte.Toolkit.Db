namespace Byte.Toolkit.Db
{
    public class ObjectNotRegisteredException : Exception
    {
        public ObjectNotRegisteredException(Type t) : base($"Type '{t}' not registered. Use DbManager.RegisterDbObject method first") { }
    }

    public class InvalidQueriesFileException : Exception
    {
        public InvalidQueriesFileException(string message) : base(message) { }
    }

    public class InvalidDbObjectException : Exception
    {
        public InvalidDbObjectException(Type t) : base($"Type '{t}' does not have the attribute [DbObjectAttribute]") { }
    }

    public class DbObjectAlreadyRegisteredException : Exception
    {
        public DbObjectAlreadyRegisteredException(Type t) : base($"Type '{t}' alredy registered") { }
    }
}
