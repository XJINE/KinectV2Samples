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

namespace Wpf_KinectV2_SimpleUserMask
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;

        ColorImageFormat colorImageFormat = ColorImageFormat.Bgra;

        FrameDescription colorFrameDescription;

        FrameDescription depthFrameDescription;

        FrameDescription bodyIndexFrameDescription;

        MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.kinect = KinectSensor.GetDefault();

            this.colorFrameDescription
                = this.kinect.ColorFrameSource.CreateFrameDescription(this.colorImageFormat);
            this.depthFrameDescription
                = this.kinect.DepthFrameSource.FrameDescription;
            this.bodyIndexFrameDescription
                = this.kinect.BodyIndexFrameSource.FrameDescription;

            this.multiSourceFrameReader
                = this.kinect.OpenMultiSourceFrameReader
                  (FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);

            this.multiSourceFrameReader.MultiSourceFrameArrived
                += MultiSourceFrameReader_MultiSourceFrameArrived;

            this.kinect.Open();
        }

        /// <summary>
        /// Kinect が複数種類のフレームを取得したとき実行されるメソッド(イベントハンドラ)。
        /// </summary>
        /// <param name="sender">
        /// イベントを通知したオブジェクト。ここでは Kinect になる。
        /// </param>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。
        /// </param>
        void MultiSourceFrameReader_MultiSourceFrameArrived
            (object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame frames = this.multiSourceFrameReader.AcquireLatestFrame();

            if (frames == null)
            {
                return;
            }

            ColorFrame colorFrame = frames.ColorFrameReference.AcquireFrame();

            if (colorFrame == null)
            {
                return;
            }

            DepthFrame depthFrame = frames.DepthFrameReference.AcquireFrame();

            if (depthFrame == null)
            {
                colorFrame.Dispose();
                return;
            }

            BodyIndexFrame bodyIndexFrame = frames.BodyIndexFrameReference.AcquireFrame();

            if (bodyIndexFrame == null)
            {
                colorFrame.Dispose();
                depthFrame.Dispose();
                return;
            }

            this.colorCanvas.Background
                = new ImageBrush(GetColorImage(colorFrame));

            this.userMaskCanvas.Background = new ImageBrush
                (GetUserMaskImage(colorFrame, depthFrame, bodyIndexFrame));

            colorFrame.Dispose();
            depthFrame.Dispose();
            bodyIndexFrame.Dispose();
        }

        /// <summary>
        /// カラー画像データを BitmapSource にして取得します。
        /// </summary>
        /// <param name="colorFrame">
        /// カラー画像フレーム。
        /// </param>
        /// <returns>
        /// カラー画像データの BitmapSource。
        /// </returns>
        BitmapSource GetColorImage(ColorFrame colorFrame)
        {
            byte[] colors = new byte[this.colorFrameDescription.Width
                                     * this.colorFrameDescription.Height
                                     * this.colorFrameDescription.BytesPerPixel];

            colorFrame.CopyConvertedFrameDataToArray(colors, ColorImageFormat.Bgra);

            BitmapSource bitmapSource
                = BitmapSource.Create(this.colorFrameDescription.Width,
                                      this.colorFrameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      colors,
                                      this.colorFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);
            return bitmapSource;
        }

        /// <summary>
        /// 人が映った画素領域だけを映した BitmapSource を取得します。
        /// </summary>
        /// <param name="colorFrame">
        /// カラー画像フレーム。
        /// </param>
        /// <param name="depthFrame">
        /// 深度フレーム。
        /// </param>
        /// <param name="bodyIndexFrame">
        /// BodyIndex フレーム。
        /// </param>
        /// <returns>
        /// 人が映った画素領域だけを映した BitmapSource。
        /// </returns>
        BitmapSource GetUserMaskImage(ColorFrame colorFrame,
                                     DepthFrame depthFrame,
                                     BodyIndexFrame bodyIndexFrame)
        {
            byte[] colors = new byte[this.colorFrameDescription.Width
                                     * this.colorFrameDescription.Height
                                     * this.colorFrameDescription.BytesPerPixel];

            colorFrame.CopyConvertedFrameDataToArray(colors, this.colorImageFormat);

            ushort[] depths = new ushort[this.depthFrameDescription.Width
                                         * this.depthFrameDescription.Height];
            depthFrame.CopyFrameDataToArray(depths);

            byte[] bodyIndexes = new byte[this.bodyIndexFrameDescription.Width
                                          * this.bodyIndexFrameDescription.Height];
            bodyIndexFrame.CopyFrameDataToArray(bodyIndexes);


            //人が映っているだけの画像を表す byte 配列を用意して 0 で初期化する。
            byte[] bodyColors = new byte[this.bodyIndexFrameDescription.Width
                                         * this.bodyIndexFrameDescription.Height
                                         * this.colorFrameDescription.BytesPerPixel];
            Array.Clear(bodyColors, 0, bodyColors.Length);

            //カラー画像と深度画像の座標を対応させる(= マッピングする)必要がある。
            //ColorSpacePoint はカラー画像の座標を示す構造体。
            ColorSpacePoint[] colorSpacePoints
                = new ColorSpacePoint[this.bodyIndexFrameDescription.Width
                                      * this.bodyIndexFrameDescription.Height];
            this.kinect.CoordinateMapper.MapDepthFrameToColorSpace(depths, colorSpacePoints);

            for (int y = 0; y < this.depthFrameDescription.Height; y++)
            {
                for (int x = 0; x < this.depthFrameDescription.Width; x++)
                {
                    int depthsIndex = y * this.depthFrameDescription.Width + x;

                    if (bodyIndexes[depthsIndex] != 255)
                    {
                        //対応するカラー画像の座標を取得し、
                        //カラー画像座標系の範囲内に収まるかを判定する。
                        ColorSpacePoint colorSpacePoint = colorSpacePoints[depthsIndex];
                        int colorX = (int)colorSpacePoint.X;
                        int colorY = (int)colorSpacePoint.Y;

                        if ((colorSpacePoint.X >= 0)
                            && (colorSpacePoint.X < this.colorFrameDescription.Width)
                            && (colorSpacePoint.Y >= 0)
                            && (colorSpacePoint.Y < this.colorFrameDescription.Height))
                        {
                            //対応するカラー画像の画素から色情報を取得して、
                            //新しいカラー画像のデータに与える。
                            int colorsIndex = 
                                (colorY * this.colorFrameDescription.Width + colorX) * 4;

                            int bodyColorsIndex = depthsIndex * 4;

                            bodyColors[bodyColorsIndex] = colors[colorsIndex];
                            bodyColors[bodyColorsIndex + 1] = colors[colorsIndex + 1];
                            bodyColors[bodyColorsIndex + 2] = colors[colorsIndex + 2];
                            bodyColors[bodyColorsIndex + 3] = 255;
                        }
                    }
                }
            }

            BitmapSource bitmapSource
                = BitmapSource.Create(depthFrameDescription.Width,
                                      depthFrameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      bodyColors,
                                      depthFrameDescription.Width * 4);
            return bitmapSource;
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

            //データの取得を中止して、関連するリソースを破棄する。
            if (this.multiSourceFrameReader != null)
            {
                this.multiSourceFrameReader.Dispose();
                this.multiSourceFrameReader = null;
            }

            //Kinect を停止して、関連するリソースを破棄する。
            if (this.kinect != null)
            {
                this.kinect.Close();
                this.kinect = null;
            }
        }
    }
}
