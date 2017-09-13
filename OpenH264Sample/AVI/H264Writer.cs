using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenH264Sample
{
    class H264Writer
    {
        private AviWriter aviWriter;

        public H264Writer(System.IO.Stream outputAvi, int width, int height, float fps)
        {
            aviWriter = new AviWriter(outputAvi, "H264", width, height, fps);
        }

        public void AddImage(byte[] data, bool keyFrame)
        {
            aviWriter.AddImage(data, keyFrame);
        }

        public void Close()
        {
            aviWriter.Close();
        }
    }
}
