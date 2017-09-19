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
            MessageBox.Show("Select images to encord H264 AVI.");

            var dialog = new OpenFileDialog() { Multiselect = true };
            dialog.Filter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                H264Encode(dialog.FileNames, (int)nudFps.Value);
            }
        }

        private delegate void OnEncodeCallbackDelegate(System.IntPtr data, int length, bool keyFrame);
        private OnEncodeCallbackDelegate onEncode = null; // GC回収されないようにメンバ変数にする(ローカル変数NG)

        private void H264Encode(string[] paths, float fps)
        {
            var firstFrame = new Bitmap(paths[0]);

            // AVIに出力するライターを作成
            var aviPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\test.avi";
            var aviFile = System.IO.File.OpenWrite(aviPath);
            var writer = new H264Writer(aviFile, firstFrame.Width, firstFrame.Height, fps);

            // H264エンコーダーを作成
            var encoder = new OpenH264Lib.OpenH264Encoder();

            // 1フレームエンコードするごとにライターに書き込み
            onEncode = (data, length, keyFrame) =>
            {
                var data_bytes = new byte[length];
                System.Runtime.InteropServices.Marshal.Copy(data, data_bytes, 0, length);
                writer.AddImage(data_bytes, keyFrame);
                Console.WriteLine("Encord {0} bytes, KeyFrame:{1}", length, keyFrame);
            };

            // H264エンコーダーの設定
            unsafe
            {
                var onEncodeFunc = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(onEncode);
                encoder.Setup(firstFrame.Width, firstFrame.Height, fps, onEncodeFunc.ToPointer());
            }

            // 1フレームごとにエンコード実施
            for (int i = 0; i < paths.Length; i++)
            {
                var bmp = new Bitmap(paths[i]);
                //var rgba = BitmapToRGBA(bmp);
                //var yuv420 = RGBAtoYUV420Planar(rgba, bmp.Width, bmp.Height);
                //encoder.Encode(yuv420, i);
                encoder.Encode(bmp, i);
            }

            writer.Close();

            MessageBox.Show(string.Format("{0}\n is created.", aviPath));
        }
    }
}
