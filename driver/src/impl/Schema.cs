using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using oracle.kv.client.data;
using oracle.kv.client.config;
using oracle.kv.client.error;
using oracle.kv.client.log;
using oracle.kv.proxy.gen;

namespace oracle.kv.client.data {
    /// <summary>
    /// Creates schema and sets it to a <see cref="IDataModel"/>.
    /// </summary>
    public static class SchemaFactory {
        static readonly object _lock = new object();
        /// <summary>
        /// Creates a schema for provided data model.
        /// </summary>
        /// <remarks>
        /// schema information is set on the model provided.
        /// Attempts to source schema descriptor from database and then from
        /// a local file.
        /// If no schema descriptor is available, initializes an empty schema.
        /// </remarks>
        /// <returns>The schema created.</returns>
        /// <param name="store">connection to data store. Must not be null.</param>
        /// <param name="model">a data model. Must not be null</param>
        public static ISchema CreateSchema(KVStore store, IDataModel model) {
            Assert.NotNull(store);
            Assert.NotNull(model);
            lock (_lock) {
                var schemaDescriptor = GetSchemaDescriptor(store);
                if (schemaDescriptor == null) {
                    schemaDescriptor = ReadFromFile((string)store.Driver[Options.SCHEMA_RESOURCE]);
                }

                ISchema schema = schemaDescriptor == null
                     ? new EmptySchema() : new DatabaseSchema(schemaDescriptor);
                /// important: Establish bi-directionalal relation between data model and schema
                model.Schema = schema;
                schema.DataModel = model;
                return schema;
            }
        }

        /// <summary>
        /// Gets the schema descriptor.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>The schema descriptor.</returns>
        /// <param name="store">Store.</param>
        static string GetSchemaDescriptor(KVStore store) {
            if (store == null) {
                return null;
            }
            try {
                return store.GetSchemaDescriptor();
            } catch (MissingMethodException) {
                LogManger.GetLogger(LogChannel.RUNTIME).Warn("Database schema is not supported");
                return null;
            }
        }

        static string ReadFromFile(string file) {
            if (file == null || !File.Exists(file)) {
                return null;
            }
            return File.ReadAllText(file);
        }

    }
    /// <summary>
    /// Implements a schema for a set of persistable tables.
    /// </summary>
    /// <remarks>
    /// a data model must be set to make this schema functional.
    /// </remarks>
    class DatabaseSchema : ISchema {
        public IDataModel DataModel { get; set; }
        Dictionary<string, ITable> _tables = new Dictionary<string, ITable>();

        /// <summary>
        /// Creates a schema from given string descriptor.
        /// </summary>
        /// <param name="schemaString">A JSON string describing a table schema.
        /// </param>
        /// <remarks>
        /// the string contains description of all tables. The properties and
        /// structure of this JSON is controlled by the database server and
        /// is an important part of schema negotiation scheme.
        /// <para>
        /// The schema may change in the server. The current schema negotiation
        /// between a client and database server does not address schema update.
        /// </para>
        /// </remarks>
        internal DatabaseSchema(string schemaString) {
            if (schemaString == null) return;
            // a schema descriptor string is paresd by native parser
            var dbSchema = new JSONParser()
                    .Parse(schemaString) as DataObject;
            var tables = dbSchema["tables"] as DataObjectArray;
            for (int i = 0; i < tables.Length; i++) {
                DataObject tableData = tables[i] as DataObject;
                string tableName = tableData.HasProperty("name") ? (string)tableData["name"] : null;
                ITable table = new DatabaseTable(tableName, tableData, this);
                _tables[tableData["name"] as string] = table;
            }
        }

        /// <summary>
        /// Gets the tables defined in this schema.
        /// </summary>
        /// <value>The tables.</value>
        public virtual IEnumerable<ITable> Tables {
            get {
                return _tables.Values;
            }
        }

        /// <summary>
        /// Gets the table of given name.
        /// </summary>
        /// <returns>handle to a table. </returns>
        /// <param name="tableName"> a table name. must not be null.</param>
        /// <param name="mustExist">If set to <c>true</c> must exist.</param>
        public virtual ITable GetTable(string tableName, bool mustExist) {
            if (tableName == null) {
                if (mustExist) {
                    throw new ArgumentException("null table name never can be found");
                }
                return null;
            } else if (_tables.ContainsKey(tableName)) {
                return _tables[tableName];
            } else if (mustExist) {
                throw new ArgumentException(tableName + " not found. Available "
                    + " tables are "
                    + string.Join(",", Tables.Select((t) => t.Name)).ToArray());
            } else {
                return null;
            }
        }

        public virtual IColumn GetColumn(string tableName, string columnName, bool mustExist) {
            ITable table = GetTable(tableName, mustExist);
            if (table == null) return null;
            return table.GetColumn(columnName, mustExist);
        }

        public virtual bool ContainsTable(string name) {
            return name != null && _tables.ContainsKey(name);
        }

        public bool RefreshSchema(IKVStore con) {
            return false;
        }

    }

    /// <summary>
    /// Implements a table that exists in database
    /// </summary>
    class DatabaseTable : ITable {
        internal DatabaseSchema Schema { get; set; }
        public virtual bool IsTransient { get { return false; } }

        public string Name { get; internal set; }
        Dictionary<string, IColumn> _columns = new Dictionary<string, IColumn>();
        Dictionary<string, IColumn> _pkcolumns = new Dictionary<string, IColumn>();



        /// <summary>
        /// Creates a table definition from given <see cref="DataObject"/>
        /// </summary>
        /// <param name="tableName">name of a table</param>
        /// <param name="fieldData">Data that contains field descriptors
        /// as an array of DataObjects. Can be null</param>
        /// <param name="schema">schema that manages row of this table</param>
        public DatabaseTable(string tableName, DataObject fieldData, DatabaseSchema schema) {
            Assert.NotNull(tableName, "can not create table with null name");
            Assert.NotNull(schema, "can not create table with null schema");
            Name = tableName;
            Schema = schema;
            if (fieldData == null) {
                return;
            }
            if (!fieldData.HasProperty("fields")) {
                return;
            }
            if (fieldData.HasProperty("name") && !fieldData["name"].Equals(Name)) {
                throw new ArgumentException("the table name " + Name + " does not"
                    + " match name of " + fieldData["name"] + " supplied field data");
            }
            var columns = fieldData["fields"] as DataObjectArray;
            var pkColumnNames = fieldData["primaryKey"] as DataObjectArray;
            for (int i = 0; columns != null && i < columns.Length; i++) {
                DataObject columnData = columns[i] as DataObject;
                string columnName = columnData["name"] as string;
                bool isPrimaryKeyColumn = pkColumnNames.HasValue(columnName);
                IColumn col = new DatabaseColumn(this, columnData, isPrimaryKeyColumn);
                _columns[columnName] = col;
                if (isPrimaryKeyColumn) {
                    _pkcolumns[columnName] = col;
                }
            }
        }



        /// <summary>
        /// Gets the columns defined in this table.
        /// </summary>
        public virtual IEnumerable<IColumn> Columns {
            get {
                return _columns.Values.ToList();
            }
        }

        /// <summary>
        /// Gets the column names defined in this table.
        /// </summary>
        public virtual string[] ColumnNames {
            get {
                return Columns.Select((c) => c.Name).ToArray();
            }
        }

        /// <summary>
        /// Affirms if the column of given Name is definable for this table.
        /// </summary>
        /// <remarks>
        /// Any column is definable for a transient table.
        /// </remarks>
        public virtual bool HasColumn(string columnName) {
            if (_columns.ContainsKey(columnName)) {
                return true;
            }
            if (PathUtil.Tail(columnName) != null) {
                foreach (DatabaseColumn col in _columns.Values) {
                    if (col.HasNestedColumn(PathUtil.Tail(columnName))) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a row for this table.
        /// </summary>
        /// <returns>The row.</returns>
        public virtual IRow CreateRow() {
            RowImpl row = new RowImpl(new TRow(), this, Schema.DataModel);
            return row;
        }

        /// <summary>
        /// Creates the row and poulates in from json.
        /// </summary>
        /// <param name="jsonString">a Json formatted string.</param>
        public virtual IRow CreateRow(string jsonString) {
            RowImpl row = new RowImpl(new TRow(), this, Schema.DataModel);
            row.PopulateWithJSON(jsonString);
            return row;
        }

        /// <summary>
        /// Creates the row and poulates in from dictionary values.
        /// </summary>
        /// <param name="dict">a dictionary of values.</param>
        public virtual IRow CreateRow(Dictionary<string, object> dict) {
            RowImpl row = new RowImpl(new TRow(), this, Schema.DataModel);
            row.PopulateWithDict(dict);
            return row;
        }


        /// <summary>
        /// Gets the primary key columns of this table.
        /// </summary>
        public virtual IColumn[] PrimaryKeys {
            get {
                return _pkcolumns.Values.ToArray();
            }
        }

        /// <summary>
        /// Gets name of the primary key columns of this table.
        /// </summary>
        public virtual string[] PrimaryKeyNames {
            get {
                return PrimaryKeys.Select((c) => c.Name).ToArray();
            }
        }

        /// <summary>
        /// Gets name of this table.
        /// </summary>
        public override string ToString() {
            return Name;
        }




        public virtual IColumn GetColumn(string columnPath, bool mustExist) {
            IColumn col = FindColumnRecursive(PathUtil.Head(columnPath), mustExist);
            if (PathUtil.Tail(columnPath) == null) return col;
            return (col as DatabaseColumn).GetNestedColumn(PathUtil.Tail(columnPath), mustExist);
        }

        IColumn FindColumnRecursive(string columnName, bool mustExist) {
            if (!_columns.ContainsKey(columnName)) {
                if (mustExist) {
                    throw new ArgumentException("column " + columnName + " does not exist"
                        + " in " + Name);
                } else {
                    return null;
                }

            }
            try {
                return _columns[columnName];
            } catch (Exception ex) {
                if (mustExist) {
                    throw new ArgumentException("column " + columnName + " can not be found"
                           + " in " + Name, ex);
                } else {
                    return null;
                }
            }
        }
    }

    class DatabaseColumn : IColumn {
        public string Name { get; private set; }
        public ITable Table { get; private set; }
        public string DatabaseTypeName { get; private set; }
        public Type LanguageType { get; private set; }
        public bool IsPrimaryKey { get; private set; }
        public virtual bool IsComposite { get; private set; }

        Dictionary<string, IColumn> _nestedColumns;
        ISerializationSupport _serializer = new JSONParser();

        static readonly string PRIMITIVE_ARRAY = "PRIMITIVE_ARRAY";

        protected bool MayBeJSON(string str) {
            if (string.IsNullOrEmpty(str)) return false;
            char startChar = str[0];
            return startChar == '{' || startChar == '[';
        }

        protected object Deserialize(string str) {
            return new JSONParser().Deserialize(str);
        }


        /// <summary>
        /// Creates a column definition from given <see cref="DataObject"/>
        /// </summary>
        /// <param name="data"> data for column definition.</param>
        /// <remarks>
        /// The column type is represented as a name in input data as a string.
        /// The type name is a database type name.
        /// The name is mapped to a concrete C# type. 
        /// </remarks>
        public DatabaseColumn(ITable table, DataObject data) : this(table, data,
            false) { }

        public DatabaseColumn(ITable table, DataObject data, bool pk) {
            Table = table;
            Name = data["name"] as string;
            DatabaseTypeName = data.HasProperty("type") ? data["type"] as string
                // TODO: DEFAULT TYPE
                : "string";
            LanguageType = TypeMapping.Map(DatabaseTypeName);
            IsPrimaryKey = pk;
            bool composite = data.HasProperty("fields")
                          || data.HasProperty("collection");
            if (composite)
                //Console.WriteLine("creating column " + data["name"] + " in " + table);
                if (composite) {
                    _nestedColumns = new Dictionary<string, IColumn>();
                    var fields = default(DataObjectArray);
                    if (data.HasProperty("fields")) {
                        fields = data["fields"] as DataObjectArray;
                        for (int i = 0; i < fields.Length; i++) {
                            IColumn childCol = new DatabaseColumn(table, fields[i] as DataObject, pk);
                            _nestedColumns[childCol.Name] = childCol;
                        }
                    } else if (data.HasProperty("collection")) {
                        var coll = data["collection"] as DataObject;
                        if (coll.HasProperty("fields")) {
                            fields = coll["fields"] as DataObjectArray;
                            for (int i = 0; i < fields.Length; i++) {
                                IColumn childCol = new DatabaseColumn(table, fields[i] as DataObject, pk);
                                _nestedColumns[childCol.Name] = childCol;
                            }
                        } else {
                            //Console.WriteLine("primiver array " + Name);
                            coll["name"] = Name + "-" + PRIMITIVE_ARRAY;
                            IColumn primitiveElement = new DatabaseColumn(table, coll, false);
                            _nestedColumns[PRIMITIVE_ARRAY] = primitiveElement;
                        }
                    };
                }
            IsComposite = composite;
        }


        internal bool HasNestedColumn(string columnName) {
            if (!IsComposite) return false;
            foreach (DatabaseColumn nested in _nestedColumns.Values) {
                if (nested.HasNestedColumn(PathUtil.Tail(columnName))) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the nested column.
        /// </summary>
        /// <remarks>
        /// Path may have array index
        /// </remarks>
        /// <returns>The child column.</returns>
        /// <param name="path">Path.</param>
        /// <param name="mustExist">If set to <c>true</c> must exist.</param>
        internal IColumn GetNestedColumn(string path, bool mustExist) {
            bool found = false;

            if (IsComposite) {
                string colName = PathUtil.PropertyName(PathUtil.Head(path));
                found = _nestedColumns.ContainsKey(colName);
                if (found) {
                    DatabaseColumn col = _nestedColumns[colName] as DatabaseColumn;
                    string tailPath = PathUtil.Tail(path);
                    return (string.IsNullOrEmpty(PathUtil.Tail(path))) ? col
                        : col.GetNestedColumn(PathUtil.Tail(path), mustExist);
                }
            }
            if (!found && mustExist) {
                throw new ArgumentException(path + " not found");
            }
            return null;
        }

        public virtual object ConvertToLanguageType(object value) {
            if (value == null) {
                return null;
            } else if (LanguageType == null || value.GetType() == LanguageType) {
                return value;
            } else if (IsComposite) {
                return value;
            }
            try {
                return Convert.ChangeType(value, LanguageType);
            } catch (Exception ex) {
                throw new ArgumentException("Can not convert " + value + " of type "
                + value.GetType() + " to language type " + LanguageType, ex);
            }
        }

        public virtual object ConvertToDatabaseType(object value) {
            if (value == null) return null;
            object storedValue = value;
            if (IsComposite) {
                if (value is string) {
                    if (!MayBeJSON(value as string)) {
                        throw new ArgumentException("Composite property " + Name + " of database"
                        + " type " + DatabaseTypeName + " can only be populated with "
                        + " a JSON string, but was given string " + value);
                    }
                    storedValue = Deserialize(value as string);
                    if (storedValue is DataObject) {
                        if (!"RECORD".Equals(DatabaseTypeName)) {
                            throw new ArgumentException("the type of column"
                            + " is " + DatabaseTypeName + " can not accept "
                            + storedValue.GetType());
                        }
                    } else if (storedValue is DataObjectArray) {
                        if (!"ARRAY".Equals(DatabaseTypeName)) {
                            throw new ArgumentException("the type of column"
                            + " is " + DatabaseTypeName + " can not accept "
                            + storedValue.GetType());
                        }
                    } else {
                        throw new InternalError("supplied string " + value
                        + " for " + Name + " is deserialized into "
                        + storedValue.GetType() + ". this deserialized "
                        + " type is not expected, expected to be one of "
                        + " canonical composite types");
                    }
                } else if (value is DataObject || value is DataObjectArray) {
                } else {
                    throw new ArgumentException("Composite property " + Name + " of database"
                        + " type " + DatabaseTypeName + " can only be populated with "
                        + " a JSON string but was supllied with value " + value
                        + " of type " + value.GetType());


                }
            } else { // not composite
                Type storedType = TypeMapping.Map(DatabaseTypeName);
                if (storedType != value.GetType()) {
                    try {
                        storedValue = Convert.ChangeType(value, storedType);

                    } catch (Exception ex) {
                        throw new ArgumentException("Can not convert supplied "
                        + " value " + value + " of type " + value.GetType()
                        + " to database type " + DatabaseTypeName + " for property "
                        + Name, ex);

                    }
                }
            }

            return storedValue;
        }

    }

    /// <summary>
    /// An empty schema.  
    /// </summary>
    class EmptySchema : DatabaseSchema {
        public static readonly DatabaseSchema Instance = new EmptySchema();
        /// <summary>
        /// An empty schema is created with null descriptor
        /// </summary>
        internal EmptySchema() : base(null) {
        }
        /// <summary>
        /// An empty schema has no table.
        /// </summary>
        /// <value>The tables.</value>
        public override IEnumerable<ITable> Tables {
            get {
                return new ITable[0];
            }
        }

        /// <summary>
        /// Returns a table transient table of given name.
        /// </summary>
        /// <returns>The table.</returns>
        /// <param name="tableName">Table name.</param>
        /// <param name="mustExist">ignored.</param>
        public override ITable GetTable(string tableName, bool mustExist) {
            return new TransientTable(tableName, this);
        }
    }


    /// <summary>
    /// A transient table is a table without any constrains on property
    /// or value.
    /// </summary>
    class TransientTable : DatabaseTable {

        public override bool IsTransient { get { return true; } }
        /// <summary>
        /// a transient table is created with null field descriptor.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="schema">Model.</param>
        public TransientTable(string tableName, DatabaseSchema schema) :
                base(tableName, null, schema) {
        }


        /// <summary>
        /// Gets an empty array because no column is defined for a transient table.
        /// </summary>
        public override string[] ColumnNames {
            get {
                return new string[0];
            }
        }

        /// <summary>
        /// Gets an empty array because no column is defined for a transient table.
        /// </summary>
        public override IEnumerable<IColumn> Columns {
            get {
                return new IColumn[0];
            }
        }

        /// <summary>
        /// creates a transient row.
        /// </summary>
        public override IRow CreateRow() {
            return new RowImpl(new TRow(), this, Schema.DataModel);
        }

        /// <summary>
        /// creates a transient row and popualtes with JSON string
        /// </summary>
        public override IRow CreateRow(string jsonString) {
            IRow row = CreateRow();
            (row as RowImpl).PopulateWithJSON(jsonString);
            return row;
        }

        /// <summary>
        /// creates a transient column
        /// </summary>
        public override IColumn GetColumn(string columnName, bool mustExist) {
            return new TransientColumn(this, columnName);
        }

        /// <summary>
        /// always true
        /// </summary>
        public override bool HasColumn(string columnName) {
            return true;
        }

        public override IColumn[] PrimaryKeys {
            get {
                return new IColumn[0];
            }
        }

        public override string[] PrimaryKeyNames {
            get {
                return new string[0];
            }
        }

    }

    /// <summary>
    /// a schema-free column
    /// </summary>
    class TransientColumn : DatabaseColumn {
        public TransientColumn(ITable table, string name) :
            base(table, new DataObject().
            FromJSON("{\"name\":\"" + name + "\"}")) {
        }
        public override object ConvertToLanguageType(object value) {
            return value;
        }

        public override object ConvertToDatabaseType(object value) {
            if (value is string) {
                if (MayBeJSON(value as string)) {
                    return Deserialize(value as string);
                }
            }
            return value;
        }

    }

    /// <summary>
    /// Maps a database type name to a concrete C# type.
    /// </summary>
    /// <remarks>
    /// The type mapping is important in determining the type of value
    /// a data row can hold.
    /// </remarks>
    static class TypeMapping {
        static Dictionary<string, Type> dict = new Dictionary<string, Type>() {
        //   Database       Langauage
        //   Type Name      Type
            {"string",   typeof(string)},
            {"boolean",  typeof(bool)},
            {"integer",  typeof(int)},
            {"long",     typeof(long)},
            {"float",    typeof(float)},
            {"double",   typeof(double)},

            {"number",   typeof(decimal)},
            {"enum",     typeof(string)},


            {"binary",        typeof(byte[])},
            {"fixed_binary",  typeof(byte[])},

            {"record", typeof(DataObject)},
            {"array",  typeof(DataObjectArray)}
        };


        /// <summary>
        /// Maps the specified database TypeName to a C# type.
        /// </summary>
        /// <returns>a C# type.</returns>
        /// <param name="dbTypeName">Database type name.</param>
        /// <exception cref="ArgumentException">if the supplied type is
        /// not mapped</exception>
        public static Type Map(string dbTypeName) {
            if (string.IsNullOrEmpty(dbTypeName)) return typeof(string);
            try {
                return dict[dbTypeName.ToLower()];
            } catch (KeyNotFoundException) {
                throw new ArgumentException("no C# type mapped to database type "
                    + dbTypeName);
            }
        }
    }

}
