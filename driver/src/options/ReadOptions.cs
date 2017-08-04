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

namespace oracle.kv.client.option {
    using System;
    using oracle.kv.client.config;
    using oracle.kv.proxy.gen;

    /// <summary>
    /// Options for read operations.
    /// </summary>
    public class ReadOptions : ImmutableThriftValue<TReadOptions>, ICloneable {
        /**
         * Creates a read options with default consistency (NONE_REQUIRED)
         * and default read time out.
         */
        public ReadOptions() : this(
            SimpleConsistency.NONE_REQUIRED,
            (long)Options.SOCKET_READ_TIMEOUT.Default) {
        }


        public ReadOptions(Consistency Consistency, long readTimeOut) :
        base(new TReadOptions()) {
            Thrift.Consistency = ConsistencyHelper.Thrift(Consistency);
            Thrift.TimeoutMs = readTimeOut;
        }

        internal ReadOptions(TReadOptions thrift) : base(thrift) { }

        public Consistency Consistency {
            get { return ConsistencyHelper.From(Thrift.Consistency); }
            set {
                assertMutable("Consistency");
                Thrift.Consistency = ConsistencyHelper.Thrift(value);
            }
        }

        public long TimeoutMs {
            get { return Thrift.TimeoutMs; }
            set { assertMutable("TimeoutMs"); Thrift.TimeoutMs = value; }
        }

        public object Clone() {
            var clone = new ReadOptions();
            clone.Consistency = (Consistency)Consistency.Clone();
            clone.TimeoutMs = TimeoutMs;
            return clone;
        }

        new internal ReadOptions makeReadOnly() {
            base.makeReadOnly();
            return this;
        }

        public override string ToString() {
            return string.Format("[ReadOptions: Consistency={0}, TimeoutMs={1}]",
                ConsistencyHelper.ToString(Consistency), TimeoutMs);
        }
    }
}
/*! @} End of Doxygen Groups*/
