/*-
 *
 *  This file is part of Oracle NoSQL Database
 *  Copyright (C) 2014, 2017 Oracle and/or its affiliates.  All rights reserved.
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


namespace java oracle.kv.proxy.gen
namespace py   oracle.kv.proxy.gen
namespace csharp   oracle.kv.proxy.gen


# Types and Data structures
typedef i64 TLong
typedef i32 TInt
typedef binary TVersion
typedef string JSON
typedef TLong TExpiration


/*
   Protocol version:
2 - The first protocol version number.
3 - KVProxy 4.1.0 now encodes differently the result for
 ONDB.indexKeyIterator(). Rows now contain primary and secondary fields with
  values separating primary key fields and secondary index fields.
    - new TimeToLive and Expiration support.
    - new service methods tableIteratorMulti and tableKeyIteratorMulti
4 - KVProxy 4.4 adds 2 new service methods:
    - getOptions
    - getTableInfo
*/
const TInt PROTOCOL_VERSION = 4;


enum TModuleInfo
{
    // 1 reserved for native client
    PROXY_SERVER = 2,
    JAVA_CLIENT  = 3
}

struct TVerifyProperties
{
    /**
     * Must match the store name of the Proxy Server
     **/
    1:string                kvStoreName;
    /**
     * List must have at least one entry and all entries
     * must be contained in the list that the server was started with.
     */
    2:list<string>          kvStoreHelperHosts;
    /**
     * The security username, required for secured stores.
     **/
    3:optional string       username;
    /**
     * The zones in which nodes must be located to be used for read operations.
     **/
    4:optional list<string> readZones;
    /**
     * The protocol version that the driver uses. This is a required field,
     * for all drivers v2+.
     **/
    5:optional TInt driverProtocolVersion;
}

enum TVerifyError
{
    INVALID_KVSTORE_Name               = 1,
    INVALID_KVSTORE_HelperHosts        = 2,
    Unsupported_Driver_ProtocolVersion = 3,
}

struct TVerifyResult
{
    1:bool          isConnected;
    2:TVerifyError  errorType;
    3:string        message;
    4:optional TInt proxyProtocolVersion = PROTOCOL_VERSION;
}

enum TSyncPolicy
{
    NO_SYNC       = 1,
    SYNC          = 2,
    WRITE_NO_SYNC = 3
}

enum TReplicaAckPolicy
{
    ALL             = 1,
    NONE            = 2,
    SIMPLE_MAJORITY = 3
}

struct TDurability
{
    1:TSyncPolicy       masterSync;
    2:TReplicaAckPolicy replicaAck;
    3:TSyncPolicy       replicaSync;
}

/**
 * Specifies whether to return the row value, version, both or neither.
 * For best performance, it is important to choose only the properties that
 * are required. The store is optimized to avoid I/O when the requested
 * properties are in cache.
 **/
enum TReturnChoice
{
    /** Return both the value and the version. **/
    ALL     = 1,
    /** Do not return the value or the version. **/
    NONE    = 2,
    /** Return only the value. **/
    ONLY_VALUE   = 3,
    /** Return only the version.**/
    ONLY_VERSION = 4
}

struct TWriteOptions
{
    1:TDurability   durability;
    2:TLong         timeoutMs;    // time in milliseconds
    3:TReturnChoice returnChoice;
    4:optional bool updateTTL = false;
}


enum TSimpleConsistency
{
    ABSOLUTE                = 1,
    NONE_REQUIRED           = 2,
    NONE_REQUIRED_NO_MASTER = 3
}

struct TTimeConsistency
{
    1:TLong permissibleLag;       // time in milliseconds
    2:TLong timeoutMs;            // time in milliseconds
}

struct TVersionConsistency
{
    1:TVersion version;
    2:TLong    timeoutMs;         // time in milliseconds
}

union TConsistency
{
    1:optional TSimpleConsistency  simple;
    2:optional TTimeConsistency    time;
    3:optional TVersionConsistency version;
}

struct TReadOptions
{
    1:TConsistency consistency;
    2:TLong        timeoutMs;     // time in milliseconds
}

enum TTimeUnit
{
    HOURS = 1;
    DAYS  = 2;
}

struct TTimeToLive
{
   1:optional TLong value;
   2:optional TTimeUnit timeUnit;
}

struct TRow
{
    1:JSON jsonRow;
    // This field is used only from driver to proxy. Never returned from proxy.
    2:optional TTimeToLive ttl;
}

struct TWriteResult
{
    // set by put
    1:TVersion currentRowVersion;
    // set by put and delete
    2:TRow     previousRow;
    // set by put and delete
    3:TVersion previousRowVersion;
    // set by delete and deleteIfVersion methods, also used as success flag
    // for executeUpdates method
    4:bool     wasDeleted;
    // set by put* methods if a Version is returned
    5:optional TExpiration expiration = 0;
}

struct TGetResult
{
    1:TRow     currentRow;
    2:TVersion currentRowVersion;
    3:optional TExpiration expiration = 0;
}

struct TFieldRange
{
    1:string        fieldName;
    2:optional JSON startValue;
    3:bool          startIsInclusive;
    4:optional JSON endValue;
    5:bool          endIsInclusive;
}

struct TRowAndMetadata
{
    1:JSON     jsonRow;
    2:TVersion rowVersion;
    3:TInt     tableId;
    4:optional TExpiration expiration = 0;
}

struct TMultiGetResult
{
    1:map<TInt, string>     idToTableNames;
    2:list<TRowAndMetadata> rowsWithMetadata;
}

struct TIteratorResult
{
    1:TLong           iteratorId; // Iterator id
    2:TMultiGetResult result;     // a list of rows with table name metadata
    3:bool            hasMore;    // true if iterator has more results left
                                  // if this is false, the iterator is closed
}

enum TDirection
{
    FORWARD   = 1,             // Iterate in ascending key order.
    REVERSE   = 2,             // Iterate in descending key order.
    UNORDERED = 3              // Iterate in no particular key order.
}

/**
* A StatementResult provides information about the execution and outcome of an
* asynchronously executed table statement. If obtained via
* executionFutureGetStatus(), it can represent information about either a
* completed or in progress operation. If obtained via executionFutureGet(),
* it represents the final status of a finished operation.
**/
struct TStatementResult
{
    /**
     * The administrative plan id for this operation if the operation
     * was a create or remove table, a create or remove index,
     * or an alter index.
     *
     * The plan id will be 0 if this statement was not an administrative
     * operation, or did not require execution.
     */
    1:TInt planId;
    /**
     * detailed information about the status of the command execution in human
     * readable form.
     **/
    2:string info;
    /**
     * detailed information about the status of the statement execution, in
     * JSON text.
     **/
    3:JSON infoAsJson;
    /**
     * true if this statement has finished and was successful.
     **/
    4:bool isSuccessful;
    /**
     * If the operation failed, and isSuccessful is false, errorMsg will return
     * a description of the problem.
     **/
    5:string errorMessage;
    /**
     * true if the operation was cancelled. Not set by executeSync.
     **/
    6:bool isCancelled;
    /**
     * true if the operation has been terminated. Always true for executeSync.
     **/
    7:bool isDone;
}

/**
 * TResult is a union of possible result types returned via a StatementResultV2.
 * It is expected that as new result types are created this union will be
 * expanded.
 */
union TResult
{
    /**
     * A single text string result.
     */
    1:optional string stringResult;
}

/**
* A StatementResultV2 provides information about the execution and outcome
* of an asynchronously executed table statement. If obtained via
* executionFutureGetStatusV2(), it can represent information about either a
* completed or in progress operation. If obtained via executionFutureGetV2(),
* it represents the final status of a finished operation.
**/
struct TStatementResultV2
{
    /**
     * The identification of the execution. This will be used with the
     * following methods: executionFutureCancelV2, executionFutureGetV2,
     * executionFutureGetTimeoutV2 and executionFutureUpdateStatusV2.
     *
     * It is null when is the result of executeSyncV2().
     */
    1:binary executionId;
    /**
     * The administrative plan id for this operation if the operation
     * was a create or remove table, a create or remove index,
     * or an alter index.
     *
     * The plan id will be 0 if this statement was not an administrative
     * operation, or did not require execution.
     */
    2:TInt planId;
    /**
     * detailed information about the status of the command execution in human
     * readable form.
     **/
    3:string info;
    /**
     * detailed information about the status of the statement execution, in
     * JSON text.
     **/
    4:JSON infoAsJson;
    /**
     * true if this statement has finished and was successful.
     **/
    5:bool isSuccessful;
    /**
     * If the operation failed, and isSuccessful is false, errorMsg will return
     * a description of the problem.
     **/
    6:string errorMessage;
    /**
     * true if the operation was cancelled. Not set by executeSync.
     **/
    7:bool isCancelled;
    /**
     * true if the operation has been terminated. Always true for executeSync.
     **/
    8:bool isDone;
    /**
     * the statement of this execution
     **/
    9:string statement;
    /**
     * a string result if the statement has a text result value and was
     * successful.
     **/
    10:optional TResult result;
}

/**
* The types of TOperation
**/
enum TOperationType
{
    DELETE = 1,
    DELETE_IF_VERSION = 2,
    PUT = 3,
    PUT_IF_ABSENT = 4,
    PUT_IF_PRESENT = 5,
    PUT_IF_VERSION = 6
}

/**
 * Defines an update operation to be passed to executeUpdates.
 **/
struct TOperation
{
    /**
     * The table name on which this operation is executed on.
     **/
    1:string tableName;
    /**
     * Determines which update operation to be executed.
     **/
    2:TOperationType type;
    /**
     * For put operations: represents the row to be stored.
     * For delete operations: represents the key of the row to be deleted.
     **/
    3:TRow row;
    /**
     * Specifies whether to return the row value, version, both or neither.
     **/
    4:TReturnChoice returnChoice;
    /**
     * true if this operation should cause the execute transaction to abort
     * when the operation fails, where failure is the condition when the
     * delete or put method returns null.
     **/
    5:bool abortIfUnsuccessful;
    /**
     * The version to be matched for: putIfVersion and deleteIfVersion.
     **/
    6:optional TVersion matchVersion;
}

# Exceptions
exception TDurabilityException
{
    1:list<string>      availableReplicas;
    2:TReplicaAckPolicy commitPolicy;
    3:TInt              requiredNodeCount;
    4:string            message;
}

exception TRequestTimeoutException
{
    1:string message;
    2:TLong  timeoutMs;       // time in milliseconds
}

exception TFaultException
{
    1:string faultClassName;
    2:string remoteStackTrace;
    3:bool   wasLoggedRemotely;
    4:string message;
}

exception TConsistencyException
{
    1:TConsistency consistencyPolicy;
    2:string       message;
}

exception TIllegalArgumentException
{
    1:string message;
}

exception TIteratorTimeoutException
{
    1:string message;
}

exception TUnverifiedConnectionException
{
    1:string message;
}

exception TProxyException
{
    1:string message;
}

exception TCancellationException
{
    1:string message;
}

exception TExecutionException
{
    1:string message;
}

exception TInterruptedException
{
    1:string message;
}

exception TTimeoutException
{
    1:string message;
}

/**
 * Used to indicate a failure in executeUpdates.
 **/
exception TTableOpExecutionException
{
    /** The operation that caused the execution to be aborted. **/
    1:TOperation   operation;
    /** The list index of the operation that caused the execution to be aborted. **/
    2:TInt         failedOperationIndex;
    /** The result of the operation that caused the execution to be aborted. **/
    3:TWriteResult operationResult;
    /** The exception message */
    4:string       message;
}

/**
 * Thrown when a request cannot be processed because it would exceed the
 * maximum number of active requests for a node as configured by
 * -request-limit.
 **/
exception TRequestLimitException
{
    1:string message;
}

/**
 * This exception is thrown if an application passes invalid credentials to
 * an authentication operation.
 **/
exception TAuthenticationFailureException
{
    1:string message;
}

/**
 * This exception is thrown when a secured operation is attempted and the
 * client is not currently authenticated. It can occur if login credentials
 * were specified, but the login session has expired, requiring that the client
 * reauthenticate itself.
 **/
exception TAuthenticationRequiredException
{
    1:string message;
}

/**
 * This exception is thrown from methods where an authenticated user is
 * attempting to perform an operation for which they are not authorized.
 * An application that receives this exception typically should not retry the
 * operation.
 **/
exception TUnauthorizedException
{
    1:string message;
}

# Service

/**
 * KVProxy interface equivalent to kvclient TableAPI.
 **/
service ONDB {

    /* Status info methods */
    /** For checking that the KVProxy server is reachable. **/
    void ping();
    /** Returns the version string of each module. **/
    string version(1:TModuleInfo whichModule);
    /** For checking the status of all the modules. **/
    string status(1:TModuleInfo whichModule);
    /** Shuts down the proxy server if it's allowed **/
    oneway void shutdown();

    /** Verifies the connection properties: kvstore, credentials, etc.
     * The kvStoreHelperHosts list must have at least one entry and all entries
     * must be contained in the list that the server was started with.
     */
    TVerifyResult verify(1:TVerifyProperties properties)
        throws (1:TUnverifiedConnectionException uve);


    // Simple operations
    TWriteResult put(1:string tableName, 2:TRow row,
    3:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
               2:TRequestTimeoutException re,
               3:TFaultException fe,
               4:TProxyException pe,
               5:TIllegalArgumentException iae);

    TWriteResult putIfAbsent(1:string tableName, 2:TRow row,
        3:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
               2:TRequestTimeoutException re,
               3:TFaultException fe,
               4:TProxyException pe,
               5:TIllegalArgumentException iae);

    TWriteResult putIfPresent(1:string tableName, 2:TRow row,
        3:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
               2:TRequestTimeoutException re,
               3:TFaultException fe,
               4:TProxyException pe,
               5:TIllegalArgumentException iae);

    TWriteResult putIfVersion(1:string tableName, 2:TRow row,
    3:TVersion matchVersion,
        4:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
               2:TRequestTimeoutException re,
               3:TFaultException fe,
               4:TProxyException pe,
               5:TIllegalArgumentException iae);

    TGetResult get(1:string tableName, 2:TRow key, 3:TReadOptions readOptions)
        throws (1:TConsistencyException ce,
               2:TRequestTimeoutException re,
               3:TFaultException fe,
               4:TProxyException pe,
               5:TIllegalArgumentException iae);

    TWriteResult deleteRow(1:string tableName, 2:TRow key,
        3:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae);

    TWriteResult deleteRowIfVersion(1:string tableName, 2:TRow key,
        3:TVersion matchVersion, 4:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae);

    TInt multiDelete(1:string tableName, 2:TRow key,
        3:TFieldRange fieldRange, 4:list<string> includedTable,
        5:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae);

    TMultiGetResult multiGet(1:string tableName, 2:TRow key,
        3:TFieldRange fieldRange, 4:list<string> includedTables,
        5:TReadOptions readOptions)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae);

    TMultiGetResult multiGetKeys(1:string tableName, 2:TRow key,
         3:TFieldRange fieldRange, 4:list<string> includedTables,
         5:TReadOptions readOptions)
         throws (1:TConsistencyException de,
                 2:TRequestTimeoutException re,
                 3:TFaultException fe,
                 4:TProxyException pe,
                 5:TIllegalArgumentException iae);


    // Iterators

    /**
     * @param maxResults Represents the maximum expected number of rows in
     * the result. The number of rows can be smaller than MaxResults but not
     * bigger. If maxResults is less than 1, the default value or
     * -max-iterator-results configured value is used.
     **/
    TIteratorResult tableIterator(1:string tableName, 2:TRow key,
        3:TFieldRange fieldRange, 4:list<string> includedTables,
        5:TReadOptions readOptions, 6:TDirection direction,
        7:TLong maxResults)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException ite);

    /**
     * Returns an iterator over the rows matching the primary keys supplied
     * (or the rows in ancestor or descendant tables, or those in a
     * range specified by the MultiRowOptions argument).
     *
     * @param maxResults Represents the maximum expected number of rows in
     * the result. The number of rows can be smaller than MaxResults but not
     * bigger. If maxResults is less than 1, the default value or
     * -max-iterator-results configured value is used.
     *
     * @param numParallelIterHint this is a hint that might be used for
     * optimizing performance.
     *
     * @since version 3
     **/
    TIteratorResult tableIteratorMulti(1:string tableName,
        2:list<TRow> keys, 3:TFieldRange fieldRange,
        4:list<string> includedTables, 5:TReadOptions readOptions,
        6:TDirection direction, 7:TLong maxResults,
        8:TInt numParallelIterHint)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException ite);

    /**
     * The number of rows in result is linked to the maxResult when the
     * iterator was created.
     **/
    TIteratorResult iteratorNext(1:TLong iteratorId)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException ite);

    /**
     * There is no need to close an iterator if the last TIteratorResult
     * returned contained hasMore == false.
     **/
    void iteratorClose(1:TLong iteratorId);


    /**
     * @param maxResults Represents the maximum expected number of rows in
     * the result. The number of rows can be smaller than MaxResults but not
     * bigger. If maxResults is less than 1, the default value or
     * -max-iterator-results configured value.
     **/
    TIteratorResult tableKeyIterator(1:string tableName, 2:TRow key,
        3:TFieldRange fieldRange, 4:list<string> includedTables,
        5:TReadOptions readOptions, 6:TDirection direction,
        7:TLong maxResults)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException tie);

    /**
     * Returns an iterator over the keys matching the primary keys supplied by
     * iterator (or the rows in ancestor or descendant tables, or those in a
     * range specified by the MultiRowOptions argument).
     *
     * @param maxResults Represents the maximum expected number of rows in
     * the result. The number of rows can be smaller than MaxResults but not
     * bigger. If maxResults is less than 1, the default value or
     * -max-iterator-results configured value.
     *
     * @param numParallelIterHint this is a hint that might be used for
     * optimizing performance.
     *
     * @since version 3
     **/
    TIteratorResult tableKeyIteratorMulti(1:string tableName,
        2:list<TRow> keys, 3:TFieldRange fieldRange,
        4:list<string> includedTables, 5:TReadOptions readOptions,
        6:TDirection direction, 7:TLong maxResults, 8:TInt numParallelIterHint)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException tie);

    /**
     * @param maxResults Represents the maximum expected number of rows in
     * the result. The number of rows can be smaller than MaxResults but not
     * bigger. If maxResults is less than 1, the default value or
     * -max-iterator-results configured value.
     **/
    TIteratorResult indexIterator(1:string tableName, 2:string indexName,
        3:TRow key, 4:TFieldRange fieldRange, 5:list<string> includedTables,
        6:TReadOptions readOptions, 7:TDirection direction,
        8:TLong maxResults)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException ite);

    /**
     * indexKeyIterator returns column values that are part of both the
     * primary key and the index key.
     * @param maxResults Represents the maximum expected number of rows in
     * the result. The number of rows can be smaller than MaxResults but not
     * bigger. If maxResults is less than 1, the default value is used.
     **/
    TIteratorResult indexKeyIterator(1:string tableName, 2:string indexName,
        3:TRow key, 4:TFieldRange fieldRange, 5:list<string> includedTables,
        6:TReadOptions readOptions, 7:TDirection direction,
        8:TLong maxResults)
        throws (1:TConsistencyException de,
                2:TRequestTimeoutException re,
                3:TFaultException fe,
                4:TProxyException pe,
                5:TIllegalArgumentException iae,
                6:TIteratorTimeoutException ite);

    /**
     * Refreshes cached information about the tables. This method is
     * required before using any tables that had been modified.
     **/
    void refreshTables()
        throws (1:TFaultException fe);


    /**
     * Synchronously execute a table statement. The method will only return
     * when the statement has finished. Has the same semantics as
     * execute(String), but offers synchronous behaviour as a convenience.
     *
     * @Deprecated This method is replaced by #executeSyncV2 in
     * protocol version 2.
     **/
    TStatementResult executeSync(1:string statement)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException ise,
                3:TProxyException pe);


    /**
     * Asynchronously executes a table statement. Currently, table statements
     * can be used to create or modify tables and indices. The operation is
     * asynchronous and may not be finished when the method returns.
     *
     * A ExecutionFuture identifier (planId) is returned and can be used to get
     * information about the status of the operation, or to await completion
     * of the operation.
     *
     * If the statement is for an administrative command, and the store is
     * currently executing an administrative command that is the logical
     * equivalent the action specified by the statement, the method will
     * return a ExecutionFuture identifier that serves as a handle to that
     * operation, rather than starting a new invocation of the command.
     * The caller can use the ExecutionFuture identifier to wait for the
     * completion of the administrative operation.
     *
     * @Deprecated This method is replaced by #executeV2 in
     * protocol version 2.
     **/
    TStatementResult execute(1:string statement)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TProxyException pe);

    /**
     * Attempts to cancel execution of this statement. Return false if the
     * statement couldn't be cancelled, possibly because it has already
     * finished. If the statement hasn't succeeded already, and can be stopped,
     * the operation will transition to the FAILED state.
     *
     * @Deprecated This method is replaced by #executionFutureCancelV2 in
     * protocol version 2.
     **/
    bool executionFutureCancel(1:TInt planId,
                               2:bool mayInterruptIfRunning)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TProxyException pe);

    /**
     * Blocks until the command represented by this future completes. Returns
     * information about the execution of the statement. This call will result
     * in communication with the kvstore server.
     *
     * @Deprecated This method is replaced by #executionFutureGetV2 in
     * protocol version 2.
     **/
    TStatementResult executionFutureGet(1:TInt planId)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TCancellationException ce,
                4:TExecutionException ee,
                5:TInterruptedException ie,
                6:TProxyException pe);

    /**
     * Blocks until the administrative operation has finished or the timeout
     * period is exceeded. This call will result in communication with the
     * kvstore server.
     *
     * @Deprecated This method is replaced by #executionFutureTimeoutV2 in
     * protocol version 2.
     **/
    TStatementResult executionFutureGetTimeout(1:TInt planId,
        2:TLong timeoutMs)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TInterruptedException ie,
                4:TTimeoutException te,
                5:TExecutionException ee,
                6:TProxyException pe);

    /**
     * Returns information about the execution of the statement. If the
     * statement is still executing, this call will result in communication
     * with the kvstore server to obtain up to date status, and the status
     * returned will reflect interim information.
     *
     * @Deprecated This method is replaced by #executionFutureUpdateStatusV2
     *  in protocol version 2.
     **/
    TStatementResult executionFutureUpdateStatus(1:TInt planId)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TProxyException pe);


    /**
     * Synchronously execute a table statement. The method will only return
     * when the statement has finished. Has the same semantics as
     * executeV2(String), but offers synchronous behaviour as a convenience.
     **/
    TStatementResultV2 executeSyncV2(1:string statement)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException ise,
                3:TProxyException pe);


    /**
     * Asynchronously executes a table statement. Currently, table statements
     * can be used to create or modify tables and indices. The operation is
     * asynchronous and may not be finished when the method returns.
     *
     * A ExecutionFuture identifier is returned and can be later used to get
     * information about the status of the operation, cancel or to await
     * completion of the operation.
     *
     * If the statement is for an administrative command, and the store is
     * currently executing an administrative command that is the logical
     * equivalent the action specified by the statement, the method will
     * return a ExecutionFuture identifier that serves as a handle to that
     * operation, rather than starting a new invocation of the command.
     * The caller can use the ExecutionFuture identifier to wait for the
     * completion of the administrative operation.
     **/
    TStatementResultV2 executeV2(1:string statement)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TProxyException pe);

    /**
     * Attempts to cancel execution of this statement. Return false if the
     * statement couldn't be cancelled, possibly because it has already
     * finished. If the statement hasn't succeeded already, and can be stopped,
     * the operation will transition to the FAILED state.
     **/
    bool executionFutureCancelV2(1:binary executionId,
                                 2:bool mayInterruptIfRunning)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TProxyException pe);

    /**
     * Blocks until the command represented by this future completes. Returns
     * information about the execution of the statement. This call will result
     * in communication with the kvstore server.
     **/
    TStatementResultV2 executionFutureGetV2(1:binary executionId)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TCancellationException ce,
                4:TExecutionException ee,
                5:TInterruptedException ie,
                6:TProxyException pe);

    /**
     * Blocks until the administrative operation has finished or the timeout
     * period is exceeded. This call will result in communication with the
     * kvstore server.
     **/
    TStatementResultV2 executionFutureGetTimeoutV2(1:binary executionId,
        2:TLong timeoutMs)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TInterruptedException ie,
                4:TTimeoutException te,
                5:TExecutionException ee,
                6:TProxyException pe);

    /**
     * Returns information about the execution of the statement. If the
     * statement is still executing, this call will result in communication
     * with the kvstore server to obtain up to date status, and the status
     * returned will reflect interim information.
     **/
    TStatementResultV2 executionFutureUpdateStatusV2(1:binary executionId)
        throws (1:TFaultException fe,
                2:TIllegalArgumentException iae,
                3:TProxyException pe);


    /**
     * This method provides an efficient and transactional mechanism for
     * executing a sequence of operations associated with tables that share
     * the same shard key portion of their primary keys.
     **/
    list<TWriteResult> executeUpdates(1:list<TOperation> operations,
        2:TWriteOptions writeOptions)
        throws (1:TDurabilityException de,
                2:TTableOpExecutionException toee,
                3:TFaultException fe,
                4:TIllegalArgumentException iae,
                5:TProxyException pe);
                
                
     /**
      * gets all non-null option values indexed by option names.
      */
      map<string,string> getOptions();          
                
}
