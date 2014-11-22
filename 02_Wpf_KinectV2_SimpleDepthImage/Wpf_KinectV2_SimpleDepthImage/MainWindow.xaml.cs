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

namespace Wpf_KinectV2_SimpleDepthImage
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
        /// 深度情報を継続的に読み込むためのリーダ。
        /// </summary>
        DepthFrameReader depthFrameReader;

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.kinect = KinectSensor.GetDefault();

            this.depthFrameDescription
                = this.kinect.DepthFrameSource.FrameDescription;
            this.depthFrameReader = this.kinect.DepthFrameSource.OpenReader();
            this.depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;

            this.kinect.Open();
        }

        /// <summary>
        /// Kinect が深度情報を取得したとき実行されるメソッド(イベントハンドラ)。
        /// </summary>
        /// <param name="sender">
        /// イベントを通知したオブジェクト。ここでは Kinect になる。
        /// </param>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。ここでは深度画像の情報が含まれる。
        /// </param>
        void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            DepthFrame depthFrame = e.FrameReference.AcquireFrame();

            if (depthFrame == null)
            {
                return;
            }

            //深度情報を確保する領域(バッファ)を用意して取得する。
            ushort[] depths = new ushort[this.depthFrameDescription.Width
                                         * this.depthFrameDescription.Height];
            depthFrame.CopyFrameDataToArray(depths);

            //深度情報を画像(深度画像)として可視化するために、画像を構成する配列を用意する。
            //"高さ * 幅 * 1画素あたりのデータ量(Bgra32 なら 4byte)"
            byte[] depthColors = new byte[this.depthFrameDescription.Width
                                          * this.depthFrameDescription.Height
                                          * 4];

            //深度情報を利用して、画像の各画素の色を決定する。
            for (int i = 0; i < depths.Length; i += 1)
            {
                ushort depth = depths[i];

                byte grayColor = (byte)(depth % 255);

                //深度画像の画素を指すインデックス。
                int depthColorsIndex = i * 4;

                //BGRA の順にデータを入れる
                depthColors[depthColorsIndex] = grayColor;//B
                depthColors[depthColorsIndex + 1] = grayColor;//G
                depthColors[depthColorsIndex + 2] = grayColor;//R
                depthColors[depthColorsIndex + 3] = 255;//A
            }

            //画素情報をビットマップとして扱う。
            //正しいストライドの大きさを算出すること。
            BitmapSource bitmapSource
                = BitmapSource.Create(this.depthFrameDescription.Width,
                                      this.depthFrameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      depthColors,
                                      this.depthFrameDescription.Width * 4);

            //キャンバスに表示する。
            this.canvas.Background = new ImageBrush(bitmapSource);

            //取得したフレームを破棄する。
            depthFrame.Dispose();
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
            if (this.depthFrameReader != null)
            {
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
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