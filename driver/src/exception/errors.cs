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
*  \addtogroup error
*  \brief Exception Support
*  @{
*/

/** 
  * Exceptions thrown by internal implemenation of the driver itself.
  * They are different than excption raised when an user operation fails.                                                              
  */
namespace oracle.kv.client.error {
    using System;
    using proxy.gen;
    using System.Diagnostics;

    /// <summary>
    /// Internal error is raised for implementation fault, not user error.
    /// </summary>
    public class InternalError : ArgumentException {
        public InternalError(string msg) : base(msg) { }
        public InternalError(string msg, Exception ex) : base(msg, ex) { }
    }

    /// <summary>
    /// Exception handler extracts an error message from exceptions arising
    /// out of Proxy Service.
    /// (which is an exception raised by Thrift server) and masks the thrift nature
    /// of the exception.
    /// </summary>
    internal class ExceptionHandler {

        /// <summary>
        /// Translate the specified exception if possible.
        /// </summary>
        /// <param name="ex">An exception.</param>
        /// <param name="ctx">an opaque object to describe the diagnostic context</param>
        /// <returns>null to indicate that the exception should be supressed
        /// and not propagated further if possible</returns>
        public static Exception translate(Exception ex, object ctx) {
            if (ex == null) {
                return new ArgumentException("null exception in " + ctx);
            }
            Type exType = ex.GetType();

            string msg = "\r\n\t" + ex.ToString()
                        + " invocation context=" + ctx ?? "none avaialble";

            if (ex.InnerException != null && ex.InnerException.Message != null) {
                msg = ex.InnerException.Message;
            }

            // some exception may need further translation
            if (ex is TRequestTimeoutException) {

            } else if (ex is TFaultException) {

            } else if (ex is TConsistencyException) {

            } else if (ex is TIllegalArgumentException) {
            } else if (ex is TUnverifiedConnectionException) {

            } else if (ex is TProxyException) {

            } else if (exType == typeof(TCancellationException)) {

            } else if (exType == typeof(TExecutionException)) {

            } else if (exType == typeof(TInterruptedException)) {

            } else if (exType == typeof(TTimeoutException)) {

            } else if (exType == typeof(TTableOpExecutionException)) {

            } else if (exType == typeof(TRequestLimitException)) {

            } else if (exType == typeof(TAuthenticationFailureException)) {

            } else if (exType == typeof(TAuthenticationRequiredException)) {

            } else if (exType == typeof(TUnauthorizedException)) {

            }

            return new ArgumentException(msg, ex);
        }


    }

    internal class Assert {
        public static void NotEmpty(string str) {
            NotEmpty(str, "cannot be empty or null");
        }

        public static void NotEmpty(string str, string msg) {
            IsTrue(!string.IsNullOrWhiteSpace(str), msg);
        }

        public static void NotNull(object obj) {
            NotNull(obj, null);
        }

        public static void NotNull(object obj, string msg) {
            NotNull(obj, msg, false);
        }

        /// <summary>
        /// Throws Exception if argument object is null.
        /// </summary>
        /// <param name="obj"> to be tested for null.</param>
        /// <param name="msg">Exception message.</param>
        /// <param name="isInternal">is internal fault</param>
        public static void NotNull(object obj, string msg, bool isInternal) {
            if (obj == null) {
                msg = (msg ?? "" + "instance is null")
                    + "\r\n\t" + new StackTrace().ToString();
                throw (isInternal) ? new InternalError(msg)
                                   : new ArgumentException(msg);
            }
        }

        public static void IsTrue(bool condition) {
            IsTrue(condition, "condition  is false", false);
        }

        public static void IsTrue(bool condition, string msg) {
            IsTrue(condition, msg, false);
        }

        public static void IsTrue(bool condition, string msg, bool isInternal) {
            IsTrue(condition, isInternal ? new InternalError(msg) :
                    new ArgumentException(msg));
        }

        /// <summary>
        /// Asserts if given condition is true.
        /// </summary>
        /// <remarks>
        /// If given condition is true, does nothing.
        /// Otherwise, if the given exception is null, throws an ArgumentException,
        /// else throws the given exception.
        /// </remarks>
        /// <param name="condition">If set to <c>true</c> condition.</param>
        /// <param name="ex">Execption to be thrown if condition is not true.
        /// If null, raises an ArgumentException.
        /// </param>
        public static void IsTrue(bool condition, Exception ex) {
            if (!condition) {
                if (ex == null) {
                    throw new ArgumentException("condition is false");
                } else {
                    throw ex;
                }
            }
        }
    }

}

/*! @} End of Doxygen Groups*/

