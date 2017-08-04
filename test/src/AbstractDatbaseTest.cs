using System;
using System.IO;
using System.Collections.Generic;
using oracle.kv.client.config;
using NUnit.Framework;
namespace oracle.kv.client.test {
    public abstract class AbstractDatbaseTest : AbstractTest {
        // Few table names are defined that can be used by derived tests.
        // The derived tests can define their own tables as well
        public static string TEST_TABLE = "CSHARP_TEST";
        public static string BASIC_TYPE_TABLE = "ALL_TYPES";
        public static string COMPOSITE_TYPE_TABLE = "COMPOSITE_TYPES";

        [OneTimeSetUp]
        public override void OneTimeSetUp() {
            try {
                base.OneTimeSetUp();
                var con = GetDriver().GetStore();
                DefineTables(con);
                PopulateTables(con);
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
            }
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown() {
            CleanupTables(GetDriver().GetStore());
            base.OneTimeTearDown();
            if (ProcessLauncher.ProcessLaunched != ProcessLauncher.ProcessKilled) {
                Console.WriteLine("*** WARN: Process launched:"
                + ProcessLauncher.ProcessLaunched
                + " killed:" + ProcessLauncher.ProcessKilled);
            }
        }



        protected virtual void DefineTables(IKVStore store) {
            foreach (string q in GetDDLStatements()) {
                bool res = store.ExecuteSQL(q);
                Assert.IsTrue(res, q);
            }

        }


        protected virtual void PopulateTables(IKVStore store) { }
        protected virtual void CleanupTables(IKVStore store) { }


        protected virtual List<string> GetDDLStatements() {
            List<string> queries = new List<string>(){
               "CREATE TABLE IF NOT EXISTS "
               + BASIC_TYPE_TABLE + " "
               + "(binary   BINARY,"
               + " boolean  BOOLEAN,"
               + " double   DOUBLE,"
               + " float    FLOAT,"
               + " integer  INTEGER,"
               + " long     LONG,"
               + " string   STRING,"
               + " pk      LONG,"
               + " PRIMARY KEY(pk))"
           ,
                 "CREATE TABLE IF NOT EXISTS "
                + TEST_TABLE + " "
                + "(id INTEGER, "
                + "firstName STRING, "
                + "lastName  STRING,"
                + "state     STRING,"
                + "age       INTEGER,"
                + "PRIMARY KEY(SHARD(state,id)))",

                "CREATE INDEX IF NOT EXISTS "
                + "CSHARP_INDEX on " + TEST_TABLE + " "
                + "( id, firstName, age)",

               "CREATE TABLE IF NOT EXISTS "
               + COMPOSITE_TYPE_TABLE
               + "(pk LONG, name STRING, age INTEGER,"
               + " address RECORD( "
               + "             city STRING, "
               + "             state STRING, "
               + "             phones ARRAY(RECORD(work INTEGER, home INTEGER))), "
               + " PRIMARY KEY (pk))",
        };

            return queries;
        }
    }

}
