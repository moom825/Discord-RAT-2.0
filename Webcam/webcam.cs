using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;

namespace Webcam
{
    public class webcam
    {
        public static Dictionary<string, bool> ready = new Dictionary<string, bool>();
        public static Dictionary<string, Bitmap> holder = new Dictionary<string, Bitmap>();
        public static Dictionary<int, string> cameras = new Dictionary<int, string>();
        public static int selected = 1;
        public static string GetWebcams()
        {

            StringBuilder deviceInfo = new StringBuilder();
            FilterInfoCollection videoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            cameras.Clear();
            int h = 1;
            foreach (FilterInfo videoCaptureDevice in videoCaptureDevices)
            {
                if (!videoCaptureDevice.MonikerString.StartsWith("@device:sw"))
                {
                    deviceInfo.Append(h + ": " + videoCaptureDevice.Name + "\n");
                    cameras[h] = videoCaptureDevice.MonikerString;
                    h++;
                }
            }
            return deviceInfo.ToString();
        }
        public static byte[] GetImage()
        {
            string key = Path.GetRandomFileName();
            while (holder.ContainsKey(key) || ready.ContainsKey(key)) key = Path.GetRandomFileName();
            ready[key] = false;
            try
            {
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                VideoCaptureDevice videoSource = new VideoCaptureDevice(cameras[selected]);
                videoSource.NewFrame += new NewFrameEventHandler((sender, e) => video_NewFrame(sender, e, key));
                videoSource.Start();
                while (true)
                {
                    if (ready[key])
                    {
                        videoSource.SignalToStop();
                        break;
                    }
                }
            }
            catch
            {
                return new byte[] { };
            }

            Bitmap data = holder[key];
            holder.Remove(key);
            ready.Remove(key);
            MemoryStream ms = new MemoryStream();
            data.Save(ms, ImageFormat.Jpeg);
            byte[] bmpBytes = ms.ToArray();
            return bmpBytes;
        }
        static void video_NewFrame(object sender, NewFrameEventArgs eventArgs, string key)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            holder[key] = bitmap;
            ready[key] = true;

        }
        public static bool select(int num)
        {
            if (num < 1 || num > cameras.Keys.ToArray().Length) return false;
            selected = num;
            return true;
        }
        public static void init()
        {
            GetWebcams();
        }
    }
}
