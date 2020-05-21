using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect
{
    public class KinectModifier : IDisposable
    {
        public KinectSensor sensor;
        protected KinectModifier self;

        public KinectModifier(out bool success)
        {
            Console.Write("Initializing Kinect sensor... ");
            success = false;
            var sensors = KinectSensor.KinectSensors;
            if (sensors.Count < 1) { Console.WriteLine("Failed: no sensors were detected."); }
            sensor = sensors.First();
            if (sensor.IsRunning)
            {
                Console.WriteLine("Failed: another process is using the sensor.");
                return;
            }
            sensor.Start();
            Console.WriteLine("Done!");
            success = true;
        }

        virtual public void Dispose()
        {
            if (sensor.IsRunning) { sensor.Stop(); }
        }
    }

}
