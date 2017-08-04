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
using System.Collections.Generic;
using oracle.kv.proxy.gen;


namespace oracle.kv.client.option {
    /// <summary>
    /// A specification used to restict the range of search  operation.
    /// </summary>
    public class FieldRange : Thrifty<TFieldRange>, ICloneable {
        public FieldRange() : this("", "0", true, "0", true) { }

        /// <summary>
        /// </summary>
        /// @param fieldName      the name for the field used in the range.
        /// @param start          the start value of the range to the specified
        ///                       string value.
        /// @param startInclusive set to true if the range is inclusive of the
        /// value, false if it is exclusive.
        /// @param end the end value of the range to the specified string
        /// value.
        /// @param endInclusive   set to true if the range is inclusive of the
        /// value, false if it is exclusive.

        public FieldRange(string fieldName, string start, bool startInclusive,
                          string end, bool endInclusive) : base(new TFieldRange()) {
            Thrift.EndIsInclusive = endInclusive;
            Thrift.FieldName = fieldName;
            Thrift.StartValue = start;
            Thrift.EndValue = end;
            Thrift.StartIsInclusive = startInclusive;

        }

        internal FieldRange(TFieldRange range) : base(range) { }

        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        /// <value>The name of the field.</value>
        public string FieldName {
            get { return Thrift.FieldName; }
            set { Thrift.FieldName = value; }
        }

        public string StartValue {
            get { return Thrift.StartValue; }
            set { Thrift.StartValue = value; }

        }

        public string EndValue {
            get { return Thrift.EndValue; }
            set { Thrift.EndValue = value; }

        }


        public bool EndIsInclusive {
            get { return Thrift.EndIsInclusive; }
            set { Thrift.EndIsInclusive = value; }
        }

        public bool StartIsInclusive {
            get { return Thrift.StartIsInclusive; }
            set { Thrift.StartIsInclusive = value; }
        }

        public object Clone() {
            return new FieldRange(this.FieldName,
                                  this.StartValue,
                                  this.StartIsInclusive,
                                  this.EndValue,
                                  this.EndIsInclusive);
        }

        public override string ToString() {
            return (StartIsInclusive ? '(' : '[')
                 + FieldName
                 + (EndIsInclusive ? ')' : ']');
        }
    }
}
/*! @} End of Doxygen Groups*/
