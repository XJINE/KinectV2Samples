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

namespace Wpf_KinectV2_SimpleAudio
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
        /// AudioBeam を継続的に読み込むためのリーダ。
        /// </summary>
        AudioBeamFrameReader audioBeamFrameReader;

        /// <summary>
        /// デシベル算出時の最小値(公式サンプルより)。
        /// </summary>
        const int MinDecibel = -90;

        /// <summary>
        /// コンストラクタ。実行時に一度だけ実行される。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.kinect = KinectSensor.GetDefault();

            //活用方法は不明です。
            this.kinect.AudioSource.FrameCaptured += AudioSource_FrameCaptured;

            //AudioBeamFrameReader を取得してイベントハンドラを設定します。
            this.audioBeamFrameReader = this.kinect.AudioSource.OpenReader();
            this.audioBeamFrameReader.FrameArrived += audioBeamFrameReader_FrameArrived;

            this.kinect.Open();
        }

        /// <summary>
        /// AudioSource.FrameCaptured のイベントハンドラです。
        /// </summary>
        /// <param name="sender">
        /// イベントを通知したオブジェクト。ここでは Kinectです。
        /// </param>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。
        /// </param>
        void AudioSource_FrameCaptured(object sender, FrameCapturedEventArgs e)
        {
            //活用方法は不明です。
        }

        /// <summary>
        /// Kinect が AudioBeam を取得したとき実行されるメソッド(イベントハンドラ)です。
        /// </summary>
        /// <param name="sender">
        /// イベントを通知したオブジェクト。ここでは Kinectです。
        /// </param>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。
        /// </param>
        void audioBeamFrameReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameList beamFrames = e.FrameReference.AcquireBeamFrames();

            if (beamFrames == null)
            {
                return;
            }
            
            IReadOnlyList<AudioBeamSubFrame> subFrames = beamFrames[0].SubFrames;

            //(公式サンプルより)
            //オーディオストリームから取得したデータのバッファです。
            //サブフレームは 16msec 間のデータを 16Khz で 取得します。
            //したがってサブフレームあたりのサンプル数は 16 * 16 = 256 です。
            //サンプル1つ辺りは 4byte 必要なので、256 * 4 = 1024 byte 確保されます。
            byte[] audioBuffer = new byte[this.kinect.AudioSource.SubFrameLengthInBytes];

            foreach (AudioBeamSubFrame subFrame in subFrames)
            {
                //音が発生している方向を取得する。
                //radian の値 -1.57~1.57 で取得される。
                //必要なら degreeの値 -90~90 に直す。
                float radianAngle = subFrame.BeamAngle;
                int degreeAngle = (int)(radianAngle * 180 / Math.PI);

                //方向検出の精度を取得する。
                float confidence = subFrame.BeamAngleConfidence;

                //発声したユーザを取得する。
                List<ulong> speakers = new List<ulong>();
                foreach (AudioBodyCorrelation audioBody in subFrame.AudioBodyCorrelations)
                {
                    speakers.Add(audioBody.BodyTrackingId);
                }

                //オーディオ情報を複製して取得する。
                //取得したデータから dB(デシベル) を算出する。
                subFrame.CopyFrameDataToArray(audioBuffer);
                float decibel = CalcDecibelWithRMS(audioBuffer);

                //UI を更新する。
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Label_BeamAngle.Content = "BeamAngle : " + degreeAngle;
                    this.Label_Confidence.Content = "Confidence : " + confidence;
                    this.Label_Decibel.Content = "dB : " + (decibel + 90);

                    string speakerIDs = "";
                    foreach (ulong speakerID in speakers)
                    {
                        speakerIDs += speakerID + ", ";
                    }
                    this.Label_Speaker.Content = "Speakers : " + speakerIDs;

                    this.Rectangle_BeamAngle.RenderTransform
                        = new RotateTransform(-1 * degreeAngle, 0.5, 0);

                    this.Rectangle_dBMeter.Width
                        = this.StackPanel_Container.ActualWidth
                          - this.StackPanel_Container.ActualWidth * (decibel / MinDecibel);
                }));

                break;
            }

            //解放しない場合、次のフレームが届かない。
            beamFrames.Dispose();
        }

        /// <summary>
        /// オーディオ情報から音量(dB)を算出して取得します。
        /// </summary>
        /// <param name="audioBuffer">
        /// オーディオ情報。
        /// </param>
        /// <returns>
        /// 音量(dB)を返します。
        /// </returns>
        private float CalcDecibelWithRMS(byte[] audioBuffer)
        {
            //累積二乗和です。
            float accumulatedSquareSum = 0;
            float decibel = 0;

            //読み込んだオーディオバッファを処理する。
            //4 byteで1組となるデータを読み込んで、累積二乗和を算出する。
            for (int i = 0; i < audioBuffer.Length; i += 4)
            {
                //サンプルの値を取得する。
                //指定した Index から 4 byte 分のデータから得られる値(float)が 1 サンプルになる。
                float audioSample = BitConverter.ToSingle(audioBuffer, i);

                //累積二乗和を算出する。
                accumulatedSquareSum += audioSample * audioSample;
            }

            //(サンプル数で割って)平均平方を求める。
            float meanSquare = accumulatedSquareSum / (audioBuffer.Length / 4);

            //二乗平均平方(RMS = √ meanSquare)を求める。
            //一般的に音処理においては入出力信号レベルを示す。
            double rms = Math.Sqrt(meanSquare);

            //デシベル(dB)を求める。
            //デシベルはパスカル(Pa)で示される音圧を、
            //分かりやすさのために常用対数(Log10)で表したもの。
            decibel = (float)(20 * Math.Log10(rms));

            //最小値までの範囲に収まるようにします。
            if (decibel < MinDecibel)
            {
                decibel = MinDecibel;
            }

            return decibel;
        }
    }
}
