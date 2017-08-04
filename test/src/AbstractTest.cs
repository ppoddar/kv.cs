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
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using oracle.kv.client.config;

    /// <summary>
    /// Base test for driver provides hooks for concrete test cases to
    /// define tables and populte with data at one time fixture.
    /// 
    /// The base test provides a single data source for all test cases.
    /// Provides a conenction for each test.
    /// 
    /// </summary>
    [TestFixture]
    public abstract class AbstractTest {
        Stopwatch stopWatch;
        public string testName;
        private static KVDriver driver;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp() {
            driver = CreateDriver(DatabaseUri.DEFAULT, null);
        }

        [SetUp]
        public virtual void SetUpPerTest() {
            stopWatch = Stopwatch.StartNew();
            testName = TestContext.CurrentContext.Test.FullName;
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown() {
            if (driver != null) driver.Dispose();
            driver = null;
        }



        /// <summary>
        /// Prints time taken by the test
        /// </summary>
        [TearDown]
        public virtual void TearDownPerTest() {
            if (stopWatch == null) return;
            stopWatch.Stop();
            string time = Math.Round((decimal)stopWatch.ElapsedTicks / 100)
                       .ToString("0.#");
            Console.WriteLine(string.Format("{0,10} ns\t{1}",
                      time, testName));
        }

        /// <summary>
        /// Gets or creates the driver. If a driver has been cached,
        /// then return cached driver.
        /// The driver is created with default URL and default configuration
        /// </summary>
        /// <returns>The driver.</returns>
        public KVDriver GetDriver() {
            if (driver == null) {
                driver = CreateDriver(DatabaseUri.DEFAULT, null);
            }
            return driver;
        }

        /// <summary>
        /// Gets the driver with defualt URL and given additional 
        /// options merged with default configuration.
        /// </summary>
        /// <returns>The driver.</returns>
        /// <param name="additional">Conf.</param>
        public KVDriver GetDriver(Dictionary<Option, object> additional) {
            return CreateDriver(DatabaseUri.DEFAULT, additional);
        }

        public KVDriver GetDriver(string uri) {
            return CreateDriver(uri, null);
        }



        /// <summary>
        /// Creates a driver by reading from driver.conf file.
        /// </summary>
        /// <returns>The driver.</returns>
        protected KVDriver CreateDriver(String url,
            Dictionary<Option, object> additionalOptions) {
            string testConfigFile = Path.Combine(
                Directory.GetCurrentDirectory(), "driver.conf");
            if (!File.Exists(testConfigFile)) {
                testConfigFile = "/Users/ppoddar/workspace/kv.cs/driver.conf";
            }
            if (!File.Exists(testConfigFile)) {
                throw new ArgumentException("driver configuration file "
                  + testConfigFile + " not found");
            } else {
                if (url == null) {
                    url = ReadProprty(testConfigFile, "uri", DatabaseUri.DEFAULT);
                }
                Configuration options = Configuration.FromFile(testConfigFile, true);
                //Console.WriteLine("Configuration (from file):" + options.ToString(true));
                if (additionalOptions != null) {
                    options.Merge(additionalOptions);
                }
                //Console.WriteLine("Configuration: " + options.ToString(true));

                return KVDriver.Create(url, options);

            }
        }

        /// <summary>
        /// Reads the proprty from a file ignoring comment lines.
        /// </summary>
        /// <returns>The proprty.</returns>
        /// <param name="fileName">File name.</param>
        /// <param name="key">Key.</param>
        /// <param name="def">Def.</param>
        static string ReadProprty(string fileName, string key,
            string def) {
            string[] lines = File.ReadAllLines(fileName);
            foreach (string line in lines) {
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Trim().ToCharArray()[0] == '#') continue;
                string[] tokens = line.Split('=');
                if (tokens[0].Equals(key))
                    return tokens[1];
            }
            return def;
        }
    }
}


