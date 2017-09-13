using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices; // for StructLayout

namespace OpenH264Sample
{
    /// <summary>
    /// MotionJPEG形式のAVI動画を作成するクラス
    /// </summary>
    public class AviWriter
    {
        private event Action<byte[], bool> OnAddImage;
        public void AddImage(byte[] data, bool keyFrame) { OnAddImage(data, keyFrame); }

        private event Action OnClose;
        public void Close() { OnClose(); }

        public AviWriter(System.IO.Stream outputAvi, string fourCC, int width, int height, float fps)
        {
            // RIFFファイルは、RIFFヘッダーとその後ろに続く 0個以上のリストとチャンクで構成されている。
            // RIFFヘッダーは、'RIFF'のFOURCC、4バイトのデータサイズ、データを識別するFOURCC、データから構成されている。
            // リストは、'LIST'のFOURCC、4バイトのデータサイズ、データを識別するFOURCC、データから構成されている。
            // チャンクは、データを識別するFOURCC、4バイトのデータサイズ、データから構成されている。
            // チャンクデータを識別するFOURCCは、2桁のストリーム番号とその後に続く2文字コード(dc=ビデオ，wb=音声，tx=字幕など)で構成されている。
            // AVIファイルは、'AVI 'のFOURCCと、2つの必須のLISTチャンク('hdrl''movi')、オプションのインデックスチャンクから構成されるRIFFファイルである。

            var riffFile = new RiffFile(outputAvi, "AVI ");

            // hdrlリストを仮のフレーム数で作成
            var hdrlList = riffFile.CreateList("hdrl");
            WriteHdrlList(hdrlList, fourCC, width, height, fps, 0);
            hdrlList.Close();

            // moviリストを作成し、OnAddImageごとにデータチャンクを追加
            var idx1List = new List<Idx1Entry>();
            var moviList = riffFile.CreateList("movi");
            this.OnAddImage += (data, keyFrame) =>
            {
                var idx1 = WriteMoviList(moviList, "00dc", data);
                idx1.KeyFrame = keyFrame;
                idx1List.Add(idx1);
            };

            // ファイルをクローズ
            this.OnClose += () =>
            {
                // moviリストを閉じる
                moviList.Close();

                // idx1チャンクを作成
                WriteIdx1Chunk(riffFile, idx1List);

                // hdrlListを正しいフレーム数で上書き
                var offset = hdrlList.Offset;
                riffFile.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin); // hdrlリストの先頭まで戻る
                riffFile.BaseStream.Seek(12, System.IO.SeekOrigin.Current);   // hdrlリストのヘッダ分飛ばす
                WriteHdrlList(riffFile, fourCC, width, height, fps, idx1List.Count);  // hdrlリストのデータを正しいフレーム数で上書き
                riffFile.BaseStream.Seek(0, System.IO.SeekOrigin.End);        // 元の場所に戻る

                // ファイルをクローズ
                riffFile.Close();
                outputAvi.Dispose();
            };
        }

        private void WriteHdrlList(RiffList hdrlList, string fourCC, int width, int height, float fps, int frames)
        {
            int streams = 1; // ストリーム数。音声なしの場合1。ありの場合2。

            // LISTチャンク'hdrl'を追加

            // 'hdrl' リストは AVI メイン ヘッダーで始まり、このメイン ヘッダーは 'avih' チャンクに含まれている。
            // メイン ヘッダーには、ファイル内のストリーム数、AVI シーケンスの幅と高さなど、AVI ファイル全体に関するグローバル情報が含まれる。
            // メイン ヘッダー チャンクは、AVIMAINHEADER 構造体で構成されている。
            {
                var chunk = hdrlList.CreateChunk("avih");
                var avih = new AVIMAINHEADER();
                avih.dwMicroSecPerFrame = (uint)(1 / fps * 1000 * 1000);
                avih.dwMaxBytesPerSec = 25000; // ffmpegと同じ値に
                avih.dwFlags = 0x0910;         // ffmpegと同じ値に
                avih.dwTotalFrames = (uint)frames;
                avih.dwStreams = (uint)streams;
                avih.dwSuggestedBufferSize = 0x100000;
                avih.dwWidth = (uint)width;
                avih.dwHeight = (uint)height;

                var data = StructureToBytes(avih);
                chunk.Write(data);
                chunk.Close();
            }

            // メイン ヘッダーの次には、1 つ以上の 'strl' リストが続く。'strl' リストは各データ ストリームごとに必要である。
            // 各 'strl' リストには、ファイル内の単一のストリームに関する情報が含まれ、ストリーム ヘッダー チャンク ('strh') とストリーム フォーマット チャンク ('strf') が必ず含まれる。
            // ストリーム ヘッダー チャンク ('strh') は、AVISTREAMHEADER 構造体で構成されている。
            // ストリーム フォーマット チャンク ('strf') は、ストリーム ヘッダー チャンクの後に続けて記述する必要がある。
            // ストリーム フォーマット チャンクは、ストリーム内のデータのフォーマットを記述する。このチャンクに含まれるデータは、ストリーム タイプによって異なる。
            // ビデオ ストリームの場合、この情報は必要に応じてパレット情報を含む BITMAPINFO 構造体である。オーディオ ストリームの場合、この情報は WAVEFORMATEX 構造体である。

            // Videoｽﾄﾘｰﾑ用の'strl'チャンク
            var strl_list = hdrlList.CreateList("strl");
            {
                var chunk = strl_list.CreateChunk("strh");
                var strh = new AVISTREAMHEADER();
                strh.fccType = ToFourCC("vids");
                strh.fccHandler = ToFourCC(fourCC);
                strh.dwScale = 1000 * 1000; // fps = dwRate / dwScale。秒間30フレームであることをあらわすのにdwScale=33333、dwRate=1000000という場合もあればdwScale=1、dwRate=30という場合もあります
                strh.dwRate = (int)(fps * strh.dwScale);
                strh.dwLength = frames;
                strh.dwSuggestedBufferSize = 0x100000;
                strh.dwQuality = -1;

                var data = StructureToBytes(strh);
                chunk.Write(data);
                chunk.Close();
            }
            {
                var chunk = strl_list.CreateChunk("strf");
                var strf = new BITMAPINFOHEADER();
                strf.biWidth = width;
                strf.biHeight = height;
                strf.biBitCount = 24;
                strf.biSizeImage = strf.biHeight * ((3 * strf.biWidth + 3) / 4) * 4; // らしい
                strf.biCompression = ToFourCC(fourCC);
                strf.biSize = System.Runtime.InteropServices.Marshal.SizeOf(strf);
                strf.biPlanes = 1;

                var data = StructureToBytes(strf);
                chunk.Write(data);
                chunk.Close();
            }
            strl_list.Close();
        }

        private class Idx1Entry
        {
            public string ChunkId { get; private set; }
            public int Length { get; private set; }
            public bool Padding { get; private set; }
            public bool KeyFrame { get; set; }

            public Idx1Entry(string chunkId, int length, bool padding)
            {
                this.ChunkId = chunkId;
                this.Length = length;
                this.Padding = padding;
            }
        }

        // たとえば、ストリーム 0 にオーディオが含まれる場合、そのストリームのデータ チャンクは FOURCC '00wb' を持つ。
        // ストリーム 1 にビデオが含まれる場合、そのストリームのデータ チャンクは FOURCC '01db' または '01dc' を持つ。
        private static Idx1Entry WriteMoviList(RiffList moviList, string chunkId, byte[] data)
        {
            var chunk = moviList.CreateChunk(chunkId);
            chunk.Write(data);

            // データはワード境界に配置しなければならない
            // バイト数が奇数の場合は、1バイトのダミーデータを書き込んでワード境界にあわせる
            int length = data.Length;
            bool padding = false;
            if (length % 2 != 0)
            {
                chunk.WriteByte(0x00); // 1バイトのダミーを書いてワード境界にあわせる
                padding = true;
            }

            chunk.Close();

            return new Idx1Entry(chunkId, length, padding);
        }

        // インデックスには、データ チャンクのリストとファイル内でのその位置が含まれている。
        // インデックスは、AVIOLDINDEX 構造体で構成され、各データ チャンクのエントリが含まれている。
        // ファイルにインデックスが含まれる場合、AVIMAINHEADER 構造体の dwFlags メンバにある AVIF_HASINDEX フラグを設定する。
        private static void WriteIdx1Chunk(RiffFile riff, List<Idx1Entry> IndexList)
        {
            const int AVIIF_KEYFRAME = 0x00000010; // 前後のフレームの情報なしにこのフレームの完全な情報を含んでいる
            var chunk = riff.CreateChunk("idx1");

            int offset = 4;
            foreach (var item in IndexList)
            {
                int length = item.Length;

                chunk.Write(ToFourCC(item.ChunkId));
                chunk.Write(item.KeyFrame? AVIIF_KEYFRAME: 0x00);
                chunk.Write(offset);
                chunk.Write(length);

                offset += 8 + length; // 8は多分00dcとﾃﾞｰﾀｻｲｽﾞ
                if (item.Padding) offset += 1;
            }

            chunk.Close();
        }

        private static int ToFourCC(string fourCC)
        {
            if (fourCC.Length != 4) throw new ArgumentException("must be 4 characters long.", "fourCC");
            return ((int)fourCC[3]) << 24 | ((int)fourCC[2]) << 16 | ((int)fourCC[1]) << 8 | ((int)fourCC[0]);
        }

        #region "Struncture Marshalling"

        private static byte[] StructureToBytes<T>(T st) where T : struct
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf(st);
            IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            System.Runtime.InteropServices.Marshal.StructureToPtr(st, ptr, false);

            byte[] data = new byte[size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, data, 0, size);

            System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            return data;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AVIMAINHEADER
        {
            public UInt32 dwMicroSecPerFrame;  // only used with AVICOMRPESSF_KEYFRAMES
            public UInt32 dwMaxBytesPerSec;
            public UInt32 dwPaddingGranularity; // only used with AVICOMPRESSF_DATARATE
            public UInt32 dwFlags;
            public UInt32 dwTotalFrames;
            public UInt32 dwInitialFrames;
            public UInt32 dwStreams;
            public UInt32 dwSuggestedBufferSize;
            public UInt32 dwWidth;
            public UInt32 dwHeight;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt32[] dwReserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RECT
        {
            public Int16 left;
            public Int16 top;
            public Int16 right;
            public Int16 bottom;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AVISTREAMHEADER
        {
            public Int32 fccType;
            public Int32 fccHandler;
            public Int32 dwFlags;
            public Int16 wPriority;
            public Int16 wLanguage;
            public Int32 dwInitialFrames;
            public Int32 dwScale;
            public Int32 dwRate;
            public Int32 dwStart;
            public Int32 dwLength;
            public Int32 dwSuggestedBufferSize;
            public Int32 dwQuality;
            public Int32 dwSampleSize;
            public RECT rcFrame;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BITMAPINFOHEADER
        {
            public Int32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public Int16 biPlanes;
            public Int16 biBitCount;
            public Int32 biCompression;
            public Int32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public Int32 biClrUsed;
            public Int32 biClrImportant;
        }

        #endregion
    }
}
