using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    static class Functions {
        static System.Random rand = new System.Random();
        /// <summary>
        /// レイヤー一覧
        /// </summary>
        public static int[] SortingLayerUniqueIDs;

        public static void Initialize() {
            using (var reader = new StreamReader("layer.txt")) {
                var layers = new List<int>();
                string str = reader.ReadLine();
                while (str != null) {
                    layers.Add(int.Parse(str));
                    str = reader.ReadLine();
                }
                SortingLayerUniqueIDs = layers.ToArray();
            }
        }

        public static int GetRandomInt(int max) { return rand.Next(max); }
        public static double GetRandomDouble() { return rand.NextDouble(); }
        public static double GetRandomDouble(double min, double max) { return rand.NextDouble() * (max - min) + min; }

        public static Vector3 ColorToVector3(Color color) {
            return new Vector3(color.r, color.g, color.b);
        }

        public static double GetCos(Vector3 vec1, Vector3 vec2) {
            double inner = vec1.x * vec2.x + vec1.y * vec2.y + vec1.y * vec2.y;
            double ret = inner / vec1.magnitude / vec2.magnitude;
            if (ret > 1) ret = 1;
            else if (ret < -1) ret = -1;
            return ret;
        }

        public static List<T3> ZipWith<T1, T2, T3>(List<T1> list1, List<T2> list2, Func<T1, T2, T3> func) {
            var result = new List<T3>();
            int max = Math.Min(list1.Count, list2.Count);
            for (int i = 0; i < max; i++) {
                result.Add(func(list1[i], list2[i]));
            }
            return result;
        }

        public static Color SubColor(Color c1, Color c2) {
            float red, green, blue, alpha;
            red = Math.Abs(c1.r - c2.r);
            green = Math.Abs(c1.g - c2.g);
            blue = Math.Abs(c1.b - c2.b);
            alpha = Math.Abs(c1.a - c2.a);
            return new Color(red, green, blue, alpha);
        }

        public static Point AveragePoint(List<Point> vecs) {
            float x = 0, y = 0, z = 0, r = 0, g = 0, b = 0;
            foreach (var v in vecs) {
                x += v.X;
                y += v.Y;
                z += v.Z;
                r += v.Red;
                g += v.Green;
                b += v.Blue;
            }
            x /= vecs.Count;
            y /= vecs.Count;
            z /= vecs.Count;
            r /= vecs.Count;
            g /= vecs.Count;
            b /= vecs.Count;
            return new Point(x, y, z, (byte)r, (byte)g, (byte)b);
        }

        public static Vector3 AverageVector(List<Vector3> vecs) {
            if (vecs.Count > 0) {
                float x = 0, y = 0, z = 0;
                foreach (var v in vecs) {
                    x += v.x;
                    y += v.y;
                    z += v.z;
                }
                return new Vector3(x, y, z) / vecs.Count;
            } else {
                return Vector3.zero;
            }
        }

        public static Color AverageColor(List<Color> colors) {
            if (colors.Count > 0) {
                float red = 0, green = 0, blue = 0, alpha = 0;
                foreach (var c in colors) {
                    red += c.r;
                    green += c.g;
                    blue += c.b;
                    alpha += c.a;
                }
                red /= colors.Count;
                green /= colors.Count;
                blue /= colors.Count;
                alpha /= colors.Count;
                return new Color(red, green, blue, alpha);
            } else {
                return new Color(0, 0, 0, 0);
            }
        }

        public static Vector3 CrossProduct(Vector3 v1, Vector3 v2) {
            return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y - v2.x);
        }

        public static int LISLength(int[] array) {
            int[] a = new int[array.Length + 1], l = new int[array.Length + 1], p = new int[array.Length + 1];
            a[0] = array.Min() - 1;
            for (int i = 0; i < array.Length; i++) {
                a[i + 1] = array[i];
            }
            for (int i = 0; i < l.Length; i++) {
                l[i] = 0; p[i] = 0;
            }
            for (int i = 1; i < a.Length; i++) {
                int k = 0;
                for (int j = 0; j < i; j++) {
                    if (a[j] < a[i] && l[j] > l[k]) {
                        k = j;
                    }
                }
                l[i] = l[k] + 1; p[i] = k;
            }
            return l.Max();
        }
    }
}
