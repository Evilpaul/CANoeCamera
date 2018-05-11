// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2012
// contacts@aforgenet.com
//

#include "StdAfx.h"
#include "VideoFileWriter.h"

#include <string>

namespace libffmpeg
{
	extern "C"
	{
		#include "libavutil\avutil.h"
		#include "libavutil\imgutils.h"
		#include "libavformat\avformat.h"
		#include "libavformat\avio.h"
		#include "libavcodec\avcodec.h"
		#include "libswscale\swscale.h"
	}
}

namespace AForge {
	namespace Video {
		namespace FFMPEG
{
#pragma region Private methods

// A structure to encapsulate all FFMPEG related private variable
ref struct WriterPrivateData
{
public:
	libffmpeg::AVFormatContext*		FormatContext;
	libffmpeg::AVStream*			VideoStream;
	libffmpeg::AVFrame*				VideoFrame;
                libffmpeg::SwsContext*			ConvertContext;
                libffmpeg::SwsContext*			ConvertContextGrayscale;

	uint8_t*	VideoOutputBuffer;
	int VideoOutputBufferSize;

	WriterPrivateData( )
	{
                    FormatContext = nullptr;
                    VideoStream = nullptr;
                    VideoFrame = nullptr;
                    ConvertContext = nullptr;
                    ConvertContextGrayscale = nullptr;
                    VideoOutputBuffer = nullptr;
	}
};

            // Writes video frame to opened video file
            void write_video_frame(WriterPrivateData^ data)
            {
				libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;

                libffmpeg::AVPacket packet;
                libffmpeg::av_init_packet(&packet);
                packet.data = nullptr;
                packet.size = 0;

                // encode the image
				if (libffmpeg::avcodec_send_frame(codecContext, data->VideoFrame) < 0)
					throw gcnew VideoException("Error sending a frame for encoding");

				if (libffmpeg::avcodec_receive_packet(codecContext, &packet) < 0)
					return;

                if (packet.pts != AV_NOPTS_VALUE)
                    packet.pts = libffmpeg::av_rescale_q(packet.pts, codecContext->time_base, data->VideoStream->time_base);
                if (packet.dts != AV_NOPTS_VALUE)
                    packet.dts = libffmpeg::av_rescale_q(packet.dts, codecContext->time_base, data->VideoStream->time_base);

                packet.stream_index = data->VideoStream->index;
                Console::WriteLine("Stream: {0} PTS: {1} -> {1} bytes", packet.stream_index, packet.pts, packet.size);

                // write the compressed frame to the media file
                if (libffmpeg::av_interleaved_write_frame(data->FormatContext, &packet) != 0)
                    throw gcnew VideoException("Error while writing video frame.");
            }

            // Allocate picture of the specified format and size
            static libffmpeg::AVFrame* alloc_picture(enum libffmpeg::AVPixelFormat pix_fmt, int width, int height)
            {
                libffmpeg::AVFrame* picture = libffmpeg::av_frame_alloc();
                if (!picture)
                    return nullptr;

                int size = libffmpeg::av_image_get_buffer_size(pix_fmt, width, height, 1);
                void* picture_buf = libffmpeg::av_malloc(size);
                if (!picture_buf)
                {
                    libffmpeg::av_free(picture);
                    return nullptr;
                }

                libffmpeg::av_image_fill_arrays(picture->data, picture->linesize, (uint8_t *)picture_buf,
                    pix_fmt, width, height, 1);
                return picture;
            }

            // Create new video stream and configure it
            void add_video_stream(WriterPrivateData^ data, int width, int height, Rational frameRate,
                int bitRate, libffmpeg::AVCodecID codecId, libffmpeg::AVPixelFormat pixelFormat)
            {
                libffmpeg::AVCodec *codec = libffmpeg::avcodec_find_encoder(codecId);
                libffmpeg::AVCodecContext* codecContex;

                // create new stream
                data->VideoStream = libffmpeg::avformat_new_stream(data->FormatContext, codec);
                if (!data->VideoStream)
                    throw gcnew VideoException("Failed creating new video stream.");

                codecContex = data->VideoStream->codec;
                codecContex->codec_id = codecId;
                codecContex->codec_type = libffmpeg::AVMEDIA_TYPE_VIDEO;

                // put sample parameters
                codecContex->bit_rate = bitRate;
                codecContex->width = width;
                codecContex->height = height;

                // time base: this is the fundamental unit of time (in seconds) in terms
                // of which frame timestamps are represented. for fixed-fps content,
                // timebase should be 1/framerate and timestamp increments should be
                // identically 1.
                codecContex->time_base.num = frameRate.Denominator;
                codecContex->time_base.den = frameRate.Numerator;

                //codecContex->framerate = { frameRate.Denominator, frameRate.Numerator };
                //codecContex->ticks_per_frame = 1;
                //data->VideoStream->time_base = codecContex->time_base;

                codecContex->gop_size = 12; // emit one intra frame every twelve frames at most
                codecContex->pix_fmt = pixelFormat;

                if (codecContex->codec_id == libffmpeg::AV_CODEC_ID_MPEG1VIDEO)
                {
                    // Needed to avoid using macroblocks in which some coeffs overflow.
                    // This does not happen with normal video, it just happens here as
                    // the motion of the chroma plane does not match the luma plane.
                    codecContex->mb_decision = 2;
                }

                if (codecContex->codec_id == libffmpeg::AV_CODEC_ID_H264 ||
                    codecContex->codec_id == libffmpeg::AV_CODEC_ID_H265)
                {
                    data->VideoStream->need_parsing = libffmpeg::AVSTREAM_PARSE_FULL_ONCE;

                    //codecContex->coder_type = FF_CODER_TYPE_AC;
                    codecContex->profile = FF_PROFILE_H264_BASELINE;
                    //codecContex->crf = 25;
                    //codecContex->me_method = 7;
                    codecContex->me_subpel_quality = 4;
                    codecContex->delay = 0;
                    codecContex->max_b_frames = 0;
                    codecContex->refs = 3;
                    /*
                    codecContex->flags            |= CODEC_FLAG_LOOP_FILTER;
                    codecContex->flags2           |= CODEC_FLAG2_WPRED | CODEC_FLAG2_8X8DCT;

                    codecContex->scenechange_threshold = 0;
                    codecContex->gop_size          = 250;
                    codecContex->max_b_frames      = 0;
                    codecContex->max_qdiff         = 4;
                    codecContex->me_method         = 10;
                    codecContex->me_range          = 16;
                    codecContex->me_cmp            = 1;
                    codecContex->me_subpel_quality = 6;
                    codecContex->qmin              = 0;
                    codecContex->qmax              = 69;
                    codecContex->qcompress         = 0.6f;
                    codecContex->keyint_min        = 25;
                    codecContex->trellis           = 0;
                    codecContex->level             = 13;
                    codecContex->refs              = 16;
                    codecContex->weighted_p_pred   = 2;
                    codecContex->b_frame_strategy  = 1;
                    */
                    codecContex->color_range = libffmpeg::AVCOL_RANGE_JPEG;
                }

                if (codecContex->codec_id == libffmpeg::AV_CODEC_ID_THEORA)
                    codecContex->color_range = libffmpeg::AVCOL_RANGE_JPEG;

                // some formats want stream headers to be separate
                if (data->FormatContext->oformat->flags & AVFMT_GLOBALHEADER)
                    codecContex->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            // Open video codec and prepare out buffer and picture
            void open_video(WriterPrivateData^ data)
            {
                libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;
                libffmpeg::AVCodec* codec = avcodec_find_encoder(codecContext->codec_id);

                if (!codec)
                    throw gcnew VideoException("Cannot find video codec.");

                // open the codec 
                if (avcodec_open2(codecContext, codec, nullptr) < 0)
                    throw gcnew VideoException("Cannot open video codec.");

                data->VideoOutputBuffer = nullptr;

                // allocate output buffer, more than enough even for raw video
                data->VideoOutputBufferSize = 6 * codecContext->width * codecContext->height;
                data->VideoOutputBuffer = (uint8_t*)libffmpeg::av_malloc(data->VideoOutputBufferSize);

                // allocate the encoded raw picture
                data->VideoFrame = alloc_picture(codecContext->pix_fmt, codecContext->width, codecContext->height);
                data->VideoFrame->width = codecContext->width;
                data->VideoFrame->height = codecContext->height;
                data->VideoFrame->format = codecContext->pix_fmt;

                if (!data->VideoFrame)
                    throw gcnew VideoException("Cannot allocate video picture.");

                // prepare scaling context to convert RGB image to video format
                data->ConvertContext = libffmpeg::sws_getContext(codecContext->width, codecContext->height,
                    libffmpeg::AV_PIX_FMT_BGR24,
                    codecContext->width, codecContext->height, codecContext->pix_fmt,
                    SWS_BICUBIC, nullptr, nullptr, nullptr);

                // prepare scaling context to convert grayscale image to video format
                data->ConvertContextGrayscale = libffmpeg::sws_getContext(codecContext->width, codecContext->height,
                    libffmpeg::AV_PIX_FMT_GRAY8,
                    codecContext->width, codecContext->height, codecContext->pix_fmt,
                    SWS_BICUBIC, nullptr, nullptr, nullptr);

                if ((data->ConvertContext == nullptr) || (data->ConvertContextGrayscale == nullptr))
                    throw gcnew VideoException("Cannot initialize frames conversion context.");
            }

#pragma endregion

            // Class constructor
            VideoFileWriter::VideoFileWriter()
                : data(nullptr), disposed(false) { }

            // Creates a video file with the specified name and properties
            void VideoFileWriter::Open(String^ fileName, int width, int height, Rational frameRate,
                VideoCodec codec, int bitRate)
            {
                CheckIfDisposed();

                // close previous file if any open
                Close();

                data = gcnew WriterPrivateData();
                bool success = false;

                // check width and height
                if (((width & 1) != 0) || ((height & 1) != 0))
                    throw gcnew ArgumentException("Video file resolution must be a multiple of two.");

                // check video codec
                if (((int)codec < -1) || ((int)codec >= CODECS_COUNT))
                    throw gcnew ArgumentException("Invalid video codec is specified.");

                m_width = width;
                m_height = height;
                m_codec = codec;
                m_frameRate = frameRate;
                m_bitRate = bitRate;
                m_framesCount = 0;

                try
                {
                    // convert specified managed String to unmanaged string
                    IntPtr ptr = System::Runtime::InteropServices::Marshal::StringToHGlobalUni(fileName);
                    wchar_t* nativeFileNameUnicode = (wchar_t*)ptr.ToPointer();
                    int utf8StringSize = WideCharToMultiByte(CP_UTF8, 0, nativeFileNameUnicode, -1, NULL, 0, NULL, NULL);
                    char* nativeFileName = new char[utf8StringSize];
                    WideCharToMultiByte(CP_UTF8, 0, nativeFileNameUnicode, -1, nativeFileName, utf8StringSize, NULL, NULL);

                    // guess about destination file format from its file name
                    libffmpeg::AVOutputFormat* outputFormat = libffmpeg::av_guess_format(nullptr,
                        nativeFileName, nullptr);

                    if (!outputFormat)
                    {
                        // guess about destination file format from its short name
                        outputFormat = libffmpeg::av_guess_format("matroska", nullptr, nullptr);

                        if (!outputFormat)
                            throw gcnew VideoException("Cannot find suitable output format.");
                    }

                    // prepare format context
                    data->FormatContext = libffmpeg::avformat_alloc_context();
                    if (!data->FormatContext)
                        throw gcnew VideoException("Cannot allocate format context.");

                    data->FormatContext->oformat = outputFormat;

                    // add video stream using the specified video codec
                    add_video_stream(data, width, height, frameRate, bitRate,
                        (codec == VideoCodec::Default)
                        ? outputFormat->video_codec : (libffmpeg::AVCodecID) video_codecs[(int)codec],
                        (codec == VideoCodec::Default)
                        ? libffmpeg::AV_PIX_FMT_YUV420P : (libffmpeg::AVPixelFormat) pixel_formats[(int)codec]);

                    open_video(data);

                    // open output file
                    if (!(outputFormat->flags & AVFMT_NOFILE))
                    {
                        int err = libffmpeg::avio_open(&data->FormatContext->pb, nativeFileName, AVIO_FLAG_WRITE);
                        
                        if (err < 0)
                        {
                            System::String^ msg = GetErrorMessage(err, fileName);
                            throw gcnew System::IO::IOException("Cannot open the video file. Error code: " + err +
                                ". Message: " + msg + " when trying to access: " + fileName);
                        }
                    }

                    libffmpeg::avformat_write_header(data->FormatContext, nullptr);
                    success = true;
                }
                finally
                {
                    if (!success)
                        Close();
                }
            }

            System::String^ VideoFileWriter::GetErrorMessage(int err, System::String ^ fileName)
            {
                char buff[AV_ERROR_MAX_STRING_SIZE];
                libffmpeg::av_make_error_string(&buff[0], AV_ERROR_MAX_STRING_SIZE, err);
                System::String^ msg = System::Runtime::InteropServices::Marshal::PtrToStringAnsi((IntPtr)&buff[0]);
                return msg;
            }

            // Close current video file
            void VideoFileWriter::Close()
            {
                if (data == nullptr)
                    return;

                Flush();

                if (data->FormatContext)
                {
                    if (data->FormatContext->pb != nullptr)
                        libffmpeg::av_write_trailer(data->FormatContext);

                    if (data->VideoStream)
                        libffmpeg::avcodec_close(data->VideoStream->codec);

                    if (data->VideoFrame)
                    {
                        libffmpeg::av_free(data->VideoFrame->data[0]);
                        libffmpeg::av_free(data->VideoFrame);
                    }

                    if (data->VideoOutputBuffer)
                        libffmpeg::av_free(data->VideoOutputBuffer);

                    if (data->FormatContext->pb != nullptr)
                        libffmpeg::avio_close(data->FormatContext->pb);

                    libffmpeg::avformat_free_context(data->FormatContext);
                }

                if (data->ConvertContext != nullptr)
                    libffmpeg::sws_freeContext(data->ConvertContext);

                if (data->ConvertContextGrayscale != nullptr)
                    libffmpeg::sws_freeContext(data->ConvertContextGrayscale);

                data = nullptr;
                m_width = 0;
                m_height = 0;
            }

            // Flushes delayed frames to disk
            void VideoFileWriter::Flush()
            {
                // This function goes by the data->VideoOutputBuffer extracting
                // and saving to disk one frame at time, using mostly the same
                // code which can be found on write_video_frame.
                if (data == nullptr)
                    return;

                libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;

                while (true) // while there are still delayed frames
                {
                    libffmpeg::AVPacket packet;
                    libffmpeg::av_init_packet(&packet);
                    packet.data = nullptr;
                    packet.size = 0;

                    // encode the image
					if (libffmpeg::avcodec_send_frame(codecContext, nullptr) < 0)
						throw gcnew VideoException("Error sending a (flush)frame for encoding");

					if (libffmpeg::avcodec_receive_packet(codecContext, &packet) < 0)
						break;

                    // TODO: consider refactoring with write_video_frame?
                    if (packet.pts != AV_NOPTS_VALUE)
                        packet.pts = libffmpeg::av_rescale_q(packet.pts, codecContext->time_base, data->VideoStream->time_base);
                    if (packet.dts != AV_NOPTS_VALUE)
                        packet.dts = libffmpeg::av_rescale_q(packet.dts, codecContext->time_base, data->VideoStream->time_base);

                    packet.stream_index = data->VideoStream->index;

                    // write the compressed frame to the media file
                    if (libffmpeg::av_interleaved_write_frame(data->FormatContext, &packet) != 0)
                        throw gcnew VideoException("Error while writing video frame.");
                }

                libffmpeg::avcodec_flush_buffers(data->VideoStream->codec);
            }

            // Writes new video frame to the opened video file
            void VideoFileWriter::WriteVideoFrame(Bitmap^ frame, unsigned long frameIndex)
            {
                // lock the bitmap
                BitmapData^ bitmapData = frame->LockBits(System::Drawing::Rectangle(0, 0, m_width, m_height),
                    ImageLockMode::ReadOnly,
                    (frame->PixelFormat == PixelFormat::Format8bppIndexed) ?
                    PixelFormat::Format8bppIndexed : PixelFormat::Format24bppRgb);

                WriteVideoFrame(bitmapData, frameIndex);

                frame->UnlockBits(bitmapData);
            }

            // Writes new video frame to the opened video file
            void VideoFileWriter::WriteVideoFrame(BitmapData^ bitmapData, unsigned long frameIndex)
            {
                CheckIfDisposed();

                if (data == nullptr)
                    throw gcnew System::IO::IOException("A video file was not opened yet.");

                if ((bitmapData->PixelFormat != PixelFormat::Format24bppRgb) &&
                    (bitmapData->PixelFormat != PixelFormat::Format32bppArgb) &&
                    (bitmapData->PixelFormat != PixelFormat::Format32bppPArgb) &&
                    (bitmapData->PixelFormat != PixelFormat::Format32bppRgb) &&
                    (bitmapData->PixelFormat != PixelFormat::Format8bppIndexed))
                {
                    throw gcnew ArgumentException("The provided bitmap must be 24 or 32 bpp color image or 8 bpp grayscale image.");
                }

                if ((bitmapData->Width != m_width) || (bitmapData->Height != m_height))
                    throw gcnew ArgumentException("Bitmap size must be of the same as video size, which was specified on opening video file.");
               

				uint8_t* srcData[4] = { static_cast<uint8_t*>(static_cast<void*>(bitmapData->Scan0)), nullptr, nullptr, nullptr };
                int srcLinesize[4] = { bitmapData->Stride, 0, 0, 0 };

                // convert source image to the format of the video file
                if (bitmapData->PixelFormat == PixelFormat::Format8bppIndexed)
                {
                    libffmpeg::sws_scale(data->ConvertContextGrayscale, srcData, srcLinesize, 0,
                        m_height, data->VideoFrame->data, data->VideoFrame->linesize);
                }
                else
                {
                    libffmpeg::sws_scale(data->ConvertContext, srcData, srcLinesize, 0, m_height,
                        data->VideoFrame->data, data->VideoFrame->linesize);
                }

                data->VideoFrame->pts = frameIndex;

                // write the converted frame to the video file
                write_video_frame(data);

                m_framesCount++;
            }

            /*
            // Writes new video frame to the opened video file
            void VideoFileWriter::WriteAudioFrame( array<System::uint8_t> ^buffer )
            {
            WriteAudioFrame( buffer, TimeSpan::MinValue );
            }
            */
        }
    }
}
