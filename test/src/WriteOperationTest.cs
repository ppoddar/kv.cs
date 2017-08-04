namespace oracle.kv.client.test {
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using client;
    using oracle.kv.client.option;

    /// <summary>
    /// Test basic write operations.
    /// 
    /// </summary>
    [TestFixture]
    public class WriteOperationsTest : AbstractDatbaseTest {
        protected override List<string> GetDDLStatements() {
            List<string> ddls = base.GetDDLStatements();
            ddls.Add("CREATE INDEX IF NOT EXISTS "
                + "CSHARP_INDEX on " + TEST_TABLE + " "
                + "( id, firstName, age)");
            ddls.Add("CREATE TABLE IF NOT EXISTS p1 "
                + "(shardKey INTEGER, id INTEGER, s "
                + "STRING, PRIMARY KEY( SHARD(shardKey), id))");
            ddls.Add("CREATE TABLE IF NOT EXISTS t1 "
                + "(id INTEGER, a STRING, PRIMARY "
                + " KEY (ID))");

            return ddls;

        }

        [Test]
        public void testRowWithCompositeFieldCanBeStoredAndRetrieved() {
            IKVStore store = GetDriver().GetStore();
            IRow row = store.CreateRow(COMPOSITE_TYPE_TABLE);
            long pk = DateTime.Now.Ticks; ;
            row["pk"] = pk;
            row["name"] = "composite table";
            row["age"] = 12;
            row["address"] = @"{""city"":""San Francisco"", 
                                ""state"":""CA"",
                                ""phones"":[
                                    {""work"":1234, 
                                     ""home"":5678
                                    }] 
                                }";

            store.Put(row);

            row = store.CreateRow(row.TableName,
                "{\"pk\":" + pk + "}");
            IRow fetched = store.Get(row);

            Assert.NotNull(fetched);

            Assert.AreEqual(pk, fetched["pk"]);
            Assert.AreEqual("composite table", fetched["name"]);
            Assert.AreEqual(12L, fetched["age"]);
            Assert.AreEqual("San Francisco", fetched["address/city"]);
            Assert.AreEqual("CA", fetched["address/state"]);
            Assert.AreEqual(1234L, fetched["address/phones[0]/work"]);
            Assert.AreEqual(5678L, fetched["address/phones[0]/home"]);
        }



        public void testBinaryFieldType() {
            VerifyPropertyViaDatabaseRoundTrip("binary", typeof(byte[]),
                    new byte[] { 1, 2, 3, 4, byte.MinValue, byte.MaxValue });
        }

        [Test]
        [Category("HasSchema")]
        public void testNumberFieldType() {
            VerifyPropertyViaDatabaseRoundTrip("number", typeof(decimal),
                new decimal[] { 4563, 45.6m, -9L, decimal.MaxValue, decimal.MinValue });
        }

        [Test]
        [Category("HasSchema")]
        public void testIntegerFieldType() {
            VerifyPropertyViaDatabaseRoundTrip("integer", typeof(int),
                default(int), 42, int.MinValue, int.MaxValue);
        }

        [Test]
        [Category("HasSchema")]
        public void testLongFieldType() {
            VerifyPropertyViaDatabaseRoundTrip("pk", typeof(long),
                default(long), 42L, long.MaxValue, long.MinValue);
        }


        void VerifyPropertyViaDatabaseRoundTrip<T>(string propertyName, Type expectedType,
                params T[] values) {
            IKVStore store = GetDriver().GetStore();
            for (int i = 0; i < values.Length; i++) {
                IRow row = store.CreateRow(COMPOSITE_TYPE_TABLE);
                long id = DateTime.Now.Ticks;
                row["pk"] = id;
                row[propertyName] = values[i];

                Assert.AreEqual(expectedType, row[propertyName].GetType(),
                    "Original value " + values[i] + " of type " + values[i].GetType()
                    + " is expeceted to be stored as " + expectedType + "  "
                    + " but was " + row[propertyName].GetType());



                store.Put(row);

                var pk = store.CreateRow(COMPOSITE_TYPE_TABLE);
                pk["pk"] = id;
                var row2 = store.Get(pk);
                Assert.NotNull(row2);

                object value = row2[propertyName];

                Assert.AreEqual(values[i], value);
                Assert.AreEqual(expectedType, row2[propertyName].GetType());
            }
        }


        [Test]
        public void testPutWithReturnRowReturnsPreviousValue() {
            KVDriver driver = GetDriver();
            IKVStore store = driver.GetStore();
            IRow row1 = store.CreateRow(TEST_TABLE,
                        @"{""id"":100, ""state"":""CA"", ""firstName"":""xyz""}");
            store.Put(row1);

            IRow row2 = store.CreateRow(TEST_TABLE,
                    @"{""id"":100, ""state"":""CA"", ""firstName"":""abc""}");

            Assert.AreEqual(row2["id"], row1["id"]);
            WriteOptions wOptions = driver.DefaultWriteOptions;
            wOptions.ReturnChoice = ReturnChoice.ALL;
            row2 = store.Put(row2, wOptions);

            Assert.IsNotNull(row2.Previous.Version);
            Assert.AreEqual(row2["id"], row2.Previous["id"]);
            Assert.AreEqual(row1["firstName"], row2.Previous["firstName"]);

        }

        [Test]
        public void testReturnChoiceDoesNotAlterDefaultWriteOption() {
            KVDriver datasource = GetDriver();
            IKVStore store = GetDriver().GetStore();
            WriteOptions o1 = datasource.DefaultWriteOptions;

            Assert.AreEqual(o1.ReturnChoice, ReturnChoice.NONE);
            IRow row = store.CreateRow(TEST_TABLE);
            row["id"] = 42;
            row["state"] = "MA";
            o1.ReturnChoice = ReturnChoice.VERSION;
            store.Put(row, o1);

            WriteOptions o2 = datasource.DefaultWriteOptions;
            Assert.AreEqual(o2.ReturnChoice, ReturnChoice.NONE);
        }
    }
}
