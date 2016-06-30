﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class PolygonManager : MonoBehaviour {
        public static PolygonManager Instance { get; private set; }
        public Dictionary<string, PolygonData[]> Data { get; private set; }
        public Dictionary<string, Vector3[]> CharacterValues { get; private set; }
        public Dictionary<string, double[][]> Histgrams { get; private set; }
        static string extensions = ".pldt";
        static string histgramsDataName = "motion.dat";
        static string characterValueName = "charavalue.dat";
        static string resultDir { get { return "result"; } }
        PlyReader reader = new PlyReader();

        private void Awake() {
            if (Instance != null) {
                Destroy(this.gameObject);
            } else {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
                Data = new Dictionary<string, PolygonData[]>();
                Histgrams = new Dictionary<string, double[][]>();
                CharacterValues = new Dictionary<string, Vector3[]>();
            }
        }

        private void Start() {
            LoadCharacters();
        }

        public void SetData(string name, PolygonData[] data) {
            Data[name] = data;
            Histgrams[name] = data.Select(d => PolygonData.Histogram(PolygonData.Magnitudes(d.Complete.Select(c => c.GetVector3()).ToList()))).ToArray();
        }

        public static void Save() {
            foreach (var d in Instance.Data) {
                string dir = Path.Combine(resultDir, d.Key);
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                string file = dir + "/" + d.Key + extensions;
                if (!File.Exists(file)) {
                    File.Create(file);
                }
                using (var writer = new StreamWriter(file, false)) {
                    writer.WriteLine(d.Key);
                    for (int i = 0; i < d.Value.Length; i++) {
                        writer.WriteLine(i);
                        d.Value[i].Save(writer);
                    }
                }
                for (int i = 0; i < d.Value.Length; i++) {
                    d.Value[i].SavePointCloud(dir, i);
                }
            }
        }

        public static void Load(string key) {
            string file = key + extensions;
            if (File.Exists(file)) {
                using (var stream = new FileStream(file, FileMode.Open)) {
                    using (var breader = new BinaryReader(stream)) {
                        if (key == breader.ReadString()) {
                            int length = breader.ReadInt32();
                            for (int i = 0; i < length; i++) {
                                Instance.Data[key][i].Load(breader);
                            }
                        }
                    }
                }
            }
        }

        public static void LoadCharacters() {
            if (File.Exists(characterValueName)) {
                using (var stream = new FileStream(characterValueName, FileMode.Open)) {
                    using (var breader = new BinaryReader(stream)) {
                        int count = breader.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            string key = breader.ReadString();
                            int length = breader.ReadInt32();
                            var vectors = new List<Vector3>();
                            for (int j = 0; j < length; j++) {
                                double x = breader.ReadDouble();
                                double y = breader.ReadDouble();
                                double z = breader.ReadDouble();
                                vectors.Add(new Vector3((float)x, (float)y, (float)z));
                            }
                            Instance.CharacterValues[key] = vectors.ToArray();
                        }
                    }
                }
            }
        }
    }
}
