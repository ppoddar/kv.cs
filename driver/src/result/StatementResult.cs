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



using System;
using oracle.kv.proxy.gen;
using Thrift;
namespace oracle.kv.client {
    /// <summary>
    /// Result of a statement execution in data store. Its properties are read-only.
    /// </summary>
    internal class StatementResult : Thrifty<TStatementResultV2> {
        public StatementResult(TStatementResultV2 r) : base(r) {
        }

        // no Thrift counterpart
        public Exception Exception { get; set; }

        public string Statement {
            get { return Thrift.Statement; }
        }

        public string StringResult {
            get { return Thrift.Result.StringResult; }
        }


        public string ErrorMessage {
            get { return Thrift.ErrorMessage; }
        }

        public byte[] ExecutionId {
            get { return Thrift.ExecutionId; }
        }

        public string Info {
            get { return Thrift.Info; }
        }

        public string InfoAsJson {
            get { return Thrift.InfoAsJson; }
        }

        public bool IsCancelled {
            get { return Thrift.IsCancelled; }
        }

        public bool IsDone {
            get { return Thrift.IsDone; }
        }

        public bool IsSuccessful {
            get { return Thrift.IsSuccessful; }
        }

        public int PlanId {
            get { return Thrift.PlanId; }
        }


        internal static StatementResult FromException(TException ex) {
            var tResult = new TStatementResultV2();
            tResult = new TStatementResultV2();
            tResult.IsDone = true;
            tResult.IsSuccessful = false;

            tResult.Info = ex.ToString();
            tResult.InfoAsJson = "{\"error\":\"" + ex + "\"}";
            tResult.ExecutionId = null;

            return new StatementResult(tResult);
        }

        public static string printStatus(StatementResult r) {
            return "status[" + (r.IsDone ? "done" : "") + " "
                + (r.IsCancelled ? "cancelled" : "") + " "
                + (r.IsSuccessful ? "success" : "failed" + " "
                   + " execution=" +
                   (r.ExecutionId == null ? "null" : r.ExecutionId.Length + " bytes")
                  + "]");
        }

        public static string printStatus(TStatementResultV2 r) {
            return "status[" + (r.IsDone ? "done" : "") + " "
                + (r.IsCancelled ? "cancelled" : "") + " "
                + (r.IsSuccessful ? "success" : "failed" + " "
                  + " execution=" +
                   (r.ExecutionId == null ? "null" : r.ExecutionId.Length + " bytes")
                  + "]");
        }










    }
}
