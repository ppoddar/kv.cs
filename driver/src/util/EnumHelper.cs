using System;
using System.Collections;
namespace oracle.kv.client.util {
    public static class EnumHelper {
        /// <summary>
        /// Affrms if the given string matches any of the values 
        /// of given enum in a case-insensitiev way.
        /// </summary>
        /// <returns><c>true</c>, if enum was valided, <c>false</c> otherwise.</returns>
        /// <param name="token">Token.</param>
        /// <typeparam name="E">The 1st type parameter.</typeparam>
        public static bool ValidEnum(Type enumType, string token) {
            foreach (string name in Enum.GetNames(enumType)) {
                if (string.Compare(name, token, true) == 0) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Resolves the gien string to an Enum.
        /// </summary>
        /// <returns>The enum. Null if the given string is not enum value</returns>
        /// <param name="enumType">Enum type.</param>
        /// <param name="token">Token.</param>
        public static object ResolveEnum(Type enumType, string token) {
            Array values = Enum.GetValues(enumType);
            foreach (object e in values) {
                if (string.Compare(e.ToString(), token, true) == 0) {
                    return e;
                }
            }
            return null;
        }


    }
}
