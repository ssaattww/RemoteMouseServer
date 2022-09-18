using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectCursor
{
    internal class Checker
    {
        private static string WINDOW_NAME = "checker";

        private static int _h_min = 5;
        private static int _h_max = 13;
        private static int _s_min = 188;
        private static int _s_max = 255;
        private static int _v_min = 180;
        private static int _v_max = 255;

        private static int _param2 = 25;
        private static int _min_radius = 50;
        private static int _max_radius = 60;

        private static Mat src = new Mat();
        private static Mat hsv = new Mat();

        public static void Execute(VideoCapture capture)
        {
            if (!capture.IsOpened()) return;
            capture.Read(src);

            if (src is null)
                return;
            Cv2.ImShow("src", src);

            Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV, 3);
            Cv2.ImShow("hsv", hsv);

            //名前つきウィンドウを作成
            Cv2.NamedWindow(WINDOW_NAME);

            //ウィンドウ名を指定してスライダーを配置
            Cv2.CreateTrackbar("H_Min", WINDOW_NAME, count:179, onChange: H_Min_Changed, value: ref _h_min);
            Cv2.CreateTrackbar("H_Max", WINDOW_NAME, count:179, onChange: H_Max_Changed, value: ref _h_max);
            Cv2.CreateTrackbar("S_Min", WINDOW_NAME, count:255, onChange: S_Min_Changed, value: ref _s_min);
            Cv2.CreateTrackbar("S_Max", WINDOW_NAME, count:255, onChange: S_Max_Changed, value: ref _s_max);
            Cv2.CreateTrackbar("V_Min", WINDOW_NAME, count:255, onChange: V_Min_Changed, value: ref _v_min);
            Cv2.CreateTrackbar("V_Max", WINDOW_NAME, count:255, onChange: V_Max_Changed, value: ref _v_max);

            Cv2.CreateTrackbar("param2", WINDOW_NAME, count:255, onChange: Param2_Changed, value: ref _param2);
            Cv2.CreateTrackbar("minRadius", WINDOW_NAME, count:255, onChange: MinRadius_Changed, value: ref _min_radius);
            Cv2.CreateTrackbar("maxRadius", WINDOW_NAME, count:255, onChange: MaxRadius_Changed, value: ref _max_radius);
            
            //初期画像を表示
            Cv2.ImShow(WINDOW_NAME, src);

            while (Cv2.WaitKey(1) == -1)
            {
                if (!capture.IsOpened()) return;
                capture.Read(src);
                if (src is null) return;
                Cv2.ImShow(WINDOW_NAME, src);
                Update();
            }

        }

        private static void V_Max_Changed(int pos, IntPtr userData)
        {
            _v_max = pos;
            Update();
        }

        private static void V_Min_Changed(int pos, IntPtr userData)
        {
            _v_min = pos;
            Update();
        }

        private static void S_Max_Changed(int pos, IntPtr userData)
        {
            _s_max = pos;
            Update();
        }

        private static void S_Min_Changed(int pos, IntPtr userData)
        {
            _s_min = pos;
            Update();
        }

        private static void H_Max_Changed(int pos, IntPtr userData)
        {
            _h_max = pos;
            Update();
        }

        private static void H_Min_Changed(int pos, IntPtr userData)
        {
            _h_min = pos;
            Console.WriteLine(_h_min);
            Update();
        }

        private static void Param2_Changed(int pos, IntPtr userData)
        {
            _param2 = pos;
            Update();
        }

        private static void MinRadius_Changed(int pos, IntPtr userData) 
        {
            _min_radius = pos;
            Update();
        }

        private static void MaxRadius_Changed(int pos, IntPtr userData)
        {
            _max_radius = pos;
            Update();
        }

        private static void Update()
        {
            //HSV画像とスライダーの値からマスクを生成
            var scalar_min = new Scalar(_h_min, _s_min, _v_min);
            var scalar_max = new Scalar(_h_max, _s_max, _v_max);
            
            Mat mask = new Mat();
            
            Mat closedHsv = new Mat();
            Mat clesedMask = new Mat();
            Mat clesedMask2 = new Mat();

            var cannyHsv = new Mat();
            var cannyMask = new Mat();

            Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV, 3);

            Cv2.MedianBlur(hsv, hsv, 3);
            Cv2.MorphologyEx(hsv, closedHsv, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)));
            //Cv2.MorphologyEx(closedHsv, closedTwiceHsv, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)));

            Cv2.InRange(hsv, scalar_min, scalar_max, mask);
            Cv2.InRange(closedHsv, scalar_min, scalar_max, clesedMask);
            //Cv2.InRange(cannyHsv, scalar_min, scalar_max, cannyMask);

            //Cv2.MorphologyEx(clesedMask, cannyMask, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2)));
            //Cv2.Canny(clesedMask, cannyMask, 5, 10);
            //マスク画像を使って元画像にフィルタをかける
            Mat dst = new Mat();
            Mat cannyDst = new Mat();

            src.CopyTo(dst, mask);
            //src.CopyTo(cannyDst, cannyHsv);
            // Cv2.MorphologyEx(mask, opened, MorphTypes.Open, Cv2.GetGaussianKernel(10,-1));
            //Cv2.MorphologyEx(mask, closed, MorphTypes.Close, Cv2.GetGaussianKernel(20, -1));
            //Cv2.BilateralFilter(mask, bilateraled, 10, 50, 50);
            
            

            UpdateHough(clesedMask, dst);
            //UpdateHough(cannyHsv, cannyDst);

            //ウィンドウの画像を更新
            Cv2.ImShow(WINDOW_NAME, dst);

            // Cv2.ImShow("cannyMask", cannyMask);
            //Cv2.ImShow("canny", cannyDst);
            Cv2.ImShow("closed", clesedMask);
            Cv2.ImShow("closed", dst);
            //Cv2.ImShow("mask", mask);


        }

        private static void UpdateHough(Mat mask, Mat outPut)
        {
            var segs = Cv2.HoughCircles(mask, HoughModes.Gradient, 2, 100, 100, _param2, _min_radius, _max_radius);
            foreach (var seg in segs) Cv2.Circle(outPut, (int)seg.Center.X, (int)seg.Center.Y, (int)seg.Radius, new Scalar(0, 255, 0), 5);
            
        }
    }
}
