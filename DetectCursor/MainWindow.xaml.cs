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
            var _flame = new Mat();
            using (var capture = new VideoCapture())
            {
                int deviceIndex = 0;
                capture.Open(deviceIndex);
                int maxWidth = 0;
                int maxHeight = 0;
                (maxWidth, maxHeight) = getMaxCaptureSize(deviceIndex);
                capture.Set(VideoCaptureProperties.FrameWidth, maxWidth);
                capture.Set(VideoCaptureProperties.FrameHeight, maxHeight);

                // キャプチャした画像のコピー先となるWriteableBitmapを作成
                wb = new WriteableBitmap(capture.FrameWidth, capture.FrameHeight, 96, 96, PixelFormats.Bgr24, null);
                while (true)
                {
                    if (!capture.IsOpened()) break;
                    await QueryFrameAsync(capture, _flame);
                    if (_flame.Empty()) break;
                    WriteableBitmapConverter.ToWriteableBitmap(_flame, wb);
                    imgResult.Source = wb;
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
