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
    /// A time-to-live is combination of a duration and time unit.
    /// </summary>
    public class TimeToLive : Thrifty<TTimeToLive> {
        private static long MS_PER_HOUR = 60 * 60 * 1000;
        private static int HOUR_PER_DAY = 24;
        private static long DO_NOT_EXPIRE = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:oracle.kv.TimeToLive"/> class
        /// from an equivalent Thrift structure. 
        /// </summary>
        /// <param name="ttl">Ttl.</param>
        internal TimeToLive(TTimeToLive ttl) : base(ttl) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:oracle.kv.TimeToLive"/> class
        /// from given Value and time unit.
        /// </summary>
        /// <param name="Value">Value of time in given unit.</param>
        /// <param name="Unit">Unit of time can be eitehr HOUR or DAY.</param>
        public TimeToLive(long Value, TimeUnit Unit) : base(new TTimeToLive()) {
            switch (Unit) {
                case TimeUnit.DAYS: Thrift.TimeUnit = TTimeUnit.DAYS; break;
                case TimeUnit.HOURS: Thrift.TimeUnit = TTimeUnit.HOURS; break;
            }
            Thrift.Value = Value;
        }


        /// <summary>
        /// Gets the expiration time in UTC calculated as a sum of 
        /// Time-To-Live (duration) and current UTC time.
        /// 
        /// </summary>
        /// <value>The expiration time as UTC.</value>
        internal static long asExpirationTime(TTimeToLive ttl) {
            if (ttl == null) return DO_NOT_EXPIRE;
            return DateTime.UtcNow.Ticks * 10
                           + (ttl.TimeUnit == TTimeUnit.DAYS ? HOUR_PER_DAY : 1)
                           * MS_PER_HOUR * ttl.Value;
        }

        internal static TTimeToLive asThrift(long UTCTime) {
            if (UTCTime == 0) return null;

            long duration = UTCTime - DateTime.UtcNow.Ticks * 10;
            long hours = duration / MS_PER_HOUR;
            bool inDays = hours >= HOUR_PER_DAY && hours % HOUR_PER_DAY == 0;

            TTimeToLive result = new TTimeToLive();
            result.TimeUnit = inDays ? TTimeUnit.DAYS : TTimeUnit.HOURS;
            result.Value = inDays ? hours / HOUR_PER_DAY : hours;
            return result;
        }
    }
}

/*! @} End of Doxygen Groups*/
