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
namespace oracle.kv.client.option {
    /// <summary>
    /// Options for multi row read operation.
    /// Unlike Read/Write Options, this is not based on underlying Thrift structure. 
    /// The state of the options are held in property of this instance itself.
    /// </summary>
    internal class MultiRowOptions : ICloneable {
        private static List<string> EMPTY_LIST = new List<string>();
        private FieldRange _fieldRange;
        private List<string> _includedParentTables;
        private List<string> _includedChildTables;

        public MultiRowOptions() : this(true) { }

        internal MultiRowOptions(bool mutable) {
            _fieldRange = new FieldRange(new proxy.gen.TFieldRange());
            _includedParentTables = EMPTY_LIST;
            _includedChildTables = EMPTY_LIST;
            Mutable = mutable;
        }

        public FieldRange FieldRange {
            get { return _fieldRange; }
            set { assertMutable("field range"); _fieldRange = value; }
        }

        public List<string> IncludedParentTables {
            get { return _includedParentTables; }
            set { assertMutable("included parent tables"); _includedParentTables = value; }
        }

        public List<string> IncludedChildTables {
            get { return _includedChildTables; }
            set { assertMutable("included child tables"); _includedChildTables = value; }
        }

        private bool Mutable { get; set; }

        private void assertMutable(string property) {
            if (!Mutable) {
                throw new NotSupportedException("can not change " + property);
            }
        }

        public object Clone() {
            var clone = new MultiRowOptions(true);
            clone.FieldRange = FieldRange;
            clone.IncludedParentTables = IncludedParentTables;
            clone.IncludedChildTables = IncludedChildTables;

            return clone;
        }



    }

}
/*! @} End of Doxygen Groups*/
