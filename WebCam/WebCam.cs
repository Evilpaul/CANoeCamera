using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using Properties;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Vector.CANoe.Runtime;
using Vector.Tools;

public class WebCam : MeasurementScript
{
    private VideoCaptureDevice videoSource;
    private VideoFileWriter vfw;
    private int height, width, fr;
    private bool saveRequested, videoRequested;
    private string SnapShotName = "";
    private readonly object lockobj = new object();
    private TimeSpan stillMeasurementTime, videoMeasurementTime;
    private DateTime stillTriggerTime, videoTriggerTime;

    private readonly Bitmap logo = Resources.logoNew;
    private PointF logoPoint;
    private readonly PointF timePoint = new PointF(160, 5);
    private readonly RectangleF rect = new RectangleF(0, 0, 165, 40);
    private readonly SolidBrush sb_black = new SolidBrush(Color.Black);
    private readonly SolidBrush sb_white = new SolidBrush(Color.White);
    private readonly Font drawFont = new Font("Courier New", 20);
    private readonly StringFormat sf = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Far };

    private delegate void AsyncMethodCaller(Bitmap frame);
    private AsyncMethodCaller caller;

    public override void Initialize()
    {
        int preferredIdx = -1;
        FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        if ((videoDevices == null) || (videoDevices.Count == 0))
        {
            Output.WriteLine("No Camera devices connected!");
        }
        else
        {
            Output.WriteLine("Connected Cameras:");
            Output.WriteLine("------------------");
            for (int i = 0; i < videoDevices.Count; i++)
            {
                Output.WriteLine("{0:00}: {1}", i, videoDevices[i].Name);
                if(videoDevices[i].Name.Equals(WebCamSysVar.PreferredCamera.Value))
                {
                    // camera name matches name of preferred camera
                    preferredIdx = i;
                }
            }

            if(preferredIdx == -1)
            {
                // preferred camera not detected, use first connected device
                preferredIdx = 0;
            }

            Output.WriteLine("Connecting to " + videoDevices[preferredIdx].Name);
            WebCamSysVar.CurrentCamera.Value = videoDevices[preferredIdx].Name;
            videoSource = new VideoCaptureDevice(videoDevices[preferredIdx].MonikerString);

            // determine largest supported resolution
            Output.WriteLine("");
            Output.WriteLine("Supported Resolutions:");
            Output.WriteLine("----------------------");
            int maxIdx = 0;
            int matchIdx = -1;
            for (int i = 0; i < videoSource.VideoCapabilities.Length; i++)
            {
                Output.WriteLine("{0:00}: {1} x {2}", i, videoSource.VideoCapabilities[i].FrameSize.Width, videoSource.VideoCapabilities[i].FrameSize.Height);
                if ((videoSource.VideoCapabilities[i].FrameSize.Height * videoSource.VideoCapabilities[i].FrameSize.Width) > (videoSource.VideoCapabilities[maxIdx].FrameSize.Height * videoSource.VideoCapabilities[maxIdx].FrameSize.Width))
                {
                    maxIdx = i;
                }

                // check is resoultion matches preferred
                if((videoSource.VideoCapabilities[i].FrameSize.Height == WebCamSysVar.PreferredHeight.Value) && (videoSource.VideoCapabilities[i].FrameSize.Width == WebCamSysVar.PreferredWidth.Value))
                {
                    matchIdx = i;
                }
            }

            // if we matched the preferred resolution use that one, else use max supported
            if(matchIdx != -1)
            {
                maxIdx = matchIdx;
            }

            Output.WriteLine("Selecting resolution : {0} x {1}", videoSource.VideoCapabilities[maxIdx].FrameSize.Width, videoSource.VideoCapabilities[maxIdx].FrameSize.Height);
            videoSource.VideoResolution = videoSource.VideoCapabilities[maxIdx];
            videoSource.NewFrame += new NewFrameEventHandler(Video_NewFrame);

            height = videoSource.VideoCapabilities[maxIdx].FrameSize.Height;
            width = videoSource.VideoCapabilities[maxIdx].FrameSize.Width;
            fr = videoSource.VideoCapabilities[maxIdx].AverageFrameRate;

            logoPoint = new PointF(width - logo.Width, 0);

            caller = SaveSnapShot;
            caller += SaveVideo;
        }
    }

    public override void Start()
    {
        if (videoSource != null)
        {
            Output.WriteLine("Starting WebCam");
            videoSource.Start();
            PrintCameraCapabilities();
        }
    }

    public override void Stop()
    {
        if (videoSource.IsRunning)
        {
            WebCamSysVar.VideoState.Value = WebCamSysVar.VideoState.Stopped;
            saveRequested = false;
            StopVideo();
            videoSource.SignalToStop();
            WebCamSysVar.CurrentCamera.Value = "";
        }
    }

    public override void Shutdown()
    {
        if (videoSource.IsRunning)
        {
            WebCamSysVar.VideoState.Value = WebCamSysVar.VideoState.Stopped;
            saveRequested = false;
            StopVideo();
            videoSource.SignalToStop();
            WebCamSysVar.CurrentCamera.Value = "";
        }
    }

    [OnChange(typeof(WebCamSysVar.SnapShotFileName))]
    public void SnapShotFileNameHandler()
    {
        if (!string.IsNullOrWhiteSpace(WebCamSysVar.SnapShotFileName.Value))
        {
            SnapShotName = WebCamSysVar.SnapShotFileName.Value;
            stillMeasurementTime = Measurement.CurrentTime;
            stillTriggerTime = DateTime.Now;

            saveRequested = true;
        }
    }

    [OnChange(typeof(WebCamSysVar.VideoState))]
    public void VideoStateHandler()
    {
        if (WebCamSysVar.VideoState.Value == WebCamSysVar.VideoState.Recording)
        {
            StopVideo();

            try
            {
                vfw = new VideoFileWriter();
                vfw.Open(WebCamSysVar.VideoFileName.Value, width, height, fr, VideoCodec.Default, WebCamSysVar.VideoBitRate.Value);
                videoMeasurementTime = Measurement.CurrentTime;
                videoTriggerTime = DateTime.Now;
                videoRequested = true;
            }
            catch (Exception ex)
            {
                Output.WriteLine(ex.ToString());
            }
        }
        else
        {
            StopVideo();
        }
    }

    private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        try
        {
            // call each save method asynchronously
            foreach (AsyncMethodCaller amc in caller.GetInvocationList())
                amc.BeginInvoke((Bitmap)eventArgs.Frame.Clone(), null, null);
        }
        catch (Exception ex)
        {
            Output.WriteLine(ex.ToString());
        }
    }

    private void SaveSnapShot(Bitmap frame)
    {
        if (saveRequested)
        {
            saveRequested = false;
            Bitmap b = AddImageOverlay(frame, stillTriggerTime, stillMeasurementTime);

            try
            {
                switch (Path.GetExtension(SnapShotName).ToUpper())
                {
                    case ".JPG":
                    case ".JPEG":
                        b.Save(SnapShotName, ImageFormat.Jpeg);
                        break;
                    case ".BMP":
                        b.Save(SnapShotName, ImageFormat.Bmp);
                        break;
                    case ".PNG":
                        b.Save(SnapShotName, ImageFormat.Png);
                        break;
                    case ".GIF":
                        b.Save(SnapShotName, ImageFormat.Gif);
                        break;
                    case ".TIF":
                    case ".TIFF":
                        b.Save(SnapShotName, ImageFormat.Tiff);
                        break;
                    case ".EXIF":
                        b.Save(SnapShotName, ImageFormat.Exif);
                        break;
                    default:
                        b.Save(SnapShotName); // png format
                        break;
                }
            }
            catch (Exception ex)
            {
                Output.WriteLine(ex.ToString());
            }
            finally
            {
                b.Dispose();
            }
        }

        frame.Dispose();
    }

    private void SaveVideo(Bitmap frame)
    {
        if (videoRequested)
        {
            // If we cannot get the lock, skip the frame
            // should only happen if a stop has been requested, or processing of previous frame takes too long
            if (Monitor.TryEnter(lockobj))
            {
                Bitmap b = AddImageOverlay(frame, videoTriggerTime, videoMeasurementTime);

                try
                {
                    vfw.WriteVideoFrame(b);
                }
                catch (Exception ex)
                {
                    Output.WriteLine(ex.ToString());
                }
                finally
                {
                    b.Dispose();
                    // release the lock
                    Monitor.Exit(lockobj);
                }
            }
        }

        frame.Dispose();
    }

    private void StopVideo()
    {
        videoRequested = false;
        if (vfw != null)
        {
            // try to obtain lock, frame capture should always complete within 100 ms
            if (Monitor.TryEnter(lockobj, 100))
            {
                try
                {
                    vfw.Close();
                    vfw.Dispose();
                }
                finally
                {
                    // release the lock
                    Monitor.Exit(lockobj);
                }
            }
            else
            {
                Output.WriteLine("Unable to close video stream!");
            }
        }
    }

    private Bitmap AddImageOverlay(Bitmap image, DateTime triggerTime, TimeSpan offset)
    {
		Bitmap b = (Bitmap)image.Clone();

        using (Graphics g = Graphics.FromImage(b))
        {
            // add Triumph logo to the image
            g.DrawImage(logo, logoPoint);

            // add common timestamp background
            g.FillRectangle(sb_black, rect);

            // add measurement timestamp to the image
            // we cannot read Measurement.CurrentTime from here, so we calculate
            // the time between trigger and current, and then add on the
            // measurement time at the trigger point
            TimeSpan diff = (DateTime.Now - triggerTime) + offset;
            g.DrawString(diff.TotalSeconds.ToString("00000.000"), drawFont, sb_white, timePoint, sf);
        }

        return b;
    }

    private void PrintCameraCapabilities()
    {
        int min;
        int max;
        int step;
        int def;
        int val;
        CameraControlFlags ccf;
        VideoProcAmpFlags vpaf;

        Output.WriteLine("Camera Properties:");
        foreach (CameraControlProperty ccp in Enum.GetValues(typeof(CameraControlProperty)))
        {
            videoSource.GetCameraPropertyRange(ccp, out min, out max, out step, out def, out ccf);
            videoSource.GetCameraProperty(ccp, out val, out ccf);

            Output.WriteLine(ccp.ToString() + " : min({0}), max({1}), step({2}), default({3}), value({4}), CCF({5})", min, max, step, def, val, ccf.ToString());
        }

        Output.WriteLine("Video Properties:");
        foreach (VideoProcAmpProperty vpap in Enum.GetValues(typeof(VideoProcAmpProperty)))
        {
            videoSource.GetVideoPropertyRange(vpap, out min, out max, out step, out def, out vpaf);
            videoSource.GetVideoProperty(vpap, out val, out vpaf);

            Output.WriteLine(vpap.ToString() + " : min({0}), max({1}), step({2}), default({3}), value({4}), CCF({5})", min, max, step, def, val, vpaf.ToString());
        }
    }
}
