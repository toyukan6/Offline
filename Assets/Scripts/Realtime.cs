using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityLib;

namespace EnvironmentMaker {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    class Realtime : MonoBehaviour {
        PlyReader reader;
        List<Vector3[][]> walkPoints;
        List<Color[][]> walkColors;
        List<Point[]> mergePoints;
        List<Vector3> bodyposes;
        int kinectNums = 4;
        public GameObject Pointer;
        List<GameObject> pointers = new List<GameObject>();
        Vector3[] estimates;
        Dictionary<JointType, Vector3>[] partsCorrections;
        List<Dictionary<JointType, Vector3>> bodyList;
        bool loadEnd = false;
        bool looped = false;
        List<Vector2> route = new List<Vector2>();
        int nextRouteIndex = 0;
        float length = 10f;
        Vector3 firstPosition;
        List<Point> standard;

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        public string DirName = "newpolygons";

        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            walkPoints = new List<Vector3[][]>();
            walkColors = new List<Color[][]>();
            fileTimes = new List<MyTime[]>();
            beforeTime = new int[kinectNums];
            pointsNumbers = new int[kinectNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            GetComponent<MeshFilter>().mesh = mesh;
            StartCoroutine(LoadModels(DirName));
            LoadIndexCSV(DirName);
            foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                var obj = Instantiate(Pointer) as GameObject;
                obj.name = Enum.GetName(typeof(JointType), type);
                pointers.Add(obj);
            }
        }

        double beforeMag = double.MaxValue;
        void Update() { }

        void FixedUpdate() {
            if (loadEnd) {
                var oldMesh = mesh;
                mesh = new Mesh();
                GetComponent<MeshFilter>().mesh = mesh;
                DestroyImmediate(oldMesh);

                var points = new List<Vector3>();
                var colors = new List<Color>();
                var time = Time.deltaTime * 1000;
                for (int i = 0; i < kinectNums; i++) {
                    beforeTime[i] += (int)Math.Floor(time);
                    int index = pointsNumbers[i];
                    var timeDiff = fileTimes[(index + 1) % fileTimes.Count][i].GetMilli() - fileTimes[index][i].GetMilli();
                    if (beforeTime[i] > timeDiff) {
                        var before = pointsNumbers[i];
                        pointsNumbers[i] = (pointsNumbers[i] + 1) % walkPoints.Count;
                        if (before > pointsNumbers[i]) {
                            looped = true;
                        }
                    }
                    foreach (var v in walkPoints[pointsNumbers[i]][i]) {
                        points.Add(v);
                    }
                    foreach (var c in walkColors[pointsNumbers[i]][i]) {
                        colors.Add(c);
                    }
                }
                if (looped) {
                    for (int i = 0; i < kinectNums; i++) {
                        pointsNumbers[i] = 0;
                        beforeTime[i] = 0;
                    }
                    var start = mergePoints.First();
                    var last = mergePoints.Last();
                    var startAverage = Functions.AverageVector(start.Select(s => s.GetVector3()).ToList());
                    var lastAverage = Functions.AverageVector(last.Select(s => s.GetVector3()).ToList());
                    var diff = lastAverage - startAverage;
                    var theta = transform.localEulerAngles.y * Math.PI / 180;
                    this.transform.position += diff.RotateXZ(-theta);
                    looped = false;
                }
                var positions = new Vector3[pointers.Count];
                var thisPos = this.transform.position;
                var pn = pointsNumbers[0];
                EstimateHip(standard, mergePoints[pn].ToList(), pn);
                foreach (JointType type in Enum.GetValues(typeof(JointType))) { 
                    var basePos = estimates[pn % estimates.Length];
                    positions[(int)type] = basePos;
                }
                for (int i = 0; i < positions.Length; i++) {
                    pointers[i].transform.position = thisPos + positions[i];
                }
                mesh.vertices = points.ToArray();
                mesh.colors = colors.ToArray();
                mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
            }
        }

        private void GotoFirst() {
            this.transform.position = firstPosition;
        }

        IEnumerator<int> LoadModels(string dir) {
            string baseDir = @"polygons\" + dir;
            int num = 0;
            while (File.Exists(baseDir + @"\model_" + num + "_0.ply")) {
                num++;
            }
            var tmpPoints = new Vector3[num][][];
            var tmpColors = new Color[num][][];
            var points = new Point[num][];
            estimates = new Vector3[num];
            partsCorrections = new Dictionary<JointType, Vector3>[num];
            var completeMerge = new Point[num][];
            for (int n = 0; n < num; n++) {
                var pointlist = new List<Point>[kinectNums];
                var list = new List<Point>();
                for (int i = 0; i < kinectNums; i++) {
                    var plist = new List<Point>();
                    var fileName = baseDir + @"\model_" + n + "_" + i + ".ply";
                    foreach (var p in reader.Load(fileName)) {
                        plist.Add(p);
                    }
                    //yield return n;
                    if (i > 0) {
                        var source = new List<Point>();
                        for (int j = 0; j < i; j++) {
                            pointlist[j].ForEach(p => source.Add(p));
                        }
                        var sourceBorder = BorderPoints(source);
                        var destBorder = BorderPoints(plist);
                        //yield return n;
                        //var sourceLine = CalcLine(SelectPoint(plist, source));
                        //var destLine = CalcLine(SelectPoint(source, plist));
                        float diffY = (float)CalcY(sourceBorder, destBorder);
                        //yield return n;
                        //var diffXZ = CalcXZ(sourceLine, destLine);
                        if (diffY < 0.2) {
                            plist = plist.Select(p => p - new Vector3(0, diffY, 0)).ToList();
                        }
                    }
                    pointlist[i] = plist;
                    foreach (var p in plist) {
                        list.Add(p);
                    }
                    //yield return n;
                }
                //ApplyXZ(pointlist);
                tmpPoints[n] = pointlist.Select(v => v.Select(p => p.GetVector3()).ToArray()).ToArray();
                tmpColors[n] = pointlist.Select(c => c.Select(p => p.GetColor()).ToArray()).ToArray();
                completeMerge[n] = list.ToArray();
                //yield return n;
                int max = (int)Math.Sqrt(list.Count) / 2;
                var tmpList = new List<Point>();
                for (int i = 0; i < max; i++) {
                    var point = list[Functions.GetRandomInt(list.Count)];
                    tmpList.Add(point);
                    list.Remove(point);
                }
                points[n] = tmpList.ToArray();
                //yield return n;
            }
            walkPoints = tmpPoints.ToList();
            walkColors = tmpColors.ToList();
            mergePoints = points.ToList();
            var sborder = BorderPoints(completeMerge[0].ToList());
            ////yield return 0;
            standard = sborder[sborder.IndexOfMin(s => Math.Abs(s.Average(v => v.Y)))];
            estimates[0] = Functions.AverageVector(standard.Select(s => s.GetVector3()).ToList());
            ////yield return 0;
            //InitPartsCorrection();
            var diff = Functions.AverageVector(completeMerge[0].Select(p => p.GetVector3()).ToList());
            this.transform.position -= diff;
            firstPosition = this.transform.position;
            loadEnd = true;
            yield return 0;
        }

        void LoadIndexCSV(string dir) {
            List<string[]> data = new List<string[]>();
            using (StreamReader reader = new StreamReader(@"polygons\" + dir + @"\index.csv")) {
                string str = reader.ReadLine();
                while (str != null) {
                    var split = str.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 0)
                        data.Add(split);
                    str = reader.ReadLine();
                }
            }
            for (int i = 0; i < data.Count; i += kinectNums) {
                var times = new MyTime[kinectNums];
                for (int j = 0; j < kinectNums; j++) {
                    times[j] = ParseTime(data[i][1]);
                }
                fileTimes.Add(times);
            }
        }

        void EstimateHip(List<Point> standard, List<Point> dest, int index) {
            var dborder = BorderPoints(dest);
            if (dborder.Count > 0) {
                Func<List<Point>, List<Point>, double> f = (p1, p2) =>
    Functions.SubColor(Functions.AverageColor(p1.Select(s => s.GetColor()).ToList()), Functions.AverageColor(p2.Select(s => s.GetColor()).ToList())).SqrLength()
     + (Functions.AverageVector(p1.Select(s => s.GetVector3()).ToList()) - Functions.AverageVector(p2.Select(s => s.GetVector3()).ToList())).sqrMagnitude;

                var min = dborder.Min(de => f(standard, de));
                var target = dborder.Find(d => Math.Abs(f(standard, d) - min) < 0.0000001);
                var vecs = target.Select(t => t.GetVector3()).ToList();
                var vec = Functions.AverageVector(vecs);
                if (Math.Abs(vec.y) < 0.1) {
                    estimates[index] = vec;
                } else {
                    estimates[index] = Functions.AverageVector(dest.Select(s => s.GetVector3()).ToList());
                }
            } else {
                estimates[index] = Vector3.zero;
            }
        }

        void InitPartsCorrection() {
            for (int i = 0; i < partsCorrections.Length; i++) {
                partsCorrections[i] = new Dictionary<JointType, Vector3>();
            }
        }

        void CalcPartsCorrection(List<Point> points, int index) {
            Arms(points, index);
        }

        MyTime ParseTime(string str) {
            var split = str.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            int hour, minute, second, millisecond;
            if (!int.TryParse(split[0], out hour)) return null;
            if (!int.TryParse(split[1], out minute)) return null;
            if (!int.TryParse(split[2], out second)) return null;
            if (!int.TryParse(split[3], out millisecond)) return null;
            return new MyTime(hour, minute, second, millisecond);
        }

        void LoadBody(string dir) {
            using (StreamReader reader = new StreamReader(@"polygons\" + dir + @"\bodyposes.txt")) {
                string str = reader.ReadLine();
                while (str != null) {
                    var split = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    int x = int.Parse(split[1]);
                    int y = int.Parse(split[2]);
                    int z = int.Parse(split[3]);
                    var point = new Point(x, y, z, 0, 0, 0);
                    bodyposes.Add(point.GetVector3() / 1000);
                    str = reader.ReadLine();
                }
            }
        }

        List<List<Point>> BorderPoints(List<Point> points) {
            var dictionary = new List<KeyValuePair<double, List<Point>>>();
            var border = new List<List<Point>>();
            var tmp = new List<Point>();
            foreach (var v in points) {
                tmp.Add(v);
            }
            tmp.Sort((t1, t2) => Math.Sign(t1.Y - t2.Y));
            while (tmp.Count > 0) {
                var v = tmp[0];
                var near = tmp.TakeWhile(t => Math.Abs(t.Y - v.Y) < 20).ToList();
                foreach (var n in near) {
                    tmp.Remove(n);
                }
                near.Sort((n1, n2) => Math.Sign(n1.GetVector3().sqrMagnitude - n2.GetVector3().sqrMagnitude));
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
                        return Functions.SubColor(col1, col2).SqrLength();
                    }
                });
                if (distance > 0.001) {
                    border.Add(dictionary[i].Value);
                }
            }
            return border;
        }

        List<Point> SelectPoint(List<Point> source, List<Point> dest) {
            var select = new List<Point>();
            var minX = source.Min(s => s.X);
            var maxX = source.Max(s => s.X);
            var minZ = source.Min(s => s.Z);
            var maxZ = source.Max(s => s.Z);
            foreach (var d in dest) {
                if (d.X >= minX && d.X <= maxX && d.Z >= minZ && d.Z <= maxZ) {
                    select.Add(d);
                }
            }
            return select;
        }

        void ApplyXZ(List<Point>[] points) {
            var correct = new[] { UpDown.Down, UpDown.Up, UpDown.Down, UpDown.Up };
            var bayes = new Bayes();
            var basepoints = new List<Point>();
            points[0].ForEach(p => basepoints.Add(p));
            points[3].ForEach(p => basepoints.Add(p));
            var averageVec = Functions.AverageVector(basepoints.Select(p => p.GetVector3()).ToList());
            for (int i = 0; i < points.Length; i++) {
                var border = BorderPoints(points[i]);
                var minIndex = border.IndexOfMin(s => Math.Abs(s.Average(v => v.Y)));
                var min = border[minIndex];
                var vec3s = min.Select(p => p.GetVector3());
                List<Vector2> bayesResult;
                if (i > 1) {
                    bayesResult = bayes.BayesEstimate(vec3s.Select(v => new Vector2(v.z, v.x)).ToList());
                } else {
                    bayesResult = bayes.BayesEstimate(vec3s.Select(v => new Vector2(v.x, v.z)).ToList());
                }
                var ud = IsUpDown(bayesResult);
                if (ud != correct[i]) {
                    var average = Functions.AverageVector(points[i].Select(p => p.GetVector3()).ToList());
                    var diff = averageVec;
                    diff = new Vector3(diff.x, 0, diff.z);
                    for (int j = 0; j < points[i].Count; j++) {
                        var moved = points[i][j].GetVector3();
                        moved -= diff;
                        moved = moved.RotateXZ(Math.PI);
                        moved += diff;
                        var color = points[i][j].GetColor();
                        points[i][j] = new Point(moved, color);
                    }
                }
                //if (i == 1 || i == 2) {
                //    var vec2s = vec3s.Select(v => new Vector2(v.x, v.z)).ToList();
                //    List<Vector2> baseVecs;
                //    int vecIndex, index;
                //    Vector2 vec, diff, baseVec;
                //    if (i == 1) {
                //        var baseBorder = BorderPoints(points[3]);
                //        var baseMinIndex = baseBorder.IndexOfMin(s => Math.Abs(s.Average(v => v.Y)));
                //        var baseMin = baseBorder[baseMinIndex];
                //        baseVecs = baseMin.Select(p => p.GetVector3()).Select(v => new Vector2(v.x, v.z)).ToList();
                //        index = baseVecs.IndexOfMax(b => b.y);
                //        vecIndex = vec2s.IndexOfMax(b => b.y);
                //    } else {
                //        var baseBorder = BorderPoints(points[0]);
                //        var baseMinIndex = baseBorder.IndexOfMin(s => Math.Abs(s.Average(v => v.Y)));
                //        var baseMin = baseBorder[baseMinIndex];
                //        baseVecs = baseMin.Select(p => p.GetVector3()).Select(v => new Vector2(v.x, v.z)).ToList();
                //        index = baseVecs.IndexOfMin(b => b.y);
                //        vecIndex = vec2s.IndexOfMin(b => b.y);
                //    }
                //    baseVec = baseVecs[index];
                //    vec = vec2s[vecIndex];
                //    diff = vec - baseVec;
                //    for (int j = 0; j < points[i].Count; j++) {
                //        points[i][j] -= new Vector3(diff.x, 0, diff.y);
                //    }
                //}
            }
        }

        UpDown IsUpDown(List<Vector2> result) {
            var first = result.First();
            var last = result.Last();
            var average = (first + last) / 2;
            var bayesY = result[result.Count / 2].y;
            if (average.y < bayesY) {
                return UpDown.Up;
            } else if (average.y > bayesY) {
                return UpDown.Down;
            } else {
                return UpDown.Equal;
            }
        }

        double CalcY(List<List<Point>> source, List<List<Point>> destination) {
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

        Vector3 ApplyPointerPos(List<Point> points, Vector3 position, JointType joint) {
            string jointName = Enum.GetName(typeof(JointType), joint);
            var list = points.FindAll(l => Math.Abs(position.y - l.GetVector3().y) < 0.8);
            var medianXZ = list.Median(l => l.X + l.Z);
            double number = 0.8;
            var beforePoint = position;
            if (jointName.Contains("Right")) {
                switch (joint) {
                    case JointType.ShoulderRight:
                    case JointType.ElbowRight:
                    case JointType.WristRight:
                    case JointType.HandTipRight:
                    case JointType.ThumbRight:
                    case JointType.HandRight: number = 0.1; break;
                    case JointType.HipRight:
                    case JointType.KneeRight:
                    case JointType.AnkleRight:
                    case JointType.FootRight:
                    default: break;
                }
                var tmp = list.FindAll(l => l.X + l.Z < medianXZ);
                var point = Functions.AverageVector(tmp.Select(t => t.GetVector3()).ToList());
                while ((point - beforePoint).sqrMagnitude > number * number) {
                    beforePoint = point;
                    tmp = tmp.FindAll(l => l.GetVector3().x <= point.x && l.GetVector3().z <= point.z);
                    if (tmp.Count == 0) break;
                    point = Functions.AverageVector(tmp.Select(t => t.GetVector3()).ToList());
                }
                return new Vector3(point.x, position.y, point.z);
            } else if (jointName.Contains("Left")) {
                switch (joint) {
                    case JointType.ShoulderLeft:
                    case JointType.ElbowLeft:
                    case JointType.WristLeft:
                    case JointType.HandTipLeft:
                    case JointType.ThumbLeft:
                    case JointType.HandLeft: number = 0.1; break;
                    case JointType.HipLeft:
                    case JointType.KneeLeft:
                    case JointType.AnkleLeft:
                    case JointType.FootLeft:
                    default: break;
                }
                var tmp = list.FindAll(l => l.X + l.Z > medianXZ);
                var point = Functions.AverageVector(tmp.Select(t => t.GetVector3()).ToList());
                while ((point - beforePoint).sqrMagnitude > number * number) {
                    beforePoint = point;
                    tmp = tmp.FindAll(l => l.GetVector3().x >= point.x && l.GetVector3().z >= point.z);
                    if (tmp.Count == 0) break;
                    point = Functions.AverageVector(tmp.Select(t => t.GetVector3()).ToList());
                }
                return new Vector3(point.x, position.y, point.z);
            } else {
                return position;
            }
        }

        Vector3 ShoulderPoint(List<Point> points) {
            var average = points.Average(r => r.Y);
            var upper = points.FindAll(r => r.Y > average);
            for (int i = 0; i < 2 && upper.Count > 10; i++) {
                average = upper.Average(r => r.Y);
                upper = upper.FindAll(r => r.Y > average);
            }
            return Functions.AverageVector(upper.Select(s => s.GetVector3()).ToList());
        }

        void Arms(List<Point> points, int index) {
            var rightArm = new List<Point>();
            var leftArm = new List<Point>();
            var line = LeastSquareMethod(points.ToList());
            float minX = points.Min(p => p.GetVector3().x);
            float maxX = points.Max(p => p.GetVector3().x);
            var minVec = new Vector2(minX, line.CalcY(minX));
            var vecs = new List<Point>();
            foreach (var p in points) {
                vecs.Add(p);
            }
            var lists = new List<List<Point>>();
            var delta = 0.04;
            double length = delta;
            while (vecs.Count > 0) {
                var tmp = vecs.FindAll(v => (v.GetXZVector() - minVec).sqrMagnitude < length * length).ToList();
                tmp.ForEach(t => vecs.Remove(t));
                if (tmp.Count > 0) {
                    lists.Add(tmp);
                }
                length += delta;
            }
            bool right = true;
            foreach (var l in lists) {
                var averageY = l.Average(p => p.Y);
                var varianceY = l.Average(p => Math.Pow(p.Y - averageY, 2));
                if (varianceY < 100000) {
                    if (right)
                        l.ForEach(p => rightArm.Add(p));
                    else
                        l.ForEach(p => leftArm.Add(p));
                } else {
                    right = false;
                }
            }

            if (rightArm.Count > 0)
                partsCorrections[index][JointType.ShoulderRight] = ShoulderPoint(rightArm);
            if (leftArm.Count > 0)
                partsCorrections[index][JointType.ShoulderLeft] = ShoulderPoint(leftArm);

            var rightXZ = rightArm.Select(r => new Vector2(r.X, r.Z)).ToList();
            var rightXY = rightArm.Select(r => new Vector2(r.X, r.Y)).ToList();
            var rightXZLine = TwoDLine.LeastSquareMethod(rightXZ);
            var rightXYLine = TwoDLine.LeastSquareMethod(rightXY);
            var direction = Functions.CrossProduct(new Vector3((float)rightXZLine.A, 0, -1), new Vector3((float)rightXYLine.A, -1, 0));
            print(direction);
        }

        TwoDLine LeastSquareMethod(List<Point> points) {
            var data = new List<Vector2>();
            foreach (var p in points) {
                var vec = p.GetVector3();
                data.Add(new Vector2(vec.x, vec.z));
            }
            return TwoDLine.LeastSquareMethod(data);
        }
    }
}
