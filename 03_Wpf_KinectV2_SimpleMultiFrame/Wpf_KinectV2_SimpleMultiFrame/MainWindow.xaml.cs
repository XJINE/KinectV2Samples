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

namespace Wpf_KinectV2_SimpleMultiFrame
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Kinect 本体への参照。
        /// </summary>
        KinectSensor kinect;

        /// <summary>
        /// 取得するカラー画像の詳細情報。
        /// </summary>
        FrameDescription colorFrameDescription;

        /// <summary>
        /// 取得する深度フレームの詳細情報。
        /// </summary>
        FrameDescription depthFrameDescription;

        /// <summary>
        /// 複数のフレームを同時に読み込むためのリーダ。
        /// </summary>
        MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.kinect = KinectSensor.GetDefault();

            //読み込むデータのフォーマットを設定する。
            this.colorFrameDescription
                = this.kinect.ColorFrameSource
                .CreateFrameDescription(ColorImageFormat.Bgra);
            this.depthFrameDescription
                = this.kinect.DepthFrameSource.FrameDescription;

            //複数のデータを読み込むリーダを用意する。
            //読み込むデータの種類を指定する。
            this.multiSourceFrameReader
                = this.kinect.OpenMultiSourceFrameReader
                  (FrameSourceTypes.Color | FrameSourceTypes.Depth);

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
                //忘れないように注意する。
                colorFrame.Dispose();
                return;
            }

            this.colorCanvas.Background
                = new ImageBrush(GetBitmapSource(colorFrame, colorFrameDescription));
            this.depthCanvas.Background
                = new ImageBrush(GetBitmapSource(depthFrame, depthFrameDescription));

            colorFrame.Dispose();
            depthFrame.Dispose();

        }

        /// <summary>
        /// カラー画像データを BitmapSource にして取得します。
        /// </summary>
        /// <param name="colorFrame">
        /// カラー画像フレーム。
        /// </param>
        /// <param name="frameDescription">
        /// カラー画像フレームの情報。
        /// </param>
        /// <returns>
        /// カラー画像データの BitmapSource。
        /// </returns>
        BitmapSource GetBitmapSource(ColorFrame colorFrame, FrameDescription frameDescription)
        {
            byte[] colors = new byte[frameDescription.Width
                                     * frameDescription.Height
                                     * frameDescription.BytesPerPixel];

            colorFrame.CopyConvertedFrameDataToArray(colors, ColorImageFormat.Bgra);

            BitmapSource bitmapSource
                = BitmapSource.Create(frameDescription.Width,
                                      frameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      colors,
                                      frameDescription.Width * (int)frameDescription.BytesPerPixel);
            return bitmapSource;
        }

        /// <summary>
        /// 深度データを可視化した BitmapSource を取得します。
        /// </summary>
        /// <param name="depthFrame">
        /// 深度フレーム。
        /// </param>
        /// <param name="frameDescription">
        /// 深度フレームの情報。
        /// </param>
        /// <returns>
        /// 深度を可視化した BitmapSource。
        /// </returns>
        BitmapSource GetBitmapSource(DepthFrame depthFrame, FrameDescription frameDescription)
        {
            ushort[] depths = new ushort[frameDescription.Width
                                         * frameDescription.Height];
            depthFrame.CopyFrameDataToArray(depths);

            byte[] depthColors = new byte[frameDescription.Width
                                          * frameDescription.Height
                                          * 4];

            for (int i = 0; i < depths.Length; i += 1)
            {
                ushort depth = depths[i];

                byte grayColor = (byte)(depth % 255);

                int depthColorsIndex = i * 4;

                depthColors[depthColorsIndex] = grayColor;//B
                depthColors[depthColorsIndex + 1] = grayColor;//G
                depthColors[depthColorsIndex + 2] = grayColor;//R
                depthColors[depthColorsIndex + 3] = 255;//A
            }

            BitmapSource bitmapSource
                = BitmapSource.Create(frameDescription.Width,
                                      frameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      depthColors,
                                      frameDescription.Width * 4);
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