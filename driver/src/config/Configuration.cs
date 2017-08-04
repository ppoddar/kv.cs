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
*  \addtogroup config
* \brief Driver Configuration.
*  @{
*/



/*!
 * Supports a dictionary of options. Each option is named and typed.
 * Allows to declare options, populate and validate option values. 
 * 
 */

/** 
  * Defines configurable options for a driver.
*/

namespace oracle.kv.client.config {
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.IO;
    using option;
    using error;
    using util;
    using log;


    public enum Flag {
        MANAGED_PROXY_ARGUMENT,
        NON_MANAGED_PROXY_ARGUMENT
    };



    /// <summary>
    /// A specialized dictionary of configurable options.
    /// </summary>
    /// <remarks>
    /// The key of this dictionary can only be  <see cref="IKVDriver.OptionsSupported" />. 
    /// </remarks>
    public class Configuration : Dictionary<Option, object> {
        private static Logger Logger =
            LogManger.GetLogger(LogChannel.RUNTIME);

        /// <summary>
        /// Creates a configuration from a file.
        /// </summary>
        /// <remarks>
        /// The file should contain key-value pairs separted
        /// by '=' sign in each line.
        /// Lines starting with # and empty lines are ignored
        /// The option for a key (if any) is overwrites the default
        /// value of the option.
        /// </remarks>
        /// <returns>The default configuration overwritten by 
        ///  option values in a file.</returns>
        /// <param name="file">File where each line can be of the form
        ///  {key}={value} where {key} is case-sensitive name of an
        /// option and {value} is the value.
        /// </param>
        /// <param name="ignoreUnrecognizedOption">ignores any {key}
        /// that are not a supported option name.</param>
        public static Configuration FromFile(string file,
            bool ignoreUnrecognizedOption) {
            Assert.NotNull(file, "file name must not be null");
            string[] lines = File.ReadAllLines(file);
            Logger.Trace("Reading from " + file);
            Configuration conf = new Configuration();
            foreach (string line in lines) {
                string str = line.Trim();
                if (string.IsNullOrEmpty(str) || str[0] == '#')
                    continue;
                int idx = str.IndexOf('=');
                if (idx != -1) {
                    Option o = Options.Find(str.Substring(0, idx).Trim());
                    if (o != null) {
                        conf[o] = str.Substring(idx + 1).Trim();
                    } else if (!ignoreUnrecognizedOption) {
                        Logger.Warn("unknown option " + str);
                    }
                }
            }
            return conf;
        }

        /// <summary>
        /// Creates a configuration with all options set to its default value.
        /// </summary>
        private Configuration() {
            foreach (Option o in Options.All()) {
                this[o] = o.Default;
            }
        }

        /// <summary>
        /// Creates a configuration with all options set to its value
        /// as in given dictionary.
        /// </summary>
        /// <param name="dict">Dictionary of values. Can be null.
        /// If null, then the configuration has no option.
        /// </param>
        public Configuration(Dictionary<Option, object> dict) : this() {
            if (dict == null) return;
            foreach (KeyValuePair<Option, object> e in dict) {
                if (e.Key != null) {
                    this[e.Key] = e.Value;
                }
            }
        }

        /// <summary>
        /// Gets the default configration for a driver.
        /// </summary>
        /// <value>A dictionary of required options set to their default value.</value>
        public static Configuration Default {
            get {
                return new Configuration();
            }
        }

        /// <summary>
        /// Affirms if this configuration is read-only.
        /// </summary>
        /// <remarks>No option can be set in a read-only configuration
        /// </remarks>
        public bool IsReadOnly { get; internal set; }

        /// <summary>
        /// Gets or sets the value for the specified option.
        /// The option must be declared for getting or setting its value.
        /// </summary>
        /// <param name="option">Option.</param>
        public new object this[Option option] { // indexer overwritten
            get {
                return base[option];
            }
            set {
                if (IsReadOnly) {
                    throw new InvalidOperationException(
                        "Configuration can not changed " +
                        " after connection has been established to store");
                }

                base[option] = option.ConvertValue(value);
            }
        }


        public Configuration Merge(Dictionary<Option, object> dict) {
            if (dict == null) {
                return this;
            }
            foreach (Option o in dict.Keys) {
                this[o] = dict[o];
            }
            return this;
        }

        internal bool HasNonNullValue(Option o) {
            return this.ContainsKey(o) && this[o] != null;
        }





        public string ToString(bool excludeDefault) {
            List<string> list = new List<string>();
            foreach (Option o in Keys) {
                object value = this[o];
                object defaultValue = o.Default;
                if (excludeDefault && o.IsDefault(value)) {
                    continue;
                }
                list.Add(o.Name + ':' + this[o]);
            }
            string[] array = list.ToArray();
            Array.Sort(array);
            return (string.Join(Environment.NewLine, array));

        }

        public override string ToString() {
            return ToString(true);

        }
    }

    /// <summary>
    /// An option configures a property of a driver. 
    /// </summary>
    /// <remarks>
    /// An option is identifiable by a name, accepts values of certain type,
    /// may have a default value and  a description.
    /// <para></para>
    /// some options must be passed to a manged proxy serive to start it.
    /// some options are used to verify a non-managed proxy serivice.
    /// some option values can be mutated per operation basis e.g. fetch
    /// size while some are not e.g. managed proxy switch. 
    /// These various non-exclusive choices are represented with flags.
    /// </remarks>
    public class Option : IComparable {
        /// <summary>
        /// Gets the type of value that can be set to this option.
        /// </summary>
        /// <value>The type of value that can be set on this option.</value>
        public Type Type { get; private set; }

        private object DefaultValue;
        public object Default {
            get {
                return DefaultValue;
            }
            private set {
                try {
                    DefaultValue = Convert.ChangeType(value, Type);
                } catch (Exception ex) {
                    throw new ArgumentException(value + " can be set "
                        + " on " + this);
                }
            }
        }
        private List<Flag> Flags = new List<Flag>();

        /// <summary>
        /// Alias for this option when used as proxy argument.
        /// </summary>
        /// <value>The alias is often the name prefixed with a '-'.</value>
        public string ProxyAlias { get; set; }


        public bool IsDefault(object value) {
            return (Default == null) ? value == null
            : Default.Equals(value);
        }

        /// <summary>
        /// Gets the name of this option.
        /// </summary>
        /// <value>The name. Never null or empty</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Creates an option of given name.
        /// </summary>
        /// <param name="name">Name of an option.</param>
        internal Option(string name, Type t) : this(name, t, "") { }


        /// <summary>
        /// Creates an option of given name with given description.
        /// </summary>
        /// <param name="name">Name of the option. Must not be null or empty</param>
        /// <param name="description">Description.</param>
        internal Option(string name, Type t, string description) {
            if (name == null)
                throw new ArgumentException("Can not create option with null name");

            if (name.Trim().Length == 0)
                throw new ArgumentException("Can not create option with empty name");


            Name = name;
            Type = t;
            ProxyAlias = "-" + name;
            withDescription(description);
        }

        /// <summary>
        /// Affirms if this option is required to be specified 
        /// to start a proxy service.
        /// </summary>
        public bool IsProxyArgument {
            get {
                return Flags.Contains(Flag.MANAGED_PROXY_ARGUMENT);
            }
        }

        /// <summary>
        /// Affirms if this option is required to be specified 
        /// to connect a proxy service.
        /// </summary>
        public bool IsNonManagedProxyArgument {
            get {
                return Flags.Contains(Flag.NON_MANAGED_PROXY_ARGUMENT);
            }
        }



        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <returns>The default.</returns>
        /// <param name="value">Value.</param>
        public Option withDefault(object value) {
            Default = ConvertValue(value);
            return this;
        }

        private bool CanSetValue(object value) {
            if (Type.IsEnum && value is string) {
                return EnumHelper.ValidEnum(Type, value as string);
            }
            return value == null || value.GetType() == Type;
        }

        /// <summary>
        /// Converts the given value to a value of the Type of this option.
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="value">Value.</param>
        public object ConvertValue(object value) {
            if (!CanSetValue(value)) {
                throw new ArgumentException(this + " can not set "
                    + (value == null ? "null" : value)
                    + (value == null ? "" : " of type " + value.GetType()));
            }
            if (Type.IsEnum && value is string) {
                return EnumHelper.ResolveEnum(Type, value as string);
            }
            return value;
        }




        /// <summary>
        /// Sets the given flag(s).
        /// </summary>
        /// <returns>The flag.</returns>
        /// <param name="flags">Flags to add.</param>
        public Option withFlags(params Flag[] flags) {
            foreach (Flag flag in flags)
                Flags.Add(flag);
            return this;
        }


        public Option withProxyAlias(string proxyAlias) {
            ProxyAlias = proxyAlias;
            return this;
        }

        /// <summary>
        /// Sets the description.
        /// </summary>
        /// <returns>The description.</returns>
        /// <param name="desc">Desc.</param>
        public Option withDescription(string desc) {
            Description = desc ?? "";
            return this;
        }

        public override bool Equals(object obj) {
            Option that = obj as Option;
            return that != null && that.Name.Equals(Name);
        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        public override string ToString() {
            return Name + '(' + Type + ')';
        }

        public int CompareTo(Object other) {
            return (other as Option == null) ? 1
            : string.Compare(this.Name, (other as Option).Name,
                StringComparison.CurrentCulture);
        }
    }

    /// <summary>
    /// A configurable option for driver.
    /// </summary>
    /// <remarks>
    /// Declares (statically) all options supported by data source.
    /// <para></para>
    /// The set of options includes option that apply to driver as well as
    /// proxy. If a data source manage its own proxy, then all proxy-related 
    /// options are used to start a proxy, otherwise those options are ignored. 
    ///</remarks>
    public class Options {

        private static readonly string MANAGED_DOC = "";
        private static readonly string NON_MANAGED_DOC = "";

        public static readonly Option PROXY_MANAGED =
            new Option("proxy-managed", typeof(bool),
                "If true, manages own proxy service.")
                .withDefault(true);

        public static Option PROXY_HOST =
            new Option("proxy-host", typeof(string),
            "The host where driver would connect to Proxy Service"
            + " in non-managed Proxy Mode.\n"
            + "Not used in Managed Proxy Mode.")
                .withDefault("localhost")
               .withFlags(Flag.NON_MANAGED_PROXY_ARGUMENT);

        public static Option PROXY_PORT =
            new Option("proxy-port", typeof(int),
            "The port where driver would connect to a Proxy Service"
            + " in non-managed proxy mode.\n "
            + "Not used in Managed Proxy Mode, "
            + "a managed Proxy listens to a randomly selected port.")
                .withDefault(5010)
                .withProxyAlias("-port")
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT,
                    Flag.NON_MANAGED_PROXY_ARGUMENT);

        public static Option PROXY_EXECUTABLE =
            new Option("proxy-executable", typeof(string),
            "Path to Java executable to start Proxy Service "
            + "in managed proxy mode. \n"
            + "The executable path refers to the same host of this driver")
                .withDefault("java");


        public static Option PROXY_CLASSPATH =
            new Option("proxy-classpath", typeof(string),
         "The classpath to start Proxy Service. \n"
         + "The wildcard can be used for classpath."
         + "The default classpath is platform-specific")
        .withDefault(StringHelper.Quote(
        Path.Combine(new string[]{
            Environment.GetFolderPath(
           Environment.SpecialFolder.ProgramFiles), "kvproxy", "*"})));

        public static Option ITERATOR_MAX_BATCH_SIZE =
            new Option("max-iterator-results", typeof(int),
           "Maximum number of results to fetch in a single iterator call.\n"
           + "In non-managed proxy mode, the value should match the value "
           + "used by the proxy service.")
                .withDefault(100)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option PROXY_STARTUP_WAIT_TIME_MS =
            new Option("proxy-startup-time", typeof(int),
            "Wait time in millisecond for a managed Proxy Service to start.\n"
            + "Used only in managed mode.")
                .withDefault(5 * 1000);

        public static Option SOCKET_OPEN_TIMEOUT =
            new Option("socket-open-timeout", typeof(long),
            "Timeout in millisecond to open a socket connection to data store. \n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(3 * 1000L)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);



        public static Option SOCKET_READ_TIMEOUT =
            new Option("socket-read-timeout", typeof(long),
            "Timeout in millisecond for reading from socket connection to data store. \n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(30 * 1000L)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);


        public static Option ITERATOR_EXPIRATION =
            new Option("iterator-expiration", typeof(int),
            "Timeout in millisecond to close an idle table iterator.\n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
               .withDefault(5 * 60 * 1000)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option ITERATOR_MAX_OPEN =
            new Option("max-open-iterators", typeof(int),
            "Maximum number of iterators that can be opened concurrently.\n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(10000)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        // Request limit flags
        public static Option REQUEST_MAX_ACTIVE =
            new Option("max-active-requests", typeof(int),
            "Maximum number of active requests to data store. \n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(100)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option REQUEST_PERCENT_LIMIT_PER_NODE =
        new Option("node-limit-percent", typeof(int),
            "Limit on the number of requests, "
            + "as a percentage of maximum active requests.\n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(80)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option REQUEST_PERCENT_THRESHOLD =
            new Option("request-threshold-percent", typeof(int),
            "Threshold for activating request throttling,"
            + " as a percentage of the requested maximum active requests. \n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(90)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option REQUEST_TIMEOUT =
            new Option("request-timeout", typeof(long),
            "The default request timeout in milliseconds. \n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(5 * 1000L)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option REQUST_MAX_CONCURRENT_PER_ITERATOR =
            new Option("max-concurrent-requests", typeof(int),
            "The maximum number of concurrent requests per iterator. \n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(2 * 4 /*available processeors*/)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option ITERATOR_MAX_RESULTS_BATCHES =
            new Option("max-results-batches", typeof(int),
            "The maximum number of result batches held in the proxy"
            + "per iterator.\n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(100)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option PERF_STATS =
            new Option("perf-stats", typeof(bool),
            "Enable performance statistics into default logger\n"
            + "Note: Statistics are logged at FINE level.\n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(false)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option STATISTICS_INTERVAL =
            new Option("stats-interval", typeof(int),
            "Interval of logging performance statistics in seconds. "
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(60)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option LATENCY_TRACKING_THRESHOLD =
            new Option("max-tracked-latency", typeof(int),
            "Maximum threshold for tracking latency in milliseconds. "
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")

                .withDefault(10 * 1000)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option LATENCY_TRACKING_CEILING =
            new Option("latency-ceiling", typeof(int),
            "Threshold for logging higher than expected latency "
            + "in milliseconds per request. Logged at WARNING level. "
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")

                .withDefault(10 * 1000)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option THROUGHPUT_FLOOR =
            new Option("throughput-floor", typeof(int),
            "Threshold for logging lower than expected throughput "
            + "in request per second. Logged at WARNING level."
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")

                .withDefault(0)
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);


        public static Option STORE_READ_ZONES =
            new Option("read-zones", typeof(string[]),
            "List of read zone names separated by comma.\n"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")
                .withDefault(new string[0] { })
                .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option STORE_USER_NAME =
            new Option("username", typeof(string),
            "The name of the user to login to the secured store."
            + "Required for connecting to a secure data store."
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")

            .withFlags(Flag.MANAGED_PROXY_ARGUMENT);


        public static Option STORE_SECURITY_FILE =
            new Option("security", typeof(string),
            "The security file used for login. "
            + "Required for connecting to a secure store"
            + "In non-managed proxy mode, the value should match the value "
            + "used by the proxy service.")

            .withFlags(Flag.MANAGED_PROXY_ARGUMENT);

        public static Option OPTIONS_READ_DEFAULT =
            new Option("read-options", typeof(ReadOptions),
            "Default options for read operations. "
            + "Only used in managed proxy mode")
                .withDefault(new ReadOptions().makeReadOnly());

        public static Option OPTIONS_WRITE_DEFAULT =
            new Option("write-options", typeof(WriteOptions),
            "Default options for write operations."
            + "Only used in managed proxy mode")
                .withDefault(new WriteOptions().makeReadOnly());

        public static Option OPTIONS_FETCH_DEFAULT =
            new Option("fetch-options", typeof(FetchOptions),
            "Default options for fetch operations. "
            + "Only used in managed proxy mode")
                .withDefault(new FetchOptions().makeReadOnly());



        public static readonly Option PROXY_START_ATTEMPT =
            new Option("proxy-start-attempt", typeof(int),
            "number of attempts made to spawn a proxy process."
            + "Only used in managed proxy mode")
                .withDefault(2);

        public static readonly Option PROXY_PORT_RANGE_START =
            new Option("proxy-port-range-start", typeof(int),
            "start of port range for managed proxy process."
            + "Only used in managed proxy mode")
                .withDefault(8000);

        public static readonly Option PROXY_PORT_RANGE_END =
            new Option("proxy-port-range-end", typeof(int),
            "end of port range for managed proxy process."
            + "Only used in managed proxy mode")
                .withDefault(9000);


        public static readonly Option SCHEMA_RESOURCE =
            new Option("schema-resource", typeof(string),
            "Absolute path to a schema descriptor file")
                .withDefault("");

        public static readonly Option LOG_LEVEL =
            new Option("log-level", typeof(TraceLevel),
            "Default logging level for channel")
                .withDefault(TraceLevel.Warning);

        public static readonly Option LOG_SPEC =
            new Option("log-spec", typeof(string),
            "Specification for logging channel")
                .withDefault("Runtime=Info");

        private static Option[] allOptions;

        /// <summary>
        /// Gets all declared Option.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>The all.</returns>
        public static Option[] All() {
            if (allOptions == null) {
                List<Option> all = new List<Option>();
                foreach (FieldInfo f in typeof(Options).GetFields()) {
                    if (f.IsStatic && f.FieldType == typeof(Option)) {
                        var o = (Option)f.GetValue(null);
                        all.Add(o);
                    }
                }
                allOptions = all.ToArray();
                Array.Sort(allOptions);
            }
            return allOptions;
        }


        /// <summary>
        /// Finds an option by name.
        /// </summary>
        /// <returns>The option with matching name. Null if no option exists
        /// of given name</returns>
        /// <param name="key">name of an option.</param>
        /// <returns>null if no options has the same name.</returns>
        public static Option Find(string key) {
            foreach (Option o in All()) {
                if (o.Name.Equals(key)) {
                    return o;
                }
            }
            return null;
        }
    }
}


/*! @} End of Doxygen Groups*/

