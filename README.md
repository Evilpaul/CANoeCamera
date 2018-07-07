
# CANoeCamera
Vector CANoe configuration integrating Image/Video capture using [AForge.NET](http://aforgenet.com/).
Compatible with any camera that interfaces with DirectShow on Microsoft Windows.

Based upon the latest AForge.NET [code](https://github.com/andrewkirillov/AForge.NET) with the following modifications:

 - Update projects to target .NET Framework 4 Client Profile
 - Integrated IAMVideoProcAmp change ([details](https://code.google.com/archive/p/aforge/issues/357))
 - Updated FFMPEG to release 4.0 ([build](https://ffmpeg.zeranoe.com/builds/))
 - Backported code from [Accord.NET](http://accord-framework.net/index.html) to allow AForge.Video.FFMPEG to integrate with newer versions of FFMPEG
 - Removed parts of AForge.NET that are not required for just Image/Video capture

Writing to the System Variables in the CANoe configuration allows for control of the camera interface:

| System Variable | Description |
| ------- | ---------------- |
| CameraProperties | Structure containing the following elements:<br/><ul><li>CameraControlProperty<ul><li>Pan<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Tilt<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Roll<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Zoom<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Exposure<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Focus<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li></ul></li><li>VideoProcAmpProperty<ul><li>Brightness<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Contrast<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Hue<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Saturation<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Sharpness<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Gamma<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>ColorEnable<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>WhiteBalance<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>BacklightCompensation<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li><li>Gain<ul><li>Override: flag to force user or default values</li><li>Flag: flag to select manual or auto control</li><li>UserValue: value to set when forcing user value</li></ul></li></ul></li><li>DXbutton: map to a panel button to enable showing the default DirectShow Control Panel when connected to a camera</li></ul><br/></br>**Note:** The minimum, maximum and default values are set for use with a Microsoft® LifeCam Studio(TM); if you use a different camera then update these values to reflect what is appropriate for your camera! |
| CurrentCamera  | The friendly name of the camera currently being used |
| PreferredCamera | Structure containing the following elements:<br/><ul><li>Name: The friendly name of the preferred camera, if it is not available then the first camera shall be used</li><li>Height: The preferred image/video height to capture, if it does not match a supported resolution then the largest available resolution shall be used</li><li>Width: The preferred image/video width to capture, if it does not match a supported resolution then the largest available resolution shall be used</li></ul> |
| SnapShotFileName | The filename of an image to be saved, updating this variable trigger saving the next available video frame as an image. File format is judged based upon file extension. |
| VideoBitRate | The bitrate to be used when saving video data |
| VideoFileName | The filename of a video to be saved. FFMPEG uses the default codec based upon the file extension |
| VideoState | Toggle between Stopped (0) and Running (1) in order to begin or end video recording |

**Note:** If WebCam is added to an existing CANoe configuration then the name of the sysvar dll referenced in WebCam/WebCam.csproj will need to be modified to reflect the name of the new CANoe configuration name.

Sysvars can be added to an existing project by importing WebCam/Canoe/WebCam.vsysvar


**Info:** This node uses a callback whenever a new frame is available from the camera which can have some unintended behaviour:
 - If the exposure value is set too long then you will not receive the expected frame rate from the camera which will cause any recorded video to playback too fast
 - If the exposure is set to Auto and the lighting enviornment changes then any recorded video may contain time dilation due the the exposure dynamically modifying the frame rate
