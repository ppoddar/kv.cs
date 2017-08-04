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



using System;
using System.IO;
using System.Diagnostics;
using Thrift.Protocol;
using Thrift.Transport;
using System.Text;
using oracle.kv.client.config;
using oracle.kv.proxy.gen;
using System.Threading;
using oracle.kv.client.log;
using oracle.kv.client.error;
using oracle.kv.client.util;
using System.Threading.Tasks;
using System.Reflection;

namespace oracle.kv.client {
    /// <summary>
    /// Manages a remote Java-based proxy service. 
    /// Starts a Proxy Service process as a Java Program in the same host 
    /// setting classpath and command line flags.
    /// 
    /// </summary>
    internal class ManagedProxyService : ProxyService {

        Process ProxyProcess { get; set; }
        protected Logger RemoteLogger;

        public static readonly string EXECUTABLE = "java";
        public static readonly string PROXY_CLASS = "oracle.kv.proxy.KVProxy";


        // a string to appear in remote process output to signal
        // that the process has started
        static string START_SIGNAL =
            "Starting listener ( Half-Sync/Half-Async server";

        // a platform specific path separator
        static string PATH_SEPARATOR {
            get {
                int p = (int)Environment.OSVersion.Platform;
                return ((p == 4) || (p == 6) || (p == 128)) ? "/" : "\\";
            }
        }

        /// <summary>
        /// Initializes a new instance of ProxyService
        /// with given configuration. 
        /// 
        /// </summary>
        /// <param name="driver">Configuration.</param>
        internal ManagedProxyService(KVDriver driver) : base(driver) {

            int maxTrial = (int)driver[Options.PROXY_START_ATTEMPT];
            int portStart = (int)driver[Options.PROXY_PORT_RANGE_START];
            int portEnd = (int)driver[Options.PROXY_PORT_RANGE_END];
            Logger.Trace("will try " + maxTrial + " times to start a proxy service "
                + " listening on any port between (" + portStart + "," + portEnd + "]");

            Random rng = new Random();
            int timeout = (int)driver[Options.PROXY_STARTUP_WAIT_TIME_MS];
            for (int trial = 0; trial < maxTrial && ProxyProcess == null; trial++) {
                driver[Options.PROXY_PORT] = rng.Next(portStart, portEnd);
                string args = CreateProcessArguments(driver, driver.dbUri);
                ProcessLauncher launcher = new ProcessLauncher();
                try {
                    Logger.Trace("trial=" + trial + " at port " + driver[Options.PROXY_PORT]);
                    ProxyProcess = launcher.Launch(EXECUTABLE, args,
                           timeout, START_SIGNAL,
                           LogManger.GetLogger(LogChannel.REMOTE));
                } catch (Exception ex) {
                    ProxyProcess = null;
                    // ignore and try again on another port
                    Logger.Trace("failed trial " + trial + "  " + ex.Message + " retrying...");
                    Logger.Warn(ex.Message);
                }
            }
            if (ProxyProcess == null) {
                throw new ArgumentException("cannot start proxy  with command ["
                 + EXECUTABLE + " "
                 + CreateProcessArguments(driver, driver.dbUri) + "]");
            }

        }

        private string CreateProcessArguments(KVDriver driver,
                DatabaseUri uri) {
            return string.Join(" ", new string[]{"-classpath",
                        ProxyClassPath(driver),
                        PROXY_CLASS,
                        CreateProxyArgumentList(driver,uri)});

        }

        /// <summary>Gets the classpath for running the proxy service.
        /// </summary>
        /// <remarks> The classpath is set via <code>PROXY_CLASSPATH</code> 
        /// option.
        /// </remarks>
        string ProxyClassPath(IKVDriver driver) {
            return (string)driver[Options.PROXY_CLASSPATH];
        }

        /// <summary>
        /// Creates the argument list. THe configuration options flagged as
        /// PROXY_ARGUMENT are used.
        /// Some options used by Java Proxy Server may not be
        /// same as the option names in supplied configuration.
        /// 
        /// </summary>
        /// <returns>The argument list.</returns>
        private string CreateProxyArgumentList(KVDriver driver,
            DatabaseUri uri) {
            string buf = "";

            foreach (Option o in driver.Config.Keys) {
                if (!o.IsProxyArgument) continue;
                object value = driver[o];
                string valueStr = value == null ? "" : value.ToString();
                if (string.IsNullOrEmpty(valueStr)) continue;
                if (o.IsDefault(value)) continue;
                if (o == Options.STORE_READ_ZONES) {
                    if (value is string[] && (value as string[]).Length == 0)
                        continue;
                }
                buf += o.ProxyAlias + " " + valueStr + " ";
            }
            buf += " -helper-hosts " + uri.HostPort
                 + " -store " + uri.StoreName;

            return buf;
        }



        public override void Dispose() {
            new ProcessLauncher().killProcess(ProxyProcess);
        }


        ~ManagedProxyService() {
            Dispose();
        }

    }
}
