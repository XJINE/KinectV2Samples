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

namespace Wpf_KinectV2_SimpleBodyIndex
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
        /// 取得する深度フレームの詳細情報。
        /// </summary>
        FrameDescription depthFrameDescription;

        /// <summary>
        /// 取得する BodyIndex フレームの詳細情報。
        /// </summary>
        FrameDescription bodyIndexFrameDescription;

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

            this.depthFrameDescription
                = this.kinect.DepthFrameSource.FrameDescription;
            this.bodyIndexFrameDescription
                = this.kinect.BodyIndexFrameSource.FrameDescription;

            this.multiSourceFrameReader
                = this.kinect.OpenMultiSourceFrameReader
                  (FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);

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

            DepthFrame depthFrame = frames.DepthFrameReference.AcquireFrame();

            if (depthFrame == null)
            {
                return;
            }

            BodyIndexFrame bodyIndexFrame = frames.BodyIndexFrameReference.AcquireFrame();

            if (bodyIndexFrame == null)
            {
                depthFrame.Dispose();
                return;
            }

            this.canvas.Background = new ImageBrush
                (GetBitmapSource(depthFrame, bodyIndexFrame,
                this.depthFrameDescription, this.bodyIndexFrameDescription));

            depthFrame.Dispose();
            bodyIndexFrame.Dispose();
        }

        /// <summary>
        /// 深度データを可視化した BitmapSource を取得します。
        /// </summary>
        /// <param name="depthFrame">
        /// 深度フレーム。
        /// </param>
        /// <param name="bodyIndexFrame">
        /// BodyIndex フレーム。
        /// </param>
        /// <param name="frameDescription">
        /// 深度フレームの情報。
        /// </param>
        /// <param name="bodyIndexFrameDescription">
        /// BodyIndex フレームの情報。
        /// </param>
        /// <returns>
        /// 深度を可視化し、プレイヤーの画素を識別した BitmapSource。
        /// </returns>
        BitmapSource GetBitmapSource(DepthFrame depthFrame,
                                     BodyIndexFrame bodyIndexFrame,
                                     FrameDescription depthFrameDescription,
                                     FrameDescription bodyIndexFrameDescription)
        {
            ushort[] depths = new ushort[depthFrameDescription.Width
                                         * depthFrameDescription.Height];
            depthFrame.CopyFrameDataToArray(depths);

            byte[] bodyIndexes = new byte[bodyIndexFrameDescription.Width
                                          * bodyIndexFrameDescription.Height];
            bodyIndexFrame.CopyFrameDataToArray(bodyIndexes);

            byte[] depthColors = new byte[depthFrameDescription.Width
                                          * depthFrameDescription.Height
                                          * 4];

            for (int i = 0; i < depths.Length; i += 1)
            {
                ushort depth = depths[i];

                //対象の画素に人が映し出されるとき 0~5 となり、
                //それ以外の場合には 255 となる。
                byte bodyIndex = bodyIndexes[i];

                int depthColorsIndex = i * 4;

                if (bodyIndex == 255)
                {
                    byte grayColor = (byte)(depth % 255);

                    depthColors[depthColorsIndex] = grayColor;//B
                    depthColors[depthColorsIndex + 1] = grayColor;//G
                    depthColors[depthColorsIndex + 2] = grayColor;//R
                    depthColors[depthColorsIndex + 3] = 255;//A
                }
                else
                {
                    depthColors[depthColorsIndex] = 255;//B
                    depthColors[depthColorsIndex + 1] = 0;//G
                    depthColors[depthColorsIndex + 2] = 0;//R
                    depthColors[depthColorsIndex + 3] = 255;//A                
                }
                //else
                //{
                //    //BodyIndex = 0-5, or 255
                //    //Pattern
                //    // 255, 0, 0    : 1
                //    // 0, 255, 0    : 2
                //    // 0, 0, 255    : 3
                //    // 255, 255, 0  : 4
                //    // 255, 0, 255, : 5
                //    // 0, 255, 255  : 6

                //    byte bValue = 0;
                //    byte gValue = 0;
                //    byte rValue = 0;

                //    if (bodyIndex != 255)
                //    {
                //        bodyIndex += 1;
                //        alpha = 255;

                //        //1, 4, 5 (!= 2, 3, 6)
                //        if (bodyIndex % 4 <= 1)
                //        {
                //            bValue = 255;
                //        }

                //        //2, 4, 6
                //        if (bodyIndex % 2 == 0)
                //        {
                //            gValue = 255;
                //        }

                //        //3, 5, 6 (!= 1, 2, 4)
                //        if (4 % bodyIndex != 0)
                //        {
                //            rValue = 255;
                //        }
                //    }

                //    //BGRA or RGBA
                //    depthColors[depthColorsIndex] = bValue;
                //    depthColors[depthColorsIndex + 1] = gValue;
                //    depthColors[depthColorsIndex + 2] = rValue;
                //    depthColors[depthColorsIndex + 3] = 255;
                //}
            }

            BitmapSource bitmapSource
                = BitmapSource.Create(depthFrameDescription.Width,
                                      depthFrameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      depthColors,
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
