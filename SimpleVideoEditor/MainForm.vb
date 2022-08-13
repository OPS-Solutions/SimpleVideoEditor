Imports System.Drawing.Imaging
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
    Private mobjGenericToolTip As ToolTipPlus = New ToolTipPlus() 'Tooltip object required for setting tootips on controls

    Private mptStartCrop As New Point(0, 0) 'Point for the top left of the crop rectangle
    Private mptEndCrop As New Point(0, 0) 'Point for the bottom right of the crop rectangle
    Private mrectLastCrop As New Rectangle? 'Crop rectangle used to track the previous video coordinates crop, maintaining stable position even with form resizing

    Private mintAspectWidth As Integer 'Holds onto the width of the video frame for aspect ration computation(Not correct width, but correct aspect)
    Private mintAspectHeight As Integer 'Holds onto the height of the video frame for aspect ration computation(Not correct height, but correct aspect)

    Private mintCurrentFrame As Integer = 0 'Current visible frame in the big picVideo control
    Private mintDisplayInfo As Integer = 0 'Timer value for how long to render special info to the main image
    Private Const RENDER_DECAY_TIME As Integer = 2000

    Private WithEvents mobjMetaData As VideoData 'Video metadata, including things like resolution, framerate, bitrate, etc.
    Private mobjRotation As System.Drawing.RotateFlipType = RotateFlipType.RotateNoneFlipNone 'Keeps track of how the user wants to rotate the image
    Private mblnUserInjection As Boolean = False 'Keeps track of if the user wants to manually modify the resulting commands
    Private mdblPlaybackSpeed As Double = 1
    Private mdblPlaybackVolume As Double = 1
    Private mblnInputMash As Boolean = False 'Whether or not the loaded file is a mash of multiple inputs like image%d.png

    Private mtskPreview As Task(Of Boolean) = Nothing 'Task for grabbing preview frames

    Private Class SpecialOutputProperties
        Public Decimate As Boolean
        Public FPS As Double
        Public ColorKey As Color
        Public PlaybackSpeed As Double
        Public PlaybackVolume As Double
        Public QScale As Double
    End Class

    Private Class TrimData
        Public StartFrame As Integer
        Public EndFrame As Integer
        Public StartPTS As Decimal
        Public EndPTS As Decimal
    End Class

#Region "File Events"
    ''' <summary>
    ''' Opens the file dialog to search for a video.
    ''' </summary>
    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        ofdVideoIn.Filter = "Video Files (*.*)|*.*"
        ofdVideoIn.Title = "Select Video File or Image Collection"
        ofdVideoIn.AddExtension = True
        ofdVideoIn.ShowDialog()
    End Sub

    ''' <summary>
    ''' Load a file when a file is opened in the open file dialog.
    ''' </summary>
    Private Sub ofdVideoIn_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ofdVideoIn.FileOk
        Try
            LoadFiles(ofdVideoIn.FileNames)
        Catch ex As Exception
            MessageBox.Show(ex.Message & vbNewLine & ex.StackTrace)
        End Try
    End Sub

    ''' <summary>
    ''' File is opened, load in the images, and the file attributes.
    ''' </summary>
    Public Sub LoadFile(ByVal fullPath As String, Optional inputMash As Boolean = False)
        mstrVideoPath = fullPath
        Me.Text = Me.Text.Split("-")(0).Trim + $" - {System.IO.Path.GetFileName(mstrVideoPath)}" + " - Open Source"
        If mobjMetaData IsNot Nothing Then
            mobjMetaData.Dispose()
            mobjMetaData = Nothing
        End If
        mobjMetaData = VideoData.FromFile(mstrVideoPath, inputMash)
        lblStatusResolution.ToolTipText = lblStatusResolution.ToolTipText.Split(vbNewLine)(0).Trim & vbNewLine & mobjMetaData.VideoStream.Raw

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
        picVideo.SetImage(Nothing)
        picFrame1.Image = Nothing
        picFrame2.Image = Nothing
        picFrame3.Image = Nothing
        picFrame4.Image = Nothing
        picFrame5.Image = Nothing
        PollPreviewFrames()
        cmsPicVideoExportFrame.Enabled = True
        cmsAutoCrop.Enabled = True
        lblStatusResolution.Text = $"{Me.mobjMetaData.Width} x {Me.mobjMetaData.Height}"
        DefaultToolStripMenuItem.Text = $"Default ({Me.mobjMetaData.Framerate})"
    End Sub

    ''' <summary>
    ''' Attempts to get the input mash from an array of files and open it, if it fails, it will just open the first file
    ''' </summary>
    ''' <param name="files"></param>
    Private Sub LoadFiles(files As String())
        'TODO Detect if user selected a bunch of images that weren't in proper format, and then ask the user if they want to rename them to a proper format
        Dim dummyArgs As List(Of String) = files.ToList
        dummyArgs.Insert(0, "")
        Dim mash As String = GetInputMash(dummyArgs.ToArray)
        ClearControls()
        If mash Is Nothing Then
            LoadFile(files(0))
        Else
            LoadFile(mash, True)
        End If
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
            .QScale = If(chkQuality.Checked, 0, -1),
            .ColorKey = dlgColorKey.Color
        }
        'Limit GIF framerate to whatever is closest to optimal since gif only supports certain equal frame pacing, and ffmpeg will set FPS to like 21.42 with some FPS like 60
        If Not Path.GetExtension(mstrVideoPath).Equals(".gif") AndAlso Path.GetExtension(outputPath).Equals(".gif") Then
            'Gif supports 100, 50, 33.3, 25, 20, 16.6, 14.2, and so on, as delay is set to #/100
            Dim maxRate As Double = mobjMetaData.Framerate * sProperties.PlaybackSpeed
            Dim currentRate As Double = If(sProperties.FPS = 0, maxRate, sProperties.FPS)
            Dim optimalRate As Double = 25
            For index As Integer = 1 To 10
                Dim testRate As Double = 100 / index
                If Math.Abs(currentRate - optimalRate) > Math.Abs(currentRate - testRate) Then
                    optimalRate = testRate
                End If
            Next
            sProperties.FPS = optimalRate
        End If
        Dim ignoreTrim As Boolean = ctlVideoSeeker.RangeMin = ctlVideoSeeker.RangeMinValue And ctlVideoSeeker.RangeMax = ctlVideoSeeker.RangeMaxValue
        'First check if something would conflict with cropping, if it will, just crop it first
        Dim willCrop As Boolean = mptStartCrop.X <> mptEndCrop.X AndAlso mptStartCrop.Y <> mptEndCrop.Y
        Dim postCropOperation As Boolean = sProperties.Decimate
        'MP4 does not work with decimate for some reason, so we should lossless convert to AVI first
        Dim isMP4 As Boolean = IO.Path.GetExtension(outputPath) = ".mp4"
        Dim intermediateFilePath As String = mstrVideoPath
        mproFfmpegProcess = Nothing
        Dim useIntermediate As Boolean = (sProperties.Decimate AndAlso isMP4) OrElse (sProperties.PlaybackSpeed <> 1 AndAlso Not ignoreTrim)
        Dim endFrame As Integer = ctlVideoSeeker.RangeMaxValue
        Dim lastPossible As Integer = mobjMetaData.TotalFrames - 1
        'Find the last frame we actually know exists
        For index As Integer = mobjMetaData.TotalFrames - 1 To 0 Step -1
            If mobjMetaData.ThumbImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                lastPossible = index
                Exit For
            End If
        Next
        endFrame = Math.Min(endFrame, lastPossible)
        Dim frameAfterEnd As Integer = Math.Min(endFrame + 1, lastPossible)
        'Apply very marginal reduction to last frame duration to ensure -ss -t can be used with frame perfect precision
        Dim lastFrameDuration As Decimal = mobjMetaData.ThumbImageCachePTS(frameAfterEnd) - mobjMetaData.ThumbImageCachePTS(Math.Max(0, frameAfterEnd - 1))
        Dim lastFramePTS As Decimal = mobjMetaData.ThumbImageCachePTS(endFrame) + lastFrameDuration * 0.99
        Dim trimData As New TrimData With {
            .StartFrame = ctlVideoSeeker.RangeMinValue,
            .StartPTS = mobjMetaData.ThumbImageCachePTS(ctlVideoSeeker.RangeMinValue),
            .EndFrame = ctlVideoSeeker.RangeMaxValue,
            .EndPTS = lastFramePTS
        }
        If useIntermediate Then
            intermediateFilePath = FileNameAppend(outputPath, "-tempCrop") + If(isMP4, ".avi", "")
            If isMP4 Then
                intermediateFilePath = IO.Path.Combine(IO.Path.GetDirectoryName(outputPath), IO.Path.GetFileNameWithoutExtension(outputPath) + "-tempCrop.avi")
            End If
            'Don't pass in special properties yet, it would be better to decimate after cropping
            RunFfmpeg(mstrVideoPath, intermediateFilePath, 0, mintAspectWidth, mintAspectHeight, New SpecialOutputProperties() With {.PlaybackSpeed = 1, .PlaybackVolume = 1, .QScale = 0}, If(ignoreTrim, New TrimData, trimData), cmbDefinition.Items(0), mptStartCrop, mptEndCrop)
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
        Dim cropRect As Rectangle? = GetRealCrop(mptStartCrop, mptEndCrop, New Size(mintAspectWidth, mintAspectHeight))
        Dim realwidth As Integer = mintAspectWidth
        Dim realheight As Integer = mintAspectHeight
        If cropRect IsNot Nothing Then
            realwidth = cropRect?.Width
            realheight = cropRect?.Height
        End If
        If (Not mobjRotation = RotateFlipType.RotateNoneFlipNone) And (Not mobjRotation = RotateFlipType.Rotate180FlipNone) Then
            SwapValues(realwidth, realheight)
        End If
        'Now you can apply everything else
        RunFfmpeg(intermediateFilePath, outputPath, mobjRotation, realwidth, realheight, sProperties, If(ignoreTrim, New TrimData, trimData), cmbDefinition.Items(cmbDefinition.SelectedIndex), If(useIntermediate, New Point(0, 0), mptStartCrop), If(useIntermediate, New Point(0, 0), mptEndCrop))
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
        Me.UseWaitCursor = True
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

        'Dim multiGrabBarrier As New Barrier(2)
        If fullFrameGrab Is Nothing Then
            mtskPreview = mobjMetaData.GetFfmpegFrameRangesAsync(Me.CreatePreviewFrameDefaults())
            Task.Run(Sub()
                         mtskPreview.Wait()
                         PreviewFinished()
                     End Sub)
        Else
            PreviewFinished()
        End If
    End Sub

    Private Sub PreviewFinished()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() PreviewFinished())
        Else
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

            'Re-enable everything, even if we failed to grab the last frame
            Me.UseWaitCursor = False
            ctlVideoSeeker.Enabled = True
            btnいくよ.Enabled = True
            ExportAudioToolStripMenuItem.Enabled = mobjMetaData.AudioStream IsNot Nothing
        End If
    End Sub


    ''' <summary>
    ''' Generates a list of frames that will be used as the default preview frame images
    ''' </summary>
    Private Function CreatePreviewFrameDefaults() As List(Of Integer)
        Dim previewFrames As New List(Of Integer)
        previewFrames.Add(0)
        previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.25))
        previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.5))
        previewFrames.Add(Math.Floor(mobjMetaData.TotalFrames * 0.75))
        For index As Integer = 10 To 1 Step -1
            previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - index))
        Next
        Return previewFrames
    End Function


    ''' <summary>
    ''' Runs ffmpeg.exe with given command information. Cropping and rotation must be seperated.
    ''' </summary>
    Private Sub RunFfmpeg(ByVal inputFile As String, ByVal outPutFile As String, ByVal flip As RotateFlipType, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal specProperties As SpecialOutputProperties, ByVal trimData As TrimData, ByVal targetDefinition As String, ByVal cropTopLeft As Point, ByVal cropBottomRight As Point)
        If specProperties?.PlaybackSpeed <> 0 Then
            'duration /= specProperties.PlaybackSpeed
            trimData.StartPTS *= specProperties.PlaybackSpeed
            trimData.EndPTS *= specProperties.PlaybackSpeed
        End If
        Dim duration As String = (trimData.EndPTS) - (trimData.StartPTS)

        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"

        'CREATE LIST OF PARAMETERS FOR EACH FILTER
        Dim videoFilterParams As New List(Of String)
        Dim audioFilterParams As New List(Of String)

        If specProperties?.PlaybackVolume <> 0 Then
            If duration > 0 Then
                'duration = Math.Truncate(duration * mobjMetaData.Framerate) / mobjMetaData.Framerate
                Dim startHHMMSS As String = FormatHHMMSSm(trimData.StartPTS / specProperties.PlaybackSpeed)
                processInfo.Arguments += " -ss " & startHHMMSS & " -t " & duration.ToString
            End If
        Else
            'Set up a select filter to trim to exact frames for maximum precision
            videoFilterParams.Add($"select=between(n\,{trimData.StartFrame}\,{trimData.EndFrame}),setpts=PTS-STARTPTS")
        End If
        processInfo.Arguments += $" -i ""{inputFile}"""

        'CROP VIDEO(Can not be done with a rotate, must run twice)
        Dim cropWidth As Integer = newWidth
        Dim cropHeight As Integer = newHeight
        Dim cropRect As Rectangle? = GetRealCrop(cropTopLeft, cropBottomRight, New Size(mintAspectWidth, mintAspectHeight))
        If cropRect IsNot Nothing Then
            videoFilterParams.Add($"crop={cropRect?.Width}:{cropRect?.Height}:{cropRect?.X}:{cropRect?.Y}")
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
                videoFilterParams.Add($"scale={scaleY}:{scaleX}")
                'processInfo.Arguments += $" -s {scaleY}x{scaleX} -threads 4"
            Else
                videoFilterParams.Add($"scale={scaleX}:{scaleY}")
                'processInfo.Arguments += $" -s {scaleX}x{scaleY} -threads 4"
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

        'COLORKEY
        If specProperties.ColorKey.A <> 0 Then
            With specProperties.ColorKey
                videoFilterParams.Add("colorkey=" & String.Format("0x{0:X2}{1:X2}{2:X2}", .R, .G, .B))
            End With
        End If

        'PLAYBACK SPEED
        If specProperties?.PlaybackSpeed <> 1 AndAlso specProperties?.PlaybackSpeed > 0 AndAlso specProperties?.PlaybackSpeed < 3 Then
            videoFilterParams.Add($"setpts={1 / specProperties.PlaybackSpeed}*PTS")
            Dim audioPlaybackSpeed As String = ""
            If specProperties.PlaybackSpeed < 0.5 Then
                'atempo has a limit of between 0.5 and 2.0
                Dim atempoStack As New List(Of Double)
                Dim currentTempo As Double = specProperties.PlaybackSpeed
                While currentTempo < 0.5
                    atempoStack.Add(0.5)
                    currentTempo = currentTempo / 0.5
                End While
                atempoStack.Add(currentTempo)
                For Each objTempo In atempoStack
                    audioPlaybackSpeed += $"atempo={objTempo},"
                Next
                audioPlaybackSpeed = audioPlaybackSpeed.TrimEnd(",")
            Else
                audioPlaybackSpeed = $"atempo={specProperties.PlaybackSpeed}"
            End If
            audioFilterParams.Add(audioPlaybackSpeed)
        End If

        'Maintain transparency when making a gif from images or other transparent content
        If (mobjMetaData.InputMash OrElse specProperties.ColorKey.A <> 0 OrElse mobjMetaData.GetImageFromCache(0).GetBytes(3) = 0) AndAlso IO.Path.GetExtension(outPutFile).ToLower().Equals(".gif") Then
            videoFilterParams.Add("split [a][b];[a] palettegen [p];[b]fifo[c];[c][p] paletteuse=dither=bayer")
            'processInfo.Arguments += " -filter_complex ""[0:v] split [a][b];[a] palettegen [p];[b]fifo[c];[c][p] paletteuse=dither=bayer"""
        End If

        'Check if the user wants to do motion interpolation when using a framerate that would cause duplicate frames
        Dim willHaveDuplicates As Boolean = (specProperties.FPS > mobjMetaData.Framerate * specProperties.PlaybackSpeed) OrElse (specProperties.FPS = 0 AndAlso specProperties.PlaybackSpeed < 1)
        If willHaveDuplicates Then
            If MotionInterpolationToolStripMenuItem.Checked Then
                videoFilterParams.Add($"minterpolate=fps={If(specProperties.FPS = 0, mobjMetaData.Framerate, specProperties.FPS)}:mi_mode=mci:mc_mode=aobmc:me_mode=bidir:vsbmc=1")
            End If
        End If

        'ASSEMBLE VIDEO PARAMETERS
        Dim filterString As String = ""
        Dim complexFilterString As String = ""
        Dim lastOutput As String = "[0:v]"
        For paramIndex As Integer = 0 To videoFilterParams.Count - 1
            If paramIndex = 0 Then
                filterString += " -filter:v " & videoFilterParams(paramIndex)
                complexFilterString += $" -filter_complex """
            Else
                filterString += "," & videoFilterParams(paramIndex)
            End If
            Dim outParam As String = $" [out{paramIndex}]; "
            complexFilterString += $"{lastOutput} " & videoFilterParams(paramIndex) & $" [out{paramIndex}]; "
            lastOutput = $"[out{paramIndex}]"
            If paramIndex = videoFilterParams.Count - 1 Then
                complexFilterString = complexFilterString.Substring(0, complexFilterString.Length - outParam.Length)
                complexFilterString += """"
            End If
            'TODO Build complex filter for maintaining trasparency for gifs
            'If IO.Path.GetExtension(inputFile).ToLower.Equals(".gif") Then
            '    filterString = " -filter_complex ""[0:v] " & videoFilterParams(paramIndex) + ", split [a][b]; [a] palettegen=reserve_transparent=on:transparency_color=ffffff [p]; [b][p] paletteuse"""
            'End If
            'processInfo.Arguments += filterString
        Next
        processInfo.Arguments += complexFilterString

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
    Private Sub picFrame_Click(sender As Object, e As EventArgs) Handles picFrame1.Click, picFrame2.Click, picFrame3.Click, picFrame4.Click, picFrame5.Click
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
        'Find a nearby available frame because the totalframes may have been modified slightly
        If Not mobjMetaData.ImageCacheStatus(newPreview) = ImageCache.CacheStatus.Cached Then
            For index As Integer = Math.Max(0, newPreview - 2) To newPreview + 2
                If mobjMetaData.ImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                    ctlVideoSeeker.PreviewLocation = index
                    Exit Sub
                End If
            Next
        End If
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
            If Not mptStartCrop.DistanceTo(e.Location) < 10 AndAlso Not mptEndCrop.DistanceTo(e.Location) < 10 Then
                mptStartCrop = New Point(e.X, e.Y)
                mptEndCrop = New Point(e.X, e.Y)
            End If
            UpdateCropStatus()
        End If
        picVideo.Invalidate()
    End Sub

    ''' <summary>
    ''' Updates the status label for crop with the correct image coordinates
    ''' </summary>
    Private Sub UpdateCropStatus()
        Dim cropStartImage As Point
        Dim cropEndImage As Point
        Dim cropActual As Rectangle
        If Me.mobjMetaData IsNot Nothing Then
            cropStartImage = picVideo.PointToImage(mptStartCrop, Me.mobjMetaData.Size)
            cropEndImage = picVideo.PointToImage(mptEndCrop, Me.mobjMetaData.Size)
        Else
            cropStartImage = New Point(0, 0)
            cropEndImage = New Point(0, 0)
        End If
        If cropEndImage.X = 0 AndAlso cropEndImage.Y = 0 Then
            'Don't show crop info if we aren't cropping
            lblStatusCropRect.Text = ""
        Else
            cropActual = New Rectangle(cropStartImage, New Size(cropEndImage.X - cropStartImage.X + 1, cropEndImage.Y - cropStartImage.Y + 1))
            'lblStatusCropRect.Text = $"{cropActual.X},{cropActual.Y},{cropActual.Width},{cropActual.Height}"
            lblStatusCropRect.Text = $"{cropActual.Width} x {cropActual.Height}"
            lblStatusCropRect.ToolTipText = lblStatusCropRect.ToolTipText.Split(vbNewLine)(0).Trim() & vbNewLine & $"crop={cropActual.Width}:{cropActual.Height}:{cropActual.X}:{cropActual.Y}"
        End If
    End Sub


    ''' <summary>
    ''' Modifies the crop region, draggable in all directions
    ''' </summary>
    Private Sub picVideo_MouseMove(sender As Object, e As MouseEventArgs) Handles picVideo.MouseMove
        'Display mouse position information
        If Me.mobjMetaData IsNot Nothing Then
            Dim actualImagePoint As Point = picVideo.PointToImage(e.Location, Me.mobjMetaData.Size)
            lblStatusMousePosition.Text = $"{actualImagePoint.X}, {actualImagePoint.Y}"
        End If
        If e.Button = Windows.Forms.MouseButtons.Left Then
            'Update the closest crop point so we can drag either
            If mptEndCrop.DistanceTo(e.Location) < mptStartCrop.DistanceTo(e.Location) Then
                mptEndCrop = New Point(e.X, e.Y)
            Else
                mptStartCrop = New Point(e.X, e.Y)
            End If
            Dim minX As Integer = Math.Max(0, Math.Min(mptStartCrop.X, mptEndCrop.X))
            Dim minY As Integer = Math.Max(0, Math.Min(mptStartCrop.Y, mptEndCrop.Y))
            Dim maxX As Integer = Math.Min(picVideo.Width, Math.Max(mptStartCrop.X, mptEndCrop.X))
            Dim maxY As Integer = Math.Min(picVideo.Height, Math.Max(mptStartCrop.Y, mptEndCrop.Y))
            mptStartCrop.X = minX
            mptStartCrop.Y = minY
            UpdateCropStatus()
            mptEndCrop.X = maxX
            mptEndCrop.Y = maxY
            picVideo.Invalidate()
        End If
    End Sub

    ''' <summary>
    ''' Sets the crop start and end to a position based on bounding non-background contents of the current region
    ''' </summary>
    Private Async Sub AutoCropContractToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles cmsAutoCrop.Click, ContractToolStripMenuItem.Click
        'Ensure context menu goes away when clicking on items that normally may not close it
        cmsPicVideo.Close()
        'Loop through all images, find any pixel different than the respective corners
        Me.UseWaitCursor = True
        'Only grab frames we have confirmed the existence of so the function can return immediately
        Dim allCached As Boolean = False
        For index As Integer = ctlVideoSeeker.RangeMinValue To ctlVideoSeeker.RangeMaxValue
            If Me.mobjMetaData.ImageCacheStatus(index) <> ImageCache.CacheStatus.Cached Then
                allCached = True
                Exit For
            End If
        Next
        If Not allCached Then
            Await mobjMetaData.GetFfmpegFrameAsync(0, -1)
        End If
        Dim displaySize As Size = Me.mobjMetaData.GetImageDataFromCache(0).Size
        Dim topLeftCropStart As Point = mptStartCrop
        Dim bottomRightCropStart As Point = mptEndCrop
        Dim cropRect As Rectangle? = GetRealCrop(mptStartCrop, mptEndCrop, displaySize)
        Dim left As Integer = displaySize.Width - 1
        Dim top As Integer = displaySize.Height - 1
        Dim bottom As Integer = 0
        Dim right As Integer = 0
        Dim largestFrame As Integer = mintCurrentFrame
        Await Task.Run(Sub()
                           For index As Integer = ctlVideoSeeker.RangeMinValue To ctlVideoSeeker.RangeMaxValue
                               If Me.mobjMetaData.ImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                                   'TODO Add something so user can specify alpha that is acceptable, 127 is just here because converting to a gif loses everything below some value(I assume 127 or 128)
                                   Using checkImage As Bitmap = Me.mobjMetaData.GetImageFromCache(index)
                                       Dim boundRect As Rectangle = checkImage.BoundContents(cropRect,, 127)
                                       Dim currentRect As New Rectangle(left, top, right - left, bottom - top)
                                       left = Math.Min(boundRect.Left, left)
                                       top = Math.Min(boundRect.Top, top)
                                       right = Math.Max(boundRect.Right, right)
                                       bottom = Math.Max(boundRect.Bottom, bottom)
                                       Dim potentialRect As New Rectangle(left, top, right - left, bottom - top)

                                       If potentialRect.Area > currentRect.Area Then
                                           largestFrame = index
                                       End If
                                   End Using
                               End If
                           Next
                       End Sub)
        ctlVideoSeeker.PreviewLocation = largestFrame
        If (left = 0 AndAlso top = 0 AndAlso right = displaySize.Width - 1 AndAlso bottom = displaySize.Height - 1) Then
            SetCropPoints(New Point(0, 0), New Point(0, 0))
        Else
            SetCropPoints(New Point(left, top), New Point(right, bottom))
        End If
        picVideo.Invalidate()
        Me.UseWaitCursor = False
    End Sub

    ''' <summary>
    ''' Add autocrop expand option, where the region will expand until a hard line perpendicular to the expand direction is found, such as black bars
    ''' Expands to the first largest border found
    ''' </summary>
    Private Async Sub AutoCropExpandToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExpandToolStripMenuItem.Click
        'Loop through all images, find any pixel different than the respective corners
        Me.UseWaitCursor = True
        'Only grab frames we have confirmed the existence of so the function can return immediately
        Dim allCached As Boolean = True
        For index As Integer = ctlVideoSeeker.RangeMinValue To ctlVideoSeeker.RangeMaxValue
            If Me.mobjMetaData.ImageCacheStatus(index) <> ImageCache.CacheStatus.Cached Then
                allCached = False
                Exit For
            End If
        Next
        If Not allCached Then
            Await mobjMetaData.GetFfmpegFrameAsync(0, -1)
        End If
        Dim displaySize As Size = Me.mobjMetaData.GetImageDataFromCache(0).Size
        Dim topLeftCropStart As Point = mptStartCrop
        Dim bottomRightCropStart As Point = mptEndCrop
        Dim cropRect As Rectangle? = GetRealCrop(mptStartCrop, mptEndCrop, displaySize)
        Dim left As Integer = displaySize.Width - 1
        Dim top As Integer = displaySize.Height - 1
        Dim bottom As Integer = 0
        Dim right As Integer = 0
        Dim largestFrame As Integer = mintCurrentFrame
        Await Task.Run(Sub()
                           For index As Integer = ctlVideoSeeker.RangeMinValue To ctlVideoSeeker.RangeMaxValue
                               If Me.mobjMetaData.ImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                                   'TODO Add something so user can specify alpha that is acceptable, 127 is just here because converting to a gif loses everything below some value(I assume 127 or 128)
                                   Using checkImage As Bitmap = Me.mobjMetaData.GetImageFromCache(index)
                                       Dim boundRect As Rectangle = checkImage.ExpandContents(cropRect, 4)
                                       Dim currentRect As New Rectangle(left, top, right - left, bottom - top)
                                       left = Math.Min(boundRect.Left, left)
                                       top = Math.Min(boundRect.Top, top)
                                       right = Math.Max(boundRect.Right, right)
                                       bottom = Math.Max(boundRect.Bottom, bottom)
                                       Dim potentialRect As New Rectangle(left, top, right - left, bottom - top)

                                       If potentialRect.Area > currentRect.Area Then
                                           largestFrame = index
                                       End If
                                   End Using
                               End If
                           Next
                       End Sub)
        ctlVideoSeeker.PreviewLocation = largestFrame
        If (left = 0 AndAlso top = 0 AndAlso right = displaySize.Width - 1 AndAlso bottom = displaySize.Height - 1) Then
            SetCropPoints(New Point(0, 0), New Point(0, 0))
        Else
            SetCropPoints(New Point(left, top), New Point(right, bottom))
        End If
        picVideo.Invalidate()
        Me.UseWaitCursor = False
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
        mobjGenericToolTip.SetToolTip(ctlVideoSeeker, $"Move sliders to trim video.{vbNewLine}Use [A][D][←][→] to move trim sliders frame by frame.{vbNewLine}Hold [Shift] to move preview slider instead.")
        mobjGenericToolTip.SetToolTip(picVideo, $"Left click and drag to crop.{vbNewLine}Right click to clear crop selection.")
        mobjGenericToolTip.SetToolTip(cmbDefinition, $"Select the ending height of your video.{vbNewLine}Right click for FPS options.")
        mobjGenericToolTip.SetToolTip(btnいくよ, $"Save video.{vbNewLine}Hold ctrl to manually modify ffmpeg arguments.")
        mobjGenericToolTip.SetToolTip(picFrame1, "View first frame of video.")
        mobjGenericToolTip.SetToolTip(picFrame2, "View 25% frame of video.")
        mobjGenericToolTip.SetToolTip(picFrame3, "View middle frame of video.")
        mobjGenericToolTip.SetToolTip(picFrame4, "View 75% frame of video.")
        mobjGenericToolTip.SetToolTip(picFrame5, "View last frame of video.")
        UpdateRotationButton()
        mobjGenericToolTip.SetToolTip(btnBrowse, $"Browse for a video to edit.{vbNewLine}Alternatively, select multiple images with the same name, but numbered like ""image0.png"", ""image1.png"", etc.")
        'mobjGenericToolTip.SetToolTip(lblFileName, "Name of the currently loaded file.")
        UpdateColorKey()
        mobjGenericToolTip.SetToolTip(picPlaybackSpeed, "Playback speed multiplier.")

        'Status  tooltips
        lblStatusMousePosition.ToolTipText = "X,Y position of the mouse in video coordinates"
        lblStatusCropRect.ToolTipText = "Width x Height crop rectangle in video coordinates"
        lblStatusResolution.ToolTipText = $"Original resolution Width x Height of the loaded content.{vbNewLine}Shows more detailed stream information on hover."

        'Context menu tooltips
        MotionInterpolationToolStripMenuItem.ToolTipText = $"Creates new frames to smooth motion when increasing FPS, or decreasing playback speed.{vbNewLine}WARNING: Slow processing and large file size may occur."
        CacheAllFramesToolStripMenuItem.ToolTipText = $"Caches every frame of the video into memory (high RAM requirement).{vbNewLine}Afterwards, frame scrubbing will be borderline instant."
        ContractToolStripMenuItem.ToolTipText = $"Attempts to shrink the current selection rectangle as long as the pixels it overlays are of consistent color."
        ExpandToolStripMenuItem.ToolTipText = $"Attempts to expand the current selection rectangle until the pixels it overlays are of consistent color."
        InjectCustomArgumentsToolStripMenuItem.ToolTipText = $"An additional editable form will appear after selecting a save location, containing the command line arguments that will be sent to ffmpeg."

        'Change window title to current version
        Me.Text &= $" - {ProductVersion} - Open Source"

        'Check if the program was started with a dragdrop exe
        Dim args() As String = Environment.GetCommandLineArgs()
        If args.Length > 1 Then
            For index As Integer = 1 To args.Length - 1
                If System.IO.File.Exists(args(index)) Then
                    Dim mash As String = GetInputMash(args)
                    mblnInputMash = mash IsNot Nothing
                    If mblnInputMash Then
                        LoadFile(GetInputMash(args), mblnInputMash)
                    Else
                        LoadFile(args(index))
                    End If
                    Exit For
                ElseIf System.IO.Directory.Exists(args(index)) Then
                    Dim mash As String = GetInputMash(args)
                    mblnInputMash = mash IsNot Nothing
                    If mblnInputMash Then
                        LoadFile(GetInputMash(args), mblnInputMash)
                    End If
                    Exit For
                End If
            Next
        End If

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
        UpdateCropStatus()
        picVideo.SetImage(Nothing)
        picFrame1.Image = Nothing
        picFrame2.Image = Nothing
        picFrame3.Image = Nothing
        picFrame4.Image = Nothing
        picFrame5.Image = Nothing
        ctlVideoSeeker.SceneFrames = Nothing
        ctlVideoSeeker.Enabled = False
        btnいくよ.Enabled = False
        ExportAudioToolStripMenuItem.Enabled = False
        Me.Text = Me.Text.Split("-")(0).Trim + " - Open Source"
        DefaultToolStripMenuItem.Text = "Default"
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
    ''' Gets the rectangle in video coordinates defining the crop rgion
    ''' </summary>
    Public Function GetRealCrop(ByRef cropTopLeft As Point, ByRef cropBottomRight As Point, contentSize As Size) As Rectangle?
        Dim realTopLeft As Point = picVideo.PointToImage(cropTopLeft, contentSize)
        Dim realBottomRight As Point = picVideo.PointToImage(cropBottomRight, contentSize)
        If ((cropBottomRight.X - cropTopLeft.X) > 0 AndAlso (cropBottomRight.Y - cropTopLeft.Y) > 0) Then
            'Calculate actual crop locations due to bars and aspect ratio changes
            Return New Rectangle(realTopLeft.X, realTopLeft.Y, realBottomRight.X - realTopLeft.X + 1, realBottomRight.Y - realTopLeft.Y + 1)
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Sets the crop points based on pixel locations of the content
    ''' </summary>
    Public Sub SetCropPoints(ByRef cropTopLeft As Point, ByRef cropBottomRight As Point)
        If cropTopLeft.X = 0 AndAlso cropTopLeft.Y = 0 AndAlso cropBottomRight.X = Me.mobjMetaData.Width - 1 AndAlso cropBottomRight.Y = Me.mobjMetaData.Height - 1 Then
            'Perfectly bounds the image
            mptStartCrop = Nothing
            mptEndCrop = Nothing
        End If
        If (cropBottomRight.X - cropTopLeft.X) > 0 And (cropBottomRight.Y - cropTopLeft.Y) > 0 Then
            Dim displaySize As Size = Me.mobjMetaData.GetImageDataFromCache(0).Size
            'Calculate actual crop locations due to bars and aspect ration changes
            Dim actualAspectRatio As Double = (displaySize.Height / displaySize.Width)
            Dim picVideoAspectRatio As Double = (picVideo.Height / picVideo.Width)
            Dim fitRatio As Double = Math.Min(picVideo.Height / displaySize.Height, picVideo.Width / displaySize.Width)
            Dim fullHeight As Double = If(actualAspectRatio < picVideoAspectRatio, (displaySize.Height / (actualAspectRatio / picVideoAspectRatio)), displaySize.Height)
            Dim fullWidth As Double = If(actualAspectRatio > picVideoAspectRatio, (displaySize.Width / (picVideoAspectRatio / actualAspectRatio)), displaySize.Width)
            Dim verticalBarSizeRealPx As Integer = If(actualAspectRatio < picVideoAspectRatio, (picVideo.Height - displaySize.Height * fitRatio) / 2, 0)
            Dim horizontalBarSizeRealPx As Integer = If(actualAspectRatio > picVideoAspectRatio, (picVideo.Width - displaySize.Width * fitRatio) / 2, 0)
            Dim displayWidth As Double = picVideo.Width - (horizontalBarSizeRealPx * 2)
            Dim displayHeight As Double = picVideo.Height - (verticalBarSizeRealPx * 2)
            Dim realStartCrop As Point = New Point(Math.Max(0, (cropTopLeft.X / displaySize.Width) * displayWidth + horizontalBarSizeRealPx), Math.Max(0, (cropTopLeft.Y / displaySize.Height) * displayHeight + verticalBarSizeRealPx))
            Dim realEndCrop As Point = New Point(Math.Max(0, (cropBottomRight.X / displaySize.Width) * displayWidth + horizontalBarSizeRealPx), Math.Max(0, (cropBottomRight.Y / displaySize.Height) * displayHeight + verticalBarSizeRealPx))
            mptStartCrop = realStartCrop
            mptEndCrop = realEndCrop
            UpdateCropStatus()
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
            If checkedItem Is DefaultToolStripMenuItem Then
                Return 0
            Else
                'Parse FPS
                Return Integer.Parse(Regex.Match(checkedItem.Text, "\d*").Value)
            End If
        End Get
    End Property

    ''' <summary>
    ''' Checks the given args to see if they can be in the form of someName%d.png
    ''' Returns nothing if single argumnt or no images found
    ''' </summary>
    ''' <param name="args"></param>
    ''' <returns></returns>
    Private Function GetInputMash(args() As String) As String
        'Check if the input is a folder, or list of files
        'If so, try to generate a type of input like image%d.png that ffmpeg accepts as a single input
        Dim fileNames As New List(Of String)
        Dim largestPad As Integer = 0
        If args.Count = 2 Then
            If IO.File.Exists(args(1)) Then
                Return Nothing
            ElseIf IO.Directory.Exists(args(1)) Then
                'Directory
                Dim objFiles As String() = Directory.GetFiles(args(1))
                For index As Integer = 0 To objFiles.Count - 1
                    If objFiles(index).IsVBImage() Then
                        fileNames.Add(objFiles(index))
                    End If
                Next
            End If
        ElseIf args.Count > 2 Then
            'Multiple files
            For index As Integer = 1 To args.Count - 1
                If args(index).IsVBImage() Then
                    fileNames.Add(args(index))
                End If
            Next
        End If
        If fileNames.Count < 1 Then
            Return Nothing
        End If
        'Sort the files in case the user dragged them in a way that caused something that was not the first file to appear first in the args list
        fileNames.Sort(Function(string1, string2) string1.CompareNatural(string2))

        'Try to extract the constant data between the file names by removing numbers
        Dim numberRegex As New Text.RegularExpressions.Regex("(?<zeros>0+)*\d+")
        'Check padding intensity
        Dim firstMatch As Match = numberRegex.Match(fileNames(0))
        Dim leadingZeros As Integer = firstMatch.Groups("zeros").Length
        Dim pattern As String = ""
        If leadingZeros = 0 Then
            pattern = "%d"
        ElseIf leadingZeros > 0 Then
            pattern = "%0" & leadingZeros + 1 & "d"
        Else
            Return Nothing
        End If
        Dim directoryPath As String = System.IO.Path.GetDirectoryName(fileNames(0))
        Dim fileName As String = System.IO.Path.GetFileName(fileNames(0))
        fileName = numberRegex.Replace(fileName, pattern)
        Return System.IO.Path.Combine(directoryPath, fileName)
    End Function
#End Region

#Region "Settings UI"
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
            Case Keys.Left
                ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue - 1
                ctlVideoSeeker.Invalidate()
            Case Keys.Right
                ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue + 1
                ctlVideoSeeker.Invalidate()
            Case Keys.A Or Keys.Shift, Keys.Left Or Keys.Shift
                ctlVideoSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation - 1
                ctlVideoSeeker.Invalidate()
            Case Keys.D Or Keys.Shift, Keys.Right Or Keys.Shift
                ctlVideoSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation + 1
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
        For Each objItem As ToolStripItem In cmsPlaybackVolume.Items
            If objItem.GetType = GetType(ToolStripSeparator) Then
                Exit For
            End If
            CType(objItem, ToolStripMenuItem).Checked = False
        Next
        If chkMute.Checked Then
            mdblPlaybackVolume = 0
            MuteToolStripMenuItem.Checked = True
        Else
            mdblPlaybackVolume = 1
            UnmuteToolStripMenuItem.Checked = True
        End If
        Dim volumeString As String = If(mdblPlaybackVolume = 0, "muted.", If(mdblPlaybackVolume = 1, "unmuted.", $"{mdblPlaybackVolume}%"))
        mobjGenericToolTip.SetToolTip(chkMute, If(chkMute.Checked, "Unmute", "Mute") & $" the videos audio track.{vbNewLine}Currently " & If(chkMute.Checked, "muted.", volumeString))
    End Sub


    ''' <summary>
    ''' Changes the display text when changing quality check control
    ''' </summary>
    Private Sub chkQuality_CheckedChanged(sender As Object, e As EventArgs) Handles chkQuality.CheckChanged
        mobjGenericToolTip.SetToolTip(chkQuality, If(chkQuality.Checked, "Automatic quality.", $"Force equivalent quality.{vbNewLine}WARNING: Slow processing and large file size may occur.") & $"{vbNewLine}Currently " & If(chkQuality.Checked, "forced equivalent (slow and large).", "automatic (fast and small)."))
    End Sub

    ''' <summary>
    ''' Toggles whether the video will be decimated or not, and changes the image to make it obvious
    ''' </summary>
    Private Sub chkDeleteDuplicates_CheckedChanged(sender As Object, e As EventArgs) Handles chkDeleteDuplicates.CheckChanged
        mobjGenericToolTip.SetToolTip(chkDeleteDuplicates, If(chkDeleteDuplicates.Checked, "Allow Duplicate Frames", $"Delete Duplicate Frames.{vbNewLine}WARNING: Audio may go out of sync.") & $"{vbNewLine}Currently " & If(chkDeleteDuplicates.Checked, "deleting them.", "allowing them."))
    End Sub

    ''' <summary>
    ''' Rotates the final video by 90 degrees per click, and updates the graphic
    ''' </summary>
    Private Sub imgRotate_Click(sender As Object, e As EventArgs) Handles imgRotate.Click
        Select Case mobjRotation
            Case RotateFlipType.RotateNoneFlipNone
                mobjRotation = RotateFlipType.Rotate90FlipNone
            Case RotateFlipType.Rotate90FlipNone
                mobjRotation = RotateFlipType.Rotate180FlipNone
            Case RotateFlipType.Rotate180FlipNone
                mobjRotation = RotateFlipType.Rotate270FlipNone
            Case RotateFlipType.Rotate270FlipNone
                mobjRotation = RotateFlipType.RotateNoneFlipNone
            Case Else
                mobjRotation = RotateFlipType.RotateNoneFlipNone
        End Select
        UpdateRotationButton()
    End Sub

    ''' <summary>
    ''' Updates the rotation setting icon to reflect the current rotation selection
    ''' </summary>
    Private Sub UpdateRotationButton()
        Select Case mobjRotation
            Case RotateFlipType.Rotate90FlipNone
                mobjGenericToolTip.SetToolTip(imgRotate, $"Rotate to 180°.{vbNewLine}Currently 90°.")
            Case RotateFlipType.Rotate180FlipNone
                mobjGenericToolTip.SetToolTip(imgRotate, $"Rotate to 270°.{vbNewLine}Currently 180°.")
            Case RotateFlipType.Rotate270FlipNone
                mobjGenericToolTip.SetToolTip(imgRotate, $"Do not rotate.{vbNewLine}Currently will rotate 270°.")
            Case RotateFlipType.RotateNoneFlipNone
                mobjGenericToolTip.SetToolTip(imgRotate, $"Rotate to 90°.{vbNewLine}Currently 0°.")
        End Select
        Dim rotatedIcon As Image = New Bitmap(My.Resources.Rotate)
        rotatedIcon.RotateFlip(mobjRotation)
        imgRotate.Image = rotatedIcon
        imgRotate.Invalidate()
    End Sub

    Private Sub cmsRotation_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsRotation.ItemClicked
        For Each objItem As ToolStripMenuItem In cmsRotation.Items
            objItem.Checked = False
        Next
        Select Case cmsRotation.Items.IndexOf(e.ClickedItem)
            Case 0
                mobjRotation = RotateFlipType.RotateNoneFlipNone
            Case 1
                mobjRotation = RotateFlipType.Rotate90FlipNone
            Case 2
                mobjRotation = RotateFlipType.Rotate180FlipNone
            Case 3
                mobjRotation = RotateFlipType.Rotate270FlipNone
        End Select
        UpdateRotationButton()
        CType(e.ClickedItem, ToolStripMenuItem).Checked = True
    End Sub

    Private Sub cmsFrameRate_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsFrameRate.ItemClicked
        If cmsFrameRate.FirstSeparator < cmsFrameRate.Items.IndexOf(e.ClickedItem) Then
            'Ignore acting as radio buttons after the first separator
            Exit Sub
        End If
        For Each objItem As ToolStripItem In cmsFrameRate.Items
            If objItem.GetType = GetType(ToolStripSeparator) Then
                Exit For
            End If
            CType(objItem, ToolStripMenuItem).Checked = False
        Next
        CType(e.ClickedItem, ToolStripMenuItem).Checked = True
    End Sub

    Private Sub picColorKey_Click(sender As Object, e As EventArgs) Handles picColorKey.Click
        Select Case dlgColorKey.ShowDialog
            Case DialogResult.OK
                UpdateColorKey()
        End Select
    End Sub

    Private Sub ClearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClearToolStripMenuItem.Click
        dlgColorKey.Color = Color.FromArgb(0, 1, 1, 1)
        picColorKey.BackColor = Color.Lime
        UpdateColorKey()
    End Sub

    ''' <summary>
    ''' Updates the color key tooltip and color to reflect the current selected color
    ''' </summary>
    Private Sub UpdateColorKey()
        If dlgColorKey.Color.A <> 0 Then
            picColorKey.BackColor = dlgColorKey.Color
            mobjGenericToolTip.SetToolTip(picColorKey, $"Color that will be made transparent if the output file type supports it.{vbNewLine}Currently {dlgColorKey.Color}.")
        Else
            picColorKey.BackColor = Color.Lime
            mobjGenericToolTip.SetToolTip(picColorKey, $"Color that will be made transparent if the output file type supports it.{vbNewLine}Currently not set.")
        End If
    End Sub

    Private Sub picPlaybackSpeed_Click(sender As Object, e As EventArgs) Handles picPlaybackSpeed.Click
        cmsPlaybackSpeed.Show(picPlaybackSpeed.PointToScreen(CType(e, MouseEventArgs).Location))
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
        If cmsPlaybackVolume.FirstSeparator < cmsPlaybackVolume.Items.IndexOf(e.ClickedItem) Then
            'Ignore acting as radio buttons after the first separator
            Exit Sub
        End If
        For Each objItem As ToolStripItem In cmsPlaybackVolume.Items
            If objItem.GetType = GetType(ToolStripSeparator) Then
                Exit For
            End If
            CType(objItem, ToolStripMenuItem).Checked = False
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
#End Region

    ''' <summary>
    ''' Clears the crop settings from the main picVideo control
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub cmsPicVideoClear_Click(sender As Object, e As EventArgs) Handles cmsPicVideoClear.Click
        mptStartCrop = New Point(0, 0)
        mptEndCrop = New Point(0, 0)
        UpdateCropStatus()
        picVideo.Invalidate()
    End Sub

#Region "Frame Export"

    ''' <summary>
    ''' User right clicked on the big image and wants to export that frame
    ''' </summary>
    Private Sub cmsPicVideoExportFrame_Click(sender As Object, e As EventArgs) Handles cmsPicVideoExportFrame.Click, CurrentToolStripMenuItem.Click
        'Ensure context menu goes away when clicking on items that normally may not close it
        cmsPicVideo.Close()
        Using sfdExportFrame As New SaveFileDialog
            sfdExportFrame.Title = "Select Frame Save Location"
            sfdExportFrame.Filter = "PNG|*.png|BMP|*.bmp|All files (*.*)|*.*"
            Dim validExtensions() As String = sfdVideoOut.Filter.Split("|")
            sfdExportFrame.FileName = "frame_" & mintCurrentFrame.ToString & ".png"
            sfdExportFrame.OverwritePrompt = True
            Select Case sfdExportFrame.ShowDialog()
                Case DialogResult.OK
                    If File.Exists(sfdExportFrame.FileName) Then
                        My.Computer.FileSystem.DeleteFile(sfdExportFrame.FileName)
                    End If
                    mobjMetaData.ExportFfmpegFrames(mintCurrentFrame, mintCurrentFrame, sfdExportFrame.FileName, GetRealCrop(mptStartCrop, mptEndCrop, Me.mobjMetaData.Size))
                Case Else
                    'Do nothing
            End Select
        End Using
    End Sub

    ''' <summary>
    ''' User right clicked on the big image and wants to export all frames within the range
    ''' </summary>
    Private Sub ExportFrameRange_Click(sender As Object, e As EventArgs) Handles SelectedRangeToolStripMenuItem.Click
        Using sfdExportFrame As New SaveFileDialog
            sfdExportFrame.Title = "Select Save Location and Name Style"
            sfdExportFrame.Filter = "PNG|*.png|BMP|*.bmp|All files (*.*)|*.*"
            Dim validExtensions() As String = sfdVideoOut.Filter.Split("|")
            Dim startFrame As Integer = ctlVideoSeeker.RangeMinValue
            Dim endFrame As Integer = ctlVideoSeeker.RangeMaxValue
            sfdExportFrame.FileName = "frame_#.png"
            sfdExportFrame.OverwritePrompt = True
            Select Case sfdExportFrame.ShowDialog()
                Case DialogResult.OK
                    Dim chosenName As String = sfdExportFrame.FileName
                    If chosenName.Contains("#") Then
                        chosenName = chosenName.Replace("#", "%03d")
                    Else
                        chosenName = Path.Combine({Path.GetDirectoryName(chosenName), Path.GetFileNameWithoutExtension(chosenName), "%03d", ".png"})
                    End If
                    mobjMetaData.ExportFfmpegFrames(startFrame, endFrame, chosenName, GetRealCrop(mptStartCrop, mptEndCrop, Me.mobjMetaData.Size))
                Case Else
                    'Do nothing
            End Select
        End Using
    End Sub
#End Region

    ''' <summary>
    ''' Given an array of scene change values, compress the array into a new array of given size
    ''' </summary>
    Public Shared Function CompressSceneChanges(ByRef sceneChanges As Double(), ByVal newTotalFrames As Integer) As Double()
        Dim compressedSceneChanges(newTotalFrames - 1) As Double
        If sceneChanges.Length <= newTotalFrames Then
            'No need to compress if we aren't using the full span anyways
            Return sceneChanges.Clone
        Else
            'Local Maximums
            For frameIndex As Integer = 0 To sceneChanges.Count - 1
                Dim compressedIndex As Integer = Math.Floor(frameIndex * newTotalFrames / sceneChanges.Count)
                compressedSceneChanges(compressedIndex) = Math.Max(compressedSceneChanges(compressedIndex), sceneChanges(frameIndex))
            Next
        End If
        Return compressedSceneChanges
    End Function

    Private Sub ctlVideoSeeker_RangeChanged(newVal As Integer) Handles ctlVideoSeeker.SeekChanged
        If mstrVideoPath IsNot Nothing AndAlso mstrVideoPath.Length > 0 AndAlso mobjMetaData IsNot Nothing Then
            If Not mintCurrentFrame = newVal Then
                mintCurrentFrame = newVal
                If mobjMetaData.ImageCacheStatus(mintCurrentFrame) = ImageCache.CacheStatus.Cached Then
                    'Grab immediate
                    picVideo.SetImage(mobjMetaData.GetImageFromCache(mintCurrentFrame))
                Else
                    If mobjMetaData.ThumbImageCacheStatus(mintCurrentFrame) = ImageCache.CacheStatus.Cached Then
                        'Check for low res thumbnail if we have it
                        picVideo.SetImage(mobjMetaData.GetImageFromThumbCache(mintCurrentFrame))
                    Else
                        'Loading image...
                        picVideo.SetImage(Nothing)
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
                    Me.BeginInvoke(Sub()
                                       NewFrameCached(sender, objCache, ranges)
                                   End Sub)
                Else
                    'Grab immediate
                    Dim gotImage As Bitmap = mobjMetaData.GetImageFromCache(mintCurrentFrame, objCache)
                    If picVideo.Image Is Nothing OrElse picVideo.Image.Width < gotImage.Width Then
                        picVideo.SetImage(gotImage)
                    Else
                        gotImage.Dispose()
                    End If
                End If
                Exit For
            End If
        Next
    End Sub

    Private Sub PreviewsLoaded(sender As Object, objCache As ImageCache, ranges As List(Of List(Of Integer))) Handles mobjMetaData.RetrievedFrames
        'Don't use thumbs for previews
        If objCache Is mobjMetaData.ThumbFrames Then
            Exit Sub
        End If
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub()
                               PreviewsLoaded(sender, objCache, ranges)
                           End Sub)
        Else
            Dim previewFrames As List(Of Integer) = Me.CreatePreviewFrameDefaults()

            For Each objRange In ranges
                For previewIndex As Integer = 0 To previewFrames.Count - 1
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
                            Case Else
                                targetPreview = picFrame5
                                For index As Integer = previewFrames.Last To previewFrames(4) Step -1
                                    If mobjMetaData.ImageCacheStatus(index) = ImageCache.CacheStatus.Cached Then
                                        mobjMetaData.OverrideTotalFrames(index + 1)
                                        RemoveHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_RangeChanged
                                        ctlVideoSeeker.UpdateRange(False)
                                        AddHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_RangeChanged
                                        Exit For
                                    End If
                                Next
                        End Select
                        If targetPreview.Image Is Nothing OrElse targetPreview.Image.Width < gotImage.Width Then
                            targetPreview.SetImage(gotImage)
                        Else
                            gotImage.Dispose()
                        End If
                    End If
                Next
            Next
        End If
    End Sub

    Private Async Sub CacheAllFramesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CacheAllFramesToolStripMenuItem.Click
        Me.UseWaitCursor = True
        Await mobjMetaData.GetFfmpegFrameAsync(0, -1)
        Me.UseWaitCursor = False
    End Sub

    Private Sub HolePuncherToolToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HolePuncherToolToolStripMenuItem.Click
        HolePuncherForm.Show()
    End Sub

    Private Sub InjectCustomArgumentsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles InjectCustomArgumentsToolStripMenuItem.Click
        mblnUserInjection = True
        SaveAs()
    End Sub

    Private Sub picVideo_MouseLeave(sender As Object, e As EventArgs) Handles picVideo.MouseLeave
        lblStatusMousePosition.Text = ""
    End Sub

    Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If mobjMetaData IsNot Nothing AndAlso mrectLastCrop IsNot Nothing Then
            'Update crop locations if needed
            mptStartCrop = picVideo.ImagePointToClient(mrectLastCrop?.Location, Me.mobjMetaData.Size)
            mptEndCrop = picVideo.ImagePointToClient(mrectLastCrop?.BottomRight, Me.mobjMetaData.Size)
        End If
    End Sub

    Private Sub MainForm_ResizeBegin(sender As Object, e As EventArgs) Handles MyBase.ResizeBegin
        If mobjMetaData IsNot Nothing Then
            mrectLastCrop = GetRealCrop(mptStartCrop, mptEndCrop, Me.mobjMetaData.Size)
        End If
    End Sub

#Region "DragDrop"
    Private Sub MainForm_DragDrop(sender As Object, e As DragEventArgs) Handles MyBase.DragDrop
        Dim files() As String = e.Data.GetData(DataFormats.FileDrop)
        Me.Activate()

        Select Case MessageBox.Show(Me, $"Open {files.Count} file(s)?", "Open File(s)?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
            Case DialogResult.OK
                LoadFiles(files)
            Case Else
                Exit Sub
        End Select
    End Sub

    Private Sub MainForm_DragEnter(sender As Object, e As DragEventArgs) Handles MyBase.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            'Change the cursor and enable the DragDrop event
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub ExportAudioToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExportAudioToolStripMenuItem.Click
        'Ensure context menu goes away when clicking on items that normally may not close it
        cmsPicVideo.Close()
        Using sfdExportAudio As New SaveFileDialog
            sfdExportAudio.Title = "Select Audio Save Location"
            sfdExportAudio.Filter = "MP3|*.mp3|aac|*.aac|All files (*.*)|*.*"
            Dim detectedStreamExtension As String = "." & mobjMetaData.AudioStream.Type
            Dim filterIndex As Integer = 0
            For Each objFilter In sfdExportAudio.Filter.Split("|")
                If objFilter.ToLower.Equals(mobjMetaData.AudioStream.Type) Then
                    sfdExportAudio.FilterIndex = filterIndex
                    Exit For
                End If
                filterIndex += 1
            Next

            sfdExportAudio.FileName = System.IO.Path.GetFileNameWithoutExtension(mobjMetaData.FullPath) & detectedStreamExtension
            sfdExportAudio.OverwritePrompt = True
            Select Case sfdExportAudio.ShowDialog()
                Case DialogResult.OK
                    If File.Exists(sfdExportAudio.FileName) Then
                        My.Computer.FileSystem.DeleteFile(sfdExportAudio.FileName)
                    End If
                    mobjMetaData.ExportFfmpegAudioStream(sfdExportAudio.FileName, detectedStreamExtension)
                Case Else
                    'Do nothing
            End Select
        End Using
    End Sub
#End Region

End Class
