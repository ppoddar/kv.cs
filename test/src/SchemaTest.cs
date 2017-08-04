using System;
using System.Collections.Generic;
namespace oracle.kv.client.test {
    using NUnit.Framework;
    using oracle.kv.client.config;
    using oracle.kv.client.data;

    [TestFixture]
    [Description(@"Tests schema from a file")]
    public class SchemaTest : AbstractTest {

        static string SCHEMA_FILE_PATH = "../../resources/schema-test.json";
        static KVDriver ds;
        static ISchema schema;

        [OneTimeSetUp]
        public void ReadSchemaFromFile() {
            Dictionary<Option, object> options = new Dictionary<Option, object>
            {{Options.SCHEMA_RESOURCE, SCHEMA_FILE_PATH}};
            ds = GetDriver(options);
            schema = ds.DataModel.Schema;
        }

        [Test]
        [Category("HasSchema")]
        public void UsesNonEmptySchema() {
            Assert.IsFalse(schema.GetType() == typeof(EmptySchema));
        }

        [Test]
        public void TableCanBeFoundByValidName() {
            string tableName = AbstractDatbaseTest.COMPOSITE_TYPE_TABLE;
            var table = schema.GetTable(tableName, false);

            Assert.NotNull(table);
            Assert.AreEqual(tableName, table.Name);
        }

        [Test]
        [Category("HasSchema")]
        public void ThrowsExceptionIfTableIsMissing() {
            Assert.Throws<ArgumentException>(
                () => {
                    schema.GetTable("XXX", true);
                }
            );

        }

        [Test]
        [Category("HasSchema")]
        public void ReturnsNullIfTableIsMissing() {
            Assert.DoesNotThrow(
                () => {
                    var table = schema.GetTable("XXX", false);
                    Assert.IsNull(table);
                }
            );
        }


        [Test]
        [Category("HasSchema")]
        public void GetColumn() {
            var table = schema.GetTable(BasicDriverTest.COMPOSITE_TYPE_TABLE,
                    true);
            string[] columnNames = table.ColumnNames;

            Assert.IsTrue(Array.IndexOf(columnNames, "address") != -1);

            var col = table.GetColumn("address", true);

            Assert.NotNull(col);
            Assert.AreEqual("RECORD", col.DatabaseTypeName);
        }

        [Test]
        [Category("HasSchema")]
        public void GetColumnPath() {
            string tableName = BasicDriverTest.COMPOSITE_TYPE_TABLE;
            var table = schema.GetTable(tableName, true);
            string[] columnNames = table.ColumnNames;

            Assert.IsTrue(Array.IndexOf(columnNames, "address") != -1);

            var col = table.GetColumn("address", true);
            var nestedCol = table.GetColumn("address/city", true);

            Assert.NotNull(col);
            Assert.NotNull(nestedCol);
            Assert.AreEqual("RECORD", col.DatabaseTypeName);
            Assert.AreEqual("STRING", nestedCol.DatabaseTypeName);
        }
    }
}
