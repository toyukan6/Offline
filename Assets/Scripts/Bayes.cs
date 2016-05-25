using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EnvironmentMaker {
    class Bayes {
        const int M = 2;
        const double ALPHA = 0.005, BETA = 11.1;

        private Matrix Phi(double x) {
            var data = new double[M + 1, 1];
            for (int i = 0; i < M + 1; i++) {
                data[i, 0] = Math.Pow(x, i);
            }
            return new Matrix(data);
        }

        private Matrix Mean(double x, Matrix sums, Matrix S) {
            var ret = BETA * Phi(x).Transpose * S * sums;
            return ret;
        }

        private Matrix MeanSum(List<Matrix> phiXList, List<double> tlist) {
            var sums = Matrix.Zero(M + 1, 1);
            for (int i = 0; i < phiXList.Count; i++) {
                sums += phiXList[i] * tlist[i];
            }
            return sums;
        }

        public List<Vector2> BayesEstimate(List<Vector2> points) {
            var result = new List<Vector2>();
            var xlist = points.Select(point => (double)point.x).ToList();
            var tlist = points.Select(point => (double)point.y).ToList();
            var phiX = xlist.Select(x => Phi(x)).ToList();
            var sums = Matrix.Zero(M + 1, M + 1);
            for (int i = 0; i < phiX.Count; i++) {
                sums += phiX[i] * phiX[i].Transpose;
            }
            var I = Matrix.Unit(M + 1);
            var S_inv = ALPHA * I + BETA * sums;
            var S = S_inv.Inverse;
            var minX = xlist.Min();
            var maxX = xlist.Max();
            int split = 500;
            var delta = (maxX - minX) / split;
            var meanSum = MeanSum(phiX, tlist);
            for (double n = minX; n <= maxX; n += delta) {
                var m = Mean(n, meanSum, S)[0, 0];
                result.Add(new Vector2((float)n, (float)m));
            }
            return result;
        }
    }
}
