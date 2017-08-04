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

namespace oracle.kv.client {
    using System;
    /// <summary>
    /// Base of user-visible structures that are convertible to Thrift structure.
    /// The Thrift sturutre is used as input/output argument to remote Proxy
    /// whereas user-visible structure is used by language (C#) application.
    /// 
    /// The user-visible structures wrap a Thrift counterpart which maintains
    /// the state variables. The user-visible types do not maintain state variable
    /// (unless in special cases). The accessor and mutator on user-visible types
    /// operate on state variables maintaned in wrapped Thrift instance.
    /// 
    /// </summary>
    public abstract class Thrifty<T> {

        /// <summary>
        /// Internal constructor requires a non-null Thrift structure.
        /// </summary>
        internal Thrifty(T thrift) {
            Thrift = thrift;
        }

        internal virtual T Thrift { get; set; }
    }

    /// <summary>
    /// A value backed by Thrift that can be made read-only.
    /// </summary>
    public class ImmutableThriftValue<T> : Thrifty<T> {

        internal ImmutableThriftValue(T t) : base(t) {
            Mutable = true;
        }

        private bool Mutable { get; set; }

        /// <summary>
        /// Makes this instance read only. Any attemp to change a property
        /// will raise exception. 
        /// </summary>
        internal void makeReadOnly() {
            Mutable = false;
        }

        protected void assertMutable(string property) {
            if (!Mutable) {
                throw new NotSupportedException("can not change " + property);
            }
        }
    }
}
