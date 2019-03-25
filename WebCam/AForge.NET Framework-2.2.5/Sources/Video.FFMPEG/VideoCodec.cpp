// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//

#include "StdAfx.h"
#include "VideoCodec.h"

namespace libffmpeg
{
	extern "C"
	{
#include "libavcodec\avcodec.h"
	}
}

int video_codecs[] =
{
	libffmpeg::AV_CODEC_ID_MPEG1VIDEO,      // mpeg1video
	libffmpeg::AV_CODEC_ID_MPEG2VIDEO,      // mpeg1video
	libffmpeg::AV_CODEC_ID_H261,            // h261
	libffmpeg::AV_CODEC_ID_H263,            // h263
	libffmpeg::AV_CODEC_ID_RV10,            // rv10
	libffmpeg::AV_CODEC_ID_RV20,            // rv20
	libffmpeg::AV_CODEC_ID_MJPEG,           // mjpeg
	libffmpeg::AV_CODEC_ID_LJPEG,           // ljpeg
	libffmpeg::AV_CODEC_ID_JPEGLS,          // jpegls
	libffmpeg::AV_CODEC_ID_MPEG4,           // mpeg4
	libffmpeg::AV_CODEC_ID_RAWVIDEO,        // rawvideo
	libffmpeg::AV_CODEC_ID_MSMPEG4V2,       // msmpeg4v2
	libffmpeg::AV_CODEC_ID_MSMPEG4V3,       // msmpeg4v3
	libffmpeg::AV_CODEC_ID_WMV1,            // wmv1
	libffmpeg::AV_CODEC_ID_WMV2,            // wmv2
	libffmpeg::AV_CODEC_ID_H263P,           // h263p
	libffmpeg::AV_CODEC_ID_FLV1,            // flv1
	libffmpeg::AV_CODEC_ID_SVQ1,            // svq1
	libffmpeg::AV_CODEC_ID_DVVIDEO,         // dvvideo
	libffmpeg::AV_CODEC_ID_HUFFYUV,         // huffyuv
	libffmpeg::AV_CODEC_ID_H264,            // h264
	libffmpeg::AV_CODEC_ID_THEORA,          // theora
	libffmpeg::AV_CODEC_ID_ASV1,            // asv1
	libffmpeg::AV_CODEC_ID_ASV2,            // asv2
	libffmpeg::AV_CODEC_ID_FFV1,            // ffv1
	libffmpeg::AV_CODEC_ID_CLJR,            // cljr
	libffmpeg::AV_CODEC_ID_ROQ,             // roq
	libffmpeg::AV_CODEC_ID_CINEPAK,         // cinepak
	libffmpeg::AV_CODEC_ID_MSVIDEO1,        // msvideo1
	libffmpeg::AV_CODEC_ID_ZLIB,            // zlib
	libffmpeg::AV_CODEC_ID_QTRLE,           // qtrle
	libffmpeg::AV_CODEC_ID_PNG,             // png
	libffmpeg::AV_CODEC_ID_PPM,             // ppm
	libffmpeg::AV_CODEC_ID_PBM,             // pbm
	libffmpeg::AV_CODEC_ID_PGM,             // pgm
	libffmpeg::AV_CODEC_ID_PGMYUV,          // pgmyuv
	libffmpeg::AV_CODEC_ID_PAM,             // pam
	libffmpeg::AV_CODEC_ID_FFVHUFF,         // ffvhuff
	libffmpeg::AV_CODEC_ID_BMP,             // bmp
	libffmpeg::AV_CODEC_ID_ZMBV,            // zmbv
	libffmpeg::AV_CODEC_ID_FLASHSV,         // flashsv
	libffmpeg::AV_CODEC_ID_JPEG2000,        // jpeg2000
	libffmpeg::AV_CODEC_ID_TARGA,           // targa
	libffmpeg::AV_CODEC_ID_TIFF,            // tiff
	libffmpeg::AV_CODEC_ID_GIF,             // gif
	libffmpeg::AV_CODEC_ID_DNXHD,           // dnxhd
	libffmpeg::AV_CODEC_ID_SGI,             // sgi
	libffmpeg::AV_CODEC_ID_AMV,             // amv
	libffmpeg::AV_CODEC_ID_PCX,             // pcx
	libffmpeg::AV_CODEC_ID_SUNRAST,         // sunrast
	libffmpeg::AV_CODEC_ID_DIRAC,           // dirac
	libffmpeg::AV_CODEC_ID_V210,            // v210
	libffmpeg::AV_CODEC_ID_DPX,             // dpx
	libffmpeg::AV_CODEC_ID_FLASHSV2,        // flashsv2
	libffmpeg::AV_CODEC_ID_R210,            // r210
	libffmpeg::AV_CODEC_ID_VP8,             // vp8
	libffmpeg::AV_CODEC_ID_A64_MULTI,       // a64multi
	libffmpeg::AV_CODEC_ID_A64_MULTI5,      // a64multi5
	libffmpeg::AV_CODEC_ID_R10K,            // r10k
	libffmpeg::AV_CODEC_ID_PRORES,          // prores
	libffmpeg::AV_CODEC_ID_UTVIDEO,         // utvideo
	libffmpeg::AV_CODEC_ID_V410,            // v410
	libffmpeg::AV_CODEC_ID_XWD,             // xwd
	libffmpeg::AV_CODEC_ID_XBM,             // xbm
	libffmpeg::AV_CODEC_ID_VP9,             // vp9
	libffmpeg::AV_CODEC_ID_WEBP,            // webp
	libffmpeg::AV_CODEC_ID_HEVC,            // hevc
	libffmpeg::AV_CODEC_ID_H265,            // h265
	libffmpeg::AV_CODEC_ID_ALIAS_PIX,       // alias_pix
	libffmpeg::AV_CODEC_ID_HAP,             // hap
	libffmpeg::AV_CODEC_ID_Y41P,            // y41p
	libffmpeg::AV_CODEC_ID_AVRP,            // avrp
	libffmpeg::AV_CODEC_ID_AVUI,            // avui
	libffmpeg::AV_CODEC_ID_AYUV,            // ayuv
	libffmpeg::AV_CODEC_ID_V308,            // v308
	libffmpeg::AV_CODEC_ID_V408,            // v408
	libffmpeg::AV_CODEC_ID_YUV4,            // yuv4
	libffmpeg::AV_CODEC_ID_XFACE,           // xface
	libffmpeg::AV_CODEC_ID_SNOW,            // snow
	libffmpeg::AV_CODEC_ID_APNG,            // apng
	libffmpeg::AV_CODEC_ID_MAGICYUV,        // magicyuv
	libffmpeg::AV_CODEC_ID_AV1,             // av1
	libffmpeg::AV_CODEC_ID_FITS,            // fits
};

int pixel_formats[] =
{
	libffmpeg::AV_PIX_FMT_YUV420P,          // mpeg1video
	libffmpeg::AV_PIX_FMT_YUV420P,          // mpeg2video
	libffmpeg::AV_PIX_FMT_YUV420P,          // h261
	libffmpeg::AV_PIX_FMT_YUV420P,          // h263
	libffmpeg::AV_PIX_FMT_YUV420P,          // rv10
	libffmpeg::AV_PIX_FMT_YUV420P,          // rv20
	libffmpeg::AV_PIX_FMT_YUVJ420P,         // mjpeg
	libffmpeg::AV_PIX_FMT_BGR24,            // ljpeg
	libffmpeg::AV_PIX_FMT_BGR24,            // jpegls
	libffmpeg::AV_PIX_FMT_YUV420P,          // mpeg4
	libffmpeg::AV_PIX_FMT_RGB24,            // rawvideo
	libffmpeg::AV_PIX_FMT_YUV420P,          // msmpeg4v2
	libffmpeg::AV_PIX_FMT_YUV420P,          // msmpeg4v3
	libffmpeg::AV_PIX_FMT_YUV420P,          // wmv1
	libffmpeg::AV_PIX_FMT_YUV420P,          // wmv2
	libffmpeg::AV_PIX_FMT_YUV420P,          // h263p
	libffmpeg::AV_PIX_FMT_YUV420P,          // flv1
	libffmpeg::AV_PIX_FMT_YUV410P,          // svq1
	libffmpeg::AV_PIX_FMT_YUV411P,          // dvvideo
	libffmpeg::AV_PIX_FMT_YUV422P,          // huffyuv
	libffmpeg::AV_PIX_FMT_YUV420P,          // h264
	libffmpeg::AV_PIX_FMT_YUV420P,          // theora
	libffmpeg::AV_PIX_FMT_YUV420P,          // asv1
	libffmpeg::AV_PIX_FMT_YUV420P,          // asv2
	libffmpeg::AV_PIX_FMT_YUV420P,          // ffv1
	libffmpeg::AV_PIX_FMT_YUV411P,          // cljr
	libffmpeg::AV_PIX_FMT_YUVJ444P,         // roq
	libffmpeg::AV_PIX_FMT_RGB24,            // cinepak
	libffmpeg::AV_PIX_FMT_RGB555LE,         // msvideo1
	libffmpeg::AV_PIX_FMT_BGR24,            // zlib
	libffmpeg::AV_PIX_FMT_RGB24,            // qtrle
	libffmpeg::AV_PIX_FMT_RGB24,            // png
	libffmpeg::AV_PIX_FMT_RGB24,            // ppm
	libffmpeg::AV_PIX_FMT_MONOWHITE,        // pbm
	libffmpeg::AV_PIX_FMT_GRAY8,            // pgm
	libffmpeg::AV_PIX_FMT_YUV420P,          // pgmyuv
	libffmpeg::AV_PIX_FMT_RGB24,            // pam
	libffmpeg::AV_PIX_FMT_YUV420P,          // ffvhuff
	libffmpeg::AV_PIX_FMT_BGRA,             // bmp
	libffmpeg::AV_PIX_FMT_PAL8,             // zmbv
	libffmpeg::AV_PIX_FMT_BGR24,            // flashsv
	libffmpeg::AV_PIX_FMT_RGB24,            // jpeg2000
	libffmpeg::AV_PIX_FMT_BGR24,            // targa
	libffmpeg::AV_PIX_FMT_RGB24,            // tiff
	libffmpeg::AV_PIX_FMT_RGB8,             // gif
	libffmpeg::AV_PIX_FMT_YUV422P,          // dnxhd
	libffmpeg::AV_PIX_FMT_RGB24,            // sgi
	libffmpeg::AV_PIX_FMT_YUVJ420P,         // amv
	libffmpeg::AV_PIX_FMT_RGB24,            // pcx
	libffmpeg::AV_PIX_FMT_BGR24,            // sunrast
	libffmpeg::AV_PIX_FMT_YUV420P,          // dirac
	libffmpeg::AV_PIX_FMT_YUV422P10LE,      // v210
	libffmpeg::AV_PIX_FMT_GRAY8,            // dpx
	libffmpeg::AV_PIX_FMT_BGR24,            // flashsv2
	libffmpeg::AV_PIX_FMT_RGB48LE,          // r210
	libffmpeg::AV_PIX_FMT_YUV420P,          // vp8
	libffmpeg::AV_PIX_FMT_GRAY8,            // a64multi
	libffmpeg::AV_PIX_FMT_GRAY8,            // a64multi5
	libffmpeg::AV_PIX_FMT_RGB48LE,          // r10k
	libffmpeg::AV_PIX_FMT_YUV422P10LE,      // prores
	libffmpeg::AV_PIX_FMT_GBRP,             // utvideo
	libffmpeg::AV_PIX_FMT_YUV444P10LE,      // v410
	libffmpeg::AV_PIX_FMT_BGRA,             // xwd
	libffmpeg::AV_PIX_FMT_MONOWHITE,        // xbm
	libffmpeg::AV_PIX_FMT_YUV420P,          // vp9
	libffmpeg::AV_PIX_FMT_BGRA,             // webp
	libffmpeg::AV_PIX_FMT_YUV420P,          // hevc
	libffmpeg::AV_PIX_FMT_YUV420P,          // h265
	libffmpeg::AV_PIX_FMT_BGR24,            // alias_pix
	libffmpeg::AV_PIX_FMT_RGBA,             // hap
	libffmpeg::AV_PIX_FMT_YUV411P,          // y41p
	libffmpeg::AV_PIX_FMT_RGB48LE,          // avrp
	libffmpeg::AV_PIX_FMT_UYVY422,          // avui
	libffmpeg::AV_PIX_FMT_YUVA444P,         // ayuv
	libffmpeg::AV_PIX_FMT_YUV444P,          // v308
	libffmpeg::AV_PIX_FMT_YUVA444P,         // v408
	libffmpeg::AV_PIX_FMT_YUV420P,          // yuv4
	libffmpeg::AV_PIX_FMT_MONOWHITE,        // xface
	libffmpeg::AV_PIX_FMT_YUV420P,          // snow
	libffmpeg::AV_PIX_FMT_RGB24,            // apng
	libffmpeg::AV_PIX_FMT_GBRP,             // magicyuv
	libffmpeg::AV_PIX_FMT_YUV420P,          // av1
	libffmpeg::AV_PIX_FMT_GBRAP16BE,        // fits
};

int CODECS_COUNT(sizeof(video_codecs) / sizeof(libffmpeg::AVCodecID));
