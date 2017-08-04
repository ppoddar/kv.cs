using System;
using NUnit.Framework;
using oracle.kv.client.data;

namespace oracle.kv.client.test {
    [TestFixture]
    public class QuickTest : AbstractTest {

        [Test]
        public void testLexerReadsQuotedString() {
            string str = @"    ""hello""  sdftr";
            NativeLexer lexer = new NativeLexer(str);
            lexer.SkipWhitespace();
            lexer.GetNextToken();
            string parsed = lexer.ReadQuotedString();
            Assert.AreEqual("hello", parsed);

        }

        [Test]
        public void testLexerReadNumericString() {
            Assert.AreEqual("1234", ReadNumericString(@"    1234  sde"));
            Assert.AreEqual("1.34", ReadNumericString(@"    1.34  sde"));
            Assert.AreEqual(".123", ReadNumericString(@"    .123  sde"));
            Assert.AreEqual(".123e+08", ReadNumericString(@"    .123e+08  sde"));
            Assert.AreEqual("12.3E-02", ReadNumericString(@"    12.3E-02  sde"));

        }

        string ReadNumericString(string text) {
            NativeLexer lexer = new NativeLexer(text);
            lexer.SkipWhitespace();
            lexer.GetNextToken();
            string parsed = lexer.ReadNumericString();
            return parsed;
        }

        [Test]
        public void testLexerIgnoresWhitespace() {
            string str = @"         xz  
                    y         ";
            NativeLexer lexer = new NativeLexer(str);

            lexer.SkipWhitespace();
            Assert.AreEqual('x', lexer.GetNextToken());

            lexer.SkipWhitespace();
            Assert.AreEqual('z', lexer.GetNextToken());

            lexer.SkipWhitespace();
            Assert.AreEqual('y', lexer.GetNextToken());

        }


        public void testDynamicJSON() {
            IDataModel JsonProvider = new BaseDataModel();
            string jsonString = @"{""name"":""hello"",""number"":1234,
                ""nested"":{""x"":true},
                ""nested_array"":[{""x"":1234},{""x"":5678}]}";

            DataObject jsonObject = JsonProvider.Deserialize(jsonString) as DataObject;

            Assert.AreEqual(1234, jsonObject["number"]);
            Assert.AreEqual("hello", jsonObject["name"]);
            Assert.IsTrue(jsonObject["nested"] is DataObject);
            Assert.AreEqual(@"{""x"":true}", jsonObject["nested"].ToString());

            DataObjectArray array = jsonObject["nested_array"] as DataObjectArray;
            Assert.AreEqual(1234, (array[0] as DataObject)["x"]);
            Assert.AreEqual(5678, (array[1] as DataObject)["x"]);

        }
        //[Test]
        public void testTimeStamp() {
            DateTime userTime = DateTime.Now;
            DateTime toLatestSecond = new DateTime(
                userTime.Year, userTime.Month, userTime.Day,
                userTime.Hour, userTime.Minute, userTime.Second);
            TimeSpan ts = new TimeSpan(userTime.Ticks - toLatestSecond.Ticks);

            string str = userTime.ToString("yyyy-MM-dd HH:mm:ss" + "." + ts.Ticks);
            Console.WriteLine("Time now in UTC:" + str);
        }


    }
}
