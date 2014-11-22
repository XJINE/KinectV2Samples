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

namespace Wpf_KinectV2_SimpleColorImage
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
        /// 取得するカラー画像のフォーマット。
        /// </summary>
        ColorImageFormat colorImageFormat;

        /// <summary>
        /// カラー画像を継続的に読み込むためのリーダ。
        /// </summary>
        ColorFrameReader colorFrameReader;

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //Kinect 本体への参照を確保する。
            this.kinect = KinectSensor.GetDefault();

            //読み込む画像のフォーマットとリーダを設定する。
            this.colorImageFormat = ColorImageFormat.Bgra;
            this.colorFrameDescription
                = this.kinect.ColorFrameSource.CreateFrameDescription(this.colorImageFormat);
            this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;

            //Kinect の動作を開始する。
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
            //通知されたフレームを取得する。
            ColorFrame colorFrame = e.FrameReference.AcquireFrame();

            //フレームが上手く取得できない場合がある。
            if (colorFrame == null)
            {
                return;
            }

            //画素情報を確保する領域(バッファ)を用意する。
            //"高さ * 幅 * 画素あたりのデータ量"だけ保存できれば良い。
            byte[] colors = new byte[this.colorFrameDescription.Width
                                     * this.colorFrameDescription.Height
                                     * this.colorFrameDescription.BytesPerPixel];

            //用意した領域に画素情報を複製する。
            colorFrame.CopyConvertedFrameDataToArray(colors, this.colorImageFormat);

            //画素情報をビットマップとして扱う。
            BitmapSource bitmapSource
                = BitmapSource.Create(this.colorFrameDescription.Width,
                                      this.colorFrameDescription.Height,
                                      96,
                                      96,
                                      PixelFormats.Bgra32,
                                      null,
                                      colors,
                                      this.colorFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);

            //キャンバスに表示する。
            this.canvas.Background = new ImageBrush(bitmapSource);

            //取得したフレームを破棄する。
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

            //カラー画像の取得を中止して、関連するリソースを破棄する。
            if (this.colorFrameReader != null) 
            {
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
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