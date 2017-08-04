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
    using proxy.gen;
    using System;
    using config;

    /// <summary>
    /// Options for write operation.
    /// </summary>
    public class WriteOptions : ImmutableThriftValue<TWriteOptions>, ICloneable {

        /// <summary>
        /// Creates write options with <code>Durability.COMMIT_NO_SYNC</code>
        /// and default <code>Options.SOCKET_READ_TIMEOUT</code>.
        /// </summary>
        internal WriteOptions() : base(new TWriteOptions()) {
            Durability = Durability.COMMIT_NO_SYNC;
            TimeoutMs = (long)Options.SOCKET_READ_TIMEOUT.Default;

        }

        /// <summary>
        /// Creates write options with given <see cref="Durability"/> and
        /// write time out in millisecond.
        /// </summary>
        /// <param name="Durability">Durability.</param>
        /// <param name="timeoutMs">Timeout ms.</param>
        public WriteOptions(Durability Durability, long timeoutMs) :
        base(new TWriteOptions()) {
            Thrift.Durability = Durability.Thrift;
            Thrift.TimeoutMs = timeoutMs;
        }


        public bool UpdateTTL {
            get { return Thrift.UpdateTTL; }
            set { assertMutable("TTL"); Thrift.UpdateTTL = value; }
        }

        public long TimeoutMs {
            get { return Thrift.TimeoutMs; }
            set { assertMutable("TimeoutMs"); Thrift.TimeoutMs = value; }
        }


        public Durability Durability {
            get { return new Durability(Thrift.Durability); }
            set { assertMutable("Durability"); Thrift.Durability = value.Thrift; }
        }

        public ReturnChoice ReturnChoice {
            get { return ReturnChoice.NONE.From(Thrift.ReturnChoice); }
            set { assertMutable("ReturnRowChoice"); Thrift.ReturnChoice = value.Thrift(); }
        }

        public object Clone() {
            var clone = new WriteOptions();
            clone.Durability = (Durability)Durability.Clone();
            clone.TimeoutMs = TimeoutMs;
            clone.ReturnChoice = (ReturnChoice)
                Enum.Parse(typeof(ReturnChoice),
                ReturnChoice.ToString());
            return clone;
        }


        new internal WriteOptions makeReadOnly() {
            base.makeReadOnly();
            return this;

        }

        public override string ToString() {
            return string.Format("[WriteOptions: Durability={0}, ReturnChoice={1},"
                + "TimeoutMs={2}, UpdateTTL={3}]",
                Durability, ReturnChoice, TimeoutMs, UpdateTTL);
        }

    }
}
/*! @} End of Doxygen Groups*/
