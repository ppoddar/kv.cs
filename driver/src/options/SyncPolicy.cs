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
*  \addtogroup option 
* \brief option for database operations
*  @{
*/

using oracle.kv.proxy.gen;
using oracle.kv.client.error;

namespace oracle.kv.client.option {
    /// <summary>
    /// Synchronization policy to be used when committing a
    /// transaction. 
    /// </summary>
    /// <remarks>
    /// Higher levels of synchronization offer a greater guarantee
    /// that the transaction is persistent to disk, but trade-off is
    /// lower performance.      
    /// </remarks>
    public class SyncPolicy {
        /// <summary>
        ///  Write and synchronously flush the log on transaction commit.
        ///  Transactions exhibit all the ACID (atomicity, consistency,
        ///  isolation, and durability) properties.
        /// </summary>
        public static SyncPolicy SYNC = new SyncPolicy(TSyncPolicy.SYNC);

        /// <summary>
        /// Do not write or synchronously flush the log on transaction commit.
        /// Transactions exhibit the ACI (atomicity, consistency, and isolation)
        /// properties, but not D (durability); that is, database integrity will
        /// be maintained, but if the application or system fails, it is
        /// possible some number of the most recently committed transactions may
        /// be undone during recovery. The number of transactions at risk is
        /// governed by how many log updates can fit into the log buffer, how
        /// often the operating system flushes dirty buffers to disk, and how
        /// often log checkpoints occur.
        /// </summary>
        public static SyncPolicy NO_SYNC = new SyncPolicy(TSyncPolicy.NO_SYNC);

        /// <summary>
        /// Write but do not synchronously flush the log on transaction commit.
        /// Transactions exhibit the ACI (atomicity, consistency, and isolation)
        /// properties, but not D (durability); that is, database integrity will
        /// be maintained, but if the operating system fails, it is possible
        /// some number of the most recently committed transactions may be
        /// undone during recovery. The number of transactions at risk is
        /// governed by how often the operating system flushes dirty buffers to
        /// disk, and how often log checkpoints occur.
        /// </summary>
        public static SyncPolicy WRITE_NO_SYNC = new SyncPolicy(TSyncPolicy.WRITE_NO_SYNC);


        public TSyncPolicy Thrift { get; internal set; }

        public static SyncPolicy create(TSyncPolicy t) {
            switch (t) {
                case TSyncPolicy.SYNC: return SYNC;
                case TSyncPolicy.NO_SYNC: return NO_SYNC;
                case TSyncPolicy.WRITE_NO_SYNC: return WRITE_NO_SYNC;
                default: return NO_SYNC;
            }

        }

        SyncPolicy(TSyncPolicy t) { Thrift = t; }

        public override string ToString() {
            if (this == SyncPolicy.SYNC) return "SYNC";
            if (this == SyncPolicy.NO_SYNC) return "NO_SYNC";
            if (this == SyncPolicy.WRITE_NO_SYNC) return "WRITE_NO_SYNC";
            return "unknown";
        }

    };

}
/*! @} End of Doxygen Groups*/
