using Byte.Toolkit.Db;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;

namespace DbCodeGenerator
{
    internal class Program
    {
        static InputXml? _xml;

        static void Main(string[] args)
        {
            try
            {
                _xml = new InputXml("sample.xml");

                DbProviderFactories.RegisterFactory(_xml.ProviderName, _xml.FactoryType);
                using (DbManager db = new DbManager(_xml.ConnectionString, _xml.ProviderName))
                {
                    db.Open();

                    GenerateDbLayer();

                    foreach (DbObject obj in _xml.Objects)
                    {
                        Dictionary<string, Type> columns = db.GetColumnsNamesAndTypes(obj.Query);
                        obj.UpdateProperties(_xml, columns);

                        GenerateObject(obj, columns);
                        GenerateQueryFile(obj, columns);
                        GenerateObjectLayer(obj, columns);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void GenerateDbLayer()
        {
            if (_xml == null)
                throw new InvalidInputXml("Input XML file is null");

            string file = Path.Combine(_xml.Output, $"{_xml.DbClassName}.cs");

            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    sw.WriteLine($"using Byte.Toolkit.Db;");
                    sw.WriteLine($"using System;");
                    sw.WriteLine($"using System.Data.Common;");
                    sw.WriteLine();
                    sw.WriteLine($"namespace {_xml.LayersNamespace}");
                    sw.WriteLine($"{{");
                    sw.WriteLine($"    internal class {_xml.DbClassName}");
                    sw.WriteLine($"    {{");
                    sw.WriteLine($"        public {_xml.DbClassName}()");
                    sw.WriteLine($"        {{");
                    sw.WriteLine($"            DbProviderFactories.RegisterFactory(\"{_xml.ProviderName}\", \"{_xml.FactoryType}\");");
                    sw.WriteLine($"            DbManager = new DbManager(\"{_xml.ConnectionString}\", \"{_xml.ProviderName}\");");
                    sw.WriteLine();

                    int i = 0;
                    foreach(DbObject obj in _xml.Objects)
                    {
                        sw.WriteLine($"            DbManager.RegisterDbObject(typeof({obj.Name}));");
                        sw.WriteLine($"            DbManager.AddQueriesFile(typeof({obj.Name}), @\"Queries\\{obj.Name}.xml\");");
                        sw.WriteLine($"            {obj.Name} = new {obj.Name}Layer(DbManager);");

                        if (i < _xml.Objects.Count - 1)
                            sw.WriteLine();

                        i++;
                    }

                    sw.WriteLine($"        }}");
                    sw.WriteLine();
                    sw.WriteLine($"        public DbManager DbManager {{ get; set; }}");

                    foreach(DbObject obj in _xml.Objects)
                    {
                        sw.WriteLine($"        public {obj.Name}Layer {obj.Name} {{ get; set; }}");
                    }

                    sw.WriteLine($"    }}");
                    sw.WriteLine($"}}");
                }
            }
        }

        static void GenerateObject(DbObject obj, Dictionary<string, Type> columns)
        {
            if (_xml == null)
                throw new InvalidInputXml("Input XML file is null");

            string objectsDir = Path.Combine(_xml.Output, "Objects");

            if (!Directory.Exists(objectsDir))
                Directory.CreateDirectory(objectsDir);

            string file = Path.Combine(objectsDir, $"{obj.Name}.cs");

            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    sw.WriteLine($"using Byte.Toolkit.Db;");

                    if (_xml.PropertyType == PropertyType.Notify)
                        sw.WriteLine($"using {_xml.NotifyUsing};");

                    sw.WriteLine($"using System;");
                    sw.WriteLine();
                    sw.WriteLine($"namespace {_xml.ObjectsNamespace}");
                    sw.WriteLine($"{{");
                    sw.WriteLine($"    [DbObject]");

                    if (_xml.PropertyType == PropertyType.Notify)
                        sw.WriteLine($"    internal class {obj.Name} : {_xml.NotifyClass}");
                    else
                        sw.WriteLine($"    internal class {obj.Name}");

                    sw.WriteLine($"    {{");

                    if (_xml.PropertyType == PropertyType.Notify)
                    {
                        foreach (DbProperty prop in obj.Properties)
                            sw.WriteLine($"        private {prop.TypeName} {prop.FieldName};");

                        sw.WriteLine();
                    }

                    int i = 0;

                    foreach (DbProperty prop in obj.Properties)
                    {
                        sw.WriteLine($"        [DbColumn(\"{prop.ColumnName}\")]");
                        if (_xml.PropertyType == PropertyType.Notify)
                        {
                            sw.WriteLine($"        public {prop.TypeName} {prop.PropertyName}");
                            sw.WriteLine($"        {{");
                            sw.WriteLine($"            get => {prop.FieldName};");
                            sw.WriteLine($"            {String.Format(_xml.PropertySetTemplate, prop.FieldName, prop.PropertyName)}");
                            sw.WriteLine($"        }}");

                            if (i < columns.Count - 1)
                                sw.WriteLine();
                        }
                        else
                        {
                            sw.WriteLine($"        public {prop.TypeName} {prop.PropertyName} {{ get; set; }}");

                            if (i < columns.Count - 1)
                                sw.WriteLine();
                        }

                        i++;
                    }

                    sw.WriteLine($"    }}");
                    sw.WriteLine($"}}");
                }
            }
        }

        static void GenerateQueryFile(DbObject obj, Dictionary<string, Type> columns)
        {
            if (_xml == null)
                throw new InvalidInputXml("Input XML file is null");

            string queriesDir = Path.Combine(_xml.Output, "Queries");

            if (!Directory.Exists(queriesDir))
                Directory.CreateDirectory(queriesDir);

            string file = Path.Combine(queriesDir, $"{obj.Name}.xml");

            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    List<string> columnList = new List<string>();
                    List<string> parameterList = new List<string>();

                    foreach(DbProperty prop in obj.Properties)
                    {
                        columnList.Add(prop.ColumnName);
                        parameterList.Add(prop.ParameterNameWithChar);
                    }

                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                    sw.WriteLine("<Queries>");
                    sw.WriteLine($"  <Query Name=\"Select{obj.Name}ById\">");
                    sw.WriteLine($"    SELECT");
                    sw.WriteLine($"      {string.Join(", ", columnList)}");
                    sw.WriteLine($"    FROM ");
                    sw.WriteLine($"       {obj.TableName}");
                    sw.WriteLine($"    WHERE ");
                    sw.WriteLine($"       {columnList[0]} = {parameterList[0]}");
                    sw.WriteLine($"  </Query>");
                    sw.WriteLine($"  <Query Name=\"SelectAll{obj.Name}s\">");
                    sw.WriteLine($"    SELECT");
                    sw.WriteLine($"      {string.Join(", ", columnList)}");
                    sw.WriteLine($"    FROM ");
                    sw.WriteLine($"       {obj.TableName}");
                    sw.WriteLine($"  </Query>");
                    sw.WriteLine($"  <Query Name=\"Insert{obj.Name}\">");
                    sw.WriteLine($"    INSERT INTO {obj.TableName} ({string.Join(", ", columnList)})");
                    sw.WriteLine($"    VALUES ({string.Join(", ", parameterList)})");
                    sw.WriteLine($"  </Query>");
                    sw.WriteLine($"  <Query Name=\"Update{obj.Name}\">");
                    sw.WriteLine($"    UPDATE {obj.TableName}");
                    sw.WriteLine($"    SET");

                    if (columnList.Count > 1)
                    {
                        for (int i = 1; i < columnList.Count; i++)
                        {
                            sw.Write($"      {columnList[i]} = {parameterList[i]}");

                            if (i < columnList.Count - 1)
                                sw.WriteLine(", ");
                        }
                        sw.WriteLine();
                    }
                    else
                        sw.WriteLine($"      {columnList[0]} = {parameterList[0]}");

                    sw.WriteLine($"    WHERE ");
                    sw.WriteLine($"       {columnList[0]} = {parameterList[0]}");
                    sw.WriteLine($"  </Query>");
                    sw.WriteLine($"  <Query Name=\"Delete{obj.Name}ById\">");
                    sw.WriteLine($"    DELETE FROM {obj.TableName}");
                    sw.WriteLine($"    WHERE ");
                    sw.WriteLine($"       {columnList[0]} = {parameterList[0]}");
                    sw.WriteLine($"  </Query>");
                    sw.WriteLine("</Queries>");
                }
            }
        }

        static void GenerateObjectLayer(DbObject obj, Dictionary<string, Type> columns)
        {
            if (_xml == null)
                throw new InvalidInputXml("Input XML file is null");

            string file = Path.Combine(_xml.Output, $"{obj.Name}Layer.cs");

            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    string columnIdType = obj.Properties[0].TypeNameWithoutNullable;
                    string columnIdParameter = obj.Properties[0].ParameterName;

                    sw.WriteLine($"using Byte.Toolkit.Db;");
                    sw.WriteLine($"using System;");
                    sw.WriteLine($"using System.Collections.Generic;");
                    sw.WriteLine($"using System.Data;");
                    sw.WriteLine($"using System.Data.Common;");
                    sw.WriteLine();
                    sw.WriteLine($"namespace {_xml.LayersNamespace}");
                    sw.WriteLine($"{{");
                    sw.WriteLine($"    internal class {obj.Name}Layer : DbObjectLayer<{obj.Name}>");
                    sw.WriteLine($"    {{");
                    sw.WriteLine($"        public {obj.Name}Layer(DbManager db)");
                    sw.WriteLine($"            : base(db) {{ }}");
                    sw.WriteLine();
                    sw.WriteLine($"        public {obj.Name} Select{obj.Name}ById({columnIdType} {columnIdParameter})");
                    sw.WriteLine($"        {{");
                    sw.WriteLine($"            List<DbParameter> parameters = new List<DbParameter>();");
                    sw.WriteLine($"            parameters.Add(DbManager.CreateParameter(\"{columnIdParameter}\", {columnIdParameter}));");
                    sw.WriteLine($"            return DbManager.FillSingleObject<{obj.Name}>(Queries[\"Select{obj.Name}ById\"], parameters: parameters);");
                    sw.WriteLine($"        }}");
                    sw.WriteLine();
                    sw.WriteLine($"        public List<{obj.Name}> SelectAll{obj.Name}s() => DbManager.FillObjects<{obj.Name}>(Queries[\"SelectAll{obj.Name}s\"]);");
                    sw.WriteLine();
                    sw.WriteLine($"        public int Insert{obj.Name}({obj.Name} {obj.NameInstance})");
                    sw.WriteLine($"        {{");
                    sw.WriteLine($"            List<DbParameter> parameters = new List<DbParameter>();");
                    
                    foreach (DbProperty prop in obj.Properties)
                        sw.WriteLine($"            parameters.Add(DbManager.CreateParameter(\"{prop.ParameterName}\", {obj.NameInstance}.{prop.PropertyName}));");

                    sw.WriteLine($"            return DbManager.ExecuteNonQuery(Queries[\"Insert{obj.Name}\"], parameters: parameters);");
                    sw.WriteLine($"        }}");
                    sw.WriteLine();
                    sw.WriteLine($"        public int Update{obj.Name}({obj.Name} {obj.NameInstance})");
                    sw.WriteLine($"        {{");
                    sw.WriteLine($"            List<DbParameter> parameters = new List<DbParameter>();");
                    
                    foreach (DbProperty prop in obj.Properties)
                        sw.WriteLine($"            parameters.Add(DbManager.CreateParameter(\"{prop.ParameterName}\", {obj.NameInstance}.{prop.PropertyName}));");

                    sw.WriteLine($"            return DbManager.ExecuteNonQuery(Queries[\"Update{obj.Name}\"], parameters: parameters);");
                    sw.WriteLine($"        }}");
                    sw.WriteLine();
                    sw.WriteLine($"        public int Delete{obj.Name}ById({columnIdType} {columnIdParameter})");
                    sw.WriteLine($"        {{");
                    sw.WriteLine($"            List<DbParameter> parameters = new List<DbParameter>();");
                    sw.WriteLine($"            parameters.Add(DbManager.CreateParameter(\"{columnIdParameter}\", {columnIdParameter}));");
                    sw.WriteLine($"            return DbManager.ExecuteNonQuery(Queries[\"Delete{obj.Name}ById\"], parameters: parameters);");
                    sw.WriteLine($"        }}");
                    sw.WriteLine($"    }}");
                    sw.WriteLine($"}}");
                }
            }
        }
    }
}
