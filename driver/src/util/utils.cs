using System;
using System.Collections.Generic;
using System.Text;
using oracle.kv.client.data;

namespace oracle.kv.client.util {
    public static class StringHelper {
        static readonly char DOUBLE_QUOTE = '"';
        static readonly char[] JSON_BRACKET = { '{', '}' };
        static readonly char[] ARRAY_BRACKET = { '[', ']' };

        /// <summary>
        /// Enclose the specified string with parenthesis.
        /// </summary>
        /// <returns>The enclosed string.</returns>
        /// <param name="str">String.</param>
        /// <param name="parenthesis">Parenthesis.</param>
        public static string Enclose(string str, char[] parenthesis) {
            return parenthesis[0] + str + parenthesis[1];
        }

        /// <summary>
        /// Enclose the specified buffer with parenthesis.
        /// </summary>
        /// <returns>The enclosed string.</returns>
        /// <param name="buf">Buffer.</param>
        /// <param name="parenthesis">Parenthesis.</param>
        public static string Enclose(StringBuilder buf, char[] parenthesis) {
            return new StringBuilder()
                .Append(parenthesis[0])
                .Append(buf)
                .Append(parenthesis[1])
                .ToString();
        }

        /// <summary>
        /// Enclose the specified string with doube quotaion mark.
        /// </summary>
        /// <returns>The quoted string.</returns>
        /// <param name="str">String.</param>
        public static string Quote(string str) {
            return Enclose(str, new char[] { DOUBLE_QUOTE, DOUBLE_QUOTE });
        }

        /// <summary>
        /// Gets white space characters.
        /// </summary>
        /// <returns>The space charaters as string.</returns>
        /// <param name="tab">number of space characters.</param>
        public static string WhiteSpace(int tab) {
            string ws = "";
            for (int i = 0; i < tab; i++) {
                ws += ' ';
            }
            return ws;
        }

        /// <summary>
        /// Stringifies the given container.
        /// </summary>
        /// <remarks>Each property of the container is stringified as name-value
        /// pair. If value is a container, then value is stringified.
        /// </remarks>
        /// <returns>The stringified container.</returns>
        /// <param name="data">Data container to be stringified.</param>
        /// <param name="ws">Ws white space between each data element.</param>
        /// <param name="tabIncrement">number of tabspaces to use for nested
        /// nested data container</param>
        public static StringBuilder Stringify(IDataContainer data, string ws,
            int tabIncrement) {
            StringBuilder buf = new StringBuilder();
            foreach (string propertyName in data.PropertyNames) {
                if (buf.Length > 0) buf.Append(',');
                buf.Append(ws);
                object value = data[propertyName];
                buf.Append(Quote(propertyName)).Append(':');
                if (value == null) {
                    buf.Append(Literal.NULL);
                } else if (value is string) {
                    buf.Append(StringHelper.Quote(((string)value)));
                } else if (value is bool) {
                    buf.Append(Literal.FromString(value.ToString()));
                } else if (value is IDataContainer) {
                    buf.Append((value as IDataContainer).ToJSONString(
                                    ws.Length + tabIncrement));
                } else {
                    buf.Append(value);
                }
            }
            return buf;
        }

        public static StringBuilder Stringify(DataObject data, string ws, int tab) {
            StringBuilder buf = Stringify(data as IDataContainer, ws, tab);
            return new StringBuilder().Append('{').Append(buf).Append('}');
        }

        public static StringBuilder Stringify(DataObjectArray array, string ws, int tab) {
            StringBuilder buf = Stringify(array as IDataContainer, ws, tab);
            return new StringBuilder().Append('[').Append(buf).Append(']');
        }


        public static StringBuilder Stringify(object value, string ws, int tab) {
            StringBuilder buf = new StringBuilder();
            if (value is DataObject) {
                buf.Append(Stringify(value as DataObject, ws, tab));
            } else if (value is DataObjectArray) {
                buf.Append(Stringify(value as DataObjectArray, ws, tab));
            } else if (value == null) {
                buf.Append(Literal.NULL);
            } else if (value is string) {
                buf.Append(StringHelper.Quote(((string)value)));
            } else if (value is bool) {
                buf.Append(Literal.FromString(value.ToString()));
            } else {
                buf.Append(value);
            }
            return buf;
        }

        public static StringBuilder Stringify<T>(IEnumerable<T> items) {
            StringBuilder buf = new StringBuilder();
            var props = typeof(T).GetProperties();

            foreach (var prop in props) {
                buf.Append(prop.Name);
            }
            buf.Append('=');
            foreach (var item in items) {
                foreach (var prop in props) {
                    buf.Append(prop.GetValue(item, null));
                }
            }
            return buf;
        }
    }
}




