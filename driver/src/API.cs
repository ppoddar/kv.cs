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



/*!
*  \addtogroup Driver
* \brief API for NoSQL database Driver.
*  @{
*/


/*!
 * C# Driver provides read, write, search facilities on Oracle NoSQL database.
 * The driver also executes  SQL statments synchronously and asynchronously.
 */
using System;
namespace oracle.kv.client {
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using oracle.kv.client.data;
    using oracle.kv.client.option;
    using oracle.kv.client.config;

    /// <summary>
    /// A driver provides connection to NoSQL database 
    /// to perform database operations. 
    /// </summary>
    /// <remarks>
    /// A driver supports a set of configurable options to tailor
    /// the data store connections.
    /// <para></para>
    /// A driver can be initialized by static <see cref="KVDriver.Create(string)"/>
    /// method.
    /// </remarks>
    public interface IKVDriver : IDisposable {
        /// <summary>
        /// Gets the URI of the connected data store.
        /// </summary>
        /// <remarks>A database URI follows the following syntax
        /// <pre>
        ///    nosql://{host:port}[,host:port]*/store-name
        /// </pre>
        /// If multiple host:ports are provided the URI does not follow
        /// standard URI syntax.
        ///  </remarks>
        /// <value>The URI of the data store.</value>
        string URI { get; }

        /// <summary>
        /// Gets the product version of this driver.
        /// </summary>
        /// <value>The version.</value>
        string Version { get; }


        /// <summary>
        /// Gets connection to a store.
        /// </summary>
        /// <remarks>
        /// Establishing a connection involves two network 
        /// calls. One from driver to a proxy server and other 
        /// from proxy to  database server. The communications use
        /// Thrift and RMI protocol respectively. 
        /// </remarks>
        /// 
        /// <returns>A connection to a data store.</returns>
        /// 
        /// <exception cref="ArgumentException"> if a 
        /// connection to a store cannot be obtained. A connection may be 
        /// unavailable for various reasons, one of the common reasons is 
        /// when a proxy service is not available.</exception>
        IKVStore GetStore();


        /// <summary>
        /// Gets and sets value of the given configuration property.
        /// </summary>
        /// <remarks>
        /// A driver can be configured with mutiple options.
        /// As all connections from same driver use same 
        /// propeties, once a connection has been obtained from a driver, 
        /// the configuration becomes immutable.
        /// </remarks>
        ///
        /// <param name="option"> a configuration property. 
        /// The property must be one of the supported options, 
        /// otherwise an exception is raised.
        /// Also a property can only be set before any connection
        /// has been created by this driver.
        /// </param>
        ///
        /// <exception cref="ArgumentException">if the given configuration property 
        /// is not supported by this driver </exception>
        object this[Option option] { get; set; }

        /// <summary>
        /// Gets all options supported by this driver.
        /// </summary>
        /// <value>All the supported options.</value>
        Option[] OptionsSupported();

        /// <summary>
        /// Gets options set on this driver to non-default value.
        /// </summary>
        /// <value>All options with non-default value.</value>
        Option[] OptionsSet();

        /// <summary>
        /// Gets the default option for read operations. 
        /// The returned option can be modified for particular operation, 
        /// but such modification does not modify the default value.
        /// </summary>
        /// <value>The default option for read operation.</value>
        ReadOptions DefaultReadOptions { get; }

        /// <summary>
        /// Gets the default option for write operation. 
        /// The returned option can be modified for particular operation, 
        /// but such modification does not modify the default value.
        /// </summary>
        /// <value>The default option for write operations.</value>
        WriteOptions DefaultWriteOptions { get; }

        /// <summary>
        /// Gets the default option for fecth operations. 
        /// The returned option can be modified for particular operation, 
        /// but such modification does not modify the default value.
        /// </summary>
        /// <value>The default option for fetch operations.</value>
        FetchOptions DefaultFetchOptions { get; }
    }

    /// <summary>
    /// Primary operational interface to NoSQL DataStore.
    /// </summary>
    /// <remarks>
    /// Supports CRUD operations. The operations use <see cref="IRow"/> as primary
    /// abstraction for data. The read operations return rows and write operations
    /// accept row as input argument.
    /// <para></para>
    /// Also supports 
    /// <list type="bullet">
    ///  <item><term>search
    /// <description>  records by partial key. Few variants exists on
    /// partial keys. A serach operation
    /// can also be invoked in asynchronous fashion.</description></term>
    /// </item>
    /// <item><term>SQL execution
    /// </term><description>for DDL statements such CREATE/DROP?ALTER tables</description>
    /// </item>
    /// </list>
    /// <para></para>
    /// The methods on this interafce mostly accepts <see cref="IRow"/>.
    /// <para></para>
    /// An instance of this interface is obtained <see cref="IKVDriver.GetStore()"/>.
    /// </remarks>
    public interface IKVStore : IObservable<IRow>, IDisposable {
        /// <summary>
        /// Gets the name of the connected data store .
        /// </summary>
        /// <value>The name of the data store. Never null.</value>
        string StoreName { get; }


        /// <summary>
        /// Creates an empty row in a given table.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <returns>The row created.</returns>
        IRow CreateRow(string tableName);

        /// <summary>
        /// Creates a row populated with given JSON formatted data.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="tableName">Name of a database table. The table
        /// name may be null or may not exist in database. In such case,
        /// the row can not be persisted.
        /// </param>
        /// <param name="jsonString">a JSON string to poplate the row.
        /// The string should represnt a single JSON object, not an 
        /// array.</param>
        /// <returns>The row created and populated with given JSON data.</returns>
        /// <exception cref="ArgumentException"> if given string in not in 
        /// JSON format.  </exception> 
        IRow CreateRow(string tableName, string jsonString);

        /// <summary>
        /// Creates a row populated with given dictionary.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="tableName">Name of a database table. The table
        /// name may be null or may not exist in database. In such case,
        /// the row can not be persisted.
        /// </param>
        /// <param name="dict">a dictionary of values to poplate the row.</param>
        /// <returns>The row created and populated with given values.</returns>
        /// <exception cref="ArgumentException"> if given values are not suported 
        /// </exception> 
        IRow CreateRow(string tableName, Dictionary<string, object> dict);

        /// <summary>
        /// Gets a row associated with specified primary key. The read operation
        /// uses default  option for read operations <see cref="IKVDriver.DefaultReadOptions"/>.
        /// </summary>
        /// 
        /// <param name="pk">a Primary Key. Must not be null and must have all
        /// primary key field(s) populated.</param>
        /// 
        /// <returns>The row assocaited with the primary key. Or null if no such 
        /// row exists</returns>
        /// 
        /// <exception cref="ArgumentException">If key is null or if the 
        /// primary key fields are not 
        /// populated</exception>
        IRow Get(IRow pk);

        /// <summary>
        /// Get the specified key with specified read options.
        /// </summary>
        /// 
        /// <param name="pk">a Primary Key. Must not be null and must have all
        /// primary key field(s) populated.</param>
        /// <param name="options">read options. If null, uses default option.</param>
        /// 
        /// <returns>The row associated with the primary key. Or null if no such 
        /// row exists</returns>
        /// 
        /// <exception cref="ArgumentException">If key is null or if the 
        /// primary key fields are not 
        /// populated</exception>
        IRow Get(IRow pk, ReadOptions options);

        /// <summary>
        /// Gets all rows associated with given key. The key may contain all or
        /// some of the fields of primary key. But it must include all shard keys
        /// defined for the key's table.
        /// </summary>
        /// 
        /// <param name="pk">a Primary Key. Must not be null and must have all
        /// shard key field(s) populated.</param>
        /// <param name="Options">options to fetch. If null, uses default option.</param>
        /// 
        /// <returns>All rows matching the specifed key. An empty list is returned
        /// if no matching row exists </returns>
        /// 
        List<IRow> GetAll(IRow pk, FetchOptions Options);


        /// <summary>
        /// Gets primary keys of all rows associated with given key. 
        /// The key may contain all or
        /// some of the fields of primary key. But it must include all shard keys
        /// defined for the key's table.
        /// </summary>
        /// 
        /// <param name="pk">a Primary Key. Must not be null and must have all
        /// shard key field(s) populated.</param>
        /// <param name="Options">options for multiple read operation. If null,
        /// uses default option.</param>
        /// 
        /// <returns>All primary key of rows matching the specifed key. 
        /// An empty list is returned
        /// if no matching row exists </returns>
        List<IRow> GetAllKeys(IRow pk, FetchOptions Options);


        /// <summary>
        /// Put the specified row. This is cover methood for Put(row, null, null)
        /// </summary>
        /// <param name="row">Row to be inserted.</param>
        IRow Put(IRow row);

        /// <summary>
        /// Put the specified row with given options.
        /// </summary>
        /// <param name="row">Row to be inserted. Must not be null
        /// and must have all its primary key fields populated.
        /// </param>
        /// <param name="options">write Options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>Row after it has been inserted</returns>
        IRow Put(IRow row, WriteOptions options);


        /// <summary>
        /// Put the specified row with given options only if no matching row
        /// exists. 
        /// </summary>
        /// 
        /// <param name="row">Row to be inserted. Must not be null and
        /// must have all its primary key fields populated.
        /// </param>
        /// <param name="options">write Options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>Row after it has been inserted</returns>
        IRow PutIfAbsent(IRow row, WriteOptions options);


        /// <summary>
        /// Put the specified row with given options only if a matching row
        /// exists.
        /// </summary>
        /// 
        /// <param name="row">Row to be inserted. Must not be null and must have
        /// all primary key fields populated.
        /// </param>
        /// <param name="options">write Options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>Row after it has been inserted</returns>
        IRow PutIfPresent(IRow row, WriteOptions options);


        /// <summary>
        /// Put the specified row with given options only if a row with 
        /// matching version exists.
        /// </summary>
        /// 
        /// <param name="row">Row to be inserted. Must not be null and must have
        /// all primary key fields populated.
        /// </param>
        /// <param name="version">A version to match. Must not be null.</param>
        /// <param name="options">options for write operation. If null,
        /// <see cref="IKVDriver.DefaultWriteOptions"/> default write options 
        /// are used </param>
        /// 
        /// <returns>Row after it has been inserted</returns>
        IRow PutIfVersion(IRow row, RowVersion version, WriteOptions options);

        /// <summary>
        /// Delete a row matching the specified key with specified write options.
        /// The key must have all its primary key fields populated.
        /// </summary>
        /// 
        /// <param name="pk">Primary Key whose corresponding row is to be deleted. 
        /// Must not be null and must have
        /// all primary key fields populated.
        /// </param>
        /// <param name="options">write Options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>The row that has been deleted or null if
        /// delete condition is not satisfied</returns>
        IRow Delete(IRow pk, WriteOptions options);

        /// <summary>
        /// Delete the the row matching the specified key 
        /// with specified write options if a row with
        /// matching version exists.
        /// </summary>
        /// 
        /// <param name="pk">The primary key for a row to be deleted. 
        /// Must not be null and must have
        /// all primary key fields populated.
        /// </param>
        /// <param name="v">A version to match. Must not be null.</param>
        /// <param name="options">write Options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>The row that has been deleted or null if
        /// delete condition is not satisfied</returns>
        IRow DeleteIfVersion(IRow pk, RowVersion v, WriteOptions options);


        /// <summary>
        /// Delete the rows matching the specified key with specified write options.
        /// The key may have none, all or some of its primary key fields populated.
        /// But all shard key fields must be populated.
        /// </summary>
        /// 
        /// <param name="pk">Primary Key whose corresponding row is to be deleted. 
        /// Must not be null and may have some or all
        ///  primary key fields populated.        
        /// But all shard key fields must be populated.
        /// </param>
        /// <param name="options">write Options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>The numer of rows deleted by this operation</returns>
        int DeleteAll(IRow pk, WriteOptions options);


        /// <summary>
        /// Gets a factory that creates data store operation for bacth execution.
        /// </summary>
        /// <value>The operation factory. Never null.</value>
        OperationFactory OperationFactory { get; }

        /// <summary>
        /// Executes the given operations in a transactional manner.
        /// The batch operation succeeds only if affeted row are in the same shard
        /// i.e. keys assocaited with the operations have same shard key. 
        /// 
        /// </summary>
        /// 
        /// <param name="ops">Operations to execute. Must not be null. The row(s)
        /// associated with the operation(s) must have the same shard key.</param>
        /// <param name="wOptions">write options. Can be null to indicate use of
        /// default.</param>
        /// 
        /// <returns>List of result for each operation. The order of result is
        /// same as the order of specified operations list.</returns>
        List<IRow> ExecuteUpdates(List<Operation> ops, WriteOptions wOptions);

        /// <summary>
        /// Executes a SQL statement. 
        /// </summary>
        /// 
        /// <param name="stmt">A SQL statement to execute. Must not be null.
        /// The statement must be a valid DDL statment in NoSQL Query Language. 
        /// </param>
        /// 
        /// <returns>true if SQL is executed.</returns>
        bool ExecuteSQL(string stmt);



        /// <summary>
        /// Searches for all rows matching the specified partial primary key.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// 
        /// <param name="pk">search key. The key may include none, some or all 
        /// shard keys.
        /// If the key has all the shard keys, the serach confines to a single
        /// target partition. If the key is partial i.e. have some shard key
        /// fields, then all partitions are searched. If the key has no field,
        /// then the entire table is matched.
        /// </param>
        /// <param name="o">options to control fetch behavior. 
        /// If null, uses default option.</param>
        /// 
        /// <returns>An iterator containing matching rows. Never null. 
        /// Can be empty.</returns>
        IEnumerable<IRow> Search(IRow pk, FetchOptions o);


        /// <summary>
        /// Searches asynchronously for all rows matching the specified 
        /// partial primary key
        /// and provides search results via callback to supplied observer.
        /// </summary>
        /// <remarks>
        /// The method is returns immediately without waiting for results. 
        /// The search results are received as callback argument to supplied
        /// <see cref="IObserver{IRow}"/>. 
        /// The results are delivered to the observer at a rate 
        /// controlled by the observer itself beacuse
        /// the next result is delivered only after the observer
        /// has returned true from <code>OnNext()</code> method.
        /// </remarks>
        /// 
        /// <param name="pk">search key. The key may include none, some or all 
        /// shard keys.
        /// If the key has all the shard keys, the serach confines to a single
        /// target partition. If the key is partial i.e. have some shard key
        /// fields, then all partitions are searched. If the key has no field,
        /// then the entire table is matched.
        /// </param>
        /// <param name="o">options to control fetch behavior. 
        /// If null, uses default option.</param>
        /// <param name="observer">receives search results via callback.
        /// must not be null. 
        /// </param>
        void SearchAsync(IRow pk, FetchOptions o, IObserver<IRow> observer);

        /// <summary>
        /// Searches for all rows matching the specified search keys. 
        /// </summary>
        /// 
        /// <param name="pk">serach keys. The keys must belong to same table
        /// and each key must have all shard key field(s) populated. 
        /// </param>
        /// <param name="o">options to control fetch behavior. 
        /// If null, uses default option.</param>
        /// 
        /// <returns>An iterator containing matching rows. Never null. Can be empty.</returns>
        IEnumerable<IRow> Search(List<IRow> pk, FetchOptions o);

        /// <summary>
        /// Searches asynchronously for all rows matching the specified search keys. 
        /// </summary>
        /// <param name="pks">list of search keys. The key may include none, some or all 
        /// shard keys.
        /// If the key has all the shard keys, the serach confines to a single
        /// target partition. If the key is partial i.e. have some shard key
        /// fields, then all partitions are searched. If the key has no field,
        /// then the entire table is matched.
        /// </param>
        /// <param name="o">options to control fetch behavior. 
        /// If null, uses default option.</param>
        /// <param name="observer">receives search results via callback.
        /// must not be null. 
        /// </param>
        void SearchAsync(List<IRow> pks, FetchOptions o, IObserver<IRow> observer);

        /// <summary>
        /// Search for all rows matching the specified index key.
        /// </summary>
        /// 
        /// <param name="ik">index key. The key must have some or all the index key fields
        /// populated. If the key has partial index fields, then the significance
        /// order of populated fields must be contiguous i.e. all fields higher than
        /// the least significant field present must be populated.</param>
        /// <param name="indexName">name of the index to search</param>
        /// <param name="o">options to control fetch behavior. 
        /// Can be null to indicate default.</param>
        /// 
        /// <returns>An iterator containing matching rows. Never null. Can be empty.</returns>
        IEnumerable<IRow> SearchByIndex(IRow ik, string indexName, FetchOptions o);

        /// <summary>
        /// Search asynchronously for all rows matching the specified index key.
        /// </summary>
        /// <param name="ik">index key. The key must have some or all the index key fields
        /// populated. If the key has partial index fields, then the significance
        /// order of populated fields must be contiguous i.e. all fields higher than
        /// the least significant field present must be populated.</param>
        /// <param name="indexName">name of the index to search</param>
        /// <param name="o">fetch options. If null, uses deafult option</param>
        /// <param name="observer">receives search results via callback.
        /// must not be null. </param>
        void SearchByIndexAsync(IRow ik, string indexName, FetchOptions o, IObserver<IRow> observer);


        /// <summary>
        /// Searches for all primary keys matching the specified search key. 
        /// </summary>
        /// 
        /// <param name="pk">search key. The search key have none, some or all shard keys.
        /// If the key contains all shrad keys, the serach is confined to a single
        /// partition. If the key is partial i.e. have some shard key
        /// fields, then all partitions are searched. If the key has no field,
        /// then the entire table is searched.
        /// </param>
        /// <param name="o">options to control fetch behavior. 
        /// Can be null to indicate default.</param>
        /// 
        /// <returns>An iterator containing primary keys of matching rows. 
        /// Never null. Can be empty.</returns>
        IEnumerable<IRow> SearchKeys(IRow pk, FetchOptions o);


        /// <summary>
        /// Search asynchronously for all rows matching the specified key.
        /// </summary>
        /// <param name="pk">search key. The key must have some or all the index key fields
        /// populated. If the key has partial index fields, then the significance
        /// order of populated fields must be contiguous i.e. all fields higher than
        /// the least significant field present must be populated.</param>
        /// <param name="o">fetch options. If null, uses deafult option</param>
        /// <param name="observer">receives search results via callback.
        /// must not be null. </param>
        void SearchKeysAsync(IRow pk, FetchOptions o, IObserver<IRow> observer);


        /// <summary>
        /// Search for all key pairs matching the specified search key.
        /// </summary>
        /// <param name="ik">index key. Must have some or all the index key fields
        /// populated. If the key has partial fields, then the significance
        /// orde of populated fields must be contiguous i.e. all fields higher than
        /// the least significant field must be populated.</param>
        /// <param name="o">options to control fetch behavior. 
        /// Can be null to indicate default.</param>
        /// 
        /// <returns>An iterator containing key pairs of matching rows. 
        /// Never null. Can be empty.</returns>
        IEnumerable<KeyPair> SearchKeyPairs(IRow ik, FetchOptions o);


        void SearchKeyPairsAsync(IRow ik, FetchOptions o, IObserver<KeyPair> subscriber);


    }

    /// <summary>
    /// Primary abstraction for datum. A row is input or output to most operations for
    /// <see cref="IKVStore"/>. 
    /// </summary>
    /// <remarks>
    /// A row is similar to tuple with named property (or dimension).  
    /// A property of a row is identifiable by name. 
    /// A property can hold value.
    /// A row belongs to a table. 
    /// A table may be defined in the datastore by SQL (CREATE TABLE ...),
    /// or may be a transient table that only exists in memory without 
    /// a corresponding database table.   
    /// <para></para>
    /// A row can be constructed by <see cref="IKVStore"/> without a table.
    /// A row without a table is schema-free in following sense:
    /// <list type="number">
    /// <item> 
    ///        <description>a property can have any name</description> 
    /// </item> 
    /// <item>
    ///       <description>a property can be set to any type of value.</description> 
    /// </item> 
    /// </list>
    /// Such a 'schema-free' row, however, can not 
    /// be persisted in NoSQL datastore.
    /// <para></para>  
    /// A persistable row is created with a table name.
    /// <para></para>
    /// The property name of a persistable row must be same as the corresponding
    /// column of the owning table. 
    /// The property value of a persistable row must be compatiable to the corresponding
    /// column type of the owning table. 
    /// <para></para>
    /// A row can be populated with JSON whose properties and values denote the names and 
    /// values of row fields. 
    /// <para></para>
    /// An row can also be populated per property using virtual array-like indexing.
    /// 
    ///
    /// <para></para>  
    /// Implementaion:
    /// A row wraps a Thrift data structure. The Thrift data structure is
    /// used to communicate with the Proxy Server. The underlying Thrift
    /// structure holds the data as a JSON formatted string. 
    /// </remarks>
    public interface IRow {
        /// <summary>
        /// Gets the table to which this row belongs.
        /// </summary>
        /// <remarks>
        /// The table may not correspond to any 
        /// database table. 
        /// However, such a row can not be persisted.
        /// </remarks>
        string TableName { get; }


        /// <summary>
        /// Gets all property names defined for this row.
        /// </summary>
        /// <remarks>
        /// an empty array if a row is schema free. 
        /// </remarks>
        /// <value>The defined property names.</value>
        IEnumerable<string> DefinedPropertyNames { get; }

        /// <summary>
        /// Gets the property names populated in this row.
        /// </summary>
        /// <value>The populted property names.</value>
        IEnumerable<string> PopulatedPropertyNames { get; }

        bool IsPopulatedProperty(string property);
        bool IsDefinedProperty(string property);
        /// <summary>
        /// Gets the version associated with this row.
        /// </summary>
        /// <value>The version.</value>
        RowVersion Version { get; }


        /// <summary>
        /// Gets the expiration time.
        /// </summary>
        /// <value>The expiration time is an UTC time that denotes when this 
        /// row will be auto-deleted in the storage.
        /// A special value of 0 implies that the row never expires.</value>
        long ExpirationTime { get; }

        /// <summary>
        /// Sets Time-To-Live on the this row.
        /// To create a row with a TTL, a positive non-zero TTL value 
        /// must be set before 
        /// <see cref="IKVStore.Put(IRow)"/> operation 
        /// and <see cref="WriteOptions.UpdateTTL"/>
        /// must be set to <code>true</code>.
        /// </summary>
        /// <value>A Time-To-Live for the given row.
        /// </value>
        TimeToLive TTL { set; }

        /// <summary>
        /// Gets previous state of this row. Can be null.
        /// </summary>
        /// <value>The previous state of this row.</value>
        IRow Previous { get; }


        /// <summary>
        /// Gets or sets the field identified by the given field path.
        /// The path can be a navigational path to refer value of
        /// a nested element.
        /// </summary>
        ///
        /// <param name="fieldName">A navigation field path.
        /// A path follows a simple grammar:
        /// <pre>
        ///  field_path   := path_segment [. path_segment]*
        ///  path_segment := simple_path | array_path
        ///  array_path   := simple_path '[' array_index ']'
        ///  simple_path  := [a-zA-Z]\\w*
        ///  array_index  := \\d+
        /// </pre>
        /// Examples of valid path are: 
        /// <pre>a, ab.cd, ab.c[3].d</pre>
        /// </param>
        /// <returns>The value of the naviageted element</returns>
        object this[string fieldName] { get; set; }

        string ToJSONString();


        /// <summary>
        /// Gets the field identified by the given field path
        /// and applies the given converter function on stored value.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="converter">Converter.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        T Get<T>(string fieldName, Func<object, T> converter);


    }

    /// <summary>
    /// A version information for each <see cref="IRow"/>.
    /// </summary>
    public interface RowVersion {
        /// <summary>
        /// Gets a string Base-64 encoded string representing a version.
        /// </summary>
        /// <value>a Base-64 encoded string.</value>
        string String { get; }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <value>The bytes representing a version.</value>
        byte[] Bytes { get; }

        /// <summary>
        /// Affirms if this version is empty
        /// </summary>
        bool IsEmpty { get; }

    }

}


/*! @} End of Doxygen Groups*/
