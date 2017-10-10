# OpenH264Lib.NET
OpenH264 wrapper library for .NET Framework.  
This library is made by C++/CLI language to bridge other .NET Framework language like C#.  
This library is encode only.(not support decoding H264 frame.)

# How to use
```C#
// create encoder
var encoder = new OpenH264Lib.OpenH264Encoder();

// setup encoder
float fps = 10.0f;
encoder.Setup(640, 480, fps, (data, length, keyFrame) =>
{
    // called when each frame encoded.
    Console.WriteLine("Encord {0} bytes, KeyFrame:{1}", length, keyFrame);
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
