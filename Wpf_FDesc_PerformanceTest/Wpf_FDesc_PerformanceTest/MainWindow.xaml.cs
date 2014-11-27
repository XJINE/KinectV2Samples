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

using Microsoft.Kinect;
using System.Diagnostics;

namespace Wpf_FDesc_PerformanceTest
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;

        FrameDescription colorFrameDescription;

        ColorImageFormat colorImageFormat;

        ColorFrameReader colorFrameReader;

        public class DummyParent
        {
            public class DummyChild
            {
                public int Value { get; set; }

                public int NewValue { get { int value = 1920; return value; } }
            }

            public DummyChild Child { get; set; }

            public DummyParent()
            {
                this.Child = new DummyChild();
            }
        }

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.kinect = KinectSensor.GetDefault();

            this.colorImageFormat = ColorImageFormat.Bgra;
            this.colorFrameDescription
                = this.kinect.ColorFrameSource.CreateFrameDescription(this.colorImageFormat);
            this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;

            this.kinect.Open();
        }

        /// <summary>
        /// Kinect がカラー画像を取得したとき実行されるメソッド(イベントハンドラ)。
        /// </summary>
        /// <param name="sender">
        /// イベントを通知したオブジェクト。ここでは Kinect になる。
        /// </param>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。ここではカラー画像の情報が含まれる。
        /// </param>
        void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            ColorFrame colorFrame = e.FrameReference.AcquireFrame();

            if (colorFrame == null)
            {
                return;
            }

            int dummyValue = 0;

            Stopwatch stopWatch = new Stopwatch();

            //=======================================================
            //colorFrame.FrameDescription.Width を 10,000 回参照する。
            //=======================================================
            stopWatch.Restart();

            for (int i = 0; i < 10000; i++)
            {
                dummyValue = colorFrame.FrameDescription.Width;
            }

            stopWatch.Stop();

            Console.WriteLine("Pattern-A : " + stopWatch.Elapsed.TotalMilliseconds);

            //=======================================================
            //予め確保した colorFrameDescription から Width を参照する。
            //=======================================================

            FrameDescription colorFrameDescription
                = colorFrame.CreateFrameDescription(ColorImageFormat.Bgra);

            stopWatch.Restart();

            for (int i = 0; i < 10000; i++)
            {
                dummyValue = colorFrameDescription.Width;
            }

            stopWatch.Stop();

            Console.WriteLine("Pattern-B : " + stopWatch.Elapsed.TotalMilliseconds);

            //=======================================================
            //予め確保した colorFrameDescription.Width を参照する。
            //=======================================================

            int width = colorFrameDescription.Width;

            stopWatch.Restart();

            for (int i = 0; i < 10000; i++)
            {
                dummyValue = width;
            }

            stopWatch.Stop();

            Console.WriteLine("Pattern-C : " + stopWatch.Elapsed.TotalMilliseconds);

            //=======================================================
            //オブジェクトの参照が等しい(≠等値)かどうか調べる。
            //=======================================================

            //FrameDescription は等価のようである。
            //Width はプロパティによる実装でアドレスが取得できない。
            //新しいインスタンスが生成されていることなどが正しく確認できない。
            //(確認する方法ありますかね?)
            Console.WriteLine(Object.Equals(colorFrame.FrameDescription,
                                            colorFrame.FrameDescription));

            //=======================================================
            //参照の類のみで発生するオーバーヘッドでないことを確認する。
            //dummyObject.Child.Value を 10,000 回参照する。
            //=======================================================

            DummyParent dummyObject = new DummyParent();

            stopWatch.Restart();

            for (int i = 0; i < 10000; i++)
            {
                dummyValue = dummyObject.Child.Value;
            }

            stopWatch.Stop();

            Console.WriteLine("Pattern-D : " + stopWatch.Elapsed.TotalMilliseconds);

            Console.WriteLine("===================================================");

            byte[] colors = new byte[this.colorFrameDescription.Width
                                     * this.colorFrameDescription.Height
                                     * this.colorFrameDescription.BytesPerPixel];

            colorFrame.CopyConvertedFrameDataToArray(colors, this.colorImageFormat);

            BitmapSource bitmapSource
                = BitmapSource.Create(this.colorFrameDescription.Width,
                                      this.colorFrameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      colors,
                                      this.colorFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);

            this.canvas.Background = new ImageBrush(bitmapSource);

            colorFrame.Dispose();
        }

        /// <summary>
        /// この WPF アプリケーションが終了するときに実行されるメソッド。
        /// </summary>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。
        /// </param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinect != null)
            {
                this.kinect.Close();
                this.kinect = null;
            }
        }
    }
}
