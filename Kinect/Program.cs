using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kinect
{
    static class Program
    {
        static void SetAngle(this KinectSensor sensor, int angle)
        {
            if (!sensor.IsRunning) { return; }
            if (angle < sensor.MinElevationAngle) { angle = sensor.MinElevationAngle; }
            if (angle > sensor.MaxElevationAngle) { angle = sensor.MaxElevationAngle; }
            sensor.ElevationAngle = angle;
        }

        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                DisplayUsage();
                return;
            }

            using (KinectModifier mod = new KinectModifier(out bool success))
            {
                if (!success) { return; }

                switch (args[0])
                {
                    case "angle":
                        if (!int.TryParse(args[1], out int value))
                        {
                            Console.WriteLine("Please enter a valid integer");
                            return;
                        }
                        try
                        {
                            Console.WriteLine($"Setting sensor elevation angle to {value} from {mod.sensor.ElevationAngle}...");
                            mod.sensor.SetAngle(value);
                            Thread.Sleep(1000);
                            Console.WriteLine("Done!");
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); }
                        break;
                    case "image":
                        if (File.Exists(args[1]))
                        {
                            Console.WriteLine($"A file already exists at {args[1]}. Are you sure you want to overwrite it? (Y/N)");
                            if (Console.ReadKey(true).Key != ConsoleKey.Y) { return; }
                        }
                        var sw = Stopwatch.StartNew();
                        Task getFrame = GetFrame(mod.sensor).ContinueWith(task =>
                        {
                            task.Result.Save(args[1]);
                            sw.Stop();
                            Console.WriteLine($"Done!\nImage saved to \'{args[1]}\'.");
                            Console.WriteLine($"This operation took {sw.ElapsedMilliseconds} ms.");
                        });
                        Task.WaitAll(getFrame);
                        break;
                    default:
                        DisplayUsage();
                        break;
                }
            }
        }

        private static async Task<Bitmap> GetFrame(KinectSensor sensor)
        {
            Bitmap bitm = null;

            Console.Write("Initializing camera... ");
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
            Console.Write("Done!\nWaiting for frame... ");
            
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<ColorImageFrameReadyEventArgs> processImage = null;
            processImage = new EventHandler<ColorImageFrameReadyEventArgs>(async (sender, e) =>
            {
                sensor.ColorFrameReady -= processImage;
                Console.Write("Done!\nProcessing data... ");

                using (var frame = e.OpenColorImageFrame())
                {
                    bitm = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppPArgb);

                    byte[] unprocessed = frame.GetRawPixelData();
                    List<Task> tasks = new List<Task>();

                    //Decoding colour information
                    for (int i = 0; i < unprocessed.Length; i += 4)
                    {
                        Color color = GetColor(unprocessed, i);
                        Get2dIndex(i, frame.BytesPerPixel, frame.Width, out int x, out int y);
                        lock (bitm) { bitm.SetPixel(x, y, color); }
                    }
                }
                tcs.SetResult(true);
            });

            sensor.ColorFrameReady += processImage;
            await tcs.Task;
            sensor.ColorStream.Disable();

            return bitm;
        }

        private static void Get2dIndex(int index, int bytesPerPixel, int width, out int x, out int y)
        {
            int current = index / bytesPerPixel;
            y = current / width;
            x = current % width;
        }

        private static int ToInt(this byte Byte)
        {
            return Convert.ToInt32(Byte);
        }

        private static Color GetColor(this byte[] array, int index)
        {
            return Color.FromArgb(
                    array[index + 2].ToInt(),
                    array[index + 1].ToInt(),
                    array[index + 0].ToInt()
                    );
        }

        private static void DisplayUsage()
        {
            Console.WriteLine("The specified argument is invalid.\n\nCommands:\n" +
                "angle [target angle]: sets the elevation of the Kinect sensor to a specified value" +
                $"between 27 and -27.\n" +
                $"image [file path]: captures a bitmap image of the current frame.");
        }
    }
}
