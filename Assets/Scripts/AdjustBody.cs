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
    class AdjustBody : MonoBehaviour {
        PlyReader reader;
        List<Vector3> bodyposes;
        int kinectNums = 3;
        public GameObject Pointer;
        List<GameObject> pointers = new List<GameObject>();
        bool loadEnd = false;
        bool looped = false;
        List<Vector2> route = new List<Vector2>();
        int nextRouteIndex = 0;
        float length = 10f;
        Vector3 firstPosition;
        TwoDLine bodyLine;
        GameObject cursor;
        JointType selectedType = JointType.SpineBase;
        public GameObject Selected;
        /// <summary>
        /// 赤いやつ
        /// </summary>
        private GameObject selectedPointer;
        public GameObject MainCamera;
        public GameObject OverViewCamera;
        private Vector3 stopCameraPosition;
        private Vector3 stopCameraEularAngle;
        private Vector3 movePosition;
        private Vector3 moveEularAngle;
        private bool stopped = true;
        private int baseIndex;
        private Vector3[,] firstBodyParts;
        private PolygonData[] polygonData;
        private int FrameAmount;
        private PolygonManager manager;

        private Mesh mesh;

        int[] beforeTime;
        int[] pointsNumbers;
        List<MyTime[]> fileTimes;

        public string DirName = "result";
        private string tmpDirName;

        bool movePointCloud = false;
        int selectedPCNumber = 0;

        // Use this for initialization
        void Start() {
            mesh = new Mesh();
            reader = new PlyReader();
            fileTimes = new List<MyTime[]>();
            beforeTime = new int[kinectNums];
            pointsNumbers = new int[kinectNums];
            for (int i = 0; i < pointsNumbers.Length; i++) {
                pointsNumbers[i] = 0;
            }
            GetComponent<MeshFilter>().mesh = mesh;
            manager = GameObject.FindObjectOfType<PolygonManager>();
            foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                var obj = Instantiate(Pointer) as GameObject;
                obj.name = Enum.GetName(typeof(JointType), type);
                obj.GetComponentInChildren<TextMesh>().text = obj.name;
                obj.SetActive(false);
                pointers.Add(obj);
            }
            baseIndex = 0;
            selectedPointer = Instantiate(Selected);
            stopCameraPosition = MainCamera.transform.position;
            stopCameraEularAngle = MainCamera.transform.localEulerAngles;
            movePosition = new Vector3(1.1f, 1.4f, -1);
            moveEularAngle = new Vector3(0, 0, 0);
            tmpDirName = DirName;
            if (DirName != "" && Directory.Exists(@"polygons/" + DirName)) {
                LoadModels(DirName);
                LoadIndexCSV(DirName);
                LoadBodyDump(DirName);
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    pointers[(int)type].transform.position = this.transform.position + polygonData[0].PartsPosition(type);
                }
                PolygonManager.Load(DirName);
                UpdateMesh();
            }
        }

        private Vector3 InputASDWZX(Vector3 before) {
            if (Input.GetKey(KeyCode.D)) {
                before += new Vector3(0.01f, 0, 0);
            } else if (Input.GetKey(KeyCode.A)) {
                before -= new Vector3(0.01f, 0, 0);
            } else if (Input.GetKey(KeyCode.W)) {
                before += new Vector3(0, 0, 0.01f);
            } else if (Input.GetKey(KeyCode.S)) {
                before -= new Vector3(0, 0, 0.01f);
            } else if (Input.GetKey(KeyCode.X)) {
                before += new Vector3(0, 0.01f, 0);
            } else if (Input.GetKey(KeyCode.Z)) {
                before -= new Vector3(0, 0.01f, 0);
            }
            return before;
        }

        private int InputKeyUpDown(int now, int max) {
            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                return (now + 1) % max;
            } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                return (now - 1 + max) % max;
            } else {
                return now;
            }
        }

        double beforeMag = double.MaxValue;
        void Update() {
            if (loadEnd) {
                if (stopped) {
                    GameObject selectedObj = pointers[(int)selectedType];

                    int number = pointsNumbers[0];
                    Vector3 beforeInput = (movePointCloud) ? polygonData[number].PointCloudOffsets[selectedPCNumber] : polygonData[number].Offsets[selectedType];
                    Vector3 afterInput = InputASDWZX(beforeInput);
                    if (movePointCloud) {
                        polygonData[number].PointCloudOffsets[selectedPCNumber] = afterInput;
                    } else {
                        polygonData[number].Offsets[selectedType] = afterInput;
                    }

                    //selectedObj.transform.position = firstJoint[number, (int)selectedType] + offsets[number, (int)selectedType];
                    if (movePointCloud) {
                        selectedPCNumber = InputKeyUpDown(selectedPCNumber, kinectNums);
                    } else {
                        selectedPointer.transform.position = selectedObj.transform.position;
                        var beforeType = selectedType;
                        selectedType = (JointType)InputKeyUpDown((int)selectedType, Enum.GetNames(typeof(JointType)).Length);

                        if (beforeType != selectedType) {
                            pointers[(int)beforeType].SetActive(false);
                            pointers[(int)selectedType].SetActive(true);
                        }
                    }

                    int before = pointsNumbers[0];
                    if (Input.GetKeyDown(KeyCode.RightArrow)) {
                        pointsNumbers[0] = (pointsNumbers[0] + 1) % FrameAmount;
                    } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                        pointsNumbers[0] = (pointsNumbers[0] - 1 + FrameAmount) % FrameAmount;
                    }
                    if (before != pointsNumbers[0]) {
                        AdjustStopCamera(before);
                    }
                    OverViewCamera.transform.position = pointers[(int)selectedType].transform.position + new Vector3(0, 0.5f, 0);

                    if (Input.GetKey(KeyCode.J)) {
                        OverViewCamera.transform.position += new Vector3(0, -0.01f, 0);
                    } else if (Input.GetKey(KeyCode.K)) {
                        OverViewCamera.transform.position += new Vector3(0, 0.01f, 0);
                    } else if (Input.GetKey(KeyCode.H)) {
                        OverViewCamera.transform.position += new Vector3(-0.01f, 0, 0);
                    } else if (Input.GetKey(KeyCode.L)) {
                        OverViewCamera.transform.position += new Vector3(0.01f, 0, 0);
                    }
                }

                if (movePointCloud) {
                    UpdateMesh();
                }

                if (Input.GetKeyDown(KeyCode.Space)) {
                    stopped = !stopped;
                    if (stopped) {
                        OverViewCamera.SetActive(true);
                        selectedPointer.SetActive(true);
                        MainCamera.transform.position = stopCameraPosition;
                        MainCamera.transform.localEulerAngles = stopCameraEularAngle;
                        for (int i = 0; i < kinectNums; i++) {
                            pointsNumbers[i] = 0;
                            beforeTime[i] = 0;
                        }
                        var positions = new Vector3[pointers.Count];
                        var thisPos = this.transform.position;
                        var pn = pointsNumbers[0];
                        foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                            pointers[(int)type].transform.position = this.transform.position + polygonData[pn].PartsPosition(type);
                        }
                        UpdateMesh();
                        foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                            pointers[(int)type].SetActive(false);
                        }
                        pointers[(int)selectedType].SetActive(true);
                    } else {
                        OverViewCamera.SetActive(false);
                        selectedPointer.SetActive(false);
                        MainCamera.transform.position = movePosition;
                        MainCamera.transform.eulerAngles = moveEularAngle;
                        for (int i = 0; i < kinectNums; i++) {
                            pointsNumbers[i] = 0;
                            beforeTime[i] = 0;
                        }
                        CalcCorrection();
                        foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                            pointers[(int)type].SetActive(true);
                        }
                    }
                }
            }
        }

        void FixedUpdate() {
            if (loadEnd) {
                bool changed = false;
                var time = Time.deltaTime * 1000;
                for (int i = 0; i < kinectNums; i++) {
                    if (!stopped) {
                        beforeTime[i] += (int)Math.Floor(time);
                        int timeDiff = 0;
                        int index = pointsNumbers[i];
                        try {
                            timeDiff = fileTimes[(index + 1) % fileTimes.Count][i].GetMilli() - fileTimes[index][i].GetMilli();
                        } catch (ArgumentOutOfRangeException e) {
                            print(e.Message);
                        }
                        if (beforeTime[i] > timeDiff) {
                            var before = pointsNumbers[i];
                            pointsNumbers[i] = (pointsNumbers[i] + 1) % FrameAmount;
                            changed = true;
                        }
                    }
                }
                if (changed) {
                    UpdateMesh();
                }
                var positions = new Vector3[pointers.Count];
                var thisPos = this.transform.position;
                var pn = pointsNumbers[0];
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    pointers[(int)type].transform.position = this.transform.position + polygonData[pn].PartsPosition(type);
                }
            }
        }

        void UpdateMesh() {
            var oldMesh = mesh;
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
            DestroyImmediate(oldMesh);

            var points = new List<Vector3>();
            var colors = new List<Color>();
            for (int i = 0; i < kinectNums; i++) {
                //if (i == 0) continue;
                var offset = polygonData[pointsNumbers[i]].PointCloudOffsets[i];
                foreach (var p in polygonData[pointsNumbers[i]].KinectPoints[i]) {
                    points.Add(p.GetVector3() + offset);
                    colors.Add(p.GetColor());
                }
            }
            mesh.vertices = points.ToArray();
            mesh.colors = colors.ToArray();
            mesh.SetIndices(Enumerable.Range(0, points.Count).ToArray(), MeshTopology.Points, 0);
        }

        private void CalcCorrection() {
            foreach (var p in polygonData) {
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    var off = (p.Offsets[type] == Vector3.zero) ? Vector3.zero : p.OffsetsAndCorrection(type);
                    p.Reset(type);
                    p.Offsets[type] = off;
                }
            }
            for (int i = 0; i < firstBodyParts.GetLength(0); i++) {
                var positions = new Vector3[pointers.Count];
                var thisPos = this.transform.position;
                foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                    firstBodyParts[i, (int)type] = this.transform.position + polygonData[i].PartsPosition(type);
                }
            }
            var reworkIndexes = new List<int>();
            for (int i = 0; i < FrameAmount; i++) {
                if (polygonData[i].Offsets.Values.ToList().Exists(o => o != Vector3.zero)) {
                    reworkIndexes.Add(i);
                }
            }
            if (reworkIndexes.Count > 0) {
                ArmCorrection(reworkIndexes);
                LegCorrection(reworkIndexes);
            }
        }

        private void ArmCorrection(List<int> reworkIndexes) {
            var partsNames = new[] { "Shoulder", "Elbow", "Wrist", "Hand", "HandTip", "Thumb" };
            var partsNamesLeft = new string[partsNames.Length + 1];
            var partsNamesRight = new string[partsNames.Length + 1];
            for (int i = 0; i < partsNames.Length; i++) {
                partsNamesLeft[i + 1] = partsNames[i] + "Left";
                partsNamesRight[i + 1] = partsNames[i] + "Right";
            }
            partsNamesLeft[0] = "SpineShoulder";
            partsNamesRight[0] = "SpineShoulder";
            EitherCorrection(reworkIndexes, partsNamesLeft);
            EitherCorrection(reworkIndexes, partsNamesRight);
        }

        private void LegCorrection(List<int> reworkIndexes) {
            var partsNames = new[] { "Hip", "Knee", "Ankle", "Foot" };
            var partsNamesLeft = new string[partsNames.Length + 1];
            var partsNamesRight = new string[partsNames.Length + 1];
            for (int i = 0; i < partsNames.Length; i++) {
                partsNamesLeft[i + 1] = partsNames[i] + "Left";
                partsNamesRight[i + 1] = partsNames[i] + "Right";
            }
            partsNamesLeft[0] = "SpineBase";
            partsNamesRight[0] = "SpineBase";
            EitherCorrection(reworkIndexes, partsNamesLeft);
            EitherCorrection(reworkIndexes, partsNamesRight);
        }

        private void EitherCorrection(List<int> reworkIndexes, string[] joints) {
            JointType[] partsTypes = joints.Select(p => (JointType)Enum.Parse(typeof(JointType), p)).ToArray();
            var allNumbers = Enumerable.Range(0, FrameAmount).ToList();
            reworkIndexes.ForEach(i => allNumbers.Remove(i));
            int depth = 1;
            var already = new List<int>();
            var search = new List<int>();
            reworkIndexes.ForEach(r => already.Add(r));
            var remove = new List<int>();
            for (int i = 1; i < partsTypes.Length; i++) {
                bool handMade = false;
                var histgrams = new List<double[]>();
                var chistgrams = new List<double[]>();
                JointType joint = partsTypes[i];
                JointType beforeJoint = partsTypes[i - 1];
                var existsIndexes = new Dictionary<int, Vector3>();
                var existsCIndexes = new Dictionary<int, Vector3>();
                var lengthes = new List<float>();
                for (int j = 0; j < reworkIndexes.Count; j++) {
                    int index = reworkIndexes[j];
                    if (polygonData[index].Offsets[joint] != Vector3.zero) {
                        handMade = true;
                        var jointIndex = polygonData[index].Voxel.GetIndexFromPosition(firstBodyParts[index, (int)joint] - this.transform.position);
                        var jointVoxel = polygonData[index].Voxel[(int)jointIndex.x, (int)jointIndex.y, (int)jointIndex.z];
                        lengthes.Add((firstBodyParts[index, (int)joint] - firstBodyParts[index, (int)beforeJoint]).magnitude);
                        if (jointVoxel.Count > 0) {
                            var histgram = polygonData[index].GetVoxelHistgram(jointIndex);
                            var chistgram = polygonData[index].GetColorHistgram(jointIndex);
                            if (histgram != null) {
                                histgrams.Add(histgram);
                                existsIndexes.Add(index, jointIndex);
                            }
                            if (chistgram != null) {
                                chistgrams.Add(chistgram);
                                existsCIndexes.Add(index, jointIndex);
                            }
                            if (histgram == null && chistgram == null) {
                                remove.Add(index);
                            }
                        } else {
                            remove.Add(index);
                        }
                    }
                }
                if (lengthes.Count == 0) continue;
                float length = lengthes.Average();
                remove.ForEach(r => reworkIndexes.Remove(r));
                Vector3 histVec = Vector3.zero, histCVec = Vector3.zero;
                double[] averageHistgram = null, averageCHistgram = null;
                if (histgrams.Count > 0) {
                    averageHistgram = new double[histgrams[0].Length];
                    for (int j = 0; j < averageHistgram.Length; j++) {
                        averageHistgram[j] = histgrams.Average(h => h[j]);
                    }
                }
                if (chistgrams.Count > 0) {
                    averageCHistgram = new double[chistgrams[0].Length];
                    for (int j = 0; j < averageCHistgram.Length; j++) {
                        averageCHistgram[j] = chistgrams.Average(h => h[j]);
                    }
                }
                var notFoundIndexes = new List<int>();
                if (reworkIndexes.Count > 0) {
                    while (already.Count < FrameAmount) {
                        foreach (var ri in reworkIndexes) {
                            int plus = ri + depth;
                            int minus = ri - depth;
                            if (plus < FrameAmount) {
                                if (!already.Contains(plus)) {
                                    search.Add(plus);
                                    already.Add(plus);
                                }
                            }
                            if (minus >= 0) {
                                if (!already.Contains(minus)) {
                                    search.Add(minus);
                                    already.Add(minus);
                                }
                            }
                        }
                        depth++;
                    }
                }
                foreach (var s in search) {
                    if (averageHistgram != null)
                        histVec = SearchHistgram(averageHistgram, s, existsIndexes, joint);
                    if (averageCHistgram != null)
                        histCVec = SearchHistgram(averageCHistgram, s, existsCIndexes, joint);
                    Vector3 result = Vector3.zero;
                    if (histVec != Vector3.zero && histCVec != Vector3.zero) {
                        result = (histVec + histCVec) / 2;
                    } else if (histCVec != Vector3.zero) {
                        result = histCVec;
                    } else if (histVec != Vector3.zero) {
                        result = histVec;
                    } else {
                        notFoundIndexes.Add(s);
                    }
                    if (result != Vector3.zero) {
                        polygonData[s].PartsCorrection[joint] = Vector3.zero;
                        var beforePosition = polygonData[s].PartsPosition(beforeJoint);
                        var nowPosition = polygonData[s].PartsPosition(joint) + result;
                        result = (nowPosition - beforePosition).normalized * length + beforePosition - polygonData[s].PartsPosition(joint);
                        //if (s == 1)
                        //    print(s + "の" + Enum.GetName(typeof(JointType), joint) + ":" + result);
                        polygonData[s].PartsCorrection[joint] = result;
                    }
                }
                notFoundIndexes.Sort();
                if (handMade && notFoundIndexes.Count > 0) {
                    var startAndEnd = new List<Tuple<int, int>>();
                    int before = -1, start = -1, end = -1;
                    for (int j = 0; j < notFoundIndexes.Count; j++) {
                        if (before == -1) {
                            start = notFoundIndexes[j];
                        } else {
                            end = notFoundIndexes[j];
                            if (end - before > 1) {
                                startAndEnd.Add(Tuple.Create(start, before));
                                start = end;
                            }
                        }
                        before = notFoundIndexes[j];
                    }
                    startAndEnd.Add(Tuple.Create(start, end));
                    for (int j = 0; j < startAndEnd.Count; j++) {
                        start = startAndEnd[j].First - 1;
                        end = Math.Min(startAndEnd[j].Second, polygonData.Length);
                        try {
                            Vector3 startPosition = polygonData[start].PartsPosition(joint);
                            Vector3 endPosition = polygonData[end + 1].PartsPosition(joint);
                            Vector3 move = (endPosition - startPosition) / (end - start);
                            for (int k = start + 1; k <= end; k++) {
                                print(k + "の" + Enum.GetName(typeof(JointType), joint) + "を補間");
                                polygonData[k].PartsCorrection[joint] = startPosition + move * (k - start) - polygonData[k].PartsPosition(joint);
                            }
                        } catch (Exception e) {
                            print(e.Message);
                        }
                    }
                }
            }
        }

        private Vector3 SearchHistgram(double[] averageHistgram, int k, Dictionary<int, Vector3> existsIndexes, JointType firstJoint) {
            int index = existsIndexes.Keys.ToList().IndexOfMin(i => Math.Abs(k - i));
            int key = existsIndexes.Keys.ToList()[index];
            Vector3 histgramIndex = polygonData[k].SearchHistgram(averageHistgram, existsIndexes[key]);
            //if (k == 1) 
            //    print(k + "の" + Enum.GetName(typeof(JointType), firstJoint) + "のHistgramIndex:" + histgramIndex.ToString());
            if ((histgramIndex - existsIndexes[key]).sqrMagnitude > 4) {
                return Vector3.zero;
            }
            if (existsIndexes.ContainsKey(k)) {
                Vector3 average = (existsIndexes[k] + histgramIndex) * 0.5f;
                existsIndexes[k] = new Vector3((int)average.x, (int)average.y, (int)average.z);
            } else {
                existsIndexes[k] = histgramIndex;
            }
            Vector3 position = polygonData[k].Voxel.GetPositionFromIndex(histgramIndex);
            //if (k == 1)
            //    print(k + "の" + Enum.GetName(typeof(JointType), firstJoint) + "position:" + position.ToString());
            //if (k == 1)
            //    print(k + "の" + Enum.GetName(typeof(JointType), firstJoint) + "firstBodyParts:" + firstBodyParts[k, (int)firstJoint].ToString());
            return this.transform.position + position - firstBodyParts[k, (int)firstJoint];
        }

        void LoadModels(string dir) {
            string baseDir = @"polygons/" + dir;
            int num = 0;
            while (File.Exists(baseDir + @"/model_" + num + "_0.ply")) {
                num++;
            }
            FrameAmount = num;
            polygonData = new PolygonData[num];
            var points = new Point[num][];
            for (int n = 0; n < num; n++) {
                var pointlist = new List<Point>[kinectNums];
                for (int i = 0; i < kinectNums; i++) {
                    var plist = new List<Point>();
                    var fileName = baseDir + @"/model_" + n + "_" + i + ".ply";
                    foreach (var p in reader.Load(fileName)) {
                        plist.Add(p);
                    }
                    if (i > 0) {
                        var source = new List<Point>();
                        for (int j = 0; j < i; j++) {
                            pointlist[j].ForEach(p => source.Add(p));
                        }
                        var sourceBorder = PolygonData.BorderPoints(source);
                        var destBorder = PolygonData.BorderPoints(plist);
                        float diffY = (float)PolygonData.CalcY(sourceBorder, destBorder);
                        if (diffY < 0.2) {
                            plist = plist.Select(p => p - new Vector3(0, diffY, 0)).ToList();
                        }
                    }
                    pointlist[i] = plist;
                }
                polygonData[n] = new PolygonData(dir, pointlist);
            }
            var standard = polygonData[0].SetFirstEstimate();
            for (int i = 1; i < FrameAmount; i++) {
                polygonData[i].EstimateHip(standard);
            }
            var diff = Functions.AverageVector(polygonData[0].Complete.Select(p => p.GetVector3()).ToList());
            this.transform.position -= diff;
            firstPosition = this.transform.position;
            if (!manager.Data.ContainsKey(dir)) {
                manager.SetData(dir, this.polygonData);
            }
            firstBodyParts = new Vector3[FrameAmount, Enum.GetNames(typeof(JointType)).Length];
            loadEnd = true;
        }

        Point[] ReducePoints(Point[] points) {
            var result = new List<Point>();
            var tmp = new List<Point>();
            int max = points.Length / 10;
            foreach (var p in points) {
                tmp.Add(p);
            }
            for (int i = 0; i < max; i++) {
                var point = tmp[Functions.GetRandomInt(tmp.Count)];
                result.Add(point);
                tmp.Remove(point);
            }

            return result.ToArray();
        }

        void LoadIndexCSV(string dir) {
            List<string[]> data = new List<string[]>();
            using (StreamReader reader = new StreamReader(@"polygons/" + dir + @"/index.csv")) {
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

        void LoadBodyDump(string dir) {
            string filePath = @"polygons/" + dir + @"/SelectedUserBody.dump";
            var bodyList = (List<Dictionary<int, float[]>>)Utility.LoadFromBinary(filePath);
            var list = bodyList.Select(bl => bl.ToDictionary(d => (JointType)d.Key, d => new Vector3(d.Value[0], d.Value[1], d.Value[2]))).ToList();
            if (list.Count < FrameAmount) {
                var newData = new PolygonData[list.Count];
                var newTimes = new List<MyTime[]>();
                for (int i = 0; i < list.Count; i++) {
                    newData[i] = polygonData[i];
                    newTimes.Add(fileTimes[i]);
                }
                polygonData = newData;
                fileTimes = newTimes;
                FrameAmount = list.Count;
            }
            for (int i = 0; i < Math.Min(list.Count, FrameAmount); i++) {
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

        void LoadBody(string dir) {
            using (StreamReader reader = new StreamReader(@"polygons/" + dir + @"/bodyposes.txt")) {
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
                var border = PolygonData.BorderPoints(points[i]);
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

        private void OnGUI() {
            GUI.TextArea(new Rect(0, 0, 100, 20), "選択中");
            GUI.TextArea(new Rect(0, 20, 100, 20), movePointCloud ? selectedPCNumber.ToString() : Enum.GetName(typeof(JointType), selectedType));

            if (loadEnd && stopped) {
                int before = pointsNumbers[0];
                int barHeight = 20;
                pointsNumbers[0] = (int)GUI.HorizontalScrollbar(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), before, 1, 0, FrameAmount);
                if (before != pointsNumbers[0]) {
                    AdjustStopCamera(before);
                }
                GUI.TextArea(new Rect(100, 0, 100, 20), pointsNumbers[0].ToString());
            }

            int finishWidth = 100;
            if (GUI.Button(new Rect(Screen.width - finishWidth, 0, finishWidth, 100), "終わる")) {
                SaveData();
                Application.Quit();
            }

            int saveWidth = 100;
            if (GUI.Button(new Rect(Screen.width - finishWidth - saveWidth, 0, saveWidth, 100), "保存")) {
                SaveData();
            }

            int switchWidth = 100;
            if (GUI.Button(new Rect(Screen.width - finishWidth - saveWidth - switchWidth, 0, switchWidth, 100), movePointCloud ? "点群移動モード" : "トラッキング\nモード")) {
                movePointCloud = !movePointCloud;
                if (movePointCloud) {
                    SwitchToMovePointCloud();
                } else {
                    SwitchToTracking();
                }
            }

            var beforeDir = tmpDirName;
            GUI.TextArea(new Rect(100, 40, 100, 20), "モーション名");
            tmpDirName = GUI.TextArea(new Rect(100, 60, 100, 20), beforeDir);
            if (beforeDir != tmpDirName && tmpDirName != "" && DirName != tmpDirName && Directory.Exists(@"polygons/" + tmpDirName)) {
                if (DirName != "") {
                    SaveData();
                }
                DirName = tmpDirName;
                LoadModels(DirName);
                LoadIndexCSV(DirName);
                LoadBodyDump(DirName);
                UpdateMesh();
            }
        }

        private void SwitchToMovePointCloud() {
            selectedPointer.SetActive(false);
            pointers[(int)selectedType].SetActive(false);
        }

        private void SwitchToTracking() {
            selectedPointer.SetActive(true);
            pointers[(int)selectedType].SetActive(true);
        }

        private void SaveData() {
            manager.SetData(DirName, polygonData);
            PolygonManager.Save();
        }

        private void AdjustStopCamera(int before) {
            for (int i = 1; i < kinectNums; i++) {
                pointsNumbers[i] = pointsNumbers[0];
            }
            UpdateMesh();
            Vector3 nowCenter = Functions.AverageVector(polygonData[pointsNumbers[0]].Merge.Select(mp => mp.GetVector3()).ToList());
            Vector3 beforeCenter = Functions.AverageVector(polygonData[before].Merge.Select(mp => mp.GetVector3()).ToList());
            Vector3 moved = nowCenter - beforeCenter;
            MainCamera.transform.position += moved;
        }
    }
}
