using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using Properties;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using Vector.CANoe.Runtime;
using Vector.Tools;

public class WebCam : MeasurementScript, IDisposable
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
                if (videoDevices[i].Name.Equals(WebCamSysVar.PreferredCamera.Name.Value, StringComparison.InvariantCulture))
                {
                    // camera name matches name of preferred camera
                    preferredIdx = i;
                }
            }

            if (preferredIdx == -1)
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
                if ((videoSource.VideoCapabilities[i].FrameSize.Height == WebCamSysVar.PreferredCamera.Height.Value) && (videoSource.VideoCapabilities[i].FrameSize.Width == WebCamSysVar.PreferredCamera.Width.Value))
                {
                    matchIdx = i;
                }
            }

            // if we matched the preferred resolution use that one, else use max supported
            if (matchIdx != -1)
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
            ProcessCameraCapabilities();
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
                    vfw = null;
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

    #region Frame Handlers
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
                switch (Path.GetExtension(SnapShotName).ToUpper(CultureInfo.InvariantCulture))
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
            g.DrawString(diff.TotalSeconds.ToString("00000.000", CultureInfo.InvariantCulture), drawFont, sb_white, timePoint, sf);
        }

        return b;
    }
    #endregion

    [OnChange(typeof(WebCamSysVar.CameraProperties.DXbutton_Type))]
    public void DXPanelHandler()
    {
        if (WebCamSysVar.CameraProperties.DXbutton.Value == WebCamSysVar.CameraProperties.DXbutton.Released)
        {
            if (videoSource.IsRunning)
            {
                videoSource.DisplayPropertyPage(IntPtr.Zero);
            }
        }
    }

    #region Camera Capability
    private void ProcessCameraCapabilities()
    {
        ProcessCameraControlProperty(CameraControlProperty.Pan,
            WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Pan.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Flag.Value);
        ProcessCameraControlProperty(CameraControlProperty.Tilt,
            WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Flag.Value);
        ProcessCameraControlProperty(CameraControlProperty.Roll,
            WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Roll.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Flag.Value);
        ProcessCameraControlProperty(CameraControlProperty.Zoom,
            WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Flag.Value);
        ProcessCameraControlProperty(CameraControlProperty.Exposure,
            WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Flag.Value);
        ProcessCameraControlProperty(CameraControlProperty.Focus,
            WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Focus.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Flag.Value);

        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Brightness,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Contrast,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Hue,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Saturation,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Sharpness,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gamma,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.ColorEnable,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.WhiteBalance,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.BacklightCompensation,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Flag.Value);
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gain,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Flag.Value);
    }

    private void ProcessCameraControlProperty(CameraControlProperty ccp, uint or_flag, int value, CameraControlFlags flag)
    {
        int val;
        CameraControlFlags ccf;

        if (or_flag == 1)
        {
            Output.WriteLine("Setting \"{0}\" to user setting ({1}, \"{2}\")", ccp, value, flag);
            videoSource.SetCameraProperty(ccp, value, flag);
        }
        else
        {
            int min;
            int max;
            int step;
            int def;
            videoSource.GetCameraPropertyRange(ccp, out min, out max, out step, out def, out ccf);
            Output.WriteLine("Setting \"{0}\" to default setting ({1}, \"{2}\")", ccp, def, ccf);
            videoSource.SetCameraProperty(ccp, def, ccf);
        }

        videoSource.GetCameraProperty(ccp, out val, out ccf);
        Output.WriteLine("{0} : value({1}), CCF({2})", ccp, val, ccf);
    }

    private void ProcessVideoProcAmpProperty(VideoProcAmpProperty vpap, uint or_flag, int value, VideoProcAmpFlags flag)
    {
        int val;
        VideoProcAmpFlags vpaf;

        if (or_flag == 1)
        {
            Output.WriteLine("Setting \"{0}\" to user setting ({1}, \"{2}\")", vpap, value, flag);
            videoSource.SetVideoProperty(vpap, value, flag);
        }
        else
        {
            int min;
            int max;
            int step;
            int def;
            videoSource.GetVideoPropertyRange(vpap, out min, out max, out step, out def, out vpaf);
            Output.WriteLine("Setting \"{0}\" to default setting ({1}, \"{2}\")", vpap, def, vpaf);
            videoSource.SetVideoProperty(vpap, def, vpaf);
        }

        videoSource.GetVideoProperty(vpap, out val, out vpaf);
        Output.WriteLine("{0} : value({1}), CCF({2})", vpap, val, vpaf);
    }
    #endregion

    #region CameraControlProperty Handlers
    #region Exposure Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Exposure_Type.Flag_Type))]
    public void ExposureFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Exposure,
                WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Exposure_Type.Override_Type))]
    public void ExposureOverrideHandler()
    {
        ProcessCameraControlProperty(CameraControlProperty.Exposure,
            WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Exposure_Type.UserValue_Type))]
    public void ExposureValueHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Exposure,
                WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Exposure.Flag.Value);
        }
    }
    #endregion

    #region Focus Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Focus_Type.Flag_Type))]
    public void FocusFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Focus,
                WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Focus.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Focus_Type.Override_Type))]
    public void FocusOverrideHandler()
    {
        ProcessCameraControlProperty(CameraControlProperty.Focus,
            WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Focus.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Focus_Type.UserValue_Type))]
    public void FocusValueHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Focus,
                WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Focus.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Focus.Flag.Value);
        }
    }
    #endregion

    #region Pan Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Pan_Type.Flag_Type))]
    public void PanFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Pan,
                WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Pan.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Pan_Type.Override_Type))]
    public void PanOverrideHandler()
    {
        ProcessCameraControlProperty(CameraControlProperty.Pan,
            WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Pan.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Pan_Type.UserValue_Type))]
    public void PanValueHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Pan,
                WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Pan.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Pan.Flag.Value);
        }
    }
    #endregion

    #region Roll Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Roll_Type.Flag_Type))]
    public void RollFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Roll,
                WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Roll.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Roll_Type.Override_Type))]
    public void RollOverrideHandler()
    {
        ProcessCameraControlProperty(CameraControlProperty.Roll,
           WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.Value,
           WebCamSysVar.CameraProperties.CameraControlProperty.Roll.UserValue.Value,
           (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Roll_Type.UserValue_Type))]
    public void RollValueHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Roll,
                WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Roll.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Roll.Flag.Value);
        }
    }
    #endregion

    #region Tilt Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Tilt_Type.Flag_Type))]
    public void TiltFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Tilt,
                WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Tilt_Type.Override_Type))]
    public void TiltOverrideHandler()
    {
        ProcessCameraControlProperty(CameraControlProperty.Tilt,
            WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Tilt_Type.UserValue_Type))]
    public void TiltValueHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Tilt,
                WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Tilt.Flag.Value);
        }
    }
    #endregion

    #region Zoom Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Zoom_Type.Flag_Type))]
    public void ZoomFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Zoom,
                WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Zoom_Type.Override_Type))]
    public void ZoomOverrideHandler()
    {
        ProcessCameraControlProperty(CameraControlProperty.Zoom,
            WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.Value,
            WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.UserValue.Value,
            (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.CameraControlProperty_Type.Zoom_Type.UserValue_Type))]
    public void ZoomValueHandler()
    {
        if (WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.Value == WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.UserValue)
        {
            ProcessCameraControlProperty(CameraControlProperty.Zoom,
                WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Override.Value,
                WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.UserValue.Value,
                (CameraControlFlags)WebCamSysVar.CameraProperties.CameraControlProperty.Zoom.Flag.Value);
        }
    }
    #endregion
    #endregion

    #region VideoProcAmpProperty Handlers
    #region BacklightCompensation Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.BacklightCompensation_Type.Flag_Type))]
    public void BacklightCompensationFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.BacklightCompensation,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.BacklightCompensation_Type.Override_Type))]
    public void BacklightCompensationOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.BacklightCompensation,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.BacklightCompensation_Type.UserValue_Type))]
    public void BacklightCompensationValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.BacklightCompensation,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.BacklightCompensation.Flag.Value);
        }
    }
    #endregion

    #region Brightness Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Brightness_Type.Flag_Type))]
    public void BrightnessFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Brightness,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Brightness_Type.Override_Type))]
    public void BrightnessOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Brightness,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Brightness_Type.UserValue_Type))]
    public void BrightnessValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Brightness,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Brightness.Flag.Value);
        }
    }
    #endregion

    #region ColorEnable Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.ColorEnable_Type.Flag_Type))]
    public void ColorEnableFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.ColorEnable,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.ColorEnable_Type.Override_Type))]
    public void ColorEnableOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.ColorEnable,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.ColorEnable_Type.UserValue_Type))]
    public void ColorEnableValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.ColorEnable,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.ColorEnable.Flag.Value);
        }
    }
    #endregion

    #region Contrast Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Contrast_Type.Flag_Type))]
    public void ContrastFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Contrast,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Contrast_Type.Override_Type))]
    public void ContrastOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Contrast,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Contrast_Type.UserValue_Type))]
    public void ContrastValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Contrast,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Contrast.Flag.Value);
        }
    }
    #endregion

    #region Gain Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Gain_Type.Flag_Type))]
    public void GainFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gain,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Gain_Type.Override_Type))]
    public void GainOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gain,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Gain_Type.UserValue_Type))]
    public void GainValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gain,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gain.Flag.Value);
        }
    }
    #endregion

    #region Gamma Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Gamma_Type.Flag_Type))]
    public void GammaFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gamma,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Gamma_Type.Override_Type))]
    public void GammaOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gamma,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Gamma_Type.UserValue_Type))]
    public void GammaValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Gamma,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Gamma.Flag.Value);
        }
    }
    #endregion

    #region Hue Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Hue_Type.Flag_Type))]
    public void HueFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Hue,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Hue_Type.Override_Type))]
    public void HueOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Hue,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Hue_Type.UserValue_Type))]
    public void HueValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Hue,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Hue.Flag.Value);
        }
    }
    #endregion

    #region Saturation Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Saturation_Type.Flag_Type))]
    public void SaturationFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Saturation,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Saturation_Type.Override_Type))]
    public void SaturationOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Saturation,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Saturation_Type.UserValue_Type))]
    public void SaturationValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Saturation,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Saturation.Flag.Value);
        }
    }
    #endregion

    #region Sharpness Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Sharpness_Type.Flag_Type))]
    public void SharpnessFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Sharpness,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Sharpness_Type.Override_Type))]
    public void SharpnessOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.Sharpness,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.Sharpness_Type.UserValue_Type))]
    public void SharpnessValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.Sharpness,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.Sharpness.Flag.Value);
        }
    }
    #endregion

    #region WhiteBalance Handlers
    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.WhiteBalance_Type.Flag_Type))]
    public void WhiteBalanceFlagHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.WhiteBalance,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Flag.Value);
        }
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.WhiteBalance_Type.Override_Type))]
    public void WhiteBalanceOverrideHandler()
    {
        ProcessVideoProcAmpProperty(VideoProcAmpProperty.WhiteBalance,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.Value,
            WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.UserValue.Value,
            (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Flag.Value);
    }

    [OnChange(typeof(WebCamSysVar.CameraProperties.VideoProcAmpProperty_Type.WhiteBalance_Type.UserValue_Type))]
    public void WhiteBalanceValueHandler()
    {
        if (WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.Value == WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.UserValue)
        {
            ProcessVideoProcAmpProperty(VideoProcAmpProperty.WhiteBalance,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Override.Value,
                WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.UserValue.Value,
                (VideoProcAmpFlags)WebCamSysVar.CameraProperties.VideoProcAmpProperty.WhiteBalance.Flag.Value);
        }
    }
    #endregion
    #endregion

    #region IDisposable Support
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO: dispose managed state (managed objects).
            if (drawFont != null)
            {
                drawFont.Dispose();
            }

            if (sb_black != null)
            {
                sb_black.Dispose();
            }

            if (sb_white != null)
            {
                sb_white.Dispose();
            }

            if (vfw != null)
            {
                vfw.Close();
                vfw.Dispose();
                vfw = null;
            }

            if (logo != null)
            {
                logo.Dispose();
            }

            if (sf != null)
            {
                sf.Dispose();
            }

            if (videoSource != null)
            {
                videoSource.Dispose();
            }
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~WebCam() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        GC.SuppressFinalize(this);
    }
    #endregion
}
