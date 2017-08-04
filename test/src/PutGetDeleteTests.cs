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

import oracle.kv.client.ArrayValue;
import oracle.kv.client.Result;
import oracle.kv.client.Row;
import oracle.kv.client.Store;
import oracle.kv.client.TimeToLive;
import oracle.kv.client.Version;
import oracle.kv.client.WriteOptions;
import org.junit.Assert;
import org.junit.Test;

/**
 * Class to test put, get, delete
 */
public class PutGetDeleteTests {
    @Test
    public void testPutGet() {
        Store store = AllTests.store;

        int id = 300;

        Row row = store.createNewEmptyRow();
        row.put("id", id);
        row.put("s", "this is a string");
        row.put("f", 1.234567f);
        row.put("d", 8.90d);
        row.put("l", 123l);
        row.put("bool", true);
        ArrayValue arrStr = row.putArray("arrStr");
        arrStr.add("1");
        arrStr.add("B");
        row.put("bin", "byte[]test".getBytes());
        //System.out.println("   row: " + row);

        /* call to put -- stores a single Row */
        //System.out.println("   Calling put ...");
        Result res = store.put("t2", row, null);

        //System.out.println("   - " + res);
        Assert.assertNotNull(res);
        Assert.assertNotNull(res.getCurrentRow());
        Assert.assertEquals(id, res.getCurrentRow().get("id")
            .asInteger().intValue());
        Assert.assertEquals("this is a string", res.getCurrentRow().get("s").
            asString());
        Assert.assertEquals(1.234567f, res.getCurrentRow().get("f").asFloat(),
            0);
        Assert.assertEquals(8.90d, res.getCurrentRow().get("d").asDouble(), 0);
        Assert.assertEquals(123l,
            res.getCurrentRow().get("l").asLong().longValue());
        Assert.assertEquals(true, res.getCurrentRow().get("bool").asBoolean());
        Assert.assertNotNull(res.getCurrentRow().get("arrStr"));
        Assert.assertEquals("1", res.getCurrentRow().get("arrStr").asArray()
            .get(0));
        Assert.assertEquals("B", res.getCurrentRow().get("arrStr").asArray()
            .get(1));
        Assert.assertArrayEquals("byte[]test".getBytes(),
            res.getCurrentRow().get("bin").asBinary());

        Assert.assertNotNull(res.getCurrentRowVersion());

        Assert.assertEquals("t2", res.getTableName());

        /* call to get -- returns most recently put Row */
        //System.out.println("   Calling get ...");
        res = store.get("t2", row, null);

//        System.out.println("\n   Result: version: " +
//            res.getCurrentRowVersion());
//        System.out.println("   - " + res.getCurrentRow());
        Assert.assertNotNull(res);
        Assert.assertNotNull(res.getCurrentRow());
        Assert.assertNotNull(res.getCurrentRowVersion());

//        System.out.println(
//            "   Number of fields: " + res.getCurrentRow().numFields());
//        for(Iterator<Field>
//                iterator = res.getCurrentRow().getFields().iterator();
//            iterator.hasNext(); )
//        {
//            Field field = iterator.next();
//            System.out.println("    - " + field.getName() + " : " + res
//                .getCurrentRow().get(field.getName()).getAsString());
//        }
        Assert.assertEquals(10, res.getCurrentRow().numFields());
        Assert.assertEquals(id, res.getCurrentRow().get("id")
            .asInteger().intValue());
        Assert.assertEquals("this is a string", res.getCurrentRow().get("s").
            asString());
        Assert.assertEquals(1.234567f, res.getCurrentRow().get("f").asFloat(),
            0);
        Assert.assertEquals(8.90d, res.getCurrentRow().get("d").asDouble(), 0);
        Assert.assertEquals(123l,
            res.getCurrentRow().get("l").asLong().longValue());
        Assert.assertEquals(true, res.getCurrentRow().get("bool").asBoolean());
        Assert.assertNotNull(res.getCurrentRow().get("arrStr"));
        Assert.assertEquals("1", res.getCurrentRow().get("arrStr").asArray()
            .get(0));
        Assert.assertEquals("B", res.getCurrentRow().get("arrStr").asArray()
            .get(1));
        Assert.assertArrayEquals("byte[]test".getBytes(),
            res.getCurrentRow().get("bin").asBinary());


        /* cast fields in accessors*/
        /*String enumVal = res.getRow().get("enum").asString();
        Day day = Day.valueOf(enumVal);
        System.out.println("GMF (sunday) : " + day);
        day = res.getRow().get("enum").asEnum(Day.class);
        System.out.println("GMF (sunday) : " + day);

        System.out.println("GMF record: " +
            res.getRow().get("array").asArray());
        */
        //System.out.println("GMF map: " +
        //    res.getRow().get("map").asMap());
        //System.out.println("GMF record: " +
        //    res.getRow().get("address").asRow());

        /* call to delete the row */
//        System.out.println("\n   Delete row");
        Row key = store.createNewEmptyRow();
        key.put("id", id);

        Result delRes = store.delete("t2", key, null);
        assert delRes != null : "Row " + id + " was not deleted";
        Assert.assertNotNull(delRes);
        //todo check this
        Assert.assertNotNull(delRes.getPreviousRow());

        //System.out.println("    - Try getting the deleted row");
        res = store.get("t2", key, null);
        //System.out.println("     - get res:" + res);
        assert res.getCurrentRow() == null :
            "Row " + id + " exists in the store";
        Assert.assertNotNull(res);
        Assert.assertNull(res.getCurrentRow());
        Assert.assertEquals("t2", res.getTableName());
    }

    @Test
    public void testPutIfVersion() {
        Store store = AllTests.store;

        int id = 303;
        Row row1 = store.createNewEmptyRow();
        row1.put("id", id);
        row1.put("s", "this is first version");
        Result res1 = store.put("t2", row1, null);

        Assert.assertNotNull(res1);

        Version v = res1.getCurrentRowVersion();
//        System.out.println("  res1 - Prev version: " +
//            res1.getPreviousRowVersion());
//        System.out.println("  res1 - Current version: " +
//            (res1.getCurrentRowVersion()!=null ?
//                res1.getCurrentRowVersion().toByteArray().length :
//                0 ) + "  '"  + res1.getCurrentRowVersion() + "'");
        if (res1.getCurrentRowVersion() != null) {
            Assert.assertEquals(50,
                res1.getCurrentRowVersion().toByteArray().length);
        }

        Row row2 = store.createNewEmptyRow();
        row2.put("id", id);
        row2.put("s", "this is the second version");
        Result res2 = store.putIfVersion("t2", row2, v, null);

        Assert.assertNotNull(res2);

//        System.out.println("  res2 - Prev version: " +
//            res2.getPreviousRowVersion());
//        System.out.println("  res2 - Current version: " +
//            (res2.getCurrentRowVersion()!=null ?
//                res2.getCurrentRowVersion().toByteArray().length :
//                0 ) + "  '"  + res2.getCurrentRowVersion() + "'");

        Assert.assertNotNull(res2);
        if (res1.getCurrentRowVersion() != null) {
            Assert.assertEquals(50,
                res2.getCurrentRowVersion().toByteArray().length);
        }

        Assert.assertNotEquals(res1.getCurrentRowVersion(),
            res2.getCurrentRowVersion());

        Assert.assertNull(res2.getPreviousRowVersion());
    }

    @Test
    public void testPutIfVersion2() {
        Store store = AllTests.store;

        int id = 305;
        Row row1 = store.createNewEmptyRow();
        row1.put("id", id);
        row1.put("s", "this is first version");
        row1.put("l", 123456709l);
        Result res1 = store.put("t2", row1, null);
        Version v = res1.getCurrentRowVersion();

//        System.out.println("  res1 - Prev version: " +
//            res1.getPreviousRowVersion());
//        System.out.println("  res1 - Current version: " +
//            (res1.getCurrentRowVersion()!=null ?
//                res1.getCurrentRowVersion().toByteArray().length :
//                0 ) + "  '"  + res1.getCurrentRowVersion() + "'");

        Row row2 = store.createNewEmptyRow();
        row2.put("id", id);
        row2.put("s", "this is the second version");

        WriteOptions wo = store.createNewWriteOptions(
            store.createNewDurability(
                WriteOptions.Durability.SyncPolicy.SYNC,
                WriteOptions.Durability.SyncPolicy.SYNC,
                WriteOptions.Durability.ReplicaAckPolicy.ALL),
            WriteOptions.ReturnRowChoice.ALL, 1000);

        Result res2 = store.putIfVersion("t2", row2, v, wo);

//        System.out.println("  res2 - Prev version: " +
//            res2.getPreviousRowVersion());
//        System.out.println("  res2 - Current version: " +
//            (res2.getCurrentRowVersion()!=null ?
//                res2.getCurrentRowVersion().toByteArray().length :
//                0 ) + "  '"  + res2.getCurrentRowVersion() + "'");

        Assert.assertNotNull(res2);
        Assert.assertNull(res2.getPreviousRowVersion());
        Assert.assertNotEquals(res1.getCurrentRowVersion().toString(),
            res2.getCurrentRowVersion().toString());
    }

    private static long EXPIRATION_DAY_ERROR = 1000 * 60 * 60 * 24;

    @Test
    public void testPutGetTtl() {
        Store store = AllTests.store;

        int id = 306;

        long time = System.currentTimeMillis();

        Row row = store.createNewEmptyRow();
        row.put("id", id);
        row.put("s", "this is a row with ttl");
        TimeToLive ttl = TimeToLive.Factory.ofDays(5);
        row.setTTL(ttl);
        Assert.assertNotNull(row.getTTL());
        Assert.assertEquals(ttl, row.getTTL());
        Assert.assertEquals(ttl.toDays(), row.getTTL().toDays());
        Assert.assertEquals(ttl.toHours(), row.getTTL().toHours());

        //System.out.println("   row: " + row);

        /* call to put -- stores a single Row */
        //System.out.println("   Calling put ...");
        WriteOptions writeOptions = null;
        writeOptions = store.createNewWriteOptions(
            WriteOptions.Durability.COMMIT_SYNC,
            WriteOptions.ReturnRowChoice.NONE, 1000);
        writeOptions = writeOptions.setUpdateTTL(true);
        Result res = store.put("t2", row, writeOptions);

        //System.out.println("   - " + res);
        Assert.assertNotNull(res);
        Assert.assertNotNull(res.getCurrentRow());
        Assert.assertEquals(id, res.getCurrentRow().get("id")
            .asInteger().intValue());
        Assert.assertEquals("this is a row with ttl",
            res.getCurrentRow().get("s").
                asString());

        Assert.assertEquals(ttl.toExpirationTime(time),
            res.getExpirationTime(), EXPIRATION_DAY_ERROR);

        /* call to get -- returns most recently put Row */
        //System.out.println("   Calling get ...");
        res = store.get("t2", row, null);

        //        System.out.println("\n   Result: version: " +
        //            res.getCurrentRowVersion());
        //        System.out.println("   - " + res.getCurrentRow());
        Assert.assertNotNull(res);
        Assert.assertNotNull(res.getCurrentRow());
        Assert.assertNotNull(res.getCurrentRowVersion());

        Assert.assertEquals(ttl.toExpirationTime(time),
            res.getExpirationTime(), EXPIRATION_DAY_ERROR);


        /* call to delete the row */
        //        System.out.println("\n   Delete row");
        Row key = store.createNewEmptyRow();
        key.put("id", id);

        Result delRes = store.delete("t2", key, null);
        assert delRes != null : "Row " + id + " was not deleted";
        Assert.assertNotNull(delRes);

        Assert.assertNotNull(delRes.getPreviousRow());
        Assert.assertEquals(0, delRes.getExpirationTime());

        //System.out.println("    - Try getting the deleted row");
        res = store.get("t2", key, null);
        //System.out.println("     - get res:" + res);
        Assert.assertNotNull(res);
        Assert.assertNull(res.getCurrentRow());
        Assert.assertEquals("t2", res.getTableName());
    }

    @Test
    public void testDelete() {
        Store store = AllTests.store;

        int id = 301;

        Row row = store.createNewEmptyRow();
        row.put("id", id);
        row.put("s", "this is a string");
        row.put("f", 1.234567f);
        row.put("d", 8.90d);
        row.put("l", 123l);
        row.put("bool", true);
        ArrayValue arrStr = row.putArray("arrStr");
        arrStr.add("1");
        arrStr.add("B");
        row.put("bin", "byte[]test".getBytes());
//        System.out.println(row);
        Assert.assertNotNull(row);


        /* call to put -- stores a single Row */
//        System.out.println("Calling put ...");
        Result res = store.put("t2", row, null);
//        System.out.println(res);
        Assert.assertNotNull(res);
        Assert.assertNotNull(res.getCurrentRow());
        Assert.assertEquals(id,
            res.getCurrentRow().get("id").asInteger().intValue());
        Assert.assertEquals("this is a string",
            res.getCurrentRow().get("s").asString());

        /* call to delete the row */
//        System.out.println("\nDelete row");
        Row key = store.createNewEmptyRow();
        key.put("id", id);

        Result delRes = store.delete("t2", key, null);
        Assert.assertNotNull("Row " + id + " was not deleted", delRes);

        //System.out.println("\nTry getting the deleted row");
        res = store.get("t2", key, null);
        Assert.assertNull("Row " + id + " exists in the store",
            res.getCurrentRow());
    }

}
