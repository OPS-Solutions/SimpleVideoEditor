Imports System.Text.RegularExpressions
Imports System.Threading

Public Class VideoData
    Implements IDisposable

    Private Structure MetaData
        Dim name As String
        Dim major_brand As String
        Dim minor_version As String
        Dim compatible_brands As String
        Dim creation_time As String
        Dim location As String
        Dim location_eng As String
        Dim comAndroidVersion As String
        Dim comAndroidCaptureFps As Double
        Dim duration As String 'HH:MM:SS.ss
        Dim bitrate As Integer 'kb/s
        Dim stream0 As VideoStreamData
        Dim totalFrames As Integer
    End Structure

    Private Structure VideoStreamData
        Dim raw As String
        Dim video As String
        Dim resolution As System.Drawing.Size
        Dim framerate As Double
    End Structure

    Private mobjMetaData As New MetaData
    Private mdblSceneFrames As Double()


    Private mobjImageCache As ImageCache
    Private mobjThumbCache As ImageCache


    ''' <summary>Event for when some number of frames have been queued for retrieval</summary>
    Public Event QueuedFrames(sender As Object, startFrame As Integer, endFrame As Integer)

    ''' <summary>Event for when some number of frames has finished retrieval, and can be accessed</summary>
    Public Event RetrievedFrames(sender As Object, startFrame As Integer, endFrame As Integer)


    ''' <summary>
    ''' Gets metadata for video files using ffmpeg command line arguments, and parses it into an object
    ''' </summary>
    ''' <param name="fullPath"></param>
    Public Shared Function FromFile(ByVal fullPath As String) As SimpleVideoEditor.VideoData
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

        Return New SimpleVideoEditor.VideoData(fullPath, output)
    End Function

    ''' <summary>
    ''' Parses metadata from a string. Generally given by "ffmpeg -i filename.ext"
    ''' </summary>
    ''' <param name="dataDump"></param>
    Public Sub New(ByVal file As String, ByVal dataDump As String)
        mobjMetaData.name = IO.Path.GetFileName(file)
        mobjMetaData.location = IO.Path.GetDirectoryName(file)
        Dim newVideoData As New VideoStreamData
        dataDump = dataDump.ToLower
        Dim metaDataIndex As Integer = dataDump.ToLower.IndexOf("metadata:")
        dataDump = dataDump.Substring(metaDataIndex)
        'Get duration
        mobjMetaData.duration = Regex.Match(dataDump, "(?<=duration: )\d\d:\d\d:\d\d\.\d\d").Groups(0).Value
        'Find video stream line
        Dim streamString As String = Regex.Match(dataDump, "stream.*video.*").Groups(0).Value
        Dim resolutionString As String = Regex.Match(streamString, "(?<=, )\d*x\d*").Groups(0).Value
        newVideoData.resolution = New System.Drawing.Size(Integer.Parse(resolutionString.Split("x")(0)), Integer.Parse(resolutionString.Split("x")(1)))
        'Get framerate from "30.00 fps"
        newVideoData.framerate = Double.Parse(Regex.Match(streamString, "\d*(\.\d*)? fps").Groups(0).Value.Split(" ")(0))
        newVideoData.raw = streamString
        Dim frameRateGroups As MatchCollection = Regex.Matches(dataDump, "(?<=frame=)( )*\d*")
        mobjMetaData.totalFrames = Integer.Parse(frameRateGroups(frameRateGroups.Count - 1).Value.Trim())
        mobjMetaData.stream0 = newVideoData
        'Failed to get duration, try getting it based on framerate and total frames
        If mobjMetaData.duration.Length = 0 Then
            mobjMetaData.duration = FormatHHMMSSm(mobjMetaData.totalFrames / newVideoData.framerate)
        End If
        mobjImageCache = New ImageCache(Me.TotalFrames)
        mobjThumbCache = New ImageCache(Me.TotalFrames)
    End Sub


#Region "Data Extraction"
    ''' <summary>
    ''' Gets a list of frames where a scene has changed
    ''' </summary>
    Public Async Function ExtractSceneChanges() As Task(Of Double())
        'ffmpeg -i GEVideo.wmv -vf select=gt(scene\,0.2),showinfo -f null -
        'ffmpeg -i GEVideo.wmv -vf select='gte(scene,0)',metadata=print -an -f null -
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        processInfo.Arguments += " -vf select='gte(scene,0)',metadata=print -an -f null -"
        processInfo.UseShellExecute = False 'Must be false to redirect standard output
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True
        Dim tempProcess As Process = Process.Start(processInfo)

        Dim sceneValues(mobjMetaData.totalFrames - 1) As Double
        Using recievedStream As New System.IO.MemoryStream
            Dim sceneMatcher As New Regex("(?<=.scene_score=)\d+\.\d+")
            Dim currentFrame As Integer = 0
            While True
                Dim currentLine As String = Await tempProcess.StandardError.ReadLineAsync()
                'Check end of stream
                If currentLine Is Nothing Then
                    Exit While
                End If
                Dim matchAttempt As Match = sceneMatcher.Match(currentLine)
                If matchAttempt.Success Then
                    sceneValues(currentFrame) = Double.Parse(matchAttempt.Value)
                    currentFrame += 1
                End If
            End While
        End Using
        Me.mdblSceneFrames = sceneValues
        Return Me.mdblSceneFrames
    End Function

    Public Async Function ExtractThumbFrames() As Task(Of ImageCache)
        Await GetFfmpegFrameAsync(0, -1, New Size(0, 10), mobjThumbCache)
        Return mobjThumbCache
    End Function

    Public Function GetImageFromCache(imageIndex As Integer) As Bitmap
        If mobjImageCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return Me.mobjImageCache(imageIndex).Image
        Else
            Return Nothing
        End If
    End Function

    Public Function ImageCacheStatus(imageIndex As Integer) As ImageCache.CacheStatus
        Return mobjImageCache.ImageCacheStatus(imageIndex)
    End Function

    Public Function GetImageFromThumbCache(imageIndex As Integer) As Bitmap
        If mobjThumbCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return Me.mobjThumbCache(imageIndex).Image
        Else
            Return Nothing
        End If
    End Function

    Public Function ThumbImageCacheStatus(imageIndex As Integer) As ImageCache.CacheStatus
        Return mobjThumbCache.ImageCacheStatus(imageIndex)
    End Function

    ''' <summary>
    ''' Polls ffmpeg for the given frame asynchrounously
    ''' </summary>
    Public Async Function GetFfmpegFrameAsync(ByVal frame As Integer, Optional cacheSize As Integer = 20, Optional frameSize As Size = Nothing, Optional targetCache As ImageCache = Nothing) As Task(Of Bitmap)
        If targetCache Is Nothing Then
            targetCache = mobjImageCache
        End If
        If targetCache(frame).Image IsNot Nothing AndAlso cacheSize >= 0 Then
            'If we are at the edge of the cached items, try to expand it a little in advance
            If targetCache(Math.Min(frame + 4, Math.Max(0, mobjMetaData.totalFrames - 4))).Status = ImageCache.CacheStatus.None Then
                Task.Run(Sub()
                             GetFfmpegFrameAsync(Math.Min(frame + 1, mobjMetaData.totalFrames - 1), cacheSize, frameSize, targetCache)
                         End Sub)
            End If
            If targetCache(Math.Max(0, frame - 4)).Status = ImageCache.CacheStatus.None Then
                Task.Run(Sub()
                             GetFfmpegFrameAsync(Math.Min(frame + 1, mobjMetaData.totalFrames - 1), cacheSize, frameSize, targetCache)
                         End Sub)
            End If
            Return targetCache(frame).Image
        End If
        Dim earlyFrame As Integer = Math.Max(0, frame - (cacheSize - 1) / 2)

        'Check what nearby frames need to be grabbed, don't re-grab ones we already have
        Dim startFrame As Integer = frame 'First frame that is not cached
        If cacheSize < 0 Then
            startFrame = 0
        End If
        'Step backwards from the requested frame, preparing to get anything that hasn't been grabbed yet
        For index As Integer = frame To earlyFrame Step -1
            If targetCache(index).Image Is Nothing AndAlso targetCache(index).QueueTime Is Nothing Then
                startFrame = index
            Else
                Exit For
            End If
        Next
        Dim lateFrame As Integer = Math.Min(mobjMetaData.totalFrames - 1, startFrame + cacheSize)
        Dim endFrame As Integer = frame 'Last frame that is not cached
        If cacheSize < 0 Then
            endFrame = mobjMetaData.totalFrames - 1
        End If
        'Step forwards from the current frame, preparing to get anything that hasn't been grabbed yet
        For index As Integer = frame To lateFrame
            If targetCache(index).Image Is Nothing AndAlso targetCache(index).QueueTime Is Nothing Then
                endFrame = index
            Else
                Exit For
            End If
        Next
        Debug.Print($"Working for frames:{startFrame}-{endFrame}")
        'Mark images that we are looking for
        targetCache.SetQueue(startFrame, endFrame)

        Dim cacheTotal As Integer = endFrame - startFrame + 1
        Dim tempWatch As New Stopwatch
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        tempWatch.Start()
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments = $" -ss {FormatHHMMSSm((frame) / Me.Framerate)} -r {Me.Framerate}"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        If frameSize.Width = 0 AndAlso frameSize.Height = 0 Then
            frameSize.Width = 288
        End If
		processInfo.Arguments += $" -r {Me.Framerate} -vf scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)} -vframes {cacheTotal} -f image2pipe -vcodec bmp -"
		processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
		processInfo.RedirectStandardOutput = True
		processInfo.WindowStyle = ProcessWindowStyle.Hidden
		Dim tempProcess As Process = Process.Start(processInfo)
		RaiseEvent QueuedFrames(Me, startFrame, endFrame)

		'Grab each frame as they are output from ffmpeg in real time as fast as possible
		'Don't wait for the entire thing to complete
		Dim currentFrame As Integer = startFrame
        While True
            Dim headerBuffer(5) As Char
            Dim readCount As Integer = Await tempProcess.StandardOutput.ReadBlockAsync(headerBuffer, 0, 6)
            If readCount < 6 Then
                Exit While
            End If
            Dim imageByteCount As Integer
            If headerBuffer(0) = "B" AndAlso headerBuffer(1) = "M" Then
                imageByteCount = BitConverter.ToInt32(System.Text.Encoding.Default.GetBytes(headerBuffer, 2, 4), 0)
            End If
            Dim imageBuffer(imageByteCount - 1) As Char
            'Copy header
            For index As Integer = 0 To 5
                imageBuffer(index) = headerBuffer(index)
            Next
            Dim readImageBytes As Integer = Await tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, 6, imageByteCount - 6)
            If readImageBytes < imageByteCount - 6 Then
                Exit While
            End If
            Dim imageBytes(imageByteCount - 1) As Byte
            imageBytes = System.Text.Encoding.Default.GetBytes(imageBuffer)

            Using recievedstream As New System.IO.MemoryStream
                recievedstream.Write(imageBytes, 0, imageByteCount)
                targetCache(currentFrame).Image = New Bitmap(recievedstream)
            End Using
            targetCache(currentFrame).QueueTime = Nothing
            currentFrame += 1
        End While

        tempWatch.Stop()
        Debug.Print(tempWatch.ElapsedTicks)

        'With a video of 14.92fps and 35 total frames, the total returned images ended up being less than expected

        'unmark in case there was an issue
        For index As Integer = startFrame To endFrame
            targetCache(index).QueueTime = Nothing
        Next
        RaiseEvent RetrievedFrames(Me, startFrame, endFrame)
        Return targetCache(frame).Image
    End Function

    ''' <summary>
    ''' Immediately polls ffmpeg for the given frame
    ''' </summary>
    Public Function GetFfmpegFrame(ByVal frame As Integer, Optional cacheSize As Integer = 20) As Bitmap
        Return GetFfmpegFrameAsync(frame).Result
    End Function

    ''' <summary>
    ''' Tells ffmpeg to make a file and returns the corresponding file path to the given seconds value, like 50.5 = "frame_000050.5.png".
    ''' </summary>
    Public Function ExportFfmpegFrame(ByVal frame As Integer, targetFilePath As String) As String
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments = $" -ss {FormatHHMMSSm((frame) / Me.Framerate)} -r {Me.Framerate}"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        'processInfo.Arguments += " -vf ""select=gte(n\," & frame.ToString & "), scale=228:-1"" -vframes 1 " & """" & targetFilePath & """"
        processInfo.Arguments += " -vframes 1 " & """" & targetFilePath & """"
        processInfo.UseShellExecute = True
        processInfo.WindowStyle = ProcessWindowStyle.Hidden
        Dim tempProcess As Process = Process.Start(processInfo)
        tempProcess.WaitForExit(5000) 'Wait up to 5 seconds for the process to finish
        Return targetFilePath
    End Function
#End Region

#Region "Properties"
    ''' <summary>
    ''' Filename, wtih extension ex: chicken.mp4
    ''' </summary>
    Public ReadOnly Property Name As String
        Get
            Return mobjMetaData.name
        End Get
    End Property

    ''' <summary>
    ''' Full path to file, including directory and extension ex: C:\Users\Neil\Videos\chicken.mp4
    ''' </summary>
    Public ReadOnly Property FullPath As String
        Get
            Return mobjMetaData.location + "\" + mobjMetaData.name
        End Get
    End Property

    ''' <summary>
    ''' Width of the video, or horizontal resolution
    ''' </summary>
    Public ReadOnly Property Width As Integer
        Get
            Return mobjMetaData.stream0.resolution.Width
        End Get
    End Property

    ''' <summary>
    ''' Height of the video, or horizontal resolution
    ''' </summary>
    Public ReadOnly Property Height As Integer
        Get
            Return mobjMetaData.stream0.resolution.Height
        End Get
    End Property

    ''' <summary>
    ''' Frames per second of the video, fps or framerate
    ''' </summary>
    Public ReadOnly Property Framerate As Double
        Get
            Return mobjMetaData.stream0.framerate
        End Get
    End Property

    ''' <summary>
    ''' Duration of the video HH:MM:SS.ss
    ''' </summary>
    Public ReadOnly Property Duration As String
        Get
            Return mobjMetaData.duration
        End Get
    End Property

    ''' <summary>
    ''' Duration of the video in seconds
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property DurationSeconds As Double
        Get
            Dim durationArray() As String = Me.Duration.Split(":")
            Return UShort.Parse(durationArray(0)) * 60 * 60 + UShort.Parse(durationArray(1)) * 60 + Double.Parse(durationArray(2))
        End Get
    End Property

    ''' <summary>
    ''' The total number of frames in the video, ex: 60 for a 2 second 30fps video
    ''' </summary>
    Public ReadOnly Property TotalFrames As Double
        Get
            Return mobjMetaData.totalFrames
        End Get
    End Property

    ''' <summary>
    ''' The raw stream data given by ffmpeg for stream 0
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property StreamData As String
        Get
            Return mobjMetaData.stream0.raw
        End Get
    End Property

    ''' <summary>
    ''' Array of scene change values(difference between consecutive frames)
    ''' </summary>
    Public ReadOnly Property SceneFrames As Double()
        Get
            Return mdblSceneFrames
        End Get
    End Property


    ''' <summary>
    ''' Modified path to saved scene frames
    ''' </summary>
    Private ReadOnly Property SceneFramesPath As String
        Get
            Return Me.FullPath + "-frames.txt"
        End Get
    End Property

    ''' <summary>
    ''' Saves scene frames to file so they can be loaded back later without wasting processing power
    ''' </summary>
    Public Sub SaveScenesToFile()
        Using streamWriter As New System.IO.StreamWriter(Me.SceneFramesPath)
            For index As Integer = 0 To Me.mdblSceneFrames.Count - 1
                streamWriter.WriteLine(Me.mdblSceneFrames(index))
            Next
            streamWriter.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Reads scene frames from a file. Returns false on failure
    ''' </summary>
    Public Function ReadScenesFromFile() As Boolean
        If IO.File.Exists(Me.SceneFramesPath) Then
            Using streamReader As New System.IO.StreamReader(Me.SceneFramesPath)
                ReDim mdblSceneFrames(Me.TotalFrames - 1)
                For index As Integer = 0 To Me.mdblSceneFrames.Count - 1
                    Me.mdblSceneFrames(index) = Double.Parse(streamReader.ReadLine())
                Next
                streamReader.Close()
            End Using
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Array of scene change values(difference between consecutive frames)
    ''' </summary>
    Public ReadOnly Property ThumbFrames As ImageCache
        Get
            Return mobjThumbCache
        End Get
    End Property

    ''' <summary>
    ''' Modified path to saved scene frames
    ''' </summary>
    Private ReadOnly Property ThumbFramesPath As String
        Get
            Return Me.FullPath + "-thumbframes.xml"
        End Get
    End Property

    ''' <summary>
    ''' Saves scene frames to file so they can be loaded back later without wasting processing power
    ''' </summary>
    Public Sub SaveThumbsToFile()
        mobjThumbCache.SaveToFile(Me.ThumbFramesPath)
    End Sub

    ''' <summary>
    ''' Reads scene frames from a file. Returns false on failure
    ''' </summary>
    Public Function ReadThumbsFromFile() As Boolean
        If IO.File.Exists(Me.ThumbFramesPath) Then
            mobjThumbCache = ImageCache.ReadFromFile(Me.ThumbFramesPath)
            Return True
        End If
        Return False
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
                mobjImageCache.ClearImageCache()
                mobjThumbCache.ClearImageCache()
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
#End Region

End Class
