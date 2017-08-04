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
*  \addtogroup logging
*  \brief Logging support
*  @{
*/

/**
  *  Logging related classes. Provides channel-oriented loggers.

*/
namespace oracle.kv.client.log {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using oracle.kv.client.config;
    using oracle.kv.client.util;
    using oracle.kv.client.error;

    /// <summary>
    /// Logging channel specifies a channel for logging messages.
    /// </summary>
    public enum LogChannel {
        /// <summary>
        /// Log messages of for API-related operations.
        /// </summary>
        RUNTIME,

        /// <summary>
        /// Log messages of for configuration operations.
        /// </summary>
        CONFIG,

        /// <summary>
        /// Log messages of for Proxy-related operations.
        /// </summary>
        PROXY,

        /// <summary>
        /// Log messages from a remote process.
        /// </summary>
        REMOTE

    };

    /// <summary>
    /// configures and manages a channel-based logging.
    /// </summary>
    internal class LogManger {
        private static Dictionary<LogChannel, Logger> Loggers =
            new Dictionary<LogChannel, Logger>();

        /// <summary>
        /// Parses a string to determine the log level of given channel.
        /// </summary>
        /// <returns>The log level of given channel as per the given
        /// logging specification.</returns>
        /// <param name="spec">logging Specification. The specification
        /// is in form of a comma-separated {key}={value} pairs.
        /// Each {key} is the name of a logging channel and value
        /// is a TraceLevel.
        /// </param>
        /// <param name="spec">Channel.</param>
        public static void Configure(string spec) {
            TraceLevel dlevel = (TraceLevel)Options.LOG_LEVEL.Default;
            foreach (LogChannel channel in Enum.GetValues(typeof(LogChannel))) {
                Loggers[channel] = new Logger(channel, dlevel);
            }

            if (string.IsNullOrEmpty(spec)) return;

            string[] tokens = spec.Split(',');
            foreach (string token in tokens) {
                string[] parts = token.Split('=');
                if (parts.Length != 2) {
                    Console.WriteLine("invalid log specification [" + spec + "]");
                    break;
                }
                string channelName = parts[0].Trim();
                string levelName = parts[1].Trim();
                if (!EnumHelper.ValidEnum(typeof(LogChannel), channelName)) {
                    Console.WriteLine("invalid log channel  '" + channelName + "' in " + spec);
                    continue;
                }
                if (!EnumHelper.ValidEnum(typeof(TraceLevel), levelName)) {
                    Console.WriteLine("invalid log level  '" + levelName + "' in " + spec);
                    continue;
                }
                LogChannel channel = (LogChannel)
                    EnumHelper.ResolveEnum(typeof(LogChannel), channelName);
                TraceLevel level = (TraceLevel)
                    EnumHelper.ResolveEnum(typeof(TraceLevel), levelName);
                GetLogger(channel).TraceLevel = level;

            }
        }



        /// <summary>
        /// Gets a cached logger that would log messages in given channel.
        /// </summary>
        /// <returns>The logger.</returns>
        /// <param name="channel">Channel to log messages</param>
        public static Logger GetLogger(LogChannel channel) {
            if (Loggers.ContainsKey(channel)) {
                return Loggers[channel];
            } else {
                Logger l = new Logger(channel, (TraceLevel)
                    Options.LOG_LEVEL.Default);
                Loggers[channel] = l;
                return l;
            }
        }
    }

    /// <summary> Logs runtime messages to console.
    /// </summary>
    public class Logger {
        /// <summary>
        /// the channel of this logger.
        /// </summary>
        /// <value>The log channel.</value>
        public LogChannel Channel { get; private set; }

        /// <summary>
        /// Gets or sets the trace level.
        /// </summary>
        /// <value>The trace level.</value>
        public TraceLevel TraceLevel { get; set; }

        public Logger(LogChannel channel, TraceLevel level) {
            Channel = channel;
            TraceLevel = level;
        }

        public void Info(string s) {
            Log(TraceLevel.Info, s);
        }

        public void Trace(string s) {
            Log(TraceLevel.Verbose, s);
        }

        public void Warn(string s) {
            Log(TraceLevel.Warning, s);
        }

        public void Error(string s) {
            Log(TraceLevel.Error, s);
        }

        private void Log(TraceLevel level, string s) {
            if (level.CompareTo(TraceLevel) > 0) return;
            string msg = Channel.ToString() + ":" + s;
            Console.WriteLine(msg);
        }

        public void Debug(string msg) {
            Console.WriteLine(msg);
        }
    }

}
/*! @} End of Doxygen Groups*/
