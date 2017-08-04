using System;

using System.Collections.Generic;

namespace oracle.kv.client.util {

    /// <summary>
    /// Calculates Levensthine distance between strings.
    /// </summary>
    public static class LevensthineDistance {
        private static readonly int COST_DELETE = 2;
        private static readonly int COST_ADD = 2;
        private static readonly int COST_REPLACE = 1;

        /// <summary>
        /// Finds the 'nearest' string to the specified key 
        /// among the candidate strings.
        /// </summary>
        /// <returns>The nearest string.</returns>
        /// <param name="key">the string to match.</param>
        /// <param name="candidates">the candidate strings.</param>
        public static string Closest(string key, List<string> candidates) {
            double min = double.MaxValue;
            string closest = null;
            foreach (string c in candidates) {
                double d = Distance(key, c);
                if (d < min) {
                    min = d;
                    closest = c;
                }
            }
            return closest;
        }

        /// <summary>
        /// Calculates distance between the specified strings.
        /// </summary>
        /// <returns>The distance.</returns>
        /// <param name="s1">one string</param>
        /// <param name="s2">other string</param>
        public static double Distance(string s1, string s2) {
            if (s1 == null) return double.MaxValue;
            if (s2 == null) return double.MaxValue;

            double[,] matrix = new double[s1.Length, s2.Length];
            double[,] cost = new double[s1.Length, s2.Length];
            for (int i = 0; i < s1.Length; i++) {
                for (int j = 0; j < s2.Length; j++) {
                    matrix[i, j] = s1[i] == s2[j] ? 0 : 1;
                }
            }
            for (int i = 1; i < s1.Length; i++) {
                for (int j = 1; j < s2.Length; j++) {
                    double c1 = cost[i - 1, j] + COST_ADD;
                    double c2 = cost[i, j - 1] + COST_DELETE;
                    double c3 = cost[i - 1, j - 1] + COST_REPLACE;
                    cost[i, j] = Math.Min(c3, Math.Min(c1, c2))
                            + Distance(s1[i], s2[j]);
                }
            }
            return cost[s1.Length - 1, s2.Length - 1];
        }

        static int Distance(char a, char b) {
            return Math.Abs(char.ToLower(a) - char.ToLower(b));
        }
    }
}
