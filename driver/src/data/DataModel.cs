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



/*!
*  \addtogroup data 
*  @{
*/

using System;
using System.Collections.Generic;
using oracle.kv.client.data;
using System.Text.RegularExpressions;
using oracle.kv.client.error;

namespace oracle.kv.client.data {


    /// <summary>
    /// Basic support for meta data management.
    /// </summary>
    internal class BaseMetaDataSupport : IMetaDataSupport {
        public ISchema Schema { get; set; }
        public static ICollection<ITable> EMPTY_TABLES = new List<ITable>().AsReadOnly();
        public static ICollection<IColumn> EMPTY_COLUMNS = new List<IColumn>().AsReadOnly();

        /// <summary>
        /// Creates a metadata model with an <see cref="EmptySchema"/> 
        /// </summary>
        internal BaseMetaDataSupport() {
            Schema = new EmptySchema();
        }

        /// <summary>
        /// Gets the tables in the schema.
        /// </summary>
        /// <value>The tables. Can be empty, but never null</value>
        public IEnumerable<ITable> Tables {
            get {
                return Schema.Tables;
            }
        }

        /// <summary>
        /// Affirms if the named table exists in this model.
        /// </summary>
        /// <returns><c>true</c>, if table is contaiend in the underlying schema.
        /// </returns>
        /// <param name="name">Name of a table.</param>
        public bool ContainsTable(string name) {
            return Schema.GetTable(name, false) != null;
        }

        /// <summary>
        /// Gets the table.
        /// </summary>
        /// <returns>The table.</returns>
        /// <param name="name">Name.</param>
        /// <param name="mustExist">If set to <c>true</c> must exist.</param>
        public ITable GetTable(string name, bool mustExist) {
            return Schema.GetTable(name, mustExist);
        }


        public IColumn GetColumn(string tableName, string columnPath, bool mustExist) {
            return Schema.GetColumn(tableName, columnPath, mustExist);

        }

        public bool RefreshSchema(ISchema newSchema) {
            if (newSchema == null) {
                throw new ArgumentException("Schema to refresh must not be null");
            }
            if (Schema != newSchema) {
                Schema = newSchema;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// A data model is implemented by delegation to other pluggable interfaces.
    /// </summary>
    /// <remarks>
    /// The implemetations of other interafces can be plugged-in.
    /// </remarks>
    internal class BaseDataModel : IDataModel {
        protected IMetaDataSupport MetaDataSupport { get; set; }
        protected ISerializationSupport SerdeSupport { get; set; }

        internal BaseDataModel() {
            MetaDataSupport = new BaseMetaDataSupport();
            SerdeSupport = new JSONParser();
        }

        protected BaseDataModel(IMetaDataSupport metadata,
            ISerializationSupport serialization) {
            MetaDataSupport = metadata;
            SerdeSupport = serialization;
        }

        public IEnumerable<ITable> Tables {
            get {
                return MetaDataSupport.Tables;
            }
        }


        public virtual ITable GetTable(string name, bool mustExist) {
            return MetaDataSupport.GetTable(name, mustExist);
        }

        public virtual IColumn GetColumn(string tableName, string columnName, bool mustExist) {
            return MetaDataSupport.GetColumn(tableName, columnName, mustExist);
        }


        public virtual IDataContainer Deserialize(string jsonString) {
            return SerdeSupport.Deserialize(jsonString);
        }

        public virtual string Serialize(object obj) {
            return SerdeSupport.Serialize(obj);
        }

        public virtual object PutValue<T>(IDataContainer container, string path,
                T userValue, Func<T, object> converter) {
            object storedValue = converter == null ? userValue :
                converter.Invoke(userValue);
            return new Path(null, path).PutValue(container, storedValue);
        }

        public virtual T GetValue<T>(IDataContainer container, string path, Func<object, T> converter) {
            object storedValue = new Path(null, path).GetValue(container);
            if (converter == null) {
                try {
                    return (T)Convert.ChangeType(storedValue, typeof(T));
                } catch (Exception ex) {
                    throw new ArgumentException("can not convert " + storedValue
                        + " of type " + storedValue.GetType() + " to type "
                        + typeof(T), ex);
                }
            } else {
                return converter.Invoke(storedValue);
            }
        }

        public virtual ISchema Schema {
            get {
                return MetaDataSupport.Schema;
            }
            set {
                MetaDataSupport.Schema = value;
            }
        }
    }
    /// <summary>
    /// Validates Path syntax.
    /// A path consists of one or more segments separated by DOT.
    /// </summary>
    internal static class PathUtil {
        public static readonly char PATH_SEPARATOR = '/';

        static Regex SimplePattern = new Regex(@"(^[a-zA-Z][a-zA-Z0-9_]*$)");

        static Regex ArrayPattern =
            new Regex(@"(?<SimpleName>\b[a-zA-Z][a-zA-Z0-9_]*\b)\[(?<ArrayIndex>\d+)\]");


        /// <summary>
        /// No of segments in given path.
        /// </summary>
        /// <remarks>The segments are separated by <see cref="PATH_SEPARATOR"/> 
        /// </remarks>
        /// <returns>The number of segments. A null or empty path has zeor segment</returns>
        /// <param name="path">a possibly multi-segment path.</param>
        /// <exception cref="ArgumentException"> if given path is invalid.
        /// </exception>
        public static int SegmentCount(string path) {
            Assert.IsTrue(!string.IsNullOrEmpty(path), path + " is null or empty");
            Assert.IsTrue(!IsValid(path), path + " is not valid");
            return path.Split(PATH_SEPARATOR).Length;
        }

        /// <summary>
        /// Affirms if given path is valid. 
        /// A valid path has at least one segment. Also each segment is
        /// <see cref="IsValidSegment"/>
        /// </summary>
        /// <param name="path">a path.</param>
        public static bool IsValid(string path) {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var segments = path.Split(PATH_SEPARATOR);
            foreach (string segment in segments) {
                if (!IsValidSegment(segment)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Affirms if the given segment is valid. 
        /// A valid segment is neither empty nor null and
        /// is either a <see cref="IsSimpleSegment"/> or <see cref="IsArraySegment"/>.
        /// </summary>
        /// <returns><c>true</c>, if valid segment, <c>false</c> otherwise.</returns>
        /// <param name="segment">Segment.</param>
        public static bool IsValidSegment(string segment) {
            if (string.IsNullOrWhiteSpace(segment)) return false;
            if (segment.Split(PATH_SEPARATOR).Length > 1) return false;
            return IsSimpleSegment(segment) || IsArraySegment(segment);
        }

        /// <summary>
        /// Gets first segment of given path.
        /// </summary>
        /// <returns>The first segment. null if given path is not valid.</returns>
        /// <param name="path">a path.</param>
        public static string FirstSegment(string path) {
            if (!IsValid(path)) return null;
            int idx = path.IndexOf(PATH_SEPARATOR);
            return idx >= 0 ? path.Substring(0, idx) : path;
        }

        /// <summary>
        /// Gets last segment of given path.
        /// </summary>
        /// <returns>The last segment. null if given path is not valid.
        /// Also null if given path has a single segment.</returns>
        /// <param name="path">a path.</param>
        public static string LastSegment(string path) {
            if (!IsValid(path)) return null;
            int idx = path.LastIndexOf(PATH_SEPARATOR);
            return idx >= 0 ? path.Substring(idx + 1) : null;
        }


        /// <summary>
        /// Gets next path of given path without the head.
        /// </summary>
        /// <returns>The next path. null if given path is not valid.
        /// Also null if given path has a single segment.</returns>
        /// <param name="path">a path.</param>
        public static string NextPath(string path) {
            if (!IsValid(path)) return null;
            int idx = path.IndexOf(PATH_SEPARATOR);
            return idx >= 0 ? path.Substring(idx + 1) : null;
        }



        /// <summary>
        /// Gets the head of a given path. Head of a path is its first segment.
        /// </summary>
        /// <returns>The head.</returns>
        /// <param name="path">Path.</param>
        public static string Head(string path) {
            return FirstSegment(path);
        }

        /// <summary>
        /// Gets the tail of a given path. Tail of a path is the path that 
        /// follows after its head.
        /// </summary>
        /// <returns>The head.</returns>
        /// <param name="path">Path.</param>
        public static string Tail(string path) {
            if (!IsValid(path)) return null;
            int idx = path.IndexOf(PATH_SEPARATOR);
            return idx >= 0 ? path.Substring(idx + 1) : null;
        }

        /// <summary>
        /// Affirms if given segment is an array segment.
        /// </summary>
        /// <param name="segment">a path segment.</param>
        public static bool IsArraySegment(string segment) {
            return ArrayPattern.IsMatch(segment);
        }

        /// <summary>
        /// Gets only the property name of the given segment.
        /// The property is the entire segment for a simple segment.
        /// The property is the name without array index in an array segment.
        /// </summary>
        /// <returns>The name.</returns>
        /// <param name="segment">Segment.</param>
        public static string PropertyName(string segment) {
            if (IsSimpleSegment(segment)) {
                return segment;
            } else if (IsArraySegment(segment)) {
                Match matchedPath = ArrayPattern.Matches(segment)[0];
                return matchedPath.Groups["SimpleName"].Value;
            } else {
                throw new ArgumentException("invalid path segment " + segment);
            }
        }

        /// <summary>
        /// Gets the array index of the given segment.
        /// The array index is -1 for a simple segmnent.
        /// The array index is an number in the given segment withing array symbol.
        /// </summary>
        /// <returns>The name.</returns>
        /// <param name="segment">Segment.</param>
        public static int ArrayIndex(string segment) {
            if (!IsArraySegment(segment)) {
                return -1;
            }
            Match matchedPath = ArrayPattern.Matches(segment)[0];
            return int.Parse(matchedPath.Groups["ArrayIndex"].Value);

        }

        public static bool IsSimpleSegment(string segment) {
            return SimplePattern.IsMatch(segment);
        }

    }

    // ----------------- Path based Navigation ------------------------ //


    /// <summary>
    /// A navigation path is used to navigate through nested data structure.
    /// </summary>
    internal class Path : IPath {
        public string ParentPath { get; private set; }
        public IPath Next { get; internal set; }
        readonly string Property;
        readonly int ArrayIndex;

        /// <summary>
        /// Creates a path with all segments.
        /// </summary>
        /// <param name="leadingPath">string for leading path, if any. 
        /// Can be null</param>
        /// <param name="str">a string for current path with segments following 
        /// the leding path.  The segments are separated by '.' character.
        /// </param>
        internal Path(string leadingPath, string str) {
            ParentPath = leadingPath;
            string head = PathUtil.Head(str);
            Property = PathUtil.PropertyName(head);
            ArrayIndex = PathUtil.ArrayIndex(head);

            string tail = PathUtil.Tail(str);
            Next = tail == null ? null :
                    new Path((leadingPath == null ? null : leadingPath + ".") + this,
                             tail);
        }


        public object GetValue(IDataContainer container) {
            return Resolve(container, null, false);
        }

        public object PutValue(IDataContainer container, object value) {
            return Resolve(container, value, true);
        }



        /// <summary>
        /// Gets a string represenation of current path segment. 
        /// For a string represenation of entire path use <see cref="FullPath"/>
        /// </summary>
        public override string ToString() {
            return Property + (ArrayIndex >= 0 ? "[" + ArrayIndex + "]" : "");
        }

        /// <summary>
        /// Gets a string represenation of entire path. 
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath {
            get {
                if (Next != null) {
                    return Next.FullPath;
                } else {
                    return (ParentPath == null ? "" : ParentPath + ".") + this;

                }
            }
        }

        /// <summary>
        /// Resolves this path on the specified container and gets the value or sets the
        /// given value. This is a recursive function.
        /// </summary>
        /// <returns>The current value for the path element for get operation 
        /// or the previous value for a set operation
        /// </returns>
        /// <param name="container">Container to be navigated.</param>
        /// <param name="valueToSet">Value to set if setValue is true.</param>
        /// <param name="setValue">If <c>true</c>, then sets the given value.</param>
        object Resolve(IDataContainer container, object valueToSet, bool setValue) {
            bool propertyExists = this.ExistsIn(container);
            object pathValue = propertyExists ? this.RawGet(container) : null;
            if (Next == null) { // this is last portion
                if (setValue) {
                    this.RawPut(container, valueToSet);
                } else { // get 
                    if (propertyExists) {
                        return pathValue;
                    } else {
                        throw new ArgumentException(FullPath + " does not exist");
                    }
                }
            } else { // next portion exists
                if (setValue) {
                    IDataContainer nextContainer = propertyExists ?
                        pathValue as IDataContainer : CreateEmptyObject();
                    if (!propertyExists) {
                        this.RawPut(container, nextContainer);
                    }
                    return Next.PutValue(nextContainer, valueToSet);
                } else {
                    IDataContainer nextContainer = null;
                    if (CanNavigate(pathValue, out nextContainer)) {
                        return Next.GetValue(nextContainer);
                    } else {
                        throw new ArgumentException("Can not navigate " + this
                        + " in " + this.FullPath + " because path value is "
                        + (pathValue == null ? "null" : "of type " + pathValue.GetType().FullName
                        + " which is not navigable"));
                    }
                }
            }
            return pathValue;
        }

        bool CanNavigate(object value, out IDataContainer container) {
            if (value is DataObject) {
                container = value as IDataContainer;
            } else if (value is DataObjectArray) {
                container = value as IDataContainer;
            } else {
                container = null;
            }
            return container != null;
        }

        IDataContainer CreateEmptyObject() {
            if (ArrayIndex >= 0) return new DataObjectArray();
            return new DataObject();
        }

        /// <summary>
        /// Affirms if the given container has the current segment of this path.
        /// This methood does not recurse.
        /// </summary>
        /// <param name="container">Container.</param>
        bool ExistsIn(IDataContainer container) {
            if (!container.HasProperty(Property)) return false;

            if (ArrayIndex >= 0) {
                object value = container[Property];
                if (value is DataObjectArray) {
                    return (value as DataObjectArray).IsInRange(ArrayIndex);
                }
            } else {
                return container is DataObject;
            }
            return false;
        }
        /// <summary>
        /// Gets the value from the given container. This method does not navigate
        /// through the nested structure.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="container">Container.</param>
        object RawGet(IDataContainer container) {
            if (container is DataObject) {
                object value = (container as DataObject)[Property];
                if (ArrayIndex >= 0) {
                    if (value is DataObjectArray) {
                        return (value as DataObjectArray)[ArrayIndex];
                    } else {
                        throw new ArgumentException("The segment [" + this + "] of "
                        + this.FullPath + " can not be evaluated because the value "
                        + (value == null ? " is null" : " is of type " + value.GetType())
                        + " is not an array");
                    }
                } else {
                    return value;
                }
            } else {
                throw new ArgumentException("The segment [" + this + "] of "
                + this.FullPath + " can not be evaluated because the value "
                + (container == null ? " is null" : " is of type " + container.GetType())
                + " is not a data container (was expecting a DataObject)");
            }

        }

        /// <summary>
        /// Puts the value to the given container. This method does not navigate
        /// through the nested structure.
        /// </summary>
        /// <param name="container">container to store the value.</param>
        /// <param name="value">raw value to be stored</param>
        void RawPut(IDataContainer container, object value) {
            if (container is DataObject) {
                (container as DataObject)[Property] = value;
            } else if (container is DataObjectArray) {
                (container as DataObjectArray)[ArrayIndex] = value;
            } else {
                throw new InternalError("unknown data container " + container.GetType());
            }
        }
    }

}
/*! @} End of Doxygen Groups*/
