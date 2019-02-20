# OpenH264Lib.NET
OpenH264 wrapper library for .NET Framework.  
This library is made with C++/CLI to bridge other .NET Framework language like C#.  

# How to use
```C#
// create encoder and decoder
var encoder = new OpenH264Lib.Encoder("openh264-1.7.0-win32.dll");
var decoder = new OpenH264Lib.Decoder("openh264-1.7.0-win32.dll");

// setup encoder
float fps = 10.0f;
encoder.Setup(640, 480, fps, (data, length, keyFrame) =>
{
    // called when each frame encoded.
    Console.WriteLine("Encord {0} bytes, KeyFrame:{1}", length, keyFrame);
    
    // decode it to Bitmap again...
    var bmp = decoder.Decode(data, length);
    if (bmp != null) Console.WriteLine(bmp.Size);
});

// encode frame
foreach(var bmp in bitmaps)
{
    encoder.Encode(bmp, i);
}
```

# See Example
(1) Open OpenH264Lib.sln Visual Studio solution file.  
(2) Build OpenH264Lib project. Then created OpenH264Lib.dll.  
(3) Build OpenH264Sample project. This is example C# project how to use OpenH264Lib.dll.  
(4) Download 'openh264-1.7.0-win32.dll' from Cisco's [OpenH264 Github repository](https://github.com/cisco/openh264/releases),
and copy it to OpenH264Sample/bin/Debug/ directory.  
(5) Execute OpenH264Sample.exe. This program demos encode bmp/jpg/png images you select.

# And...
By combining OpenH264Lib.NET and UsbCamera and MotionJPEGWriter, you can make H264 recorder.
```C#
int index = 0;
var camera = new UsbCamera(index, new Size(640, 480));
camera.Start();

// create H264 encorder.
var path = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\test.avi";
var encoder = new OpenH264Lib.OpenH264Encoder("openh264-1.7.0-win32.dll");

// write avi for every frame encorded.
var fps = 10.0f;
var writer = new Github.secile.Video.AviWriter(System.IO.File.OpenWrite(path), "H264", camera.Size.Width, camera.Size.Height, fps);
OpenH264Lib.OpenH264Encoder.OnEncodeCallback onEncode = (data, length, frameType) =>
{
    var keyFrame = (frameType == OpenH264Lib.OpenH264Encoder.FrameType.IDR) || (frameType == OpenH264Lib.OpenH264Encoder.FrameType.I);
    writer.AddImage(data, keyFrame);
};

// setup encorder.
int bps = 100000; // 100kbps
encoder.Setup(camera.Size.Width, camera.Size.Height, bps, fps, 10.0f, onEncode);

// encode image for every captured image.
var sw = System.Diagnostics.Stopwatch.StartNew();
var timer = new System.Timers.Timer(1000 / fps) { SynchronizingObject = this };
timer.Elapsed += (s, ev) =>
{
    var bmp = camera.GetBitmap();
    pbxImage.Image = bmp;
    encoder.Encode(bmp, sw.ElapsedMilliseconds);
};
timer.Start();

// release resource when close.
this.FormClosing += (s, ev) =>
{
    camera.Release();
    timer.Stop();
    writer.Close();
    encoder.Dispose();
};
```
