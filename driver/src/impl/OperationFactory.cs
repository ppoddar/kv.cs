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



using oracle.kv.proxy.gen;
using oracle.kv.client.data;
using oracle.kv.client.error;
using oracle.kv.client.option;
namespace oracle.kv.client {
    /// <summary>
    /// A factory to create operations.
    /// </summary>
    public class OperationFactory {
        public Operation create(OperationType type, IRow row, bool abort) {
            return new Operation(type, row, ReturnChoice.NONE, null, abort);
        }

        public Operation create(OperationType type, IRow row, ReturnChoice c, bool abort) {
            return create(type, row, c, null, abort);
        }

        public Operation create(OperationType type, IRow row, RowVersion v, bool abort) {
            return create(type, row, ReturnChoice.NONE, v, abort);

        }


        public Operation create(OperationType type, IRow row, ReturnChoice c,
                                RowVersion version, bool abort) {
            return new Operation(type, row, c, version, abort);

        }
    }

    /// <summary>
    /// Definition of a data store operation with parameters.
    /// </summary>
    public class Operation : Thrifty<TOperation> {
        internal IRow Row { get; set; }

        public OperationType Type {
            get { return OperationType.PUT.from(Thrift.Type); }
            private set { Thrift.Type = value.Thrift(); }
        }

        public ReturnChoice ReturnChoice {
            get {
                return ReturnChoice.NONE.From(Thrift.ReturnChoice);
            }
            private set {
                Thrift.ReturnChoice = value.Thrift();
            }
        }

        public RowVersion Version {
            get {
                return new RowVersionImpl(Thrift.MatchVersion);
            }
            private set {
                Thrift.MatchVersion = value == null ? null : value.Bytes;
            }
        }

        public bool AbortIfUnsuccessful { get; private set; }

        internal Operation(OperationType type, IRow r, ReturnChoice c,
                           RowVersion v, bool abort)
            : base(new TOperation()) {
            Row = r;
            Thrift.Type = type.Thrift();
            Thrift.Row = (r as RowImpl).Thrift;
            Thrift.TableName = r.TableName;
            Thrift.ReturnChoice = c.Thrift();
            Thrift.MatchVersion = v == null ? null : v.Bytes;
            Thrift.AbortIfUnsuccessful = abort;
        }
    }

    /// <summary>
    /// Enumeration of data store operations.
    /// </summary>
    public enum OperationType {
        GET, PUT, PUT_IF_PRESENT, PUT_IF_ABSENT, PUT_IF_VERSION,
        DELETE, DELETE_IF_VERSION

    };


    static class OperationTypeMethods {
        public static TOperationType Thrift(this OperationType op) {
            switch (op) {
                case OperationType.PUT: return TOperationType.PUT;
                case OperationType.PUT_IF_ABSENT: return TOperationType.PUT_IF_ABSENT;
                case OperationType.PUT_IF_PRESENT: return TOperationType.PUT_IF_PRESENT;
                case OperationType.PUT_IF_VERSION: return TOperationType.PUT_IF_VERSION;
                case OperationType.DELETE: return TOperationType.DELETE;
                case OperationType.DELETE_IF_VERSION: return TOperationType.DELETE_IF_VERSION;
                default: throw new InternalError("Invalid conversion to TOperationType from " + op);
            }
        }

        public static OperationType from(this OperationType dummy, TOperationType thrift) {
            switch (thrift) {
                case TOperationType.PUT: return OperationType.PUT;
                case TOperationType.PUT_IF_ABSENT: return OperationType.PUT_IF_ABSENT;
                case TOperationType.PUT_IF_PRESENT: return OperationType.PUT_IF_PRESENT;
                case TOperationType.PUT_IF_VERSION: return OperationType.PUT_IF_VERSION;
                case TOperationType.DELETE: return OperationType.DELETE;
                case TOperationType.DELETE_IF_VERSION: return OperationType.DELETE_IF_VERSION;

                default: throw new InternalError("Invalid conversion from TOperationType " + thrift);
            }

        }

    }
}

