using System;
using NUnit.Framework;
using oracle.kv.client.data;
namespace oracle.kv.client.test {
    [TestFixture]
    public class PathTest : AbstractTest {

        [Test]
        public void testValidPaths() {

            string[] validPaths = { "abc", "ab/c","a/b/c", "a/b[7]/c",
                "valid2", "val_id", "valid_",
                "a/bc[34]/d", "a/b/c[42]", "a/b[42]/c"  };
            foreach (string p in validPaths) {
                Assert.IsTrue(PathUtil.IsValid(p), "path [" + p + "] is not valid");
            }
        }

        [Test]
        public void testInvalidPaths() {
            string[] invalidPaths = {
            null,
            "",
            "a.b[-7].c",
            "not valid", // contains a space
            "invalid!",  // conatins a ! char
            "inval[id]", // array index is not a numebr
            "2abc",      // first char is not a letter  
            "abc[34",    // missing array end bracket 
            "abc34]",    // missing array start bracket
            "abc[-34]",  // negative array index 
            "a. b"       // contains space aftre . char
            };
            foreach (string p in invalidPaths) {
                Assert.IsFalse(PathUtil.IsValid(p), "path [" + p + "] is valid,"
                    + " but should not be");

            }
        }





    }
}
