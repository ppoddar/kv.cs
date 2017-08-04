using System;
namespace oracle.kv.client.data {
    public class JSONObject : DataObject {
        public JSONObject() {
        }

        public void Accumulate(string key, Object value) {
            if (HasProperty(key)) {
                var currentValue = this[key];
                if (currentValue is DataObjectArray) {
                    ((DataObjectArray)currentValue).Add(value);
                } else {
                    DataObjectArray array = new DataObjectArray();
                    array.Add(currentValue);
                    array.Add(value);
                    this[key] = array;
                }
            } else {
                this[key] = value;
            }
        }


    }

    public class JSONArray : DataObjectArray {
    }
}
