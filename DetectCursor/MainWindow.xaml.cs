using OpenCvSharp;
using OpenCvSharp.Extensions;

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
using OpenCvSharp.WpfExtensions;

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
                capture.Open(0);
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
    }
}
