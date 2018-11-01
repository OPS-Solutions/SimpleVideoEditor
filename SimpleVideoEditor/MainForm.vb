Imports System.IO
Imports System.IO.Pipes
Imports System.Text.RegularExpressions

Public Class MainForm
	'FFMPEG Usefull Commands
	'https://www.labnol.org/internet/useful-ffmpeg-commands/28490/

	Private mStrVideoPath As String = "" 'Fullpath of the video file being edited
	Private mProFfmpegProcess As Process 'TODO This doesn't really need to be module level
	Private mthdDefaultLoadThread As System.Threading.Thread 'Thread for loading images upon open
	Private mobjGenericToolTip As ToolTip = New ToolTip 'Tooltip object required for setting tootips on controls

	Private mPtStartCrop As New Point(0, 0) 'Point for the top left of the crop rectangle
	Private mPtEndCrop As New Point(0, 0) 'Point for the bottom right of the crop rectangle

	Private mDblVideoDurationSS As Double

	Private mIntAspectWidth As Integer 'Holds onto the width of the video frame for aspect ration computation(Not correct width, but correct aspect)
	Private mIntAspectHeight As Integer 'Holds onto the height of the video frame for aspect ration computation(Not correct height, but correct aspect)

	Private mDblScaleFactorX As Double 'Keeps track of the width scale for the image display so that cropping can work with the right size
	Private mDblScaleFactorY As Double 'Keeps track of the height scale for the image display so that cropping can work with the right size

	Private mIntFrameRate As Double = 30 'Number of frames per second in the video
	Private mIntCurrentFrame As Integer = 0 'Current visible frame in the big picVideo control

	Private mobjMetaData As MetaData 'Video metadata, including things like resolution, framerate, bitrate, etc.
	Private mobjRotation As System.Drawing.RotateFlipType = RotateFlipType.RotateNoneFlipNone 'Keeps track of how the user wants to rotate the image
	Private mblnUserInjection As Boolean = False 'Keeps track of if the user wants to manually modify the resulting commands

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
			MessageBox.Show(ex.StackTrace)
		End Try
	End Sub

	''' <summary>
	''' File is opened, load in the images, and the file attributes.
	''' </summary>
	Public Sub LoadFile(ByVal fullPath As String)
		mStrVideoPath = fullPath
		lblFileName.Text = System.IO.Path.GetFileName(mStrVideoPath)
		mobjMetaData = GetMetaData(mStrVideoPath)
		mobjGenericToolTip.SetToolTip(lblFileName, "Name of the currently loaded file." & vbNewLine & mobjMetaData.StreamData)
		'Set frame rate
		mIntFrameRate = mobjMetaData.Framerate

		'Get video duration
		mDblVideoDurationSS = mobjMetaData.DurationSeconds

		'Set range of slider
		ctlVideoSeeker.RangeMax = mobjMetaData.TotalFrames - 1
		ctlVideoSeeker.RangeMinValue = 0
		ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMax
		ctlVideoSeeker.RangeValues(0) = ctlVideoSeeker.RangeMinValue
		ctlVideoSeeker.RangeValues(1) = ctlVideoSeeker.RangeMaxValue

		'Create a temporary directory to store images
		If (System.IO.Directory.Exists(TempFolderPath)) Then
			DeleteDirectory(TempFolderPath) 'Recursively delete directory
		End If
		System.IO.Directory.CreateDirectory(TempFolderPath)

		'Clear images
		picVideo.Image = Nothing
		picFrame1.Image = Nothing
		picFrame2.Image = Nothing
		picFrame3.Image = Nothing
		picFrame4.Image = Nothing
		picFrame5.Image = Nothing
		mthdDefaultLoadThread = New System.Threading.Thread(AddressOf LoadDefaultFrames)
		mthdDefaultLoadThread.Start()
		PollPreviewFrames()
	End Sub

	''' <summary>
	''' Saves the file at the specified location
	''' </summary>
	Private Sub SaveFile(ByVal outputPath As String, Optional overwrite As Boolean = False)
		'If overwrite is checked, re-name the current video, then run ffmpeg and output to original, and delete the re-named one
		Dim overwriteOriginal As Boolean = False
		If overwrite And System.IO.File.Exists(outputPath) Then
			'If you want to overwrite the original file that is being used, rename it
			If outputPath = mStrVideoPath Then
				overwriteOriginal = True
				My.Computer.FileSystem.RenameFile(outputPath, System.IO.Path.GetFileName(FileNameAppend(outputPath, "-temp")))
				mStrVideoPath = FileNameAppend(mStrVideoPath, "-temp")
			Else
				My.Computer.FileSystem.DeleteFile(outputPath)
			End If
		End If
		Dim ignoreTrim As Boolean = ctlVideoSeeker.RangeMin = ctlVideoSeeker.RangeMinValue And ctlVideoSeeker.RangeMax = ctlVideoSeeker.RangeMaxValue
		'First check if rotation would conflict with cropping, if it will, just crop it first
		Dim cropAndRotateOrChangeRes As Boolean = cmbDefinition.SelectedIndex > 0 OrElse ((Not mobjRotation = RotateFlipType.RotateNoneFlipNone) AndAlso mPtStartCrop.X <> mPtEndCrop.X AndAlso mPtStartCrop.Y <> mPtEndCrop.Y)
		Dim intermediateFilePath As String = mStrVideoPath
		If cropAndRotateOrChangeRes Then
			intermediateFilePath = FileNameAppend(outputPath, "-tempCrop")
			RunFfmpeg(mStrVideoPath, intermediateFilePath, 0, mIntAspectWidth, mIntAspectHeight, chkMute.Checked, 0, 0, cmbDefinition.Items(0), mPtStartCrop, mPtEndCrop)
			mProFfmpegProcess.WaitForExit()
		End If
		Dim realTopLeftCrop As Point = mPtStartCrop
		Dim realBottomRightCrop As Point = mPtEndCrop
		SetCalculateRealCropPoints(realTopLeftCrop, realBottomRightCrop)
		Dim realwidth As Integer = mIntAspectWidth
		Dim realheight As Integer = mIntAspectHeight
		If mPtStartCrop.X <> mPtEndCrop.X AndAlso mPtStartCrop.Y <> mPtEndCrop.Y Then
			realwidth = realBottomRightCrop.X - realTopLeftCrop.X
			realheight = realBottomRightCrop.Y - realTopLeftCrop.Y
		End If
		If (Not mobjRotation = RotateFlipType.RotateNoneFlipNone) And (Not mobjRotation = RotateFlipType.Rotate180FlipNone) Then
			SwapValues(realwidth, realheight)
		End If
		'Now you can apply everything else
		RunFfmpeg(intermediateFilePath, outputPath, mobjRotation, realwidth, realheight, chkMute.Checked, If(ignoreTrim, 0, ctlVideoSeeker.RangeMinValue / mIntFrameRate), If(ignoreTrim, 0, ctlVideoSeeker.RangeMaxValue / mIntFrameRate), cmbDefinition.Items(cmbDefinition.SelectedIndex), If(cropAndRotateOrChangeRes, New Point(0, 0), mPtStartCrop), If(cropAndRotateOrChangeRes, New Point(0, 0), mPtEndCrop))
		mProFfmpegProcess.WaitForExit()
		If overwriteOriginal Or cropAndRotateOrChangeRes Then
			My.Computer.FileSystem.DeleteFile(intermediateFilePath)
		End If
	End Sub

	''' <summary>
	''' Save file when the save file dialog is finished with an "ok" click
	''' </summary>
	Private Sub sfdVideoOut_FileOk(sender As System.Windows.Forms.SaveFileDialog, e As EventArgs) Handles sfdVideoOut.FileOk
		If IO.Path.GetExtension(sfdVideoOut.FileName).Length = 0 Then
			'If the user failed to have a file extension, default to the one it already was
			sfdVideoOut.FileName += IO.Path.GetExtension(mStrVideoPath)
		End If
		SaveFile(sfdVideoOut.FileName, System.IO.File.Exists(sfdVideoOut.FileName))
	End Sub

	''' <summary>
	''' sets up needed information and runs ffmpeg.exe to render the final video.
	''' </summary>
	Private Sub btnいくよ_Click(sender As Object, e As EventArgs) Handles btnいくよ.Click
		mblnUserInjection = My.Computer.Keyboard.CtrlKeyDown
		sfdVideoOut.Filter = "WMV|*.wmv|AVI|*.avi|All files (*.*)|*.*"
		Dim validExtensions() As String = sfdVideoOut.Filter.Split("|")
		For index As Integer = 1 To validExtensions.Count - 1 Step 2
			If System.IO.Path.GetExtension(mStrVideoPath).Contains(validExtensions(index).Replace("*", "")) Then
				sfdVideoOut.FilterIndex = ((index - 1) \ 2) + 1
				Exit For
			End If
		Next
		sfdVideoOut.FileName = System.IO.Path.GetFileName(FileNameAppend(mStrVideoPath, "-SHINY"))
		sfdVideoOut.OverwritePrompt = True
		sfdVideoOut.ShowDialog()
	End Sub
#End Region

	''' <summary>
	''' Loads default frames when called.
	''' </summary>
	Public Sub LoadDefaultFrames()
		'Use ffmpeg to grab images into a temporary folder
		Dim tempImage As Bitmap = GetFfmpegFrame(0)
		picVideo.Image = tempImage
		mIntAspectWidth = mobjMetaData.Width
		mIntAspectHeight = mobjMetaData.Height
		If picVideo.Image IsNot Nothing Then
			'If the resolution failed to load, put in something
			If mIntAspectWidth = 0 Or mIntAspectHeight = 0 Then
				mIntAspectWidth = tempImage.Width
				mIntAspectHeight = tempImage.Height
			End If
			'If the aspect ratio was somehow saved wrong, fix it
			'Try flipping the known aspect, if its closer to what was loaded, change it
			If Math.Abs((mIntAspectWidth / mIntAspectHeight) - (picVideo.Image.Height / picVideo.Image.Width)) < Math.Abs((mIntAspectHeight / mIntAspectWidth) - (picVideo.Image.Height / picVideo.Image.Width)) Then
				SwapValues(mIntAspectWidth, mIntAspectHeight)
			End If
		End If
		mDblScaleFactorX = mIntAspectWidth / picVideo.Width
		mDblScaleFactorY = mIntAspectHeight / picVideo.Height
	End Sub

	''' <summary>
	''' Asynchronously polls for keyframe image data from ffmpeg, gives a loading cursor
	''' </summary>
	Private Async Sub PollPreviewFrames()
		'Make sure the user is notified that the application is working
		If Cursor = Cursors.Arrow Then
			Cursor = Cursors.WaitCursor
		End If
		'Check if the default frames are available
		If picVideo.Image Is Nothing Then
			picVideo.Image = GetFfmpegFrame(0)
			'Now that the use can see things, they can go ahead and try to edit
		End If
		If picVideo.Image IsNot Nothing Then
			'If the aspect ration was somehow saved wrong, fix it
			'Try flipping the known aspect, if its closer to what was loaded, change it
			If mIntAspectWidth <> 0 And mIntAspectHeight <> 0 Then
				If Math.Abs((mIntAspectWidth / mIntAspectHeight) - (picVideo.Image.Height / picVideo.Image.Width)) < Math.Abs((mIntAspectHeight / mIntAspectWidth) - (picVideo.Image.Height / picVideo.Image.Width)) Then
					Dim tempHeight As Integer = mIntAspectHeight
					mIntAspectHeight = mIntAspectWidth
					mIntAspectWidth = tempHeight
				End If
			End If
			ctlVideoSeeker.Enabled = True
			btnいくよ.Enabled = True
		End If

		'Grab keyframes
		picFrame1.Image = Await GetFfmpegFrameAsync(0)
		picFrame2.Image = Await GetFfmpegFrameAsync(mobjMetaData.TotalFrames * 0.25)
		picFrame3.Image = Await GetFfmpegFrameAsync(mobjMetaData.TotalFrames * 0.5)
		picFrame4.Image = Await GetFfmpegFrameAsync(mobjMetaData.TotalFrames * 0.75)
		picFrame5.Image = Await GetFfmpegFrameAsync(mobjMetaData.TotalFrames - 1)
		If picFrame5.Image Is Nothing Then
			picFrame5.Image = Await GetFfmpegFrameAsync(mobjMetaData.TotalFrames - 2)
		End If
		If mobjMetaData.TotalFrames < 5 OrElse (picVideo.Image IsNot Nothing AndAlso picFrame1.Image IsNot Nothing AndAlso picFrame2.Image IsNot Nothing AndAlso picFrame3.Image IsNot Nothing AndAlso picFrame4.Image IsNot Nothing AndAlso picFrame5.Image IsNot Nothing) Then
			'Application is finished searching for images, reset cursor
			Cursor = Cursors.Arrow
			ctlVideoSeeker.Enabled = True
			btnいくよ.Enabled = True
		End If
		ctlVideoSeeker.SceneFrames = CompressSceneChanges(Await ExtractSceneChanges(mStrVideoPath), ctlVideoSeeker.Width)
	End Sub

#Region "Get File Attributes"
	''' <summary>
	''' Searches file details to find duration information
	''' </summary>
	Function GetHHMMSS(ByVal fullPath As String) As String
		'Check a few known possible locations for details that look like duration like 00:08:02
		If System.IO.File.Exists(fullPath) Then
			Dim shell As Object = CreateObject("Shell.Application")
			Dim folder As Object = shell.Namespace(System.IO.Path.GetDirectoryName(fullPath))
			For Each strFileName In folder.Items
				If System.IO.Path.GetFileName(strFileName.Path) = System.IO.Path.GetFileName(fullPath) Then
					For index As Integer = 0 To 500
						If (folder.GetDetailsOf(Nothing, index).ToString.ToLower.Equals("length")) Then
							Return folder.GetDetailsOf(strFileName, index).ToString
						End If
					Next
					Exit For
				End If
			Next
		End If
		Return ""
	End Function

	''' <summary>
	''' Gets metadata for video files using ffmpeg command line arguments, and parses it into an object
	''' </summary>
	''' <param name="fullPath"></param>
	Function GetMetaData(ByVal fullPath As String) As MetaData
		'Request metadata from ffmpeg vis -i command
		Dim processInfo As New ProcessStartInfo
		processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		processInfo.Arguments += "-i " & """" & fullPath & """" & " -c copy -f null null"
		processInfo.UseShellExecute = False
		processInfo.RedirectStandardError = True
		processInfo.CreateNoWindow = True
		Dim tempProcess As Process = Process.Start(processInfo)

		'Swap output to inside this application
		Dim output As String
		Using streamReader As System.IO.StreamReader = tempProcess.StandardError
			output = streamReader.ReadToEnd()
		End Using
		tempProcess.WaitForExit(1000)
		tempProcess.Close()

		Return New MetaData(output)
	End Function

	''' <summary>
	''' Searches file details to find frame rate information
	''' </summary>
	Function GetFrameRate(ByVal fullPath As String) As Integer
		Try
			'Loop through folder info for information that looks like frames/second
			If System.IO.File.Exists(fullPath) Then
				Dim shell As Object = CreateObject("Shell.Application")
				Dim folder As Object = shell.Namespace(System.IO.Path.GetDirectoryName(fullPath))
				For Each strFileName In folder.Items
					If System.IO.Path.GetFileName(strFileName.Path) = System.IO.Path.GetFileName(fullPath) Then
						For index As Integer = 0 To 500
							Debug.Print(folder.GetDetailsOf(Nothing, index).ToString & " | " & folder.GetDetailsOf(strFileName, index))
							If (folder.GetDetailsOf(Nothing, index).ToString.ToLower.Contains("frame rate")) Then
								Return Integer.Parse(folder.GetDetailsOf(strFileName, index).ToString.Split(" ")(0).Trim("‎")) 'Trim is not empty, it contains zero width space
							End If
						Next
						Exit For
					End If
				Next
			End If
		Catch ex As Exception
			'MessageBox.Show("No Framerate Detected... Defaulted to 30 FPS...")
			'Error, default to 30 fps
			Return 30
		End Try
		Return 30
	End Function

	''' <summary>
	''' Searches file details to find horizontal resolution of the video
	''' </summary>
	Function GetHorizontalResolution(ByVal fullPath As String) As Integer
		Dim attrib As FileAttribute = System.IO.File.GetAttributes(fullPath)

		'Loop through folder info for information that looks like frames/second
		If System.IO.File.Exists(fullPath) Then
			Dim shell As Object = CreateObject("Shell.Application")
			Dim folder As Object = shell.Namespace(System.IO.Path.GetDirectoryName(fullPath))
			For Each strFileName In folder.Items
				If System.IO.Path.GetFileName(strFileName.Path) = System.IO.Path.GetFileName(fullPath) Then
					For index As Integer = 0 To 500
						If (folder.GetDetailsOf(Nothing, index).ToString.ToLower.Contains("frame width")) Then
							Return Integer.Parse(folder.GetDetailsOf(strFileName, index).ToString)
						End If
					Next
					Exit For
				End If
			Next
		End If
		Return 0
	End Function

	''' <summary>
	''' Searches file details to find vertical resolution of the video
	''' </summary>
	Function GetVerticalResolution(ByVal fullPath As String) As Integer
		'Loop through folder info for information that looks like frames/second
		If System.IO.File.Exists(fullPath) Then
			Dim shell As Object = CreateObject("Shell.Application")
			Dim folder As Object = shell.Namespace(System.IO.Path.GetDirectoryName(fullPath))
			For Each strFileName In folder.Items
				If System.IO.Path.GetFileName(strFileName.Path) = System.IO.Path.GetFileName(fullPath) Then
					For index As Integer = 0 To 500
						'Debug.Print(index & ":" & folder.GetDetailsOf(Nothing, index).ToString)
						If (folder.GetDetailsOf(Nothing, index).ToString.ToLower.Contains("frame height")) Then
							Return Integer.Parse(folder.GetDetailsOf(strFileName, index).ToString)
						End If
					Next
					Exit For
				End If
			Next
		End If
		Return 0
	End Function
#End Region

	''' <summary>
	''' Runs ffmpeg.exe with given command information. Cropping and rotation must be seperated.
	''' </summary>
	Private Sub RunFfmpeg(ByVal inputFile As String, ByVal outPutFile As String, ByVal flip As RotateFlipType, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal mute As Boolean, ByVal startSS As Double, ByVal endSS As Double, ByVal targetDefinition As String, ByVal cropTopLeft As Point, ByVal cropBottomRight As Point)
		Dim duration As String = (endSS) - (startSS)
		Dim startHHMMSS As String = FormatHHMMSSss(startSS)
		Dim processInfo As New ProcessStartInfo
		processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		'Flip vertical
		'-vf "vflip,hflip"
		'Cropping
		'-filter:v "crop=out_w:out_h:x:y"
		processInfo.Arguments = "-i """ & inputFile & """"
		If duration > 0 Then
			processInfo.Arguments += " -ss " & startHHMMSS & " -t " & duration.ToString
		End If
		'CROP VIDEO(Can not be done with a rotate, must run twice)
		Dim cropWidth As Integer = newWidth
		Dim cropHeight As Integer = newHeight
		If (cropBottomRight.X - cropTopLeft.X) > 0 And (cropBottomRight.Y - cropTopLeft.Y) > 0 Then
			SetCalculateRealCropPoints(cropTopLeft, cropBottomRight)
			cropWidth = (cropBottomRight.X - cropTopLeft.X)
			cropHeight = (cropBottomRight.Y - cropTopLeft.Y)
			processInfo.Arguments += " -filter:v ""crop=" & cropWidth & ":" & cropHeight & ":" & cropTopLeft.X & ":" & cropTopLeft.Y & """"
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
			Case "460p"
				scale = 460
			Case "720p"
				scale = 720
			Case "1080p"
				scale = 1080
		End Select
		scale /= newHeight
		If scale <> 1 Then
			processInfo.Arguments += " -s " & ForceEven(Math.Floor(cropWidth * scale)) & "x" & ForceEven(Math.Floor(cropHeight * scale)) & " -threads 4"
		End If
		'ROTATE VIDEO
		processInfo.Arguments += If(flip = RotateFlipType.Rotate90FlipNone, " -vf transpose=1", If(flip = RotateFlipType.Rotate180FlipNone, " -vf ""transpose=2,transpose=2""", If(flip = RotateFlipType.Rotate270FlipNone, " -vf transpose=2", "")))
		'MUTE VIDEO
		processInfo.Arguments += If(mute, " -an", " -c:a copy")
		'OUTPUT TO FILE
		processInfo.Arguments += " """ & outPutFile & """"
		If mblnUserInjection Then
			'Show a form where the user can modify the arguments manually
			Dim manualEntryForm As New ManualEntryForm(processInfo.Arguments)
			manualEntryForm.ShowDialog()
			processInfo.Arguments = manualEntryForm.ModifiedText
		End If
		processInfo.UseShellExecute = True
		processInfo.WindowStyle = ProcessWindowStyle.Normal
		mProFfmpegProcess = Process.Start(processInfo)
	End Sub

	''' <summary>
	''' Immediately polls ffmpeg for the given frame
	''' </summary>
	Public Function GetFfmpegFrame(ByVal frame As Integer) As Bitmap
		'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
		Dim processInfo As New ProcessStartInfo
		processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		processInfo.Arguments = " -ss " & FormatHHMMSSss((frame) / mobjMetaData.Framerate)
		processInfo.Arguments += " -i """ & mStrVideoPath & """"
		processInfo.Arguments += " -vf scale=228:-1 -vframes 1 -f image2pipe -vcodec bmp -"
		processInfo.UseShellExecute = False
		processInfo.CreateNoWindow = True
		processInfo.RedirectStandardOutput = True
		processInfo.WindowStyle = ProcessWindowStyle.Hidden
		Dim tempProcess As Process = Process.Start(processInfo)

		Using recievedStream As New System.IO.MemoryStream
			Dim dataRead As String = tempProcess.StandardOutput.ReadToEnd()
			Dim byteBuffer() As Byte = System.Text.Encoding.Default.GetBytes(dataRead)
			recievedStream.Write(byteBuffer, 0, byteBuffer.Length)
			If byteBuffer.Length > 0 Then
				Return New Bitmap(recievedStream)
			Else
				Return Nothing
			End If
		End Using
		'If System.IO.File.Exists(targetFilePath) Then
		'	Return GetImageNonLocking(targetFilePath)
		'End If
		Return Nothing
	End Function

	''' <summary>
	''' Immediately polls ffmpeg for the given frame
	''' </summary>
	Public Async Function GetFfmpegFrameAsync(ByVal frame As Integer) As Task(Of Bitmap)
		'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
		Dim processInfo As New ProcessStartInfo
		processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		processInfo.Arguments = " -ss " & FormatHHMMSSss((frame) / mobjMetaData.Framerate)
		processInfo.Arguments += " -i """ & mStrVideoPath & """"
		processInfo.Arguments += " -vf scale=228:-1 -vframes 1 -f image2pipe -vcodec bmp -"
		processInfo.UseShellExecute = False
		processInfo.CreateNoWindow = True
		processInfo.RedirectStandardOutput = True
		processInfo.WindowStyle = ProcessWindowStyle.Hidden
		Dim tempProcess As Process = Process.Start(processInfo)

		Using recievedStream As New System.IO.MemoryStream
			Dim dataRead As String = Await tempProcess.StandardOutput.ReadToEndAsync()
			Dim byteBuffer() As Byte = System.Text.Encoding.Default.GetBytes(dataRead)
			recievedStream.Write(byteBuffer, 0, byteBuffer.Length)
			If byteBuffer.Length > 0 Then
				Return New Bitmap(recievedStream)
			Else
				Return Nothing
			End If
		End Using
		'If System.IO.File.Exists(targetFilePath) Then
		'	Return GetImageNonLocking(targetFilePath)
		'End If
		Return Nothing
	End Function

	''' <summary>
	''' Tells ffmpeg to make a file and returns the corresponding file path to the given seconds value, like 50.5 = "frame_000050.5.png".
	''' </summary>
	Public Function ExportFfmpegFrame(ByVal frame As Integer, targetFilePath As String) As String
		'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
		Dim processInfo As New ProcessStartInfo
		processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		processInfo.Arguments = " -ss " & FormatHHMMSSss((frame) / mobjMetaData.Framerate)
		processInfo.Arguments += " -i """ & mStrVideoPath & """"
		'processInfo.Arguments += " -vf ""select=gte(n\," & frame.ToString & "), scale=228:-1"" -vframes 1 " & """" & targetFilePath & """"
		processInfo.Arguments += " -vframes 1 " & """" & targetFilePath & """"
		processInfo.UseShellExecute = True
		processInfo.WindowStyle = ProcessWindowStyle.Hidden
		Dim tempProcess As Process = Process.Start(processInfo)
		tempProcess.WaitForExit(5000) 'Wait up to 5 seconds for the process to finish
		Return targetFilePath
	End Function



#Region "CROPPING CLICK AND DRAG"
	''' <summary>
	''' Updates the main image with one of the pre-selected images from the picture box clicked.
	''' </summary>
	Private Sub picFrame_Click(sender As PictureBox, e As EventArgs) Handles picFrame1.Click, picFrame2.Click, picFrame3.Click, picFrame4.Click, picFrame5.Click
		If sender.Image IsNot Nothing Then
			picVideo.Image = sender.Image.Clone
		End If
		Select Case True
			Case sender Is picFrame1
				mIntCurrentFrame = 0
			Case sender Is picFrame2
				mIntCurrentFrame = mobjMetaData.TotalFrames * 0.25
			Case sender Is picFrame3
				mIntCurrentFrame = mobjMetaData.TotalFrames * 0.5
			Case sender Is picFrame4
				mIntCurrentFrame = mobjMetaData.TotalFrames * 0.75
			Case sender Is picFrame5
				mIntCurrentFrame = mobjMetaData.TotalFrames - 1
		End Select
	End Sub

	''' <summary>
	''' Draws cropping graphics over the main video picturebox.
	''' </summary>
	Private Sub picVideo_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picVideo.Paint
		Using pen As New Pen(Color.White, 1)
			e.Graphics.DrawLine(pen, New Point(mPtStartCrop.X, 0), New Point(mPtStartCrop.X, picVideo.Height))
			e.Graphics.DrawLine(pen, New Point(0, mPtStartCrop.Y), New Point(picVideo.Width, mPtStartCrop.Y))
			e.Graphics.DrawLine(pen, New Point(mPtEndCrop.X - 1, 0), New Point(mPtEndCrop.X - 1, picVideo.Height))
			e.Graphics.DrawLine(pen, New Point(0, mPtEndCrop.Y - 1), New Point(picVideo.Width, mPtEndCrop.Y - 1))
			'e.Graphics.DrawRectangle(pen, 10, 75, 100, 100)
		End Using
		e.Graphics.DrawRectangle(New Pen(Color.Green, 1), mPtStartCrop.X, mPtStartCrop.Y, mPtEndCrop.X - mPtStartCrop.X - 1, mPtEndCrop.Y - mPtStartCrop.Y - 1)
	End Sub

	''' <summary>
	''' Modifies the crop region, sets to current point
	''' </summary>
	Private Sub picVideo_MouseDown(sender As Object, e As MouseEventArgs) Handles picVideo.MouseDown
		'Start dragging start or end point
		If e.Button = Windows.Forms.MouseButtons.Left Then
			mPtStartCrop = New Point(e.X, e.Y)
			mPtEndCrop = New Point(e.X, e.Y)
		End If
		picVideo.Refresh()
	End Sub

	''' <summary>
	''' Modifies the crop region, draggable in all directions
	''' </summary>
	Private Sub picVideo_MouseMove(sender As Object, e As MouseEventArgs) Handles picVideo.MouseMove
		If e.Button = Windows.Forms.MouseButtons.Left Then
			mPtEndCrop = New Point(e.X, e.Y)
			Dim minX As Integer = Math.Max(0, Math.Min(mPtStartCrop.X, mPtEndCrop.X))
			Dim minY As Integer = Math.Max(0, Math.Min(mPtStartCrop.Y, mPtEndCrop.Y))
			Dim maxX As Integer = Math.Min(picVideo.Width, Math.Max(mPtStartCrop.X, mPtEndCrop.X))
			Dim maxY As Integer = Math.Min(picVideo.Height, Math.Max(mPtStartCrop.Y, mPtEndCrop.Y))
			mPtStartCrop.X = minX
			mPtStartCrop.Y = minY
			mPtEndCrop.X = maxX
			mPtEndCrop.Y = maxY
		End If
		picVideo.Refresh()
	End Sub
#End Region

#Region "Form Open/Close"
	''' <summary>
	''' Prepares temporary directory and sets up tool tips for controls.
	''' </summary>
	Private Sub SimpleVideoEditor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		cmbDefinition.SelectedIndex = 0
		'Create a temporary directory to store images
		If (System.IO.Directory.Exists(TempFolderPath)) Then
			DeleteDirectory(TempFolderPath) 'Recursively delete directory
		End If
		System.IO.Directory.CreateDirectory(TempFolderPath)

		'Setup Tooltips
		mobjGenericToolTip.SetToolTip(ctlVideoSeeker, "Move sliders to trim video. Use [A][D][←][→] to move frame by frame.")
		mobjGenericToolTip.SetToolTip(picVideo, "Left click and drag to crop. Right click to clear crop selection.")
		mobjGenericToolTip.SetToolTip(cmbDefinition, "Select the ending height of your video.")
		mobjGenericToolTip.SetToolTip(btnいくよ, "Save video. Hold ctrl to manually modify ffmpeg arguments.")
		mobjGenericToolTip.SetToolTip(picFrame1, "View first frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame2, "View 25% frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame3, "View middle frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame4, "View 75% frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame5, "View last frame of video.")
		mobjGenericToolTip.SetToolTip(chkMute, "Unmute the videos audio track. Currently Muted.")
		mobjGenericToolTip.SetToolTip(imgRotate, "Rotate to 90°. Currently 0°.")
		mobjGenericToolTip.SetToolTip(btnBrowse, "Search for a video to edit.")
		mobjGenericToolTip.SetToolTip(lblFileName, "Name of the currently loaded file.")

		'Check if the program was started with a dragdrop exe
		Dim args() As String = Environment.GetCommandLineArgs()
		If args.Length > 1 Then
			For index As Integer = 1 To args.Length - 1
				If System.IO.File.Exists(args(index)) Then
					LoadFile(args(index))
				End If
			Next
		End If
	End Sub


	''' <summary>
	''' Resets controls to an empty state as if no file has been loaded
	''' </summary>
	Private Sub ClearControls()
		mPtStartCrop = New Point(0, 0)
		mPtEndCrop = New Point(0, 0)
		ctlVideoSeeker.RangeValues(0) = ctlVideoSeeker.RangeMin
		ctlVideoSeeker.RangeValues(1) = ctlVideoSeeker.RangeMax
		picVideo.Image = Nothing
		picFrame1.Image = Nothing
		picFrame2.Image = Nothing
		picFrame3.Image = Nothing
		picFrame4.Image = Nothing
		picFrame5.Image = Nothing
		ctlVideoSeeker.Enabled = False
		btnいくよ.Enabled = False
		lblFileName.Text = ""
	End Sub

	''' <summary>
	''' Clears up unneeded images from temporary directory
	''' </summary>
	Private Sub SimpleVideoEditor_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
		'Create a temporary directory to store images
		If (System.IO.Directory.Exists(TempFolderPath)) Then
			DeleteDirectory(TempFolderPath) 'Recursively delete directory
		End If
		System.IO.Directory.CreateDirectory(TempFolderPath)
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
	Public Function FileNameAppend(ByVal fullPath As String, ByVal newEnd As String)
		Return System.IO.Path.GetDirectoryName(mStrVideoPath) & "\" & System.IO.Path.GetFileNameWithoutExtension(mStrVideoPath) & newEnd & System.IO.Path.GetExtension(mStrVideoPath)
	End Function

	''' <summary>
	''' Changes the extension of a filepath string
	''' </summary>
	Public Function FileNameChangeExtension(ByVal fullPath As String, ByVal newExtension As String)
		Return System.IO.Path.GetDirectoryName(mStrVideoPath) & "\" & System.IO.Path.GetFileNameWithoutExtension(mStrVideoPath) & newExtension
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
	''' Converts a double like 100.5 seconds to HHMMSSss... like "00:01:40.5"
	''' </summary>
	Public Shared Function FormatHHMMSSss(ByVal totalSS As Double) As String
		Dim hours As Double = ((totalSS / 60) / 60)
		Dim minutes As Double = (hours Mod 1) * 60
		Dim seconds As Double = (minutes Mod 1) * 60
		Dim millisecond As Integer = Math.Round(totalSS Mod 1, 2, MidpointRounding.AwayFromZero) * 100
		Return Math.Truncate(hours).ToString.PadLeft(2, "0") & ":" & Math.Truncate(minutes).ToString.PadLeft(2, "0") & ":" & Math.Truncate(seconds).ToString.PadLeft(2, "0") & "." & millisecond.ToString.PadLeft(2, "0")
	End Function

	''' <summary>
	''' Converts a time like "00:01:40.5" to the total number of seconds
	''' </summary>
	''' <param name="duration"></param>
	''' <returns></returns>
	Public Shared Function HHMMSSssToSeconds(ByVal duration As String) As Double
		Dim totalSeconds As Double = 0
		totalSeconds += Integer.Parse(duration.Substring(0, 2)) * 60 * 60 'Hours
		totalSeconds += Integer.Parse(duration.Substring(3, 2)) * 60 'Minutes
		totalSeconds += Integer.Parse(duration.Substring(6, 2)) 'Seconds
		totalSeconds += Integer.Parse(duration.Substring(9, 2)) / 100.0 'Milliseconds
		Return totalSeconds
	End Function

	''' <summary>
	''' Takes a number like 123 and forces it to be divisible by 2, either by returning 122 or 124
	''' </summary>
	Public Function ForceEven(ByVal number As Integer, Optional ByVal forceDown As Boolean = True) As String
		If number \ 2 = (number - 1) \ 2 Then
			Return number + If(forceDown, -1, 1)
		Else
			Return number
		End If
	End Function

	''' <summary>
	''' Sets the given crop locations to their real points
	''' </summary>
	Public Sub SetCalculateRealCropPoints(ByRef cropTopLeft As Point, ByRef cropBottomRight As Point)
		If (cropBottomRight.X - cropTopLeft.X) > 0 And (cropBottomRight.Y - cropTopLeft.Y) > 0 Then
			'Calculate actual crop locations due to bars and aspect ration changes
			Dim actualAspectRatio As Double = (mIntAspectHeight / mIntAspectWidth)
			Dim picVideoAspectRatio As Double = (picVideo.Height / picVideo.Width)
			Dim fullHeight As Double = If(actualAspectRatio < picVideoAspectRatio, (mIntAspectHeight / (actualAspectRatio / picVideoAspectRatio)), mIntAspectHeight)
			Dim fullWidth As Double = If(actualAspectRatio > picVideoAspectRatio, (mIntAspectWidth / (picVideoAspectRatio / actualAspectRatio)), mIntAspectWidth)
			Dim verticalBarSizeRealPx As Integer = If(actualAspectRatio < picVideoAspectRatio, (fullHeight - mIntAspectHeight) / 2, 0)
			Dim horizontalBarSizeRealPx As Integer = If(actualAspectRatio > picVideoAspectRatio, (fullWidth - mIntAspectWidth) / 2, 0)
			Dim realStartCrop As Point = New Point(Math.Max(0, mPtStartCrop.X * (fullWidth / picVideo.Width) - horizontalBarSizeRealPx), Math.Max(0, mPtStartCrop.Y * (fullHeight / picVideo.Height) - verticalBarSizeRealPx))
			Dim realEndCrop As Point = New Point(Math.Min(mIntAspectWidth, mPtEndCrop.X * (fullWidth / picVideo.Width) - horizontalBarSizeRealPx), Math.Min(mIntAspectHeight, mPtEndCrop.Y * (fullHeight / picVideo.Height) - verticalBarSizeRealPx))
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
#End Region

	''' <summary>
	''' Captures key events before everything else, and uses them to modify the video trimming picRangeSlider control.
	''' </summary>
	Protected Overrides Function ProcessCmdKey(ByRef message As Message, ByVal keys As Keys) As Boolean
		Select Case keys
			Case Keys.A
				ctlVideoSeeker.RangeMinValue = ctlVideoSeeker.RangeMinValue - 1
				'picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
			Case Keys.D
				ctlVideoSeeker.RangeMinValue = ctlVideoSeeker.RangeMinValue + 1
				'picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
			Case Keys.Left
				ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue - 1
				'picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
			Case Keys.Right
				ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue + 1
				'picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
		End Select
		Return MyBase.ProcessCmdKey(message, keys)
	End Function

	''' <summary>
	''' Show company and development information
	''' </summary>
	Private Sub SimpleVideoEditor_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
		frmAbout.Show()
		frmAbout.Focus()
		e.Cancel = True
	End Sub

	''' <summary>
	''' Toggles whether the video will be muted or not, and changes the image to make it obvious
	''' </summary>
	Private Sub imgMute_CheckedChanged(sender As Object, e As EventArgs) Handles chkMute.CheckChanged
		mobjGenericToolTip.SetToolTip(chkMute, If(chkMute.Checked, "Unmute", "Mute") & " the videos audio track. Currently " & If(chkMute.Checked, "muted.", "unmuted."))
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
		mPtStartCrop = New Point(0, 0)
		mPtEndCrop = New Point(0, 0)
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
			sfdExportFrame.FileName = "frame_" & mIntCurrentFrame.ToString & ".png"
			sfdExportFrame.OverwritePrompt = True
			Select Case sfdExportFrame.ShowDialog()
				Case DialogResult.OK
					ExportFfmpegFrame(mIntCurrentFrame, sfdExportFrame.FileName)
				Case Else
					'Do nothing
			End Select
		End Using
	End Sub


	''' <summary>
	''' Gets a list of frames where a scene has changed
	''' </summary>
	Private Async Function ExtractSceneChanges(ByVal strVideoPath As String) As Task(Of Double())
		'ffmpeg -i GEVideo.wmv -vf select=gt(scene\,0.2),showinfo -f null -
		'ffmpeg -i GEVideo.wmv -vf select='gte(scene,0)',metadata=print -an -f null -
		Dim processInfo As New ProcessStartInfo
		processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
		processInfo.Arguments += " -i """ & strVideoPath & """"
		processInfo.Arguments += " -vf select='gte(scene,0)',metadata=print -an -f null -"
		processInfo.UseShellExecute = False 'Must be false to redirect standard output
		processInfo.CreateNoWindow = True
		processInfo.RedirectStandardOutput = True
		processInfo.RedirectStandardError = True
		Dim tempProcess As Process = Process.Start(processInfo)

		Dim sceneValues(mobjMetaData.TotalFrames - 1) As Double
		Using recievedStream As New System.IO.MemoryStream
			Dim dataRead As String = Await tempProcess.StandardError.ReadToEndAsync()
			Dim matches As MatchCollection = Regex.Matches(dataRead, "(?<=.scene_score=)\d+\.\d+")
			Dim frameIndex As Integer = 0
			For Each sceneString As Match In matches
				sceneValues(frameIndex) = Double.Parse(sceneString.Value)
				frameIndex += 1
			Next
			'Dim matches As Match = Regex.Match(dataRead, "(?m)Parsed_showinfo.*pts_time.*(?!$)")
			'For Each sceneString As String In matches.Groups(0).Captures(0).Value.Split(vbNewLine)
			'	frameList.Add(Double.Parse(Regex.Match(sceneString, "(?<=time:).*(?= pos)").Value) * mobjMetaData.Framerate)
			'Next
			'Dim matches As Match = Regex.Match(dataRead, "(?m)frame=.*(?!$)")
			'For Each sceneString As String In matches.Groups(0).Captures(0).Value.Split(vbNewLine)
			'	frameList.Add(HHMMSSssToSeconds(Regex.Match(sceneString, "(?<=time=).*(?=bitrate)").Groups(0).Captures(0).Value) * mobjMetaData.Framerate)
			'Next
		End Using
		Return sceneValues
	End Function

	''' <summary>
	''' Given an array of scene change values, compress the array into a new array of given size
	''' </summary>
	Public Function CompressSceneChanges(ByRef sceneChanges As Double(), ByVal newTotalFrames As Integer) As Double()
		Dim compressedSceneChanges(newTotalFrames - 1) As Double
		'Local Maximums
		For frameIndex As Integer = 0 To sceneChanges.Count - 1
			Dim compressedIndex As Integer = Math.Floor(frameIndex * newTotalFrames / sceneChanges.Count)
			compressedSceneChanges(compressedIndex) = Math.Max(compressedSceneChanges(compressedIndex), sceneChanges(frameIndex))
		Next
		Return compressedSceneChanges
	End Function

	Private Sub ctlVideoSeeker_RangeChanged(newVal As Integer, ChangeMin As Boolean) Handles ctlVideoSeeker.RangeChanged
		If mStrVideoPath IsNot Nothing AndAlso mStrVideoPath.Length > 0 AndAlso mobjMetaData IsNot Nothing Then
			mIntCurrentFrame = newVal
			picVideo.Image = GetFfmpegFrame(mIntCurrentFrame)
		End If
	End Sub
End Class
