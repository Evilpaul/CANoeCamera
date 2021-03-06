﻿// AForge Direct Show Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2013
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

using AForge.Video.DirectShow;

namespace AForge.Video.DirectShow
{
    /// <summary>
    /// Local video device selection form.
    /// </summary>
    /// 
    /// <remarks><para>The form provides a standard way of selecting local video
    /// device (USB web camera, capture board, etc. - anything supporting DirectShow
    /// interface), which can be reused across applications. It allows selecting video
    /// device, video size and snapshots size (if device supports snapshots and
    /// <see cref="ConfigureSnapshots">user needs them</see>).</para>
    /// 
    /// <para><img src="img/video/VideoCaptureDeviceForm.png" width="478" height="205" /></para>
    /// </remarks>
    /// 
    public partial class VideoCaptureDeviceForm : Form
    {
        // collection of available video devices
        private readonly FilterInfoCollection videoDevices;

        // supported capabilities of video and snapshots
        private readonly Dictionary<string, VideoCapabilities> videoCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>( );
        private readonly Dictionary<string, VideoCapabilities> snapshotCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>( );

        // available video inputs
        private VideoInput[] availableVideoInputs = null;

        // flag telling if user wants to configure snapshots as well
        private bool configureSnapshots = false;

        /// <summary>
        /// Specifies if snapshot configuration should be done or not.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if the dialog form should
        /// allow configuration of snapshot sizes (if selected video source supports
        /// snapshots). If the property is set to <see langword="true"/>, then
        /// the form will provide additional combo box enumerating supported
        /// snapshot sizes. Otherwise the combo boxes will be hidden.
        /// </para>
        /// 
        /// <para>If the property is set to <see langword="true"/> and selected
        /// device supports snapshots, then <see cref="VideoCaptureDevice.ProvideSnapshots"/>
        /// property of the <see cref="VideoDevice">configured device</see> is set to
        /// <see langword="true"/>.</para>
        /// 
        /// <para>Default value of the property is set to <see langword="false"/>.</para>
        /// </remarks>
        /// 
        public bool ConfigureSnapshots
        {
            get { return configureSnapshots; }
            set
            {
                configureSnapshots = value;
                snapshotsLabel.Visible = value;
                snapshotResolutionsCombo.Visible = value;
            }
        }

        /// <summary>
        /// Provides configured video device.
        /// </summary>
        /// 
        /// <remarks><para>The property provides configured video device if user confirmed
        /// the dialog using "OK" button. If user canceled the dialog, the property is
        /// set to <see langword="null"/>.</para></remarks>
        /// 
        public VideoCaptureDevice VideoDevice { get; private set; }

        private Size captureSize = new Size( 0, 0 );
        private Size snapshotSize = new Size( 0, 0 );

        /// <summary>
        /// Moniker string of the selected video device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get moniker string of the selected device
        /// on form completion or set video device which should be selected by default on
        /// form loading.</para></remarks>
        /// 
        public string VideoDeviceMoniker { get; set; } = string.Empty;

        /// <summary>
        /// Video frame size of the selected device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get video size of the selected device
        /// on form completion or set the size to be selected by default on form loading.</para>
        /// </remarks>
        /// 
        public Size CaptureSize
        {
            get { return captureSize; }
            set { captureSize = value; }
        }

        /// <summary>
        /// Snapshot frame size of the selected device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get snapshot size of the selected device
        /// on form completion or set the size to be selected by default on form loading
        /// (if <see cref="ConfigureSnapshots"/> property is set <see langword="true"/>).</para>
        /// </remarks>
        public Size SnapshotSize
        {
            get { return snapshotSize; }
            set { snapshotSize = value; }
        }

        /// <summary>
        /// Video input to use with video capture card.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get video input of the selected device
        /// on form completion or set it to be selected by default on form loading.</para></remarks>
        /// 
        public VideoInput VideoInput { get; set; } = VideoInput.Default;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoCaptureDeviceForm"/> class.
        /// </summary>
        /// 
        public VideoCaptureDeviceForm( )
        {
            InitializeComponent( );
            ConfigureSnapshots = false;

            // show device list
			try
			{
                // enumerate video devices
                videoDevices = new FilterInfoCollection( FilterCategory.VideoInputDevice );

                if ( videoDevices.Count == 0 )
                    throw new ApplicationException( );

                // add all devices to combo
                foreach ( FilterInfo device in videoDevices )
                {
                    devicesCombo.Items.Add( device.Name );
                }
            }
            catch ( ApplicationException )
            {
                devicesCombo.Items.Add( "No local capture devices" );
                devicesCombo.Enabled = false;
                okButton.Enabled = false;
            }
        }

        // On form loaded
        private void VideoCaptureDeviceForm_Load( object sender, EventArgs e )
        {
            int selectedCameraIndex = 0;

            for ( int i = 0; i < videoDevices.Count; i++ )
            {
                if ( VideoDeviceMoniker == videoDevices[i].MonikerString )
                {
                    selectedCameraIndex = i;
                    break;
                }
            }

            devicesCombo.SelectedIndex = selectedCameraIndex;
        }

        // Ok button clicked
        private void OkButton_Click( object sender, EventArgs e )
        {
            VideoDeviceMoniker = VideoDevice.Source;

            // set video size
            if ( videoCapabilitiesDictionary.Count != 0 )
            {
                VideoCapabilities caps = videoCapabilitiesDictionary[(string) videoResolutionsCombo.SelectedItem];

                VideoDevice.VideoResolution = caps;
                captureSize = caps.FrameSize;
            }

            if ( configureSnapshots )
            {
                // set snapshots size
                if ( snapshotCapabilitiesDictionary.Count != 0 )
                {
                    VideoCapabilities caps = snapshotCapabilitiesDictionary[(string) snapshotResolutionsCombo.SelectedItem];

                    VideoDevice.ProvideSnapshots = true;
                    VideoDevice.SnapshotResolution = caps;

                    snapshotSize = caps.FrameSize;
                }
            }

            if ( availableVideoInputs.Length != 0 )
            {
                VideoInput = availableVideoInputs[videoInputsCombo.SelectedIndex];
                VideoDevice.CrossbarVideoInput = VideoInput;
            }
        }

        // New video device is selected
        private void DevicesCombo_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( videoDevices.Count != 0 )
            {
                VideoDevice = new VideoCaptureDevice( videoDevices[devicesCombo.SelectedIndex].MonikerString );
                EnumeratedSupportedFrameSizes( VideoDevice );
            }
        }

        // Collect supported video and snapshot sizes
        private void EnumeratedSupportedFrameSizes( VideoCaptureDevice videoDevice )
        {
            this.Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear( );
            snapshotResolutionsCombo.Items.Clear( );
            videoInputsCombo.Items.Clear( );

            videoCapabilitiesDictionary.Clear( );
            snapshotCapabilitiesDictionary.Clear( );

            try
            {
                // collect video capabilities
                VideoCapabilities[] videoCapabilities = videoDevice.GetVideoCapabilities();
                int videoResolutionIndex = 0;

                foreach ( VideoCapabilities capabilty in videoCapabilities )
                {
                    string item = string.Format(CultureInfo.InvariantCulture,
                        "{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height );

                    if ( !videoResolutionsCombo.Items.Contains( item ) )
                    {
                        if ( captureSize == capabilty.FrameSize )
                        {
                            videoResolutionIndex = videoResolutionsCombo.Items.Count;
                        }

                        videoResolutionsCombo.Items.Add( item );
                    }

                    if ( !videoCapabilitiesDictionary.ContainsKey( item ) )
                    {
                        videoCapabilitiesDictionary.Add( item, capabilty );
                    }
                }

                if ( videoCapabilities.Length == 0 )
                {
                    videoResolutionsCombo.Items.Add( "Not supported" );
                }

                videoResolutionsCombo.SelectedIndex = videoResolutionIndex;


                if ( configureSnapshots )
                {
                    // collect snapshot capabilities
                    VideoCapabilities[] snapshotCapabilities = videoDevice.GetSnapshotCapabilities();
                    int snapshotResolutionIndex = 0;

                    foreach ( VideoCapabilities capabilty in snapshotCapabilities )
                    {
                        string item = string.Format(CultureInfo.InvariantCulture,
                            "{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height );

                        if ( !snapshotResolutionsCombo.Items.Contains( item ) )
                        {
                            if ( snapshotSize == capabilty.FrameSize )
                            {
                                snapshotResolutionIndex = snapshotResolutionsCombo.Items.Count;
                            }

                            snapshotResolutionsCombo.Items.Add( item );
                            snapshotCapabilitiesDictionary.Add( item, capabilty );
                        }
                    }

                    if ( snapshotCapabilities.Length == 0 )
                    {
                        snapshotResolutionsCombo.Items.Add( "Not supported" );
                    }

                    snapshotResolutionsCombo.SelectedIndex = snapshotResolutionIndex;
                }

                // get video inputs
                availableVideoInputs = videoDevice.GetAvailableCrossbarVideoInputs();
                int videoInputIndex = 0;

                foreach ( VideoInput input in availableVideoInputs )
                {
                    string item = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", input.Index, input.Type );

                    if ( ( input.Index == VideoInput.Index ) && ( input.Type == VideoInput.Type ) )
                    {
                        videoInputIndex = videoInputsCombo.Items.Count;
                    }

                    videoInputsCombo.Items.Add( item );
                }

                if ( availableVideoInputs.Length == 0 )
                {
                    videoInputsCombo.Items.Add( "Not supported" );
                }

                videoInputsCombo.SelectedIndex = videoInputIndex;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}
