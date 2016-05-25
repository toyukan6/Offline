using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentMaker {
    class Matrix {
        private double[,] matrix;

        public int Width { get { return matrix.GetLength(1); } }
        public int Height { get { return matrix.GetLength(0); } }

        private bool decomposited = false;

        private double[,] u;
        private double[,] l;

        public double[,] U {
            get {
                if (!decomposited) {
                    LUDecomposition();
                }
                return u;
            }
        }

        public double[,] L {
            get {
                if (!decomposited) {
                    LUDecomposition();
                }
                return l;
            }
        }

        public Matrix(int height, int width) {
            matrix = new double[height, width];
        }

        public Matrix(double[,] matrix) {
            this.matrix = matrix.Clone() as double[,];
        }

        public double this[int index1, int index2] {
            get {
                return matrix[index1, index2];
            } 
            set {
                if (matrix[index1, index2] != value) {
                    decomposited = false;
                }
                matrix[index1, index2] = value;
            }
        }

        public double[] GetColumn(int index) {
            var column = new double[Height];
            if (index >= 0 && index < Width) {
                for (int i = 0; i < column.Length; i++) {
                    column[i] = matrix[i, index];
                }
            } else {
                for (int i = 0; i < column.Length; i++) {
                    column[i] = 0;
                }
            }
            return column;
        }

        public double[] GetRow(int index) {
            var row = new double[Width];
            if (index >= 0 && index < Height) {
                for (int i = 0; i < row.Length; i++) {
                    row[i] = matrix[index, i];
                }
            } else {
                for (int i = 0; i < row.Length; i++) {
                    row[i] = 0;
                }
            }
            return row;
        }

        public Matrix Transpose {
            get {
                var transpose = new double[Width, Height];
                for (int i = 0; i < Height; i++) {
                    for (int j = 0; j < Width; j++) {
                        transpose[j, i] = matrix[i, j];
                    }
                }
                return new Matrix(transpose);
            }
        }

        public Matrix Cofactor(int column, int row) {
            if (column >= 0 && column < Width && row >= 0 && row < Height) {
                var cofactor = new Matrix(Height - 1, Width - 1);
                int k = 0;
                for (int i = 0; i < Height; i++) {
                    if (row == i) continue;
                    int l = 0;
                    for (int j = 0; j < Width; j++) {
                        if (column == j) continue;
                        cofactor[k, l] = matrix[i, j];
                        l++;
                    }
                    k++;
                }
                return cofactor;
            } else {
                throw new NotImplementedException("行列の範囲外です");
            }
        }

        public static Matrix Zero(int height, int width) {
            var zero = new Matrix(height, width);
            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    zero[i, j] = 0;
                }
            }
            return zero;
        }

        public static Matrix Unit(int size) {
            var unit = new Matrix(size, size);
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    unit[i, j] = (i == j) ? 1 : 0;
                }
            }
            return unit;
        }

        public double Determinant {
            get {
                if (Width != Height) {
                    throw new NotImplementedException("正方行列以外の行列式は定義されていません");
                } else {
                    if (Width == 1) {
                        return matrix[0, 0];
                    } else if (Width == 2) {
                        return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
                    } else if (Width == 3) {
                        return matrix[0, 0] * matrix[1, 1] * matrix[2, 2]
                               + matrix[1, 0] * matrix[2, 1] * matrix[0, 2]
                               + matrix[2, 0] * matrix[0, 1] * matrix[1, 2]
                               - matrix[2, 0] * matrix[1, 1] * matrix[0, 2]
                               - matrix[1, 0] * matrix[0, 1] * matrix[2, 2]
                               - matrix[0, 0] * matrix[2, 1] * matrix[1, 2];
                    } else {
                        if (!decomposited) {
                            LUDecomposition();
                        }
                        double det = 1;
                        for (int i = 0; i < Width; i++) {
                            det *= u[i, i];
                        }
                        return det;
                    }
                }
            }
        }

        public Matrix Inverse {
            get {
                if (!decomposited) {
                    LUDecomposition();
                }
                var l_inv = new double[Height, Width];
                var u_inv = new double[Height, Width];
                for (int i = 0; i < Height; i++) {
                    for (int j = 0; j < Width; j++) {
                        l_inv[i, j] = 0;
                        u_inv[i, j] = 0;
                    }
                }
                for (int i = 0; i < Height; i++) {
                    for (int j = 0; j <= i; j++) {
                        if (i == j) {
                            l_inv[i, j] = 1;
                        }
                        for (int k = i + 1; k < Height; k++) {
                            l_inv[k, j] -= l_inv[i, j] * l[k, i];
                        }
                    }
                }
                for (int i = Height - 1; i >= 0; i--) {
                    for (int j = 0; j < Width; j++) {
                        u_inv[i, j] += l_inv[i, j];
                        for (int k = Height - 1; k > i; k--) {
                            u_inv[i, j] -= u_inv[k, j] * u[i, k];
                        }
                        u_inv[i, j] /= u[i, i];
                    }
                }

                return new Matrix(u_inv);
            }
        }

        /// <summary>
        /// 対角成分に0がくると死ぬ
        /// </summary>
        private void LUDecomposition() {
            if (Width != Height) {
                throw new NotImplementedException("正方行列以外をLU分解できません");
            } else {
                u = matrix.Clone() as double[,];
                l = new double[Height, Width];
                for (int i = 0; i < Width; i++) {
                    l[i, i] = 1;
                    for (int j = i + 1; j < Width; j++) {
                        l[i, j] = 0;
                        double buf = u[j, i] / u[i, i];
                        l[j, i] = buf;
                        for (int k = 0; k < Width; k++) {
                            u[j, k] -= u[i, k] * buf;
                        }
                    }
                }
            }
            decomposited = true;
        }

        public Matrix Reshape(int height, int width) {
            if (height * width != Height * Width) {
                throw new NotImplementedException("総サイズを変更することはできません");
            } else {
                var result = new double[height, width];
                for (int i = 0; i < Height; i++) {
                    for (int j = 0; j < Width; j++) {
                        int k = (i * Width + j) / width,
                            l = (i * Width + j) % width;
                        result[k, l] = matrix[i, j];
                    }
                }
                return new Matrix(result);
            }
        }

        public static Matrix operator +(Matrix m1, Matrix m2) {
            if (m1.Height != m2.Height || m1.Width != m2.Width) throw new NotImplementedException("加算は同じサイズの行列同士で行ってください");
            var result = new Matrix(m1.Height, m1.Width);
            for (int i = 0; i < result.Height; i++) {
                for (int j = 0; j < result.Width; j++) {
                    result[i, j] = m1[i, j] + m2[i, j];
                }
            }
            return result;
        }

        public static Matrix operator -(Matrix m1, Matrix m2) {
            if (m1.Height != m2.Height || m1.Width != m2.Width) throw new NotImplementedException("減算は同じサイズの行列同士で行ってください");
            var result = new Matrix(m1.Height, m1.Width);
            for (int i = 0; i < result.Height; i++) {
                for (int j = 0; j < result.Width; j++) {
                    result[i, j] = m1[i, j] - m2[i, j];
                }
            }
            return result;
        }

        public static Matrix operator *(Matrix m1, Matrix m2) {
            if (m1.Width != m2.Height) throw new NotImplementedException("m1の幅とm2の高さが等しくないです");
            var result = Matrix.Zero(m1.Height, m2.Width);
            for (int i = 0; i < result.Height; i++) {
                for (int k = 0; k < m1.Width; k++) {
                    for (int j = 0; j < result.Width; j++) {
                        result[i, j] += m1[i, k] * m2[k, j];
                    }
                }
            }
            return result;
        }

        public static Matrix operator *(Matrix m, double d) {
            var result = new Matrix(m.Height, m.Width);
            for (int i = 0; i < result.Height; i++) {
                for (int j = 0; j < result.Width; j++) {
                    result[i, j] = m[i, j] * d;
                }
            }
            return result;
        }

        public static Matrix operator *(double d, Matrix m) {
            return m * d;
        }
    }
}
