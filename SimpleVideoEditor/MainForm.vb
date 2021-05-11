Imports System.IO
Imports System.IO.Pipes
Imports System.Text.RegularExpressions
Imports System.Threading

Public Class MainForm
    'FFMPEG Usefull Commands
    'https://www.labnol.org/internet/useful-ffmpeg-commands/28490/

    Private mstrVideoPath As String = "" 'Fullpath of the video file being edited
    Private mproFfmpegProcess As Process 'TODO This doesn't really need to be module level
    Private mthdDefaultLoadThread As System.Threading.Thread 'Thread for loading images upon open
    Private mobjGenericToolTip As ToolTip = New ToolTip 'Tooltip object required for setting tootips on controls

    Private mptStartCrop As New Point(0, 0) 'Point for the top left of the crop rectangle
    Private mptEndCrop As New Point(0, 0) 'Point for the bottom right of the crop rectangle

    Private mdblVideoDurationSS As Double

    Private mintAspectWidth As Integer 'Holds onto the width of the video frame for aspect ration computation(Not correct width, but correct aspect)
    Private mintAspectHeight As Integer 'Holds onto the height of the video frame for aspect ration computation(Not correct height, but correct aspect)

    Private mdblScaleFactorX As Double 'Keeps track of the width scale for the image display so that cropping can work with the right size
    Private mdblScaleFactorY As Double 'Keeps track of the height scale for the image display so that cropping can work with the right size

    Private mintFrameRate As Double = 30 'Number of frames per second in the video
    Private mintCurrentFrame As Integer = 0 'Current visible frame in the big picVideo control
    Private mintDisplayInfo As Integer = 0 'Timer value for how long to render special info to the main image
    Private Const RENDER_DECAY_TIME As Integer = 2000

    Private WithEvents mobjMetaData As VideoData 'Video metadata, including things like resolution, framerate, bitrate, etc.
    Private mobjRotation As System.Drawing.RotateFlipType = RotateFlipType.RotateNoneFlipNone 'Keeps track of how the user wants to rotate the image
    Private mblnUserInjection As Boolean = False 'Keeps track of if the user wants to manually modify the resulting commands
    Private mdblPlaybackSpeed As Double = 1
    Private mdblPlaybackVolume As Double = 1

    Private Class SpecialOutputProperties
        Public Decimate As Boolean
        Public FPS As Integer
        Public ChromaKey As Color
        Public PlaybackSpeed As Double
        Public PlaybackVolume As Double
        Public QScale As Double
    End Class

#Region "File Events"
    ''' <summary>
    ''' Opens the file dialog to search for a video.
    ''' </summary>
    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        ofdVideoIn.Filter = "Video Files (*.*)|*.*"
        ofdVideoIn.Title = "Select Video File"
        ofdVideoIn.AddExtension = True
        ofdVideoIn.ShowDialog()
    End Sub

    ''' <summary>
    ''' Load a file when a file is opened in the open file dialog.
    ''' </summary>
    Private Sub ofdVideoIn_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ofdVideoIn.FileOk
        Try
            ClearControls()
            LoadFile(ofdVideoIn.FileName)
        Catch ex As Exception
            MessageBox.Show(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub

    ''' <summary>
    ''' File is opened, load in the images, and the file attributes.
    ''' </summary>
    Public Sub LoadFile(ByVal fullPath As String)
        mstrVideoPath = fullPath
        lblFileName.Text = System.IO.Path.GetFileName(mstrVideoPath)
        If mobjMetaData IsNot Nothing Then
            mobjMetaData.Dispose()
            mobjMetaData = Nothing
        End If
        mobjMetaData = VideoData.FromFile(mstrVideoPath)
        mobjGenericToolTip.SetToolTip(lblFileName, "Name of the currently loaded file." & vbNewLine & mobjMetaData.StreamData)
        'Set frame rate
        mintFrameRate = mobjMetaData.Framerate

        RemoveHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_RangeChanged
        ctlVideoSeeker.MetaData = mobjMetaData
        AddHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_RangeChanged

        'Remove irrelevant resolutions
        cmbDefinition.Items.Clear()
        cmbDefinition.Items.AddRange(New Object() {"Original", "120p", "240p", "360p", "480p", "720p", "1080p"})
        cmbDefinition.SelectedIndex = 0
        Dim removeStart As Integer = cmbDefinition.Items.Count
        For definitionIndex As Integer = 1 To cmbDefinition.Items.Count - 1
            If Integer.Parse(Regex.Match(cmbDefinition.Items(definitionIndex), "\d*").Value) > mobjMetaData.Height Then
                removeStart = definitionIndex
                Exit For
            End If
        Next
        While removeStart < cmbDefinition.Items.Count
            cmbDefinition.Items.RemoveAt(removeStart)
        End While

        'Clear images
        picVideo.Image = Nothing
        picFrame1.Image = Nothing
        picFrame2.Image = Nothing
        picFrame3.Image = Nothing
        picFrame4.Image = Nothing
        picFrame5.Image = Nothing
        PollPreviewFrames()
        cmsPicVideoExportFrame.Enabled = True
    End Sub


    ''' <summary>
    ''' Saves the file at the specified location
    ''' </summary>
    Private Sub SaveFile(ByVal outputPath As String, Optional overwrite As Boolean = False)
		'If overwrite is checked, re-name the current video, then run ffmpeg and output to original, and delete the re-named one
		Dim overwriteOriginal As Boolean = False
		If overwrite And System.IO.File.Exists(outputPath) Then
			'If you want to overwrite the original file that is being used, rename it
			If outputPath = mstrVideoPath Then
				overwriteOriginal = True
				My.Computer.FileSystem.RenameFile(outputPath, System.IO.Path.GetFileName(FileNameAppend(outputPath, "-temp")))
				mstrVideoPath = FileNameAppend(mstrVideoPath, "-temp")
			Else
				My.Computer.FileSystem.DeleteFile(outputPath)
			End If
		End If
		Dim sProperties As New SpecialOutputProperties With {
			.Decimate = chkDeleteDuplicates.Checked,
			.FPS = Me.TargetFPS,
			.PlaybackSpeed = mdblPlaybackSpeed,
			.PlaybackVolume = mdblPlaybackVolume,
			.QScale = If(chkQuality.Checked, 0, -1)
		}
		'Limit GIF framerate if the default is assigned
		If Not Path.GetExtension(mstrVideoPath).Equals(".gif") AndAlso Path.GetExtension(outputPath).Equals(".gif") AndAlso sProperties.FPS = 0 Then
			If (mobjMetaData.Framerate * sProperties.PlaybackSpeed) > 30 Then
				sProperties.FPS = 25
			End If
		End If
		Dim ignoreTrim As Boolean = ctlVideoSeeker.RangeMin = ctlVideoSeeker.RangeMinValue And ctlVideoSeeker.RangeMax = ctlVideoSeeker.RangeMaxValue
		'First check if something would conflict with cropping, if it will, just crop it first
		Dim willCrop As Boolean = mptStartCrop.X <> mptEndCrop.X AndAlso mptStartCrop.Y <> mptEndCrop.Y
		Dim postCropOperation As Boolean = sProperties.Decimate
		'MP4 does not work with decimate for some reason, so we should lossless convert to AVI first
		Dim isMP4 As Boolean = IO.Path.GetExtension(outputPath) = ".mp4"
		Dim intermediateFilePath As String = mstrVideoPath
		mproFfmpegProcess = Nothing
		Dim useIntermediate As Boolean = (postCropOperation AndAlso willCrop) OrElse (sProperties.Decimate AndAlso isMP4)
		Dim startTrim As Decimal = mobjMetaData.ThumbImageCachePTS(ctlVideoSeeker.RangeMinValue)
		Dim endFrame As Integer = Math.Min(ctlVideoSeeker.RangeMaxValue + 1, mobjMetaData.TotalFrames)
		Dim endTrim As Decimal = If(endFrame = mobjMetaData.TotalFrames, mobjMetaData.DurationSeconds, mobjMetaData.ThumbImageCachePTS(endFrame))
		If useIntermediate Then
			intermediateFilePath = FileNameAppend(outputPath, "-tempCrop") + If(isMP4, ".avi", "")
			If isMP4 Then
				intermediateFilePath = IO.Path.Combine(IO.Path.GetDirectoryName(outputPath), IO.Path.GetFileNameWithoutExtension(outputPath) + "-tempCrop.avi")
			End If
			'Don't pass in special properties yet, it would be better to decimate after cropping
			RunFfmpeg(mstrVideoPath, intermediateFilePath, 0, mintAspectWidth, mintAspectHeight, New SpecialOutputProperties() With {.PlaybackSpeed = 1, .PlaybackVolume = 1, .QScale = 0}, If(ignoreTrim, 0, startTrim), If(ignoreTrim, 0, endTrim), cmbDefinition.Items(0), mptStartCrop, mptEndCrop)
			If Not ignoreTrim Then
				ignoreTrim = True
			End If
			If mproFfmpegProcess Is Nothing Then
				Exit Sub
			End If
			mproFfmpegProcess.WaitForExit()
			'Check if user canceled manual entry
			If Not File.Exists(intermediateFilePath) Then
				Exit Sub
			End If
		End If
		Dim realTopLeftCrop As Point = mptStartCrop
		Dim realBottomRightCrop As Point = mptEndCrop
		SetCalculateRealCropPoints(realTopLeftCrop, realBottomRightCrop)
		Dim realwidth As Integer = mintAspectWidth
		Dim realheight As Integer = mintAspectHeight
		If mptStartCrop.X <> mptEndCrop.X AndAlso mptStartCrop.Y <> mptEndCrop.Y Then
			realwidth = realBottomRightCrop.X - realTopLeftCrop.X
			realheight = realBottomRightCrop.Y - realTopLeftCrop.Y
		End If
		If (Not mobjRotation = RotateFlipType.RotateNoneFlipNone) And (Not mobjRotation = RotateFlipType.Rotate180FlipNone) Then
			SwapValues(realwidth, realheight)
		End If
		'Now you can apply everything else
		RunFfmpeg(intermediateFilePath, outputPath, mobjRotation, realwidth, realheight, sProperties, If(ignoreTrim, 0, startTrim), If(ignoreTrim, 0, endTrim), cmbDefinition.Items(cmbDefinition.SelectedIndex), If(postCropOperation, New Point(0, 0), mptStartCrop), If(postCropOperation, New Point(0, 0), mptEndCrop))
		If mproFfmpegProcess Is Nothing Then
			Exit Sub
		End If
		mproFfmpegProcess.WaitForExit()
		If overwriteOriginal Or (useIntermediate) Then
			My.Computer.FileSystem.DeleteFile(intermediateFilePath)
		End If
		If File.Exists(outputPath) Then
			'Show file location of saved file
			OpenOrFocusFile(outputPath)
		End If
	End Sub

	''' <summary>
	''' Save file when the save file dialog is finished with an "ok" click
	''' </summary>
	Private Sub sfdVideoOut_FileOk(sender As System.Windows.Forms.SaveFileDialog, e As EventArgs) Handles sfdVideoOut.FileOk
		If IO.Path.GetExtension(sfdVideoOut.FileName).Length = 0 Then
			'If the user failed to have a file extension, default to the one it already was
			sfdVideoOut.FileName += IO.Path.GetExtension(mstrVideoPath)
		End If
		SaveFile(sfdVideoOut.FileName, System.IO.File.Exists(sfdVideoOut.FileName))
	End Sub

	''' <summary>
	''' sets up needed information and runs ffmpeg.exe to render the final video.
	''' </summary>
	Private Sub btnいくよ_Click(sender As Object, e As EventArgs) Handles btnいくよ.Click
		mblnUserInjection = My.Computer.Keyboard.CtrlKeyDown
		SaveAs()
	End Sub

	Private Sub SaveAs()
		sfdVideoOut.Filter = "MP4|*.mp4|GIF|*.gif|MKV|*.mkv|WMV|*.wmv|AVI|*.avi|MOV|*.mov|All files (*.*)|*.*"
		Dim validExtensions() As String = sfdVideoOut.Filter.Split("|")
		For index As Integer = 1 To validExtensions.Count - 1 Step 2
			If System.IO.Path.GetExtension(mstrVideoPath).Contains(validExtensions(index).Replace("*", "")) Then
				sfdVideoOut.FilterIndex = ((index - 1) \ 2) + 1
				Exit For
			End If
		Next
		sfdVideoOut.FileName = System.IO.Path.GetFileName(FileNameAppend(mstrVideoPath, "-SHINY"))
		sfdVideoOut.OverwritePrompt = True
		sfdVideoOut.ShowDialog()
	End Sub
#End Region

    ''' <summary>
    ''' Polls for keyframe image data from ffmpeg, gives a loading cursor
    ''' </summary>
    Private Sub PollPreviewFrames()
        Debug.Print("Starting PollPreviewFrames")
        'Make sure the user is notified that the application is working
        If Cursor = Cursors.Arrow Then
            Cursor = Cursors.WaitCursor
        End If
        mintCurrentFrame = 0

        'Create temporary task to supress warnings for async usage in Task.Run()
        Dim tempTask As Task

        'Try to read from file, otherwise go ahead and extract them
        If Not mobjMetaData.ReadScenesFromFile Then
            tempTask = Task.Run(Async Function()
                                    Await mobjMetaData.ExtractSceneChanges()
                                    Me.BeginInvoke(Sub()
                                                       If mobjMetaData.SceneFrames IsNot Nothing Then
                                                           'Check for nothing to avoid issue with loading a new file before the scene frames were set from the last
                                                           ctlVideoSeeker.SceneFrames = CompressSceneChanges(mobjMetaData.SceneFrames, ctlVideoSeeker.Width)
                                                       End If
                                                   End Sub)
                                End Function)
            'mobjMetaData.SaveScenesToFile()
        End If
        Dim fullFrameGrab As Task(Of Bitmap) = Nothing
        'Grab compressed frames
        If Not mobjMetaData.ReadThumbsFromFile Then
            If mobjMetaData.DurationSeconds < 5 Then
                'If the video is pretty short, just cache the whole thing
                fullFrameGrab = mobjMetaData.GetFfmpegFrameAsync(0, -1)
            Else
                ThreadPool.QueueUserWorkItem(Async Sub()
                                                 Await mobjMetaData.ExtractThumbFrames()
                                             End Sub)
            End If
            'mobjMetaData.SaveThumbsToFile()
        End If

        'Grab keyframes in a parallel manner
        'Spin up a thread for each set of frame grabs so they can be done at the same time
        'Update the images via invoking the main thread to avoid cross thread UI access
        'Signal barrer to synchronize completion between all keyframes

        'TODO Replace with a single ffmpeg call
        Dim previewFrames As New List(Of Integer)
        previewFrames.Add(0)
        previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.25))
        previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.5))
        previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.75))
        previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 3))
        previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 2))
        previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 1))
        'Dim multiGrabBarrier As New Barrier(2)
        If fullFrameGrab Is Nothing Then
            mobjMetaData.GetFfmpegFrameRangesAsync(previewFrames)
        End If
    End Sub

    ''' <summary>
    ''' Runs ffmpeg.exe with given command information. Cropping and rotation must be seperated.
    ''' </summary>
    Private Sub RunFfmpeg(ByVal inputFile As String, ByVal outPutFile As String, ByVal flip As RotateFlipType, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal specProperties As SpecialOutputProperties, ByVal startSS As Decimal, ByVal endSS As Decimal, ByVal targetDefinition As String, ByVal cropTopLeft As Point, ByVal cropBottomRight As Point)
        If specProperties?.PlaybackSpeed <> 0 Then
            'duration /= specProperties.PlaybackSpeed
            endSS *= specProperties.PlaybackSpeed
            startSS *= specProperties.PlaybackSpeed
        End If
        Dim duration As String = (endSS) - (startSS)
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		'Flip vertical
		'-vf "vflip,hflip"
		'Cropping
		'-filter:v "crop=out_w:out_h:x:y"
		If duration > 0 Then
			'duration = Math.Truncate(duration * mobjMetaData.Framerate) / mobjMetaData.Framerate
			Dim startHHMMSS As String = FormatHHMMSSm(startSS / specProperties.PlaybackSpeed)
			processInfo.Arguments += " -ss " & startHHMMSS & " -t " & duration.ToString
		End If
		processInfo.Arguments += $" -i ""{inputFile}"""

		'CREATE LIST OF PARAMETERS FOR EACH FILTER
		Dim videoFilterParams As New List(Of String)
		Dim audioFilterParams As New List(Of String)

		'CROP VIDEO(Can not be done with a rotate, must run twice)
		Dim cropWidth As Integer = newWidth
		Dim cropHeight As Integer = newHeight
		If (cropBottomRight.X - cropTopLeft.X) > 0 And (cropBottomRight.Y - cropTopLeft.Y) > 0 Then
			SetCalculateRealCropPoints(cropTopLeft, cropBottomRight)
			cropWidth = (cropBottomRight.X - cropTopLeft.X)
			cropHeight = (cropBottomRight.Y - cropTopLeft.Y)
			videoFilterParams.Add($"crop={cropWidth}:{cropHeight}:{cropTopLeft.X}:{cropTopLeft.Y}")
		End If
		'SCALE VIDEO
		Dim scale As Double = newHeight
		Select Case targetDefinition
			Case "Original"
			Case "120p"
				scale = 120
			Case "240p"
				scale = 240
			Case "360p"
				scale = 360
            Case "480p"
                scale = 480
            Case "720p"
				scale = 720
			Case "1080p"
				scale = 1080
		End Select
		scale /= newHeight
		If scale <> 1 Then
			Dim scaleX As Integer = ForceEven(Math.Floor(cropWidth * scale))
			Dim scaleY As Integer = ForceEven(Math.Floor(cropHeight * scale))
			If flip = RotateFlipType.Rotate90FlipNone OrElse flip = RotateFlipType.Rotate270FlipNone Then
				processInfo.Arguments += $" -s {scaleY}x{scaleX} -threads 4"
			Else
				processInfo.Arguments += $" -s {scaleX}x{scaleY} -threads 4"
			End If
		End If

		'ROTATE VIDEO
		Dim rotateString As String = If(flip = RotateFlipType.Rotate90FlipNone, "transpose=1", If(flip = RotateFlipType.Rotate180FlipNone, """transpose=2,transpose=2""", If(flip = RotateFlipType.Rotate270FlipNone, "transpose=2", "")))
		If rotateString.Length > 0 Then
			videoFilterParams.Add(rotateString)
		End If
		'DELETE DUPLICATE FRAMES
		Dim decimateString As String = If(specProperties?.Decimate, "mpdecimate,setpts=N/FRAME_RATE/TB", "")
		If decimateString.Length > 0 Then
			videoFilterParams.Add(decimateString)
		End If
		'CHROMAKEY
		'If Not specProperties.ChromaKey = Nothing Then
		'	With specProperties.ChromaKey
		'		vfParams.Add("chromakey=" & String.Format("0x{0:X2}{1:X2}{2:X2}", .R, .G, .B))
		'	End With
		'End If
		'PLAYBACK SPEED
		If specProperties?.PlaybackSpeed <> 1 AndAlso specProperties?.PlaybackSpeed > 0 AndAlso specProperties?.PlaybackSpeed < 3 Then
			videoFilterParams.Add($"setpts={1 / specProperties.PlaybackSpeed}*PTS")
			Dim audioPlaybackSpeed As String = $"atempo={specProperties.PlaybackSpeed}"
			If specProperties.PlaybackSpeed = 0.25 Then
				'atempo has a limit of between 0.5 and 2.0
				audioPlaybackSpeed = $"atempo=0.5,atempo=0.5"
			End If
			audioFilterParams.Add(audioPlaybackSpeed)
		End If
		'ASSEMBLE VIDEO PARAMETERS
		For paramIndex As Integer = 0 To videoFilterParams.Count - 1
			If paramIndex = 0 Then
				processInfo.Arguments += " -filter:v " & videoFilterParams(paramIndex)
			Else
				processInfo.Arguments += "," & videoFilterParams(paramIndex)
			End If
		Next

		'ADJUST VOLUME
		If specProperties?.PlaybackVolume <> 1 Then
			If specProperties.PlaybackVolume = 0 Then
				processInfo.Arguments += " -an"
			Else
				audioFilterParams.Add($"volume={specProperties.PlaybackVolume}")
			End If
		End If

		'ASSEMBLE AUDIO PARAMETERS
		For paramIndex As Integer = 0 To audioFilterParams.Count - 1
			If paramIndex = 0 Then
				processInfo.Arguments += " -filter:a " & audioFilterParams(paramIndex)
			Else
				processInfo.Arguments += "," & audioFilterParams(paramIndex)
			End If
		Next

		'SET QUALITY
		processInfo.Arguments += If(specProperties?.QScale <> -1, $" -q:v {specProperties.QScale}", "")

		'LIMIT FRAMERATE
		processInfo.Arguments += If(specProperties?.FPS > 0, $" -r {specProperties.FPS}", "")

		'OUTPUT TO FILE
		processInfo.Arguments += " """ & outPutFile & """"
		If mblnUserInjection Then
			'Show a form where the user can modify the arguments manually
			Dim manualEntryForm As New ManualEntryForm(processInfo.Arguments)
			Select Case manualEntryForm.ShowDialog()
				Case DialogResult.Cancel
					Exit Sub
			End Select
			processInfo.Arguments = manualEntryForm.ModifiedText
		End If
		processInfo.UseShellExecute = True
		processInfo.WindowStyle = ProcessWindowStyle.Normal
		mproFfmpegProcess = Process.Start(processInfo)
	End Sub



#Region "CROPPING CLICK AND DRAG"
    ''' <summary>
    ''' Updates the main image with one of the pre-selected images from the picture box clicked.
    ''' </summary>
    Private Sub picFrame_Click(sender As PictureBox, e As EventArgs) Handles picFrame1.Click, picFrame2.Click, picFrame3.Click, picFrame4.Click, picFrame5.Click
        Dim newPreview As Integer = 0
        Select Case True
            Case sender Is picFrame1
                newPreview = 0
            Case sender Is picFrame2
                newPreview = Math.Floor(mobjMetaData.TotalFrames * 0.25)
            Case sender Is picFrame3
                newPreview = Math.Floor(mobjMetaData.TotalFrames * 0.5)
            Case sender Is picFrame4
                newPreview = Math.Floor(mobjMetaData.TotalFrames * 0.75)
            Case sender Is picFrame5
                newPreview = Math.Floor(mobjMetaData.TotalFrames - 1)
        End Select
        'Find nearest available frame because the totalframes may have been modified slightly
        For index As Integer = Math.Max(0, newPreview - 2) To newPreview + 2
            If mobjMetaData.ImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                ctlVideoSeeker.PreviewLocation = index
                Exit Sub
            End If
        Next
        ctlVideoSeeker.PreviewLocation = newPreview
    End Sub

    ''' <summary>
    ''' Draws cropping graphics over the main video picturebox.
    ''' </summary>
    Private Sub picVideo_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picVideo.Paint
		Using pen As New Pen(Color.White, 1)
			If Not (mptStartCrop.X = 0 AndAlso mptStartCrop.X = mptEndCrop.X) Then
				e.Graphics.DrawLine(pen, New Point(mptStartCrop.X, 0), New Point(mptStartCrop.X, picVideo.Height))
				e.Graphics.DrawLine(pen, New Point(0, mptStartCrop.Y), New Point(picVideo.Width, mptStartCrop.Y))
				e.Graphics.DrawLine(pen, New Point(mptEndCrop.X - 1, 0), New Point(mptEndCrop.X - 1, picVideo.Height))
				e.Graphics.DrawLine(pen, New Point(0, mptEndCrop.Y - 1), New Point(picVideo.Width, mptEndCrop.Y - 1))
			End If
			If mintDisplayInfo <> 0 Then
				e.Graphics.FillRectangle(Brushes.White, New RectangleF(New PointF(0, 0), e.Graphics.MeasureString(mintCurrentFrame, Me.Font)))
				e.Graphics.DrawString(mintCurrentFrame, Me.Font, Brushes.Black, New PointF(0, 0))
			End If
			'e.Graphics.DrawRectangle(pen, 10, 75, 100, 100)
		End Using
		e.Graphics.DrawRectangle(New Pen(Color.Green, 1), mptStartCrop.X, mptStartCrop.Y, mptEndCrop.X - mptStartCrop.X - 1, mptEndCrop.Y - mptStartCrop.Y - 1)
	End Sub

	''' <summary>
	''' Modifies the crop region, sets to current point
	''' </summary>
	Private Sub picVideo_MouseDown(sender As Object, e As MouseEventArgs) Handles picVideo.MouseDown
		'Start dragging start or end point
		If e.Button = Windows.Forms.MouseButtons.Left Then
			If Not mptEndCrop.DistanceTo(e.Location) < 10 Then
				mptStartCrop = New Point(e.X, e.Y)
			End If
			mptEndCrop = New Point(e.X, e.Y)
		End If
		picVideo.Refresh()
	End Sub

	''' <summary>
	''' Modifies the crop region, draggable in all directions
	''' </summary>
	Private Sub picVideo_MouseMove(sender As Object, e As MouseEventArgs) Handles picVideo.MouseMove
		If e.Button = Windows.Forms.MouseButtons.Left Then
			mptEndCrop = New Point(e.X, e.Y)
			Dim minX As Integer = Math.Max(0, Math.Min(mptStartCrop.X, mptEndCrop.X))
			Dim minY As Integer = Math.Max(0, Math.Min(mptStartCrop.Y, mptEndCrop.Y))
			Dim maxX As Integer = Math.Min(picVideo.Width, Math.Max(mptStartCrop.X, mptEndCrop.X))
			Dim maxY As Integer = Math.Min(picVideo.Height, Math.Max(mptStartCrop.Y, mptEndCrop.Y))
			mptStartCrop.X = minX
			mptStartCrop.Y = minY
			mptEndCrop.X = maxX
			mptEndCrop.Y = maxY
			picVideo.Refresh()
		End If
	End Sub
#End Region

#Region "Form Open/Close"
	''' <summary>
	''' Prepares temporary directory and sets up tool tips for controls.
	''' </summary>
	Private Sub SimpleVideoEditor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		'Destroy old version from any updates
		Threading.ThreadPool.QueueUserWorkItem(Sub()
												   DeleteUpdateFiles()
											   End Sub)

		cmbDefinition.SelectedIndex = 0

		'Setup Tooltips
		mobjGenericToolTip.SetToolTip(ctlVideoSeeker, "Move sliders to trim video. Use [A][D][←][→] to move frame by frame.")
		mobjGenericToolTip.SetToolTip(picVideo, "Left click and drag to crop. Right click to clear crop selection.")
		mobjGenericToolTip.SetToolTip(cmbDefinition, "Select the ending height of your video. Right click for FPS options.")
		mobjGenericToolTip.SetToolTip(btnいくよ, "Save video. Hold ctrl to manually modify ffmpeg arguments.")
		mobjGenericToolTip.SetToolTip(picFrame1, "View first frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame2, "View 25% frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame3, "View middle frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame4, "View 75% frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame5, "View last frame of video.")
		mobjGenericToolTip.SetToolTip(imgRotate, "Rotate to 90°. Currently 0°.")
		mobjGenericToolTip.SetToolTip(btnBrowse, "Search for a video to edit.")
		mobjGenericToolTip.SetToolTip(lblFileName, "Name of the currently loaded file.")
		mobjGenericToolTip.SetToolTip(picChromaKey, "Color that will be made transparent if the output file type supports it.")
		mobjGenericToolTip.SetToolTip(picPlaybackSpeed, "Playback speed multiplier.")

		'Check if the program was started with a dragdrop exe
		Dim args() As String = Environment.GetCommandLineArgs()
		If args.Length > 1 Then
			For index As Integer = 1 To args.Length - 1
				If System.IO.File.Exists(args(index)) Then
					LoadFile(args(index))
				End If
			Next
		End If

		'Change window title to current version
		Me.Text = Me.Text & $" - Open Source"

		'Start render decay timer
		ThreadPool.QueueUserWorkItem(Sub()
										 While (True)
											 Dim oldValue As Integer = mintDisplayInfo
											 mintDisplayInfo = Math.Max(mintDisplayInfo - 10, 0)
											 Threading.Thread.Sleep(10)
											 If oldValue <> 0 AndAlso mintDisplayInfo = 0 Then
												 picVideo.Invalidate()
											 End If
										 End While
									 End Sub)
	End Sub

	Private Sub DeleteUpdateFiles()
		Dim badExePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly.Location) + "\DeletableSimpleVideoEditor.exe"
		If File.Exists(badExePath) Then
			'Keep trying to delete the file for a few seconds, in case the application is still running for some reason
			For index As Integer = 0 To 10
				Try
					File.Delete(badExePath)
				Catch ex As Exception
					'Exception, oh well, hopefully it's just because the application is still open, not because the user has no permissions <.<
				End Try
				Threading.Thread.Sleep(500)
			Next
		End If
	End Sub

	''' <summary>
	''' Resets controls to an empty state as if no file has been loaded
	''' </summary>
	Private Sub ClearControls()
		mptStartCrop = New Point(0, 0)
		mptEndCrop = New Point(0, 0)
		picVideo.Image = Nothing
		picFrame1.Image = Nothing
		picFrame2.Image = Nothing
		picFrame3.Image = Nothing
		picFrame4.Image = Nothing
		picFrame5.Image = Nothing
		ctlVideoSeeker.SceneFrames = Nothing
		ctlVideoSeeker.Enabled = False
		btnいくよ.Enabled = False
		lblFileName.Text = ""
	End Sub
#End Region

#Region "Misc Functions"
	''' <summary>
	''' Recursively deletes directories in as safe a manner as possible.
	''' </summary>
	Public Shared Sub DeleteDirectory(ByVal directoryPath As String)
		'Assure each file is not read-only, then delete them
		For Each file As String In System.IO.Directory.GetFiles(directoryPath)
			System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal)
			System.IO.File.Delete(file)
		Next
		'Delete each directory inside
		For Each directory As String In System.IO.Directory.GetDirectories(directoryPath)
			DeleteDirectory(directory)
		Next
		'Delete parent directory
		System.IO.Directory.Delete(directoryPath, False)
	End Sub

	''' <summary>
	''' Returns the temporary file path used to store images that ffmpeg.exe finds. TODO Would be nice to have a Ramdisk, or just not use files at all.
	''' </summary>
	Public ReadOnly Property TempFolderPath() As String
		Get
			Return Application.StartupPath & "\tempSimpleVideoEditor"
		End Get
	End Property

	''' <summary>
	''' Add a little bit of text to the end of a file name string between its extension like "-temp" or "-SHINY".
	''' </summary>
	Public Shared Function FileNameAppend(ByVal fullPath As String, ByVal newEnd As String)
		Return System.IO.Path.GetDirectoryName(fullPath) & "\" & System.IO.Path.GetFileNameWithoutExtension(fullPath) & newEnd & System.IO.Path.GetExtension(fullPath)
	End Function

	''' <summary>
	''' Changes the extension of a filepath string
	''' </summary>
	Public Shared Function FileNameChangeExtension(ByVal fullPath As String, ByVal newExtension As String)
		Return System.IO.Path.GetDirectoryName(fullPath) & "\" & System.IO.Path.GetFileNameWithoutExtension(fullPath) & newExtension
	End Function

	''' <summary>
	''' Returns an image from file without locking it from being deleted
	''' </summary>
	Public Shared Function GetImageNonLocking(ByVal fullPath As String) As Image
		Try
			Using fileStream As New System.IO.FileStream(fullPath, IO.FileMode.Open, IO.FileAccess.Read)
				Return Image.FromStream(fileStream)
			End Using
		Catch
			'Failed to access image
			Return Nothing
		End Try
	End Function

	''' <summary>
	''' Sets the given crop locations to their real points
	''' </summary>
	Public Sub SetCalculateRealCropPoints(ByRef cropTopLeft As Point, ByRef cropBottomRight As Point)
		If (cropBottomRight.X - cropTopLeft.X) > 0 And (cropBottomRight.Y - cropTopLeft.Y) > 0 Then
			'Calculate actual crop locations due to bars and aspect ration changes
			Dim actualAspectRatio As Double = (mintAspectHeight / mintAspectWidth)
			Dim picVideoAspectRatio As Double = (picVideo.Height / picVideo.Width)
			Dim fullHeight As Double = If(actualAspectRatio < picVideoAspectRatio, (mintAspectHeight / (actualAspectRatio / picVideoAspectRatio)), mintAspectHeight)
			Dim fullWidth As Double = If(actualAspectRatio > picVideoAspectRatio, (mintAspectWidth / (picVideoAspectRatio / actualAspectRatio)), mintAspectWidth)
			Dim verticalBarSizeRealPx As Integer = If(actualAspectRatio < picVideoAspectRatio, (fullHeight - mintAspectHeight) / 2, 0)
			Dim horizontalBarSizeRealPx As Integer = If(actualAspectRatio > picVideoAspectRatio, (fullWidth - mintAspectWidth) / 2, 0)
			Dim realStartCrop As Point = New Point(Math.Max(0, mptStartCrop.X * (fullWidth / picVideo.Width) - horizontalBarSizeRealPx), Math.Max(0, mptStartCrop.Y * (fullHeight / picVideo.Height) - verticalBarSizeRealPx))
			Dim realEndCrop As Point = New Point(Math.Min(mintAspectWidth, mptEndCrop.X * (fullWidth / picVideo.Width) - horizontalBarSizeRealPx), Math.Min(mintAspectHeight, mptEndCrop.Y * (fullHeight / picVideo.Height) - verticalBarSizeRealPx))
			cropTopLeft = realStartCrop
			cropBottomRight = realEndCrop
		End If
	End Sub

	''' <summary>
	''' Swaps the data between two objects
	''' </summary>
	Public Sub SwapValues(ByRef object1 As Object, ByRef object2 As Object)
		Dim tempObject As Object = object1
		object1 = object2
		object2 = tempObject
	End Sub

	Private ReadOnly Property TargetFPS As Integer
		Get
			Dim checkedItem As ToolStripMenuItem = DefaultToolStripMenuItem
			For Each objItem As ToolStripMenuItem In cmsFrameRate.Items
				If objItem.Checked Then
					checkedItem = objItem
					Exit For
				End If
			Next
			Select Case True
				Case checkedItem Is TenFPSToolStripMenuItem
					Return 10
				Case checkedItem Is FifteenFPSToolStripMenuItem
					Return 15
				Case checkedItem Is TwentyFPSToolStripMenuItem
					Return 20
				Case checkedItem Is ThirtyFPSToolStripMenuItem
					Return 30
				Case checkedItem Is SixtyFPSToolStripMenuItem
					Return 60
				Case Else
					Return 0
			End Select
		End Get
	End Property
#End Region

	''' <summary>
	''' Captures key events before everything else, and uses them to modify the video trimming picRangeSlider control.
	''' </summary>
	Protected Overrides Function ProcessCmdKey(ByRef message As Message, ByVal keys As Keys) As Boolean
		Select Case keys
			Case Keys.A
				ctlVideoSeeker.RangeMinValue = ctlVideoSeeker.RangeMinValue - 1
				ctlVideoSeeker.Invalidate()
			Case Keys.D
				ctlVideoSeeker.RangeMinValue = ctlVideoSeeker.RangeMinValue + 1
				ctlVideoSeeker.Invalidate()
			Case Keys.A Or Keys.Shift
				ctlVideoSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation - 1
				ctlVideoSeeker.Invalidate()
			Case Keys.D Or Keys.Shift
				ctlVideoSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation + 1
				ctlVideoSeeker.Invalidate()
			Case Keys.Left
				ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue - 1
				ctlVideoSeeker.Invalidate()
			Case Keys.Right
				ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue + 1
				ctlVideoSeeker.Invalidate()
			Case Else
				Return MyBase.ProcessCmdKey(message, keys)
		End Select
		Return True
	End Function

	''' <summary>
	''' Show company and development information
	''' </summary>
	Private Sub SimpleVideoEditor_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
		AboutForm.ShowDialog(Me)
		e.Cancel = True
	End Sub

	''' <summary>
	''' Changes the display text when muting/unmuting a video
	''' </summary>
	Private Sub chkMute_CheckedChanged(sender As Object, e As EventArgs) Handles chkMute.CheckChanged
		For Each objItem As ToolStripMenuItem In cmsPlaybackVolume.Items
			objItem.Checked = False
		Next
		If chkMute.Checked Then
			mdblPlaybackVolume = 0
			MuteToolStripMenuItem.Checked = True
		Else
			mdblPlaybackVolume = 1
			UnmuteToolStripMenuItem.Checked = True
		End If
		Dim volumeString As String = If(mdblPlaybackVolume = 0, "muted.", If(mdblPlaybackVolume = 1, "unmuted.", $"{mdblPlaybackVolume}%"))
		mobjGenericToolTip.SetToolTip(chkMute, If(chkMute.Checked, "Unmute", "Mute") & " the videos audio track. Currently " & If(chkMute.Checked, "muted.", volumeString))
	End Sub


	''' <summary>
	''' Changes the display text when changing quality check control
	''' </summary>
	Private Sub chkQuality_CheckedChanged(sender As Object, e As EventArgs) Handles chkQuality.CheckChanged
		mobjGenericToolTip.SetToolTip(chkQuality, If(chkQuality.Checked, "Automatic quality.", "Force equivalent quality. WARNING: Slow processing and large file size may occur.") & " Currently " & If(chkQuality.Checked, "forced equivalent (slow and large).", "automatic (fast and small)."))
	End Sub

	''' <summary>
	''' Toggles whether the video will be decimated or not, and changes the image to make it obvious
	''' </summary>
	Private Sub chkDeleteDuplicates_CheckedChanged(sender As Object, e As EventArgs) Handles chkDeleteDuplicates.CheckChanged
		mobjGenericToolTip.SetToolTip(chkDeleteDuplicates, If(chkDeleteDuplicates.Checked, "Allow Duplicate Frames", "Delete Duplicate Frames. Audio may go out of sync.") & " Currently " & If(chkDeleteDuplicates.Checked, "deleting them.", "allowing them."))
	End Sub

	''' <summary>
	''' Rotates the final video by 90 degrees per click, and updates the graphic
	''' </summary>
	Private Sub imgRotate_Click(sender As Object, e As EventArgs) Handles imgRotate.Click
		Select Case mobjRotation
			Case RotateFlipType.RotateNoneFlipNone
				mobjRotation = RotateFlipType.Rotate90FlipNone
				mobjGenericToolTip.SetToolTip(imgRotate, "Rotate to 180°. Currently 90°.")
			Case RotateFlipType.Rotate90FlipNone
				mobjRotation = RotateFlipType.Rotate180FlipNone
				mobjGenericToolTip.SetToolTip(imgRotate, "Rotate to 270°. Currently 180°.")
			Case RotateFlipType.Rotate180FlipNone
				mobjRotation = RotateFlipType.Rotate270FlipNone
				mobjGenericToolTip.SetToolTip(imgRotate, "Do not rotate. Currently will rotate 270°.")
			Case RotateFlipType.Rotate270FlipNone
				mobjRotation = RotateFlipType.RotateNoneFlipNone
				mobjGenericToolTip.SetToolTip(imgRotate, "Rotate to 90°. Currently 0°.")
			Case Else
				mobjRotation = RotateFlipType.RotateNoneFlipNone
		End Select
		Dim rotatedIcon As Image = New Bitmap(My.Resources.Rotate)
		rotatedIcon.RotateFlip(mobjRotation)
		imgRotate.Image = rotatedIcon
		imgRotate.Refresh()
	End Sub

	''' <summary>
	''' Clears the crop settings from the main picVideo control
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub cmsPicVideoClear_Click(sender As Object, e As EventArgs) Handles cmsPicVideoClear.Click
		mptStartCrop = New Point(0, 0)
		mptEndCrop = New Point(0, 0)
		picVideo.Refresh()
	End Sub

	''' <summary>
	''' User right clicked on the big image and wants to export that frame
	''' </summary>
	Private Sub cmsPicVideoExportFrame_Click(sender As Object, e As EventArgs) Handles cmsPicVideoExportFrame.Click
		Using sfdExportFrame As New SaveFileDialog
			sfdExportFrame.Title = "Select Frame Save Location"
			sfdExportFrame.Filter = "PNG|*.png|BMP|*.bmp|All files (*.*)|*.*"
			Dim validExtensions() As String = sfdVideoOut.Filter.Split("|")
			sfdExportFrame.FileName = "frame_" & mintCurrentFrame.ToString & ".png"
			sfdExportFrame.OverwritePrompt = True
			Select Case sfdExportFrame.ShowDialog()
				Case DialogResult.OK
					mobjMetaData.ExportFfmpegFrame(mintCurrentFrame, sfdExportFrame.FileName)
				Case Else
					'Do nothing
			End Select
		End Using
	End Sub

	''' <summary>
	''' Given an array of scene change values, compress the array into a new array of given size
	''' </summary>
	Public Shared Function CompressSceneChanges(ByRef sceneChanges As Double(), ByVal newTotalFrames As Integer) As Double()
		Dim compressedSceneChanges(newTotalFrames - 1) As Double
		'Local Maximums
		For frameIndex As Integer = 0 To sceneChanges.Count - 1
			Dim compressedIndex As Integer = Math.Floor(frameIndex * newTotalFrames / sceneChanges.Count)
			compressedSceneChanges(compressedIndex) = Math.Max(compressedSceneChanges(compressedIndex), sceneChanges(frameIndex))
		Next
		Return compressedSceneChanges
	End Function

	Private Sub ctlVideoSeeker_RangeChanged(newVal As Integer) Handles ctlVideoSeeker.SeekChanged
		If mstrVideoPath IsNot Nothing AndAlso mstrVideoPath.Length > 0 AndAlso mobjMetaData IsNot Nothing Then
			If Not mintCurrentFrame = newVal Then
				mintCurrentFrame = newVal
				If mobjMetaData.ImageCacheStatus(mintCurrentFrame) = ImageCache.CacheStatus.Cached Then
					'Grab immediate
					picVideo.Image = mobjMetaData.GetImageFromCache(mintCurrentFrame)
				Else
					If mobjMetaData.ThumbImageCacheStatus(mintCurrentFrame) = ImageCache.CacheStatus.Cached Then
						'Check for low res thumbnail if we have it
						picVideo.Image = mobjMetaData.GetImageFromThumbCache(mintCurrentFrame)
					Else
						'Loading image...
						picVideo.Image = Nothing
					End If
					'Queue, event will change the image for us
					If mobjSlideQueue Is Nothing OrElse Not mobjSlideQueue.IsAlive Then
						mobjSlideQueue = New Thread(Sub()
														Dim startFrame As Integer
														Do
															startFrame = mintCurrentFrame
															mobjMetaData.GetFfmpegFrame(mintCurrentFrame)
														Loop While startFrame <> mintCurrentFrame
													End Sub)
						mobjSlideQueue.Start()
					End If
				End If
			Else
				If mobjSlideQueue IsNot Nothing AndAlso mobjSlideQueue.IsAlive AndAlso ctlVideoSeeker.SelectedSlider = VideoSeeker.SliderID.None Then
					'Force frame grab because the user let go
					ThreadPool.QueueUserWorkItem(Sub()
													 mobjMetaData.GetFfmpegFrame(mintCurrentFrame)
												 End Sub)
				End If
			End If
			mintDisplayInfo = RENDER_DECAY_TIME
		End If
	End Sub

	Private mobjSlideQueue As Thread


    Private Sub NewFrameCached(sender As Object, objCache As ImageCache, ranges As List(Of List(Of Integer))) Handles mobjMetaData.RetrievedFrames
        For Each objRange In ranges
            If mintCurrentFrame >= objRange(0) AndAlso mintCurrentFrame <= objRange(1) Then
                'Ensure we avoid cross thread GDI access of the bitmap
                If Me.InvokeRequired Then
                    Me.Invoke(Sub()
                                  NewFrameCached(sender, objCache, ranges)
                              End Sub)
                Else
                    'Grab immediate
                    Dim gotImage As Bitmap = mobjMetaData.GetImageFromCache(mintCurrentFrame, objCache)
                    If picVideo.Image Is Nothing OrElse picVideo.Image.Width < gotimage.Width Then
                        picVideo.Image = gotimage
                    End If
                End If
                Exit For
            End If
        Next
    End Sub

    Private Sub PreviewsLoaded(sender As Object, objCache As ImageCache, ranges As List(Of List(Of Integer))) Handles mobjMetaData.RetrievedFrames
        If Me.InvokeRequired Then
            Me.Invoke(Sub()
                          PreviewsLoaded(sender, objCache, ranges)
                      End Sub)
        Else
            Dim previewFrames As New List(Of Integer)
            previewFrames.Add(0)
            previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.25))
            previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.5))
            previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.75))
            previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 3))
            previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 2))
            previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 1))

            For Each objRange In ranges
                For previewIndex As Integer = 0 To 6
                    Dim gotImage As Bitmap = Nothing
                    If previewFrames(previewIndex) >= objRange(0) AndAlso previewFrames(previewIndex) <= objRange(1) Then
                        gotImage = mobjMetaData.GetImageFromCache(previewFrames(previewIndex), objCache)
                        Dim targetPreview As PictureBox = Nothing
                        Select Case previewIndex
                            Case 0
                                targetPreview = picFrame1
                            Case 1
                                targetPreview = picFrame2
                            Case 2
                                targetPreview = picFrame3
                            Case 3
                                targetPreview = picFrame4
                            Case 4, 5, 6
                                targetPreview = picFrame5
                                For index As Integer = previewFrames(6) To previewFrames(4) Step -1
                                    If mobjMetaData.ImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                                        mobjMetaData.OverrideTotalFrames(index + 1)
                                        RemoveHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_RangeChanged
                                        ctlVideoSeeker.MetaData = mobjMetaData
                                        AddHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_RangeChanged
                                    End If
                                Next
                        End Select
                        If targetPreview.Image Is Nothing OrElse targetPreview.Image.Width < gotImage.Width Then
                            targetPreview.Image = gotImage
                        End If
                    End If
                Next
            Next
            If picFrame5.Image IsNot Nothing Then
                mintAspectWidth = mobjMetaData.Width
                mintAspectHeight = mobjMetaData.Height
                If picVideo.Image IsNot Nothing Then
                    'If the resolution failed to load, put in something
                    If mintAspectWidth = 0 Or mintAspectHeight = 0 Then
                        mintAspectWidth = picFrame1.Image.Width
                        mintAspectHeight = picFrame1.Image.Height
                    End If
                    'If the aspect ratio was somehow saved wrong, fix it
                    'Try flipping the known aspect, if its closer to what was loaded, change it
                    If Math.Abs((mintAspectWidth / mintAspectHeight) - (picVideo.Image.Height / picVideo.Image.Width)) < Math.Abs((mintAspectHeight / mintAspectWidth) - (picVideo.Image.Height / picVideo.Image.Width)) Then
                        SwapValues(mintAspectWidth, mintAspectHeight)
                    End If
                End If
                Cursor = Cursors.Arrow
                ctlVideoSeeker.Enabled = True
                btnいくよ.Enabled = True
            End If
        End If
    End Sub

    Private Sub picFrame_Click(sender As Object, e As EventArgs) Handles picFrame5.Click, picFrame4.Click, picFrame3.Click, picFrame2.Click, picFrame1.Click

	End Sub

	Private Sub cmsFrameRate_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsFrameRate.ItemClicked
		For Each objItem As ToolStripMenuItem In cmsFrameRate.Items
			objItem.Checked = False
		Next
		CType(e.ClickedItem, ToolStripMenuItem).Checked = True
	End Sub

	Private Sub picChromaKey_Click(sender As Object, e As EventArgs) Handles picChromaKey.Click
		Select Case dlgChromaColor.ShowDialog
			Case DialogResult.OK
				picChromaKey.BackColor = dlgChromaColor.Color
		End Select
	End Sub

	Private Sub picPlaybackSpeed_Click(sender As Object, e As MouseEventArgs) Handles picPlaybackSpeed.Click
		cmsPlaybackSpeed.Show(picPlaybackSpeed.PointToScreen(e.Location))
	End Sub

	Private Sub cmsPlaybackSpeed_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsPlaybackSpeed.ItemClicked
		For Each objItem As ToolStripMenuItem In cmsPlaybackSpeed.Items
			objItem.Checked = False
		Next
		'Sets the target playback speed global based on the text in the context menu
		Dim resultValue As Double = 1
		If Double.TryParse(Regex.Match(CType(e.ClickedItem, ToolStripMenuItem).Text, "\d*.?\d*").Value, resultValue) Then
			CType(e.ClickedItem, ToolStripMenuItem).Checked = True
			mdblPlaybackSpeed = resultValue
		End If
	End Sub

	Private Sub cmsPlaybackVolume_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsPlaybackVolume.ItemClicked
		For Each objItem As ToolStripMenuItem In cmsPlaybackVolume.Items
			objItem.Checked = False
		Next
		'Sets the target playback speed global based on the text in the context menu
		Dim resultValue As Double = 1
		If Double.TryParse(Regex.Match(CType(e.ClickedItem, ToolStripMenuItem).Text, "\d*.?\d*").Value, resultValue) Then
			CType(e.ClickedItem, ToolStripMenuItem).Checked = True
		End If
		Me.chkMute.Checked = (resultValue = 0)
		mdblPlaybackVolume = resultValue
		For Each objItem As ToolStripMenuItem In cmsPlaybackVolume.Items
			objItem.Checked = False
		Next
		If Double.TryParse(Regex.Match(CType(e.ClickedItem, ToolStripMenuItem).Text, "\d*.?\d*").Value, resultValue) Then
			CType(e.ClickedItem, ToolStripMenuItem).Checked = True
		End If
	End Sub

    Private Async Sub CacheAllFramesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CacheAllFramesToolStripMenuItem.Click
        Me.Cursor = Cursors.WaitCursor
        Await mobjMetaData.GetFfmpegFrameAsync(0, -1)
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub HolePuncherToolToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HolePuncherToolToolStripMenuItem.Click
		HolePuncherForm.Show()
	End Sub

	Private Sub picPlaybackSpeed_Click(sender As Object, e As EventArgs) Handles picPlaybackSpeed.Click

	End Sub

	Private Sub InjectCustomArgumentsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles InjectCustomArgumentsToolStripMenuItem.Click
		mblnUserInjection = True
		SaveAs()
	End Sub
End Class
