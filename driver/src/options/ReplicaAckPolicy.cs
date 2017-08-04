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
    /// Replica acknowledgement policy.
    /// </summary>
    public class ReplicaAckPolicy {
        public static ReplicaAckPolicy ALL = new ReplicaAckPolicy(TReplicaAckPolicy.ALL);
        public static ReplicaAckPolicy NONE = new ReplicaAckPolicy(TReplicaAckPolicy.NONE);
        public static ReplicaAckPolicy SIMPLE_MAJORITY = new ReplicaAckPolicy(TReplicaAckPolicy.SIMPLE_MAJORITY);

        public TReplicaAckPolicy Thrift { get; set; }

        public static ReplicaAckPolicy create(TReplicaAckPolicy t) {
            switch (t) {
                case TReplicaAckPolicy.ALL: return ALL;
                case TReplicaAckPolicy.NONE: return NONE;
                case TReplicaAckPolicy.SIMPLE_MAJORITY: return SIMPLE_MAJORITY;
                default: return NONE;

            }
        }

        ReplicaAckPolicy(TReplicaAckPolicy t) {
            Thrift = t;
        }

        public override string ToString() {
            if (this == ReplicaAckPolicy.ALL)
                return "ALL";
            if (this == ReplicaAckPolicy.NONE)
                return "NONE";
            if (this == ReplicaAckPolicy.SIMPLE_MAJORITY)
                return "SIMPLE_MAJORITY";
            return "unknown";
        }

    };
}
/*! @} End of Doxygen Groups*/
