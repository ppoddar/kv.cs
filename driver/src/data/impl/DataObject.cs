/*-
 *
 *  This file is part of Oracle NoSQL Database
 *  Copyright (C) 2015, 2020 Oracle and/or its affiliates.  All rights reserved.
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



namespace oracle.kv.client.data {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using oracle.kv.client.error;
    using oracle.kv.client.util;
    using oracle.kv.client.data;


    /// <summary>
    /// A cannonical form of data appropriate for in-memory representation. 
    /// Maintains data elements as dictionary of values indexed by property
    /// names.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>C# types</item>
    /// <description>built-in value types of C# lanaguge and string
    /// </description>
    /// <item>composite type</item>
    /// <description>composite type <see cref="IDataContainer"/> 
    /// </description>
    /// </list>
    /// <para></para> 
    /// A composite type can be composed of composite types themselves, 
    /// thereby allowing arbitrary depth of nested structures.
    /// <para></para>
    /// </remarks>
    public class DataObject : IDataContainer {
        Dictionary<string, object> _delegate;


        /// <summary>
        /// Creates an empty data container.
        /// </summary>
        public DataObject() {
            _delegate = new Dictionary<string, object>();
        }

        public DataObject(Dictionary<string, object> dict) : this() {
            foreach (KeyValuePair<string, object> kvp in dict) {
                this[kvp.Key] = kvp.Value;
            }
        }

        public DataObject FromJSON(string jsonString) {
            var deserailized = new JSONParser().Deserialize(jsonString);
            foreach (string p in deserailized.PropertyNames) {
                this[p] = deserailized[p];
            }
            return this;
        }

        /// <summary>
        /// Gets the value of specified property.
        /// </summary>
        /// <returns>The value of specified property. 
        /// </returns>
        /// <param name="property">name of a property. The property must be defined.
        /// Otherwise an exception is raised. 
        /// </param>
        public object this[string property] {
            get {
                if (!HasProperty(property)) {
                    throw new ArgumentException("Property [" + property + "] not found"
                    + " in available properties [" + string.Join(",", PropertyNames) + "]");
                }
                return _delegate[property];

            }
            set {
                if (!IsValueSupported(value)) {
                    throw new ArgumentException(value.GetType() + " is not supported");
                }

                if (value is Array) {
                    value = new DataObjectArray(value as Array);
                }
                _delegate[property] = value;
            }
        }

        /// <summary>
        /// Affirms if the given value type is supported.
        /// A supported value is either null, or of value type, string,
        /// DataObject or DataObjectArray.
        /// </summary>
        /// <param name="value">a value to be tested.</param>
        protected bool IsValueSupported(object value) {
            return value == null
                || value is string
                || value.GetType().IsValueType
                || value is DataObject
                || value is DataObjectArray
                || (value.GetType().IsArray
                 && IsTypeSupported(value.GetType().GetElementType()))
                ;
        }

        protected bool IsTypeSupported(Type type) {
            if (type.IsArray) {
                return IsTypeSupported(type.GetElementType());
            }
            return type == typeof(string)
            || type.IsValueType
            || type == typeof(DataObject)
            || type.IsSubclassOf(typeof(DataObject))
            || type == typeof(DataObjectArray)
            || type.IsSubclassOf(typeof(DataObjectArray))
            ;

        }

        /// <inheritDoc/>
        public bool HasProperty(string property) {
            return _delegate.ContainsKey(property);

        }

        /// <inheritDoc/>
        public string[] PropertyNames {
            get { return new List<string>(_delegate.Keys).ToArray(); }
        }

        /// <summary>
        /// Gets a JSON formatted string
        /// </summary>
        public override string ToString() {
            return ToJSONString(0).ToString();
        }

        /// <inheritDoc/>
        public StringBuilder ToJSONString() {
            return ToJSONString(0);
        }

        /// <summary>
        /// Gets a JSON formatted string
        /// </summary>
        /// <returns>The JSON String.</returns>
        public StringBuilder ToJSONString(int tab) {
            var ws = tab > 0 ? Environment.NewLine + StringHelper.WhiteSpace(tab) : "";
            return StringHelper.Stringify(this, ws, tab);
        }

        public Object query(string path) {
            return queryInternal(path, path);
        }

        private Object queryInternal(string path, string fullPath) {
            var segment = PathUtil.Head(path);
            var property = PathUtil.PropertyName(segment);
            Assert.IsTrue(HasProperty(property), "invalid query because '" + property
                + "' in '" + fullPath + "' does not exist"
                + " Available properties are " + this.PropertyNames);
            var val = this[property];
            if (PathUtil.Tail(path) == null) {
                return val;
            }
            if (val is DataObject) {
                return (val as DataObject).queryInternal(PathUtil.Tail(path), fullPath);
            } else if (val is DataObjectArray) {
                Assert.IsTrue(PathUtil.IsArraySegment(segment),
                    "invalid query because " + segment + " is not navigable");
                val = (val as DataObjectArray)[PathUtil.ArrayIndex(segment)];
                return (val as DataObject).queryInternal(PathUtil.Tail(path), fullPath);
            } else {
                Assert.IsTrue(false,
                    "invalid query because " + segment + " is not navigable");
                return null;
            }

        }


    }


    /// <summary>
    /// A DataObject array is an array of basic and composite elememts.
    /// </summary>
    public class DataObjectArray : IDataContainer {
        System.Collections.IList array = new System.Collections.ArrayList();

        /// <summary>
        /// Creates an array without any element.
        /// </summary>
        public DataObjectArray() { }

        /// <summary>
        /// Creates an array of given elements.
        /// </summary>
        /// <returns>The array of basic type.</returns>
        /// <param name="elements">Elements.</param>
        public DataObjectArray(Array elements) {
            if (elements == null) {
                Add(null);
                return;
            }
            foreach (object e in elements) {
                Add(e);
            }
        }

        public string Name { get { return "[]"; } }

        public bool IsEmpty {
            get { return array.Count <= 0; }
        }

        /// <summary>
        /// Gets an empty array of string because an array has no named property.
        /// </summary>
        /// <value>The property names.</value>
        public string[] PropertyNames {
            get { return new string[] { }; }
        }

        /// <summary>
        /// Adds the specified element to this array.
        /// </summary>
        /// <remarks>
        /// The elements must be of type compatiable.
        ///</remarks>
        /// <param name="e">an element to be added.</param>
        /// <exception cref="ArgumentException">If element is not type compatiable to other elements.
        /// </exception> 
        public void Add(object e) {
            if (e == null) {
                array.Add(e);
                return;
            }
            Type eType = e.GetType();
            Assert.IsTrue(IsSupportedElementType(eType),
                "can not add array element " + e + " because element type "
              + eType + " is not a supported element type");
            Assert.IsTrue(ElementType.IsAssignableFrom(eType),
              "can not add array element " + e + " because element type "
              + eType + " is not assignable from existing element type " + ElementType);

            array.Add(e);

        }

        /// <summary>
        /// Affirms if this array contains the given element.
        /// </summary>
        /// <param name="e">an element</param>
        public bool HasValue(object e) {
            return array.Contains(e);
        }

        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        /// <returns>The element type. If the array is empty, returns object as type.</returns>
        public Type ElementType {
            get {
                foreach (object e in array) {
                    if (e != null) return e.GetType();
                }
                return typeof(object);
            }
        }

        /// <summary>
        /// Gets the number of elements in this array.
        /// </summary>
        /// <value>The length.</value>
        public int Length {
            get { return array.Count; }
        }

        /// <summary>
        /// Gets or Sets an element at a given index.
        /// </summary>
        /// <param name="index">a 0-based index (number) as string.</param>
        public object this[string index] {
            get {
                return GetElementAt(int.Parse(index));
            }

            set {
                PutElementAt(int.Parse(index), value);
            }
        }

        public object this[int index] {
            get { return GetElementAt(index); }
            set { PutElementAt(index, value); }
        }


        /// <summary>
        /// Gets the element at given index.
        /// </summary>
        /// <returns>The elememt at given index</returns>
        /// <param name="idx">an index in the range of (0,Length].</param>
        public object GetElementAt(int idx) {
            if (!IsInRange(idx)) {
                throw new IndexOutOfRangeException("Can not get element at"
                + " index " + idx + " from " + this);
            }
            return array[idx];
        }

        public object PutElementAt(int idx, object value) {
            if (!IsInRange(idx)) {
                throw new IndexOutOfRangeException("Can not put element at"
                + " index " + idx + " in " + this);
            }
            var old = GetElementAt(idx);
            array.Remove(idx);
            array.Insert(idx, value);
            return old;
        }


        public Array AsArray() {
            Array a = Array.CreateInstance(ElementType, array.Count);
            int i = 0;
            foreach (object e in array) {
                a.SetValue(e, new int[] { i });
                i++;
            }
            return a;
        }


        public override string ToString() {
            return "array<" + ElementType + ">[" + Length + "]";
        }


        public StringBuilder ToJSONString() {
            return ToJSONString(0);
        }

        bool IsPrimitive {
            get {
                Type eType = ElementType;
                return eType.IsValueType
                    || eType == typeof(object)
                    || eType == typeof(string);
            }
        }


        public StringBuilder ToJSONString(int tab) {
            var ws = tab > 0 && !IsPrimitive
                ? Environment.NewLine + StringHelper.WhiteSpace(tab) : "";
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < Length; i++) {
                if (buf.Length > 0) buf.Append(',');
                buf.Append(ws);
                Object e = this[i];
                buf.Append(StringHelper.Stringify(e, ws, tab));
            }
            return new StringBuilder()
                    .Append('[')
                    .Append(buf)
                    .Append(']');
        }


        internal bool IsInRange(int idx) {
            return idx >= 0 && idx < Length;
        }

        bool IsSupportedElementType(Type t) {
            return (t.IsValueType || t == typeof(string)
                || t == typeof(DataObject) || t.IsSubclassOf(typeof(DataObject))
                || t == typeof(DataObjectArray) || t.IsSubclassOf(typeof(DataObjectArray)));
        }


        /// <summary>
        /// Always returns false because an array has no named property.
        /// </summary>
        /// <returns><c>true</c>, if property was hased, <c>false</c> otherwise.</returns>
        /// <param name="proprty">Proprty.</param>
        public bool HasProperty(string proprty) {
            return false;
        }
    }
}
