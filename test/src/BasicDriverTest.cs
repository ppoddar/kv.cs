namespace oracle.kv.client.test {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using oracle.kv.client;
    using oracle.kv.client.config;
    using oracle.kv.client.option;

    [TestFixture]
    public class BasicDriverTest : AbstractDatbaseTest {

        [Test]
        public void testDataSourceFailFastOnWrongUri() {
            string wrongHost = "a.b.c.d";
            string wrongUri = "nosql://a.b.c.d:6789/xyz";
            Exception ex = Assert.Throws<ArgumentException>(() => {
                GetDriver(wrongUri);
            }
            );
            Assert.True(ex.Message.Contains(wrongHost),
                "The message [" + ex.Message + "] does not contain wrong host");

        }

        [Test]
        public void testDefaultDataSourceCreatesConnection() {
            var driver = GetDriver();
            Assert.IsNotNull(driver.GetStore());
        }

        [Test]
        public void testDefaultReadOptionIsMutableButDoesNotModifyDefault() {
            var ds = GetDriver();
            var roption = ds.DefaultReadOptions;
            Assert.IsNotNull(roption);

            var defaultConsistency = roption.Consistency;
            var newConsistency = new TimeConsistency(100, 10);
            roption.Consistency = newConsistency;

            Assert.AreEqual(newConsistency, roption.Consistency);
            Assert.AreEqual(defaultConsistency, ds.DefaultReadOptions.Consistency);
        }

        [Test]
        public void testDefaultWriteOptionIsMutableButDoesNotModifyDefault() {
            var driver = GetDriver();
            var woption = driver.DefaultWriteOptions;
            Assert.IsNotNull(woption);
            long defaultTimeout = woption.TimeoutMs;
            long newTimeout = 101232;
            woption.TimeoutMs = newTimeout;


            Assert.AreEqual(newTimeout, woption.TimeoutMs);
            Assert.AreEqual(defaultTimeout, driver.DefaultWriteOptions.TimeoutMs);

        }

        [Test]
        public void testDefaultFetchOptionIsMutableButDoesNotModifyDefault() {
            var driver = GetDriver();
            var foption = driver.DefaultFetchOptions;

            string defaultName = foption.FieldRange.FieldName;

            foption.FieldRange.FieldName = "xyz";

            Assert.AreEqual("xyz", foption.FieldRange.FieldName);

            FetchOptions defaultOption = driver.DefaultFetchOptions;

            Assert.AreEqual(defaultName, defaultOption.FieldRange.FieldName);
        }


        [Test]
        public void testDriverVersionIsNotEmpty() {
            KVDriver driver = GetDriver();
            Assert.IsFalse(string.IsNullOrEmpty(driver.Version));
            Assert.IsFalse(string.IsNullOrWhiteSpace(driver.Version));
        }

        [Test]
        public void DefaultDataSourceURIMatchesDefaultConfiguration() {
            var driver = GetDriver();

            Assert.NotNull(driver.URI);
            Assert.AreEqual(DatabaseUri.DEFAULT.ToString(), driver.URI);

            var uri = new DatabaseUri(driver.URI);
            Assert.AreEqual("nosql", uri.Scheme);
            KVStore store = driver.GetStore() as KVStore;
            Assert.AreEqual(uri.StoreName, store.StoreName);
        }

        [Test]
        public void ReadonlyConfigurationIsImmutable() {
            Assert.Throws<InvalidOperationException>(() => {
                KVDriver driver = GetDriver();
                Assert.True(driver.Config.IsReadOnly);

                driver.Config[Options.ITERATOR_EXPIRATION] = 1000;
            });
        }


        [Test]
        public void DriverThrowsExceptionWithWrongReadZonesConfiguration() {
            var wrongReadZones = new string[] { "a", "b", "c" };
            var options = new Dictionary<Option, object>
                { {Options.STORE_READ_ZONES, wrongReadZones }};

            Assert.Throws<ArgumentException>(() => {
                GetDriver(options);
            });
        }

        [Test]
        public void UsesNoneConsistencyByDefault() {
            KVDriver d = GetDriver();
            Assert.AreEqual(SimpleConsistency.NONE_REQUIRED,
            d.DefaultReadOptions.Consistency);
        }

        [Test]
        public void UsesNoSyncDurabilityByDefault() {
            KVDriver d = GetDriver();
            Assert.AreEqual(Durability.COMMIT_NO_SYNC,
            d.DefaultWriteOptions.Durability);
        }



        [Test]
        public void UseAbsolueConsistencyByConfiguration() {
            ReadOptions options = new ReadOptions();
            options.Consistency = SimpleConsistency.ABSOLUTE;
            Dictionary<Option, Object> conf =
                new Dictionary<Option, Object>(){
                  {Options.OPTIONS_READ_DEFAULT, options}};
            KVDriver driver = GetDriver(conf);

            Assert.AreEqual(SimpleConsistency.ABSOLUTE,
            driver.DefaultReadOptions.Consistency);
        }

        [Test]
        public void UseAbsolueDurabilityByConfiguration() {
            WriteOptions options = new WriteOptions();
            options.Durability = Durability.COMMIT_WRITE_NO_SYNC;
            Dictionary<Option, Object> conf =
                new Dictionary<Option, Object>(){
                  {Options.OPTIONS_WRITE_DEFAULT, options}};
            KVDriver d = GetDriver(conf);

            Assert.AreEqual(Durability.COMMIT_WRITE_NO_SYNC,
            d.DefaultWriteOptions.Durability);
        }


        static StringBuilder ToString(TestParameters testParameters) {
            Console.WriteLine(testParameters.Count + " parameters for test");
            var buf = new StringBuilder('[');
            foreach (string name in testParameters.Names) {
                buf.Append(name).Append('=')
                   .Append(testParameters[name])
                   .Append(';');
            }
            return buf.Append(']');
        }

        protected override void DefineTables(IKVStore store) {
            foreach (string q in GetDDLStatements()) {
                bool res = store.ExecuteSQL(q);
                Assert.IsTrue(res, q);
            }
        }



    }

}



/*



A test must be invoked "
                + "with a database URI. The uri specified as 'uri' key."
                + " For example, to invoke a test, run \r\n\t"
                + " nant run.testcase <fqdn test classs> "
                + "-D:test.args=uri=nosql://slc04atu.us.oracle.com:5000/kvstore";
*/
