using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnvironmentMaker {
    class MyTime {
        public int Hour { get; private set; }
        public int Minute { get; private set; }
        public int Second { get; private set; }
        public int MilliSecond { get; private set; }
        public MyTime(int hour, int minute, int second, int milli) {
            Hour = hour;
            Minute = minute;
            Second = second;
            MilliSecond = milli;
        }

        public int GetMilli() {
            return Second * 1000 + MilliSecond;
        }

        public void AddMilli(int milli) {
            int second = milli / 1000;
            milli %= 1000;
            MilliSecond += milli;
            second += second;
            if (MilliSecond < 0) {
                while(MilliSecond < 0) {
                    MilliSecond += 1000;
                    Second -= 1;
                }
            } else if (MilliSecond > 1000) {
                Second += MilliSecond / 1000;
                MilliSecond %= 1000;
            }
            if (Second < 0) {
                while (Second < 0) {
                    Second += 60;
                    Minute -= 1;
                }
            } else if (Second > 60) {
                Minute += Second / 60;
                Second %= 60;
            }
        }
    }
}
