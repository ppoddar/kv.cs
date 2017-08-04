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

import java.util.ArrayList;
import java.util.List;
import oracle.kv.client.FieldRange;
import oracle.kv.client.MapValue;
import oracle.kv.client.ReadOptions;
import oracle.kv.client.Result;
import oracle.kv.client.ResultIterator;
import oracle.kv.client.Row;
import oracle.kv.client.Store;
import oracle.kv.client.TimeToLive;
import org.junit.Assert;
import org.junit.Test;

/**
 * Class to test Iterators
 */
public class TestIterators {
    private static final int NO_OF_ITEMS = 1000;

    @Test
    public void testTableIterator() {
        Store store = AllTests.store;

        Row k = store.createNewEmptyRow();
        k.put("shardKey", 1);
        store.multiDelete("p1", k, null, null, null);

        long time = System.currentTimeMillis();
        for (int i = 0; i < NO_OF_ITEMS; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", 1);
            row.put("id", i);
            row.put("s", "row: " + i);
            store.put("p1", row, null);
        }
        time = System.currentTimeMillis() - time;
        System.out.println("   " + ((double) NO_OF_ITEMS / time) + " puts/ms");

        Row key = store.createNewEmptyRow();
        key.put("shardKey", 1);

        time = System.currentTimeMillis();
        ResultIterator it = store.tableIterator("p1", key, null, null,
            null,
            null);
        int counter = 0;

        while (it.hasNext()) {
            Result r = (Result) it.next();
            //System.out.println("  - " + r.getCurrentRow());
            Assert.assertEquals(
                r.getCurrentRow().get("shardKey").asInteger().intValue(), 1);
            Assert.assertEquals(
                r.getCurrentRow().get("id").asInteger().intValue(),
                counter);
            Assert.assertEquals(r.getCurrentRow().get("s").asString(),
                "row: " + counter);
            counter++;
        }
        time = System.currentTimeMillis() - time;
        System.out.println(
            "   " + ((double) NO_OF_ITEMS / time) + " iterator-items/ms");

        Assert.assertEquals(counter, NO_OF_ITEMS);
    }


    @Test
    public void testKeysIterator() {
        Store store = AllTests.store;

        for (int i = 0; i < 10; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", 2);
            row.put("id", i);
            row.put("s", "row: " + i);
            store.put("p1", row, null);
        }

        Row key = store.createNewEmptyRow();
        Assert.assertNotNull(key);

        ResultIterator it = store.tableKeysIterator("p1", key,
            null, null, null, null);
        Assert.assertNotNull(it);

        int i = 0;
        while (it.hasNext()) {
            Result r = (Result) it.next();
            i++;
        }

        it.close();
        Assert.assertTrue(i > 0);
    }


    @Test
    public void testIndexIterator() {
        Store store = AllTests.store;
        int id = 100;
        Result res;

        for (int i = 0; i < 10; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", i);
            row.put("id", id + i);
            row.put("indexKey1", id + i);
            row.put("s", "test index iterator " + i);
            row.put("indexKey2", "indexKey2: test index iterator " + i);
            res = store.put("t3", row, null);

            Assert.assertNotNull(res);
        }

        Row key = store.createNewEmptyRow();
        Assert.assertNotNull(key);

        ResultIterator it = store.indexIterator("t3", "t3_i1_idx", key,
            null, null, null, null);
        Assert.assertNotNull(it);

        int i = 0;
        while (it.hasNext()) {
            Result r = it.next();
            i++;
        }

        Assert.assertTrue(i > 0);
    }


    private static long EXPIRATION_HOUR_ERROR = 1000 * 60 * 60 + 1000 * 60;

    @Test
    public void testIndexIteratorWithTtl() {
        Store store = AllTests.store;
        int id = 130;
        Result res;

        long time = System.currentTimeMillis();
        TimeToLive ttl = TimeToLive.Factory.ofHours(6);

        for (int i = 0; i < 10; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", i);
            row.put("id", id + i);
            row.put("indexKey1", id + i);
            row.put("s", "test index iterator with ttl " + i);
            row.put("indexKey2", "indexKey2: test index iterator " + i);
            row.setTTL(ttl);
            Assert.assertNotNull(row.getTTL());
            Assert.assertEquals(ttl, row.getTTL());

            res = store.put("t3", row, null);

            Assert.assertNotNull("Unexpected null result", res);
            Assert.assertEquals("Unexpected result expiration time",
                ttl.toExpirationTime(time),
                res.getExpirationTime(), EXPIRATION_HOUR_ERROR);
        }

        Row key = store.createNewEmptyRow();
        Assert.assertNotNull(key);

        // Use field range because this table is used by other tests too
        FieldRange fr = store.createNewFieldRange("indexKey1", "130", true,
            "140", false);

        ResultIterator it = store.indexIterator("t3", "t3_i1_idx", key,
            fr, null, null, null);
        Assert.assertNotNull(it);

        int i = 0;
        while (it.hasNext()) {
            Result r = it.next();
            i++;

            Assert.assertEquals(ttl.toExpirationTime(time),
                r.getExpirationTime(), EXPIRATION_HOUR_ERROR);
        }

        Assert.assertTrue(i > 0);
    }

    @Test
    public void testIndexKeysIterator() {
        Store store = AllTests.store;
        int id = 200;
        Result res;

        for (int i = 0; i < 10; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", 20 + i);
            row.put("id", id + i);
            row.put("indexKey1", id + i);
            row.put("s", "test index key iterator " + i);
            row.put("indexKey2", "indexKey2: test index key iterator " +
                i);
            res = store.put("t3", row, null);
            Assert.assertNotNull(res);
        }

        Row key = store.createNewEmptyRow();
        Assert.assertNotNull(key);

        ResultIterator it =
            store.indexKeysIterator("t3", "t3_i2_idx", key,
                null, null, null, null);
        Assert.assertNotNull(it);

        int i = 0;
        while (it.hasNext()) {
            Result r = it.next();
            i++;
        }

        Assert.assertTrue(i > 0);
    }

    @Test
    public void testMapIndex() {
        Store store = AllTests.store;

        /* create a populate a table with a map index */
        populateMapTable(store);

        Row key = store.createNewEmptyRow();
        ResultIterator it =
            store.indexKeysIterator("MapTable", "MapIndex", key,
                null, null, null, null);

        int i = 0;
        while (it.hasNext()) {
            Row row = it.next().getCurrentRow();
            i++;
        }
        /* 2 map entries for each row, and there are 20 rows */
        Assert.assertTrue(i == 40);
    }

    @Test
    public void testRecordIndex() {
        Store store = AllTests.store;

        /* create a populate a table with a map index */
        populateRecordTable(store);

        Row key = store.createNewEmptyRow();
        ResultIterator it =
            store.indexKeysIterator("RecordTable", "RecordIndex", key,
                null, null, null, null);

        int i = 0;
        while (it.hasNext()) {
            Row row = it.next().getCurrentRow();
            i++;
        }
        /* 1 entry for each row, and there are 20 rows */
        Assert.assertTrue(i == 20);
    }

    @Test
    public void testIteratorWithNullKey() {
        Store store = AllTests.store;

        Row row = store.createNewEmptyRow();
        row.put("shardKey", 1);
        row.put("id", 1);
        row.put("s", "row: " + 1);
        store.put("p1", row, null);

        try {
            Row key = null;
            ResultIterator it = store.tableIterator("p1", key, null, null,
                null, null);

            Assert.assertNotNull(it);

            int counter = 0;
            while (it.hasNext()) {
                Result r = (Result) it.next();
                //System.out.println("  - " + r.getCurrentRow());
                counter++;
            }

            Assert.assertTrue(counter > 0);
        }
        catch (IllegalArgumentException e) {
            Assert.assertTrue("tableIterator with null key throws " +
                "IllegalArgumentException", false);
        }
    }

    @Test
    public void testKeysIteratorWithNullKey() {
        Store store = AllTests.store;

        Row row = store.createNewEmptyRow();
        row.put("shardKey", 1);
        row.put("id", 1);
        row.put("s", "row: " + 1);
        store.put("p1", row, null);

        try {
            Row key = null;
            ResultIterator it = store.tableKeysIterator("p1", key, null, null,
                null, null);

            Assert.assertNotNull(it);

            int counter = 0;
            while (it.hasNext()) {
                Result r = (Result) it.next();
                //System.out.println("  - " + r.getCurrentRow());
                counter++;
            }

            Assert.assertTrue(counter > 0);
        }
        catch (IllegalArgumentException e) {
            Assert.assertTrue("tableKeyIterator with null key throws " +
                "IllegalArgumentException", false);
        }
    }

    @Test
    public void testIndexIteratorWithNullKey() {
        Store store = AllTests.store;

        Row row = store.createNewEmptyRow();
        row.put("shardKey", 1);
        row.put("id", 1);
        row.put("indexKey1", 1);
        row.put("s", "row: " + 1);
        store.put("t3", row, null);

        try {
            Row key = null;
            ResultIterator it = store.indexIterator("t3", "t3_i1_idx", key,
                null, null, null, null);

            Assert.assertNotNull(it);

            int counter = 0;
            while (it.hasNext()) {
                Result r = (Result) it.next();
                //System.out.println("  - " + r.getCurrentRow());
                counter++;
            }

            Assert.assertTrue(counter > 0);
        }
        catch (IllegalArgumentException e) {
            Assert.assertTrue("indexIterator with null key throws " +
                "IllegalArgumentException", false);
        }
    }

    @Test
    public void testIndexKeysIteratorWithNullKey() {
        Store store = AllTests.store;

        Row row = store.createNewEmptyRow();
        row.put("shardKey", 1);
        row.put("id", 1);
        row.put("indexKey1", 1);
        row.put("s", "row: " + 1);
        store.put("t3", row, null);

        try {
            Row key = null;
            ResultIterator it = store.indexKeysIterator("t3", "t3_i1_idx", key,
                null, null, null, null);

            Assert.assertNotNull(it);

            int counter = 0;
            while (it.hasNext()) {
                Result r = (Result) it.next();
                //System.out.println("  - " + r.getCurrentRow());
                counter++;
            }

            Assert.assertTrue(counter > 0);
        }
        catch (IllegalArgumentException e) {
            Assert.assertTrue("indexKeysIterator with null key throws " +
                "IllegalArgumentException", false);
        }
    }

    @Test
    public void testTableIteratorMulti() {
        Store store = AllTests.store;

        Row k = store.createNewEmptyRow();
        k.put("shardKey", 7);
        store.multiDelete("p1", k, null, null, null);
        k.put("shardKey", 8);
        store.multiDelete("p1", k, null, null, null);

        long time = System.currentTimeMillis();
        for (int i = 0; i < NO_OF_ITEMS; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey",  i % 2 + 7);
            row.put("id", i);
            row.put("s", "row: " + i);
            store.put("p1", row, null);
        }
        time = System.currentTimeMillis() - time;
        System.out.println("   " + ((double) NO_OF_ITEMS*2 / time) + " " +
            "puts/ms");

        Row key1 = store.createNewEmptyRow();
        key1.put("shardKey", 7);
        Row key2 = store.createNewEmptyRow();
        key2.put("shardKey", 8);
        List<Row> keys = new ArrayList<Row>();
        keys.add(key1);
        keys.add(key2);

        time = System.currentTimeMillis();
        ResultIterator it = store.tableIterator("p1", keys, null, null,
            null, null);
        int counter = 0;

        while (it.hasNext()) {
            Result r = it.next();
            //System.out.println("  - " + r.getCurrentRow());
            int shardKey = r.getCurrentRow().get("shardKey").asInteger()
                .intValue();
            Assert.assertTrue( shardKey == 7 || shardKey == 8);
            Assert.assertEquals(shardKey,
                r.getCurrentRow().get("id").asInteger().intValue() % 2 + 7);
            Assert.assertTrue(r.getCurrentRow().get("s").asString()
                .startsWith("row:"));
            counter++;
        }
        time = System.currentTimeMillis() - time;
        System.out.println(
            "   " + ((double) NO_OF_ITEMS / time) + " iterator-items/ms");

        Assert.assertEquals(counter, NO_OF_ITEMS);
    }


    @Test
    public void testKeysIteratorMulti() {
        Store store = AllTests.store;

        for (int i = 0; i < 10; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", i % 2 + 5);
            row.put("id", i);
            row.put("s", "row: " + i);
            store.put("p1", row, null);
        }

        Row key1 = store.createNewEmptyRow();
        key1.put("shardKey", 5);
        Assert.assertNotNull(key1);
        Row key2 = store.createNewEmptyRow();
        key2.put("shardKey", 6);
        Assert.assertNotNull(key2);

        List<Row> keys = new ArrayList<>();
        keys.add(key1);
        keys.add(key2);

        ResultIterator it = store.tableKeysIterator("p1", keys,
            null, null, null, null);
        Assert.assertNotNull(it);

        int i = 0;
        while (it.hasNext()) {
            Result r = (Result) it.next();
            i++;
        }

        it.close();
        Assert.assertTrue(i > 0);
    }


    @Test
    public void testMtTableIterator()
        throws InterruptedException {
        testMtIterator(ClientThread.Kind.TABLE_ITERATOR);
    }

    @Test
    public void testMtTableKeysIterator()
        throws InterruptedException {
        testMtIterator(ClientThread.Kind.TABLE_KEYS_ITERATOR);
    }

    @Test
    public void testMtTIndexIterator()
        throws InterruptedException {
        testMtIterator(ClientThread.Kind.INDEX_ITERATOR);
    }

    @Test
    public void testMtTIndexKeysIterator()
        throws InterruptedException {
        testMtIterator(ClientThread.Kind.INDEX_KEYS_ITERATOR);
    }

    public void testMtIterator(ClientThread.Kind kind)
        throws InterruptedException {
        System.out.println("- testMtIterator: " + kind);
        Store store = AllTests.store;

        Row k = store.createNewEmptyRow();
        k.put("shardKey", 1234);
        store.multiDelete("t3", k, null, null, null);

        long time = System.currentTimeMillis();
        for (int i = 0; i < NO_OF_ITEMS; i++) {
            Row row = store.createNewEmptyRow();
            row.put("shardKey", 1234);
            row.put("id", i);
            row.put("s", "row: " + i);
            row.put("indexKey1", 111);
            store.put("t3", row, null);
        }
        time = System.currentTimeMillis() - time;
        System.out
            .println("   " + ((double) NO_OF_ITEMS / time) + " puts/ms");


        int noOfThreads = 20;
        time = System.currentTimeMillis();

        ClientThread[] threads = new ClientThread[noOfThreads];

        for (int i = 0; i < noOfThreads; i++) {
            threads[i] = new ClientThread(kind);
        }

        for (int i = 0; i < noOfThreads; i++) {
            threads[i].start();
        }

        for (int i = 0; i < noOfThreads; i++) {
            threads[i].join();
        }

        time = System.currentTimeMillis() - time;

        System.out.println("Number of threads: " + noOfThreads +
            "    Total time: " + time + " ms");
        System.out.println("    " + ((double) noOfThreads * NO_OF_ITEMS) / time
            + " mt_iterator_items/ms");

        for (int i = 0; i < noOfThreads; i++) {
            Assert.assertFalse(threads[i].isFailing);
        }
    }

    private void populateMapTable(Store store) {
        /* populate the table */
        for (int i = 0; i < 20; i++) {
            Row row = store.createNewEmptyRow();
            row.put("id", i);
            /*
             * put 2 entries in the map -- one that has a unique key for
             * each row and one common key with unique values in each row.
             */
            MapValue map = row.putMap("intMap");
            map.put(("key" + i), i);
            map.put("common", i + 10);
            Result result = store.put("MapTable", row, null);
            Assert.assertTrue(result.getSuccess());
        }
    }

    private void populateRecordTable(Store store) {
        /* populate the table */
        for (int i = 0; i < 20; i++) {
            Row row = store.createNewEmptyRow();
            row.put("id", i);
            /*
             * put 2 entries in the map -- one that has a unique key for
             * each row and one common key with unique values in each row.
             */
            Row record = row.putRecord("address");
            record.put("city", ("city" + i));
            Result result = store.put("RecordTable", row, null);
            Assert.assertTrue(result.getSuccess());
        }
    }

    static class ClientThread
        extends Thread {
        volatile boolean isFailing = true;
        private Kind kind;
        ClientThread(Kind kind) {
            this.kind = kind;
        }

        @Override
        public void run() {
            // Since Thrift client is not thread safe it needs a new store
            // for each thread.
            Store store = AllTests.getNewStore();

            Row key = store.createNewEmptyRow();
            key.put("shardKey", 1234);

            ResultIterator it;
            switch (kind) {
            case TABLE_ITERATOR:
                it = store.tableIterator("t3", key, null, null,
                    null, ReadOptions.Direction.FORWARD);
                break;
            case TABLE_KEYS_ITERATOR:
                it = store.tableKeysIterator("t3", key, null, null,
                    null, ReadOptions.Direction.FORWARD);
                break;
            case INDEX_ITERATOR:
                Row indexKey1 = store.createNewEmptyRow();
                indexKey1.put("indexKey1", 111);
                it = store.indexIterator("t3", "t3_i1_idx", indexKey1, null,
                    null, null, ReadOptions.Direction.FORWARD);
                break;
            case INDEX_KEYS_ITERATOR:
                Row indexKey2 = store.createNewEmptyRow();
                indexKey2.put("indexKey1", 111);
                it = store.indexKeysIterator("t3", "t3_i1_idx", indexKey2,
                    null, null, null, ReadOptions.Direction.FORWARD);
                break;
            default:
                Assert.assertTrue(false);
                throw new IllegalStateException();
            }
            int counter = 0;

            while (it.hasNext()) {
                Result r = (Result) it.next();

                Assert.assertNotNull(r);
                Assert.assertNotNull(r.getCurrentRow());

                if (kind == Kind.INDEX_KEYS_ITERATOR ) {
                    Assert.assertNotNull(r.getCurrentRow().get("primary"));
                    Assert.assertNotNull(r.getCurrentRow().get("primary")
                        .asRecord());
                    Assert.assertNotNull(r.getCurrentRow().get("primary")
                        .asRecord().get("shardKey"));
                    Assert.assertNotNull(r.getCurrentRow().get("primary")
                        .asRecord().get("shardKey").asInteger());
                    Assert.assertEquals(1234,
                        r.getCurrentRow().get("primary")
                            .asRecord().get("shardKey").asInteger()
                            .intValue());
                    Assert.assertEquals(counter,
                        r.getCurrentRow().get("primary")
                            .asRecord().get("id").asInteger().intValue());
                } else {
                    Assert.assertNotNull(r.getCurrentRow().get("shardKey"));
                    Assert.assertNotNull(r.getCurrentRow().get("shardKey").
                        asInteger());
                    Assert.assertEquals(1234,
                        r.getCurrentRow().get("shardKey").asInteger()
                            .intValue());
                    Assert.assertEquals(counter,
                        r.getCurrentRow().get("id").asInteger().intValue());
                }

                if (kind == Kind.TABLE_ITERATOR ||
                    kind == Kind.INDEX_ITERATOR) {
                    Assert.assertNotNull(r);
                    Assert.assertNotNull(r.getCurrentRow());
                    Assert.assertNotNull(r.getCurrentRow().get("s"));

                    Assert.assertEquals("row: " + counter,
                        r.getCurrentRow().get("s").asString());

                    Assert.assertEquals(111,
                        r.getCurrentRow().get("indexKey1").asInteger()
                            .intValue());
                }

                counter++;
            }

            Assert.assertEquals(counter, NO_OF_ITEMS);

            isFailing = false;
        }

        static enum Kind {
            TABLE_ITERATOR,
            TABLE_KEYS_ITERATOR,
            INDEX_ITERATOR,
            INDEX_KEYS_ITERATOR
        }
    }
}
