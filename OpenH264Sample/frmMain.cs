using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenH264Sample
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void btnEncode_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Select images to encord H264 AVI.\nImages must be same width & height.");

            var dialog = new OpenFileDialog() { Multiselect = true };
            dialog.Filter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                H264Encode(dialog.FileNames, (int)nudFps.Value);
            }
        }

        private void H264Encode(string[] paths, int fps)
        {
            var firstFrame = new Bitmap(paths[0]);

            // AVIに出力するライターを作成
            var aviPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\test.avi";
            var aviFile = System.IO.File.OpenWrite(aviPath);
            var writer = new H264Writer(aviFile, firstFrame.Width, firstFrame.Height, fps);

            // H264エンコーダーを作成
            var encoder = new OpenH264Lib.Encoder("openh264-1.7.0-win32.dll");

            // 1フレームエンコードするごとにライターに書き込み
            OpenH264Lib.Encoder.OnEncodeCallback onEncode = (data, length, frameType) =>
            {
                var keyFrame = (frameType == OpenH264Lib.Encoder.FrameType.IDR) || (frameType == OpenH264Lib.Encoder.FrameType.I);
                writer.AddImage(data, keyFrame);
                Console.WriteLine("Encord {0} bytes, KeyFrame:{1}", length, keyFrame);
            };

            // H264エンコーダーの設定
            encoder.Setup(firstFrame.Width, firstFrame.Height, 5000000, fps, 2.0f, onEncode);

            // 1フレームごとにエンコード実施
            for (int i = 0; i < paths.Length; i++)
            {
                var bmp = new Bitmap(paths[i]);
                encoder.Encode(bmp, i);
                bmp.Dispose();
            }

            writer.Close();

            MessageBox.Show(string.Format("{0}\n is created.", aviPath));
        }

        private void btnDecode_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog() { Filter = "avi|*.avi" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                H264Decode(dialog.FileName, (int)nudFps.Value);
            }
        }

        private void H264Decode(string path, int fps)
        {
            var decoder = new OpenH264Lib.Decoder("openh264-1.7.0-win32.dll");

            var aviFile = System.IO.File.OpenRead(path);
            var riff = new RiffFile(aviFile);

            var frames = riff.Chunks.OfType<RiffChunk>().Where(x => x.FourCC == "00dc");
            var enumerator = frames.GetEnumerator();
            var timer = new System.Timers.Timer(fps * 1000) { SynchronizingObject = this, AutoReset = true };
            timer.Elapsed += (s, e) =>
            {
                if (enumerator.MoveNext() == false)
                {
                    timer.Stop();
                    return;
                }

                var chunk = enumerator.Current;
                var frame = chunk.ReadToEnd();
                var image = decoder.Decode(frame, frame.Length);
                if (image == null) return;
                pbxScreen.Image = image;
            };
            timer.Start();
        }
    }
}
