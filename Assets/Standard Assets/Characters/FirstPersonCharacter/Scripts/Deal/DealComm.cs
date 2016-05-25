using UnityEngine;

using DealFuncPlug;
using System.Collections.Generic;
using System;
using System.Linq;

public class DealComm : MonoBehaviour {

    // Related to DealEmitter
    DealFuncPlugBase unityFuncPlug;
    public static string connectionState = "";
    public static short[] receivedHueVoxData;
    public static List<Dictionary<string, Dictionary<string, double>>> bodyHistoryList = new List<Dictionary<string, Dictionary<string, double>>>();
    public static Dictionary<string, double> receivedBodyDirection;
    public static double MovingState { get; private set; }
    public static double ThrowBallFlag { get; private set; }
    public static double IsRaiseRH { get; private set; }
    public static double IsRaiseLH { get; private set; }
    public static int Degree { get; private set; }
    static HandFlags handFlag;
    public static bool IsSpread { get; private set; }
    public static bool NearBy { get; private set; }
    public static bool BeforeIsSpread { get; private set; }
    public static bool BeforeNearBy { get; private set; }
    public static bool IsNewSpread { get { return IsSpread && !BeforeIsSpread; } }
    public static bool NewNearBy { get; private set; }
    static HandFlags beforeHandFlag;

    // Use this for initialization
    void Start() {
        unityFuncPlug = new DealFuncPlugBase("localhost", 48200);
        connectionState = unityFuncPlug.ConnectionStatus;
        unityFuncPlug.ReceiveFromEmitter += unityFuncPlug_ReceiveFromEmitter;
        unityFuncPlug.RegisterTrigger("MainBodyData", "Dictionary<string, Dictionary<string, double>>", "No");
        unityFuncPlug.RegisterTrigger("BodyDirection", "Dictionary<string, double>", "No");
    }

    List<int> bodyDirDegHistory = new List<int>();

    // Update is called once per frame
    void Update() {
        if (receivedBodyDirection != null) {
            double s;
            int deg;
            s = Math.Acos(receivedBodyDirection["X"] / Math.Sqrt(receivedBodyDirection["X"] * receivedBodyDirection["X"] + receivedBodyDirection["Z"] * receivedBodyDirection["Z"])); // 角度θを求める
            s = (s / Math.PI) * 180.0; // ラジアンを度に変換
            if (receivedBodyDirection["Z"] < 0) s = 360 - s; // θ＞πの時
            deg = (int)Math.Floor(s);
            if ((s - deg) >= 0.5) deg++; // 小数点を四捨五入
            deg = deg - 180;
            bodyDirDegHistory.Add(deg);
            if (bodyDirDegHistory.Count > 10) bodyDirDegHistory.RemoveAt(0);
            // 履歴からメディアンフィルタをかける
            int[] tmpDirHistory = bodyDirDegHistory.ToArray();
            Array.Sort(tmpDirHistory);
            deg = tmpDirHistory[(int)Math.Floor(tmpDirHistory.Length / 2.0)];
            Degree = deg;
        }
    }

    private void UpdateFlag() {
        beforeHandFlag = handFlag;
        handFlag = 0;
        var raiseLeftHands = new List<HandFlags>();
        for (int i = 0; i < bodyHistoryList.Count; i++) {
            // 左手のY座標が左肩の上にあるか取得
            try {
                if (bodyHistoryList[i]["HandLeft"]["Y"] - bodyHistoryList[i]["ShoulderLeft"]["Y"] > 0) {
                    raiseLeftHands.Add(HandFlags.LeftHandUp);
                } else {
                    raiseLeftHands.Add(HandFlags.LeftHandDown);
                }
            } catch (NullReferenceException e) {
                print(e.Message);
            }
        }

        var raiseRightHands = new List<HandFlags>();
        // 右腕の位置関係の把握
        for (int i = 0; i < bodyHistoryList.Count; i++) {
            // 右手のY座標が右肩の上にあるか取得
            try {
                if (bodyHistoryList[i]["HandRight"]["Y"] - bodyHistoryList[i]["ShoulderRight"]["Y"] > 0) {
                    raiseRightHands.Add(HandFlags.RightHandUp);
                } else {
                    raiseRightHands.Add(HandFlags.RightHandDown);
                }
            } catch (KeyNotFoundException e) {
                print(e.Message);
            }
        }
        var spread = new List<bool>();
        var nearby = new List<bool>();
        double threshold = 0.2;
        for (int i = 0; i < bodyHistoryList.Count; i++) {
            //両手を広げているか取得
            try {
                double x = bodyHistoryList[i]["HandRight"]["X"] - bodyHistoryList[i]["HandLeft"]["X"];
                double z = bodyHistoryList[i]["HandRight"]["Z"] - bodyHistoryList[i]["HandLeft"]["Z"];
                if (x * x + z * z > 1) {
                    spread.Add(true);
                } else {
                    spread.Add(false);
                }
                if (x * x + z * z < threshold * threshold) {
                    nearby.Add(true);
                } else {
                    nearby.Add(false);
                }
            } catch (KeyNotFoundException e) {
                print(e.Message);
            }
        }
        BeforeIsSpread = IsSpread;
        IsSpread = spread.All(s => s);
        BeforeNearBy = NearBy;
        NearBy = nearby.All(n => n);
        foreach (HandFlags flag in Enum.GetValues(typeof(HandFlags))) {
            if (raiseLeftHands.All(r => r == flag) || raiseRightHands.All(r => r == flag)) {
                handFlag |= flag;
            }
        }
        bodyHistoryList.RemoveAt(0);
    }

    public static bool GetNewHandFlag(HandFlags flag) {
        return GetHandFlag(flag) && (beforeHandFlag & flag) != flag;
    }

    public static bool GetNewHandFlag(params HandFlags[] flag) {
        return flag.All(f => GetNewHandFlag(f));
    }

    public static bool GetHandFlag(HandFlags flag) {
        return (handFlag & flag) == flag;
    }

    public static bool GetHandFlag(params HandFlags[] flag) {
        return flag.All(f => GetHandFlag(f));
    }

    // Data Receiving Event
    void unityFuncPlug_ReceiveFromEmitter(object sender, DealFuncPlug.ReceiveFromEmitterEventArgs e) {
        switch (e.dataIdentifier) {
            case "MainBodyData":
                bodyHistoryList.Add((Dictionary<string, Dictionary<string, double>>)unityFuncPlug.DataDeserializing(e.dataBytes));
                if (bodyHistoryList.Count > 11) {
                    UpdateFlag();
                }
                break;
            case "BodyDirection":
                receivedBodyDirection = (Dictionary<string, double>)unityFuncPlug.DataDeserializing(e.dataBytes);
                break;
        }
    }
}

public enum HandFlags {
    RightHandDown = 1,
    RightHandMiddle = 2,
    RightHandUp = 4,
    LeftHandDown = 8,
    LeftHandMiddle = 16,
    LeftHandUp = 32,
}
