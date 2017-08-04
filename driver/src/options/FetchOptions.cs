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
    using config;

    /// <summary>
    /// Options for fetch operations.
    /// </summary>
    /// <remarks>
    ///
    ///
    /// </remarks>
    public class FetchOptions : ICloneable {
        private Direction direction;
        private int batchResultSize;
        private ReadOptions readOptions;
        private long readTimeout;
        private FieldRange fieldRange;
        private List<string> includedTableNames;
        private static readonly List<string> EMPTY_LIST = new List<string>();

        public FetchOptions() : this(true) { }

        internal FetchOptions(bool mutable) {
            Mutable = true;

            Direction = Direction.UNORDERED;
            ReadOptions = new ReadOptions();
            FieldRange = new FieldRange();
            IncludedTableNames = EMPTY_LIST;
            ReadTimeout = (long)Options.REQUEST_TIMEOUT.Default;
            BatchResultSize = (int)Options.ITERATOR_MAX_RESULTS_BATCHES.Default;

            Mutable = mutable;

        }

        /// <summary>
        /// Direction of traversal while iterating over rows.
        /// </summary>
        /// <remarks>
        /// Possible values are <code>Direction.FORWARD</code>, 
        /// <code>Direction.REVERSE</code> and <code>Direction.UNORDERED</code> 
        /// </remarks>
        /// <value>The direction.</value>
        public Direction Direction {
            get { return direction; }
            set { assertMutable("Direction"); direction = value; }
        }

        /// <summary>
        /// number of results to fetch in abatch result.
        /// </summary>
        /// <value>The size of the batch result.</value>
        public int BatchResultSize {
            get { return batchResultSize; }
            set {
                assertMutable("BatchResultSize");
                batchResultSize = value;
            }
        }

        /// <summary>
        /// options to read the results.
        /// </summary>
        /// <value>The read options.</value>
        public ReadOptions ReadOptions {
            get { return readOptions; }
            set { assertMutable("ReadOptions"); readOptions = value; }
        }

        public long ReadTimeout {
            get { return readTimeout; }
            set { assertMutable("ReadTimeout"); readTimeout = value; }
        }

        public FieldRange FieldRange {
            get {
                return fieldRange;
            }
            set { assertMutable("FieldRange"); fieldRange = value; }
        }

        public List<string> IncludedTableNames {
            get { return includedTableNames; }
            set { assertMutable("IncludedTableNames"); includedTableNames = value; }
        }

        private bool Mutable { get; set; }



        public object Clone() {
            FetchOptions clone = new FetchOptions();
            clone.Direction = (Direction)this.Direction.Clone();
            clone.ReadOptions = (ReadOptions)this.ReadOptions.Clone();
            clone.FieldRange = (FieldRange)this.FieldRange.Clone();
            clone.ReadTimeout = this.ReadTimeout;
            clone.IncludedTableNames.AddRange(this.IncludedTableNames);
            return clone;
        }


        internal FetchOptions makeReadOnly() {
            Mutable = false;
            return this;
        }

        private void assertMutable(string property) {
            if (!Mutable) {
                throw new NotSupportedException("can not change " + property);
            }
        }

        public override string ToString() {
            return string.Format("FetchOptions: direction={0}, "
                        + "batchResultSize={1}, readOptions={2}, "
                        + "readTimeout={3}, fieldRange={4}, "
                        + "includedTableNames={5}",
                        Direction, BatchResultSize, ReadOptions,
                        ReadTimeout, FieldRange,
                        '{' + string.Join(",", IncludedTableNames) + '}');
        }
    }

}

/*! @} End of Doxygen Groups*/
