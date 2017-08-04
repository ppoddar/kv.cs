namespace oracle.kv.client {
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using oracle.kv.client.config;
    using oracle.kv.client.error;

    /// <summary>
    /// Uniform Resource Ientifer for NoSQL database.
    /// </summary>
    /// <remarks>
    /// A database URI is similar to a typical URI except 
    /// <list type="bullet">
    /// <item><description>the path must have store name 
    /// as  last segment</description>
    /// </item>
    /// <item><description>the query part is
    /// not supported. </description> </item> 
    /// </list>
    /// <para>
    /// A database uri takes the form
    ///    <code>nosql://{hostport}[,{hostport}]*/{storeName}</code>
    /// For example, <code>nosql://cloud.db:12345/db01</code>
    /// </para>
    /// The uri supports multiple hostport separated by comma,
    /// however, multiple hostport makes the uri non-standard.
    /// </remarks>
    public class DatabaseUri {
        internal string UriString { get; private set; }
        public readonly string Scheme = "nosql";
        public string HostPort { get; private set; }
        public string StoreName { get; private set; }

        private static readonly Regex UriPattern =
            new Regex(@"^nosql://(?<HostPort>.+)/(?<StoreName>.+)$");

        public static readonly string DEFAULT =
            "nosql://localhost:5000/kvstore";


        public DatabaseUri(string uriString) {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(uriString),
                "URI must not be null or empty");
            Assert.IsTrue(UriPattern.IsMatch(uriString),
                "invalid URI [" + uriString + "]");
            UriString = uriString;
            Match match = UriPattern.Match(uriString);
            HostPort = match.Groups["HostPort"].Value;
            StoreName = match.Groups["StoreName"].Value;
        }

        /// <summary>
        /// Retunrs the original URI string.
        /// </summary>
        public override string ToString() {
            return UriString;
        }
    }
}
