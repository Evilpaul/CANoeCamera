// Snapshot Maker sample application
// AForge.NET Framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//

using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;

namespace SnapshotMaker
{
    public partial class MainForm : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private VideoCapabilities[] videoCapabilities;
        private VideoCapabilities[] snapshotCapabilities;

        private SnapshotForm snapshotForm = null;

        private delegate void AsyncMethodCaller(Bitmap frame);
        private AsyncMethodCaller caller;
        private bool videoRequested = false;
        private readonly object lockobj = new object();
        private VideoFileWriter vfw;

        public MainForm( )
        {
            InitializeComponent( );
        }

        // Main form is loaded
        private void MainForm_Load( object sender, EventArgs e )
        {
            // enumerate video devices
            videoDevices = new FilterInfoCollection( FilterCategory.VideoInputDevice );

            if ( videoDevices.Count != 0 )
            {
                // add all devices to combo
                foreach ( FilterInfo device in videoDevices )
                {
                    devicesCombo.Items.Add( device.Name );
                }
            }
            else
            {
                devicesCombo.Items.Add( "No DirectShow devices found" );
            }

            devicesCombo.SelectedIndex = 0;

            EnableConnectionControls( true );
        }

        // Closing the main form
        private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
        {
            Disconnect( );
        }

        // Enable/disable connection related controls
        private void EnableConnectionControls( bool enable )
        {
            devicesCombo.Enabled = enable;
            videoResolutionsCombo.Enabled = enable;
            snapshotResolutionsCombo.Enabled = enable;
            connectButton.Enabled = enable;
            disconnectButton.Enabled = !enable;
            triggerButton.Enabled = ( !enable ) && ( snapshotCapabilities.Length != 0 );
            settingsButton.Enabled = !enable;
            videoButton.Enabled = !enable;
        }

        // New video device is selected
        private void DevicesCombo_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( videoDevices.Count != 0 )
            {
                videoDevice = new VideoCaptureDevice( videoDevices[devicesCombo.SelectedIndex].MonikerString );
                EnumeratedSupportedFrameSizes( videoDevice );
            }
        }

        // Collect supported video and snapshot sizes
        private void EnumeratedSupportedFrameSizes( VideoCaptureDevice videoDevice )
        {
            Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear( );
            snapshotResolutionsCombo.Items.Clear( );

            try
            {
                videoCapabilities = videoDevice.VideoCapabilities;
                snapshotCapabilities = videoDevice.SnapshotCapabilities;

                foreach ( VideoCapabilities capabilty in videoCapabilities )
                {
                    videoResolutionsCombo.Items.Add( string.Format(CultureInfo.InvariantCulture, "{0} x {1}",
                        capabilty.FrameSize.Width, capabilty.FrameSize.Height ) );
                }

                foreach ( VideoCapabilities capabilty in snapshotCapabilities )
                {
                    snapshotResolutionsCombo.Items.Add( string.Format(CultureInfo.InvariantCulture, "{0} x {1}",
                        capabilty.FrameSize.Width, capabilty.FrameSize.Height ) );
                }

                if ( videoCapabilities.Length == 0 )
                {
                    videoResolutionsCombo.Items.Add( "Not supported" );
                }
                if ( snapshotCapabilities.Length == 0 )
                {
                    snapshotResolutionsCombo.Items.Add( "Not supported" );
                }

                videoResolutionsCombo.SelectedIndex = 0;
                snapshotResolutionsCombo.SelectedIndex = 0;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // On "Connect" button clicked
        private void ConnectButton_Click( object sender, EventArgs e )
        {
            if ( videoDevice != null )
            {
                if ( ( videoCapabilities != null ) && ( videoCapabilities.Length != 0 ) )
                {
                    videoDevice.VideoResolution = videoCapabilities[videoResolutionsCombo.SelectedIndex];
                }

                if ( ( snapshotCapabilities != null ) && ( snapshotCapabilities.Length != 0 ) )
                {
                    videoDevice.ProvideSnapshots = true;
                    videoDevice.SnapshotResolution = snapshotCapabilities[snapshotResolutionsCombo.SelectedIndex];
                    videoDevice.SnapshotFrame += new NewFrameEventHandler( VideoDevice_SnapshotFrame );
                }

                EnableConnectionControls( false );

                videoSourcePlayer.VideoSource = videoDevice;

                videoDevice.NewFrame += new NewFrameEventHandler(Video_NewFrame);
                caller = SaveVideo;

                videoSourcePlayer.Start( );
            }
        }

        // On "Disconnect" button clicked
        private void DisconnectButton_Click( object sender, EventArgs e )
        {
            Disconnect( );
        }

        // Disconnect from video device
        private void Disconnect( )
        {
            if ( videoSourcePlayer.VideoSource != null )
            {
                StopVideo();

                // stop video device
                videoSourcePlayer.SignalToStop( );
                videoSourcePlayer.WaitForStop( );
                videoSourcePlayer.VideoSource = null;

                if ( videoDevice.ProvideSnapshots )
                {
                    videoDevice.SnapshotFrame -= new NewFrameEventHandler( VideoDevice_SnapshotFrame );
                }

                EnableConnectionControls( true );
            }
        }

        // Simulate snapshot trigger
        private void TriggerButton_Click( object sender, EventArgs e )
        {
            if ( ( videoDevice != null ) && ( videoDevice.ProvideSnapshots ) )
            {
                videoDevice.SimulateTrigger( );
            }
        }

        // New snapshot frame is available
        private void VideoDevice_SnapshotFrame( object sender, NewFrameEventArgs eventArgs )
        {
            Console.WriteLine( eventArgs.Frame.Size );

            ShowSnapshot( (Bitmap) eventArgs.Frame.Clone( ) );
        }

        private void ShowSnapshot( Bitmap snapshot )
        {
            if ( InvokeRequired )
            {
                Invoke( new Action<Bitmap>( ShowSnapshot ), snapshot );
            }
            else
            {
                if ( snapshotForm == null )
                {
                    snapshotForm = new SnapshotForm( );
                    snapshotForm.FormClosed += new FormClosedEventHandler( SnapshotForm_FormClosed );
                    snapshotForm.Show( );
                }

                snapshotForm.SetImage( snapshot );
            }
        }

        private void SnapshotForm_FormClosed( object sender, FormClosedEventArgs e )
        {
            snapshotForm = null;
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            if (videoDevice != null)
            {
                videoDevice.DisplayPropertyPage(IntPtr.Zero);
            }
        }

        private void VideoButton_Click(object sender, EventArgs e)
        {
            if (videoDevice != null)
            {
                if (videoRequested == false)
                {
                    StopVideo();

                    string filename;
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "video files|*.mp4;*.mkv;*.avi;*.mpg|All files (*.*)|*.*";
                    if(sfd.ShowDialog() == DialogResult.OK)
                    {
                        filename = sfd.FileName;
                    }
                    else
                    {
                        return;
                    }

                    try
                    {
                        vfw = new VideoFileWriter();
                        vfw.Open(filename, videoDevice.VideoResolution.FrameSize.Width, videoDevice.VideoResolution.FrameSize.Height, videoDevice.VideoResolution.AverageFrameRate, VideoCodec.Default, 800000);
                        videoRequested = true;
                        videoState.Visible = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    StopVideo();
                }
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
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private void SaveVideo(Bitmap frame)
        {
            if (videoRequested)
            {
                // If we cannot get the lock, skip the frame
                // should only happen if a stop has been requested, or processing of previous frame takes too long
                if (Monitor.TryEnter(lockobj))
                {
                    try
                    {
                        vfw.WriteVideoFrame(frame);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                    }
                    finally
                    {
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
                        videoState.Visible = false;
                        // release the lock
                        Monitor.Exit(lockobj);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Unable to close video stream!");
                }
            }
        }
    }
}
