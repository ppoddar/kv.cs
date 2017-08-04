﻿/*-
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
using oracle.kv.client.error;
using System.Runtime.InteropServices;
namespace oracle.kv.client.option {
    /// <summary>
    /// Direction for key traversal during iteration.
    /// Avaiable directions are <see cref="FORWARD"/>, <see cref="REVERSE"/>
    /// or <see cref="UNORDERED"/>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class Direction : ICloneable {
        /// <summary>
        /// Iterate in ascending order of key 
        /// </summary>
        public static readonly Direction FORWARD = new Direction(TDirection.FORWARD);


        /// <summary>
        /// Iterate in descending order of key 
        /// </summary>
        public static readonly Direction REVERSE = new Direction(TDirection.REVERSE);

        /// <summary>
        /// Iterate without any particular order of key 
        /// </summary>
        public static readonly Direction UNORDERED = new Direction(TDirection.UNORDERED);

        internal TDirection Thrift {
            get; set;
        }

        Direction(TDirection t) {
            Thrift = t;

        }

        public object Clone() {
            if (this == FORWARD) {

                return new Direction(TDirection.FORWARD);
            } else if (this == REVERSE) {
                return new Direction(TDirection.REVERSE);
            } else if (this == UNORDERED) {
                return new Direction(TDirection.UNORDERED);

            } else {
                throw new ArgumentException("Can not clone " + this);
            }
        }

        public override string ToString() {
            if (this == Direction.FORWARD) return "FORWARD";
            if (this == Direction.REVERSE) return "REVESE";
            if (this == Direction.UNORDERED) return "UNORDERED";
            return "unknown direction";
        }
    };

}
/*! @} End of Doxygen Groups*/
