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


namespace oracle.kv.client.test {
    using System.Collections.Generic;
    using oracle.kv.client;
    using oracle.kv.client.option;
    using NUnit.Framework;

    [TestFixture]
    public class MultiGetDeleteTests : AbstractDatbaseTest {
        static string DELETE_TABLE = "DELETE_TABLE";

        public void testMultiDelete() {

            int id = 310;
            IKVStore store = GetDriver().GetStore();
            for (int i = 0; i < 10; i++) {
                IRow row = store.CreateRow(DELETE_TABLE);
                row["shardKey"] = 1;
                row["id"] = id + i;
                row["s"] = "Multi del " + i;
                row = store.Put(row, null);
                Assert.IsNotNull(row);
                Assert.IsNotNull(row.Version);
                Assert.IsNotNull(row.Previous);
            }

            /* call to delete the row */
            IRow key = store.CreateRow("");
            Assert.IsNotNull(key);
            key["shardKey"] = 1;

            FieldRange range = new FieldRange("id", "310",
                true, "319", true);
            Assert.IsNotNull(range);

            int mdRes = store.DeleteAll(key, null);

            Assert.IsTrue(mdRes == 10, "multiDelete didn't work");

            key["id"] = id;
            var res = store.Get(key, null);
            Assert.IsNull(res, "Row " + key + " exists in the store");
        }

        [Test]
        public void testMultiGetWithFieldRange() {

            int id = 330;
            KVDriver driver = GetDriver();
            IKVStore store = driver.GetStore();
            var k = store.CreateRow(DELETE_TABLE);
            k["shardKey"] = 2;
            store.DeleteAll(k, null);

            IRow res;

            for (int j = 0; j < 10; j++) {
                IRow row = store.CreateRow(DELETE_TABLE);
                row["shardKey"] = 2;
                row["id"] = id + j;
                row["s"] = "Multi get " + j;
                res = store.Put(row, null);
                Assert.IsNotNull(res);
            }

            IRow shardKey = store.CreateRow(DELETE_TABLE);
            shardKey["shardKey"] = 2;

            FieldRange range = new FieldRange("id", "329", true, "342", true);
            Assert.IsNotNull(range);

            FetchOptions options = driver.DefaultFetchOptions;
            options.FieldRange = range;
            List<IRow> resultList = store.GetAll(shardKey, options);

            Assert.IsNotNull(resultList, "multiGet should not never return null");

            Assert.AreEqual(10, resultList.Count);

            int i = 330;
            foreach (IRow r in resultList) {
                Assert.IsNotNull(r, "result should not be null");
                Assert.AreEqual(DELETE_TABLE, r.TableName);

                Assert.AreEqual(int.Parse(r["id"].ToString()), i++);

            }
        }

        [Test]
        public void testMultiGetKeys() {
            int id = 360;

            KVDriver driver = GetDriver();
            IKVStore store = driver.GetStore();
            IRow row = store.CreateRow(DELETE_TABLE);
            IRow res;

            for (int j = 0; j < 10; j++) {
                row["shardKey"] = 3;
                row["id"] = id + j;
                row["s"] = "Multi get " + j;
                res = store.Put(row);
                Assert.IsNotNull(res);
            }

            /* call to delete the row */
            IRow shardKey = store.CreateRow(DELETE_TABLE);
            shardKey["shardKey"] = 3;

            FieldRange range = new FieldRange("id", "300",
                true, "399", true);
            Assert.IsNotNull(range);

            FetchOptions options = driver.DefaultFetchOptions;
            options.FieldRange = range;
            List<IRow> resultList = store.GetAllKeys(shardKey, options);

            Assert.IsNotNull(resultList, "multiGet should not never return null");


            Assert.AreEqual(resultList.Count, 10);


            int k = 360;
            foreach (IRow r in resultList) {
                Assert.IsNotNull(r);
                Assert.AreEqual(DELETE_TABLE, r.TableName);
                Assert.AreEqual(int.Parse(r["id"].ToString()), k++);
            }
        }

        [Test]
        public void testPutWithNullOptions() {

            IKVStore store = GetDriver().GetStore();
            OperationFactory tof = store.OperationFactory;
            Assert.IsNotNull(tof);

            IRow row = store.CreateRow(DELETE_TABLE);
            Assert.IsNotNull(row);
            row["shardKey"] = 1;
            row["id"] = 1;

            Operation op = tof.create(OperationType.PUT, row, null, false);
            Assert.IsNotNull(op);

            List<Operation> opList = new List<Operation>();
            opList.Add(op);

            List<IRow> resultList = store.ExecuteUpdates(opList, null);
            Assert.IsNotNull(resultList);
            Assert.AreEqual(1, resultList.Count);

            foreach (IRow res in resultList) {
                Assert.IsNotNull(res);
            }
        }

        protected override List<string> GetDDLStatements() {
            string sql = "CREATE TABLE IF NOT EXISTS "
            + DELETE_TABLE
            + " (shardKey INTEGER, id INTEGER, s STRING,"
            + "PRIMARY KEY( SHARD(shardKey), id))";
            List<string> ddls = base.GetDDLStatements();
            ddls.Add(sql);
            return ddls;

        }
    }
}
