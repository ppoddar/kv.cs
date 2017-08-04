
/**
 * Tests for execute* methods.
 */

namespace oracle.kv.client.test {
    using System;
    using System.Linq;
    using NUnit.Framework;
    using System.Collections.Generic;
    using oracle.kv.client.option;

    [TestFixture]
    public class ExecuteTests : AbstractDatbaseTest {


        [Test]
        public void testExecuteUpdates() {
            string[] rowData = {
                 @"{""shardKey"":6,""id"":600,""s"":""six hundred and zero""}",
                 @"{""shardKey"":6,""id"":601,""s"":""six hundred and one""}",
                 @"{""shardKey"":6,""id"":602,""s"":""six hundred and two""}",
           };

            IKVStore store = GetDriver().GetStore();
            foreach (string rowString in rowData) {
                var row = store.CreateRow("t3", rowString);
                store.Put(row);
            }


            var factory = store.OperationFactory;
            var ops = new List<Operation>();
            var r0 = store.CreateRow("t3", rowData[0]);
            var r1 = store.CreateRow("t3", rowData[1]);
            var r2 = store.CreateRow("t3", rowData[2]);

            ops.Add(factory.create(OperationType.PUT, r0, ReturnChoice.ALL, true));
            ops.Add(factory.create(OperationType.DELETE, r1, ReturnChoice.NONE, false));
            ops.Add(factory.create(OperationType.PUT_IF_ABSENT, r2, ReturnChoice.ALL, false));

            IRow[] res = store.ExecuteUpdates(ops, null).ToArray();

            Assert.NotNull(res);
            Assert.IsTrue(res.Length == 3);

            Assert.NotNull(res[0]);
            Assert.NotNull(res[1]);
            Assert.NotNull(res[2]);

            Assert.NotNull(res[1].Previous);


            var pk = store.CreateRow("t3");
            pk["shardKey"] = 6;
            pk["id"] = 602;
            var r = store.Get(pk, null);
            Assert.NotNull(r);
            Assert.AreEqual("six hundred and two", r["s"]);
        }

        [Test]
        public void testExecuteSync() {
            bool sr = GetDriver().GetStore().ExecuteSQL("show tables");
            Assert.True(sr);
        }

        [Test]
        public void testExecute() {

            try {
                IKVStore store = GetDriver().GetStore();
                bool sr = store.ExecuteSQL("show tables");

                Assert.NotNull(sr);

                Assert.IsTrue(sr);
            } catch (Exception e) {
                Console.WriteLine(e.StackTrace);
            }
        }

        //[Test]
        [Category("LongRunning")]
        public void testUseIndexWithoutRefresh() {

            IKVStore store = GetDriver().GetStore();
            bool sr = store.ExecuteSQL("DROP INDEX IF EXISTS name ON ExecTest_IdxTbl");

            Assert.True(sr);

            sr = store.ExecuteSQL("DROP TABLE IF EXISTS ExecTest_IdxTbl");

            Assert.True(sr);

            sr = store.ExecuteSQL("CREATE TABLE ExecTest_IdxTbl (id Integer, name String, " +
                    "PRIMARY KEY (id))");

            Assert.True(sr);

            // store data in the table
            int id = 203;

            for (int k = 0; k < 10; k++) {
                IRow row = store.CreateRow("ExecTest_IdxTbl");
                row["id"] = id + k;
                row["name"] = "John_Doe" + (id + k);
                row = store.Put(row);

                Assert.NotNull(row.Previous);
            }

            // create index on name
            sr = store.ExecuteSQL("CREATE INDEX ExecTest_name_idx ON ExecTest_IdxTbl (name)");

            Assert.True(sr);


            IRow key = store.CreateRow("ExecTest_IdxTbl");
            Assert.NotNull(key);

            // without calling refreshTables() use the index
            IRow indexKey = null;//store.CreateIndexKey("ExecTest_IdxTbl", "ExecTest_name_idx");
            IEnumerable<IRow> it = store.Search(indexKey, null);
            Assert.NotNull(it);

            Assert.AreEqual(10, it.Count());
        }

        /**
         * Tests consistency of Table metadata between 2 proxy instances using the
         * same KVStore on the back end.
         *
         * 1.  create a table
         * 2.  use that table from both Store handles
         * 3.  drop that table using one handle
         * 4.  verify that the table name can be used successfully, from both
         * handles.  This requires that the 2 proxies involved get sync'd up
         * with respect to metadata used to access that table.
         */
        [Test]
        public void testMetadataConsistency() {

            string CREATE_TABLE_QRY =
                "CREATE TABLE IF NOT EXISTS ConsistencyTable " +
                " (id integer, name string, primary key(id))";
            string DROP_TABLE_QRY = "DROP TABLE IF EXISTS ConsistencyTable";

            /* get two Store handles on the same cluster */
            IKVStore store1 = GetDriver().GetStore();
            IKVStore store2 = GetDriver().GetStore();


            /* drop and re-create the table using separte handles */
            Assert.True(store1.ExecuteSQL(DROP_TABLE_QRY));
            Assert.True(store2.ExecuteSQL(CREATE_TABLE_QRY));

            /* add data using both handles */
            addData(store1, store1);

            /* drop and re-create the table using separte handles */
            Assert.True(store1.ExecuteSQL(DROP_TABLE_QRY));
            Assert.True(store2.ExecuteSQL(CREATE_TABLE_QRY));
            /* add data using both handles */
            addData(store1, store1);
        }

        private void addData(IKVStore store1, IKVStore store2) {
            int id = 0;
            for (int i = 0; i < 10; i++) {
                var row1 = store1.CreateRow("ConsistencyTable");
                var row2 = store2.CreateRow("ConsistencyTable");
                row1["id"] = id + i; row2["id"] = id + i;
                row1["name"] = "joe"; row2["name"] = "joe";
                store1.Put(row1);
                store2.Put(row2);
                Assert.NotNull(row1.Previous);
                Assert.NotNull(row2.Previous);
            }
        }

        protected override List<string> GetDDLStatements() {
            List<string> ddls = base.GetDDLStatements();

            ddls.Add("CREATE TABLE IF NOT EXISTS t1 "
                + "(id INTEGER, a STRING, PRIMARY KEY (ID))");


            ddls.Add("CREATE TABLE IF NOT EXISTS t2 "
                + "(id INTEGER, s STRING, f FLOAT, "
                + "d DOUBLE, l LONG, bool BOOLEAN, "
                + "arrStr ARRAY(STRING), bin BINARY, "
                + "fbin BINARY(10), e ENUM (A, B, C), "
                + "PRIMARY KEY (id))");

            ddls.Add("CREATE TABLE IF NOT EXISTS t3 "
                + "(shardKey INTEGER, id INTEGER, "
                + "indexKey1 INTEGER, s STRING, "
                + "indexKey2 STRING, "
                + "PRIMARY KEY (SHARD(shardKey) , id) )");

            ddls.Add("CREATE INDEX IF NOT EXISTS t3_i1_idx "
                + "ON t3 (indexKey1)");

            ddls.Add("CREATE INDEX IF NOT EXISTS t3_i2_idx "
                + "ON t3 (indexKey2)");
            return ddls;
        }
    }
}
