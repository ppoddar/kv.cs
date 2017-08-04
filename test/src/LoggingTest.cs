using System;
using NUnit.Framework;
using System.Diagnostics;
using oracle.kv.client.log;
using oracle.kv.client.util;
using oracle.kv.client.config;

namespace oracle.kv.client.test {
    [TestFixture]
    public class LoggingTest {

        [Test]
        public void enumValueCanBeReadFromString() {
            object v = EnumHelper.ResolveEnum(typeof(TraceLevel), "verBose");
            Assert.AreEqual(TraceLevel.Verbose, v);

        }
        [Test]
        public void LogChannelsConfiguredBySpec() {
            string loggingSpec = "RUNTIME=INFO,Proxy=Warning";
            LogManger.Configure(loggingSpec);

            Logger logger = LogManger.GetLogger(LogChannel.RUNTIME);

            Assert.NotNull(logger);
            Assert.AreEqual(TraceLevel.Info, logger.TraceLevel);

            logger = LogManger.GetLogger(LogChannel.PROXY);
            Assert.NotNull(logger);
            Assert.AreEqual(TraceLevel.Warning, logger.TraceLevel);
        }


        [Test]
        public void LogChannelDefault() {
            LogManger.Configure(null);
            TraceLevel level = (TraceLevel)Options.LOG_LEVEL.Default;
            foreach (LogChannel channel in Enum.GetValues(typeof(LogChannel))) {
                Logger logger = LogManger.GetLogger(channel);
                Assert.NotNull(logger);
                Assert.AreEqual(level, logger.TraceLevel);
            }
        }
    }

}
