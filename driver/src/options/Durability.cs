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

using System;
using oracle.kv.proxy.gen;

namespace oracle.kv.client.option {
    /// <summary>
    /// Durability options for a write
    /// operation.
    /// </summary>
    /// <remarks>
    /// The overall durability is a function of the <see cref="SyncPolicy"/>
    /// in effect for each Replica.and <see cref="ReplicaAckPolicy"/>
    /// in effect for the Master.
    /// </remarks>
    public class Durability : Thrifty<TDurability>, ICloneable {
        /// <summary>
        /// A convenience constant that defines a durability policy with COMMIT_SYNC
        /// for Master commit synchronization.
        ///
        /// The policies default to COMMIT_NO_SYNC for commits of replicated
        /// transactions that need acknowledgment and SIMPLE_MAJORITY for the
        /// acknowledgment policy.
        /// </summary>
        public static Durability COMMIT_SYNC = new Durability(SyncPolicy.SYNC,
                                                              SyncPolicy.NO_SYNC,
                                                              ReplicaAckPolicy.SIMPLE_MAJORITY);
        /// <summary>
        /// A convenience constant that defines a durability policy with
        /// COMMIT_NO_SYNC for Master commit synchronization.
        ///
        /// The policies default to COMMIT_NO_SYNC for commits of replicated
        /// transactions that need acknowledgment and SIMPLE_MAJORITY for the
        /// acknowledgment policy.
        /// </summary>
        public static Durability COMMIT_NO_SYNC = new Durability(SyncPolicy.NO_SYNC,
                                                      SyncPolicy.NO_SYNC,
                                                      ReplicaAckPolicy.SIMPLE_MAJORITY);

        /// <summary>
        /// A convenience constant that defines a durability policy with
        /// COMMIT_WRITE_NO_SYNC for Master commit synchronization.
        ///
        /// The policies default to COMMIT_NO_SYNC for commits of replicated
        /// transactions that need acknowledgment and SIMPLE_MAJORITY for the
        /// acknowledgment policy.
        /// </summary>
        public static Durability COMMIT_WRITE_NO_SYNC = new Durability(SyncPolicy.WRITE_NO_SYNC,
                                                              SyncPolicy.NO_SYNC,
                                                              ReplicaAckPolicy.SIMPLE_MAJORITY);

        internal Durability() : this(SyncPolicy.SYNC,
                                     SyncPolicy.NO_SYNC,
                                     ReplicaAckPolicy.SIMPLE_MAJORITY) { }


        internal Durability(TDurability thrift) : base(thrift) { }

        /// <summary>
        /// Create  <see cref="T:oracle.kv.client.Durability"/> instance.
        /// </summary>
        /// <param name="masterSync">Synchronization policy for master node.</param>
        /// <param name="replicaSync">Synchronization policy for replica nodes.</param>
        /// <param name="replicaAck">Replica acknowledge policy.</param>
        public Durability(SyncPolicy masterSync, SyncPolicy replicaSync,
                          ReplicaAckPolicy replicaAck) : base(new TDurability()) {
            Thrift.MasterSync = masterSync.Thrift;
            Thrift.ReplicaSync = replicaSync.Thrift;
            Thrift.ReplicaAck = replicaAck.Thrift;
        }

        /// <summary>
        /// policy for the master sync.
        /// </summary>
        /// <value>The master sync policy.</value>
        public SyncPolicy MasterSyncPolicy {
            get { return SyncPolicy.create(Thrift.MasterSync); }
            set { Thrift.MasterSync = value.Thrift; }
        }

        /// <summary>
        /// policy for the replica sync.
        /// </summary>
        /// <value>The replica sync policy.</value>
        public SyncPolicy ReplicaSyncPolicy {
            get { return SyncPolicy.create(Thrift.ReplicaSync); }
            set { Thrift.ReplicaSync = value.Thrift; }
        }

        /// <summary>
        /// policy for the replicae acknowledgement.
        /// </summary>
        /// <value>The replica ack policy.</value>
        public ReplicaAckPolicy ReplicaAckPolicy {
            get { return ReplicaAckPolicy.create(Thrift.ReplicaAck); }
            set { Thrift.ReplicaAck = value.Thrift; }
        }


        public object Clone() {
            return new Durability(this.MasterSyncPolicy,
                                  this.ReplicaSyncPolicy,
                                  this.ReplicaAckPolicy);
        }


        public override bool Equals(object other) {
            if (other is Durability) {
                Durability d2 = other as Durability;
                return MasterSyncPolicy.Equals(d2.MasterSyncPolicy)
                    && ReplicaAckPolicy.Equals(d2.ReplicaAckPolicy)
                    && ReplicaSyncPolicy.Equals(d2.ReplicaSyncPolicy);
            }
            return false;
        }

        public override string ToString() {
            return string.Format("[Master={0}, Replica={1}, ReplicaAck={2}]",
                MasterSyncPolicy, ReplicaSyncPolicy, ReplicaAckPolicy);
        }

    }
}
/*! @} End of Doxygen Groups*/
