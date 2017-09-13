// これは メイン DLL ファイルです。

#include "stdafx.h"

#include "OpenH264Lib.h"

namespace OpenH264Lib {

	// Reference
	// https://github.com/kazuki/video-codec.js
	// file://openh264-master\test\api\BaseEncoderTest.cpp
	// file://openh264-master\codec\console\enc\src\welsenc.cpp

	// C#からbyte[]として呼び出し可能
	int OpenH264Encoder::Encode(array<Byte> ^data, float timestamp)
	{
		// http://xptn.dtiblog.com/blog-entry-21.html
		pin_ptr<Byte> ptr = &data[0];
		int rc = Encode(ptr, timestamp);
		ptr = nullptr; // unpin
		return rc;
	}

	// C#からはunsafe&fixedを利用しないと呼び出しできない
	int OpenH264Encoder::Encode(unsigned char *data, float timestamp)
	{
		memcpy(i420_buffer, data, buffer_size);

		// 一定間隔でキーフレーム(Iフレーム)を挿入
		if (num_of_frames++ % keyframe_interval == 0) {
			encoder->ForceIntraFrame(true);
		}

		// タイムスタンプをmsで付加
		pic->uiTimeStamp = (long long)(timestamp * 1000.0f);

		// フレームエンコード
		int ret = encoder->EncodeFrame(pic, bsi);
		if (ret != 0) {
			return ret;
		}

		// エンコード完了コールバック
		if (bsi->eFrameType != videoFrameTypeSkip) {
			OnEncode(dynamic_cast<SFrameBSInfo%>(*bsi));
		}

		return 0;
	}

	void OpenH264Encoder::OnEncode(const SFrameBSInfo% info)
	{
		for (int i = 0; i < info.iLayerNum; ++i) {
			const SLayerBSInfo& layerInfo = info.sLayerInfo[i];
			int layerSize = 0;
			for (int j = 0; j < layerInfo.iNalCount; ++j) {
				layerSize += layerInfo.pNalLengthInByte[j];
			}

			bool keyFrame = (info.eFrameType == videoFrameTypeIDR) || (info.eFrameType == videoFrameTypeI);
			OnEncodeFunc(layerInfo.pBsBuf, layerSize, keyFrame);
		}
	}

	// エンコーダーの設定
	// width, height:画像サイズ
	// fps:フレームレート
	// onEncode:1フレームエンコードするごとに呼び出されるコールバック
	int OpenH264Encoder::Setup(int width, int height, float fps, void *onEncode)
	{
		OnEncodeFunc = static_cast<OnEncodeFuncPtr>(onEncode);

		// 何フレームごとにキーフレーム(Iフレーム)を挿入するか
		// 通常の動画(30fps)では60(つまり2秒ごと)位が適切らしい。
		keyframe_interval = (int)(fps * 2);

		// encoderの初期化。encoder->Initializeを使う。
		// encoder->InitializeExtは初期化に必要なパラメータが多すぎる
		SEncParamBase base;
		memset(&base, 0, sizeof(SEncParamBase));
		base.iPicWidth = width;
		base.iPicHeight = height;
		base.fMaxFrameRate = fps;
		base.iUsageType = CAMERA_VIDEO_REAL_TIME;
		base.iTargetBitrate = 5000000;
		int rc = encoder->Initialize(&base);
		if (rc != 0) return -1;

		// ソースフレームメモリ確保
		buffer_size = width * height * 3 / 2;
		i420_buffer = new unsigned char[buffer_size];
		pic = new SSourcePicture();
		pic->iPicWidth = width;
		pic->iPicHeight = height;
		pic->iColorFormat = videoFormatI420;
		pic->iStride[0] = pic->iPicWidth;
		pic->iStride[1] = pic->iStride[2] = pic->iPicWidth >> 1;
		pic->pData[0] = i420_buffer;
		pic->pData[1] = pic->pData[0] + width * height;
		pic->pData[2] = pic->pData[1] + (width * height >> 2);

		// ビットストリーム確保
		bsi = new SFrameBSInfo();

		return 0;
	};

	// コンストラクタ
	OpenH264Encoder::OpenH264Encoder()
	{
		HMODULE hDll = LoadLibrary(L"openh264-1.7.0-win32.dll");
		if (hDll == NULL) {
			throw gcnew System::DllNotFoundException("Unable to load 'openh264-1.7.0-win32.dll'");
		}

		typedef int(__stdcall *WelsCreateSVCEncoderFunc)(ISVCEncoder** ppEncoder);
		WelsCreateSVCEncoderFunc createEncoder = (WelsCreateSVCEncoderFunc)GetProcAddress(hDll, "WelsCreateSVCEncoder");
		if (createEncoder == NULL) {
			throw gcnew System::DllNotFoundException("Unable to load WelsCreateSVCEncoder func in 'openh264-1.7.0-win32.dll'");
		}

		ISVCEncoder* enc = nullptr;
		int rc = createEncoder(&enc);
		encoder = enc;
	}

	// デストラクタ：リソースを積極的に解放する為にあるメソッド。C#のDisposeに対応。
	// マネージド、アンマネージド両方とも解放する。
	OpenH264Encoder::~OpenH264Encoder()
	{
		// マネージド解放→なし
		// ファイナライザ呼び出し
		this->!OpenH264Encoder();
	}

	// ファイナライザ：リソースの解放し忘れによる被害を最小限に抑える為にあるメソッド。
	// アンマネージドリソースを解放する。
	OpenH264Encoder::!OpenH264Encoder()
	{
		// アンマネージド解放
		HMODULE hDll = LoadLibrary(L"openh264-1.7.0-win32.dll");
		if (hDll == NULL) {
			throw gcnew System::DllNotFoundException("Unable to load 'openh264-1.7.0-win32.dll'");
		}

		typedef void(__stdcall *WelsDestroySVCEncoderFunc)(ISVCEncoder* ppEncoder);
		WelsDestroySVCEncoderFunc destroyEncoder = (WelsDestroySVCEncoderFunc)GetProcAddress(hDll, "WelsDestroySVCEncoder");
		if (destroyEncoder == NULL) {
			throw gcnew System::DllNotFoundException("Unable to load WelsDestroySVCEncoder func in 'openh264-1.7.0-win32.dll'");
		}

		encoder->Uninitialize();
		destroyEncoder(encoder);

		delete i420_buffer;
		delete pic;
		delete bsi;
	}
}
