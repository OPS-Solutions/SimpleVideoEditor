﻿Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.IO.Pipes
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.Remoting.Metadata.W3cXsd2001
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualBasic.Devices

Public Class MainForm
    'FFMPEG Usefull Commands
    'https://www.labnol.org/internet/useful-ffmpeg-commands/28490/

    Private mstrVideoPath As String = "" 'Fullpath of the video file being edited
    Private mproFfmpegProcess As Process 'Keeps track of ffmpeg process that we spawn with RunFfmpeg() so we can wait for completion and hook to outputs
    Private mobjErrorLog As New StringBuilder 'Keeps track of process error data from RunFfmpeg()
    Private mobjOutputLog As New StringBuilder 'Keeps track of process output data from RunFfmpeg()
    Private mobjGenericToolTip As ToolTipPlus = New ToolTipPlus() 'Tooltip object required for setting tootips on controls

    Private mptStartCrop As New Point(0, 0) 'Point for the top left of the crop rectangle, in video coordinates
    Private mptEndCrop As New Point(0, 0) 'Point for the bottom right of the crop rectangle, in video coordinates
    Private mblnCropping As Boolean = False 'Flag for if the user has clicked to crop, useful to avoid potential mousemove events that were not initiated by the user clicking on the panel

    Private mintCurrentFrame As Integer = 0 'Current visible frame in the big picVideo control
    Private mintDisplayInfo As Integer = 0 'Timer value for how long to render special info to the main image
    Private Const RENDER_DECAY_TIME As Integer = 2000
    Private Const CROP_COLLISION_RADIUS As Integer = 10

    Private WithEvents mobjMetaData As VideoData 'Video metadata, including things like resolution, framerate, bitrate, etc.
    Private mblnUserInjection As Boolean = False 'Keeps track of if the user wants to manually modify the resulting commands
    Private mblnInputMash As Boolean = False 'Whether or not the loaded file is a mash of multiple inputs like image%d.png

    Private mobjOutputProperties As New SpecialOutputProperties 'Keeps track of settings to apply to the final output video

    Private mtskPreview As Task(Of Boolean) = Nothing 'Task for grabbing preview frames

    Private runTextbox As ManualEntryForm = New ManualEntryForm("") With {.Text = "FFMPEG Data", .Width = 680, .Height = 400, .Persistent = True} 'For displaying data as it comes in from ffmpeg so the user gets more than just a loading cursor
    Private WithEvents subForm As New SubtitleForm

    Private mthdFrameGrabber As Thread 'Handles grabbing frames when user clicks to view
    Private mobjFramesToGrab As New System.Collections.Concurrent.BlockingCollection(Of Integer) 'Queue of frames to grab, will be emptied until latest relevant item to avoid wasting CPU
    Private mthdRenderDecay As Thread 'Loops and reduces display time for frame display on image preview

    ''' <summary>
    ''' Stores the last location of the form, used to detect location delta for moving child forms
    ''' </summary>
    Private lastLocation As New Point

    Private Class SpecialOutputProperties
        Implements ICloneable

        Public Decimate As Boolean
        Public FPS As Double
        Public ColorKey As Color
        Public PlaybackSpeed As Double = 1
        Public PlaybackVolume As Double = 1
        Public QScale As Double
        Public Rotation As System.Drawing.RotateFlipType = RotateFlipType.RotateNoneFlipNone 'Keeps track of how the user wants to rotate the image
        Public Subtitles As String = ""
        Public BakeSubs As Boolean = True
        ''' <summary>
        ''' Angle of rotation in degrees
        ''' </summary>
        ''' <returns></returns>
        Public Property RotationAngle As Integer
            Get
                Select Case Rotation
                    Case RotateFlipType.RotateNoneFlipNone
                        Return 0
                    Case RotateFlipType.Rotate90FlipNone
                        Return 90
                    Case RotateFlipType.Rotate180FlipNone
                        Return 180
                    Case RotateFlipType.Rotate270FlipNone
                        Return 270
                    Case Else
                        Return 0
                End Select
            End Get
            Set(value As Integer)
                Select Case value
                    Case 0
                        Rotation = RotateFlipType.RotateNoneFlipNone
                    Case 90
                        Rotation = RotateFlipType.Rotate90FlipNone
                    Case 180
                        Rotation = RotateFlipType.Rotate180FlipNone
                    Case 270
                        Rotation = RotateFlipType.Rotate270FlipNone
                    Case Else
                        Rotation = 0
                End Select
            End Set
        End Property

        Public Function Clone() As Object Implements ICloneable.Clone
            Return Me.MemberwiseClone
        End Function
    End Class

    Private Class TrimData
        Public StartFrame As Integer
        Public EndFrame As Integer
        Public StartPTS As Decimal
        Public EndPTS As Decimal
    End Class

    Private ReadOnly Property CropRect As Rectangle?
        Get
            If mptStartCrop.X = mptEndCrop.X OrElse mptStartCrop.Y = mptEndCrop.Y Then
                Return Nothing
            Else
                Return New Rectangle(mptStartCrop.X, mptStartCrop.Y, mptEndCrop.X - mptStartCrop.X, mptEndCrop.Y - mptStartCrop.Y)
            End If
        End Get
    End Property

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
            ClearControls()
            MessageBox.Show($"Failed to load selected file(s) '{ofdVideoIn.FileNames.Flatten()}'" & vbNewLine & ex.ToString)
        End Try
    End Sub

    ''' <summary>
    ''' File is opened, load in the images, and the file attributes.
    ''' </summary>
    Public Sub LoadFile(ByVal fullPath As String, Optional inputMash As Boolean = False)
        mstrVideoPath = fullPath
        sfdVideoOut.FileName = System.IO.Path.GetFileName(FileNameAppend(mstrVideoPath, "-SHINY"))
        If mobjMetaData IsNot Nothing Then
            mobjMetaData.Dispose()
            mobjMetaData = Nothing
        End If
        ClearControls()
        mobjMetaData = VideoData.FromFile(mstrVideoPath, inputMash)
        Me.Text = Me.Text.Split("-")(0).Trim + $" - {System.IO.Path.GetFileName(mstrVideoPath)}" + " - Open Source"
        ctlVideoSeeker.Enabled = True
        RefreshStatusToolTips()


        RemoveHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_SeekChanged
        RemoveHandler subForm.PreviewChanged, AddressOf subForm_PreviewChanged
        ctlVideoSeeker.MetaData = mobjMetaData
        subForm.Seeker.MetaData = mobjMetaData
        AddHandler ctlVideoSeeker.SeekChanged, AddressOf ctlVideoSeeker_SeekChanged
        AddHandler subForm.PreviewChanged, AddressOf subForm_PreviewChanged

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
        CheckSave()
        lblStatusResolution.Text = $"{mobjMetaData.Width} x {mobjMetaData.Height}"
        DefaultToolStripMenuItem.Text = $"Default ({Me.mobjMetaData.Framerate})"
    End Sub

    ''' <summary>
    ''' Attempts to get the input mash from an array of files and open it, if it fails, it will just open the first file
    ''' </summary>
    ''' <param name="files"></param>
    Private Sub LoadFiles(files As String())
        Dim dummyArgs As List(Of String) = files.ToList
        dummyArgs.Insert(0, "")
        Dim mash As String = GetInputMash(dummyArgs.ToArray)
        If mash Is Nothing Then
            LoadFile(files(0))
        Else
            LoadFile(mash, True)
        End If
    End Sub

    ''' <summary>
    ''' Sets up necessary information and runs ffmpeg targetting the desired filepath, saving to the target location, overwriting or deleting as necessary
    ''' </summary>
    Private Async Sub SaveFile(ByVal outputPath As String, Optional overwrite As Boolean = False)
        Me.UseWaitCursor = True
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
        Dim sProperties As SpecialOutputProperties = mobjOutputProperties.Clone
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
        Dim ignoreTrim As Boolean = Not ctlVideoSeeker.RangeModified
        Dim trimData As TrimData = Nothing
        If Not ignoreTrim Then
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
            trimData = New TrimData With {
                        .StartFrame = ctlVideoSeeker.RangeMinValue,
                        .StartPTS = mobjMetaData.ThumbImageCachePTS(ctlVideoSeeker.RangeMinValue),
                        .EndFrame = ctlVideoSeeker.RangeMaxValue,
                        .EndPTS = lastFramePTS
                    }
        End If

        'First check if something would conflict with cropping, if it will, just crop it first
        Dim willCrop As Boolean = mptStartCrop.X <> mptEndCrop.X AndAlso mptStartCrop.Y <> mptEndCrop.Y
        Dim postCropOperation As Boolean = sProperties.Decimate
        'MP4 does not work with decimate for some reason, so we should lossless convert to AVI first
        Dim isMP4 As Boolean = IO.Path.GetExtension(outputPath) = ".mp4"
        Dim intermediateFilePath As String = mstrVideoPath
        mproFfmpegProcess = Nothing
        Dim useIntermediate As Boolean = (sProperties.Decimate AndAlso isMP4) OrElse (sProperties.PlaybackSpeed <> 1 AndAlso Not ignoreTrim)

        Dim errorLog As New StringBuilder
        Dim outputLog As New StringBuilder
        Dim cropArea As Rectangle = If(Me.CropRect, New Rectangle(0, 0, mobjMetaData.Width, mobjMetaData.Height))
        Dim runArgs As String = ""
        'If doing an intermediate conversion when making a gif, we don't want to mess up the framerate, so save as an avi first
        Dim isGIF As Boolean = IO.Path.GetExtension(outputPath) = ".gif"
        Dim sourceIsGIF As Boolean = IO.Path.GetExtension(mstrVideoPath) = ".gif"
        Dim workingMetadata As VideoData = mobjMetaData
        If useIntermediate Then
            intermediateFilePath = FileNameAppend(outputPath, "-tempCrop") + If(isMP4 OrElse (Not sourceIsGIF AndAlso isGIF), ".avi", "")
            If isMP4 Then
                intermediateFilePath = IO.Path.Combine(IO.Path.GetDirectoryName(outputPath), IO.Path.GetFileNameWithoutExtension(outputPath) + "-tempCrop.avi")
            End If
            'Don't pass in special properties yet, it would be better to decimate after cropping
            RunFfmpeg(workingMetadata, intermediateFilePath, New SpecialOutputProperties() With {.PlaybackSpeed = 1, .Rotation = RotateFlipType.RotateNoneFlipNone, .PlaybackVolume = If(mobjOutputProperties.PlaybackVolume <= 0, 0, 1), .QScale = 0}, If(ignoreTrim, Nothing, trimData), cmbDefinition.Items(0), cropArea)
            If Not ignoreTrim Then
                ignoreTrim = True
            End If
            If mproFfmpegProcess Is Nothing Then
                Exit Sub
            End If

            'Await Task.Run(Sub() mproFfmpegProcess.WaitForExit())
            mproFfmpegProcess.BeginErrorReadLine()
            mproFfmpegProcess.BeginOutputReadLine()
            runArgs += mproFfmpegProcess.StartInfo.Arguments
            Await mproFfmpegProcess.WaitForExitAsync()
            workingMetadata = VideoData.FromFile(intermediateFilePath)
            'Check if user canceled manual entry
            CheckOutput(intermediateFilePath, runArgs, False)
            If Not File.Exists(intermediateFilePath) Then
                Exit Sub
            End If
        End If
        Try
            'Now you can apply everything else
            RunFfmpeg(workingMetadata, outputPath, sProperties, If(ignoreTrim, Nothing, trimData), cmbDefinition.Items(cmbDefinition.SelectedIndex), If(useIntermediate, New Rectangle?, cropArea))
            If mproFfmpegProcess Is Nothing Then
                Exit Sub
            End If
            mproFfmpegProcess.BeginErrorReadLine()
            mproFfmpegProcess.BeginOutputReadLine()
            runArgs += vbNewLine & mproFfmpegProcess.StartInfo.Arguments
            Await mproFfmpegProcess.WaitForExitAsync()

            If overwriteOriginal Or (useIntermediate) Then
                My.Computer.FileSystem.DeleteFile(intermediateFilePath)
            End If
            CheckOutput(outputPath, runArgs, True)
        Catch
            Throw
        Finally
            If useIntermediate Then
                If File.Exists(intermediateFilePath) Then
                    My.Computer.FileSystem.DeleteFile(intermediateFilePath)
                End If
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Checks if the output file exists, displaying a popup message if it doesn't
    ''' </summary>
    Private Sub CheckOutput(outputPath As String, runargs As String, focusFile As Boolean)
        runTextbox.Hide()
        If File.Exists(outputPath) Then
            If focusFile Then
                'Show file location of saved file
                OpenOrFocusFile(outputPath)
            End If
        Else
            MessageBox.Show(Me, $"Failed to generate output '{outputPath}' using ffmpeg{vbNewLine}{vbNewLine}Arguments:{runargs}{vbNewLine}{vbNewLine}Stdout: {mobjOutputLog}{vbNewLine}{vbNewLine}Stderr: {mobjErrorLog}",
                            "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    Private Sub AddRunHandlers()
        If Not runTextbox.Visible Then
            runTextbox.Show(Me)
        End If
        If mproFfmpegProcess IsNot Nothing Then
            AddHandler mproFfmpegProcess.ErrorDataReceived, AddressOf NewErrorData
            AddHandler mproFfmpegProcess.OutputDataReceived, AddressOf NewOutputData
        End If
    End Sub

    Private Sub NewErrorData(sender As Object, e As System.Diagnostics.DataReceivedEventArgs)
        If e.Data IsNot Nothing AndAlso Not String.IsNullOrEmpty(e.Data) Then
            mobjErrorLog.AppendLine(e.Data)
            If runTextbox IsNot Nothing Then
                runTextbox.SetText(mobjErrorLog.ToString)
            End If
        End If
    End Sub

    Private Sub NewOutputData(sender As Object, e As System.Diagnostics.DataReceivedEventArgs)
        If e.Data IsNot Nothing AndAlso Not String.IsNullOrEmpty(e.Data) Then
            mobjOutputLog.AppendLine(e.Data)
        End If
    End Sub

    ''' <summary>
    ''' Checks for the user to be holding ctrl for injection, then opens a "save as" dialog
    ''' </summary>
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        mblnUserInjection = My.Computer.Keyboard.CtrlKeyDown
        SaveAs()
    End Sub

    ''' <summary>
    ''' Opens a "save as" dialog for the user to define a target file location
    ''' </summary>
    Private Sub SaveAs()
        sfdVideoOut.Filter = "MP4|*.mp4|GIF|*.gif|MKV|*.mkv|WMV|*.wmv|AVI|*.avi|MOV|*.mov|All files (*.*)|*.*"
        Dim validExtensions() As String = sfdVideoOut.Filter.Split("|")
        Dim targetName As String = System.IO.Path.GetFileName(sfdVideoOut.FileName)
        For index As Integer = 1 To validExtensions.Count - 1 Step 2
            If System.IO.Path.GetExtension(targetName).Contains(validExtensions(index).Replace("*", "")) Then
                sfdVideoOut.FilterIndex = ((index - 1) \ 2) + 1
                Exit For
            End If
        Next
        subForm.SaveToTemp()
        'Retarget the location of the last attempted save, as when you save, the full path gets placed into the FileName member
        Dim initialDir As String = Path.GetDirectoryName(sfdVideoOut.FileName)
        If IO.Directory.Exists(initialDir) Then
            sfdVideoOut.InitialDirectory = initialDir
        End If
        sfdVideoOut.FileName = targetName
        sfdVideoOut.OverwritePrompt = True
        sfdVideoOut.ShowDialog()
    End Sub

    ''' <summary>
    ''' Checks if save button should be available for use, enabling or disabling when needed
    ''' </summary>
    Private Sub CheckSave()
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() CheckSave())
        Else
            If mobjMetaData IsNot Nothing Then
                If ctlVideoSeeker.RangeModified Then
                    If mobjMetaData.TotalOk AndAlso mobjMetaData.ThumbFrames(ctlVideoSeeker.RangeMaxValue).PTSTime IsNot Nothing Then
                        btnSave.Enabled = True
                    Else
                        btnSave.Enabled = False
                    End If
                Else
                    btnSave.Enabled = True
                End If
            End If
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
        Me.UseWaitCursor = False
    End Sub
#End Region

#Region "Preview Frames"
    ''' <summary>
    ''' Polls for keyframe image data from ffmpeg, gives a loading cursor
    ''' </summary>
    Private Sub PollPreviewFrames()
        Debug.Print("Starting PollPreviewFrames")
        'Make sure the user is notified that the application is working
        Me.UseWaitCursor = True
        mintCurrentFrame = 0
        Task.Run(Sub()
                     'Try to read from file, otherwise go ahead and extract them
                     If Not mobjMetaData.ReadScenesFromFile Then
                         mobjMetaData.ExtractSceneChanges(mobjMetaData.TotalFrames / ctlVideoSeeker.Width)
                         'mobjMetaData.SaveScenesToFile()
                     End If
                     Dim fullFrameGrab As Task(Of Bitmap) = Nothing
                     'Grab compressed frames
                     If Not mobjMetaData.ReadThumbsFromFile Then
                         If mobjMetaData.FileSize < 50000 AndAlso mobjMetaData.DurationSeconds <= 300 Then
                             'If the video is pretty small, just cache the whole thing
                             'Tests showed 7.5s load for a 3.5 minute 50MB video, vs 9s for full cache
                             fullFrameGrab = mobjMetaData.GetFfmpegFrameAsync(0, -1)
                         ElseIf mobjMetaData.DurationSeconds < 7 Then
                             'If the video is pretty short, just cache the whole thing
                             fullFrameGrab = mobjMetaData.GetFfmpegFrameAsync(0, -1)
                         Else
                             Dim thumbSize As Integer = 32
                             If mobjMetaData.FileSize <= 50000 AndAlso mobjMetaData.DurationSeconds <= 600 Then
                                 thumbSize = 64
                             End If
                             Task.Run(Async Function()
                                          Await mobjMetaData.ExtractThumbFrames(thumbSize)
                                      End Function)
                         End If
                         'mobjMetaData.SaveThumbsToFile()
                     End If

                     If fullFrameGrab Is Nothing Then
                         mtskPreview = mobjMetaData.GetFfmpegFrameRangesAsync(Me.CreatePreviewFrameDefaults())
                         Task.Run(Sub()
                                      mtskPreview.Wait()
                                      PreviewFinished()
                                  End Sub)
                     Else
                         Task.Run(Sub()
                                      fullFrameGrab.Wait()
                                      PreviewFinished()
                                  End Sub)
                     End If
                 End Sub)
    End Sub


    ''' <summary>
    ''' Re-enables important controls on the UI and sets up known actual width/height
    ''' </summary>
    Private Sub PreviewFinished()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() PreviewFinished())
        Else
            Dim detectedWidth As Integer = mobjMetaData.Width
            Dim detectedHeight As Integer = mobjMetaData.Height
            If picVideo.Image IsNot Nothing Then
                'If the resolution failed to load, put in something
                If detectedWidth = 0 Or detectedHeight = 0 Then
                    detectedWidth = picFrame1.Image.Width
                    detectedHeight = picFrame1.Image.Height
                    mobjMetaData.OverrideResolution(New Size(detectedWidth, detectedHeight))
                    picVideo.Invalidate()
                End If
                'If the aspect ratio was somehow saved wrong, fix it
                'Try flipping the known aspect, if its closer to what was loaded, change it
                If Math.Abs((detectedWidth / detectedHeight) - (picVideo.Image.Height / picVideo.Image.Width)) < Math.Abs((detectedHeight / detectedWidth) - (picVideo.Image.Height / picVideo.Image.Width)) Then
                    SwapValues(detectedWidth, detectedHeight)
                    mobjMetaData.OverrideResolution(New Size(detectedWidth, detectedHeight))
                    picVideo.Invalidate()
                End If
            End If

            'Re-enable everything, even if we failed to grab the last frame
            Me.UseWaitCursor = False
            CheckSave()
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
        previewFrames.Add(Math.Max(0, mobjMetaData.TotalFrames - 1))
        Return previewFrames
    End Function

    Private Sub PreviewsLoaded(sender As Object, objCache As ImageCache, ranges As List(Of List(Of Integer))) Handles mobjMetaData.RetrievedFrames
        'Don't use thumbs for previews
        If objCache Is mobjMetaData.ThumbFrames Then
            If ctlVideoSeeker.RangeMaxValue.InRange(ranges(0)(0), ranges(0)(1)) Then
                CheckSave()
            End If
            Exit Sub
        End If
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub()
                               PreviewsLoaded(sender, objCache, ranges)
                           End Sub)
        Else
            Dim previewFrames As List(Of Integer) = Me.CreatePreviewFrameDefaults()

            For previewIndex As Integer = 0 To previewFrames.Count - 1
                Dim gotImage As Bitmap = Nothing
                If objCache.ImageCacheStatus(previewIndex) = ImageCache.CacheStatus.Cached Then
                    gotImage = mobjMetaData.GetImageFromCache(previewFrames(previewIndex), objCache)
                    Dim targetPreview As PictureBoxPlus = Nothing
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
                    End Select
                    If targetPreview.Image Is Nothing OrElse targetPreview.Image.Width < gotImage.Width Then
                        targetPreview.SetImage(gotImage)
                    Else
                        gotImage.Dispose()
                    End If
                End If
            Next
        End If
    End Sub
#End Region

    ''' <summary>
    ''' Runs ffmpeg.exe with given command information. Cropping and rotation must be seperated.
    ''' </summary>
    Private Sub RunFfmpeg(ByVal inputFile As VideoData, ByVal outPutFile As String, ByVal specProperties As SpecialOutputProperties, ByVal trimData As TrimData, ByVal targetDefinition As String, croprect As Rectangle?)
        mobjErrorLog.Clear()
        mobjOutputLog.Clear()
        Dim softSubs As Boolean = mobjOutputProperties.Subtitles?.Length > 0 AndAlso Not mobjOutputProperties.BakeSubs

        If specProperties?.PlaybackSpeed <> 0 AndAlso trimData IsNot Nothing Then
            'duration /= specProperties.PlaybackSpeed
            trimData.StartPTS *= specProperties.PlaybackSpeed
            trimData.EndPTS *= specProperties.PlaybackSpeed
        End If

        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"

        'CREATE LIST OF PARAMETERS FOR EACH FILTER
        Dim videoFilterParams As New List(Of String)
        Dim audioFilterParams As New List(Of String)

        If specProperties?.PlaybackVolume <> 0 Then
            If trimData IsNot Nothing Then
                Dim duration As String = (trimData.EndPTS) - (trimData.StartPTS)
                If duration > 0 Then
                    'duration = Math.Truncate(duration * mobjMetaData.Framerate) / mobjMetaData.Framerate
                    Dim startHHMMSS As String = FormatHHMMSSm(trimData.StartPTS / specProperties.PlaybackSpeed)
                    processInfo.Arguments += " -ss " & startHHMMSS & " -t " & duration.ToString
                End If
            End If
        Else
            If trimData IsNot Nothing Then
                'Set up a select filter to trim to exact frames for maximum precision
                videoFilterParams.Add($"select=between(n\,{trimData.StartFrame}\,{trimData.EndFrame}),setpts=PTS-STARTPTS")
            End If
        End If
        processInfo.Arguments += $" -i ""{inputFile.FullPath}"""
        If softSubs Then
            processInfo.Arguments += $" -i ""{mobjOutputProperties.Subtitles}"""
        End If

        'CROP VIDEO(Can not be done with a rotate, must run twice)
        Dim cropWidth As Integer = inputFile.Width
        Dim cropHeight As Integer = inputFile.Height
        If croprect IsNot Nothing Then
            If croprect.Value.Width <> cropWidth AndAlso croprect.Value.Height <> cropHeight Then
                cropWidth = croprect.Value.Width
                cropHeight = croprect.Value.Height
                videoFilterParams.Add(GetCropArgs(croprect))
            End If
        End If

        'SCALE VIDEO
        Dim scale As Double = cropHeight
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
        scale /= cropHeight
        If scale <> 1 Then
            Dim scaleX As Integer = ForceEven(Math.Floor(cropWidth * scale))
            Dim scaleY As Integer = ForceEven(Math.Floor(cropHeight * scale))
            If specProperties.Rotation = RotateFlipType.Rotate90FlipNone OrElse specProperties.Rotation = RotateFlipType.Rotate270FlipNone Then
                videoFilterParams.Add($"scale={scaleY}:{scaleX}")
                'processInfo.Arguments += $" -s {scaleY}x{scaleX} -threads 4"
            Else
                videoFilterParams.Add($"scale={scaleX}:{scaleY}")
                'processInfo.Arguments += $" -s {scaleX}x{scaleY} -threads 4"
            End If
        End If

        'ROTATE VIDEO
        Dim rotateString As String = If(specProperties.Rotation = RotateFlipType.Rotate90FlipNone, "transpose=1", If(specProperties.Rotation = RotateFlipType.Rotate180FlipNone, """transpose=2,transpose=2""", If(specProperties.Rotation = RotateFlipType.Rotate270FlipNone, "transpose=2", "")))
        If rotateString.Length > 0 Then
            videoFilterParams.Add(rotateString)
        End If

        'HARD SUBTITLES
        If mobjOutputProperties.Subtitles?.Length > 0 Then
            If mobjOutputProperties.BakeSubs Then
                'To use a file path inside complex filter, you need to escape the colon, and reverse all slashes
                Dim reverseSlashed As String = mobjOutputProperties.Subtitles.Replace("\", "/")
                reverseSlashed = reverseSlashed.Replace(":", "\:")
                videoFilterParams.Add($"subtitles='{reverseSlashed}'")
            End If
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
        Dim isGif As Boolean = IO.Path.GetExtension(outPutFile).ToLower().Equals(".gif")
        If isGif Then
            videoFilterParams.Add("split [a][b];[a] palettegen [p];[b]fifo[c];[c][p] paletteuse=dither=none")
        End If

        'Check if the user wants to do motion interpolation when using a framerate that would cause duplicate frames
        Dim willHaveDuplicates As Boolean = (specProperties.FPS > inputFile.Framerate * specProperties.PlaybackSpeed) OrElse (specProperties.FPS = 0 AndAlso specProperties.PlaybackSpeed < 1)
        If willHaveDuplicates Then
            If MotionInterpolationToolStripMenuItem.Checked Then
                videoFilterParams.Add($"minterpolate=fps={If(specProperties.FPS = 0, inputFile.Framerate, specProperties.FPS)}:mi_mode=mci:mc_mode=aobmc:me_mode=bidir:vsbmc=1")
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

        If softSubs Then
            processInfo.Arguments += " -c:s mov_text -metadata:s:s:0 language=eng"
        End If

        'ANIMATED PNG FORMAT
        If outPutFile.EndsWith(".png") Then
            processInfo.Arguments += " -f apng"
        End If

        'OUTPUT TO FILE
        processInfo.Arguments += " """ & outPutFile & """"
        If mblnUserInjection Then
            'Show a form where the user can modify the arguments manually
            Dim manualEntryForm As New ManualEntryForm(processInfo.Arguments)
            Select Case manualEntryForm.ShowDialog()
                Case DialogResult.Cancel
                    mproFfmpegProcess = Nothing
                    Exit Sub
            End Select
            processInfo.Arguments = manualEntryForm.ModifiedText
        End If
        processInfo.RedirectStandardError = True
        processInfo.RedirectStandardOutput = True
        processInfo.UseShellExecute = False
        processInfo.WindowStyle = ProcessWindowStyle.Hidden
        processInfo.CreateNoWindow = True
        mproFfmpegProcess = New Process()
        AddRunHandlers()
        mproFfmpegProcess.StartInfo = processInfo
        mproFfmpegProcess.Start()
    End Sub

#Region "CROPPING"
    ''' <summary>
    ''' Updates the main image with one of the pre-selected images from the picture box clicked.
    ''' </summary>
    Private Sub picFrame_Click(sender As Object, e As EventArgs) Handles picFrame1.Click, picFrame2.Click, picFrame3.Click, picFrame4.Click, picFrame5.Click
        If mobjMetaData IsNot Nothing Then
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
        End If
    End Sub

    ''' <summary>
    ''' Draws cropping graphics over the main video picturebox.
    ''' </summary>
    Private Sub picVideo_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picVideo.Paint
        If Me.mobjMetaData IsNot Nothing Then
            e.Graphics.Clear(picVideo.BackColor)

            Dim lastTransform As Drawing2D.Matrix = e.Graphics.Transform
            e.Graphics.Transform = GetVideoToClientMatrix()
            Dim penSize As Single = 1
            If picVideo.Image IsNot Nothing Then
                Dim currentScale As Single = FitScale(picVideo.Image.Size, picVideo.Size)
                If currentScale > 1 Then
                    'Image is being increased in size, use nearest neightbor to do clean rendering
                    picVideo.InterpolationMode = InterpolationMode.NearestNeighbor
                Else
                    picVideo.InterpolationMode = InterpolationMode.Default
                End If
                e.Graphics.DrawImage(picVideo.Image, 0, 0, mobjMetaData.Width, mobjMetaData.Height)
                penSize = Math.Min(1 / currentScale, 1)
            End If

            Using pen As New Pen(Color.White, penSize)
                If Not Me.CropRect Is Nothing Then
                    e.Graphics.DrawLine(pen, New Point(mptStartCrop.X, 0), New Point(mptStartCrop.X, mobjMetaData.Height))
                    e.Graphics.DrawLine(pen, New Point(0, mptStartCrop.Y), New Point(mobjMetaData.Width, mptStartCrop.Y))
                    e.Graphics.DrawLine(pen, New Point(mptEndCrop.X, 0), New Point(mptEndCrop.X, mobjMetaData.Height))
                    e.Graphics.DrawLine(pen, New Point(0, mptEndCrop.Y), New Point(mobjMetaData.Width, mptEndCrop.Y))
                End If
            End Using
            e.Graphics.DrawRectangle(New Pen(Color.Green, penSize), mptStartCrop.X, mptStartCrop.Y, mptEndCrop.X - mptStartCrop.X, mptEndCrop.Y - mptStartCrop.Y)
            e.Graphics.Transform = lastTransform

            'Draw frame info
            Using pen As New Pen(Color.White, 1)
                If mintDisplayInfo <> 0 Then
                    e.Graphics.FillRectangle(Brushes.White, New RectangleF(New PointF(0, 0), e.Graphics.MeasureString(mintCurrentFrame, Me.Font)))
                    e.Graphics.DrawString(mintCurrentFrame, Me.Font, Brushes.Black, New PointF(0, 0))
                End If
            End Using
        End If
    End Sub

    ''' <summary>
    ''' Gets the transformation which converts video coorinates into client coordinates of picVideo
    ''' </summary>
    Private Function GetVideoToClientMatrix() As System.Drawing.Drawing2D.Matrix
        Dim resultMatrix As New System.Drawing.Drawing2D.Matrix
        'Scale to video coordinates
        Dim clientRect As Rectangle = picVideo.ClientRectangle
        Dim imageRect As Rectangle = mobjMetaData.Size.ToRect
        If mobjOutputProperties.RotationAngle = 90 OrElse mobjOutputProperties.RotationAngle = 270 Then
            imageRect = New Rectangle(0, 0, imageRect.Height, imageRect.Width)
        End If
        Dim fitScale As Double = imageRect.FitScale(clientRect)
        Dim fitImage As Rectangle = Me.mobjMetaData.Size.ToRect.Scale(fitScale)

        Dim clientCenter As Point = clientRect.Center
        Dim fitCenter As Point = fitImage.Center

        'Transformations are set up in reverse order to how they will be applied
        Select Case mobjOutputProperties.RotationAngle
            Case 90
                resultMatrix.Translate(fitCenter.Y + clientCenter.X, -fitCenter.X + clientCenter.Y)
            Case 180
                resultMatrix.Translate(fitCenter.X + clientCenter.X, fitCenter.Y + clientCenter.Y)
            Case 270
                resultMatrix.Translate(-fitCenter.Y + clientCenter.X, fitCenter.X + clientCenter.Y)
            Case Else
                resultMatrix.Translate(-fitCenter.X + clientCenter.X, -fitCenter.Y + clientCenter.Y)
        End Select
        resultMatrix.Scale(fitScale, fitScale)
        resultMatrix.Rotate(Me.mobjOutputProperties.RotationAngle)
        Return resultMatrix
    End Function

    ''' <summary>
    ''' Modifies the crop region, sets to current point
    ''' </summary>
    Private Sub picVideo_MouseDown(sender As Object, e As MouseEventArgs) Handles picVideo.MouseDown
        If Me.mobjMetaData IsNot Nothing Then
            Dim videoToClientMatrix As System.Drawing.Drawing2D.Matrix = Me.GetVideoToClientMatrix()
            Dim startCropClient As Point = mptStartCrop.Transform(videoToClientMatrix)
            Dim endCropClient As Point = mptEndCrop.Transform(videoToClientMatrix)
            Dim topRight As Point = New Point(mptEndCrop.X, mptStartCrop.Y).Transform(videoToClientMatrix)
            Dim bottomLeft As Point = New Point(mptStartCrop.X, mptEndCrop.Y).Transform(videoToClientMatrix)
            videoToClientMatrix.Invert()
            Dim actualImagePoint As Point = e.Location.Transform(videoToClientMatrix)
            'Start dragging start or end point
            If e.Button = Windows.Forms.MouseButtons.Left Then
                mblnCropping = True
                If Not startCropClient.DistanceTo(e.Location) < CROP_COLLISION_RADIUS AndAlso
                    Not endCropClient.DistanceTo(e.Location) < CROP_COLLISION_RADIUS AndAlso
                    Not topRight.DistanceTo(e.Location) < CROP_COLLISION_RADIUS AndAlso
                    Not bottomLeft.DistanceTo(e.Location) < CROP_COLLISION_RADIUS Then
                    mptStartCrop = actualImagePoint
                    mptEndCrop = actualImagePoint
                End If
                UpdateCropStatus()
            End If
            picVideo.Invalidate()
        End If
    End Sub

    ''' <summary>
    ''' Disables flag for cropping so mousemove crop isn't miss-triggered
    ''' </summary>
    Private Sub picVideo_MouseUp(sender As Object, e As MouseEventArgs) Handles picVideo.MouseUp
        mblnCropping = False
    End Sub

    ''' <summary>
    ''' Updates the status label for crop with the correct image coordinates
    ''' </summary>
    Private Sub UpdateCropStatus()
        Dim cropActual As Rectangle? = Me.CropRect()
        If Me.mobjMetaData Is Nothing OrElse cropActual Is Nothing Then
            'Don't show crop info if we aren't cropping
            lblStatusCropRect.Text = ""
            lblStatusCropRect.ToolTipText = "Width x Height crop rectangle in video coordinates"
        Else
            'lblStatusCropRect.Text = $"{cropActual.X},{cropActual.Y},{cropActual.Width},{cropActual.Height}"
            lblStatusCropRect.Text = $"{cropActual?.Width} x {cropActual?.Height}"
            lblStatusCropRect.ToolTipText = lblStatusCropRect.ToolTipText.Split(vbNewLine)(0).Trim() & vbNewLine & $"crop={cropActual?.Width}:{cropActual?.Height}:{cropActual?.X}:{cropActual?.Y} (w:h:x:y)"
        End If
    End Sub

    ''' <summary>
    ''' Modifies the crop region, draggable in all directions
    ''' </summary>
    Private Sub picVideo_MouseMove(sender As Object, e As MouseEventArgs) Handles picVideo.MouseMove
        'Display mouse position information
        If Me.mobjMetaData IsNot Nothing Then
            Dim videoToClientMatrix As System.Drawing.Drawing2D.Matrix = Me.GetVideoToClientMatrix()
            Dim clientToVideoMatrix As System.Drawing.Drawing2D.Matrix = Me.GetVideoToClientMatrix()
            clientToVideoMatrix.Invert()
            Dim startCropClient As Point = mptStartCrop.Transform(videoToClientMatrix)
            Dim endCropClient As Point = mptEndCrop.Transform(videoToClientMatrix)
            Dim actualImagePoint As Point = e.Location.Transform(clientToVideoMatrix)
            lblStatusMousePosition.Text = $"{actualImagePoint.X}, {actualImagePoint.Y}"
            If e.Button = Windows.Forms.MouseButtons.Left AndAlso mblnCropping Then
                'Update the closest crop point so we can drag either
                Dim topRight As Point = New Point(mptEndCrop.X, mptStartCrop.Y).Transform(videoToClientMatrix)
                Dim bottomLeft As Point = New Point(mptStartCrop.X, mptEndCrop.Y).Transform(videoToClientMatrix)
                Dim target As Integer = 2
                Dim closestDistance As Single = endCropClient.DistanceTo(e.Location)
                If startCropClient.DistanceTo(e.Location) < closestDistance Then
                    target = 0
                    closestDistance = startCropClient.DistanceTo(e.Location)
                End If
                If topRight.DistanceTo(e.Location) < closestDistance Then
                    target = 1
                    closestDistance = topRight.DistanceTo(e.Location)
                End If
                If bottomLeft.DistanceTo(e.Location) < closestDistance Then
                    target = 3
                    closestDistance = bottomLeft.DistanceTo(e.Location)
                End If
                Select Case target
                    Case 0
                        mptStartCrop = actualImagePoint
                    Case 1
                        mptEndCrop = New Point(actualImagePoint.X, mptEndCrop.Y)
                        mptStartCrop = New Point(mptStartCrop.X, actualImagePoint.Y)
                    Case 2
                        mptEndCrop = actualImagePoint
                    Case 3
                        mptEndCrop = New Point(mptEndCrop.X, actualImagePoint.Y)
                        mptStartCrop = New Point(actualImagePoint.X, mptStartCrop.Y)
                End Select
                Dim minX As Integer = Math.Max(0, Math.Min(mptStartCrop.X, mptEndCrop.X))
                Dim minY As Integer = Math.Max(0, Math.Min(mptStartCrop.Y, mptEndCrop.Y))
                Dim maxX As Integer = Math.Min(mobjMetaData.Width, Math.Max(mptStartCrop.X, mptEndCrop.X))
                Dim maxY As Integer = Math.Min(mobjMetaData.Height, Math.Max(mptStartCrop.Y, mptEndCrop.Y))
                mptStartCrop.X = minX
                mptStartCrop.Y = minY
                mptEndCrop.X = maxX
                mptEndCrop.Y = maxY
                UpdateCropStatus()
                picVideo.Invalidate()
            End If
        End If
    End Sub

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

    ''' <summary>
    ''' Sets the crop start and end to a position based on bounding non-background contents of the current region
    ''' </summary>
    Private Async Sub AutoCropContractToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles cmsAutoCrop.Click, ContractToolStripMenuItem.Click
        Try
            'Ensure context menu goes away when clicking on items that normally may not close it
            cmsPicVideo.Close()
            'Loop through all images, find any pixel different than the respective corners
            Me.UseWaitCursor = True
            pgbOperationProgress.Minimum = ctlVideoSeeker.RangeMinValue
            pgbOperationProgress.Maximum = ctlVideoSeeker.RangeMaxValue + 1
            pgbOperationProgress.Value = pgbOperationProgress.Minimum
            pgbOperationProgress.Visible = True

            'Only grab frames we have confirmed the existence of so the function can return immediately
            Dim allCached As Boolean = True
            For index As Integer = ctlVideoSeeker.RangeMinValue To ctlVideoSeeker.RangeMaxValue
                If Me.mobjMetaData.ImageCacheStatus(index) <> ImageCache.CacheStatus.Cached Then
                    allCached = False
                    Exit For
                End If
            Next
            If Not allCached Then
                Await mobjMetaData.GetFfmpegFramesAsync(Me.ctlVideoSeeker.RangeMinValue, Me.ctlVideoSeeker.RangeMaxValue)
            End If
            Dim displaySize As Size = Me.mobjMetaData.GetImageDataFromCache(0).Size
            Dim topLeftCropStart As Point = mptStartCrop
            Dim bottomRightCropStart As Point = mptEndCrop
            Dim fitScale As Double = Me.mobjMetaData.Size.FitScale(displaySize)
            Dim cropRect As Rectangle? = Me.CropRect()
            If Not cropRect.HasValue Then
                cropRect = New Rectangle(0, 0, displaySize.Width, displaySize.Height)
            Else
                cropRect = cropRect.Value.Scale(fitScale)
            End If
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
                                   Dim currentFrame As Integer = index
                                   Me.Invoke(Sub()
                                                 pgbOperationProgress.Value = currentFrame + 1
                                             End Sub)
                               Next
                           End Sub)
            ctlVideoSeeker.PreviewLocation = largestFrame
            'Scale to actual size
            If (left = 0 AndAlso top = 0 AndAlso right = displaySize.Width - 1 AndAlso bottom = displaySize.Height - 1) Then
                SetCropPoints(New Point(0, 0), New Point(0, 0))
            Else
                top = Math.Max(Me.CropRect.Value.Top, top / fitScale)
                bottom = Math.Min(Me.CropRect.Value.Bottom, bottom / fitScale)
                right = Math.Min(Me.CropRect.Value.Right, right / fitScale)
                left = Math.Max(Me.CropRect.Value.Left, left / fitScale)
                SetCropPoints(New Point(left, top), New Point(right, bottom))
            End If
            picVideo.Invalidate()
        Catch
            Throw
        Finally
            pgbOperationProgress.Visible = False
            Me.UseWaitCursor = False
        End Try
    End Sub

    ''' <summary>
    ''' Add autocrop expand option, where the region will expand until a hard line perpendicular to the expand direction is found, such as black bars
    ''' Expands to the first largest border found
    ''' </summary>
    Private Async Sub AutoCropExpandToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExpandToolStripMenuItem.Click
        Try
            'Loop through all images, find any pixel different than the respective corners
            Me.UseWaitCursor = True
            pgbOperationProgress.Minimum = ctlVideoSeeker.RangeMinValue
            pgbOperationProgress.Maximum = ctlVideoSeeker.RangeMaxValue + 1
            pgbOperationProgress.Value = pgbOperationProgress.Minimum
            pgbOperationProgress.Visible = True

            'Only grab frames we have confirmed the existence of so the function can return immediately
            Dim allCached As Boolean = True
            For index As Integer = ctlVideoSeeker.RangeMinValue To ctlVideoSeeker.RangeMaxValue
                If Me.mobjMetaData.ImageCacheStatus(index) <> ImageCache.CacheStatus.Cached Then
                    allCached = False
                    Exit For
                End If
            Next
            If Not allCached Then
                Await mobjMetaData.GetFfmpegFramesAsync(Me.ctlVideoSeeker.RangeMinValue, Me.ctlVideoSeeker.RangeMaxValue)
            End If
            Dim displaySize As Size = Me.mobjMetaData.GetImageDataFromCache(0).Size
            Dim topLeftCropStart As Point = mptStartCrop
            Dim bottomRightCropStart As Point = mptEndCrop
            Dim fitScale As Double = Me.mobjMetaData.Size.FitScale(displaySize)
            If Me.CropRect Is Nothing Then
                Exit Sub
            End If
            Dim cropRect As Rectangle? = Me.CropRect().Value.Scale(fitScale)
            Dim left As Integer = displaySize.Width - 1
            Dim top As Integer = displaySize.Height - 1
            Dim bottom As Integer = 0
            Dim right As Integer = 0
            Dim largestFrame As Integer = mintCurrentFrame
            Await Task.Run(Sub()
                               Dim stillExpanding As Boolean = False
                               Do
                                   stillExpanding = False
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
                                                   stillExpanding = True
                                                   cropRect = potentialRect
                                               End If
                                           End Using
                                       End If
                                       Dim currentFrame As Integer = index
                                       Me.Invoke(Sub()
                                                     pgbOperationProgress.Value = currentFrame + 1
                                                 End Sub)
                                   Next
                               Loop While stillExpanding
                           End Sub)
            ctlVideoSeeker.PreviewLocation = largestFrame
            'Scale to actual size
            If (left = 0 AndAlso top = 0 AndAlso right = displaySize.Width - 1 AndAlso bottom = displaySize.Height - 1) Then
                SetCropPoints(New Point(0, 0), New Point(0, 0))
            Else
                top = Math.Min(Me.CropRect.Value.Top, top / fitScale)
                bottom = Math.Max(Me.CropRect.Value.Bottom, bottom / fitScale)
                right = Math.Max(Me.CropRect.Value.Right, right / fitScale)
                left = Math.Min(Me.CropRect.Value.Left, left / fitScale)
                SetCropPoints(New Point(left, top), New Point(right, bottom))
            End If
            picVideo.Invalidate()
        Catch
            Throw
        Finally
            pgbOperationProgress.Visible = False
            Me.UseWaitCursor = False
        End Try
    End Sub

    Private Function GetCropArgs(cropRect As Rectangle) As String
        Return $"crop={cropRect.Width}:{cropRect.Height}:{cropRect.X}:{cropRect.Y}"
    End Function

    Private Function ReadCropData(cropData As String) As Rectangle?
        Dim matchInfo As Match = Regex.Match(cropData, "crop=(?<width>\d*):(?<height>\d*):(?<x>\d*):(?<y>\d*)")
        If matchInfo.Success Then
            Return New Rectangle(matchInfo.Groups("x").Value, matchInfo.Groups("y").Value, matchInfo.Groups("width").Value, matchInfo.Groups("height").Value)
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Copy data that can be used quickly in batch files or transfered to another SVE
    ''' </summary>
    Private Sub CopyToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyToolStripMenuItem.Click
        Dim cropRect As Rectangle? = Me.CropRect()
        Clipboard.SetText(GetCropArgs(cropRect))
    End Sub

    ''' <summary>
    ''' Load data from clipboard in the format we copy out to from other SVE instances
    ''' </summary>
    Private Sub LoadFromClipboardToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadFromClipboardToolStripMenuItem.Click
        Dim clipboardData As String = Clipboard.GetText()
        Dim cropRect As Rectangle = ReadCropData(clipboardData)
        'Update crop locations if needed
        Dim displaySize As Size = Me.mobjMetaData.GetImageDataFromCache(0).Size
        Dim scale As Single = New Point(displaySize.Width, displaySize.Height).Magnitude / New Point(mobjMetaData.Width, mobjMetaData.Height).Magnitude
        mptStartCrop = cropRect.TopLeft
        mptEndCrop = mptStartCrop.Add(New Point(cropRect.Width - 1, cropRect.Height - 1))
        UpdateCropStatus()
        picVideo.Invalidate()
    End Sub

    Private Sub lblStatusCropRect_MouseUp(sender As Object, e As MouseEventArgs) Handles lblStatusCropRect.MouseUp
        If e.Button = MouseButtons.Right Then
            Dim stripLabelOffset As Integer = 0
            For index As Integer = 0 To StatusStrip1.Items.Count - 1
                If StatusStrip1.Items(index) Is sender Then
                    Exit For
                End If
                stripLabelOffset += StatusStrip1.Items(index).Width
            Next
            cmsCrop.Show(StatusStrip1, e.Location.Add(New Point(stripLabelOffset, 0)))
        End If
    End Sub

    ''' <summary>
    ''' Disable crop items that are not available as no crop is present
    ''' </summary>
    Private Sub cmsCrop_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles cmsCrop.Opening
        Dim clipboardData As String = Clipboard.GetText()
        LoadFromClipboardToolStripMenuItem.Enabled = clipboardData.Length > 0 AndAlso ReadCropData(clipboardData) IsNot Nothing
        CopyToolStripMenuItem.Enabled = Me.CropRect IsNot Nothing
    End Sub
#End Region

#Region "Form Open/Close"
    ''' <summary>
    ''' Prepares temporary directory and sets up tool tips for controls.
    ''' </summary>
    Private Sub SimpleVideoEditor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Destroy old version from any updates
        CleanupTempFolder()
        Task.Run(Sub()
                     DeleteUpdateFiles()
                 End Sub)

        cmbDefinition.SelectedIndex = 0

        'Setup Tooltips
        mobjGenericToolTip.SetToolTip(ctlVideoSeeker, $"Move sliders to trim video.{vbNewLine}Use [A][D][←][→] to move trim sliders frame by frame.{vbNewLine}Hold [Shift] to move preview slider instead.")
        mobjGenericToolTip.SetToolTip(picVideo, $"Left click and drag to crop.{vbNewLine}Right click to clear crop selection.")
        mobjGenericToolTip.SetToolTip(cmbDefinition, $"Select the ending height of your video.{vbNewLine}Right click for FPS options.")
        mobjGenericToolTip.SetToolTip(btnSave, $"Save video.{vbNewLine}Hold ctrl to manually modify ffmpeg arguments.")
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

        RefreshStatusToolTips()

        'Context menu tooltips
        MotionInterpolationToolStripMenuItem.ToolTipText = $"Creates new frames to smooth motion when increasing FPS, or decreasing playback speed.{vbNewLine}WARNING: Slow processing and large file size may occur."
        CacheAllFramesToolStripMenuItem.ToolTipText = $"Caches every frame of the video into memory (high RAM requirement).{vbNewLine}Afterwards, frame scrubbing will be borderline instant."
        ContractToolStripMenuItem.ToolTipText = $"Attempts to shrink the current selection rectangle as long as the pixels it overlays are of consistent color."
        ExpandToolStripMenuItem.ToolTipText = $"Attempts to expand the current selection rectangle until the pixels it overlays are of consistent color."
        InjectCustomArgumentsToolStripMenuItem.ToolTipText = $"An additional editable form will appear after selecting a save location, containing the command line arguments that will be sent to ffmpeg."
        BakedInHardToolStripMenuItem.ToolTipText = "Subtitles are baked into the video stream. This ensures any player will render the text."
        ToggleableSoftToolStripMenuItem.ToolTipText = "Subtitles added as an element that can be turned on or off during playback. Relies on player support to see."

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
        mthdRenderDecay = New Thread(Sub()
                                         While (True)
                                             Dim oldValue As Integer = mintDisplayInfo
                                             mintDisplayInfo = Math.Max(mintDisplayInfo - 10, 0)
                                             Threading.Thread.Sleep(10)
                                             If oldValue <> 0 AndAlso mintDisplayInfo = 0 Then
                                                 picVideo.Invalidate()
                                             End If
                                         End While
                                     End Sub)
        mthdRenderDecay.IsBackground = True
        mthdRenderDecay.Start()
    End Sub

    ''' <summary>
    ''' Ensures status tooltips that change dynamically with data are up to date
    ''' </summary>
    Private Sub RefreshStatusToolTips()
        'Status  tooltips
        lblStatusMousePosition.ToolTipText = "X,Y position of the mouse in video coordinates"
        UpdateCropStatus()
        Dim startText As String = $"Original resolution Width x Height of the loaded content.{vbNewLine}Double click to fit window to original resolution."
        If mobjMetaData IsNot Nothing Then
            lblStatusResolution.ToolTipText = startText & vbNewLine & vbNewLine & mobjMetaData.VideoStream.Raw
        Else
            lblStatusResolution.ToolTipText = startText & $"{vbNewLine}{vbNewLine}Shows more detailed stream information on hover."
        End If
    End Sub

    Private Sub DeleteUpdateFiles()
        Dim badExePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly.Location) + "\DeletableSimpleVideoEditor.exe"
        If File.Exists(badExePath) Then
            'Keep trying to delete the file for a few seconds, in case the application is still running for some reason
            For index As Integer = 0 To 10
                Try
                    File.Delete(badExePath)
                Catch ex As Exception
                    'Exception, hopefully it's just because the application is still open, not because the user has no permissions <.<
                    If index = 10 Then
                        OpenOrFocusFile(badExePath)
                        MessageBox.Show(New Form() With {.TopMost = True}, $"Failed to remove update files. Please remove them manually.{vbNewLine}{ex.Message}", "Update Cleanup Failure", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
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
        CheckSave()
        ExportAudioToolStripMenuItem.Enabled = False
        Me.Text = Me.Text.Split("-")(0).Trim + $" - {ProductVersion} - Open Source"
        DefaultToolStripMenuItem.Text = "Default"
        lblStatusResolution.Text = ""
        subForm.SetSRT(Nothing)
        RefreshStatusToolTips()
    End Sub
#End Region

#Region "Misc Functions"
    ''' <summary>
    ''' Sets the crop points to the given values, setting them to nothing in the case where the entire image is selected
    ''' </summary>
    Public Sub SetCropPoints(ByRef cropTopLeft As Point, ByRef cropBottomRight As Point)
        If cropTopLeft.X = 0 AndAlso cropTopLeft.Y = 0 AndAlso cropBottomRight.X = mobjMetaData.Width - 1 AndAlso cropBottomRight.Y = mobjMetaData.Height - 1 Then
            'Perfectly bounds the image
            mptStartCrop = Nothing
            mptEndCrop = Nothing
        End If
        If (cropBottomRight.X - cropTopLeft.X) > 0 And (cropBottomRight.Y - cropTopLeft.Y) > 0 Then
            mptStartCrop = cropTopLeft
            mptEndCrop = cropBottomRight
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
        Dim imageFiles As New List(Of String)
        Dim largestPad As Integer = 0
        If args.Count = 2 Then
            If IO.File.Exists(args(1)) Then
                Return Nothing
            ElseIf IO.Directory.Exists(args(1)) Then
                'Directory
                Dim objFiles As String() = Directory.GetFiles(args(1))
                For index As Integer = 0 To objFiles.Count - 1
                    If objFiles(index).IsVBImage() Then
                        imageFiles.Add(objFiles(index))
                    End If
                Next
            End If
        ElseIf args.Count > 2 Then
            'Multiple files
            Dim sameExt As Boolean = True
            Dim defaultExt As String = Path.GetExtension(args(1))
            For index As Integer = 1 To args.Count - 1
                If args(index).IsVBImage() Then
                    imageFiles.Add(args(index))
                End If
                If Not Path.GetExtension(args(index)).ToLower.Equals(defaultExt.ToLower) Then
                    sameExt = False
                End If
            Next
            If imageFiles.Count = 0 AndAlso sameExt Then
                'Might be some other kind of files, ask to concatenate
                Select Case MessageBox.Show(Me, $"Detected multiple {defaultExt} inputs. Concatenate {args.Count - 1} files temporarily?", "Concatenate Files?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    Case DialogResult.Yes
                        Dim argInputs As String = ""
                        For index As Integer = 1 To args.Count - 1
                            argInputs += $"-i ""{args(index)}"" "
                        Next
                        Dim outPath As String = Path.Combine(Globals.TempPath, $"combined {Now.ToString("yyyy-MM-dd hh-mm-ss")}{Path.GetExtension(args(1))}")
                        If File.Exists(outPath) Then
                            File.Delete(outPath)
                        End If

                        If Not Directory.Exists(Path.GetDirectoryName(outPath)) Then
                            Directory.CreateDirectory(Path.GetDirectoryName(outPath))
                        End If

                        Dim startInfo As New ProcessStartInfo("ffmpeg.exe", argInputs + $" -filter_complex ""concat=n={args.Count - 1}"" ""{outPath}""")
                        Dim manualEntryForm As New ManualEntryForm(startInfo.Arguments)
                        Select Case manualEntryForm.ShowDialog()
                            Case DialogResult.Cancel
                                Return Nothing
                        End Select
                        startInfo.Arguments = manualEntryForm.ModifiedText

                        Dim concatProcess As Process = Process.Start(startInfo)
                        concatProcess.WaitForExit()
                        Return outPath
                    Case DialogResult.No
                        Return Nothing
                End Select
            End If
        End If
        If imageFiles.Count < 1 Then
            Return Nothing
        End If

        'Sort the files in case the user dragged them in a way that caused something that was not the first file to appear first in the args list
        imageFiles.Sort(Function(string1, string2) string1.CompareNatural(string2))

        'Check if the names are the proper pattern and unbroken so we know they will actually load
        Dim numberRegex As New Text.RegularExpressions.Regex("(?<zeros>0+)*(?<number>\d+)")
        Dim lastNumber As Integer = -1
        Dim padLength As Integer = -1
        Dim brokenPattern As Boolean = False
        For Each objFilename In imageFiles
            Dim currentMatch As Match = numberRegex.Match(IO.Path.GetFileName(objFilename))
            If currentMatch.Success Then
                Dim imageNumber As Integer = Integer.Parse(currentMatch.Groups("number").Value)
                Dim padNumber As Integer = Integer.Parse(currentMatch.Groups("zeros").Value.Length)
                If padLength = -1 Then
                    padLength = padNumber
                ElseIf padNumber <> padLength Then
                    brokenPattern = True
                    Exit For
                End If
                If lastNumber = -1 OrElse (Not imageNumber - lastNumber > 1) Then
                    lastNumber = imageNumber
                Else
                    brokenPattern = True
                    Exit For
                End If
            End If
        Next

        'A user may select a working set of filenames, but it could be a subset of all files in the directory, which will end with ffmpeg loading everything instead of just what they selected
        If brokenPattern Then
            Select Case MessageBox.Show(Me, $"Detected multiple {Path.GetExtension(args(1))} inputs, but could not detect a consistent filename pattern like 'image_001{Path.GetExtension(args(1))}'.{vbCrLf}Copy and rename {args.Count - 1} files temporarily?", "Copy and Rename Files?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                Case DialogResult.Yes
                    'Copy files over into temp directory in case user tried to select files that didn't strictly follow the pattern
                    Dim tempDir As String = Path.Combine(Globals.TempPath, $"mash {Now.ToString("yyyy-MM-dd hh-mm-ss")}")
                    If Not Directory.Exists(tempDir) Then
                        Directory.CreateDirectory(tempDir)
                    End If

                    Dim digits As Integer = imageFiles.Count.ToString.Length
                    For fileIndex As Integer = 0 To imageFiles.Count - 1
                        Dim newName As String = Path.Combine(tempDir, "image_" + fileIndex.ToString.PadLeft(digits, "0") + Path.GetExtension(imageFiles(fileIndex)))
                        System.IO.File.Copy(imageFiles(fileIndex), newName)
                        imageFiles(fileIndex) = newName
                    Next
                Case Else
                    'This will end up with ffmpeg just doing whatever it can with the bad pattern, usually loading just the first chunk of ok images
            End Select
        End If

        Return imageFiles(0).ExtractPattern
    End Function
#End Region

#Region "Settings UI"
    ''' <summary>
    ''' Captures key events before everything else, and uses them to modify the video trimming picRangeSlider control.
    ''' </summary>
    Protected Overrides Function ProcessCmdKey(ByRef message As Message, ByVal keys As Keys) As Boolean
        'Check number key states for frameskip
        Dim skipValue As Integer = 1
        For index As Integer = 0 To 9
            If My.Computer.Keyboard.KeyPressed(Keys.D0 + index) Then
                If index = 0 Then
                    skipValue = 10
                Else
                    skipValue = index
                End If
                Exit For
            End If
        Next

        'Check for slider motion
        Select Case keys
            Case Keys.A
                ctlVideoSeeker.RangeMinValue = ctlVideoSeeker.RangeMinValue - skipValue
                ctlVideoSeeker.Invalidate()
            Case Keys.D
                ctlVideoSeeker.RangeMinValue = ctlVideoSeeker.RangeMinValue + skipValue
                ctlVideoSeeker.Invalidate()
            Case Keys.Left
                ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue - skipValue
                ctlVideoSeeker.Invalidate()
            Case Keys.Right
                ctlVideoSeeker.RangeMaxValue = ctlVideoSeeker.RangeMaxValue + skipValue
                ctlVideoSeeker.Invalidate()
            Case Keys.A Or Keys.Shift, Keys.Left Or Keys.Shift
                ctlVideoSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation - skipValue
                ctlVideoSeeker.Invalidate()
            Case Keys.D Or Keys.Shift, Keys.Right Or Keys.Shift
                ctlVideoSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation + skipValue
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
            mobjOutputProperties.PlaybackVolume = 0
            MuteToolStripMenuItem.Checked = True
        Else
            mobjOutputProperties.PlaybackVolume = 1
            UnmuteToolStripMenuItem.Checked = True
        End If
        Dim volumeString As String = If(mobjOutputProperties.PlaybackVolume = 0, "muted.", If(mobjOutputProperties.PlaybackVolume = 1, "unmuted.", $"{mobjOutputProperties.PlaybackVolume}%"))
        mobjGenericToolTip.SetToolTip(chkMute, If(chkMute.Checked, "Unmute", "Mute") & $" the videos audio track.{vbNewLine}Currently " & If(chkMute.Checked, "muted.", volumeString))
    End Sub


    ''' <summary>
    ''' Changes the display text when changing quality check control
    ''' </summary>
    Private Sub chkSubtitles_CheckedChanged(sender As Object, e As EventArgs) Handles chkSubtitles.CheckChanged
        If Not chkSubtitles.Checked Then
            mobjOutputProperties.Subtitles = ""
        Else
            mobjOutputProperties.Subtitles = subForm.FilePath
        End If
        UpdateSubtitleTooltip()
        If chkSubtitles.Checked AndAlso subForm.Visible = False Then
            subForm.Show(Me)
            subForm.Location = New Point(Me.Location.X - subForm.Width, Me.Location.Y)
        End If
        If Not chkSubtitles.Checked AndAlso subForm.Visible = True Then
            subForm.Hide()
        End If
    End Sub

    ''' <summary>
    ''' Checks state of the UI and output properties to set up tooltip for subtitle settings button
    ''' </summary>
    Private Sub UpdateSubtitleTooltip()
        Dim tipBuilder As New StringBuilder
        tipBuilder.AppendLine(If(chkSubtitles.Checked, "Remove subtitles.", $"Add subtitles."))
        tipBuilder.AppendLine(If(chkSubtitles.Checked, $"Currently adding subtitles from '{IO.Path.GetFileName(subForm.FilePath)}'.", "Currently no subtitles."))
        If mobjOutputProperties.Subtitles?.Length > 0 Then
            tipBuilder.Append(If(mobjOutputProperties.BakeSubs, $"Subtitles will be baked directly into the video, right click to change.", $"Subtitles will be added as the english track, right click to change."))
        End If
        mobjGenericToolTip.SetToolTip(chkSubtitles, tipBuilder.ToString)
    End Sub

    ''' <summary>
    ''' Toggles whether the video will be decimated or not, and changes the image to make it obvious
    ''' </summary>
    Private Sub chkDeleteDuplicates_CheckedChanged(sender As Object, e As EventArgs) Handles chkDeleteDuplicates.CheckChanged
        mobjGenericToolTip.SetToolTip(chkDeleteDuplicates, If(chkDeleteDuplicates.Checked, "Allow Duplicate Frames", $"Delete Duplicate Frames.{vbNewLine}WARNING: Audio may go out of sync.") & $"{vbNewLine}Currently " & If(chkDeleteDuplicates.Checked, "deleting them.", "allowing them."))
        mobjOutputProperties.Decimate = chkDeleteDuplicates.Checked
    End Sub

    ''' <summary>
    ''' Rotates the final video by 90 degrees per click, and updates the graphic
    ''' </summary>
    Private Sub imgRotate_MouseUp(sender As Object, e As MouseEventArgs) Handles imgRotate.MouseUp
        Select Case mobjOutputProperties.Rotation
            Case RotateFlipType.RotateNoneFlipNone
                mobjOutputProperties.Rotation = RotateFlipType.Rotate90FlipNone
            Case RotateFlipType.Rotate90FlipNone
                mobjOutputProperties.Rotation = RotateFlipType.Rotate180FlipNone
            Case RotateFlipType.Rotate180FlipNone
                mobjOutputProperties.Rotation = RotateFlipType.Rotate270FlipNone
            Case RotateFlipType.Rotate270FlipNone
                mobjOutputProperties.Rotation = RotateFlipType.RotateNoneFlipNone
            Case Else
                mobjOutputProperties.Rotation = RotateFlipType.RotateNoneFlipNone
        End Select
        UpdateRotationButton()
    End Sub

    ''' <summary>
    ''' Updates the rotation setting icon to reflect the current rotation selection
    ''' </summary>
    Private Sub UpdateRotationButton()
        Select Case mobjOutputProperties.Rotation
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
        rotatedIcon.RotateFlip(mobjOutputProperties.Rotation)
        imgRotate.Image = rotatedIcon
        picVideo.Invalidate()
    End Sub

    Private Sub cmsRotation_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsRotation.ItemClicked
        For Each objItem As ToolStripMenuItem In cmsRotation.Items
            objItem.Checked = False
        Next
        Select Case cmsRotation.Items.IndexOf(e.ClickedItem)
            Case 0
                mobjOutputProperties.Rotation = RotateFlipType.RotateNoneFlipNone
            Case 1
                mobjOutputProperties.Rotation = RotateFlipType.Rotate90FlipNone
            Case 2
                mobjOutputProperties.Rotation = RotateFlipType.Rotate180FlipNone
            Case 3
                mobjOutputProperties.Rotation = RotateFlipType.Rotate270FlipNone
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
        mobjOutputProperties.FPS = Me.TargetFPS
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
        mobjOutputProperties.ColorKey = dlgColorKey.Color
    End Sub

    Private Sub picPlaybackSpeed_Click(sender As Object, e As EventArgs) Handles picPlaybackSpeed.Click
        cmsPlaybackSpeed.Show(picPlaybackSpeed.PointToScreen(CType(e, MouseEventArgs).Location))
    End Sub

    Private Sub cmsPlaybackSpeed_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles cmsPlaybackSpeed.ItemClicked
        If e.ClickedItem.GetType <> GetType(ToolStripMenuItem) Then
            Exit Sub
        End If
        For Each objItem As ToolStripItem In cmsPlaybackSpeed.Items
            If objItem.GetType = GetType(ToolStripSeparator) Then
                Exit For
            End If
            CType(objItem, ToolStripMenuItem).Checked = False
        Next
        'Sets the target playback speed global based on the text in the context menu
        Dim resultValue As Double = 1
        If Double.TryParse(Regex.Match(CType(e.ClickedItem, ToolStripMenuItem).Text, "\d*.?\d*").Value, resultValue) Then
            CType(e.ClickedItem, ToolStripMenuItem).Checked = True
            mobjOutputProperties.PlaybackSpeed = resultValue
        ElseIf e.ClickedItem Is CustomToolStripMenuItem Then
            CType(e.ClickedItem, ToolStripMenuItem).Checked = True
            If CustomSpeedTextToolStripMenuItem.Text.Length = 0 Then
                mobjOutputProperties.PlaybackSpeed = 1
            Else
                mobjOutputProperties.PlaybackSpeed = Double.Parse(CustomSpeedTextToolStripMenuItem.Text)
            End If
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
        mobjOutputProperties.PlaybackVolume = resultValue
    End Sub
#End Region

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
                    Dim chosenName As String = sfdExportFrame.FileName
                    Task.Run(Sub()
                                 Me.Invoke(Sub()
                                               Me.UseWaitCursor = True
                                               pgbOperationProgress.Minimum = mintCurrentFrame
                                               pgbOperationProgress.Maximum = mintCurrentFrame + 1
                                               pgbOperationProgress.Value = pgbOperationProgress.Minimum
                                               pgbOperationProgress.Visible = True
                                           End Sub)
                                 If File.Exists(chosenName) Then
                                     My.Computer.FileSystem.DeleteFile(chosenName)
                                 End If
                                 mobjMetaData.ExportFfmpegFrames(mintCurrentFrame, mintCurrentFrame, chosenName, Me.CropRect, mobjOutputProperties.Rotation)
                                 Me.Invoke(Sub()
                                               Me.UseWaitCursor = False
                                           End Sub)
                                 If File.Exists(chosenName) Then
                                     'Show file location of saved file
                                     OpenOrFocusFile(chosenName)
                                 End If
                             End Sub)
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
                    Task.Run(Sub()
                                 Me.Invoke(Sub()
                                               Me.UseWaitCursor = True
                                               pgbOperationProgress.Minimum = 0
                                               pgbOperationProgress.Maximum = endFrame - startFrame
                                               pgbOperationProgress.Value = pgbOperationProgress.Minimum
                                               pgbOperationProgress.Visible = True
                                           End Sub)

                                 If chosenName.Contains("#") Then
                                     chosenName = chosenName.Replace("#", "%03d")
                                 Else
                                     chosenName = Path.Combine({Path.GetDirectoryName(chosenName), Path.GetFileNameWithoutExtension(chosenName), "%03d", ".png"})
                                 End If
                                 Dim firstFrame As String = Regex.Replace(chosenName, "%03d", "001")
                                 mobjMetaData.ExportFfmpegFrames(startFrame, endFrame, chosenName, Me.CropRect, mobjOutputProperties.Rotation)
                                 Me.Invoke(Sub()
                                               Me.UseWaitCursor = False
                                           End Sub)
                                 If File.Exists(firstFrame) Then
                                     'Show file location of saved file
                                     OpenOrFocusFile(firstFrame)
                                 End If
                             End Sub)
                Case Else
                    'Do nothing
            End Select
        End Using
    End Sub

    Private Sub ExportProgress(sender As Object, newFrame As Integer) Handles mobjMetaData.ExportProgressed
        If Me.InvokeRequired Then
            Me.Invoke(Sub()
                          ExportProgress(sender, newFrame)
                      End Sub)
        Else
            If pgbOperationProgress.Minimum <= newFrame AndAlso pgbOperationProgress.Maximum >= newFrame Then
                pgbOperationProgress.Value = newFrame + 1
            End If
            If pgbOperationProgress.Maximum = newFrame Then
                pgbOperationProgress.Visible = False
            End If
        End If
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

    Private Sub subForm_PreviewChanged(newval As Integer) Handles subForm.PreviewChanged
        subForm.ctlSubtitleSeeker.EventsEnabled = False
        ctlVideoSeeker.PreviewLocation = newval
        subForm.ctlSubtitleSeeker.EventsEnabled = True
    End Sub

    Private Sub ctlVideoSeeker_SeekChanged(newVal As Integer) Handles ctlVideoSeeker.SeekChanged
        If mstrVideoPath IsNot Nothing AndAlso mstrVideoPath.Length > 0 AndAlso mobjMetaData IsNot Nothing Then
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
                If mthdFrameGrabber Is Nothing OrElse Not mthdFrameGrabber.IsAlive Then
                    mthdFrameGrabber = New Thread(Sub()
                                                      FrameQueueProcessor()
                                                  End Sub)
                    mthdFrameGrabber.IsBackground = True
                    mthdFrameGrabber.Start()
                End If
                mobjFramesToGrab.Add(mintCurrentFrame)
            End If
            mintDisplayInfo = RENDER_DECAY_TIME
            subForm.ctlSubtitleSeeker.PreviewLocation = ctlVideoSeeker.PreviewLocation
        End If
        CheckSave()
    End Sub

    ''' <summary>
    ''' Eats anything in frame queue such that only the latest is grabbed
    ''' </summary>
    Private Sub FrameQueueProcessor()
        Thread.CurrentThread.Name = "FrameQueueProcessor"
        While True
            Dim latestFrameRequest As Integer = mobjFramesToGrab.Take()
            Dim discardCount As Integer = 0
            Dim latestValue As Integer = 0
            While mobjFramesToGrab.TryTake(latestValue)
                discardCount += 1
                latestFrameRequest = latestValue
                'Loops until no elements in the queue, meaning we have the latest relevant request
                'All others are discarded to avoid wasting CPU resources on frames the user doesn't care for anymore
            End While
            If discardCount > 0 Then
                Debug.Print($"Discarded {discardCount} frame requests")
            End If
            mobjMetaData.GetFfmpegFrame(latestFrameRequest)
        End While
    End Sub

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
            If ctlVideoSeeker.RangeMaxValue.InRange(objRange(0), objRange(1)) Then
                CheckSave()
            End If
        Next
    End Sub

    Private Sub NewSceneCached(sender As Object, newFrame As Integer) Handles mobjMetaData.ProcessedScene
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub()
                               NewSceneCached(sender, newFrame)
                           End Sub)
        Else
            If mobjMetaData.SceneFrames IsNot Nothing Then
                Dim increments As Integer = mobjMetaData.TotalFrames / ctlVideoSeeker.Width
                'Check for nothing to avoid issue with loading a new file before the scene frames were set from the last
                ctlVideoSeeker.SceneFrames = CompressSceneChanges(mobjMetaData.SceneFrames, ctlVideoSeeker.Width)
                If mobjMetaData.ThumbFrames(ctlVideoSeeker.RangeMaxValue).PTSTime Then
                    subForm.SetSRT(mobjMetaData.SubtitleStream?.Text)
                    chkSubtitles.Enabled = True
                End If
            End If
            If ctlVideoSeeker.RangeMaxValue.InRange(0, newFrame) Then
                CheckSave()
            End If
            If newFrame = -1 Then
                'Scenes finished grabbing
                'Clear previews to allow them to be rebuilt, ensuring frame accuracy
                picFrame1.SetImage(Nothing)
                picFrame2.SetImage(Nothing)
                picFrame3.SetImage(Nothing)
                picFrame4.SetImage(Nothing)
                picFrame5.SetImage(Nothing)
                PreviewsLoaded(Me, mobjMetaData.ImageFrames, New List(Of List(Of Integer)) From {New List(Of Integer) From {0, mobjMetaData.TotalFrames - 1}})
                ctlVideoSeeker.EventsEnabled = False
                subForm.ctlSubtitleSeeker.EventsEnabled = False
                ctlVideoSeeker.UpdateRange(False)
                subForm.ctlSubtitleSeeker.UpdateRange(False)
                subForm.ctlSubtitleSeeker.EventsEnabled = True
                ctlVideoSeeker.EventsEnabled = True
            End If
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

    Private Sub SubsChanged() Handles subForm.SubChanged
        mobjOutputProperties.Subtitles = subForm.FilePath
        UpdateSubtitleTooltip()
    End Sub

    Private Sub subForm_VisibleChanged(sender As Object, e As EventArgs) Handles subForm.VisibleChanged
        'Assume a user doesn't want subtitles anymore if they close the form
        If Not subForm.Visible Then
            chkSubtitles.Checked = False
        End If
    End Sub

#Region "DragDrop"
    Private Sub MainForm_DragDrop(sender As Object, e As DragEventArgs) Handles MyBase.DragDrop
        Dim files() As String = e.Data.GetData(DataFormats.FileDrop)
        Me.Activate()

        If Me.mobjMetaData Is Nothing AndAlso files.Count = 1 Then
            LoadFiles(files)
        Else
            Select Case MessageBox.Show(Me, $"Open {files.Count} file(s)?", "Open File(s)?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                Case DialogResult.OK
                    LoadFiles(files)
                Case Else
                    Exit Sub
            End Select
        End If
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
            sfdExportAudio.Filter = "MP3|*.mp3|MP4|*.mp4|AAC|*.aac|FLAC|*.flac|WAV|*.wav|OGG|*.ogg|WMA|*.wma|M4A|*.m4a|All files (*.*)|*.*"
            Dim detectedStreamExtension As String = "." & mobjMetaData.AudioStream.Type
            Dim filterIndex As Integer = 0
            For Each objFilter In sfdExportAudio.Filter.Split("|")
                If objFilter.ToLower.Equals(mobjMetaData.AudioStream.Type) Then
                    sfdExportAudio.FilterIndex = (filterIndex / 2) + 1
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
                    If File.Exists(sfdExportAudio.FileName) Then
                        'Show file location of saved file
                        OpenOrFocusFile(sfdExportAudio.FileName)
                    End If
                Case Else
                    'Do nothing
            End Select
        End Using
    End Sub

    ''' <summary>
    ''' Ensure value in custom playback speed is a number
    ''' </summary>
    Private Sub CustomSpeedTextToolStripMenuItem_TextChanged(sender As Object, e As EventArgs) Handles CustomSpeedTextToolStripMenuItem.TextChanged
        CustomSpeedTextToolStripMenuItem.Text = Regex.Match(CustomSpeedTextToolStripMenuItem.Text, "\d*.?\d*").Value
    End Sub

    Private Sub cmsPlaybackSpeed_Closing(sender As Object, e As ToolStripDropDownClosingEventArgs) Handles cmsPlaybackSpeed.Closing
        If CustomToolStripMenuItem.Checked Then
            cmsPlaybackVolume_ItemClicked(Me, New ToolStripItemClickedEventArgs(CustomToolStripMenuItem))
        End If
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        'Cleanup files we don't need
        If File.Exists(mobjMetaData?.FullPath) AndAlso mobjMetaData.FullPath.StartsWith(TempPath) Then
            File.Delete(mobjMetaData.FullPath)
        End If
        CleanupTempFolder()
    End Sub

    ''' <summary>
    ''' Deletes the temp folder for this process as long as this is the only instance of the process
    ''' </summary>
    Private Sub CleanupTempFolder()
        Try
            Dim sveProcesses As Process() = Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName)
            If sveProcesses.Count = 1 Then
                'We are the only process, so we can clean our temp folder
                If Directory.Exists(TempPath) Then
                    DeleteDirectory(TempPath)
                End If
            End If
        Catch ex As Exception
            'Bad news bears
            'If we ever add logging, this would be nice to log, but otherwise don't bother the user
        End Try
    End Sub

    Private Sub MainForm_LocationChanged(sender As Object, e As EventArgs) Handles MyBase.LocationChanged
        Dim locationDelta As New Point(Me.Location.X - lastLocation.X, Me.Location.Y - lastLocation.Y)
        subForm.Location = New Point(subForm.Location.X + locationDelta.X, subForm.Location.Y + locationDelta.Y)
        lastLocation = Me.Location
    End Sub

    Private Sub MainForm_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        'Setup for form location tracking
        lastLocation = Me.Location
    End Sub

    Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        lastLocation = Me.Location
    End Sub

    Private Sub BakedInHardToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BakedInHardToolStripMenuItem.Click
        BakedInHardToolStripMenuItem.Checked = True
        ToggleableSoftToolStripMenuItem.Checked = False
        mobjOutputProperties.BakeSubs = True
    End Sub

    Private Sub ToggleableSoftToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ToggleableSoftToolStripMenuItem.Click
        BakedInHardToolStripMenuItem.Checked = False
        ToggleableSoftToolStripMenuItem.Checked = True
        mobjOutputProperties.BakeSubs = False
    End Sub

    Private Sub lblStatusResolution_DoubleClick(sender As Object, e As EventArgs) Handles lblStatusResolution.DoubleClick
        If mobjMetaData IsNot Nothing Then
            'Resize form so preview is as big as the original resolution
            Dim widthDelta As Integer = mobjMetaData.Width - picVideo.Width
            Dim heightDelta As Integer = mobjMetaData.Height - picVideo.Height
            Me.Width += widthDelta
            Me.Height += heightDelta
        End If
    End Sub
#End Region

End Class
