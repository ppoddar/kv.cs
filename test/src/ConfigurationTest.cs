namespace oracle.kv.client.test {
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using oracle.kv.client;
    using oracle.kv.client.config;
    using oracle.kv.client.option;
    using System.Reflection;


    [TestFixture]
    public class ConfigurationTest : AbstractTest {

        [Test]
        public void TestAssemblyPath() {
            string assemblyFile = (
            new System.Uri(Assembly.GetExecutingAssembly().CodeBase)
                ).AbsolutePath;
            Console.WriteLine("assembly file:" + assemblyFile);
        }

        [Test]
        public void OptionsCanNotBeSetWithWrongValueType() {
            Configuration conf = Configuration.Default;
            var zones = "a b c";
            Assert.Throws<ArgumentException>(() => {
                conf[Options.STORE_READ_ZONES] = zones;
            });

        }

        [Test]
        public void OptionsCanBeSetWithCorrectValueType() {
            Configuration conf = Configuration.Default;
            var zones = new string[] { "a", "b", "c" };
            conf[Options.STORE_READ_ZONES] = zones;

        }


        [Test]
        public void ValidDatabaseURIStrings() {
            string connStr = "nosql://host0:6000/s0";
            var testUri = new DatabaseUri(connStr);
            Assert.AreEqual("host0:6000", testUri.HostPort);

            connStr = "nosql://host1/s1";
            testUri = new DatabaseUri(connStr);
            Assert.AreEqual("host1", testUri.HostPort);
            Assert.AreEqual("s1", testUri.StoreName);


            connStr = "nosql://host2:5000/db01";
            testUri = new DatabaseUri(connStr);
            Assert.AreEqual("host2:5000", testUri.HostPort);
            Assert.AreEqual("db01", testUri.StoreName);

        }

        [Test]
        public void URIMustHaveHostPort() {
            Exception ex = Assert.Throws<ArgumentException>(() => {
                DatabaseUri uri = new DatabaseUri("nosql:///db01");
            });
            Assert.IsTrue(ex.Message.Contains("invalid"), ex.Message);
        }

        [Test]
        public void URIMustHavenosqlProtocol() {
            Exception ex = Assert.Throws<ArgumentException>(() => {
                DatabaseUri uri = new DatabaseUri("nosql2://localhost/db");
            });
            Assert.IsTrue(ex.Message.Contains("invalid"), ex.Message);
        }

        [Test]
        public void URIMustHaveStoreName() {
            Exception ex = Assert.Throws<ArgumentException>(() => {
                DatabaseUri uri = new DatabaseUri("nosql://localhost:5000/");
            });
            Assert.IsTrue(ex.Message.Contains("invalid"), ex.Message);
        }



        [Test]
        public void DefaultConfiguarionIsNotEqualByReference() {
            Assert.IsFalse(ReferenceEquals(
            Configuration.Default, Configuration.Default));
        }

        [Test]
        public void NewConfiguarionIsNotReadonly() {
            Assert.IsFalse(Configuration.Default.IsReadOnly);
        }

        [Test]
        public void NewConfiguarionIsNotEmpty() {
            Assert.IsFalse(Configuration.Default.Count == 0);
        }

        [Test]
        public void DefaultConfiguarionHasDefaultOptionValue() {
            Configuration conf = Configuration.Default;
            Assert.IsFalse(conf.IsReadOnly);
            Assert.IsTrue(conf.Count > 0);
            foreach (Option o in conf.Keys) {
                Assert.AreEqual(o.Default, conf[o]);
                Assert.True(o.IsDefault(conf[o]));
            }
        }

        [Test]
        public void OptionValueCanBeCastToOriginalReferenceType() {
            Configuration conf = Configuration.Default;

            Assert.IsTrue(conf[Options.OPTIONS_READ_DEFAULT] is ReadOptions);

        }

        [Test]
        public void OptionValueCanBeCastToOriginalValueType() {
            Configuration conf = Configuration.Default;

            Assert.IsTrue(conf[Options.THROUGHPUT_FLOOR] is int);

        }


        [Test]
        public void OptionsForProxy() {
            AssertOptionFlag(Options.PROXY_PORT, true, true);
            AssertOptionFlag(Options.STORE_READ_ZONES, true, false);
            AssertOptionFlag(Options.PROXY_HOST, false, true);
            AssertOptionFlag(Options.PROXY_CLASSPATH, false, false);
        }

        void AssertOptionFlag(Option o, bool IsProxyArgument, bool IsNonManagedProxyArgument) {
            Assert.AreEqual(IsProxyArgument, o.IsProxyArgument,
            o + " is proxy argument " + o.IsProxyArgument
            + " but expected " + IsProxyArgument);

            Assert.AreEqual(IsNonManagedProxyArgument, o.IsNonManagedProxyArgument,
            o + " is required for non-mamaged proxy " + o.IsNonManagedProxyArgument
            + " but expected " + IsNonManagedProxyArgument);
        }

        //        [Test]
        public void PrintConfig() {
            Console.WriteLine("default options=" + Configuration.Default.ToString());
            Console.WriteLine("exclude default options=" + Configuration.Default.ToString(true));
            Console.WriteLine("all options=" + Configuration.Default.ToString(false));
        }


    }
}
