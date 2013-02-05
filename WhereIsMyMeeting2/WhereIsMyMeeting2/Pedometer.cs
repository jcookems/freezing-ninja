using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace WhereIsMyMeeting2
{
    class Pedometer
    {
        double minAcc;
        double maxAcc;

        double curAcc;
        bool upswing = true;
        private double runningAveAcc = 0;
        private Queue<double> accelerations = new Queue<double>();
        DateTime lastStep = DateTime.Now;

        public System.Windows.Point? getStrideIfStep(MotionReading reading)
        {
            int count = 8;
            double prevAcc = curAcc;
            curAcc = Vector3.Dot(reading.Gravity, reading.DeviceAcceleration);
            accelerations.Enqueue(curAcc);
            runningAveAcc += curAcc / count;
            if (accelerations.Count > count)
            {
                runningAveAcc -= accelerations.Dequeue() / count;
            }

            if (!upswing)
            {
                if (curAcc < 0 && curAcc > prevAcc)
                {
                    minAcc = prevAcc;
                    upswing = true;
                }
            }
            else
            {
                if (curAcc > 0 && curAcc < prevAcc)
                {
                    maxAcc = curAcc;
                    upswing = false;

                    // From Application note AN-602: http://www.analog.com/static/imported-files/application_notes/513772624AN602.pdf
                    double STRIDE = 60 * Math.Sqrt(Math.Sqrt(maxAcc - minAcc));

                    // Steps take longer than 1/4 second, and the delta acceleration is more than 1/4 g.
                    double stepDuration = ((DateTime.Now - lastStep).TotalSeconds);
                    lastStep = DateTime.Now;
                    if ((stepDuration > 0.25) && (stepDuration < 2) && ((maxAcc - minAcc) > 0.25))
                    {
                        Vector3 transformed = Vector3.Transform(new Vector3(-1, 0, 0), reading.Attitude.RotationMatrix);
                        double rot = Math.Atan2(transformed.X, transformed.Y);
                        double strideX = -STRIDE * Math.Sin(rot);
                        double strideY = -STRIDE * Math.Cos(rot);
                        return new System.Windows.Point(strideX, strideY);
                    }
                }
            }
            return null;
        }

 
    }
}
