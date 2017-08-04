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
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    /// <summary>
    /// A pool of instances of generic type created by a factory.
    /// </summary>
    public class Pool<T> : ConcurrentBag<T> {
        internal Usage usage { get; private set; }

        /// <summary>
        /// Creates an ampty pool. 
        /// </summary>
        internal Pool() {
            usage = new Usage();

        }

        /// <summary>
        /// Acquires a pooled instance. If pool is empty, return null.
        /// </summary>
        public T Acquire() {
            T t;
            TryTake(out t);
            if (EqualityComparer<T>.Default.Equals(t, default(T))) {
                usage.Served++;
            }
            return t;
        }

        /// <summary>
        /// Releases an instance for pooling.
        /// </summary>
        /// <param name="t">T.</param>
        public void Release(T t) {
            if (EqualityComparer<T>.Default.Equals(t, default(T))) {
                Add(t);
                usage.Returned++;
            }
        }

        /// <summary>
        /// Offers a new instance to be pooled.
        /// </summary>
        /// <param name="t">T.</param>
        public void Offer(T t) {
            if (EqualityComparer<T>.Default.Equals(t, default(T))) {
                Add(t);
                usage.Created++;
            }
        }

        public void Clear() {
            T t;
            while (TryTake(out t)) { }
        }

        public override string ToString() {
            return string.Format("{0}", usage);
        }


        public class Usage {
            public int Created { get; internal set; }
            public int Served { get; internal set; }
            public int Returned { get; internal set; }

            public int Efficiency {
                get {
                    return percent(Created, Served);
                }
            }

            int percent(int a, int total) {
                return total == 0 ? 0 : (int)Math.Round(100 * (double)(total - a + 1) / total);
            }

            public override string ToString() {
                return "{" + string.Format("\"Served\":{0},"
                + "\"Created\":{1}, \"Returned\":{2},\"Efficiency\":{3}",
                Served, Created, Returned, Efficiency) + "}";
            }
        }
    }
}
