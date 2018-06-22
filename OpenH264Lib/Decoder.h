// OpenH264Lib.h

#pragma once

using namespace System;

namespace OpenH264Lib {

	public ref class Decoder
	{
	private:
		ISVCDecoder* decoder;

	private:
		typedef int(__stdcall *WelsCreateDecoderFunc)(ISVCDecoder** ppDecoder);
		WelsCreateDecoderFunc CreateDecoderFunc;
		typedef void(__stdcall *WelsDestroyDecoderFunc)(ISVCDecoder* ppDecoder);
		WelsDestroyDecoderFunc DestroyDecoderFunc;

	private:
		~Decoder(); // デストラクタ
		!Decoder(); // ファイナライザ
	public:
		Decoder(String ^dllName);

	public:
		int Setup();
		System::Drawing::Bitmap^ Decode(array<Byte> ^frame, int srcLen);
		System::Drawing::Bitmap^ Decode(unsigned char *frame, int srcLen);

	private:
		static byte* YUV420PtoRGBA(byte* yplane, byte* uplane, byte* vplane, int width, int height, int stride);
	};
}
