# CANoeCamera
Vector CANoe configuration integrating Image/Video capture using [AForge.NET](http://aforgenet.com/).
Compatible with any camera that interfaces with DirectShow on Microsoft Windows.

Based upon the latest AForge.NET [code](https://github.com/andrewkirillov/AForge.NET) with the following modifications:

 - Update projects to target .NET Framework 4 Client Profile
 - Integrated IAMVideoProcAmp change ([details](https://code.google.com/archive/p/aforge/issues/357))
 - Updated FFMPEG to release 3.4.2 ([build](https://ffmpeg.zeranoe.com/builds/))
 - Backported code from [Accord.NET](http://accord-framework.net/index.html) to allow AForge.Video.FFMPEG to integrate with newer versions of FFMPEG
 - Removed parts of AForge.NET that are not required for just Image/Video capture

Writing to the System Variables in the CANoe configuration allows for control of the camera interface:

| System Variable  | Description           |
| ------- | ---------------- |
| CurrentCamera  | The friendly name of the camera currently being used |
| PreferredCamera | The friendly name of the preferred camera, if it is not available then the first camera shall be used |
| PreferredHeight | The preferred image/video height to capture, if it does not match a supported resolution then the largest available resolution shall be used |
| PreferredWidth | The preferred image/video width to capture, if it does not match a supported resolution then the largest available resolution shall be used |
| SnapShotFileName | The filename of an image to be saved, updating this variable trigger saving the next available video frame as an image. File format is judged based upon file extension. |
| VideoBitRate | The bitrate to be used when saving video data |
| VideoFileName | The filename of a video to be saved. FFMPEG uses the default codec based upon the file extension |
| VideoState | Toggle between Stopped (0) and Running (1) in order to begin or end video recording |
