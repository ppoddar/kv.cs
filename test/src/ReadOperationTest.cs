using System.Threading;
namespace oracle.kv.client.test
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using client;
    using iterator;

    /// <summary>
    /// Tests read operations. 
    /// The onetime steup defaines and populates a table with data.
    /// The other test cases are read-only operations.

    /// </summary>
    [TestFixture]
    public class ReadOperationTest : BaseDriverTest
    {
        static readonly string[] STATES = { "CA", "MA", "FL" };
        static readonly int MAX_POPULATION = 100;
        static readonly string[] LOCATIONS = new string[MAX_POPULATION];
        static readonly int[] STATE_POPULATION = new int[STATES.Length];
        static readonly int MAX_AGE = 80;
        public static string READ_TABLE = "CSHARP_READ_TEST";

        protected override void DefineTables(IDataStore<Row> store)
        {
            
            string[] queries = {

                 "CREATE TABLE IF NOT EXISTS " + READ_TABLE
                + "(id INTEGER, "
                + "firstName STRING, "
                + "lastName  STRING,"
                + "state     STRING,"
                + "age       INTEGER,"
                + "PRIMARY KEY(SHARD(state),id))",

                "CREATE INDEX IF NOT EXISTS "
                + "CSHARP_INDEX on " + READ_TABLE
                + "( id, firstName, age)",
            };

            foreach (string q in queries)
            {
                StatementResult res = store.ExecuteSQL(q);
                Assert.IsTrue(res.IsSuccessful, res.ErrorMessage);
            }
        }


        /// <summary>
        /// Populates 
        /// </summary>
        protected override void PopulateTables(IDataStore<Row> store)
        {
            Random rng = new Random();
            for (int id = 0; id < MAX_POPULATION; id++) 
            {
                Row row = new Row(READ_TABLE);
                int stateIdx = rng.Next() % STATES.Length;

                STATE_POPULATION[stateIdx]++;
                LOCATIONS[id] = STATES[stateIdx];

                row["state"] = STATES[stateIdx];
                row["id"] = id;
                row["firstName"] = FirstNameForId(id);
                row["age"] = rng.Next() % MAX_AGE;

                store.Put(row);
                
            }
            Console.WriteLine("Populated " + MAX_POPULATION + "rows");
            Console.WriteLine("States (used for shard key)= " +  string.Join(",",STATES));
            Console.WriteLine("State Population           = " +  string.Join(",",STATE_POPULATION));
        }

        protected override void CleanupTables(IDataStore<Row> store)
        {
            string[] tables = { "CSHARP_READ_TEST" };
            foreach (string table in tables)
            {
                StatementResult res = store.ExecuteSQL(
                    "DROP TABLE IF EXISTS " + table);
            }
        }




        [Test]
        public void testGetByFullPrimaryKeyReturnsRow()
        {
            PrimaryKey pk = new PrimaryKey(READ_TABLE);
            Random rng = new Random();
            int id = rng.Next() % MAX_POPULATION;
            string state = LOCATIONS[id];;
            pk["id"]    = id;
            pk["state"] = state;

            Row row = store.Get(pk, null);

            Assert.IsNotNull(row);
            Assert.AreEqual(id,    row["id"]);
            Assert.AreEqual(state, row["state"]);
            Assert.AreEqual(FirstNameForId(id), row["firstName"]);

        }


        [Test]
        public void testGetByPartialPrimaryKeyThrowsException()
        {
            PrimaryKey pk = new PrimaryKey(READ_TABLE);
            // Missing pk field: state
            pk["id"]    = 42;
            Assert.Throws<ArgumentException>(() => { store.Get(pk); });
        }


        [Test]
        public void testGetMultipleRowsByPartialPrimaryKey()
        {
            int stateIdx = -1;
            PrimaryKey pk = createPrimaryKey(ref stateIdx);
            int statePopulation = STATE_POPULATION[stateIdx];
            //Console.WriteLine("Expected " + statePopulation + " rows for State " + STATES[stateIdx]); 
            List<Row> rows = store.GetAll(pk, null);

            Assert.AreEqual(statePopulation, rows.Count);
        }

        [Test]
        public void testGetMultipleKeysByPartialPrimaryKey()
        {
            int stateIdx = -1;
            PrimaryKey pk = createPrimaryKey(ref stateIdx);
            int statePopulation = STATE_POPULATION[stateIdx];

            List<PrimaryKey> keys = store.GetAllKeys(pk, null);


            Assert.AreEqual(statePopulation, keys.Count);
        }

        [Test]
        public void testSearchByPartialPrimaryKey()
        {
            int stateIdx = -1;
            PrimaryKey pk = createPrimaryKey(ref stateIdx);
            int statePopulation = STATE_POPULATION[stateIdx];

            IEnumerable<Row> rs = store.Search(pk, null);

            Assert.AreEqual(statePopulation, rs.Count());
        }



        [Test]
        public void testSearchResultIsLINQCompatiable()
        {
            int stateIdx = -1;
            PrimaryKey pk = createPrimaryKey(ref stateIdx);
              
            IEnumerable<Row> rs = store.Search(pk, null);
            IEnumerable<string> firstNames =
                rs.Select(r => r["firstName"]).Cast<string>();
            
        }

        [Test]
        public void testAsyncSearch()
        {
            AutoResetEvent callbackCalled = new AutoResetEvent(false);
            Func<Row, bool> printRow = delegate (Row r)
            {
                callbackCalled.Set();
                return true;
            };

            Func<Exception, bool> printError = delegate (Exception r)
            {
                callbackCalled.Set();
                return false;
            };

            int stateIdx = -1;
            PrimaryKey pk = createPrimaryKey(ref stateIdx);
           
            IObserver<Row> resultConsumer = new SimpleResultConsumer<Row>(
                printRow, printError);

            store.SearchAsync(pk, null)
                .Subscribe(resultConsumer);

            callbackCalled.WaitOne(2*1000);

        }

        PrimaryKey createPrimaryKey(ref int stateIdx) {
            PrimaryKey pk = new PrimaryKey(READ_TABLE);
            Random rng = new Random();
            stateIdx = rng.Next() % STATES.Length;
            string state = STATES[stateIdx]; ;
            pk["state"] = state;
            return pk;
       }
       
       void assertResult(IEnumerable<Row> rs, int expectedSize) {
            if (rs.Count() != expectedSize) {
                foreach (Row row in rs) {
                    Console.WriteLine(row.ToJsonString());
                }
            }
            Assert.AreEqual(expectedSize, rs.Count());
        }

        static string FirstNameForId(int id)
        {
            return "firstName-" + id;
        }
    }
}

