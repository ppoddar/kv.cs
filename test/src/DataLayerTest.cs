
/**
 * Test basic data layer.
 */


namespace oracle.kv.client.test {
    using NUnit.Framework;
    using System;
    using oracle.kv.client.data;

    [TestFixture,
     Description(@"Tests data layer:
        Data Layer stores basic value type, DataObject and DataObjectArray
        A DataObject can be parsed from a JSON string
        A DataObject can nest other DataObject/DataObjectArray
        A DataObject can be navigated to a nested property value by a path 
        A DataObject can transform a user value to store a different type

        The tests have no depenendeny on a database")]

    public class DataLayerTest : AbstractTest {
        static IDataModel model;

        [OneTimeSetUp]
        public void InitailizeModel() {
            if (model != null) return;

            model = new BaseDataModel();
            //Assert.False(dataProvider.UsesSchema);

        }

        [Test]
        public void GetNonExistentPropertyThrowsException() {
            DataObject data = new DataObject();
            Assert.Throws<ArgumentException>(
                () => { var x = data["x"]; }
            );
        }

        [Test]
        public void SetGetProperty() {
            DataObject data = new DataObject();
            data["x"] = "p";
            Assert.AreEqual("p", data["x"]);
        }

        [Test]
        public void populateFromJSON() {
            DataObject data = new DataObject();
            Assert.IsFalse(data.HasProperty("x"));
            string json = "{\"x\":1234}";
            data.FromJSON(json);
            Assert.IsTrue(data.HasProperty("x"));
            Assert.AreEqual(1234, data["x"]);

            string json2 = "{\"y\":5678}";
            data.FromJSON(json2);
            Assert.IsTrue(data.HasProperty("y"));
            Assert.AreEqual(1234, data["x"]);
            Assert.AreEqual(5678, data["y"]);

            Assert.IsTrue(Array.IndexOf(data.PropertyNames, "x") >= 0);
            Assert.IsTrue(Array.IndexOf(data.PropertyNames, "y") >= 0);
        }

        [Test]
        public void toJSONString() {
            DataObject data = new DataObject();
            data["x"] = 1234;
            string json = "{\"x\":1234}";
            Assert.AreEqual(json, data.ToJSONString().ToString());

            json = "{\"x\":1234,\"y\":\"a string\"}";
            data["y"] = "a string";
            Assert.AreEqual(json, data.ToJSONString().ToString());

            DataObjectArray array = new DataObjectArray();
            array.Add(2);
            array.Add(3);
            array.Add(4);
            Assert.AreEqual("[2,3,4]", array.ToJSONString().ToString());

            array = new DataObjectArray();
            array.Add(data);
            Assert.AreEqual("[" + json + "]", array.ToJSONString().ToString());

        }

        [Test]
        public void AccumulateNonCompositeElements() {
            JSONObject json = new JSONObject();
            int N = 3;
            for (int i = 0; i < 3; i++) {
                json.Accumulate("x", i);
            }
            Object value = json["x"];
            Assert.AreEqual(typeof(DataObjectArray), value.GetType());
            Assert.AreEqual(N, (value as DataObjectArray).Length);
        }

        [Test]
        public void AccumulateCompositeElements() {
            JSONObject json = new JSONObject();

            int N = 3;
            for (int i = 0; i < 3; i++) {
                JSONObject e = new JSONObject();
                e["y"] = 1234;
                json.Accumulate("x", e);
            }
            Object value = json["x"];
            Assert.AreEqual(typeof(DataObjectArray), value.GetType());
            Assert.AreEqual(N, (value as DataObjectArray).Length);
            Assert.AreEqual(1234, ((value as DataObjectArray)[0] as DataObject)["y"]);
        }



        [Test]
        public void PropertyValueOfNestedPath() {
            var jsonObject = model.Deserialize(@"{ 
            ""Name"": ""Jon Smith"", 
            ""Address"": { 
                ""City"": ""New York"", 
                ""State"": ""NY"" 
            }, 
            ""Age"": 42 
            }");

            var city = model.GetValue<string>(jsonObject, "Address/City", null);
            Assert.AreEqual("New York", city);

            var state = model.GetValue<string>(jsonObject, "Address/State", null);
            Assert.AreEqual("NY", state);

            var age = model.GetValue<int>(jsonObject, "Age", null);
            Assert.AreEqual(42, age);

        }

        [Test]
        public void PropertyValueCanContainUnicodeCharacter() {
            var jsonString = "{\"test\":\"hello\u0021 world\"}";
            AssertJSONStringIsPreservedAcrossSerializationBundary(jsonString);
        }

        [Test]
        public void PropertyValueCanContainEscapeCharacter() {
            var jsonString = "{\"test\":\"hello\tworld\"}";
            AssertJSONStringIsPreservedAcrossSerializationBundary(jsonString);
        }


        void AssertJSONStringIsPreservedAcrossSerializationBundary(string jsonString) {
            var jsonObject = model.Deserialize(jsonString);
            var serialized = model.Serialize(jsonObject);
            var deserialized = model.Deserialize(serialized).ToString();

            Assert.AreEqual(jsonString, serialized);
            Assert.AreEqual(serialized, deserialized);
        }

        [Test]
        public void ArrayOfBasicTypeCanBeDeserialized() {
            string jsonString = @"{""intArray"":[1,2,3,4]}";
            var deserialized = model.Deserialize(jsonString) as DataObject;

            Assert.IsTrue(deserialized is DataObject);

            var raw = deserialized["intArray"];
            Assert.AreEqual(typeof(DataObjectArray), raw.GetType());
            Assert.AreEqual((raw as DataObjectArray).ElementType, typeof(long));
            Assert.AreEqual(jsonString, model.Serialize(deserialized));
        }

        [Test]
        public void ArrayValueIsStoredAsDataObjectArray() {
            int[] typedArrayValue = { 1, 2, 3, 4 };

            var dataObject = new DataObject();
            model.PutValue(dataObject, "intArray", typedArrayValue, null);
            Assert.AreEqual(typeof(DataObjectArray), dataObject["intArray"].GetType());
        }

        [Test]
        public void ArrayElementCanBeNull() {
            DataObject[] arrayData = new DataObject[]
                    {new DataObject(), null, new DataObject() };
            DataObject dataObject = new DataObject();
            dataObject["array"] = arrayData;

            var arrayRetrieved = dataObject["array"] as DataObjectArray;
            Assert.IsNotNull(arrayRetrieved);
            Assert.AreEqual(3, arrayRetrieved.Length);
            Assert.IsNotNull(arrayRetrieved[0]);
            Assert.IsNull(arrayRetrieved[1]);
            Assert.IsNotNull(arrayRetrieved[2]);
        }

        [Test]
        public void ArrayElementsMustBeHomogeneousType() {
            string jsonString = @"{""arrayOfHeterogeneousElements"":[1, ""second""]}";
            Assert.Throws<ArgumentException>(delegate {
                model.Deserialize(jsonString);
            });
        }


        [Test]
        public void ArrayOfCompositeTypeCanBeDeserialized() {
            string jsonString = @"{""array"":[{""x"":10}, {""x"":20}]}";
            var jsonObject = model.Deserialize(jsonString);

            Assert.AreEqual(typeof(DataObject), jsonObject.GetType());
            Assert.AreEqual(typeof(DataObjectArray), jsonObject["array"].GetType());

            var array = jsonObject["array"] as DataObjectArray;
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(typeof(DataObject), array.ElementType);
            Assert.AreEqual(10, model.GetValue<int>(jsonObject, "array[0]/x", null));
            Assert.AreEqual(20, model.GetValue<int>(jsonObject, "array[1]/x", null));

        }

        /// <summary>
        /// Tests common assertions about value type and string.
        /// </summary>
        /// <remarks>
        /// The userValue is stored without any change of its type of value.
        /// </remarks>
        void AssertBasicType<T>(T userValue) {
            var dataObject = new DataObject();
            dataObject["x"] = userValue;

            Assert.AreEqual(userValue, dataObject["x"]);
            Assert.AreEqual(userValue.GetType(), dataObject["x"].GetType());

        }

        [Test]
        public void PropertyValueOfByteType() {
            AssertBasicType((byte)42);
        }

        [Test]
        public void PropertyValueOfShortType() {
            AssertBasicType((short)42);
        }

        [Test]
        public void PropertyValueOfIntegerType() {
            AssertBasicType(42);
        }

        [Test]
        public void PropertyValueOfLongType() {
            AssertBasicType(42L);
        }

        [Test]
        public void PropertyValueOfFloatType() {
            AssertBasicType(42.24F);
        }

        [Test]
        public void PropertyValueOfDoubleType() {
            AssertBasicType(42.24);
        }

        [Test]
        public void PropertyValueOfBooleanType() {
            AssertBasicType(true);
        }

        [Test]
        public void PropertyValueOfByteArrayHasByteAsElementType() {
            var dataObject = new DataObject();
            byte[] bytes = { 23, 12, 13 };
            model.PutValue(dataObject, "bytes", bytes, null);

            var encoded = dataObject.ToJSONString().ToString();
            var deserialized = model.Deserialize(encoded) as DataObject;

            Assert.AreEqual(encoded, deserialized.ToJSONString().ToString());
            Assert.IsTrue(dataObject["bytes"] is DataObjectArray);
            var dataArray = dataObject["bytes"] as DataObjectArray;
            Assert.AreEqual(typeof(byte), dataArray.ElementType);
        }


        [Test]
        public void ParseIntegerValue() {
            var dataObject = new DataObject();
            dataObject["long"] = 1234;
            var serialized = dataObject.ToJSONString().ToString();
            var deserilaized = model.Deserialize(serialized) as DataObject;

            Assert.AreEqual(1234, deserilaized["long"]);
        }

        [Test]
        public void ParseBooleanValue() {
            var dataObject = new DataObject();
            dataObject["boolean"] = true;
            var serialized = dataObject.ToJSONString().ToString();

            var deserilaized = model.Deserialize(serialized) as DataObject;

            Assert.AreEqual(true, deserilaized["boolean"]);
        }


        [Test]
        public void ParseEmptyJSON() {
            var obj = model.Deserialize(@"{}");

            Assert.IsTrue((obj as DataObject).PropertyNames.Length == 0);
        }

        [Test]
        public void ParseSingleSimpleProperty() {
            var obj = model.Deserialize(@"{""id"":""hello""}");
            Assert.IsTrue(obj is DataObject);
            Assert.AreEqual("hello", (obj as DataObject)["id"]);
        }

        [Test]
        public void ParseMultipleSimpleProperty() {

            var original = @"{""id"":1234,""name"":""hello"",""home_grown"":true}";

            var obj = model.Deserialize(original);
            Assert.IsTrue(obj is DataObject,
                "Deserialzed " + obj + " is " + obj.GetType() + " not JSONObject");
            var jsonObject = obj as DataObject;
            var encoded = jsonObject.ToJSONString().ToString();

            Assert.AreEqual(original, encoded);
            Assert.AreEqual(1234, jsonObject["id"]);
            Assert.AreEqual("hello", jsonObject["name"]);
            Assert.AreEqual(true, jsonObject["home_grown"]);
        }

        [Test]
        public void ParseNestedProperty() {
            var deserilaized = model.Deserialize(@"{
                ""nested"":{
                    ""name"":""hello"", 
                    ""home_grown"":true
                    }
                }");
            Assert.NotNull(deserilaized);
            Assert.IsTrue(deserilaized is DataObject);
            Assert.IsTrue((deserilaized as DataObject)["nested"] is DataObject);
        }

        [Test]
        public void ParseNoProperty() {
            string jsonString = @"{}";
            var jsonObject = model.Deserialize(jsonString);

            Assert.AreEqual(0, jsonObject.PropertyNames.Length);
        }

        [Test]
        public void ParseSingleProperty() {
            string jsonString = @"{""hello"":""JSON PARSER""}";
            var jsonObject = model.Deserialize(jsonString);

            Assert.AreEqual("JSON PARSER", jsonObject["hello"]);
        }

        [Test]
        public void ParseMultipleProperty() {
            string jsonString = @"{""p1"":""v1"",""p2"":""v2""}";
            var jsonObject = model.Deserialize(jsonString);
            //Console.WriteLine(JSON.Serialize(jsonObject));

            Assert.AreEqual("v1", jsonObject["p1"]);
            Assert.AreEqual("v2", jsonObject["p2"]);
        }

        [Test]
        public void ParseNumericProperty() {
            string jsonString = @"{""p"":1234.56}";
            var jsonObject = model.Deserialize(jsonString);

            Assert.AreEqual(1234.56, jsonObject["p"]);
        }

        [Test]
        public void ParseNestedroperty() {
            var jsonString = @"{""nested1"":{""p"":""x""}, ""p"":""y"",""nested2"":{""p"":""z""}}";
            var dataObject = model.Deserialize(jsonString);
            var nested = dataObject["nested1"] as DataObject;

            Assert.AreEqual("x", nested["p"]);

            nested = dataObject["nested2"] as DataObject;

            Assert.AreEqual("z", nested["p"]);
        }

        [Test]
        public void PropertyValueOfByteArrayField() {
            int N = 10;
            byte[] bytes = new byte[N];
            for (int i = 0; i < N; i++) {
                bytes[i] = (byte)(42 + i);
            }
            var dataObject = new DataObject();
            model.PutValue(dataObject, "bytes", bytes, null);

            Assert.AreEqual(typeof(DataObjectArray), dataObject["bytes"].GetType());
            var byteArray = dataObject["bytes"] as DataObjectArray;
            Assert.AreEqual(typeof(byte), byteArray.ElementType);
            Assert.AreEqual(N, byteArray.Length);
            for (int i = 0; i < N; i++) {
                Assert.AreEqual((byte)(42 + i), byteArray[i]);
            }
        }





    }
}
