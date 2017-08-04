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
    /// specifies choices for previous state of a row to be returned after
    /// a mutating operation.
    /// </summary>
    public enum ReturnChoice {
        /// <summary>
        /// Return all previous state including version
        /// </summary>
        ALL,

        /// <summary>
        /// Return no previous state, not even version.
        /// </summary>
        NONE,

        /// <summary>
        /// Return only previous state but not version.
        /// </summary>
        VALUE,

        /// <summary>
        /// Return previous version but no previous state.
        /// </summary>
        VERSION
    }

    internal static class ReturnChoiceMethods {
        public static TReturnChoice Thrift(this ReturnChoice c) {
            switch (c) {
                case ReturnChoice.ALL: return TReturnChoice.ALL;
                case ReturnChoice.NONE: return TReturnChoice.NONE;
                case ReturnChoice.VALUE: return TReturnChoice.ONLY_VALUE;
                case ReturnChoice.VERSION: return TReturnChoice.ONLY_VERSION;
                default: throw new InternalError("invalid convesrion of ReturnChoice:" + c);

            }
        }

        public static ReturnChoice From(this ReturnChoice dummy, TReturnChoice thrift) {
            switch (thrift) {
                case TReturnChoice.ALL: return ReturnChoice.ALL;
                case TReturnChoice.NONE: return ReturnChoice.NONE;
                case TReturnChoice.ONLY_VALUE: return ReturnChoice.VALUE;
                case TReturnChoice.ONLY_VERSION: return ReturnChoice.VERSION;
                default: return ReturnChoice.NONE;
                    // TODO: (debug) default: throw new InternalError(
                    // "invalid conversion from TReturnChoice " + thrift);
            }

        }
    }
}
/*! @} End of Doxygen Groups*/
