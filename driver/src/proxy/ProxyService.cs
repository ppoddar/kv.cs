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
using System.Diagnostics;
using Thrift.Protocol;
using Thrift.Transport;
using System.Text;
using oracle.kv.client.config;
using oracle.kv.proxy.gen;
using System.Threading;
using oracle.kv.client.log;
using oracle.kv.client.error;
using System.Threading.Tasks;
using System.ComponentModel;
namespace oracle.kv.client {
    /// <summary>
    /// A remote Java-based proxy service. 
    /// </summary>
    internal abstract class ProxyService : IDisposable {
        IKVDriver Driver { get; set; }

        /// <summary>
        /// Name of the host running the Proxy Service.
        /// </summary>
        /// <value>The hostname.</value>
        public string Host {
            get {
                return (string)Driver[Options.PROXY_HOST];
            }
        }

        /// <summary>
        /// The port at which Proxy Service is listening.
        /// </summary>
        /// <value>The port.</value>
        public int Port {
            get {
                return (int)Driver[Options.PROXY_PORT];
            }
        }

        /// <summary>
        /// An URL for the Proxy Service listening with 
        /// <code>Thrift</code> protcol.
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get { return "thrift://" + Host + ":" + Port; } }

        protected readonly Logger Logger;

        /// <summary>
        /// Creates a proxy for the given driver.
        /// </summary>
        /// <returns>The create.</returns>
        /// <param name="driver">Driver.</param>
        public static ProxyService Create(KVDriver driver) {
            bool isManaged = (bool)driver[Options.PROXY_MANAGED];
            if (isManaged) {
                return new ManagedProxyService(driver);
            }
            return new NonmanagedProxyService(driver);
        }

        /// <summary>
        /// Initializes a new instance of ProxyService
        /// for given driver. 
        /// </summary>
        /// <param name="driver">a driver that uses this service.</param>
        protected ProxyService(IKVDriver driver) {
            Driver = driver;
            Logger = LogManger.GetLogger(LogChannel.PROXY);

        }

        /// <summary>
        /// Connects to remote process process using Thrift protcol.
        /// </summary>
        /// <returns>ONDB Thrift client interface </returns>
        internal ONDB.Client ConnectToThrift() {
            AutoResetEvent stopWatch = new AutoResetEvent(false);
            try {
                Task connectionTask = new Task(() => {
                    this.waitForConnection(stopWatch,
                        (int)Driver[Options.PROXY_STARTUP_WAIT_TIME_MS]);
                });
                connectionTask.Start();
                TSocket socket = new TSocket(Host, Port);
                TTransport transport = new TFramedTransport(socket);
                TProtocol protocol = new TBinaryProtocol(transport);
                transport.Open();
                stopWatch.Set();
                return new ONDB.Client(protocol);
            } catch (Exception ex) {
                throw new ArgumentException("Can not open connetcion to " + this, ex);
            }
        }

        /// <summary>
        /// Waits for connection.
        /// </summary>
        /// <param name="clock">Clock.</param>
        /// <param name="ms">Ms.</param>
        void waitForConnection(AutoResetEvent clock, int ms) {
            try {
                clock.WaitOne(ms);
            } catch (AbandonedMutexException ex) {
                throw new ArgumentException("can not connect"
                + " to " + this + " in " + ms + " ms");
            }
        }

        public override string ToString() {
            return "thrift://" + Host + ":" + Port;
        }

        public abstract void Dispose();
    }

}
