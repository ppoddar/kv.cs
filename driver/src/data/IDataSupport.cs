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
* \brief Semi-Typed, Nested Data Model
*  @{
*/


/**
 * Semi-typed, nested data definition
 */

namespace oracle.kv.client.data {
    using System;
    using System.Text;
    using System.Collections.Generic;

    /// <summary>
    /// A data model is used by the driver to negotiate meta-data and data
    /// with a NoSQL datastore.
    /// </summary>
    /// <remarks>
    /// The data is sourced from Oracle NoSQL databas, is made available to
    /// C# language application and is maintained in memory by the driver in a 
    /// canonical form <see cref="IDataContainer"/>.
    /// The canonical form supports a type system with
    /// partial type support that excludes any user-defined type, but recognizes
    /// composite type which allow nested strcture and navigation. 
    /// 
    /// <para></para>
    /// A data model uses the type system support to control 
    /// <list type="bullet">
    /// <item>read</item>
    ///   <description>how data is accepted from input of this interface,</description>
    /// <item>write</item>
    /// <description>how data is presented to database server</description>
    /// <item>exchange</item>
    /// <description>how data is represented across processes of that use
    /// different langauge runtime</description>
    /// </list>
    /// <para></para>
    /// This interface combines these core facilities defined in separate
    /// interfaces:
    /// <list type="bullet">
    /// <item><term><see cref="IMetaDataSupport"/> </term>
    /// <description>controls type of data being handled. 
    /// The metadata negotiates between two Type Systems, namely, database and C# 
    /// types. 
    /// MetaData supports mapping between all supported type of database
    /// and subset of C# types: all Value types (byte, short,int,duble etc), string
    /// and <see cref="IDataContainer"/> which are composite types defined by
    /// this driver itself.
    /// Metadata does not support application-defined types. The composite types
    /// <see cref="IDataContainer"/> also does not contain any application-defined
    /// type.
    /// Hence, the type support is called semi-typed.
    /// </description>
    /// </item>
    /// <item><term><see cref="ISerializationSupport"/></term>
    /// <description>controls how data is represnted 
    /// for propagation across processes that use different langauge runtime. 
    /// <para></para>
    /// JSON being de-facto standard for language neutral data exchange 
    /// -- the serialization support, by deafult, uses JSON for inter-process data.
    /// However, this interface is not
    /// restricted to JSON format and makes it possible to plug-in other formats.
    /// </description>
    /// </item>
    /// </list>
    /// <para></para>
    /// This interface supports navigation through the nested data structre of
    /// <see cref="IDataContainer"/>. A data container supports composite types,
    /// but does not naviagte through a composite value. This interface, on the
    /// other hand, supports navigation through composite values in a given
    /// <see cref="IDataContainer"/> object. 
    /// </remarks>
    public interface IDataModel : IMetaDataSupport, ISerializationSupport {
        /// <summary>
        /// Gets the value of given path in the given data container.
        /// </summary>
        /// <remarks>
        /// An <see cref="IDataContainer"/> can store composite values, 
        /// but a container itself does not naviagte through composite values. 
        /// Whereas, this interface method can get a nested value 
        /// navigating a path from a given container.   
        /// <para></para>
        /// It is noteworthy that a value retuned by this method may be
        /// different than the value actually stored in the data container.
        /// The  value stored in the data container is transformed by
        /// method to transform the value stored in the data container before
        /// the data being availale to an user of this interface.
        /// </remarks>
        /// <returns>The value of given path in the given container,
        /// transformed accoding to this data model.
        /// </returns>
        /// <param name="container">the container on which the path 
        /// is defined.</param>
        /// <param name="path">a path like string to refer an element in a given 
        /// container </param>
        /// <param name="converter">a function to convert type of the stored value
        /// to a different type return value.
        /// Can be null to skip type conversion.</param>
        /// <exception cref="System.ArgumentException"> if path is not defined in the given
        /// data container</exception>
        T GetValue<T>(IDataContainer container, string path, Func<object, T> converter);

        /// <summary>
        /// Puts the given value on given path element of the given container.
        /// </summary>
        /// <remarks>
        /// A container <see cref="IDataContainer"/> can store composite values, 
        /// but a container does not naviagte through composite values. 
        /// Whereas, this interface method can set a nested value navigating 
        /// a path from a given container.   
        /// <para></para>
        /// It is noteworthy that a value supplied to this method may be
        /// different than the value actually stored in the comtainer.
        /// The stored value is transformed 
        /// A derivated model can transform the value differently.
        /// </remarks>
        /// <returns>The existing value on given path element of the given container.</returns>
        /// <param name="container">the container on which the path is defined.</param>
        /// <param name="path">a path like string to refer an element in a given container </param>
        /// <param name="useValue">user value to store. </param>
        /// <param name="converter">a function to convert type user supplied value
        /// to a different type of value for storage.
        /// can be null to skip conversion</param> 
        object PutValue<T>(IDataContainer container, string path, T useValue,
            Func<T, object> converter);

        ISchema Schema { get; set; }
    }


    /// <summary>
    /// Defines serialization/deserialization of canonical <see cref="IDataContainer"/>.
    /// </summary>
    /// <remarks> 
    /// This interface does not abstract serialization/deserialization of
    /// application-defined custom data types, but a canonical, semi-typed system 
    /// <see cref="IDataContainer"/> as used by the database driver. 
    /// <para></para>
    /// The data needs to deserialzated from string, because the data can
    /// be supplied by a any non-C# process. Cuurently the data arrives 
    /// (from proxy service) as JSON formatted string, but the serialization/
    /// desrialization of a data should handle other format as well.
    /// <para></para> 
    /// JSON provides a de facto language-neutral data format. The default implementation
    /// of this interface parses JSON format, but this interface does not assume 
    /// any specific  format.
    /// </remarks> 
    public interface ISerializationSupport {
        /// <summary>
        /// Deserializes the specified string to a canonical <see cref="IDataContainer"/>.
        /// </summary>
        /// <returns>The canonical object generated by deserialization.</returns>
        /// <param name="jsonString">an input string, most likely to be in
        /// JSON format, but not neccessarily.</param>
        /// <exception cref="System.ArgumentException"> if can not deserialize the
        /// the input string</exception>/
        IDataContainer Deserialize(string jsonString);

        /// <summary>
        /// Serialize the specified object.
        /// </summary>
        /// <returns>The serialized string, most likely to be in
        /// JSON format, but not neccessarily.</returns>
        /// <param name="obj">an object to be serialized.</param>
        /// <exception cref="System.ArgumentException"> if can not serialize the
        /// the input object</exception>/
        string Serialize(object obj);
    }
    /// <summary>
    /// A container holds data values. The values are indexed by 
    /// property name, and may also by 0-based integral index for containers
    /// that behave as array but have no named property.
    /// </summary>
    /// <remarks>
    /// An implementation restricts the contained values. The restriction
    /// may apply only on type of values but can also be applied by actual
    /// value.    
    /// A container does not navigate to nested composite value(s).
    /// Access/Mutation of a nested value is provided by <see cref="IDataModel"/>
    /// </remarks>
    public interface IDataContainer {

        /// <summary>
        /// Represents as a JSON formattted string.
        /// The string is formatted without line break or any whitespace
        /// between each property.
        /// </summary>
        /// <returns>The JSON string.</returns>
        StringBuilder ToJSONString();

        /// <summary>
        /// Represents as a JSON formattted string.
        /// The string is formatted with line break followed by number
        /// of given space charactre between each property
        /// </summary>
        /// <returns>The JSON string.</returns>
        StringBuilder ToJSONString(int tab);

        /// <summary>
        /// Affirms if the named property exists in this container.
        /// </summary>
        /// <remarks>
        /// This affirmation is for properties of this container only and it
        /// does not navigate to nested composite value(s) contained in this container.
        /// </remarks>
        /// <returns><c>true</c>, if property exists in this container.</returns>
        /// <param name="property">Proprty.</param>
        bool HasProperty(string property);

        string[] PropertyNames { get; }

        /// <summary>
        /// Allows array like syntax to get/set values by property name. 
        /// </summary>
        /// <remarks>
        /// This property refers to properties of this container only and it
        /// does not navigate to composite value(s) contained in this container.
        /// <para></para>
        /// Access to nested value is provided by <see cref="IDataModel"/>
        ///
        /// <para></para>
        /// These values are stored as they were preneted.
        /// They are returned as they had been stored.
        /// No conversion. 
        /// </remarks>
        /// <param name="property">Property name.</param>
        object this[string property] { get; set; }

    }

    /// <summary>
    /// A path navigates through a data container to a nested  value.
    /// </summary>
    /// <remarks>
    /// A path consists of a series of path segments separated by '.' character.
    /// A path describes a syntax to refer an element in a data container,
    /// for example, <code>person.address[2].city</code>.
    /// <para></para>
    /// A path uses following syntax:
    /// <pre>
    ///  path         := path_segment ['.' path_segment]*
    ///  path_segment := simple_path | array_path
    ///  array_path   := simple_path '[' array_index ']'
    ///  simple_path  := [a-zA-Z][a-zA-Z0-9_]*
    ///  array_index  := [0-9]+
    /// </pre>
    /// </remarks>
    public interface IPath {
        /// <summary>
        /// Gets the next path segment, if any.
        /// </summary>
        /// <value>The next path segment. can be null</value>
        IPath Next { get; }


        /// <summary>
        /// Gets the full path including leading and following path.
        /// </summary>
        /// <value>The full path.</value>
        string FullPath { get; }

        /// <summary>
        /// Extract the value of the element referred by this path with 
        /// the given container as root.
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="container">Container.</param>
        object GetValue(IDataContainer container);

        /// <summary>
        /// Puts the given value of the element referred by this path with the 
        /// given container as root.
        /// </summary>
        /// <remarks>
        /// The intermediate segments are created if they do not exist.
        /// </remarks>
        /// <returns>The value stored before put operation. null if no value
        /// was stored or it was actually null.</returns>
        /// <param name="container">the root container.</param>
        /// <param name="valueToStore">Value to store.</param>
        object PutValue(IDataContainer container, object valueToStore);
    }


    /// <summary>
    /// Provides meta data information. 
    /// </summary>/
    /// <remarks>
    /// Meta data can originate from database, file or can be empty.
    /// The underlying schema is organized with table-column metaphor.
    /// A table and a column with naviation path can be located via
    /// this interface. For example, a column such as <c>address.city</c> 
    /// for a table named <c>person</c> can be found.
    /// <para></para> 
    /// This interface is mainly read-only.
    /// </remarks>
    public interface IMetaDataSupport {
        /// <summary>
        /// Gets or sets the underlying schema.
        /// </summary>
        ISchema Schema { get; set; }

        /// <summary>
        /// Gets all supported tables. 
        /// </summary>
        /// <value>all tables defined in current schema. Can be empty, but never
        ///  null.</value>
        IEnumerable<ITable> Tables { get; }

        /// <summary>
        /// Gets the table of given name.
        /// </summary>
        /// <returns>Handle to a table, if avaialble.</returns>
        /// <param name="name">Name of the table. Case-sensitive.</param>
        /// <param name="mustExist">If <c>true</c>, table must exist in schema,
        /// otherwise an expection is raised. If <c>false</c>, then
        /// <c>null</c> is returned if table can not be found.
        /// </param>
        ITable GetTable(string name, bool mustExist);

        /// <summary>
        /// Gets the column of a table.
        /// </summary>
        /// <returns>Handle to a column, if avaialble.</returns>
        /// <param name="tableName">Name of the table. Case-sensitive.</param>
        /// <param name="columnPath">Path name to a column name. The path
        /// may have more than one segment separted by DOT <c>'.'</c> character</param>
        /// <param name="mustExist">If <c>true</c>, table and column must exist,
        /// otherwise an expection is raised. If <c>false</c>, then
        /// a <c>null</c> is returned if table or column can not be found.</param>
        IColumn GetColumn(string tableName, string columnPath, bool mustExist);
    }

    /// <summary>
    /// A schema describes of <see cref="ITable"/>. 
    /// A <see cref="ITable"/> can be looked up by its name. 
    /// </summary>
    public interface ISchema {
        IDataModel DataModel { get; set; }
        /// <summary>
        /// Gets the table defintions contained in this schema.
        /// </summary>
        /// <value>The definition of all tables in this schema. 
        /// Can be empty but never null.
        /// </value>
        IEnumerable<ITable> Tables { get; }

        /// <summary>
        /// Gets the  defintion identifed by table name.
        /// </summary>
        /// <returns>A table definition. Can be null if named table doew
        /// not exist and mustExist is set to false
        /// </returns>
        /// <param name="tableName">name of a table.</param>
        /// <param name="mustExist">If <c>true</c>, raises an exception if table
        /// of given name can not be found in schema. If <c>false</c>, returns
        /// null if table of given name an not be found in schema. </param>
        ITable GetTable(string tableName, bool mustExist);

        /// <summary>
        /// Gets the column in a given tablle.
        /// </summary>
        /// <remarks>
        /// Naviagtes through nested coloumns.
        /// A nested column is named with more than one segment where each
        /// segment is separated by <see cref="PathUtil.PATH_SEPARATOR"/> symbol. 
        /// </remarks>
        /// <returns>The column. Can be null if column does not exist and 
        /// mustExist is set to false</returns>
        /// <param name="tableName">name of a table.</param>
        /// <param name="columnName">name of a column. can be a path like
        /// name. A path segment can have array index.</param>
        /// <param name="mustExist">If <c>true</c>, raises an exception if table
        /// or column of given name can not be found in schema. If <c>false</c>, 
        /// returns null if table or column of given name an not be found in schema. </param>
        IColumn GetColumn(string tableName, string columnName, bool mustExist);
    }

    /// <summary>
    /// A table describes itself and a set of <see cref="IColumn"/>.
    /// </summary>
    public interface ITable {
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Affirms if this table is transient.
        /// </summary>
        /// <remarks>
        /// The rows created from a transient table
        /// are also trainsient.
        /// </remarks>
        /// <value><c>true</c> if is transient; otherwise, <c>false</c>.</value>
//        bool IsTransient { get; }

        /// <summary>
        /// Gets the definition of columns of this table, in no particular order.
        /// </summary>
        IEnumerable<IColumn> Columns { get; }

        /// <summary>
        /// Gets names of all columns defined in this table.
        /// </summary>
        /// <value>The column names. Can be empty but never null</value>
        string[] ColumnNames { get; }

        /// <summary>
        /// Gets the column definition that constitute primary key for this table.
        /// </summary>
        /// <value>The definition of primary key column(s).</value>
        IColumn[] PrimaryKeys { get; }


        /// <summary>
        /// Gets the column names that constitute primary key for this table.
        /// </summary>
        /// <value>The name of primary key column(s).</value>
        string[] PrimaryKeyNames { get; }

        /// <summary>
        /// Gets a column of this table.
        /// </summary>
        /// <returns>The column.</returns>
        /// <param name="columnName">name of a column. name can represnt 
        /// a nested column</param>
        /// <param name="mustExist">If <c>true</c>, raises an exception if table
        /// or column of given name can not be found in schema. If <c>false</c>, 
        /// returns null if table or column of given name an not be found in schema. </param>
        IColumn GetColumn(string columnName, bool mustExist);


        /// <summary>
        /// Affirms a column of given name exists this table.
        /// </summary>
        /// <param name="columnName">name of a column. name can represnt 
        /// a nested column</param>
        bool HasColumn(string columnName);
    }

    /// <summary>
    /// A column represents property of a row.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public interface IColumn {
        /// <summary>
        /// name of the column is same as name of a property in a row.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Affirms if this column represnts a composiet type.
        /// </summary>
        bool IsComposite { get; }

        /// <summary>
        /// Gets name of database type of value held in this column.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <value>The database type name.</value>
        string DatabaseTypeName { get; }

        /// <summary>
        /// Gets the type of value in the column when a value in stored
        /// in canonical form.
        /// </summary>
        /// <value>The type of the language.</value>
        Type LanguageType { get; }

        /// <summary>
        /// Affirms if this colun is a primary key.
        /// </summary>
        /// <value><c>true</c> if is primary key; otherwise, <c>false</c>.</value>
        bool IsPrimaryKey { get; }

        /// <summary>
        /// Converts the type of given value to a type that can be stored in 
        /// database
        /// </summary>
        /// <returns>a database type.</returns>
        /// <param name="value">Value as stored in canonical form.</param>
        object ConvertToDatabaseType(object value);

        /// <summary>
        /// Converts the type of given value to a type that can is viewable  
        /// by user
        /// </summary>
        /// <returns>a language type.</returns>
        /// <param name="value">Value as seen by the user.</param>
        object ConvertToLanguageType(object value);
    }

}
/*! @} End of Doxygen Groups*/
