// OpenH264Lib.h

#pragma once

using namespace System;

namespace OpenH264Lib {

	public ref class OpenH264Encoder
	{
	private:
		int num_of_frames;
		int keyframe_interval;
		int buffer_size;
		unsigned char *i420_buffer;
		ISVCEncoder* encoder;
		SSourcePicture* pic;
		SFrameBSInfo* bsi;

	private:
		~OpenH264Encoder();
		!OpenH264Encoder();

	public:
		OpenH264Encoder();
		int Setup(int width, int height, float fps, void *onEncode);
		int Encode(array<Byte> ^data, float timestamp);
		int Encode(unsigned char *data, float timestamp);

	private:
		void OnEncode(const SFrameBSInfo% info);
		typedef void(__stdcall *OnEncodeFuncPtr)(unsigned char* data, int length, bool keyFrame);
		OnEncodeFuncPtr OnEncodeFunc;

	};
}
