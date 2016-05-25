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
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PointCloud : MonoBehaviour {

        PlyReader reader;
        List<Vector3> bodyposes;
        int kinectNums = 3;
        public GameObject Pointer;
        List<GameObject> pointers = new List<GameObject>();
        bool looped = false;
        List<Vector2> route = new List<Vector2>();
        int nextRouteIndex = 1;
        float length = 10f;
        Vector3 firstPosition;
        Vector2? start;
        Vector2? end;
        private PolygonData[] polygonData;
        private int frameAmount;
        private bool loadEnd = false;
        private Dictionary<JointType, List<GameObject>> accessories = new Dictionary<JointType, List<GameObject>>();
        private List<string> addMotions = new List<string>();
        //public Dictionary<JointType, List<GameObject>> AllAccessories;
        public GameObject Bag;

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        public string DirName = "newpolygons";
        public GameObject SelectedPrehab;


        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            fileTimes = new List<MyTime[]>();
            GetComponent<MeshFilter>().mesh = mesh;
            SetPolygonData(DirName);
            beforeTime = new int[kinectNums];
            pointsNumbers = new int[kinectNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            frameAmount = polygonData.Length;
            //AddAccessories(JointType.WristRight, Instantiate(Bag));
            //foreach (JointType type in Enum.GetValues(typeof(JointType))) {
            //    var obj = Instantiate(Pointer) as GameObject;
            //    obj.name = Enum.GetName(typeof(JointType), type);
            //    pointers.Add(obj);
            //}
        }

        private void SetPolygonData(string name) {
            var manager = GameObject.FindObjectOfType<PolygonManager>();
            if (manager.Data.ContainsKey(name)) {
                loadEnd = true;
            } else {
                LoadModels(name);
            }
            PolygonManager.Load(name);
            polygonData = manager.Data[name];
            kinectNums = polygonData[0].Positions.Length;
            LoadIndexCSV(name);
            LoadBodyDump(name);
        }

        public void Initialize(List<Vector2> route) {
            this.route = route;
            if (route.Count > 0) {
                this.start = route[0];
            } else this.start = null;
            if (route.Count > 1) {
                this.end = route[1];
            } else this.end = null;
            if (start.HasValue) {
                var value = start.Value;
                this.transform.position = new Vector3(value.x, this.transform.position.y, value.y);
                firstPosition = this.transform.position;
                //Instantiate(SelectedPrehab, this.transform.position, Quaternion.Euler(90, 0, 0));
                if (end.HasValue) {
                    SetTarget(end.Value);
                }
            }
        }

        void SetTarget(Vector2 target) {
            nextRouteIndex = (nextRouteIndex + 1) % route.Count;
            Vector2 thisPos = new Vector2(this.transform.position.x, this.transform.position.z);
            end = target;
            Vector2 diff = target - thisPos;
            double theta = Math.Atan2(-diff.y, diff.x) - Math.PI / 2;
            var angle = this.transform.localEulerAngles;
            this.transform.localEulerAngles = new Vector3(angle.x, (float)(theta * 180 / Math.PI), angle.z);
            //Instantiate(SelectedPrehab, new Vector3(end.Value.x, this.transform.position.y, end.Value.y), Quaternion.Euler(90, 0, 0));
        }

        double beforeMag = double.MaxValue;
        void Update() {
            //var next = route[nextRouteIndex];
            //var nowIndex = pointsNumbers[0];
            //var startAvr = Functions.AverageVector(mergePoints[0].Select(p => p.GetVector3()).ToList());
            //var nowAvr = Functions.AverageVector(mergePoints[nowIndex].Select(p => p.GetVector3()).ToList());
            //var diffAvr = nowAvr - startAvr;
            //var mag = (this.transform.position + diff - new Vector3(next.x, 0, next.y)).sqrMagnitude;
            //if (mag < 10) {
            //    print("changed!");
            //    int before = nextRouteIndex;
            //    nextRouteIndex = (nextRouteIndex + 1) % route.Count;
            //    if (before > nextRouteIndex) {
            //        this.transform.position = firstPosition;
            //        nextRouteIndex = 1;
            //    }
            //    Vector2 target = route[nextRouteIndex];
            //    Vector3 target3 = new Vector3(target.x, this.transform.position.y, target.y);
            //    print(this.transform.position);
            //    this.transform.LookAt(target3);
            //    print(this.transform.position);
            //    this.transform.localEulerAngles -= new Vector3(0, 45, 0);
            //}
            //beforeMag = mag;
        }

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
                    if (index == fileTimes.Count - 1) {
                        timeDiff = 1000;
                    }
                    if (beforeTime[i] > timeDiff) {
                        var before = pointsNumbers[i];
                        pointsNumbers[i] = (pointsNumbers[i] + 1) % frameAmount;
                        if (before > pointsNumbers[i]) {
                            looped = true;
                        }
                    }
                    foreach (var v in polygonData[pointsNumbers[i]].Positions[i]) {
                        points.Add(v);
                    }
                    foreach (var c in polygonData[pointsNumbers[i]].Colors[i]) {
                        colors.Add(c);
                    }
                }
                if (looped) {
                    for (int i = 0; i < kinectNums; i++) {
                        pointsNumbers[i] = 0;
                        beforeTime[i] = 0;
                    }
                    var start = polygonData.First().Merge;
                    var last = polygonData.Last().Merge;
                    var startAverage = Functions.AverageVector(start.Select(s => s.GetVector3()).ToList());
                    var lastAverage = Functions.AverageVector(last.Select(s => s.GetVector3()).ToList());
                    var diff = lastAverage - startAverage;
                    diff.y = 0;
                    var theta = transform.localEulerAngles.y * Math.PI / 180;
                    this.transform.position += diff.RotateXZ(-theta);
                    if (end.HasValue) {
                        Vector2 value = end.Value;
                        Vector3 target = new Vector3(value.x, this.transform.position.y, value.y);
                        float sqrMagnitude = (target - this.transform.position).sqrMagnitude;
                        if (sqrMagnitude > beforeMag) {
                            if (nextRouteIndex == 0) {
                                GotoFirst();
                            } else {
                                var next = route[nextRouteIndex];
                                SetTarget(next);
                                beforeMag = double.MaxValue;
                            }
                        } else {
                            beforeMag = sqrMagnitude;
                        }
                    } else {
                        float sqrMagnitude = (firstPosition - this.transform.position).sqrMagnitude;
                        if (sqrMagnitude > length * length) {
                            GotoFirst();
                        }
                    }
                    looped = false;
                }
                int accessIndex = pointsNumbers[0];
                foreach (var accessory in accessories) {
                    var diff = polygonData[accessIndex].PartsPosition(accessory.Key) - polygonData[0].Estimate;
                    diff = diff.RotateXZ(-this.transform.eulerAngles.y * Math.PI / 180);
                    diff.y += this.transform.position.y;
                    accessory.Value.ForEach(a => a.transform.position = this.transform.position + diff);
                }
                //var positions = new Vector3[pointers.Count];
                //var thisPos = this.transform.position;
                //var pn = pointsNumbers[0];
                //Parallel.ForEach<JointType>(Enum.GetValues(typeof(JointType)).Cast<JointType>(), type => {
                //    var diff = bodyList[pn][type] - bodyList[pn][(int)JointType.SpineBase];
                //    var basePos = estimates[pn % estimates.Length] + diff;
                //    positions[(int)type] = basePos;// ApplyPointerPos(mergePoints[pn].ToList(), basePos, type);
                //});
                //for (int i = 0; i < positions.Length; i++) {
                //if (partsCorrections[pn].ContainsKey((JointType)i)) {
                //    pointers[i].transform.position =  thisPos + partsCorrections[pn][(JointType)i];
                //} else {
                //    pointers[i].transform.position = thisPos + positions[i];
                //}
                //    pointers[i].transform.position = thisPos + positions[i];
                //}
                mesh.vertices = points.ToArray();
                mesh.colors = colors.ToArray();
                mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
            }
        }

        private void GotoFirst() {
            this.transform.position = firstPosition;
            beforeMag = double.MaxValue;
            nextRouteIndex = 1;
            if (route.Count > 1) {
                var next = route[nextRouteIndex];
                SetTarget(next);
            }
        }

        public void AddAccessories(JointType type, GameObject accessory) {
            if (!accessories.ContainsKey(type)) {
                accessories[type] = new List<GameObject>();
            }
            accessories[type].Add(accessory);
        }

        public void AddMotion(string motionName) {
            PolygonData[] before = polygonData.Clone() as PolygonData[];
            Vector3 center = Functions.AverageVector(before.Last().Complete.Select(c => c.GetVector3()).ToList());
            SetPolygonData(motionName);
            Vector3 newCenter = Functions.AverageVector(polygonData.First().Complete.Select(c => c.GetVector3()).ToList());
            print(center);
            print(newCenter);
            foreach (var pd in polygonData) {
                for (int i = 0; i < pd.Positions.Length; i++) {
                    for (int j = 0; j < pd.Positions[i].Length; j++) {
                        pd.Positions[i][j] += (center - newCenter);
                    }
                }
            }
            polygonData = before.Concat(polygonData).ToArray();
            addMotions.Add(motionName);
        }

        void LoadIndexCSV(string dir) {
            List<string[]> data = new List<string[]>();
            using (StreamReader reader = new StreamReader(string.Format(@"polygons\{0}\index.csv", dir))) {
                string str = reader.ReadLine();
                while(str != null) {
                    var split = str.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 0)
                        data.Add(split);
                    str = reader.ReadLine();
                }
            }
            var addTimes = new List<MyTime[]>();
            for (int i = 0; i < data.Count; i += kinectNums) {
                var times = new MyTime[kinectNums];
                for (int j = 0; j < kinectNums; j++) {
                    times[j] = ParseTime(data[i + j][1]);
                }
                addTimes.Add(times);
            }
            if (fileTimes.Count == 0) {
                fileTimes = addTimes;
            } else {
                MyTime[] lastTime = fileTimes.Last();
                MyTime[] firstTime = addTimes.First();
                int[] timeDiffs = new int[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    timeDiffs[i] = lastTime[i].GetMilli() - firstTime[i].GetMilli();
                }
                for (int i = 0; i < addTimes.Count; i++) {
                    for (int j = 0; j < kinectNums; j++) {
                        addTimes[i][j].AddMilli(timeDiffs[j]);
                    }
                }
                fileTimes = fileTimes.Concat(addTimes).ToList();
            }
        }

        void LoadModels(string dir) {
            string baseDir = @"polygons\" + dir;
            int num = 0;
            while (File.Exists(baseDir + @"\model_" + num + "_0.ply")) {
                num++;
            }
            while (File.Exists(baseDir + @"\model_0_" + (kinectNums - 1) + ".ply")) {
                kinectNums++;
            }
            kinectNums -= 1;
            frameAmount = num;
            polygonData = new PolygonData[num];
            var points = new Point[num][];
            for (int n = 0; n < num; n++) {
                var pointlist = new List<Point>[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    var plist = new List<Point>();
                    var fileName = baseDir + @"\model_" + n + "_" + i + ".ply";
                    foreach (var p in reader.Load(fileName)) {
                        plist.Add(p);
                    }
                    //yield return n;
                    if (i > 0) {
                        var source = PolygonData.BorderPoints(pointlist[0]);
                        var dest = PolygonData.BorderPoints(plist);
                        var diffY = PolygonData.CalcY(source, dest);
                        if (diffY < 0.2) {
                            pointlist[0] = pointlist[0].Select(p => p + new Vector3(0, (float)diffY, 0)).ToList();
                            //plist = plist.Select(p => p - new Vector3(0, (float)diffY, 0)).ToList();
                        }
                    }
                    pointlist[i] = plist;
                    //yield return n;
                }
                //ApplyXZ(pointlist);
                polygonData[n] = new PolygonData(dir, pointlist, true);
                //yield return n;
            }
            var manager = GameObject.FindObjectOfType<PolygonManager>();
            manager.SetData(dir, polygonData);
            var standard = polygonData[0].SetFirstEstimate();
            for (int i = 1; i < frameAmount; i++) {
                polygonData[i].EstimateHip(standard);
            }
            Vector3 center = Functions.AverageVector(polygonData[0].Complete.Select(p => p.GetVector3()).ToList());
            //center.y *= -1;
            foreach (var pd in polygonData) {
                for (int i = 0; i < pd.Positions.Length; i++) {
                    for (int j = 0; j < pd.Positions[i].Length; j++) {
                        pd.Positions[i][j] -= center;
                    }
                }
            }
            //firstPosition = this.transform.position;
            loadEnd = true;
        }

        void LoadBodyDump(string dir) {
            string filePath = @"polygons\" + dir + @"\SelectedUserBody.dump";
            var bodyList = (List<Dictionary<int, float[]>>)Utility.LoadFromBinary(filePath);
            var list = bodyList.Select(bl => bl.ToDictionary(d => (JointType)d.Key, d => new Vector3(d.Value[0], d.Value[1], d.Value[2]))).ToList();
            if (list.Count < polygonData.Length) {
                var newData = new PolygonData[list.Count];
                var newTimes = new List<MyTime[]>();
                for (int i = 0; i < list.Count; i++) {
                    newData[i] = polygonData[i];
                    newTimes.Add(fileTimes[i]);
                }
                polygonData = newData;
                fileTimes = newTimes;
            }
            for (int i = 0; i < Math.Min(list.Count, polygonData.Length); i++) {
                polygonData[i].SetBodyDump(list[i]);
            }
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

        public void Save(BinaryWriter writer) {
            writer.Write(DirName);
            writer.Write(addMotions.Count);
            foreach (var a in addMotions) {
                writer.Write(a);
            }
            writer.Write(route.Count);
            foreach (var r in route) {
                writer.Write(r.x);
                writer.Write(r.y);
            }
        }

        public static Tuple<List<string>, List<Vector2>> Load(BinaryReader reader) {
            var route = new List<Vector2>();
            var motions = new List<string>();
            string str = reader.ReadString();
            motions.Add(str);
            int motionCount = reader.ReadInt32();
            for (int i = 0; i < motionCount; i++) {
                motions.Add(reader.ReadString());
            }
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                route.Add(new Vector2(x, y));
            }
            return Tuple.Create(motions, route);
        }
    }

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
