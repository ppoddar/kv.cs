using System;
using System.Collections.Generic;
using oracle.kv.client.config;
using oracle.kv.client.data;
namespace oracle.kv.client.test {
    using NUnit.Framework;
    using client;

    [TestFixture]
    [Description(@"Tests a row. 
                 A row may or may not be persistable.  
                 A row must be persistable for database operation
                 A row uses schema from a file")]
    public class RowTest : AbstractTest {
        static readonly string SCHEMA_FILE_PATH = "../../resources/schema.json";
        static KVStore store;
        static KVDriver ds;

        [OneTimeSetUp]
        public void InitDataModel() {
            Dictionary<Option, object> options = new Dictionary<Option, object>
            {{Options.SCHEMA_RESOURCE,SCHEMA_FILE_PATH }};
            ds = GetDriver(options);
            store = ds.GetStore() as KVStore;
        }

        [Test]
        public void RowCanBeCreatedFromTable() {
            IRow row = store.CreateRow("ALL_TYPES");
            Assert.NotNull(row);

        }

        [Test]
        public void RowCanBeCreatedFromJSONString() {
            string jsonString = @"{""integer"":42}";
            IRow row = store.CreateRow("ALL_TYPES", jsonString);
            Assert.NotNull(row);
            Assert.AreEqual(42, row["integer"]);
        }

        [Test]
        public void RowCanBeCreatedFromDict() {
            Dictionary<string, object> dict = new Dictionary<string, object>()
            {{"integer",42}};
            IRow row = store.CreateRow("ALL_TYPES", dict);
            Assert.NotNull(row);
            Assert.AreEqual(42, row["integer"]);
        }

        [Test]
        [Category("HasSchema")]
        public void PersistableRowThrowsExceptionOnUndefindProperty() {
            IRow row = store.CreateRow("ALL_TYPES");

            Assert.IsFalse(string.IsNullOrEmpty(row.TableName));

            Assert.Throws<ArgumentException>(() => {
                row["foo"] = "not a column";
            }
            );
        }

        [Test]
        [Category("HasSchema")]
        public void PersistableRowThrowsExceptionOnWrongPropertyType() {
            IRow row = store.CreateRow("ALL_TYPES");

            Assert.IsFalse(string.IsNullOrEmpty(row.TableName));

            Assert.Throws<ArgumentException>(() => {
                row["integer"] = "valid propety name but invlid value type";
            }
            );
        }


        [Test]
        [Category("HasSchema")]
        public void PersistableEmptyRowHasNoPopulatedProperty() {
            IRow emptyRow = store.CreateRow("ALL_TYPES");
            Assert.IsTrue(emptyRow.IsDefinedProperty("integer"));
            Assert.IsFalse(emptyRow.IsPopulatedProperty("integer"));
            Assert.IsTrue(emptyRow.IsDefinedProperty("string"));
            Assert.IsFalse(emptyRow.IsPopulatedProperty("string"));
        }

        [Test]
        [Category("HasSchema")]
        public void PersistableRowFromJSONHasPopulatedProperty() {
            IRow row = store.CreateRow("ALL_TYPES",
                       @"{""integer"":42, ""string"":""cs driver""}");
            Assert.IsTrue(row.IsDefinedProperty("integer"));
            Assert.IsTrue(row.IsPopulatedProperty("integer"));
            Assert.IsTrue(row.IsDefinedProperty("string"));
            Assert.IsTrue(row.IsPopulatedProperty("string"));
        }


        [Test]
        [Category("HasSchema")]
        public void RowPopulatedWithJSONPreserveSchemaType() {
            IRow row = store.CreateRow("ALL_TYPES",
                       @"{""integer"":42, ""string"":""cs driver""}");

            Assert.AreEqual(typeof(int), row["integer"].GetType());
            Assert.AreEqual(42, row["integer"]);

            Assert.AreEqual(typeof(string), row["string"].GetType());
            Assert.AreEqual("cs driver", row["string"]);
        }

        [Test]
        public void RowPopulatedByPropertyPreservesPropertyType() {

            IRow row = store.CreateRow("");

            row["id"] = 34;
            row["name"] = "cs driver";

            Assert.AreEqual(typeof(int), row["id"].GetType());
            Assert.AreEqual(34, row["id"]);

            Assert.AreEqual(typeof(string), row["name"].GetType());
            Assert.AreEqual("cs driver", row["name"]);

        }

        [Test]
        public void RowToJSONStringIsIdenticalForPopulationStyles() {
            string jsonString = @"{""id"":42,""name"":""cs driver""}";
            IRow rowByJSON = store.CreateRow("", jsonString);

            Assert.AreEqual(jsonString, rowByJSON.ToJSONString());

            IRow rowByProperty = store.CreateRow("");
            rowByProperty["id"] = rowByJSON["id"];
            rowByProperty["name"] = rowByJSON["name"];

            Assert.AreEqual(rowByJSON.ToJSONString(), rowByProperty.ToJSONString());
        }

        [Test]
        public void RowHasNullVersionByDefault() {
            Assert.IsNull(store.CreateRow("ALL_TYPES").Version);
        }

        [Test]
        public void RowHasZeroExpirationTimeByDefault() {
            Assert.IsTrue(store.CreateRow("ALL_TYPES")
                    .ExpirationTime.CompareTo(0L) == 0);
        }

        [Test]
        public void RowHasNullPreviousRowByDefault() {
            Assert.IsNull(store.CreateRow("ALL_TYPES").Previous);
        }

        [Test]
        public void RowCanBeConstructedWithoutTable() {
            IRow row = store.CreateRow("");
            Assert.True(string.IsNullOrEmpty(row.TableName));
        }

        [Test]
        public void RowFieldCanBePopulatedWithPrimitiveArray() {
            IRow row = store.CreateRow("");
            row["array"] = new int[] { 101, 202, 303 };

            var value = row["array"];
            Assert.IsTrue(value is DataObjectArray);

            Assert.AreEqual(101, row["array[0]"]);
            Assert.AreEqual(202, row["array[1]"]);
            Assert.AreEqual(303, row["array[2]"]);

        }


        [Test]
        public void RowPropertyCanBeStructure() {
            var row = store.CreateRow("");

            row["struct"] = @"{""id"":42,""name"":""cs driver""}";

            var value = row["struct"];
            Assert.AreEqual(typeof(DataObject), value.GetType());
            Assert.AreEqual(42L, row["struct/id"]);
            Assert.AreEqual("cs driver", row["struct/name"]);
        }

        [Test]
        public void RowPropertyCanBeStructureWithNestedStructure() {
            IRow row = store.CreateRow("");
            row["struct"] = "{'id':421, 'nested':{'name':'nested cs driver'}}"
                            .Replace('\'', '"');

            var value = row["struct"];
            Assert.AreEqual(typeof(DataObject), value.GetType());
            Assert.AreEqual(421L, row["struct/id"]);
            Assert.IsTrue(row["struct/id"] is System.Int64);

            var nested = row["struct/nested"];
            Assert.AreEqual(typeof(DataObject), nested.GetType());
            Assert.AreEqual("nested cs driver", row["struct/nested/name"]);
        }

        [Test]
        public void RowPropertyCanBeStructureWithNestedArray() {
            IRow row = store.CreateRow("");
            row["struct"] = @"{""id"":422,
                               ""array"":[
                                    {""name"":""cs driver 0""}, 
                                    {""name"":""cs driver 1""}
                                    ]
                               }";

            var value = row["struct"];
            Assert.AreEqual(typeof(DataObject), value.GetType());
            Assert.AreEqual(422, row["struct/id"]);
            Assert.IsTrue(row["struct/id"] is System.Int64);

            var nestedValue = row["struct/array"];
            Assert.AreEqual(typeof(DataObjectArray), nestedValue.GetType());
            Assert.AreEqual("cs driver 0", row["struct/array[0]/name"]);
            Assert.AreEqual("cs driver 1", row["struct/array[1]/name"]);
        }

        [Test]
        public void testDecimal() {
            IRow row = store.CreateRow("");
            row["decimal"] = 42.24m;

            Assert.AreEqual(typeof(decimal), row["decimal"].GetType());
            Assert.AreEqual(42.24m, row["decimal"]);
        }

        [Test]
        public void testDateTime() {
            IRow row = store.CreateRow("");
            DateTime now = DateTime.UtcNow;
            row["datetime"] = now;
            var stored = row["datetime"];
            Assert.AreEqual(typeof(DateTime), stored.GetType());
        }

        [Test]
        public void testValidPropertyNames() {
            string[] propertyNames = { "x", "xyz", "xy_z", "x1y2_z" };
            foreach (string name in propertyNames) {
                IRow row = store.CreateRow("");
                row[name] = 42;
            }
        }

        [Test]
        public void testPropertyValueCanBeSetWithNestedPath() {
            string jsonString = @"{""a"":{""b"":""a->b""}}";
            IRow row = store.CreateRow("", jsonString);
            Assert.AreEqual("a->b", row["a/b"]);
            Assert.AreEqual(jsonString, row.ToJSONString());

        }

        [Test]
        public void testPropertyValueCanBeSetWithNestedPathUsingDict() {
            string jsonString = @"{""a"":{""b"":""a.b""}}";
            string nestedjsonString = @"{""b"":""a.b""}";
            Dictionary<string, object> dict = new Dictionary<string, object>() {
                {"a",nestedjsonString}};
            IRow row = store.CreateRow("", dict);
            Assert.AreEqual("a.b", row["a/b"]);
            Assert.AreEqual(jsonString, row.ToJSONString());
        }


        [Test]
        public void RowCanBePopulatedWithArrayOfStructAsJSON() {
            var row = store.CreateRow("");
            row["array"] = @"[{""x"":10, ""y"":20}, {""x"":30, ""y"":40}]";
            string expected = @"{""array"":[{""x"":10,""y"":20},{""x"":30,""y"":40}]}";
            Assert.AreEqual(expected, ds.DataModel.Serialize(row));
            Assert.DoesNotThrow(delegate {
                ds.DataModel.Deserialize(ds.DataModel.Serialize(row));
            });

            var array = row["array"];
            Assert.AreEqual(typeof(DataObjectArray), array.GetType());

            //  can be accessed with array indexing
            Assert.AreEqual(10L, row["array[0]/x"]);
            Assert.AreEqual(20L, row["array[0]/y"]);
            Assert.AreEqual(30L, row["array[1]/x"]);
            Assert.AreEqual(40L, row["array[1]/y"]);
        }




        [Test]
        public void GetFieldWithLambdaFunction() {
            var row = store.CreateRow("");
            row["x"] = 1234;

            // normal behavior. 
            Assert.AreEqual(1234, row["x"]);

            // hint of lambda to convert an int field value to a string with comments
            var str = row.Get<string>("x", (v) => v + " as a string");
            Assert.AreEqual(str, "1234 as a string");

            // or convert to C# native type  
            var x = row.Get<uint>("x", (v) => (uint)int.Parse(v.ToString()));
            Assert.AreEqual(typeof(uint), x.GetType());
        }

        [Test]
        public void testNonNavigableStructPathThrowsException() {
            var row = store.CreateRow("");
            row["struct"] = @"{""a"":1234, ""b"":5678}";

            Assert.Throws<ArgumentException>(delegate {
                var value = row["struct/a/b"];
            });
        }

        [Test]
        public void testArrayPathThrowsIndexOutOfRangeException() {
            var row = store.CreateRow("");
            row["array"] = @"[{""a"":1234}, {""a"":5678}]";

            Assert.Throws<ArgumentException>(delegate {
                var value = row["array[5].a"];
            });
        }

        [Test]
        public void testArrayPathThrowsNotNavigableException() {
            var row = store.CreateRow("");
            row["array"] = @"[{""a"":1234}, {""a"":5678}]";

            Assert.Throws<ArgumentException>(delegate {
                var value = row["array[0]/x"];
            });
        }

        [Test]
        [Category("HasSchema")]
        public void testUndefindRowPropertyThrowsExceptionWithSchema() {
            var row = store.CreateRow("ALL_TYPES");

            Assert.Throws<ArgumentException>(() => {
                row["extra"] = "extra value";
            });
        }

    }
}

