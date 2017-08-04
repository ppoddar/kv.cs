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

package test;

import java.util.List;

import oracle.kv.KVStore;
import oracle.kv.KVStoreConfig;
import oracle.kv.KVStoreFactory;
import oracle.kv.client.ArrayValue;
import oracle.kv.client.Config;
import oracle.kv.client.Factory;
import oracle.kv.client.FaultException;
import oracle.kv.client.FieldRange;
import oracle.kv.client.Result;
import oracle.kv.client.Row;
import oracle.kv.client.Store;
import oracle.kv.table.PrimaryKey;
import oracle.kv.table.Table;
import oracle.kv.table.TableAPI;
import org.junit.Assert;
import org.junit.Test;

/**
 * Test performance sanity.
 */
public class PerfTest {
    public static int NO_OF_PUTS = 10000;
    public static int NO_OF_WARMUP_PUTS = NO_OF_PUTS / 5;

    public static int NO_OF_GETS = 10000;
    public static int NO_OF_WARMUP_GETS = NO_OF_GETS / 5;

    public static int NO_OF_MULTI_GETS = 1000;
    public static int NO_OF_MULTI_GET_RESULTS = 1000;
    public static int NO_OF_WARMUP_MULTI_GETS = NO_OF_MULTI_GETS / 5;

    public static int NO_OF_THREADS = 1;

    private static String longString = "long string of 100 bytes .....long " +
        "string of 100 bytes .....long string of 100 bytes .....123456789 ";

    static void putsWarmup() {
        doProxyPuts(NO_OF_WARMUP_PUTS);
        doDirectPuts(NO_OF_WARMUP_PUTS);
    }

    static void doProxyPuts(int noOfPuts) {
        Store store = test.AllTests.getNewStore();

        for (int i = 0; i < noOfPuts; i++) {
            Row row = store.createNewEmptyRow();
            row.put("id", i);
            row.put("s", longString);
            ArrayValue arr = row.putArray("arrStr");

            for (int j = 0; j < 10; j++) {
                arr.add(longString + j);
            }

            store.put("t2", row, null);
        }

        store.close();
    }

    static void doDirectPuts(int noOfPuts) {
        KVStoreConfig config = new KVStoreConfig("kvstore",
            "localhost:5000");
        KVStore store = KVStoreFactory.getStore(config);
        TableAPI tableAPI = store.getTableAPI();
        Table t = tableAPI.getTable("t2");

        for (int i = 0; i < noOfPuts; i++) {
            oracle.kv.table.Row row = t.createRow();
            row.put("id", i);
            row.put("s", longString);
            oracle.kv.table.ArrayValue arr = row.putArray("arrStr");

            for (int j = 0; j < 10; j++) {
                arr.add(longString + j);
            }

            tableAPI.put(row, null, null);
        }
        store.close();
    }

    static void getsWarmup() {
        doProxyGets(NO_OF_WARMUP_GETS);
        doDirectGets(NO_OF_WARMUP_GETS);
    }

    static void doProxyGets(int noOfGets) {
        Store store = test.AllTests.getNewStore();

        for (int i = 0; i < noOfGets; i++) {
            Row row = store.createNewEmptyRow();
            row.put("id", i);

            Result res = store.get("t2", row, null);
            Assert.assertNotNull(res);
            Assert.assertNotNull(res.getCurrentRow());
            Assert.assertEquals(i,
                res.getCurrentRow().get("id").asInteger().intValue());
            Assert.assertEquals(longString + i,
                res.getCurrentRow().get("s").asString());
            Assert.assertNotNull(res.getCurrentRow().get("arrStr"));
            ArrayValue av = res.getCurrentRow().get("arrStr").asArray();
            for (int j = 0; j < 10; j++) {
                Assert.assertEquals(longString + i + " " + j,
                    av.get(j));
            }
        }

        store.close();
    }

    static void doDirectGets(int noOfGets) {
        KVStoreConfig config = new KVStoreConfig("kvstore",
            "localhost:5000");
        KVStore store = KVStoreFactory.getStore(config);
        TableAPI tableAPI = store.getTableAPI();
        Table t = tableAPI.getTable("t2");

        for (int i = 0; i < noOfGets; i++) {
            PrimaryKey key = t.createPrimaryKey();
            key.put("id", i);

            oracle.kv.table.Row res = tableAPI.get(key, null);

            Assert.assertNotNull(res);
            Assert.assertEquals(i, res.get("id").asInteger().get());
            Assert.assertEquals(longString + i, res.get("s").asString().get());
            oracle.kv.table.ArrayValue av = res.get("arrStr").asArray();
            Assert.assertNotNull(av);

            for (int j = 0; j < 10; j++) {
                Assert.assertEquals(longString + i + " " + j,
                    av.get(j).asString().get());
            }
        }
        store.close();
    }

    @Test
    public void testPut()
        throws InterruptedException {
        Thread.sleep(100);
        System.out.flush();
        putsWarmup();

        // test using thin client through proxy server
        PutProxyThread[] threads = new PutProxyThread[NO_OF_THREADS];

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i] = new PutProxyThread();
        }

        long time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].join();
        }

        long pTime = System.currentTimeMillis() - time;

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Assert.assertFalse(threads[i].isFailing);
        }


        // test using direct kvclient
        PutDirectThread[] dThreads = new PutDirectThread[NO_OF_THREADS];

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i] = new PutDirectThread();
        }

        time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i].join();
        }

        long dTime = System.currentTimeMillis() - time;

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Assert.assertFalse(dThreads[i].isFailing);
        }


        System.out.println("Testing put\n");
        System.out.println("  Using thin java client:");

        System.out.println("    - Number of threads: " + NO_OF_THREADS +
            "    Total time: " + pTime + " ms");
        System.out.println("    - " + ((double) NO_OF_THREADS * NO_OF_PUTS)
            / pTime + " puts/ms");


        System.out.println("  Using java kvclient:");
        System.out.println("    - Number of threads: " + NO_OF_THREADS +
            "    Total time: " + dTime + " ms");
        System.out.println("    - " +
            ((double) NO_OF_THREADS * NO_OF_PUTS) / dTime + " puts/ms");
        System.out.println("  %" + (double) (pTime - dTime) / dTime * 100);

        Assert.assertTrue("Proxy put performance too low", pTime < dTime * 3);
    }

    @Test
    public void testGet()
        throws InterruptedException {
        Store store = test.AllTests.store;

        for (int i = 0; i < NO_OF_GETS; i++) {
            Row row = store.createNewEmptyRow();
            row.put("id", i);
            row.put("s", longString + i);
            ArrayValue arr = row.putArray("arrStr");

            for (int j = 0; j < 10; j++) {
                arr.add(longString + i + " " + j);
            }

            store.put("t2", row, null);
        }

        Thread.sleep(100);
        System.out.flush();
        getsWarmup();

        // test using thin client through proxy server
        GetProxyThread[] threads = new GetProxyThread[NO_OF_THREADS];

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i] = new GetProxyThread();
        }

        long time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].join();
        }

        long pTime = System.currentTimeMillis() - time;

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Assert.assertFalse(threads[i].isFailing);
        }


        // test using direct kvclient
        GetDirectThread[] dThreads = new GetDirectThread[NO_OF_THREADS];

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i] = new GetDirectThread();
        }

        time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i].join();
        }

        long dTime = System.currentTimeMillis() - time;

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Assert.assertFalse(dThreads[i].isFailing);
        }


        System.out.println("Testing get\n");
        System.out.println("  Using thin java client:");

        System.out.println("    - Number of threads: " + NO_OF_THREADS +
            "    Total time: " + pTime + " ms");
        System.out.println("    - " + (double)pTime / ((double) NO_OF_THREADS *
            NO_OF_GETS) + " ms/get");

        System.out.println("  Using java kvclient:");
        System.out.println("    - Number of threads: " + NO_OF_THREADS +
            "    Total time: " + dTime + " ms");
        System.out.println("    - " +  (double)dTime /
            ((double) NO_OF_THREADS * NO_OF_GETS) + " ms/get");
        System.out.println("  %" + (double) (pTime - dTime) / dTime * 100);

        Assert.assertTrue("Proxy get performance too low", pTime < dTime * 3);
    }

    static void multiGetWarmup() {

        doProxyMultiGets(NO_OF_WARMUP_MULTI_GETS, AllTests.store, 1);

        KVStoreConfig config = new KVStoreConfig("kvstore",
            "localhost:5000");
        KVStore kvstore = KVStoreFactory.getStore(config);
        TableAPI tableAPI = kvstore.getTableAPI();
        Table t = tableAPI.getTable("p1");

        doDirectMultiGets(NO_OF_WARMUP_MULTI_GETS, tableAPI, t, 1);

        kvstore.close();
    }

    @Test
    public void testMultiGet()
        throws InterruptedException {
        testMultiGetSetup();
        testMultiGet1();
        testMultiGet1();
        testMultiGet1();
        testMultiGet1();
        testMultiGet1();
    }

    public void testMultiGetSetup()
        throws InterruptedException {

        Store store = test.AllTests.store;

        for (int threadId = 0; threadId < NO_OF_THREADS; threadId++) {
            int shardKeyValue = 10000 * threadId;
            Row shardKey = store.createNewEmptyRow();
            shardKey.put("shardKey", shardKeyValue);

            FieldRange range = store.createNewFieldRange("id", "0",
                true, "1000000000", true);
            Assert.assertNotNull(range);

            store.multiDelete("p1", shardKey, range, null, null);
            Result res;

            for (int i = 0; i < NO_OF_MULTI_GET_RESULTS; i++) {
                Row row = store.createNewEmptyRow();
                row.put("shardKey", shardKeyValue);
                row.put("id", i);
                row.put("s", "Multi get " + i);
                res = store.put("p1", row, null);
                Assert.assertNotNull(res);
            }
        }

        Thread.sleep(100);
        System.out.flush();
        multiGetWarmup();
    }

    public void testMultiGet1()
        throws InterruptedException {

        System.out.println("Testing multiGet\n");

        // test using direct kvclient
        System.out.println("  Using java kvclient"); System.out.flush();
        MultiGetDirectThread[] dThreads = new MultiGetDirectThread[NO_OF_THREADS];

        for (int i = 0; i < NO_OF_THREADS; i++) {
            KVStoreConfig config = new KVStoreConfig("kvstore",
                "localhost:5000");
            KVStore s = KVStoreFactory.getStore(config);
            TableAPI tableAPI = s.getTableAPI();
            Table t = tableAPI.getTable("p1");

            dThreads[i] = new MultiGetDirectThread(s, tableAPI, t, i);
        }

        long time;

        // test using kvclient directly
        time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            dThreads[i].join();
        }

        long dTime = System.currentTimeMillis() - time;

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Assert.assertFalse(dThreads[i].isFailing);
            dThreads[i].close();
            dThreads[i] = null;
        }

        dThreads = null;


        // test using thin client through proxy server
        System.out.println("  Using thin java client"); System.out.flush();
        MultiGetProxyThread[] threads = new MultiGetProxyThread[NO_OF_THREADS];

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Store s = AllTests.getNewStore();
            threads[i] = new MultiGetProxyThread(s, i);
        }

        time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].join();
        }

        long pTime = System.currentTimeMillis() - time;

        for (int i = 0; i < NO_OF_THREADS; i++) {
            Assert.assertFalse(threads[i].isFailing);
            threads[i].close();
            threads[i] = null;
        }

        threads = null;


        System.out.println("  Using thin java client:");

        System.out.println("    - Number of threads: " + NO_OF_THREADS +
            "    Total time: " + pTime + " ms");
        System.out.println("    - " + (double)pTime /
            ((double) NO_OF_THREADS * NO_OF_MULTI_GETS)
             + " ms/multiget call");

        System.out.println("  Using java kvclient:");
        System.out.println("    - Number of threads: " + NO_OF_THREADS +
            "    Total time: " + dTime + " ms");
        System.out.println("    - " + (double)dTime /
            ((double) NO_OF_THREADS * NO_OF_MULTI_GETS)  + " " +
            "ms/multiget call");
        System.out.println("  %" + ((double)(pTime - dTime) / dTime * 100) +
            "   diff: " +
            ((double)(pTime - dTime)/(NO_OF_THREADS * NO_OF_MULTI_GETS)) +
            " ms/call");

        Assert.assertTrue("Proxy get performance too low", pTime < dTime * 3);

        //store.shutdown();
    }

    static void doProxyMultiGets(int numberOfCalls, Store store,
        int threadId) {

        int shardKeyValue = 10000 * threadId;
        Row shardKey = store.createNewEmptyRow();
        shardKey.put("shardKey", shardKeyValue);

        FieldRange range = store.createNewFieldRange("id", "0",
            true, "1000000000", true);
//        Assert.assertNotNull(range);

        /* call to multiGet */
        for (int i = 0; i<numberOfCalls; i++) {
            List<Result> resultList = store.multiGet("p1", shardKey, range,
                null, null);

//            Assert.assertNotNull("multiGet should not never return null",
//                resultList);
//            Assert.assertTrue("multiGet didn't return the " +
//                "expected number of results", resultList.size() == NO_OF_MULTI_GET_RESULTS);

            for (Result r : resultList) {
            }
        }
    }

    static void doDirectMultiGets(int noOfCalls, TableAPI tableAPI, Table t,
        int threadId) {

        int shardKeyValue = 10000 * threadId;
        oracle.kv.table.PrimaryKey pk = t.createPrimaryKey();
        pk.put("shardKey", shardKeyValue);

        for (int i = 0; i<noOfCalls; i++) {
//            String jpk = pk.toJsonString(false);
//            oracle.kv.table.PrimaryKey pk2 = t.createPrimaryKeyFromJson(jpk,
//                false);
//            List<oracle.kv.table.Row> results = tableAPI.multiGet(pk2, null,
//                null);
            List<oracle.kv.table.Row> results = tableAPI.multiGet(pk, null,
                null);

//            Assert.assertNotNull("multiGet should not never return null",
//                results);
//
//            Assert.assertTrue("multiGet didn't return the " +
//                "expected number of results", results.size() ==
//                NO_OF_MULTI_GET_RESULTS);

            for (oracle.kv.table.Row r : results) {
//                String json = r.toJsonString(false);
//                oracle.kv.table.Row r2 = t.createRowFromJson(json, false);
//                ByteBuffer bb = ByteBuffer.wrap(r.getVersion().toByteArray());
            }
        }
    }

    static class PutProxyThread
        extends Thread {
        volatile boolean isFailing = true;

        @Override
        public void run() {
            doProxyPuts(NO_OF_PUTS);
            isFailing = false;
        }
    }

    static class PutDirectThread
        extends Thread {
        volatile boolean isFailing = true;

        @Override
        public void run() {
            doDirectPuts(NO_OF_PUTS);
            isFailing = false;
        }
    }

    static class GetProxyThread
        extends Thread {
        volatile boolean isFailing = true;

        @Override
        public void run() {
            doProxyGets(NO_OF_GETS);
            isFailing = false;
        }
    }

    static class GetDirectThread
        extends Thread {
        volatile boolean isFailing = true;

        @Override
        public void run() {
            doDirectGets(NO_OF_GETS);
            isFailing = false;
        }
    }

    static class MultiGetProxyThread
        extends Thread {
        volatile boolean isFailing = true;
        private Store store;
        private int threadId;

        MultiGetProxyThread(Store store, int threadId) {
            this.store = store;
            this.threadId = threadId;
        }

        @Override
        public void run() {
            doProxyMultiGets(NO_OF_MULTI_GETS, store, threadId);
            isFailing = false;
        }

        public void close()
        {
            store.close();
        }
    }

    static class MultiGetDirectThread
        extends Thread {
        volatile boolean isFailing = true;
        private KVStore store;
        private TableAPI tableAPI;
        private Table table;
        private int threadId;

        MultiGetDirectThread(KVStore store, TableAPI tableAPI, Table table,
            int threadId)
        {
            this.store = store;
            this.tableAPI = tableAPI;
            this.table = table;
            this.threadId = threadId;
        }

        @Override
        public void run() {
            doDirectMultiGets(NO_OF_MULTI_GETS, tableAPI, table, threadId);
            isFailing = false;
        }

        public void close()
        {
            store.close();
        }
    }

    public static void main(String[] args)
        throws InterruptedException {
        //new PerfTest().testMultiGet();
//        testMarshall();
//        testMarshall();
//        testMarshall();
//        testMarshall();
//        testMarshall();

        long time = System.currentTimeMillis();

        testMarshalDirect();
        testMarshalDirect();
        testMarshalDirect();
        testMarshalDirect();
        testMarshalDirect();
        System.out.println("Total time: " + (System.currentTimeMillis() -
            time));
    }

    public static void testMarshall()
    {
        Store store = AllTests.store;

        long time = System.currentTimeMillis();
        for (int i = 0; i<100000000; i++) {
            Row key = store.createNewEmptyRow();
            key.put("i", 123);
            key.put("s", "abcd");
            key.toString();
        }
        System.out.println(" " + (System.currentTimeMillis() - time));
    }

    public static void testMarshalDirect()
    {
        KVStoreConfig config = new KVStoreConfig("kvstore",
                "localhost:5000");
        KVStore s = KVStoreFactory.getStore(config);
        TableAPI tableAPI = s.getTableAPI();
        Table t = tableAPI.getTable("p1");

        int shardKeyValue = 10000;
        oracle.kv.table.PrimaryKey pk = t.createPrimaryKey();
        pk.put("shardKey", shardKeyValue);
        String jpk = pk.toJsonString(false);
        oracle.kv.table.PrimaryKey pk2 = t.createPrimaryKeyFromJson(jpk, false);

        List<oracle.kv.table.Row> results = tableAPI.multiGet(pk2, null, null);

        long time = System.currentTimeMillis();
        for (int i=0; i<1; i++) {
            for (oracle.kv.table.Row r : results) {
                String json = null;
                for ( int j = 0; j < 10000; j++) {
                    //json = r.toJsonString(false);
                    r.getVersion().toByteArray();
                }
//                oracle.kv.table.Row r2 = t.createRowFromJson(json, false);
//                ByteBuffer bb = ByteBuffer.wrap(r.getVersion().toByteArray());
            }
        }
        System.out.println(" " + (System.currentTimeMillis() - time));
    }

    @Test
    public void testSimultaneousProxyStarting()
        throws InterruptedException {

        final int NO_OF_THREADS = 20;

        SimultaneousStartingThread[] threads =
            new SimultaneousStartingThread[NO_OF_THREADS];
        
        for (int i = 0 ; i < threads.length; i++) {
            threads[i] = new SimultaneousStartingThread(i);
        }

        long time = System.currentTimeMillis();

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].start();
        }

        for (int i = 0; i < NO_OF_THREADS; i++) {
            threads[i].join();
        }

        long endTime = System.currentTimeMillis() - time;

        System.out.println("testSimultaneousProxyStarting: " + endTime + "ms");

        int failing = 0;
        for (int i = 0; i < NO_OF_THREADS; i++) {
            if (threads[i].isFailing)
                failing++;
            threads[i].shutdown();
            threads[i] = null;
        }

        System.out.println("testSimultaneousProxyStarting: failing threads " +
            failing + " out of " + NO_OF_THREADS);

        threads = null;
    }

    
    static class SimultaneousStartingThread
        extends Thread {
        volatile boolean isFailing = true;
        private Store store;
        private Table table;
        private int threadId;

        SimultaneousStartingThread(int threadId)
        {
            this.threadId = threadId;
        }

        @Override
        public void run() {
            Config config = Factory.createNewConfig();
            config.setProxyPort(8010);
            config.setProxyHost("localhost");
            config.setHelperHosts("localhost:5000");
            config.setStoreName("kvstore");

            if ("true".equals(System.getProperty("test.useSecurity"))) {
                config.setSecurity("mylogin");
                config.setUsername("nonadmin");
            }

            try {
                store = Factory.open(config);
                if (store != null)
                    isFailing = false;
                else 
                    isFailing = true;
                
            } catch (FaultException fe) {
                isFailing = true;
                System.out.println("Thread " + threadId + " got an exception.");
            }
        }
        
        public void shutdown()
        {
            if (store != null) {
                System.out.println("Shutting down proxy.");
                store.shutdown();
            }
        }
    }
}
