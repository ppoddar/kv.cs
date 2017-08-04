namespace oracle.kv.client.test {
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using client;
    using config;

    [TestFixture]
    public class ProxyServiceTest : AbstractTest {
        [Test]
        public void ProxyConnectionFailsFastForWrongConfiguration() {
            Dictionary<Option, object> options = new Dictionary<Option, object>
            {
            {Options.PROXY_HOST, "invalid host"},
            {Options.PROXY_START_ATTEMPT, 1},

            };
            Assert.Throws<ArgumentException>(() => {
                GetDriver(options);
            });

        }





        [Test]
        public void ProxyCanConnectInManagedMode() {
            Dictionary<Option, object> options = new Dictionary<Option, object>
            {{Options.PROXY_MANAGED, true}};
            KVDriver dsWithManagedProxy = GetDriver(options);

            var store = dsWithManagedProxy.GetStore();
            Assert.IsNotNull(store);
        }



        [Test]
        [NUnit.Framework.CategoryAttribute("Cloud")]
        public void testConnectToProxyInCloud() {
            Dictionary<Option, Object> additionalOptions =
                new Dictionary<Option, object>() {
                {Options.PROXY_PORT, 8772},
                {Options.PROXY_HOST, "129.146.38.252"}
                };
            string url = "nosql://Oracle_NoSQL_DB_AD1_0:5000,Oracle_NoSQL_DB_AD2_0:5000,Oracle_NoSQL_DB_AD3_0:5000/db01";
            KVDriver driver = CreateDriver(url, additionalOptions);
            Assert.NotNull(driver);
            Assert.NotNull(driver.GetStore());

        }
    }
}
