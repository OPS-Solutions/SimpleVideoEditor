﻿Imports System.IO
Imports System.IO.Pipes

Public Class SimpleVideoEditor
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

#Region "RangeSlider"
	Event RangeChanged(ByVal newVal As Integer, ByVal ChangeMin As Boolean)
	Private pRangeMinValue As Integer = 0 'Value that should only be accessed by the property below
	Property RangeMinValue As Integer 'Current value of the leftmost slider on the picRangeSlider control
		Get
			Return pRangeMinValue
		End Get
		Set(value As Integer)
			pRangeMinValue = Math.Max(value, mRangeMin)
			If pRangeMinValue >= RangeMaxValue Then
				RangeMaxValue = pRangeMinValue + 1
				If RangeMaxValue = pRangeMinValue Then
					pRangeMinValue = RangeMaxValue - 1
				End If
			End If
			RaiseEvent RangeChanged(pRangeMinValue, True)
		End Set
	End Property

	Private pRangeMaxValue As Integer = 100 'Value that should only be accessed by the property below
	Property RangeMaxValue As Integer 'Current value of the rightmost slider on the picRangeSlider control
		Get
			Return pRangeMaxValue
		End Get
		Set(value As Integer)
			pRangeMaxValue = Math.Min(value, mRangeMax)
			If pRangeMaxValue <= RangeMinValue Then
				RangeMinValue = pRangeMaxValue - 1
				If RangeMinValue = pRangeMaxValue Then
					pRangeMaxValue = RangeMinValue + 1
				End If
			End If
			RaiseEvent RangeChanged(pRangeMaxValue, False)
		End Set
	End Property

	Private mRangeMin As Integer = 0 'Minimum limit for picRangeSlider control
	Private mRangeMax As Integer = 100 'Maximum limit for picRangeSlider control
	Private mRangeValues As Integer() = {0, 100} 'Storage for the last range values for the picRangeSlider control
#End Region



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
		mRangeMax = mobjMetaData.TotalFrames - 1
		RangeMinValue = 0
		RangeMaxValue = mRangeMax
		mRangeValues(0) = RangeMinValue
		mRangeValues(1) = RangeMaxValue

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
		Dim ignoreTrim As Boolean = mRangeMin = RangeMinValue And mRangeMax = RangeMaxValue
		'First check if rotation would conflict with cropping, if it will, just crop it first
		Dim cropAndRotateOrChangeRes As Boolean = cmbDefinition.SelectedIndex > 0 OrElse (Not radUp.Checked AndAlso mPtStartCrop.X <> mPtEndCrop.X AndAlso mPtStartCrop.Y <> mPtEndCrop.Y)
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
		If Not radUp.Checked And Not radDown.Checked Then
			SwapValues(realwidth, realheight)
		End If
		'Now you can apply everything else
		RunFfmpeg(intermediateFilePath, outputPath, If(radUp.Checked, 0, If(radRight.Checked, 1, If(radDown.Checked, 2, If(radLeft.Checked, 3, 0)))), realwidth, realheight, chkMute.Checked, If(ignoreTrim, 0, RangeMinValue / mIntFrameRate), If(ignoreTrim, 0, RangeMaxValue / mIntFrameRate), cmbDefinition.Items(cmbDefinition.SelectedIndex), If(cropAndRotateOrChangeRes, New Point(0, 0), mPtStartCrop), If(cropAndRotateOrChangeRes, New Point(0, 0), mPtEndCrop))
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
	''' sets up neede information and runs ffmpeg.exe to render the final video.
	''' </summary>
	Private Sub btnいくよ_Click(sender As Object, e As EventArgs) Handles btnいくよ.Click
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
			picRangeSlider.Enabled = True
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
			picRangeSlider.Enabled = True
			btnいくよ.Enabled = True
		End If
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
	Private Sub RunFfmpeg(ByVal inputFile As String, ByVal outPutFile As String, ByVal flip As Short, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal mute As Boolean, ByVal startSS As Double, ByVal endSS As Double, ByVal targetDefinition As String, ByVal cropTopLeft As Point, ByVal cropBottomRight As Point)
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
		processInfo.Arguments += If(flip = 0, "", If(flip = 1, " -vf transpose=1", If(flip = 2, " -vf ""transpose=2,transpose=2""", If(flip = 3, " -vf transpose=2", ""))))
		'MUTE VIDEO
		processInfo.Arguments += If(mute, " -an", " -c:a copy")
		'OUTPUT TO FILE
		processInfo.Arguments += " """ & outPutFile & """"
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

#Region "Range Slider Events"
	''' <summary>
	''' Paints over the picRangeSlider control with custom dual trackbar looking graphics.
	''' </summary>
	Private Sub picRangeSlider_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picRangeSlider.Paint
		Using pen As New Pen(Color.Black, 1)
			'Draw ticks
			Dim numberOfTicks As Integer = Math.Min(picRangeSlider.Width \ 2, mRangeMax - mRangeMin)
			Dim distanceBetweenPoints As Double = picRangeSlider.Width / numberOfTicks
			'Draw background
			Dim fullrange As Integer = mRangeMax - mRangeMin
			e.Graphics.DrawRectangle(New Pen(If(picRangeSlider.Enabled, Color.Green, Color.Gray), 6), CType((RangeMinValue * ((picRangeSlider.Width - 1) / fullrange)) + 3, Integer), 6, CType(((RangeMaxValue - RangeMinValue) * ((picRangeSlider.Width - 1) / fullrange)) - 6, Integer), picRangeSlider.Height - 12)
			For index As Integer = 0 To numberOfTicks - 1
				e.Graphics.DrawLine(pen, New Point(index * distanceBetweenPoints, picRangeSlider.Height), New Point(index * distanceBetweenPoints, picRangeSlider.Height - 2))
			Next
			'Draw current points
			e.Graphics.DrawLine(pen, New Point((RangeMinValue * ((picRangeSlider.Width - 1) / fullrange)), picRangeSlider.Height - 3), New Point((RangeMinValue * ((picRangeSlider.Width - 1) / fullrange)), 0))
			e.Graphics.DrawLine(pen, New Point((RangeMaxValue * ((picRangeSlider.Width - 1) / fullrange)), picRangeSlider.Height - 3), New Point((RangeMaxValue * ((picRangeSlider.Width - 1) / fullrange)), 0))
		End Using
	End Sub

	''' <summary>
	''' Searches for the nearest slider in the picRangeSlider control, and selects it for use.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	''' <remarks></remarks>
	Private Sub picRangeSlider_MouseDown(sender As Object, e As MouseEventArgs) Handles picRangeSlider.MouseDown
		'Convert mouse coordinates to increments, Grab closest slider
		If picRangeSlider.Enabled Then
			Dim newValue As Integer = ((mRangeMax - mRangeMin) / picRangeSlider.Width) * e.X
			If Math.Abs(newValue - RangeMinValue) < Math.Abs(newValue - RangeMaxValue) Then
				RangeSliderMinSelected = True
			Else
				RangeSliderMaxSelected = True
			End If
			picRangeSlider_MouseMove(sender, e)
			picRangeSlider.Refresh()
		End If
	End Sub

	Private RangeSliderMinSelected As Boolean = False
	Private RangeSliderMaxSelected As Boolean = False
	''' <summary>
	''' Changes the corresponding range values for the sliders in the control.
	''' </summary>
	Private Sub picRangeSlider_MouseMove(sender As Object, e As MouseEventArgs) Handles picRangeSlider.MouseMove
		If picRangeSlider.Enabled Then
			If e.Button = Windows.Forms.MouseButtons.Left Then
				'Move range sliders
				Dim newValue As Integer = ((mRangeMax - mRangeMin) / picRangeSlider.Width) * e.X
				If RangeSliderMinSelected Then
					RangeMinValue = newValue
				ElseIf RangeSliderMaxSelected Then
					RangeMaxValue = newValue
				End If
				picRangeSlider.Refresh()
			End If
		End If
	End Sub

	''' <summary>
	''' Updates the image only when mouse up occurs so your CPU doesn't get burned up with too many reads/writes.
	''' </summary>
	Private Sub picRangeSlider_SlowValueChanged(sender As Object, e As EventArgs) Handles picRangeSlider.MouseUp
		If picRangeSlider.Enabled Then
			RangeSliderMinSelected = False
			RangeSliderMaxSelected = False
			Dim minChanged As Boolean = Not RangeMinValue = mRangeValues(0)
			mRangeValues(0) = RangeMinValue
			Dim maxChanged As Boolean = Not RangeMaxValue = mRangeValues(1)
			mRangeValues(1) = RangeMaxValue
			If minChanged Or maxChanged Then
				mIntCurrentFrame = If(minChanged, RangeMinValue, RangeMaxValue)
				picVideo.Image = GetFfmpegFrame(mIntCurrentFrame)
				picRangeSlider.Refresh()
			End If
		End If
	End Sub
#End Region

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
		mobjGenericToolTip.SetToolTip(picRangeSlider, "Move sliders to trim video. Use [A][D][←][→] to move frame by frame.")
		mobjGenericToolTip.SetToolTip(picVideo, "Left click and drag to crop. Right click to clear crop selection.")
		mobjGenericToolTip.SetToolTip(cmbDefinition, "Select the ending height of your video.")
		mobjGenericToolTip.SetToolTip(grpRotation, "Select a new orientation, where the selected dot is the new ""up"" direction after rendering.")
		mobjGenericToolTip.SetToolTip(btnいくよ, "Save video.")
		mobjGenericToolTip.SetToolTip(picFrame1, "View first frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame2, "View 25% frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame3, "View middle frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame4, "View 75% frame of video.")
		mobjGenericToolTip.SetToolTip(picFrame5, "View last frame of video.")
		mobjGenericToolTip.SetToolTip(chkMute, "Mute the videos audio track.")
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
		mRangeValues(0) = mRangeMin
		mRangeValues(1) = mRangeMax
		picVideo.Image = Nothing
		picFrame1.Image = Nothing
		picFrame2.Image = Nothing
		picFrame3.Image = Nothing
		picFrame4.Image = Nothing
		picFrame5.Image = Nothing
		picRangeSlider.Enabled = False
		btnいくよ.Enabled = False
		lblFileName.Text = ""
		radUp.Checked = True
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
				RangeMinValue = RangeMinValue - 1
				picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
			Case Keys.D
				RangeMinValue = RangeMinValue + 1
				picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
			Case Keys.Left
				RangeMaxValue = RangeMaxValue - 1
				picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
				Return True
			Case Keys.Right
				RangeMaxValue = RangeMaxValue + 1
				picRangeSlider_SlowValueChanged(New Object, New System.EventArgs)
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
	''' Shows to the user that the video will be muted or not
	''' </summary>
	Private Sub chkMute_CheckedChanged(sender As Object, e As EventArgs) Handles chkMute.CheckedChanged
		lblMute.Image = If(chkMute.Checked, My.Resources.SpeakerOff, My.Resources.SpeakerOn)
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

	Private Sub picFrame_Click(sender As Object, e As EventArgs) Handles picFrame5.Click, picFrame4.Click, picFrame3.Click, picFrame2.Click, picFrame1.Click

	End Sub

	Private Sub picRangeSlider_SlowValueChanged(sender As Object, e As MouseEventArgs) Handles picRangeSlider.MouseUp

	End Sub
End Class
