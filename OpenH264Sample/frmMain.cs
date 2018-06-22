using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            MessageBox.Show("Select images to encord H264 AVI.\nImage must be same width & height.");

            var dialog = new OpenFileDialog() { Multiselect = true };
            dialog.Filter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                H264Encode(dialog.FileNames, (float)nudFps.Value);
            }
        }

        private void H264Encode(string[] paths, float fps)
        {
            var firstFrame = new Bitmap(paths[0]);

            // AVIに出力するライターを作成
            var aviPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\test.avi";
            var aviFile = System.IO.File.OpenWrite(aviPath);
            var writer = new H264Writer(aviFile, firstFrame.Width, firstFrame.Height, fps);

            // H264エンコーダーを作成
            var encoder = new OpenH264Lib.Encoder("openh264-1.7.0-win32.dll");
            var decoder = new OpenH264Lib.Decoder("openh264-1.7.0-win32.dll");

            // 1フレームエンコードするごとにライターに書き込み
            OpenH264Lib.Encoder.OnEncodeCallback onEncode = (data, length, frameType) =>
            {
                var keyFrame = (frameType == OpenH264Lib.Encoder.FrameType.IDR) || (frameType == OpenH264Lib.Encoder.FrameType.I);
                writer.AddImage(data, keyFrame);
                Console.WriteLine("Encord {0} bytes, KeyFrame:{1}", length, keyFrame);

                // エンコードしたデータをでコードして、もとの画像に戻す。
                var bmp = decoder.Decode(data, length);
                if (bmp == null) return;
                pbxScreen.Image = bmp;
            };

            // H264エンコーダーの設定
            encoder.Setup(firstFrame.Width, firstFrame.Height, 5000000, fps, 2.0f, onEncode);
            decoder.Setup();

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
    }
}
