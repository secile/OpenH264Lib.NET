using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenH264Sample
{
    // https://msdn.microsoft.com/ja-jp/library/cc352264.aspx
    // RIFFファイルは、RIFFヘッダーとその後ろに続く0個以上のリストとチャンクで構成されている。
    // RIFFヘッダーは、チャンク階層のルートに存在する必要がある、という点をのぞいて、リストと同じである。
    // リストは、'LIST'のFOURCC、4バイトのデータサイズ、データを識別するFOURCC、データから構成されている。
    // チャンクは、データを識別するFOURCC、4バイトのデータサイズ、データから構成されている。
    class RiffFile : RiffList
    {
        public System.IO.Stream BaseStream { get; private set; }
        public RiffFile(System.IO.Stream output, string fourCC) : base(output, "RIFF", fourCC)
        {
            BaseStream = output;
        }

        public override void Close()
        {
            base.Close();
            BaseStream.Close();
        }
    }

    class RiffList : RiffBase
    {
        private System.IO.BinaryWriter Writer;
        public RiffList(System.IO.Stream output, string fourCC, string type) : base(output, fourCC)
        {
            Writer = new System.IO.BinaryWriter(output);

            Writer.Write(ToFourCC(type));
        }

        public RiffList CreateList(string fourCC)
        {
            return new RiffList(Writer.BaseStream, "LIST", fourCC);
        }

        public RiffChunk CreateChunk(string fourCC)
        {
            return new RiffChunk(Writer.BaseStream, fourCC);
        }
    }

    class RiffChunk : RiffBase
    {
        private System.IO.BinaryWriter Writer;
        public RiffChunk(System.IO.Stream output, string fourCC): base(output, fourCC)
        {
            Writer = new System.IO.BinaryWriter(output);
        }

        public void Write(byte[] data)
        {
            Writer.BaseStream.Write(data, 0, data.Length);
        }
        public void Write(int value)
        {
            Writer.Write(value);
        }
        public void WriteByte(byte value)
        {
            Writer.Write(value);
        }
    }

    class RiffBase : IDisposable
    {
        /// <summary>
        /// RiffFile先頭からのオフセット
        /// </summary>
        public long Offset { get; private set; }
        private long SizeOffset = 0;
        private long DataOffset = 0;

        public uint ChunkSize { get; private set; }

        public string FourCC { get; private set; }
        public static int ToFourCC(string fourCC)
        {
            if (fourCC.Length != 4) throw new ArgumentException("fourCCは4文字である必要があります。", "fourCC");
            return ((int)fourCC[3]) << 24 | ((int)fourCC[2]) << 16 | ((int)fourCC[1]) << 8 | ((int)fourCC[0]);
        }

        private System.IO.BinaryWriter Writer;
        public RiffBase(System.IO.Stream output, string fourCC)
        {
            this.FourCC = fourCC;
            this.Offset = output.Position;

            Writer = new System.IO.BinaryWriter(output);
            Writer.Write(RiffFile.ToFourCC(fourCC));

            this.SizeOffset = output.Position;

            uint dummy_size = 0;
            Writer.Write(dummy_size);

            this.DataOffset = output.Position;
        }

        public virtual void Close()
        {
            // sizeを正しい値に変更する
            var position = Writer.BaseStream.Position;
            ChunkSize = (uint)(position - DataOffset);
            Writer.BaseStream.Position = SizeOffset;
            Writer.Write(ChunkSize);
            Writer.BaseStream.Position = position;
        }

        #region IDisposable Support
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 管理（managed）リソースの破棄処理をここに記述します。 
                Close();
            }
            // 非管理（unmanaged）リソースの破棄処理をここに記述します。
        }

        ~RiffBase() { Dispose(false); }
        #endregion
    }
}
