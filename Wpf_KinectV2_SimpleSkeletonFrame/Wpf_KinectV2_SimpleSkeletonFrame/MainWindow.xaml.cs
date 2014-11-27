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

namespace Wpf_KinectV2_SimpleSkeletonFrame
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;

        FrameDescription colorFrameDescription;

        MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.kinect = KinectSensor.GetDefault();

            this.colorFrameDescription
                = this.kinect.ColorFrameSource
                .CreateFrameDescription(ColorImageFormat.Bgra);

            this.multiSourceFrameReader
                = this.kinect.OpenMultiSourceFrameReader
                  (FrameSourceTypes.Color  |FrameSourceTypes.Body);

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

            BodyFrame bodyFrame = frames.BodyFrameReference.AcquireFrame();

            if (bodyFrame == null)
            {
                colorFrame.Dispose();
                return;
            }

            //キャンバスをクリアして更新する。
            this.canvas.Background
                = new ImageBrush(GetBitmapSource(colorFrame, colorFrameDescription));

            this.canvas.Children.Clear();


            Body[] bodies = new Body[bodyFrame.BodyCount];

            bodyFrame.GetAndRefreshBodyData(bodies);

            foreach(Body body in bodies)
            {
                if (body.IsTracked == false)
                {
                    continue;                
                }


                //描く関節を可視化する。
                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                foreach (KeyValuePair<JointType, Joint> joint in joints)
                //foreach (var joint in joints) // こちらでも可。
                {
                    if (joint.Value.TrackingState == TrackingState.Tracked)
                    {
                        DrawJointEllipseInColorSpace(joint.Value, 10, Colors.Aqua);
                    }
                    else if (joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        DrawJointEllipseInColorSpace(joint.Value, 10, Colors.Yellow);                    
                    }
                }

                //左手の状態を可視化する。
                switch (body.HandLeftState)
                {
                    //閉じてる(グー)。
                    case HandState.Closed:
                        {
                            DrawJointEllipseInColorSpace(joints[JointType.HandLeft], 20, Colors.Blue);
                            break;
                        }
                    //チョキ(実際には精度の都合上、指一本でも反応するが)
                    case HandState.Lasso:
                        {
                            DrawJointEllipseInColorSpace(joints[JointType.HandLeft], 20, Colors.Green);
                            break;
                        }
                    //開いている(パー)。
                    case HandState.Open:
                        {
                            DrawJointEllipseInColorSpace(joints[JointType.HandLeft], 20, Colors.Red);
                            break;
                        }                
                }
            }

            colorFrame.Dispose();
            bodyFrame.Dispose();

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
        /// 指定した Joint が映し出される位置に円を描画します。
        /// </summary>
        /// <param name="joint">
        /// 描画する Joint です。
        /// </param>
        /// <param name="radius">
        /// 描画される円の半径です。
        /// </param>
        /// <param name="color">
        /// 描画される円の色です。
        /// </param>
        private void DrawJointEllipseInColorSpace(Joint joint, int radius, Color color)
        {
            //そのまま取得できるデータは 3 次元空間中の座標。
            CameraSpacePoint jointCameraPos = joint.Position;

            //カメラで映し出された平面上の座標に変換する。
            ColorSpacePoint jointColorPos
                = this.kinect.CoordinateMapper.MapCameraPointToColorSpace(jointCameraPos);

            if (float.IsInfinity(jointColorPos.X)
                || float.IsInfinity(jointColorPos.Y))
            {
                return;
            }

            //描画する円を作成する。
            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = ellipse.Width;
            ellipse.Fill = new SolidColorBrush(color);

            //座標を合わせて追加する。
            Canvas.SetLeft(ellipse,
                           jointColorPos.X * (this.canvas.ActualWidth / 1920) - radius);
            Canvas.SetTop(ellipse,
                           jointColorPos.Y * (this.canvas.ActualHeight / 1080) - radius);
            this.canvas.Children.Add(ellipse);
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
