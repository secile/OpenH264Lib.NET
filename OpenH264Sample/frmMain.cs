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
                var rgba = BitmapToRGBA(bmp);
                var yuv420 = RGBAtoYUV420Planar(rgba, bmp.Width, bmp.Height);
                encoder.Encode(yuv420, i);
            }


            writer.Close();

            MessageBox.Show(string.Format("{0}\n is created.", aviPath));
        }

        private byte[] BitmapToRGBA(Bitmap bmp)
        {
            //1ピクセルあたりのバイト数を取得する
            int pixelSize = 0;
            switch (bmp.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    pixelSize = 3;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    pixelSize = 4;
                    break;
                default:
                    throw new ArgumentException("1ピクセルあたり24または32ビットの形式のイメージのみ有効です。");
            }

            var bmpDate = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            IntPtr ptr = bmpDate.Scan0;
            byte[] pixels = new byte[bmpDate.Stride * bmp.Height];
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

            byte[] rgb_buffer = new byte[bmp.Width * bmp.Height * 4];

            int cnt = 0;
            for (int y = 0; y <= bmpDate.Height - 1; y++)
            {
                for (int x = 0; x <= bmpDate.Width - 1; x++)
                {
                    //ピクセルデータでのピクセル(x,y)の開始位置を計算する
                    int pos = y * bmpDate.Stride + x * pixelSize;

                    rgb_buffer[cnt + 0] = pixels[pos + 0]; // r
                    rgb_buffer[cnt + 1] = pixels[pos + 1]; // g
                    rgb_buffer[cnt + 2] = pixels[pos + 2]; // b
                    //rgb_buffer[cnt + 3] = 0x00; unused
                    cnt += 4;
                }
            }

            //ロックを解除する
            bmp.UnlockBits(bmpDate);

            return rgb_buffer;
        }

        // http://qiita.com/gomachan7/items/54d43693f943a0986e95
        private static byte[] RGBAtoYUV420Planar(byte[] rgba, int width, int height)
        {
            int frameSize = width * height;
            int yIndex = 0;
            int uIndex = frameSize;
            int vIndex = frameSize + (frameSize / 4);
            int r, g, b, y, u, v;
            int index = 0;
            byte[] yuv420p = new byte[width * height * 3 / 2];

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    r = rgba[index * 4 + 0] & 0xff;
                    g = rgba[index * 4 + 1] & 0xff;
                    b = rgba[index * 4 + 2] & 0xff;
                    // a = rgba[index * 4 + 3] & 0xff; unused

                    y = (int)(0.257 * r + 0.504 * g + 0.098 * b) + 16;
                    u = (int)(0.439 * r - 0.368 * g - 0.071 * b) + 128;
                    v = (int)(-0.148 * r - 0.291 * g + 0.439 * b) + 128;

                    yuv420p[yIndex++] = (byte)((y < 0) ? 0 : ((y > 255) ? 255 : y));

                    if (j % 2 == 0 && index % 2 == 0)
                    {
                        yuv420p[uIndex++] = (byte)((u < 0) ? 0 : ((u > 255) ? 255 : u));
                        yuv420p[vIndex++] = (byte)((v < 0) ? 0 : ((v > 255) ? 255 : v));
                    }

                    index++;
                }
            }

            return yuv420p;
        }
    }
}
