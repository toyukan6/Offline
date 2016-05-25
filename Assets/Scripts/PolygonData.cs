using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityLib;

namespace EnvironmentMaker {
    class PolygonData {
        public Vector3[][] Positions { get; private set; }
        public Color[][] Colors { get; private set; }
        public List<Point> Complete { get; private set; }
        public List<Point> Merge { get; private set; }
        public Voxel<List<Point>> Voxel { get; private set; }
        public Voxel<List<Point>> AnotherVoxel { get; private set; }
        const int SECTIONNUMBER = 64;
        public Dictionary<JointType, Vector3> Offsets { get; private set; }
        public Dictionary<JointType, Vector3> PartsCorrection { get; private set; }
        public List<Point>[] Points;
        public Vector3 Estimate { get; private set; }
        public string Name { get; private set; }
        public Dictionary<JointType, Vector3> BodyList { get; private set; }
        Vector3? characterValue = null;

        public PolygonData(string name, List<Point>[] points, bool simple = false) {
            Name = name;
            this.Points = points;
            Positions = new Vector3[points.Length][];
            Colors = new Color[points.Length][];
            Complete = new List<Point>();
            Merge = new List<Point>();
            for (int i = 0; i < points.Length; i++) {
                Positions[i] = points[i].Select(p => p.GetVector3()).ToArray();
                Colors[i] = points[i].Select(p => p.GetColor()).ToArray();
                Complete = Complete.Concat(points[i]).ToList();
                Merge = Merge.Concat(ReducePoints(points[i])).ToList();
            }
            Offsets = new Dictionary<JointType, Vector3>();
            PartsCorrection = new Dictionary<JointType, Vector3>();
            foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                Offsets[type] = Vector3.zero;
                PartsCorrection[type] = Vector3.zero;
            }
            if (!simple) {
                PointToVoxel(Complete);
            }
        }

        public void SetBodyDump(Dictionary<JointType, Vector3> dump) {
            BodyList = dump;
        }

        public List<Point> SetFirstEstimate() {
            var sborder = BorderPoints(Complete);
            ////yield return 0;
            var ymed = sborder.Min(s => Math.Abs(s.Average(v => v.Y)));
            var standard = sborder.Find(s => Math.Abs(s.Average(v => v.Y)) == ymed);
            Estimate = Functions.AverageVector(standard.Select(s => s.GetVector3()).ToList());
            return standard;
        }

        public static List<List<Point>> BorderPoints(List<Point> points) {
            var dictionary = new List<KeyValuePair<double, List<Point>>>();
            var border = new List<List<Point>>();
            var tmp = points.ToList();
            tmp.Sort((t1, t2) => Math.Sign(t1.Y - t2.Y));
            while (tmp.Count > 0) {
                var v = tmp[0];
                var near = tmp.TakeWhile(t => Math.Abs(t.Y - v.Y) < 20).ToList();
                foreach (var n in near) {
                    tmp.Remove(n);
                }
                //near.Sort((n1, n2) => Math.Sign(n1.GetVector3().sqrMagnitude - n2.GetVector3().sqrMagnitude));
                if (near.Count > 1) {
                    var lengthes = new List<double>();
                    var tmp2 = new Point[near.Count];
                    near.CopyTo(tmp2);
                    var max = Math.Sqrt(near.Count);
                    for (int i = 0; i < max && near.Count > 0; i++) {
                        var first = near.First();
                        var last = near.Last();
                        near.Remove(first);
                        near.Remove(last);
                        lengthes.Add((first.GetColor() - last.GetColor()).SqrLength());
                    }
                    var median = lengthes.Median();
                    dictionary.Add(new KeyValuePair<double, List<Point>>(median, tmp2.ToList()));
                }
            }
            for (int i = 0; i < dictionary.Count; i++) {
                var nears = dictionary[i].Value;
                var distance = dictionary.Min(d => {
                    if (d.Value == nears) {
                        return double.MaxValue;
                    } else {
                        var col1 = Functions.AverageColor(d.Value.Select(s => s.GetColor()).ToList());
                        var col2 = Functions.AverageColor(nears.Select(s => s.GetColor()).ToList());
                        return (col1 - col2).SqrLength();
                    }
                });
                if (distance > 0.001) {
                    border.Add(dictionary[i].Value);
                }
            }
            return border;
        }

        public static double CalcY(List<List<Point>> source, List<List<Point>> destination) {
            var diffs = new List<double>();
            Func<List<Point>, List<Point>, double> f = (p1, p2) =>
                (Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()) - Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
                 + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;
            for (int i = 0; i < source.Count; i++) {
                var spoints = source[i];
                var index = destination.IndexOfMin(b => f(b, spoints));
                var dpoints = destination[index];
                var diff = Functions.AverageVector(dpoints.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(spoints.Select(s => s.GetVector3()).ToList());
                diffs.Add(diff.y);
            }

            return diffs.Median();
        }

        public void EstimateHip(List<Point> standard) {
            var dborder = BorderPoints(Complete);
            if (dborder.Count > 0) {
                Func<List<Point>, List<Point>, double> f = (p1, p2) =>
    Functions.SubColor(Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()), Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
     + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;

                var min = dborder.Min(de => f(standard, de));
                var target = dborder.Find(d => Math.Abs(f(standard, d) - min) < 0.0000001);
                var vecs = target.Select(t => t.GetVector3()).ToList();
                var vec = Functions.AverageVector(vecs);
                if (Math.Abs(vec.y) < 0.1) {
                    Estimate = vec;
                } else {
                    Estimate = Functions.AverageVector(Complete.Select(s => s.GetVector3()).ToList());
                }
            } else {
                Estimate = Vector3.zero;
            }
        }

        public Vector3 PartsPosition(JointType type) {
            if (BodyList.ContainsKey(type)) {
                var diff = BodyList[type] - BodyList[JointType.SpineBase];
                var basePos = Estimate + diff + Offsets[type] + PartsCorrection[type];
                return basePos;
            } else {
                BodyList[type] = Vector3.zero;
                return Vector3.zero;
            }
        }

        public Vector3 GetCharacterValue() {
            if (!characterValue.HasValue) {
                Func<JointType, Vector3> func = (type) => BodyList[type] + Offsets[type] + PartsCorrection[type];
                characterValue = func(JointType.WristRight) - func(JointType.WristLeft);
            }
            return characterValue.Value;
        }

        public double[] GetVoxelHistgram(Vector3 index) {
            return GetVoxelHistgram((int)index.x, (int)index.y, (int)index.z);
        }

        public double[] GetVoxelHistgram(int i, int j, int k) {
            List<Vector3> vectors = Voxel[i, j, k].Select(v => v.GetVector3()).ToList();
            return Histogram(BetweenMagnitudes(vectors));
        }

        public double[] GetAnotherVoxelHistgram(Vector3 index) {
            return GetAnotherVoxelHistgram((int)index.x, (int)index.y, (int)index.z);
        }

        public double[] GetAnotherVoxelHistgram(int i, int j, int k) {
            List<Vector3> vectors = AnotherVoxel[i, j, k].Select(v => v.GetVector3()).ToList();
            return Histogram(BetweenMagnitudes(vectors));
        }

        public double[] GetColorHistgram(Vector3 index) {
            return GetColorHistgram((int)index.x, (int)index.y, (int)index.z);
        }

        public double[] GetColorHistgram(int i, int j, int k) {
            List<Vector3> colors = Voxel[i, j, k].Select(v => v.GetColor().ToVector3()).ToList();
            return Histogram(BetweenMagnitudes(colors));
        }

        public double[] GetAnotherColorHistgram(Vector3 index) {
            return GetAnotherColorHistgram((int)index.x, (int)index.y, (int)index.z);
        }

        public double[] GetAnotherColorHistgram(int i, int j, int k) {
            List<Vector3> colors = AnotherVoxel[i, j, k].Select(v => v.GetColor().ToVector3()).ToList();
            return Histogram(BetweenMagnitudes(colors));
        }

        void PointToVoxel(List<Point> basedata) {
            double maxX = Math.Ceiling(basedata.Max(p => p.GetVector3().x));
            double minX = Math.Floor(basedata.Min(p => p.GetVector3().x));
            double maxY = Math.Ceiling(basedata.Max(p => p.GetVector3().y));
            double minY = Math.Floor(basedata.Min(p => p.GetVector3().y));
            double maxZ = Math.Ceiling(basedata.Max(p => p.GetVector3().z));
            double minZ = Math.Floor(basedata.Min(p => p.GetVector3().z));
            //1 = 1メートル
            double delta = 0.05;
            int inverse = (int)(1 / delta);
            int width = (int)((maxX - minX) * inverse);
            int height = (int)((maxY - minY) * inverse);
            int depth = (int)((maxZ - minZ) * inverse);
            Voxel = new Voxel<List<Point>>(width, height, depth, minX, minY, minZ, delta);
            AnotherVoxel = new Voxel<List<Point>>(width, height, depth, minX - delta * 0.5, minY - delta * 0.5, minZ - delta * 0.5, delta);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    for (int k = 0; k < depth; k++) {
                        Voxel[i, j, k] = new List<Point>();
                        AnotherVoxel[i, j, k] = new List<Point>();
                    }
                }
            }
            foreach (var d in basedata) {
                var indexVec = Voxel.GetIndexFromPosition(d.GetVector3());
                if (indexVec.x == Voxel.Width) indexVec.x -= 1;
                if (indexVec.y == Voxel.Height) indexVec.y -= 1;
                if (indexVec.z == Voxel.Depth) indexVec.z -= 1;
                Voxel[(int)indexVec.x, (int)indexVec.y, (int)indexVec.z].Add(d);
                var aindex = AnotherVoxel.GetIndexFromPosition(d.GetVector3());
                if (aindex.x == AnotherVoxel.Width) aindex.x -= 1;
                if (aindex.y == AnotherVoxel.Height) aindex.y -= 1;
                if (aindex.z == AnotherVoxel.Depth) aindex.z -= 1;
                AnotherVoxel[(int)indexVec.x, (int)indexVec.y, (int)indexVec.z].Add(d);
            }
        }

        List<Point> ReducePoints(List<Point> points) {
            var result = new List<Point>();
            var tmp = new List<Point>();
            int max = points.Count / 10;
            var rand = new System.Random();
            tmp = points.ToList();
            for (int i = 0; i < max; i++) {
                var point = tmp[rand.Next(tmp.Count)];
                result.Add(point);
                tmp.Remove(point);
            }

            return result;
        }

        public static double[] Histogram(List<double> numericGroup) {
            if (numericGroup.Count < 2) {
                return null;
            } else if (numericGroup.All(n => n == numericGroup[0])) {
                double[] result = new double[SECTIONNUMBER];
                for (int i = 0; i < SECTIONNUMBER; i++) {
                    result[i] = 0;
                }
                result[SECTIONNUMBER / 2] = 1;
                return result;
            } else {
                double average = numericGroup.Average();
                double min = numericGroup.Min();
                double max = numericGroup.Max();
                int[] counts = new int[SECTIONNUMBER];
                for (int i = 0; i < SECTIONNUMBER; i++) {
                    counts[i] = 0;
                }
                double lowDelta = (average - min) / SECTIONNUMBER * 2;
                double highDelta = (max - average) / SECTIONNUMBER * 2;
                foreach (var n in numericGroup) {
                    double index;
                    if (n < average) {
                        index = n - min;
                        index = index / lowDelta;
                    } else {
                        index = n - average;
                        index = index / highDelta;
                        index += SECTIONNUMBER / 2;
                        if (index == SECTIONNUMBER) {
                            index -= 1;
                        }
                    }
                    counts[(int)index]++;
                }
                double sum = counts.Sum();
                double[] histogram = new double[SECTIONNUMBER];
                for (int i = 0; i < SECTIONNUMBER; i++) {
                    histogram[i] = counts[i] / sum;
                }
                return histogram;
            }
        }

        public static List<double> Magnitudes(List<Vector3> vectors) {
            var numericGroup = new List<double>();
            vectors.ForEach(v => numericGroup.Add(v.magnitude));
            return numericGroup;
        }

        public static List<double> BetweenMagnitudes(List<Vector3> vectors) {
            var numericGroup = new List<double>();
            for (int i = 0; i < vectors.Count; i++) {
                for (int j = i + 1; j < vectors.Count; j++) {
                    numericGroup.Add((vectors[i] - vectors[j]).magnitude);
                }
            }
            return numericGroup;
        }

        public Vector3 SearchHistgram(double[] histgram, Vector3 index) {
            return SearchHistgramFunction(histgram, GetVoxelHistgram, GetAnotherVoxelHistgram, index);
        }

        public Vector3 SearchColorHistgram(double[] histgram, Vector3 index) {
            return SearchHistgramFunction(histgram, GetColorHistgram, GetAnotherColorHistgram, index);
        }

        Vector3 SearchHistgramFunction(double[] histgram, Func<int, int, int, double[]> targetHist, Func<int, int, int, double[]> targetAHist, Vector3 index) {
            Vector3 result = Vector3.zero, anotherResult = Vector3.zero;
            double minLength = double.MaxValue, anotherminLength = double.MaxValue;
            int depth = 2;
            int minX = (int)Math.Max(index.x - depth, 0);
            int maxX = (int)Math.Min(index.x + depth, Voxel.Width);
            int minY = (int)Math.Max(index.y - depth, 0);
            int maxY = (int)Math.Min(index.y + depth, Voxel.Height);
            int minZ = (int)Math.Max(index.z - depth, 0);
            int maxZ = (int)Math.Min(index.z + depth, Voxel.Depth);
            for (int i = minX; i < maxX; i++) {
                for (int j = minY; j < maxY; j++) {
                    for (int k = minZ; k < maxZ; k++) {
                        double length = 0, alength = 0;
                        double[] target = targetHist(i, j, k);
                        double[] atarget = targetAHist(i, j, k);
                        if (target != null) {
                            for (int l = 0; l < histgram.Length; l++) {
                                length += Math.Pow(target[l] - histgram[l], 2);
                            }
                            length *= ((new Vector3(i, j, k) - index).magnitude + 1);
                            if (length < minLength) {
                                minLength = length;
                                result = new Vector3(i, j, k);
                            }
                        }
                        if (atarget != null) {
                            for (int l = 0; l < histgram.Length; l++) {
                                alength += Math.Pow(atarget[l] - histgram[l], 2);
                            }
                            alength *= ((new Vector3(i, j, k) - index).magnitude + 1);
                            if (alength < anotherminLength) {
                                anotherminLength = alength;
                                anotherResult = new Vector3(i, j, k);
                            }
                        }
                    }
                }
            }
            return ((index - result).magnitude < (index - anotherResult).magnitude ? result : anotherResult);
        }

        public void Save(BinaryWriter bwriter) {
            bwriter.Write(Offsets.Count);
            foreach (var o in Offsets) {
                bwriter.Write((int)o.Key);
                bwriter.Write(o.Value.x);
                bwriter.Write(o.Value.y);
                bwriter.Write(o.Value.z);
            }
            bwriter.Write(PartsCorrection.Count);
            foreach (var pc in PartsCorrection) {
                bwriter.Write((int)pc.Key);
                bwriter.Write(pc.Value.x);
                bwriter.Write(pc.Value.y);
                bwriter.Write(pc.Value.z);
            }
        }

        public void Load(BinaryReader breader) {
            int offsetsCount = breader.ReadInt32();
            for (int i = 0; i < offsetsCount; i++) {
                JointType type = (JointType)breader.ReadInt32();
                float x = breader.ReadSingle();
                float y = breader.ReadSingle();
                float z = breader.ReadSingle();
                Offsets[type] = new Vector3(x, y, z);
            }
            int partsCount = breader.ReadInt32();
            for (int i = 0; i < partsCount; i++) {
                JointType type = (JointType)breader.ReadInt32();
                float x = breader.ReadSingle();
                float y = breader.ReadSingle();
                float z = breader.ReadSingle();
                PartsCorrection[type] = new Vector3(x, y, z);
            }
        }
    }
}
