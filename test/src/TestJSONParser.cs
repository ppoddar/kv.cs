using System;
namespace oracle.kv.client.test {
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class TestJSONParser {
        public TestJSONParser() {
        }

        [Test]
        public void testLexicalNameToken() {
            Token token = Token.NAME;
            string[] validNames = {
                @"""nameQuoted""",
                @"""normalName""",
                @"""nameWith_Underscore""" };
            foreach (string s in validNames) {
                Assert.IsTrue(token.Matches(s), token + " does not match " + s);
            }
            string[] invalidNames = {
                @"nameUnquoted",
                @"""nameNotClosed",
                @"""name with spaces""",
                @"" };
            foreach (string s in invalidNames) {
                Assert.IsFalse(token.Matches(s), token + " wrongly matches " + s);
            }
        }

        [Test]
        public void testLexicalStringToken() {
            Token token = Token.STRING;
            string[] validNames = {
                    @"""stringQuoted""",
                    @"""string with spaces""",
                    @"""stringWith\tEscapeCharacter""",
                    @"""string-with-nonwordcharacters""" };

            foreach (string s in validNames) {
                Assert.IsTrue(token.Matches(s), token + " does not match " + s);
            }
        }

        [Test]
        public void testLexicalNumericToken() {
            Token token = Token.NUMBER;
            string[] validNumbers = {
                "1234", "-1234",
                "12.34", "-12.34", ".1234",
                "12e34", "12.e+34", "12e-34",};
            foreach (string s in validNumbers) {
                Assert.IsTrue(token.Matches(s), token + " does not match " + s);
            }

            string[] invalidNumbers = { "pedr1" };
            foreach (string s in invalidNumbers) {
                Assert.IsFalse(token.Matches(s), token + " wrongly match " + s);
            }
        }

        [Test]
        public void testLexicalLiteralToken() {
            Token token = Token.LITERAL;
            string[] validLiterals = { "null", "true", "false" };
            foreach (string s in validLiterals) {
                Assert.IsTrue(token.Matches(s), token + " does not match " + s);
            }
        }


        [Test]
        public void testNoProperty() {
            string jsonString = @"{}";
            object parsed = new Parser().Parse<object>(jsonString);
            Assert.AreEqual(typeof(Dictionary<string, object>), parsed.GetType());

            Dictionary<string, object> jsonObject = parsed as Dictionary<string, object>;
            Assert.AreEqual(0, jsonObject.Count);
        }



        [Test]
        public void testSingleProperty() {
            string jsonString = @"{""hello"":""JSON PARSER""}";
            object parsed = new Parser().Parse<object>(jsonString);


            Assert.AreEqual(typeof(Dictionary<string, object>), parsed.GetType());

            var jsonObject = parsed as Dictionary<string, object>;
            foreach (KeyValuePair<string, object> pair in jsonObject) {
                Console.WriteLine(pair.Key + ":" + pair.Value);
            }
            Assert.AreEqual("JSON PARSER", jsonObject["hello"]);
        }

        [Test]
        public void testMultipleProperty() {
            string jsonString = @"{""p1"":""v1"",""p2"":""v2""}";
            object parsed = new Parser().Parse<object>(jsonString);


            Assert.AreEqual(typeof(Dictionary<string, object>), parsed.GetType());

            var jsonObject = parsed as Dictionary<string, object>;
            foreach (KeyValuePair<string, object> pair in jsonObject) {
                Console.WriteLine(pair.Key + ":" + pair.Value);
            }
            Assert.AreEqual("v1", jsonObject["p1"]);
            Assert.AreEqual("v2", jsonObject["p2"]);
            Assert.AreEqual(2, jsonObject.Count);
        }

        [Test]
        public void testNumericProperty() {
            string jsonString = @"{""p"":1234}";
            object parsed = new Parser().Parse<object>(jsonString);


            Assert.AreEqual(typeof(Dictionary<string, object>), parsed.GetType());

            var jsonObject = parsed as Dictionary<string, object>;
            foreach (KeyValuePair<string, object> pair in jsonObject) {
                Console.WriteLine(pair.Key + ":" + pair.Value);
            }
            Assert.AreEqual(1234, jsonObject["p"]);
            Assert.AreEqual(1, jsonObject.Count);
        }

        [Test]
        public void testNestedroperty() {
            string jsonString = @"{""nested1"":{""p"":""x""}, ""p"":""y"",""nested2"":{""p"":""z""}}";
            object parsed = new Parser().Parse<object>(jsonString);


            Assert.AreEqual(typeof(Dictionary<string, object>), parsed.GetType());

            var jsonObject = parsed as Dictionary<string, object>;

            print(jsonObject);

            var nested = jsonObject["nested1"] as Dictionary<string, object>;


            Assert.AreEqual("x", nested["p"]);
            Assert.AreEqual(1, nested.Count);


            nested = jsonObject["nested2"] as Dictionary<string, object>;

            Assert.AreEqual("z", nested["p"]);
            Assert.AreEqual(1, nested.Count);

        }

        void print(Dictionary<string, object> jsonObject) {
            print(jsonObject, 0);
        }

        void print(Dictionary<string, object> jsonObject, int tab) {
            foreach (KeyValuePair<string, object> pair in jsonObject) {
                for (int i = 0; i < tab; i++) Console.Write("   ");
                Console.Write(pair.Key + ":");
                if (pair.Value is Dictionary<string, object>) {
                    print(pair.Value as Dictionary<string, object>, tab + 1);
                } else {
                    Console.WriteLine(pair.Value);
                }

            }
        }


    }
}
