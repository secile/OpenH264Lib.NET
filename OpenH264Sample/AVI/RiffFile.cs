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
    // チャンクの先頭はWord境界にアラインされる。そのためデータの長さが奇数の場合は、末尾が0でパディングされる（0を追加して偶数に合わせられる）。
    class RiffFile : RiffList
    {
        /// <summary>このファイルに含まれるチャンクを列挙する。RiffChunk型かRiffList型で取得する。</summary>
        /// <remarks>読み込み用に開いた場合のみ利用可能。</remarks>
        public IEnumerable<RiffBase> Chunks
        {
            get
            {
                var origin = BaseStream.Position; // はじめの場所を覚えておく
                var reader = new System.IO.BinaryReader(BaseStream);

                while (BaseStream.Position != BaseStream.Length)
                {
                    if (BaseStream.Length - BaseStream.Position < 4) break;   // fourCCを読めるか
                    var fourCC = ToFourCC(reader.ReadInt32());                // fourCCを覗き見
                    reader.BaseStream.Seek(-4, System.IO.SeekOrigin.Current); // fourCCぶん戻す
                    var item = (fourCC == "LIST") ? new RiffList(BaseStream) : new RiffChunk(BaseStream) as RiffBase;
                    if (item.Broken) break;

                    yield return item;

                    var chunk = item as RiffChunk;                            // chunkのみ
                    if (chunk != null) chunk.SkipToEnd();                     // データの最後に移動。
                }

                BaseStream.Position = origin; // 次回の列挙に備えはじめの場所に戻す
            }
        }

        public System.IO.Stream BaseStream { get; private set; }

        /// <summary>書き込み用に開く。</summary>
        public RiffFile(System.IO.Stream output, string fourCC) : base(output, "RIFF", fourCC)
        {
            BaseStream = output;
        }

        /// <summary>読み取り用に開く。</summary>
        public RiffFile(System.IO.Stream input) : base(input)
        {
            BaseStream = input;
        }

        public override void Close()
        {
            base.Close();
            BaseStream.Close();
        }
    }

    class RiffList : RiffBase
    {
        public string Id { get; private set; }

        private System.IO.BinaryWriter Writer;

        public RiffList(System.IO.Stream output, string fourCC, string id) : base(output, fourCC)
        {
            Writer = new System.IO.BinaryWriter(output);
            Writer.Write(ToFourCC(id));
            this.Id = id;
        }

        public RiffList(System.IO.Stream input) : base(input)
        {
            // IDが読めるか？
            if (input.Length - input.Position < 4)
            {
                Broken = true;
                return;
            }

            var reader = new System.IO.BinaryReader(input);
            this.Id = ToFourCC(reader.ReadInt32());
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
        public RiffChunk(System.IO.Stream output, string fourCC) : base(output, fourCC)
        {
            Writer = new System.IO.BinaryWriter(output);
        }

        private System.IO.BinaryReader Reader;
        public RiffChunk(System.IO.Stream input) : base(input)
        {
            Reader = new System.IO.BinaryReader(input);
        }

        public byte[] ReadBytes(int count)
        {
            return Reader.ReadBytes(count);
        }

        public byte[] ReadToEnd()
        {
            var count = ChunkSize - Reader.BaseStream.Position + DataOffset;
            var bytes = ReadBytes((int)count);
            if (count % 2 > 0) Reader.BaseStream.Seek(1, System.IO.SeekOrigin.Current);
            return bytes;
        }

        public void SkipToEnd()
        {
            var count = ChunkSize - Reader.BaseStream.Position + DataOffset;
            if (count > 0) Reader.BaseStream.Seek(count, System.IO.SeekOrigin.Current);
            if (count % 2 > 0) Reader.BaseStream.Seek(1, System.IO.SeekOrigin.Current);
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
        public long SizeOffset { get; private set; }
        public long DataOffset { get; private set; }

        public uint ChunkSize { get; private set; }

        public string FourCC { get; private set; }
        internal static int ToFourCC(string fourCC)
        {
            if (fourCC.Length != 4) throw new ArgumentException("fourCCは4文字である必要があります。", "fourCC");
            return ((int)fourCC[3]) << 24 | ((int)fourCC[2]) << 16 | ((int)fourCC[1]) << 8 | ((int)fourCC[0]);
        }
        internal static string ToFourCC(int fourCC)
        {
            var bytes = new byte[4];
            bytes[0] = (byte)(fourCC >> 0 & 0xFF);
            bytes[1] = (byte)(fourCC >> 8 & 0xFF);
            bytes[2] = (byte)(fourCC >> 16 & 0xFF);
            bytes[3] = (byte)(fourCC >> 24 & 0xFF);
            return System.Text.ASCIIEncoding.ASCII.GetString(bytes);
        }

        public RiffBase(System.IO.Stream output, string fourCC)
        {
            this.FourCC = fourCC;
            this.Offset = output.Position;

            var writer = new System.IO.BinaryWriter(output);
            writer.Write(ToFourCC(fourCC));

            this.SizeOffset = output.Position;

            uint dummy_size = 0; // sizeに0を書いておく。Close時に正しい値を書き直す。
            writer.Write(dummy_size);

            this.DataOffset = output.Position;

            // Close(Dispose)時に呼び出される処理。
            OnClose = () =>
            {
                // sizeを正しい値に変更する
                var position = writer.BaseStream.Position;
                ChunkSize = (uint)(position - DataOffset);
                writer.BaseStream.Position = SizeOffset;
                writer.Write(ChunkSize);
                writer.BaseStream.Position = position;
            };
        }

        public bool Broken { get; protected set; }
        public RiffBase(System.IO.Stream input)
        {
            var reader = new System.IO.BinaryReader(input);

            // FourCCとChunkSizeが読めるか？
            if (input.Length - input.Position < 8) { Broken = true; return; }
            // FourCCとChunkSizeを読む。
            this.Offset = input.Position;
            FourCC = ToFourCC(reader.ReadInt32());
            this.SizeOffset = input.Position;
            ChunkSize = reader.ReadUInt32();
            this.DataOffset = input.Position;

            if (input.Position + ChunkSize > input.Length) Broken = true;
        }

        private Action OnClose = () => { };
        public virtual void Close() { OnClose(); }

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
