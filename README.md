# OpenH264Lib
OpenH264 wrapper library for .NET Framework.  
This library is coded by C++/CLI language to bridge other .NET Framework language like C#.
Currently, this library is encode only.(unable to decode H264 frame.)

# How to use
(1) Open OpenH264Lib.sln Visual Studio solution file.  
(2) Build OpenH264Lib project. Then created OpenH264Lib.dll.  
(3) Build OpenH264Sample project. This is example C# project how to use OpenH264Lib.dll.  
(4) Download 'openh264-1.7.0-win32.dll' from GitHub [OpenH264 repository](https://github.com/cisco/openh264/releases),
and copy it to OpenH264Sample/bin/Debug/ directory.  
(5) Execute OpenH264Sample.exe. Select some bmp/jpg/png images to encode H264 avi format.
