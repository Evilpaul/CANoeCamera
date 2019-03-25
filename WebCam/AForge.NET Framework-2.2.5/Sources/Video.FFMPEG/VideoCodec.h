// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//

#pragma once

using namespace System;

extern int video_codecs[];
extern int pixel_formats[];

extern int CODECS_COUNT;

namespace AForge
{
	namespace Video
	{
		namespace FFMPEG
		{
			/// <summary>
			/// Enumeration of some video codecs from FFmpeg library, which are available for writing video files.
			/// </summary>
			///
			public enum class VideoCodec
			{
				/// <summary>
				///   Special codec identifier meaning that the audio codec should be chosen automatically.
				/// </summary>
				Default = -1,

				/// <summary>
				///   MPEG-1 video.
				/// </summary>
				mpeg1video,

				/// <summary>
				///   MPEG-2 video.
				/// </summary>
				mpeg2video,

				/// <summary>
				///   H.261.
				/// </summary>
				h261,

				/// <summary>
				///   H.263 / H.263-1996, H.263+ / H.263-1998 / H.263 version 2.
				/// </summary>
				h263,

				/// <summary>
				///   RealVideo 1.0.
				/// </summary>
				rv10,

				/// <summary>
				///    RealVideo 2.0.
				/// </summary>
				rv20,

				/// <summary>
				///   Motion JPEG.
				/// </summary>
				mjpeg,

				/// <summary>
				///   Lossless JPEG.
				/// </summary>
				ljpeg,

				/// <summary>
				///   JPEG-LS.
				/// </summary>
				jpegls,

				/// <summary>
				///   MPEG-4 part 2.
				/// </summary>
				mpeg4,

				/// <summary>
				///   raw video.
				/// </summary>
				rawvideo,

				/// <summary>
				///   MPEG-4 part 2 Microsoft variant version 2.
				/// </summary>
				msmpeg4v2,

				/// <summary>
				///   MPEG-4 part 2 Microsoft variant version 3.
				/// </summary>
				msmpeg4v3,

				/// <summary>
				///   Windows Media Video 7.
				/// </summary>
				wmv1,

				/// <summary>
				///   Windows Media Video 8.
				/// </summary>
				wmv2,

				/// <summary>
				///   H.263+ / H.263-1998 / H.263 version 2.
				/// </summary>
				h263p,

				/// <summary>
				///   LV / Sorenson Spark / Sorenson H.263 (Flash Video).
				/// </summary>
				flv1,

				/// <summary>
				///   Sorenson Vector Quantizer 1 / Sorenson Video 1 / SVQ1.
				/// </summary>
				svq1,

				/// <summary>
				///   DV (Digital Video).
				/// </summary>
				dvvideo,

				/// <summary>
				///   HuffYUV.
				/// </summary>
				huffyuv,

				/// <summary>
				///   H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10.
				/// </summary>
				h264,

				/// <summary>
				///   Theora.
				/// </summary>
				theora,

				/// <summary>
				///   ASUS V1.
				/// </summary>
				asv1,

				/// <summary>
				///  ASUS V2.
				/// </summary>
				asv2,

				/// <summary>
				///   FFmpeg video codec #1.
				/// </summary>
				ffv1,

				/// <summary>
				///   Cirrus Logic AccuPak.
				/// </summary>
				cljr,

				/// <summary>
				///   id RoQ video.
				/// </summary>
				roq,

				/// <summary>
				///   Cinepak.
				/// </summary>
				cinepak,

				/// <summary>
				///   Microsoft Video 1.
				/// </summary>
				msvideo1,

				/// <summary>
				///   LCL (LossLess Codec Library) ZLIB.
				/// </summary>
				zlib,

				/// <summary>
				///   QuickTime Animation (RLE) video.
				/// </summary>
				qtrle,

				/// <summary>
				///   PNG (Portable Network Graphics) image.
				/// </summary>
				png,

				/// <summary>
				///   PPM (Portable PixelMap) image.
				/// </summary>
				ppm,

				/// <summary>
				///   PBM (Portable BitMap) image.
				/// </summary>
				pbm,

				/// <summary>
				///   PGM (Portable GrayMap) image.
				/// </summary>
				pgm,

				/// <summary>
				///   PGMYUV (Portable GrayMap YUV) image.
				/// </summary>
				pgmyuv,

				/// <summary>
				///   PAM (Portable AnyMap) image.
				/// </summary>
				pam,

				/// <summary>
				///   Huffyuv FFmpeg variant.
				/// </summary>
				ffvhuff,

				/// <summary>
				///   BMP (Windows and OS/2 bitmap).
				/// </summary>
				bmp,

				/// <summary>
				///    Zip Motion Blocks Video.
				/// </summary>
				zmbv,

				/// <summary>
				///   Flash Screen Video v1.
				/// </summary>
				flashsv,

				/// <summary>
				///   JPEG 2000.
				/// </summary>
				jpeg2000,

				/// <summary>
				///   Truevision Targa image.
				/// </summary>
				targa,

				/// <summary>
				///   TIFF image.
				/// </summary>
				tiff,

				/// <summary>
				///   GIF (Graphics Interchange Format).
				/// </summary>
				gif,

				/// <summary>
				///   VC3/DNxHD.
				/// </summary>
				dnxhd,

				/// <summary>
				///   SGI image.
				/// </summary>
				sgi,

				/// <summary>
				///   AMV Video.
				/// </summary>
				amv,

				/// <summary>
				///   PC Paintbrush PCX image.
				/// </summary>
				pcx,

				/// <summary>
				///   Sun Rasterfile image
				/// </summary>
				sunrast,

				/// <summary>
				///   Dirac.
				/// </summary>
				dirac,

				/// <summary>
				///   Uncompressed 4:2:2 10-bit.
				/// </summary>
				v210,

				/// <summary>
				///   DPX (Digital Picture Exchange) image.
				/// </summary>
				dpx,

				/// <summary>
				///   Flash Screen Video v2.
				/// </summary>
				flashsv2,

				/// <summary>
				///   Uncompressed RGB 10-bit.
				/// </summary>
				r210,

				/// <summary>
				///   On2 VP8.
				/// </summary>
				vp8,

				/// <summary>
				///   Multicolor charset for Commodore 64.
				/// </summary>
				a64multi,

				/// <summary>
				///   Multicolor charset for Commodore 64, extended with 5th color (colram).
				/// </summary>
				a64multi5,

				/// <summary>
				///   AJA Kona 10-bit RGB Codec.
				/// </summary>
				r10k,

				/// <summary>
				///   Apple ProRes (iCodec Pro).
				/// </summary>
				prores,

				/// <summary>
				///   Ut Video.
				/// </summary>
				utvideo,

				/// <summary>
				///    Uncompressed 4:4:4 10-bit.
				/// </summary>
				v410,

				/// <summary>
				///   XWD (X Window Dump) image.
				/// </summary>
				xwd,

				/// <summary>
				///   XBM (X BitMap) image.
				/// </summary>
				xbm,

				/// <summary>
				///   Google VP9.
				/// </summary>
				vp9,

				/// <summary>
				///   WebP.
				/// </summary>
				webp,

				/// <summary>
				///   H.265 / HEVC (High Efficiency Video Coding).
				/// </summary>
				hevc,

				/// <summary>
				///   H.265 / HEVC (High Efficiency Video Coding).
				///   This is an alias for <see cref="hevc"/>.
				/// </summary>
				h265,

				/// <summary>
				///   Alias/Wavefront PIX image.
				/// </summary>
				alias_pix,

				/// <summary>
				///   Vidvox Hap.
				/// </summary>
				hap,

				/// <summary>
				///   Uncompressed YUV 4:1:1 12-bit.
				/// </summary>
				y41p,

				/// <summary>
				///   Avid 1:1 10-bit RGB Packer.
				/// </summary>
				avrp,

				/// <summary>
				///   Avid Meridien Uncompressed.
				/// </summary>
				avui,

				/// <summary>
				///   Uncompressed packed MS 4:4:4:4.
				/// </summary>
				ayuv,

				/// <summary>
				///   Uncompressed packed 4:4:4.
				/// </summary>
				v308,

				/// <summary>
				///   Uncompressed packed QT 4:4:4:4.
				/// </summary>
				v408,

				/// <summary>
				///   Uncompressed packed 4:2:0.
				/// </summary>
				yuv4,

				/// <summary>
				///   X-face image.
				/// </summary>
				xface,

				/// <summary>
				///   Snow.
				/// </summary>
				snow,

				/// <summary>
				///   APNG (Animated Portable Network Graphics) image.
				/// </summary>
				apng,

				/// <summary>
				///   MagicYUV video.
				/// </summary>
				magicyuv,

				/// <summary>
				///   Alliance for Open Media AV1.
				/// </summary>
				av1,

				/// <summary>
				///   FITS (Flexible Image Transport System).
				/// </summary>
				fits,
			};
		}
	}
}
