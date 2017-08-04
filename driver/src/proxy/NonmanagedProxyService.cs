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
namespace oracle.kv.client {
    /// <summary>
    /// Manages a remote Java-based proxy service. 
    /// Can start a Proxy Service process as a Java Program in the same host 
    /// setting classpath and command line flags.
    /// 
    /// </summary>
    internal class NonmanagedProxyService : ProxyService {


        /// <summary>
        /// Initializes a new instance of ProxyService
        /// with given configuration. 
        /// 
        /// </summary>
        /// <param name="driver">a driver that uses this service.</param>
        internal NonmanagedProxyService(IKVDriver driver)
            : base(driver) { }

        public override void Dispose() { }
    }
}
