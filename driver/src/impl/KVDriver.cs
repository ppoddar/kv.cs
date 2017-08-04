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
    using System.IO;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using System.Web;
    using System.Linq;
    using oracle.kv.client.data;
    using oracle.kv.client.option;
    using oracle.kv.client.config;
    using oracle.kv.client.error;
    using oracle.kv.client.log;
    using oracle.kv.proxy.gen;

    /// <summary>
    /// A  driver for Oracle NOSQL database.
    /// A driver acts as a factory for database connection 
    /// which is the funtional interface <see cref="IKVStore"/> 
    /// for database operations .
    /// </summary>
    /// <remarks>
    /// A driver establishes connection to database via a
    /// proxy.
    /// <para></para>
    /// A driver is created by static <code>Create()</code>
    /// methods of <see cref="KVStore"/>.
    /// These <code>Create()</code> methods accept a URI
    /// to identify the database and set of options. 
    /// The options specify the proxy as well as database
    /// connction parameters.
    /// The list of supported options are avaialble in
    /// <see cref="IKVDriver.OptionsSupported"/> 
    /// <para></para> 
    /// The connections created by the same driver has
    /// same properties. Hence, once a connection has
    /// been created by a driver, its configuration is frozen. 
    /// </remarks>
    public class KVDriver : IKVDriver {
        internal DatabaseUri dbUri;
        internal ProxyService ProxyService;
        internal IDataModel DataModel { get; private set; }
        internal Configuration Config;
        internal string version = "";
        /// <summary>
        /// The driver protocol version is on-the-wire data   
        /// protocol between driver and proxy.
        /// </summary>
        public static readonly int PROTOCOL_VERSION = 4;

        Logger Logger;

        /// <summary>
        /// internal constructor. called by all static Create() methods
        /// </summary>
        /// <param name="uri">URI of the database. Must not be null</param>
        /// <param name="config">Configuered options. Must not be null</param>
        private KVDriver(DatabaseUri uri, Configuration config) {
            Assert.NotNull(uri, "cannot construct with null URI");
            Assert.NotNull(uri, "cannot construct with null configuration");

            dbUri = uri;
            Config = config;

            //Config[Options.STORE_HOSTPORT] = dbUri.HostPort;
            //Config[Options.STORE_NAME] = dbUri.StoreName;

            version = FileVersionInfo
                .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                .ProductVersion;

            LogManger.Configure((string)Config[Options.LOG_SPEC]);
            Logger = LogManger.GetLogger(LogChannel.RUNTIME);

            ProxyService = ProxyService.Create(this);


            Config.IsReadOnly = true;

            ONDB.Client client0 = ProxyService.ConnectToThrift();
            VerifyOptionsMatch(client0, dbUri);
            InitalizeDataModel();
        }



        /// <summary>
        /// Creates a driver with given URI string
        /// and default options.
        /// </summary>
        /// <returns>The created driver.</returns>
        public static KVDriver Create(string uri) {
            return Create(uri, Configuration.Default);
        }


        /// <summary>
        /// Creates a driver the default URI and given options.
        /// </summary>
        /// <returns>The created driver.</returns>
        /// <param name="dict">Dictionary of options. All known options 
        /// are available from <see cref="Options.All"/>
        /// </param>
        public static KVDriver Create(Dictionary<Option, object> dict) {
            return Create(DatabaseUri.DEFAULT, dict);
        }



        /// <summary>
        /// Creates a driver the given URI and given options.
        /// </summary>
        /// <returns>The created driver.</returns>
        /// <param name="uriString">URI for database. 
        /// Must be in <see cref="DatabaseUri"/> format,
        /// for example, <code>nosql://myhost.com:5674/myStore</code>
        /// </param>
        /// <param name="dict">Dictionary of options. All known options 
        /// are available from <see cref="Options.All"/>
        /// </param>
        public static KVDriver Create(string uriString, Dictionary<Option, object> dict) {
            return new KVDriver(new DatabaseUri(uriString), new Configuration(dict));
        }

        public static KVDriver Create(string uriString, Configuration conf) {
            return new KVDriver(new DatabaseUri(uriString), conf);
        }

        public static KVDriver Create(DatabaseUri uri, Configuration conf) {
            return new KVDriver(uri, conf);
        }

        /// <summary>
        /// Gets Unique Resource Identifer of this data source.
        /// </summary>
        /// <value>The URI of this datasource. The URI is of the form
        /// <c>nosql://host:port/store-name</c></value>
        public string URI {
            get {
                return dbUri.ToString();
            }
        }

        /// <summary>
        /// Gets product version of this driver.
        /// </summary>
        /// <value>The product version.</value>
        public string Version {
            get { return version; }
        }


        public object this[Option option] {
            get {
                return Config[option];
            }
            set {
                Config[option] = value;
            }
        }

        /// <summary>
        /// Gets all the options supported by this driver.
        /// </summary>
        /// <value>The options supported.</value>
        public Option[] OptionsSupported() {
            return Options.All();
        }

        public Option[] OptionsSet() {
            return OptionsSet(true);
        }


        public Option[] OptionsSet(bool excludeDefaults) {
            List<Option> options = new List<Option>();
            foreach (Option o in Config.Keys) {
                object value = this[o];
                if (o.IsDefault(value) && excludeDefaults) continue;
                options.Add(o);
            }
            return options.ToArray();
        }

        public void Dispose() {
            ProxyService.Dispose();
        }

        /// <summary>
        /// Gets default read options. 
        /// The caller gets a copy, so modification to the return value does not 
        /// modify deafault option of this context.
        /// </summary>
        /// <returns>The default read options.</returns>
        public ReadOptions DefaultReadOptions {
            get {
                return GetClonedValue<ReadOptions>(
                    Options.OPTIONS_READ_DEFAULT);
            }
        }

        /// <summary>
        /// Gets default write options. 
        /// The caller gets a copy, so modification to the return value does not 
        /// modify deafault option of this context.
        /// </summary>
        /// <returns>The default write options.</returns>
        public WriteOptions DefaultWriteOptions {
            get {
                return GetClonedValue<WriteOptions>(
                    Options.OPTIONS_WRITE_DEFAULT);
            }
        }

        public FetchOptions DefaultFetchOptions {
            get {
                return GetClonedValue<FetchOptions>(
                    Options.OPTIONS_FETCH_DEFAULT);
            }
        }

        /// <summary>
        /// Gets the default value of the given option after cloning 
        /// </summary>
        /// <returns>The default option.</returns>
        /// <param name="opt">Opt.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        T GetClonedValue<T>(Option opt) {
            var value = Config.ContainsKey(opt) ? Config[opt] : opt.Default;
            return (T)((ICloneable)value).Clone();

        }

        /// <summary>
        /// Affirms if this data source manages its own proxy.
        /// </summary>
        /// <value><c>true</c> if is managed proxy; otherwise, <c>false</c>.</value>
        public bool IsManagedProxy {
            get {
                return (bool)this[Options.PROXY_MANAGED];
            }
        }


        /// <summary>
        /// Gets a connection to data store. A connection performs data store 
        /// operation via proxy. The connections are pooled.
        /// The user should close the connection after usage for efficient pooling.
        /// </summary>
        /// <value>The connection.</value>
        public IKVStore GetStore() {
            return new KVStore(this);
        }

        public override string ToString() {
            return URI;
        }

        /// <summary>
        /// Called at initalization after a configuration is populated.
        /// This important step attaches a DataModel for the driver.
        /// The data model determines constraints on row property names and values. 
        /// </summary>
        /// <remarks>
        /// the exceptions arised at either connecting to proxy or
        /// misconfiguration is rethrown
        /// </remarks>
        static readonly object _lock = new object();
        internal void InitalizeDataModel() {
            lock (_lock) {
                KVStore store = GetStore() as KVStore;
                DataModel = new RowDataModel();
                SchemaFactory.CreateSchema(store, DataModel);
            }
        }

        /// <summary>
        /// Verify this connection properties with proxy server options.
        /// </summary>
        /// <exception cref="ArgumentException"/> if local and remote options
        /// do not match.
        internal void VerifyOptionsMatch(ONDB.Iface con, DatabaseUri uri) {
            if (IsManagedProxy) return;
            var driverProps = new TVerifyProperties();
            driverProps.DriverProtocolVersion = PROTOCOL_VERSION;
            driverProps.KvStoreHelperHosts = uri.HostPort.Split(',').ToList();
            driverProps.KvStoreName = uri.StoreName;
            driverProps.ReadZones = ((string[])
                this[Options.STORE_READ_ZONES]).ToList();
            driverProps.Username = (string)this[Options.STORE_USER_NAME];

            try {
                TVerifyResult remoteError = con.verify(driverProps);
                if (!remoteError.IsConnected) {
                    throw new ArgumentException(remoteError.Message);
                }
            } catch (TUnverifiedConnectionException ex) {
                throw ExceptionHandler.translate(ex, "VerifyOptionsMatch");
            }
            try {
                Dictionary<string, string> remoteOptions = con.getOptions();
                foreach (Option localOption in this.Config.Keys) {
                    string remoteName = localOption.ProxyAlias;
                    if (!remoteOptions.ContainsKey(remoteName)) continue;
                    string remoteValue = remoteOptions[remoteName];
                    if (localOption == Options.STORE_READ_ZONES) continue;
                    object localValue = this[localOption];
                    if (!remoteValue.Equals(localValue.ToString())) {
                        string cause = "Option " + remoteName + " set to "
                         + " [" + remoteValue + "] on Proxy is different than "
                         + " value of driver's option " + localOption.Name
                         + " [" + localValue + "]";
                        Logger.Warn("Option " + localOption.Name + "is ignored"
                        + "becuase " + cause);
                    }

                }
            } catch (MissingMethodException) {
                Logger.Warn("Can not verify non-essential options because"
                    + " proxy does not provide its options");
            }
        }

        public string Describe() {
            return Describe(false);
        }

        public string Describe(bool showOptions) {
            StringBuilder buf = new StringBuilder();
            buf.Append("Oracle NoSQL C# driver")
               .Append(" (version ")
               .Append(Version)
               .Append(")");
            if (!showOptions) return buf.ToString();
            if (OptionsSet(true).Length > 0) {
                buf.Append(Environment.NewLine)
                   .Append("\t").Append("options:");
            }
            foreach (Option o in OptionsSet(true)) {
                buf.Append("\t")
                    .Append(o.Name).Append("=")
                    .Append(this[o])
                    .Append(Environment.NewLine);
            }
            return buf.ToString();
        }

    }


}
