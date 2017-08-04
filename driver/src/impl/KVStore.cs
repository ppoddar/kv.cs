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


namespace oracle.kv.client {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using Thrift.Transport;
    using oracle.kv.proxy.gen;
    using oracle.kv.client.data;
    using oracle.kv.client.error;
    using oracle.kv.client.iterator;
    using oracle.kv.client.config;
    using oracle.kv.client.option;
    using oracle.kv.client.log;


    /// <summary>
    /// Represent a connection to Oracle NoSQL data store. This class is primary
    /// functional interface to data store operation. 
    /// </summary>
    /// <remarks>
    /// The connection to data store is designed to correspond driver in Java language.
    /// The semantics of the API is same as that of orcle.kv.KVStore API in Java.
    /// The syntax is also same as much as possible given differences across languages.
    /// <para></para>
    /// This connection delegates database operations to a remote Proxy. The proxy, 
    /// in turn, acts as a client to a NoSQL database server.
    /// <para></para> 
    /// The proxy is defined as a Thrift service. This connection interacts  
    /// with the proxy over network happens through Thrift protocol with data 
    /// being exchanged as Thrift structures . 
    /// <para></para>
    /// The user of the API, however, is neither aware of underlying Thrift protocol
    /// nor the Thrift data structures. 
    /// The API methods accepts and returns C# structures. This connection performs
    /// necessary conversion to and from Thrift structures. 
    /// <para></para>
    /// The main functional methods of this connection are 
    /// <list type="bullet">
    /// <item>
    ///     <term>Database operations</term>
    ///     <description>invoke data base operations such as 
    ///     <see cref="Get(IRow)"/>, <see cref="Put(IRow)"/>,
    ///     <see cref="Search(IRow, FetchOptions)"/>  
    ///      </description>
    /// </item>
    /// <item>
    ///     <term>Data conversion</term>
    ///     <description>bi-directional conversion of C# and Thrift data strcutures</description>
    /// </item>
    /// <item>
    ///     <term>Connection management</term>
    ///     <description>establish and maintain network connection to Proxy server.</description>
    /// </item>
    /// </list>
    /// <para></para>
    /// Some of the functions support asynchronous behavior. Such asychronous behavior
    /// is implemented by spawning another connection that is invoked on a separate thread
    /// by .NET  Task Scheduling framework.
    /// <para></para>  
    /// The search-like functions potentaiily return large number of records. 
    /// However, this connection does not cache. The proxy server retuns results 
    /// batches. This connection uses yield pattern to lazily evaluate long list
    /// of batches each of which can be long list of records.
    /// <para></para>
    /// The connections opens a socket to Proxy (Thrift) server. As Thrift uses 
    /// one socket per client thread, this connection being accessed by multitple
    /// thread requires to maintain per thread connection that reestablishes new Thrift
    /// connection per thread.
    /// <para></para>
    /// Error Handling:
    /// raises <see cref="ArgumentException"/>
    /// for application errors such as wrong input or database operation failure. 
    /// raises <see cref="InternalError"/> for implementation error.
    ///  
    ///
    /// </remarks>
    public class KVStore : IKVStore {
        private static ThreadLocal<ONDB.Iface> Proxies
                = new ThreadLocal<ONDB.Iface>();


        public ONDB.Iface proxy {
            get {
                return Proxies.IsValueCreated
                ? Proxies.Value
                : (Driver.GetStore() as KVStore).proxy;
            }
        }

        internal KVDriver Driver { get; set; }
        Logger Logger;


        public OperationFactory OperationFactory {
            get { return new OperationFactory(); }
        }

        public string StoreName {
            get {
                return Driver.dbUri.StoreName;
            }
        }

        public IEnumerable<ITable> Tables {
            get {
                return (Driver as KVDriver).DataModel.Tables;
            }
        }

        public ITable GetTable(string tableName) {
            return (Driver as KVDriver).DataModel.GetTable(tableName, false);
        }

        public ITable Table(IRow row) {
            return GetTable(row.TableName);
        }


        /// <summary>
        /// Initializes a new connection to data store.
        /// The connection to data store is established via a Proxy Service. 
        /// The proxy service
        /// runs as Java Virtual Machine in a separate process. This connection communcates
        /// with Proxy Service using Thrift protocol.
        /// This constructor will fail-fast if it can not connect to proxy service. 
        /// </summary>
        /// <param name="driver"> a driver</param>
        internal KVStore(KVDriver driver) {
            Driver = driver;
            Logger = LogManger.GetLogger(LogChannel.RUNTIME);
            Proxies.Value = driver.ProxyService.ConnectToThrift(); ;
        }

        /// <summary>
        /// Accepts the given subscriber/observer. The subscriber would
        /// be called back by the returned provider at its On*() methods.
        /// </summary>
        /// <param name="cb">Callback interface</param>
        public IDisposable Subscribe(IObserver<IRow> cb) {
            return Driver.GetStore() as IDisposable;
        }


        public IRow CreateRow(string tableName) {
            IDataModel model = (Driver as KVDriver).DataModel;
            var impl = new RowImpl(new TRow(), model.GetTable(tableName, true), model);

            return impl;
        }

        public IRow CreateRow(string tableName, string jsonString) {
            return (CreateRow(tableName) as RowImpl)
                    .PopulateWithJSON(jsonString);
        }

        public IRow CreateRow(string tableName, Dictionary<string, object> dict) {
            return (CreateRow(tableName) as RowImpl)
                .PopulateWithDict(dict);
        }


        /// <summary>
        /// Returns a row associated with the specified primary key.
        /// The read operation uses default read options.
        /// </summary>
        /// <param name="pk">Primary Key must be complete i.e. all fields
        /// defined as primary key fields of the table must be populated
        /// </param>
        public IRow Get(IRow pk) {
            return InvokeWithErrorHandler(this, "Primary Key:" + pk, () => {
                return Get(pk, null);
            });
        }

        T Thrift<T>(IRow row) {
            return (row as Thrifty<T>).Thrift;
        }




        /// <summary>
        /// Get the Row assocaied with the specified key, reading with
        /// specified option.
        /// </summary>
        /// <param name="pk">Primary Key must be complete i.e. all fields
        /// defined as primary key fields of the table must be populated
        /// </param>
        /// <param name="options">Read options. If null, default options are used
        /// </param>
        /// <exception cref="ArgumentException"> if key is null or does not belong
        /// to a table or is not complete.
        /// </exception>
        public IRow Get(IRow pk, ReadOptions options) {
            return InvokeWithErrorHandler(this, "Get(primary key:" + pk + ")",
            () => {
                Assert.NotNull(pk, "key is null");

                ReadOptions rOpt = options ?? Driver.DefaultReadOptions;
                TGetResult result = proxy.get(pk.TableName, Thrift<TRow>(pk), rOpt.Thrift);
                return Decorate(CreateRow(pk.TableName) as RowImpl, result);
            });
        }

        IDataModel DataModel {
            get {
                return (Driver as KVDriver).DataModel;
            }
        }

        /// <summary>
        /// Inserts the specified row in the datastore overwriting any data
        /// that may be present.
        /// Uses default write options and returns no previous state
        /// of the row.
        /// </summary>
        /// <param name="row">Row must not be null and must have all
        /// primary and shard key fields set.</param>
        public IRow Put(IRow row) {
            return InvokeWithErrorHandler(this, "Put:" + row, () => {
                return Put(row, null);
            });
        }

        /// <summary>
        ///  Puts a row into a table with given write options and choice to 
        /// return state of previous row.
        /// </summary>
        /// <param name="row">Row to be inserted. Must not be null, must belong
        /// to a table and must have all primary key fields set.
        /// If previous state does not exist or 
        /// return row choice specifes they should not be returned, then version
        /// on returned row is null and none of its field is accessible.
        /// If previous state exists, then expiration time is available 
        /// via <see cref="IRow.ExpirationTime"/> property.
        /// </param>
        /// <param name="options">Options write options. Can be null to apply
        /// default option.
        /// </param>
        /// 
        /// <returns>The row after it has been inserted/updated. The state
        /// of the row such as version, previous row etc,</returns>
		public IRow Put(IRow row, WriteOptions options) {
            return InvokeWithErrorHandler(this, "Put:" + row, () => {
                return _put(row, OperationType.PUT, options, null);
            });
        }

        /// <summary>
        ///  Puts a row into a table only if it does not exist, with given write 
        /// options and choice to 
        /// return state of previous row.
        /// </summary>
        /// <param name="row">Row to be inserted. Must not be null, must belong
        /// to a table and must have all primary key fields set.</param>
        /// <param name="options">Options write options can be null to use
        /// default.
        /// </param>
        /// <returns>The row after it has been inserted. The state
        /// of the row such as version, previous row etc,</returns>
        public IRow PutIfAbsent(IRow row, WriteOptions options) {
            return InvokeWithErrorHandler(this, "PutIfAbsent:" + row, () => {
                return _put(row, OperationType.PUT_IF_ABSENT, options, null);
            });
        }

        /// <summary>
        ///  Puts a row into a table only if it exists, with given write options 
        /// and choice to 
        /// return state of previous row.
        /// </summary>
        /// <param name="row">Row to be inserted. Must not be null, must belong
        /// to a table and must have all primary key fields set.
        ///  row to be populated with previous state of inserted row.
        /// It can be null to indicate to retun nothing. 
        /// If previous state does not exist or 
        /// return row choice specifes they should not be returned, then version
        /// on returned row is null and none of its field is accessible.
        /// If previous state exists, then expiration time is available 
        /// via <see cref="IRow.ExpirationTime"/> property.
        /// </param>/
        /// <param name="options">Options write options can be null to use
        /// default.
        /// </param>
        /// <returns>The row after it has been updated. The state
        /// of the row such as version, previous row etc,</returns>
        public IRow PutIfPresent(IRow row, WriteOptions options) {
            return InvokeWithErrorHandler(this, "PutIfPresent:" + row, () => {
                return _put(row, OperationType.PUT_IF_PRESENT, options, null);
            });
        }

        /// <summary>
        ///  Puts a row into a table only if given version matches existing one, 
        /// with given write options 
        /// and choice to 
        /// return state of previous row.
        /// </summary>
        /// <param name="row">Row to be inserted. Must not be null, must belong
        /// to a table and must have all primary key fields set.
        /// <param name="version">Version to match</param>
        /// row to be populated with previous state of inserted row.
        /// It can be null to indicate to retun nothing. 
        /// If previous state does not exist or 
        /// return row choice specifes they should not be returned, then version
        /// on returned row is null and none of its field is accessible.
        /// If previous state exists, then expiration time is available 
        /// via <see cref="IRow.ExpirationTime"/> property.        
        /// </param>/
        /// <param name="options">Options write options can be null to use
        /// default.
        /// </param>
        /// <param name="version">a version to compare with. the given row
        /// is updated if its version match the given version.</param>
        /// <returns>The row after it has been updated. The state
        /// of the row such as version, previous row etc,</returns>
        public IRow PutIfVersion(IRow row, RowVersion version, WriteOptions options) {
            return InvokeWithErrorHandler(this, "PutIfVersion:" + row, () => {
                Assert.NotNull(version, "can not compare with null version");
                return _put(row, OperationType.PUT_IF_VERSION,
                                options, version);
            });
        }

        /// <summary>
        /// General purpose, internal, common put operation.
        /// </summary>
        /// <param name="row">Row row to be put. Must not be null.</param>
        /// <param name="putOption">Put style.</param>
        /// <param name="options">Options write options.</param>
        /// <param name="version">Version can be null other than
        /// PUT_IF_VERSION style</param>
        private IRow _put(IRow row, OperationType putOption,
                                  WriteOptions options, RowVersion version) {

            Logger.Trace("" + putOption + " " + row);
            TWriteResult result = null;
            WriteOptions wOptions = options ??
                Driver.DefaultWriteOptions;
            TRow tRow = (row as RowImpl).Thrift;
            string tableName = row.TableName;
            switch (putOption) {

                case OperationType.PUT:
                    result = proxy.put(tableName, tRow, wOptions.Thrift);
                    break;
                case OperationType.PUT_IF_ABSENT:
                    result = proxy.putIfAbsent(tableName, tRow, wOptions.Thrift);
                    break;
                case OperationType.PUT_IF_PRESENT:
                    result = proxy.putIfPresent(tableName, tRow, wOptions.Thrift);
                    break;
                case OperationType.PUT_IF_VERSION:
                    result = proxy.putIfVersion(
                        tableName,
                        tRow,
                        version.Bytes,
                        wOptions.Thrift);
                    break;
            }

            return Decorate(row as RowImpl, result, wOptions.ReturnChoice);


        }

        public IRow Delete(IRow pk, WriteOptions wOptions) {
            return InvokeWithErrorHandler(this, "Delete:" + pk, () => {
                TWriteResult result = proxy.deleteRow(
                    pk.TableName,
                    Thrift<TRow>(pk),
                    wOptions.Thrift);
                if (result == null) {
                    return null;
                }
                return Decorate(CreateRow(pk.TableName) as RowImpl,
                    result, (wOptions ?? Driver.DefaultWriteOptions).ReturnChoice);
            });
        }

        public int DeleteAll(IRow pk, WriteOptions wOptions) {
            return InvokeWithErrorHandler(this, "DeleteAll:" + pk, () => {
                WriteOptions wo = wOptions ?? Driver.DefaultWriteOptions;
                FetchOptions fo = Driver.DefaultFetchOptions;

                int deleteCount = proxy.multiDelete(
                    pk.TableName,
                    Thrift<TRow>(pk),
                    fo.FieldRange.Thrift,
                    fo.IncludedTableNames,
                    wo.Thrift);

                return deleteCount;
            });
        }


        public IRow DeleteIfVersion(IRow pk, RowVersion version, WriteOptions wOptions) {
            return InvokeWithErrorHandler(this, "DeleteIfVersion:" + pk, () => {
                TWriteResult result = proxy.deleteRowIfVersion(
                    pk.TableName,
                    Thrift<TRow>(pk),
                    version.Bytes,
                    wOptions.Thrift);
                RowImpl row = CreateRow(pk.TableName) as RowImpl;
                return result == null ? null :
                    Decorate(row, result,
                    (wOptions ?? Driver.DefaultWriteOptions).ReturnChoice);
            });
        }


        public bool ExecuteSQL(string stmt) {
            return InvokeWithErrorHandler(this, "SQL=" + stmt, () => {
                Assert.NotNull(stmt, "Can not execute null SQL statement");
                TStatementResultV2 result = proxy.executeSyncV2(stmt);
                Assert.IsTrue(result != null && result.IsSuccessful,
                    "Failed to execute SQL statement[" + stmt + "] " +
                    (result == null ? "no error message available"
                                    : "due to " + result.ErrorMessage));

                proxy.refreshTables();
                RefreshSchema();

                return result.IsSuccessful;
            });
        }

        bool RefreshSchema() {
            return InvokeWithErrorHandler(this, "RefreshSchema()", () => {
                KVDriver driver = Driver as KVDriver;
                SchemaFactory.CreateSchema(this, driver.DataModel);
                return true;
            });
        }

        public List<IRow> GetAll(IRow pk, FetchOptions fOptions) {
            return InvokeWithErrorHandler(this, "GetAll", () => {
                Assert.NotNull(pk, "Primary key must not be null.");

                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                TMultiGetResult result = proxy.multiGet(
                    pk.TableName,
                    Thrift<TRow>(pk),
                    o.FieldRange.Thrift,
                    o.IncludedTableNames,
                    o.ReadOptions.Thrift);

                var rows = result.RowsWithMetadata.Select((mr) => {
                    RowImpl row = CreateRow(pk.TableName) as RowImpl;
                    row.PopulateWithJSON(mr.JsonRow);
                    row.ExpirationTime = mr.Expiration;
                    return row as IRow;
                }).ToList();

                return rows;
            });
        }

        public List<IRow> GetAllKeys(IRow pk, FetchOptions fOptions) {
            return InvokeWithErrorHandler(this, "GetAllKeys()", () => {
                Assert.NotNull(pk, "Primary key must not be null.");
                string TableName = pk.TableName;
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                TMultiGetResult result = proxy.multiGetKeys(
                    TableName,
                    Thrift<TRow>(pk),
                    o.FieldRange.Thrift,
                    o.IncludedTableNames,
                    o.ReadOptions.Thrift);

                var keys = result.RowsWithMetadata.Select((mr) => {
                    IRow key = CreateRow(TableName, mr.JsonRow);
                    return key;
                }).ToList();
                return keys;
            });
        }

        public IEnumerable<IRow> Search(IRow pk, FetchOptions fOptions) {
            Assert.NotNull(pk, "Primary key must not be null.");
            return InvokeWithErrorHandler(this, pk, () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                Task<TIteratorResult> proxyCall = Task<TIteratorResult>.Factory
                    .StartNew(() => {
                        return proxy.tableIterator(pk.TableName, Thrift<TRow>(pk),
                               o.FieldRange.Thrift, o.IncludedTableNames,
                               o.ReadOptions.Thrift, o.Direction.Thrift, o.BatchResultSize);
                    });
                ThriftResultTransformer<IRow> transfomer =
                        new ThriftResultTransformer<IRow>(
                                this.DataModel, GetTable(pk.TableName));
                return new SyncResultProvider<IRow>(this, proxyCall, transfomer);



            });
        }

        public void SearchAsync(IRow pk, FetchOptions fOptions, IObserver<IRow> subscriber) {
            SearchIAsync(pk, fOptions, subscriber);
        }

        public Task SearchIAsync(IRow pk, FetchOptions fOptions, IObserver<IRow> subscriber) {
            return InvokeWithErrorHandler(this, "SearchAsync", async () => {
                FetchOptions o = fOptions ?? (FetchOptions)Driver[Options.OPTIONS_FETCH_DEFAULT];

                var asyncConnection = Driver.GetStore() as KVStore;
                Task<TIteratorResult> proxyCall = Task<TIteratorResult>.Factory.StartNew(() => {
                    // the task is executed with another connection on a separte thread
                    return asyncConnection.proxy.tableIterator(
                       pk.TableName,
                       Thrift<TRow>(pk),
                       o.FieldRange.Thrift,
                       o.IncludedTableNames,
                       o.ReadOptions.Thrift,
                       o.Direction.Thrift,
                       o.BatchResultSize);
                });
                var transformer = new ThriftResultTransformer<IRow>(
                        this.DataModel, Table(pk));

                AsyncResultProvider<IRow> a = new AsyncResultProvider<IRow>(asyncConnection,
                   proxyCall, transformer);
                await a.SubscribeAsync(subscriber);
            });

        }


        public IEnumerable<IRow> SearchByIndex(IRow ik, string indexName, FetchOptions fOptions) {
            return InvokeWithErrorHandler(this, "SearchByIndex", () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                Task<TIteratorResult> result = Task<TIteratorResult>.Factory
                .StartNew(() => {
                    return proxy.indexIterator(
                        ik.TableName,
                        indexName,
                        Thrift<TRow>(ik),
                        o.FieldRange.Thrift,
                        o.IncludedTableNames,
                        o.ReadOptions.Thrift,
                        o.Direction.Thrift,
                        o.BatchResultSize);
                });
                var transfomer = new ThriftResultTransformer<IRow>(
                                        this.DataModel, GetTable(ik.TableName));
                return new SyncResultProvider<IRow>(this, result, transfomer);
            });
        }

        public void SearchByIndexAsync(IRow ik, string indexName,
            FetchOptions fOptions, IObserver<IRow> subscriber) {
            SearchByIIndexAsync(ik, indexName, fOptions, subscriber);
        }

        public Task SearchByIIndexAsync(IRow ik, string indexName,
                FetchOptions fOptions, IObserver<IRow> subscriber) {
            return InvokeWithErrorHandler(this, "SearchByIndexAsync",
                async () => {
                    FetchOptions o = fOptions ?? (FetchOptions)Driver[Options.OPTIONS_FETCH_DEFAULT];

                    var asyncConnection = Driver.GetStore() as KVStore;
                    Task<TIteratorResult> proxyCall =
                    Task<TIteratorResult>.Factory.StartNew(() => {
                        // the task is executed with another connection on a separte thread
                        return asyncConnection.proxy.indexIterator(
                                ik.TableName,
                                indexName,
                                Thrift<TRow>(ik),
                                o.FieldRange.Thrift,
                                o.IncludedTableNames,
                                o.ReadOptions.Thrift,
                                o.Direction.Thrift,
                                o.BatchResultSize);
                    });
                    var transfomer =
                        new ThriftResultTransformer<IRow>(DataModel, Table(ik));
                    AsyncResultProvider<IRow> a = new AsyncResultProvider<IRow>(
                        asyncConnection, proxyCall, transfomer);
                    await a.SubscribeAsync(subscriber);
                });
        }


        public IEnumerable<IRow> Search(List<IRow> pks, FetchOptions fOptions) {
            int hint = 0;
            return InvokeWithErrorHandler(this, "Search", () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                ITable table = Table(pks[0]);
                Task<TIteratorResult> result = Task<TIteratorResult>.Factory
                .StartNew(() => {
                    return proxy.tableIteratorMulti(
                    table.Name,
                    pks.Select(e => Thrift<TRow>(e)).ToList(),
                    o.FieldRange.Thrift,
                    o.IncludedTableNames,
                    o.ReadOptions.Thrift,
                    o.Direction.Thrift,
                    o.BatchResultSize,
                    hint);
                });

                var transfomer = new ThriftResultTransformer<IRow>(DataModel, table);
                return new SyncResultProvider<IRow>(this, result, transfomer);

            });

            return null;
        }

        public void SearchAsync(List<IRow> pks, FetchOptions fOptions, IObserver<IRow> subscriber) {
            SearchIAsync(pks, fOptions, subscriber);
        }


        public Task SearchIAsync(List<IRow> pks, FetchOptions fOptions,
                IObserver<IRow> subscriber) {

            int hint = 0;
            ITable table = Table(pks[0]);
            FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
            var asyncConnection = Driver.GetStore() as KVStore;
            return InvokeWithErrorHandler(this, "Search", async () => {
                Task<TIteratorResult> proxyCall =
                    Task<TIteratorResult>.Factory.StartNew(() => {
                        return asyncConnection.proxy.tableIteratorMulti(
                         table.Name,
                        pks.Select(e => Thrift<TRow>(e)).ToList(),
                         o.FieldRange.Thrift,
                         o.IncludedTableNames,
                         o.ReadOptions.Thrift,
                         o.Direction.Thrift,
                         o.BatchResultSize,
                         hint);
                    });

                var transfomer = new ThriftResultTransformer<IRow>(this.DataModel, table);
                AsyncResultProvider<IRow> a = new AsyncResultProvider<IRow>(asyncConnection, proxyCall, transfomer);
                await a.SubscribeAsync(subscriber);
            });
        }

        //        IEnumerable<KeyPair> SearchKeyPairs(IRow ik, FetchOptions o);
        public IEnumerable<KeyPair> SearchKeyPairs(IRow ik, FetchOptions fOptions) {
            return InvokeWithErrorHandler(this, "SerachKeys", () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                Task<TIteratorResult> result = Task<TIteratorResult>.Factory
                .StartNew(() => {
                    return proxy.tableKeyIterator(
                        ik.TableName,
                        Thrift<TRow>(ik),
                        o.FieldRange.Thrift,
                        o.IncludedTableNames,
                        o.ReadOptions.Thrift,
                        o.Direction.Thrift,
                        o.BatchResultSize);
                });

                var transfomer = new ThriftResultTransformer<KeyPair>(this.DataModel, Table(ik));
                return new SyncResultProvider<KeyPair>(this, result, transfomer);

            });

        }
        public void SearchKeyPairsAsync(IRow ik, FetchOptions fOptions, IObserver<KeyPair> subscriber) {
            SearchIKeyPairsAsync(ik, fOptions, subscriber);
        }

        public Task SearchIKeyPairsAsync(IRow ik, FetchOptions fOptions, IObserver<KeyPair> subscriber) {
            return InvokeWithErrorHandler(this, "SearchKeysAsync", async () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                var asynConnection = Driver.GetStore() as KVStore;
                Task<TIteratorResult> proxyCall =
                Task<TIteratorResult>.Factory.StartNew(() => {
                    return asynConnection.proxy.tableKeyIterator(
                        ik.TableName,
                        Thrift<TRow>(ik),
                        o.FieldRange.Thrift,
                        o.IncludedTableNames,
                        o.ReadOptions.Thrift,
                        o.Direction.Thrift,
                        o.BatchResultSize);
                });

                var transfomer = new ThriftResultTransformer<KeyPair>(this.DataModel, Table(ik));
                AsyncResultProvider<KeyPair> a =
                 new AsyncResultProvider<KeyPair>(asynConnection, proxyCall, transfomer);
                await a.SubscribeAsync(subscriber);
            });

        }


        public IEnumerable<IRow> SearchKeys(IRow pk, FetchOptions fOptions) {
            return InvokeWithErrorHandler(this, "SerachKeys", () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                Task<TIteratorResult> result = Task<TIteratorResult>.Factory
                  .StartNew(() => {
                      return proxy.tableKeyIterator(
                          pk.TableName,
                          Thrift<TRow>(pk),
                          o.FieldRange.Thrift,
                          o.IncludedTableNames,
                          o.ReadOptions.Thrift,
                          o.Direction.Thrift,
                          o.BatchResultSize);
                  });
                var transfomer =
                new ThriftResultTransformer<IRow>(this.DataModel, Table(pk));

                return new SyncResultProvider<IRow>(this, result, transfomer);
            });
        }

        public void SearchKeysAsync(IRow pk, FetchOptions fOptions, IObserver<IRow> subscriber) {
            SearchIKeysAsync(pk, fOptions, subscriber);
        }

        public Task SearchIKeysAsync(IRow pk, FetchOptions fOptions, IObserver<IRow> subscriber) {
            return InvokeWithErrorHandler(this, "SerachKeys", async () => {
                FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                var asynConnection = Driver.GetStore() as KVStore;
                Task<TIteratorResult> proxyCall =
                Task<TIteratorResult>.Factory.StartNew(() => {
                    return asynConnection.proxy.tableKeyIterator(
                  pk.TableName,
                        Thrift<TRow>(pk),
                  o.FieldRange.Thrift,
                  o.IncludedTableNames,
                  o.ReadOptions.Thrift,
                  o.Direction.Thrift,
                  o.BatchResultSize);
                });
                var transfomer = new ThriftResultTransformer<IRow>(
                    this.DataModel, Table(pk));
                AsyncResultProvider<IRow> a =
             new AsyncResultProvider<IRow>(
                    asynConnection, proxyCall, transfomer);
                await a.SubscribeAsync(subscriber);

            });
        }

        public void SearchKeysAsync(List<IRow> all,
            FetchOptions fOptions, IObserver<IRow> subscriber) {
            SearchIKeysAsync(all, fOptions, subscriber);
        }


        public Task SearchIKeysAsync(List<IRow> all,
            FetchOptions fOptions, IObserver<IRow> subscriber) {
            return InvokeWithErrorHandler(this, "SearchKeys", async () => {
                var asynConnection = Driver.GetStore() as KVStore;
                foreach (List<IRow> pks in all.GroupBy((pk) => pk.TableName)) {
                    FetchOptions o = fOptions ?? Driver.DefaultFetchOptions;
                    int hint = 0;
                    Task<TIteratorResult> proxyCall =
                    Task<TIteratorResult>.Factory.StartNew(() => {
                        return asynConnection.proxy.tableIteratorMulti(
                      pks[0].TableName,
                      pks.Select(e => Thrift<TRow>(e)).ToList(),
                      o.FieldRange.Thrift,
                      o.IncludedTableNames,
                      o.ReadOptions.Thrift,
                      o.Direction.Thrift,
                      o.BatchResultSize,
                      hint);
                    });
                    var transfomer = new ThriftResultTransformer<IRow>
                        (this.DataModel, Table(pks[0]));
                    AsyncResultProvider<IRow> a =
                    new AsyncResultProvider<IRow>(
                            asynConnection, proxyCall, transfomer);
                    await a.SubscribeAsync(subscriber);
                }

            });
        }


        IRow Decorate(RowImpl row, TGetResult result) {
            if (row == null) return row;
            row.PopulateWithJSON(result.CurrentRow.JsonRow);
            row.Version = new RowVersionImpl(result.CurrentRowVersion);
            row.ExpirationTime = result.Expiration;
            return row;
        }



        /// <summary>
        /// Decorates the with result.
        /// </summary>
        /// <returns>The with result.</returns>
        /// <param name="row">Row current row before decoration.</param>
        /// <param name="result">Result result from server.</param>
        /// <param name="choice">Choice how much to set in prvious row?.</param>
        IRow Decorate(RowImpl row, TWriteResult result, ReturnChoice choice) {
            if (result.__isset.currentRowVersion) {
                row.Version = new RowVersionImpl(result.CurrentRowVersion);
            }
            if (result.__isset.expiration) {
                row.ExpirationTime = result.Expiration;
            }
            RowImpl prev = new RowImpl(result.PreviousRow ?? new TRow(),
                                       row.TableName, DataModel);



            switch (choice) {
                case ReturnChoice.ALL:
                    prev.Version = new RowVersionImpl(result.PreviousRowVersion);
                    break;
                case ReturnChoice.VALUE:
                    break;
                case ReturnChoice.VERSION:
                    prev.Version = new RowVersionImpl(result.PreviousRowVersion);
                    break;
            }
            (row as RowImpl).Previous = prev;

            return row;

        }


        /**
         * Sets given return row from the given result based on given choice.
         */
        private void SetReturnRow(TWriteResult result, IRow row, ReturnChoice c) {
            if (c == ReturnChoice.NONE) return;
            RowImpl prev = new RowImpl(result.PreviousRow, Table(row), this.DataModel);
            switch (c) {
                case ReturnChoice.ALL:
                    prev.Version = new RowVersionImpl(result.PreviousRowVersion);
                    break;
                case ReturnChoice.VALUE:
                    break;
                case ReturnChoice.VERSION:
                    prev.Version = new RowVersionImpl(result.PreviousRowVersion);
                    break;
            }
            (row as RowImpl).Previous = prev;
        }


        public List<IRow> ExecuteUpdates(List<Operation> ops, WriteOptions wOptions) {
            return InvokeWithErrorHandler(this, ops, () => {
                WriteOptions o = wOptions ?? (WriteOptions)Driver[Options.OPTIONS_WRITE_DEFAULT];
                List<TWriteResult> results = proxy.executeUpdates(
                    ops.Select((e) => e.Thrift).ToList(), o.Thrift);
                List<IRow> local = new List<IRow>();
                for (int i = 0; i < results.Count; i++) {
                    local.Add(Decorate(ops[i].Row as RowImpl, results[i],
(wOptions ?? Driver.DefaultWriteOptions).ReturnChoice));
                }
                return local;
            });
        }






        private static bool StartWith(string stmt, string[] firstWords) {
            string verb = stmt.Split(null)[0];
            return Array.FindIndex(firstWords,
                s => s.Equals(verb, StringComparison.InvariantCultureIgnoreCase))
                >= 0;
        }


        /// <summary>
        /// This routine invokes given action in context of this instance to 
        /// catch all unhandled exceptions raised by the action.
        /// The exceptions raised by Proxy Service are translated
        /// if they indicte user error (e.g. DDL with bad syntax). Such exceptions
        /// are translated to platform <exception cref="ArgumentException"/>
        /// are raised again.
        /// Other exceptions are not handled and allowed to be processed by
        /// .NET platform's exception processing.
        /// </summary>
        /// <returns>whatever the action has to return</returns>
        /// <param name="ctx">a context descriptor, to be added to exception message
        ///  for diagnostic information. </param>
        /// <param name="action">action is an API method executed with coverage
        /// for exception handling.</param>
        /// <typeparam name="TResult">The type parameter indicates the type of
        /// return value of the action.</typeparam>
        F InvokeWithErrorHandler<F>(KVStore con, object ctx, Func<F> action) {
            using (KVStore store = con) {
                try {
                    return action.Invoke();
                } catch (Exception ex) {
                    Exception translated = ExceptionHandler.translate(ex, ctx);
                    if (translated != null) throw translated;
                }
                return default(F);
            }

        }

        IEnumerable<IRow> DatabaseQuery<IRow>(KVStore database, Func<IEnumerable<IRow>> action) {
            using (KVStore store = database) {
                try {
                    return action.Invoke();
                } catch (Exception ex) {
                    Exception translated = ExceptionHandler.translate(ex, "");
                    if (translated != null) throw translated;
                }
                return null as IEnumerable<IRow>;
            }
        }


        /// <summary>
        /// Releases all resource used by the <see cref="T:oracle.kv.client.KVStore"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:oracle.kv.client.KVStore"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:oracle.kv.client.KVStore"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:oracle.kv.client.KVStore"/> so the garbage collector can reclaim the memory that the
        /// <see cref="T:oracle.kv.client.KVStore"/> was occupying.</remarks>
        public void Dispose() {
            if (Proxies.IsValueCreated) {

            }
        }

        /// <summary>
        /// Gets the schema descriptor from database.
        /// </summary>
        /// <remarks>
        /// Uses reflection because underlying proxy service 
        /// may not provide a method to access schema descriptor.
        /// </remarks>
        /// <returns>The schema descriptor.</returns>
        public string GetSchemaDescriptor() {
            var SchemaIntrospectionMethod = typeof(ONDB.Iface)
                    .GetMethod("getSchema");
            return SchemaIntrospectionMethod == null ? null
                : (string)
                SchemaIntrospectionMethod.Invoke(proxy, null);
        }


    } // end of KVStore


    /// <summary>
    /// Represents version of a <see cref="IRow"/>. 
    /// Detail structure of a version is not avaialble,
    /// it is an opaque array of bytes.
    /// </summary>
    /// <remarks>
    /// The version represents a point in the serialized
    /// transaction schedule created by the master. 
    /// A node ensures that the commit identified by
    /// a version has been executed before allowing the transaction on
    /// the node to proceed.
    /// </remarks>
    public class RowVersionImpl : RowVersion {
        public byte[] Bytes { get; private set; }
        public string String { get; private set; }
        public bool IsEmpty { get { return Bytes.Length == 0; } }

        /// <summary>
        /// Initializes a new instance with (opaque) array of bytes.
        /// </summary>
        /// <param name="bytes">opaque array of Bytes. Can be null</param>
        public RowVersionImpl(byte[] bytes) {
            Bytes = bytes ?? new byte[0];
        }

        public override string ToString() {
            return Convert.ToBase64String(Bytes);
        }



        public bool Matches(RowVersion v1, RowVersion v2) {
            return v1.String.Equals(v2.String);

        }

    }

    /// <summary>
    /// An enumeration of allowed units of time for TTL parameter 
    /// </summary>
    public enum TimeUnit { DAYS, HOURS };



}
/*! @} End of Doxygen Groups*/

