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

/** 
  * Defines configurable options of a datastore operations such a read
  * consistency, write durability etc.
*/
namespace oracle.kv.client.option {
    using System;
    using oracle.kv.proxy.gen;
    using oracle.kv.client.error;


    /// <summary>
    /// Consistency policy to read from a distributed, replicated database.
    /// </summary>
    /// <remarks>
    /// In distributed, replicated database environment, data can be read from 
    /// master or replica node(s).
    /// A read is performed by choosing a
    /// node (usually a Replica) from appropriate replication group, and
    /// sending it a request.  If the chosen node cannot guarantee the
    /// desired Consistency within the Consistency timeout, it replies
    /// with a failure indication.  If there is still time remaining
    /// within the operation timeout, the driver chooses another node 
    /// and retries the request. The series of actions are transparent 
    /// to the application.
    /// <para></para>
    /// All read operations support a Consistency policy,
    /// and a separate operation timeout.  
    /// <para></para>
    /// A consistency policy is specified for read operation via 
    /// <see cref="ReadOptions.Consistency"/>. 
    /// <para></para>
    /// <em>Consistency Timeout and Operation Timeout</em>
    /// 
    /// The operation timeout is the
    /// maximum amount of time the application is willing to wait for
    /// the operation to complete.  
    /// 
    /// The consistency Timeout controls how long a Replica may wait for the 
    /// desired consistency to be achieved before giving up.
    ///
    /// Note that for the Consistency timeout to be
    /// meaningful, it must be smaller than the operation timeout.
    /// 
    /// </remarks>
    public interface Consistency : ICloneable {
    }

    internal static class ConsistencyHelper {
        public static TConsistency Thrift(Consistency consistency) {
            TConsistency t = new TConsistency();
            if (consistency is SimpleConsistency) {
                t = (consistency as SimpleConsistency).Thrift;
            } else if (consistency is TimeConsistency) {
                t.Time = (consistency as TimeConsistency).Thrift;
            } else if (consistency is VersionConsistency) {
                t.Version = (consistency as VersionConsistency).Thrift;
            } else {
                throw new InternalError("can not convert consistency " + consistency + " to thrift");
            }
            return t;
        }

        public static Consistency From(TConsistency t) {
            if (t == null) {
                return new SimpleConsistency(false);
            }
            if (t.Time != null) {
                return new TimeConsistency(t.Time);
            } else if (t != null && t.Version != null) {
                return new VersionConsistency(t.Version);
            } else {
                return new SimpleConsistency(t,
                t.Simple == TSimpleConsistency.ABSOLUTE);
            }
        }

        public static string ToString(Consistency c) {
            if (c is SimpleConsistency) return "Simple";
            if (c is TimeConsistency) return "Time";
            if (c is VersionConsistency) return "Version";
            return "Unknown";
        }

    }


    /// <summary>
    /// A simple consistency can be <see cref="ABSOLUTE"/> or 
    /// <see cref="NONE_REQUIRED"/>
    /// </summary>
    public class SimpleConsistency : Thrifty<TConsistency>, Consistency {
        /// <summary>
        ///  requires that a transaction be serviced on the Master node 
        ///  so that consistency is absolute. This is the highest level
        ///  of consistency warranty.
        /// </summary>
        public static Consistency ABSOLUTE = new SimpleConsistency(true);
        /// <summary>
        /// policy that lets a transaction on a replica proceed regardless 
        /// of the state of the Replica relative to the Master. This is the 
        /// lowest level of consistency warranty.
        /// </summary>
        public static Consistency NONE_REQUIRED = new SimpleConsistency(false);


        internal SimpleConsistency(bool absolute) : this(new TConsistency(), absolute) { }


        internal SimpleConsistency(TConsistency thrift, bool absolute) : base(thrift) {
            Thrift.Simple = absolute ? TSimpleConsistency.ABSOLUTE
                : TSimpleConsistency.NONE_REQUIRED;
        }

        public bool IsAbsolute {
            get {
                return Thrift.Simple == TSimpleConsistency.ABSOLUTE;
            }
        }

        public object Clone() {
            return new SimpleConsistency(this.Thrift.Simple ==
                                         TSimpleConsistency.ABSOLUTE);
        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            if (obj.GetType() != typeof(SimpleConsistency)) return false;
            return (obj as SimpleConsistency).IsAbsolute == this.IsAbsolute;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override string ToString() {
            return string.Format("Consistency:" + (IsAbsolute ? "ABSOLUTE" : "NONE"));
        }
    }

    /// <summary>
    /// A timing-based consistency policy.
    /// </summary>
    /// <remarks>
    /// Describes the amount of time the Replica is
    /// allowed to lag the Master. An application can use this policy to ensure
    /// that the Replica node sees all transactions that were committed on the
    /// Master before the lag interval.
    /// <para></para>
    /// Effective use of this policy requires that the clocks on the Master and
    /// Replica are synchronized by using a protocol like NTP.
    /// </remarks>
    public class TimeConsistency : Thrifty<TTimeConsistency>, Consistency {
        public TimeConsistency(long lag, long timeout) : this(new TTimeConsistency()) {
            PermissibleLag = lag;
            TimeoutMs = timeout;

        }

        internal TimeConsistency(TTimeConsistency thrift) : base(thrift) {
        }

        public long TimeoutMs {
            get { return Thrift.TimeoutMs; }
            set { Thrift.TimeoutMs = value; }
        }

        public long PermissibleLag {
            get { return Thrift.PermissibleLag; }
            set { Thrift.PermissibleLag = value; }
        }

        public object Clone() {
            return new TimeConsistency(this.PermissibleLag, this.TimeoutMs);

        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            if (obj.GetType() != typeof(TimeConsistency)) return false;
            TimeConsistency other = (TimeConsistency)obj;
            return other.PermissibleLag == this.PermissibleLag
                        && other.TimeoutMs == this.TimeoutMs;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// A policy to ensure a Replica node is at least as recent as
    /// denoted by a given version.
    /// </summary>
    public class VersionConsistency : Thrifty<TVersionConsistency>, Consistency {

        public VersionConsistency(RowVersion v, long timeout) : this(new TVersionConsistency()) {
            Version = v;
            TimeoutMs = timeout;
        }

        internal VersionConsistency(TVersionConsistency thrift) : base(thrift) { }

        public long TimeoutMs {
            get { return Thrift.TimeoutMs; }
            set { Thrift.TimeoutMs = value; }
        }

        public RowVersion Version {
            get { return new RowVersionImpl(Thrift.Version); }
            set { Thrift.Version = value == null ? null : value.Bytes; }
        }


        public object Clone() {
            return new VersionConsistency(this.Version, this.TimeoutMs);
        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            if (obj.GetType() != typeof(VersionConsistency)) return false;

            VersionConsistency other = (VersionConsistency)obj;
            return this.Version.Equals(other.Version)
                && this.TimeoutMs == other.TimeoutMs;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}
/*! @} End of Doxygen Groups*/
