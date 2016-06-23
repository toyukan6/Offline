using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityLib;

namespace EnvironmentMaker {
    class TwoDLine {
        public double A { get; private set; }
        public double B { get; private set; }
        public List<Vector2> Vectors { get; private set; }
        public TwoDLine(double a, double b, List<Vector2> vectors) {
            A = a;
            B = b;
            Vectors = vectors;
        }

        public TwoDLine(double a, double b)
            : this(a, b, new List<Vector2>()) {
        }

        public double Calc(double x, double y) {
            return A * x - y + B;
        }

        public float CalcY(double x) {
            return (float)(A * x + B);
        }

        public double Calc(Vector2 vec) {
            return Calc(vec.x, vec.y);
        }

        public static TwoDLine LeastSquareMethod(List<Vector2> vectors) {
            double a, b;
            double sumX = vectors.Sum(v => v.x), sumY = vectors.Sum(v => v.y),
                sumXY = vectors.Sum(v => v.x * v.y), sumXX = vectors.Sum(v => v.x * v.x);
            int n = vectors.Count;
            a = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            b = (sumXX * sumY - sumX * sumXY) / (n * sumXX - sumX * sumX);
            return new TwoDLine(a, b, vectors);
        }

        public override string ToString() {
            return "z = " + A + "x + " + B;
        }
    }

    static class IListExtension {
        public static int IndexOfMax<T>(this IList<T> list, Func<T, double> predicate) {
            double maxValue = double.MinValue;
            int maxIndex = -1;
            for (int i = 0; i < list.Count; i++) {
                var value = predicate(list[i]);
                if (value > maxValue) {
                    maxValue = value;
                    maxIndex = i;
                }
            }
            return maxIndex;
        }

        public static int IndexOfMin<T>(this IList<T> list, Func<T, double> predicate) {
            double minValue = double.MaxValue;
            int minIndex = -1;
            for (int i = 0; i < list.Count; i++) {
                var value = predicate(list[i]);
                if (value < minValue) {
                    minValue = value;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public static double Median<T>(this IList<T> lists, Func<T, double> selector) {
            var list = lists.Select(p => selector(p)).ToList();
            return list.Median();
        }
    }

    static class Vector3Extension {
        public static Vector3 RotateXZ(this Vector3 vector, double theta) {
            var rotateX = Math.Cos(theta) * vector.x - Math.Sin(theta) * vector.z;
            var rotateZ = Math.Sin(theta) * vector.x + Math.Cos(theta) * vector.z;
            return new Vector3((float)rotateX, vector.y, (float)rotateZ);
        }
    }

    enum UpDown {
        Up,
        Down,
        Equal
    }
}
