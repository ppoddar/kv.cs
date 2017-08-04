/*-
 *
 *  This file is part of Oracle NoSQL Database
 *  Copyright (C) 2011, 2016 Oracle and/or its affiliates.  All rights reserved.
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
using System.Collections.Generic;
using NUnit.Framework.Internal;


/**
 * Tests put and get
 */

namespace nunit
{
    using oracle.kv.client;
    using NUnit.Framework;

    [TestFixture]
    public class JavaClientTests
    {
        [Test]
        public void testExecuteUpdates()
        {
            Console.WriteLine("\n-- testUpdates");


            Result row = new Result("t3");
            row.Value("shardKey", 6);
            row.Value("id", 602);
            store.Put(row, null, null);


            OperationFactory factory = store.OperationFactory;
            List<Operation> ops = new List<Operation>();

            Result r1 = new Result("s");
            r1.Value("shardKey", 6);
            r1.Value("id", 601);
            r1.Value("s", "six hundred");

            ops.Add(factory.create(OperationType.PUT, r1, ReturnRowChoice.ALL,
            true));

            Result r2 = new Result("t3");
            r2.Value("shardKey", 6);
            r2.Value("id", 602);
            r2.Value("s", "six hundred");

            ops.Add(factory.create(OperationType.DELETE, r2,
            ReturnRowChoice.NONE, false));

            List<Result> res = store.ExecuteUpdates(ops, null);

            for (int i = 0; i < res.Count; i++)
            {
                Console.WriteLine("Res " + i + " : " + res[i]);
            }

            Console.WriteLine("\n Success\n");
        }

        [Test]
        public void testExecuteSync()
        {
            Console.WriteLine("\n-- testExecuteSync");

            try
            {
                StatementResult sr = store.ExecuteSync("show tables");
                Console.WriteLine("    " + sr);

                Console.WriteLine("\n Success\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Test]
        public void testExecute()
        {
            Console.WriteLine("\n-- testExecute");

            try
            {
                StatementResult sr = store.ExecuteSync("show tables");
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.StackTrace);
            }
        }
    }



}
