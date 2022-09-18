using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;

using AForge.Video;
using AForge.Video.DirectShow;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RemoteController;

namespace DetectCursor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private WriteableBitmap wb;

        private async Task CaptureAsync()
        {
            int deviceIndex = 1;
            var _flame = new Mat();
            using (var capture = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW))
            {
                

                (var maxWidth, var maxHeight) = getMaxCaptureSize(deviceIndex);
                capture.Set(VideoCaptureProperties.FrameWidth, maxWidth);
                capture.Set(VideoCaptureProperties.FrameHeight, maxHeight);
                // capture.Open(deviceIndex);

                // キャプチャした画像のコピー先となるWriteableBitmapを作成
                wb = new WriteableBitmap(capture.FrameWidth, capture.FrameHeight, 96, 96, PixelFormats.Bgr24, null);

                await QueryFrameAsync(capture, _flame);

                // Checker.Execute(capture);
                // H min 5   max 13
                // S min 188 max 255
                // V min 180 max 255

                // dp 2 minDist 100
                // param1 100 param2 25
                // minRadius 54 maxRadius 62

                var scalar_min = new Scalar(5, 188, 180);
                var scalar_max = new Scalar(13, 255, 255);
                var maskedMat = new Mat();
                var hsvMat = new Mat();
                using (var m5stackSp = MouseTask.getM5StackSerialPort())
                {
                    m5stackSp.BaudRate = 115200;
                    m5stackSp.Open();

                    var mouse = new MouseTask(m5stackSp);
                    if (mouse != null)
                    {
                        while (true)
                        {
                            if (!capture.IsOpened()) return;
                            await QueryFrameAsync(capture, _flame);
                            if (_flame.Empty()) return;

                            statusText.Text = "";
                            Cv2.CvtColor(_flame, hsvMat, ColorConversionCodes.BGR2HSV, 3);
                            Cv2.MorphologyEx(hsvMat, hsvMat, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)));
                            Cv2.InRange(hsvMat, scalar_min, scalar_max, maskedMat);

                            var circles = Cv2.HoughCircles(maskedMat, HoughModes.Gradient, 2, 100, 100, 25, 54, 62);
                            foreach (var seg in circles) Cv2.Circle(_flame, (int)seg.Center.X, (int)seg.Center.Y, (int)seg.Radius, new Scalar(0, 255, 0), 5);
                            WriteableBitmapConverter.ToWriteableBitmap(_flame, wb);
                            imgResult.Source = wb;

                            if (circles.Length !=0)
                            {
                                (var curPosX, var curPosY) = circles.Select(c => (c.Center.X, c.Center.Y)).FirstOrDefault();
                                statusText.Text = $"{curPosX} : {curPosY}";
                                var pos = new MousePos { buttons = 0, pressing = 0 };
                                if (curPosX < 950) pos.x = 5;
                                else if(curPosX > 950) pos.x = -5;

                                if(curPosY < 530) pos.y = 5;
                                else if( curPosY > 530) pos.y = -5;

                                var res = await mouse.Move(pos);
                                statusText.Text = $"{curPosX}/{curPosY} : {pos.x} : {pos.y}";
                            }
                        }

                    }
                    m5stackSp.Close();
                }
                
            }
        }

        private async Task<bool> QueryFrameAsync(VideoCapture capture, Mat mat)
        {
            // awaitできる形で、非同期にフレームの取得を行います。
            return await Task.Run(() =>
            {
                return capture.Read(mat);
            });
        }

        private async void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            await CaptureAsync();
        }

        private (int, int) getMaxCaptureSize(int index)
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var videoDeviceList = new List<FilterInfo>();
            string videoDeviceName; 
            foreach (FilterInfo dev in videoDevices) videoDeviceList.Add(dev);
            
            if (videoDeviceList.Count < index) return (0, 0);
            else  videoDeviceName = videoDeviceList[index].MonikerString; //VideoCaptureDevice

            var videoDevice = new VideoCaptureDevice(videoDeviceName);
            var capabilities = videoDevice.VideoCapabilities.ToList();

            var maxSizeCapability = capabilities.OrderByDescending(c => c.FrameSize.Width * c.FrameSize.Height).FirstOrDefault();
            
            if (maxSizeCapability is null) return (0, 0);
            else return (maxSizeCapability.FrameSize.Width, maxSizeCapability.FrameSize.Height);
        }
    }
}
