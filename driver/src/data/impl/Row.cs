/*-
 *
 *  This file is part of Oracle NoSQL Database
 *  Copyright (C) 2015, 2020 Oracle and/or its affiliates.  All rights reserved.
 *
 * If you have received this file as part of Oracle NoSQL Database the
 * following applies to the work as a whole:
 *
 *   Oracle NoSQL Database server software is free software: you can
 *   redistribute it and/or modify it under the terms of the GNU Affero
 *   General Public License as published by the Free Software Foundation,
 *   version 3.
 *
 *   Oracle NoSQL Database is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *   Affero General Public License for more details.
 *
 * If you have received this file as part of Oracle NoSQL Database Client or
 * distributed separately the following applies:
 *
 *   Oracle NoSQL Database client software is free software: you can
 *   redistribute it and/or modify it under the terms of the Apache License
 *   as published by the Apache Software Foundation, version 2.0.
 *
 * You should have received a copy of the GNU Affero General Public License
 * and/or the Apache License in the LICENSE file along with Oracle NoSQL
 * Database client or server distribution.  If not, see
 * <http://www.gnu.org/licenses/>
 * or
 * <http://www.apache.org/licenses/LICENSE-2.0>.
 *
 * An active Oracle commercial licensing agreement for this product supersedes
 * these licenses and in such case the license notices, but not the copyright
 * notice, may be removed by you in connection with your distribution that is
 * in accordance with the commercial licensing terms.
 *
 * For more information please contact:
 *
 * berkeleydb-info_us@oracle.com
 *
 */




namespace oracle.kv.client.data {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using oracle.kv.client;
    using oracle.kv.proxy.gen;
    using oracle.kv.client.error;
    using oracle.kv.client.option;

    /// <summary>
    /// Represnts state of a database row in runtime memory.
    /// <para></para>
    /// A row is a multi-valued tuple whose value can be accessed
    /// by property name. A property value can be a composite of other
    /// types, supporting a semi-typed, nested hierarchy of property
    /// values that can be referred by navigational path.
    /// </summary>
    /// <remarks>
    /// A row is managed. A property value is validated for its type
    /// and a property name is validated by a database schema. 
    /// <para>
    /// A row can be sourced from and externalized to a a JSON string.
    /// A row is backed by a Thrift strcture. The state of the row
    /// is sourced from and flushed to the underlying Thrift structure.
    /// </para>
    /// </remarks>
    /// 
    public class RowImpl : Thrifty<TRow>, IRow {
        DataObject _dataObject;
        public IDataModel DataModel { get; private set; }
        ITable Table { get; set; }
        public string TableName { get { return Table.Name; } }
        bool Dirty { get; set; }
        public RowVersion Version { get; internal set; }
        public IRow Previous { get; internal set; }


        //public bool IsTransient {
        //    get { return Table.IsTransient; }
        //}

        public virtual IEnumerable<string> DefinedPropertyNames {
            get {
                return Table.ColumnNames;
            }
        }

        public virtual IEnumerable<string> PopulatedPropertyNames {
            get {
                return DataObject().PropertyNames;
            }
        }

        public virtual bool IsDefinedProperty(string property) {
            return Table.HasColumn(property);
        }

        public bool IsPopulatedProperty(string property) {
            return DataObject().HasProperty(property);
        }

        internal RowImpl(TRow tRow, ITable table, IDataModel dataModel) : base(tRow) {
            Assert.NotNull(dataModel, "Can not create row with null data model", true);
            DataModel = dataModel;
            Table = table;
            ExpirationTime = TimeToLive.asExpirationTime(tRow.Ttl);
        }

        internal RowImpl(TRow tRow, string tableName, IDataModel dataModel) :
        this(tRow, dataModel.GetTable(tableName, true), dataModel) { }




        internal RowImpl(TRowAndMetadata tRowAndMetaData, ITable table, IDataModel dataModel) :
        this(new TRow(), table, dataModel) {
            Thrift.JsonRow = tRowAndMetaData.JsonRow;
            ExpirationTime = tRowAndMetaData.Expiration;
            Version = new RowVersionImpl(tRowAndMetaData.RowVersion);
        }


        public IRow PopulateWithDict(Dictionary<string, object> dict) {
            return Populate(new DataObject(dict));
        }

        public IRow PopulateWithJSON(string jsonString) {
            var deserailized = DataModel.Deserialize(jsonString) as DataObject;
            return Populate(deserailized);
        }

        IRow Populate(DataObject dataObject) {
            _dataObject = new DataObject();
            foreach (string property in dataObject.PropertyNames) {
                IColumn col = Table.GetColumn(property, true);
                object value = dataObject[property];
                DataModel.PutValue(_dataObject, property, value,
                    (v) => col.ConvertToDatabaseType(v));
            }
            Thrift.JsonRow = dataObject.ToJSONString().ToString();

            return this;
        }


        public long ExpirationTime {
            get {
                return TimeToLive.asExpirationTime(Thrift.Ttl);
            }
            internal set {
                Thrift.Ttl = TimeToLive.asThrift(value);
            }
        }

        public TimeToLive TTL {
            set {
                Assert.NotNull(value);
                Thrift.Ttl = value.Thrift;
            }
        }

        /// <summary>
        /// Gets or Sets the value to a given property of this row.
        /// </summary>
        /// <param name="property">name of a property. A property name
        /// can be a navigation path.</param>
        public object this[string property] {
            get { return Get(property); }
            set { Put(property, value); }
        }


        /// <summary>
        /// Gets the value to a given property of this row converted
        /// by the given function.
        /// </summary>
        /// <param name="property">name of a property. A property name
        /// can be a navigation path.</param>
        /// <param name="converter">a pointer to a function that converts
        /// a given input argument (as avialble in a row property) to
        /// a given generic type.</param>
        public T Get<T>(string property, Func<object, T> converter) {
            return converter(Get(property));
        }

        object Get(string property) {
            IColumn col = Table.GetColumn(property, true);
            return DataModel.GetValue<object>(DataObject(), property,
                (v) => col.ConvertToLanguageType(v));
        }

        void Put(string property, object value) {
            if (!IsDefinedProperty(property)) {
                throw new ArgumentException("Property [" + property + "] not defined."
                    + " Defined properties are " + string.Join(",", DefinedPropertyNames));
            }
            IColumn col = Table.GetColumn(property, true);
            DataModel.PutValue(DataObject(), property, value,
                (v) => col.ConvertToDatabaseType(v));
            Dirty = true;
        }


        public string ToJSONString() {
            return DataObject().ToJSONString().ToString();
        }

        /// <summary>
        /// Affirms if this row contains given property.
        /// </summary>
        /// <remarks>
        /// Only affirms if the property refers to a 'top-level' property.
        /// Does not check property of nested elements.
        /// </remarks>
        /// <returns><c>true</c>, if this row contains the property</returns>
        /// <param name="property">name of a property. The name is case-sensitive.</param>
        public bool HasProperty(string property) {
            if (string.IsNullOrEmpty(TableName)) return true;
            ITable table = DataModel.GetTable(TableName, false);
            return table == null || table.HasColumn(property);
        }

        /// <summary>
        /// Gets the underlying Thrift data structure.
        /// The local data, if dirty, is flushed to internal Thrift structure 
        /// before caller gets access.
        /// </summary>
        /// <value>The thrift.</value>
        internal override TRow Thrift {
            get {
                flush();
                return base.Thrift;
            }
        }

        /// <summary>
        /// The actual data is mainatined in a Thrift structure in JSON format.
        /// This method parses JSON data to a JSONOBject and caches it in this
        /// instance to reduce computational cost of parsing for each field access.
        /// </summary>

        // TODO: provide option to opt for improved memory consumption over access
        // by not having to store a parsed dictionary.

        internal DataObject DataObject() {
            if (_dataObject == null) {
                _dataObject = (Thrift == null || Thrift.JsonRow == null) ?
                    new DataObject()
                  : DataModel.Deserialize(Thrift.JsonRow) as DataObject;
            }
            Assert.NotNull(_dataObject, "cached row data must never be null!");

            return _dataObject;
        }

        void flush() {
            if (!Dirty) return;

            Dirty = false;
            Thrift.JsonRow = DataObject().ToJSONString().ToString();
        }


        public override string ToString() {
            return ToJSONString();
        }
    }



    public class KeyPair : RowImpl, IRow {
        internal KeyPair(IRow pk, IRow ik, IDataModel support) :
        base(new TRow(), support.GetTable(pk.TableName, true), support) {
            PrimaryKey = pk;
            IndexKey = ik;
        }
        public IRow PrimaryKey { get; private set; }
        public IRow IndexKey { get; private set; }
    }





    /// <summary>
    /// Extends basic data support for driver specific customization.
    /// </summary>

    class RowDataModel : BaseDataModel {

        public override string Serialize(object obj) {
            if (obj is RowImpl) {
                return (obj as RowImpl).DataObject().ToJSONString().ToString();
            }
            return base.Serialize(obj);
        }
    }

    public class RowTypeMapping {
        internal ISerializationSupport serailizer { get; set; }
        /// <summary>
        /// Extends type conversion of base implementation by
        /// <list type="bullet">
        /// <item><description>
        /// a string is deserialized to a <see cref="IDataContainer"/>,
        /// if possible. Otherwise, it is treated as regular string.
        /// </description></item>
        /// <item><description>
        /// A <see cref="DateTime"/> is converted to a formatted string
        /// </description></item>
        /// <item><description>
        /// A <see cref="decimal"/> is converted to a string with suffix 'M'
        /// </description></item>
        /// </list>
        /// </summary>
        /// <returns>The value to stored type.</returns>
        /// <param name="userValue">the value as provided by user of the
        /// interface.</param>
        public object ConvertToStoredType(object userValue, Type storedType) {
            object storedValue = userValue;
            if (userValue is string) {
                string possibleJSON = (userValue as string);
                char startChar = possibleJSON.Length > 0 ? possibleJSON[0] : '\0';
                if (startChar == '{' || startChar == '[') {
                    try {
                        storedValue = serailizer.Deserialize(userValue as string);
                    } catch (Exception ex) {
                        Console.WriteLine("Can not parse " + storedValue + " as a JSON formatted value,"
                    + " hence stored as regular string. Parse exception was " + ex);
                    }
                }
            } else if (userValue is DateTime) {
                return storedValue = ToTimestamp((DateTime)userValue);
            } else if (userValue is decimal) {
                return storedValue = userValue.ToString() + "M";
            } else {
                try {
                    storedValue = Convert.ChangeType(userValue, storedType);
                } catch (Exception) {
                    throw new ArgumentException("Can not convert value " + userValue
                    + " of type " + userValue.GetType() + " to type " + storedType);
                }
            }
            // finally check with data type against base
            return storedValue;
        }
        /// <summary>
        /// Converts the stored type to a user type.
        /// </summary>
        /// <remarks>
        /// This method mirros the effect of <see cref="M:ConvertToStoredType(object)"/>
        /// by convering stored type to user type if possible. 
        /// <para></para>
        /// The <see cref="IDataContainer"/>  values are converted back to string.
        /// <para></para>
        /// The other types e.g. DataTime, Decimal or Float that are converted 
        /// to a different type for storage do not carry enough type information
        /// to be reconverted to original type. 
        /// <para></para>
        /// </remarks>
        /// <returns>A type originally specifed by the user.</returns>
        /// <param name="storedValue">a stored object to be converted.</param>
        public object ConvertToUserType(object storedValue, Type userType) {
            return (storedValue is IDataContainer)
                 ? serailizer.Serialize(storedValue) : storedValue;
        }
        static string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss.";

        /// <summary>
        /// Converts given timestamp value to a string.
        /// </summary>
        /// <returns>The timestamp as a string.</returns>
        /// <param name="userTime">a timestamp value.</param>
        /// <remarks>
        /// The timesatmp is of the format
        /// <code>yyyy-MM-dd HH:mm:ss.nnn</code>
        /// where <code>nnn</code> is a value of sub-second precsion.
        /// The sub-second precison is same as <see cref="DateTime.Ticks"/> 
        /// </remarks>
        static string ToTimestamp(DateTime userTime) {
            DateTime toLatestSecond = new DateTime(
                userTime.Year, userTime.Month, userTime.Day,
                userTime.Hour, userTime.Minute, userTime.Second);
            long ticks = userTime.Ticks - toLatestSecond.Ticks;

            return userTime.ToString(TIME_FORMAT) + ticks;
        }
    }
}





