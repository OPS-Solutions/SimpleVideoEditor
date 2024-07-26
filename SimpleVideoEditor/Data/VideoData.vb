Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports SimpleVideoEditor.ImageCache

Public Class VideoData
    Implements IDisposable

    Private Structure MetaData
        Dim Name As String
        Dim MajorBrand As String
        Dim MinorVersion As String
        Dim CompatibleBrands As String
        Dim CreationTime As String
        Dim Location As String
        Dim LocationEng As String
        Dim ComAndroidVersion As String
        Dim ComAndroidCaptureFps As Double
        Dim Duration As String 'HH:MM:SS.ss

        Dim VideoStreams As List(Of VideoStreamData)
        Dim SubtitleStreams As List(Of SubtitleStreamData)
        Dim AudioStreams As List(Of AudioStreamData)

        Dim VideoSize As Long 'Size in kB
        Dim AudioSize As Long 'Size in kB

        Dim TotalFrames As Integer
    End Structure

    Public Class VideoStreamData
        Public ReadOnly Property Raw As String
        Public ReadOnly Property Type As String
        Public ReadOnly Property Resolution As System.Drawing.Size
        Public ReadOnly Property Framerate As Double
        Public ReadOnly Property Bitrate As Integer 'kb/s
        Public ReadOnly Property TimeBase As Integer
        Private Sub New(streamDescription As String)
            _Raw = streamDescription
            Dim resolutionString As String = Regex.Match(streamDescription, "(?<=, )\d*x\d*").Groups(0).Value

            'Sample Aspect Ratio
            Dim sarMatch As Match = Regex.Match(streamDescription, "sar (?<width>\d+):(?<height>\d+)")
            Dim sarWidth As Integer = 1
            Dim sarHeight As Integer = 1
            If sarMatch.Success Then
                sarWidth = sarMatch.Groups("width").Value
                sarHeight = sarMatch.Groups("height").Value
            End If

            'Display Aspect Ratio
            Dim darMatch As Match = Regex.Match(streamDescription, "dar (?<width>\d+):(?<height>\d+)")
            Dim darWidth As Integer = 1
            Dim darHeight As Integer = 1
            If darMatch.Success Then
                darWidth = darMatch.Groups("width").Value
                darHeight = darMatch.Groups("height").Value
            End If

            _Resolution = New System.Drawing.Size(Integer.Parse(resolutionString.Split("x")(0)), Integer.Parse(resolutionString.Split("x")(1)))
            If sarWidth <> sarHeight Then
                If sarWidth > sarHeight Then
                    _Resolution = New Size(_Resolution.Width * sarWidth / sarHeight, _Resolution.Height)
                End If
            End If

            'Displaymatrix
            Dim matrixRotation As String = Regex.Match(streamDescription, "displaymatrix: rotation of (?<rotation>(-|)\d*.\d*)").Groups("rotation").Value
            Dim rotationValue As Double = 0
            If Double.TryParse(matrixRotation, rotationValue) Then
                If rotationValue = -90.0 OrElse rotationValue = 90 Then
                    _Resolution = New Size(_Resolution.Height, _Resolution.Width)
                End If
            End If

            'Get framerate from "30.00 fps"
            _Framerate = Double.Parse(Regex.Match(streamDescription, "\d*(\.\d*)? fps").Groups(0).Value.Split(" ")(0))
            _Type = Regex.Match(streamDescription, "stream.*video.*? (?<Type>.*?) .*").Groups("Type").Value

            Dim tempRate As Integer = -1
            Integer.TryParse(Regex.Match(streamDescription, "bitrate: (?<bitrate>\d*).kb\/s").Groups("bitrate").Value, tempRate)
            _Bitrate = tempRate

            'Timebase
            Dim timeBaseMatcher As New Regex("stream.*?((?<timebasereal>\d+)(?<multtbr>[kmb])? tbr)?(, (?<timebasenumber>\d+)(?<multtbn>[kmb])? tbn)")
            Dim tempBase As Integer = 0
            Dim tempBaseReal As Integer = 0
            Dim timeBaseMatch As Match = timeBaseMatcher.Match(streamDescription)
            If timeBaseMatch.Success Then
                Integer.TryParse(timeBaseMatch.Groups("timebasereal").Value, tempBaseReal)
                Select Case timeBaseMatch.Groups("multtbr").Value
                    Case "k"
                        tempBaseReal *= 1000
                    Case "m" 'Unsure if this can actually happen
                        tempBaseReal *= 1000000
                    Case "b" 'Unsure if this can actually happen
                        tempBaseReal *= 1000000000
                End Select
                Integer.TryParse(timeBaseMatch.Groups("timebasenumber").Value, tempBase)
                Select Case timeBaseMatch.Groups("multtbn").Value
                    Case "k"
                        tempBase *= 1000
                    Case "m" 'Unsure if this can actually happen
                        tempBase *= 1000000
                    Case "b" 'Unsure if this can actually happen
                        tempBase *= 1000000000
                End Select
            End If

            'Some videos can be missing the framerate (observed on a 2 frame gif)
            'tbr is not perfectly accurate, but should at least be better than 0
            If _Framerate = 0 Then
                _Framerate = tempBaseReal
            End If
            _TimeBase = tempBase
        End Sub
        Public Shared Function FromDescription(streamDescription As String)
            If Regex.Match(streamDescription, "stream.*video.*").Groups(0).Value?.Length > 0 Then
                Return New VideoStreamData(streamDescription)
            Else
                Return Nothing
            End If
        End Function
    End Class

    Public Class AudioStreamData
        Public ReadOnly Property Raw As String
        Public ReadOnly Property Type As String 'Like aac or mp3
        Public ReadOnly Property SampleRate As Double 'Like 44100 hz
        Public ReadOnly Property Channel As String 'Like stereo
        Public ReadOnly Property Bitrate As Double 'Like 127 kb/s
        Private Sub New(streamDescription As String)
            _Raw = streamDescription
            Dim audioMatch As Match = Regex.Match(streamDescription, "stream.*audio.*? (?<Type>.*?) .*?(?<SampleRate>\d*) hz, (?<Channel>.*?),")
            _Type = audioMatch.Groups("Type").Value
            Double.TryParse(audioMatch.Groups("SampleRate").Value, _SampleRate)
            _Channel = audioMatch.Groups("Channel").Value
            'Apparently bitrate isn't guaranteed to appear here. Could be fltp instead
            Double.TryParse(Regex.Match(streamDescription, ".*?(?<Bitrate>\d*) kb\/s.*").Groups("Bitrate").Value, _Bitrate)
        End Sub

        Public Shared Function FromDescription(streamDescription As String)
            If streamDescription?.Length > 0 Then
                Return New AudioStreamData(streamDescription)
            Else
                Return Nothing
            End If
        End Function
    End Class

    Public Class SubtitleStreamData
        Public ReadOnly Property Text As String
        Public ReadOnly Property Language As String
        Private Sub New(streamDescription As String)
            _Text = streamDescription
            Dim subMatch As Match = Regex.Match(streamDescription, "stream.*\((?<Language>.*)\).*subtitle")
            _Language = subMatch.Groups("Language").Value
        End Sub

        ''' <summary>
        ''' Uses ffmpeg to attempt to extract the first subtitle track from the given file
        ''' </summary>
        ''' <param name="filePath"></param>
        Public Sub ExtractSrt(filePath As String)
            If Not Directory.Exists(Globals.TempPath) Then
                Directory.CreateDirectory(Globals.TempPath)
            End If
            Dim processInfo As New ProcessStartInfo
            processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
            Dim srtFile As String = Globals.GetTempSrt()
            processInfo.Arguments += $" -i ""{filePath}"" -map 0:s:0 -f srt ""{srtFile}"""
            processInfo.RedirectStandardOutput = True
            processInfo.RedirectStandardError = True
            processInfo.UseShellExecute = False
            processInfo.WindowStyle = ProcessWindowStyle.Hidden
            processInfo.CreateNoWindow = True
            Using srtProcess As Process = Process.Start(processInfo)
                Dim errOutput As String
                Using srtErr As StreamReader = srtProcess.StandardError
                    errOutput = srtErr.ReadToEndAsync.Wait(1000)
                End Using
            End Using
            _Text = IO.File.ReadAllText(srtFile)
        End Sub

        Public Shared Function FromDescription(streamDescription As String)
            If streamDescription?.Length > 0 Then
                Return New SubtitleStreamData(streamDescription)
            Else
                Return Nothing
            End If
        End Function
    End Class

    Private mobjMetaData As New MetaData
    Private mdblSceneFrames As Double()
    Private mblnSceneFramesLoaded As Boolean = False
    Private mblnTotalOk As Boolean = False
    Public Exporting As Boolean = False 'Tracks completion of a frame export request


    Private mobjImageCache As ImageCache
    Private mobjTempCache As ImageCache 'Used for temporary storage when attempting to grab frames of a different size than image or thumbcache, contents get cleared away
    Private mintTempIndex As Integer 'Last grabbed image index
    Private mobjThumbCache As ImageCache
    Private mblnInputMash As Boolean 'Data we set manaully saying the type of input we are using is actually a way to specify multiple files
    Private mobjSizeOverride As Size? 'For holding resolution data, such as when the resolution fails to be read, but we can still get an image from the stream and figure it out

    Private Shared mobjLock As String = "Lock"

    Private showInfoRegex As New Regex("n:\s*(?<index>\d*).*pts:\s*(?<pts>(-|)\d+|nan).*pts_time:(?<pts_time>(-|)[\d\.]+|nan)")
    Private showInfoBaseRegex As New Regex("config in time_base: (?<numerator>\d+)\/(?<denominator>\d+)")


    ''' <summary>Event for when some number of frames have been queued for retrieval</summary>
    Public Event QueuedFrames(sender As Object, cache As ImageCache, ranges As List(Of List(Of Integer)))

    ''' <summary>Event for when some number of frames has finished retrieval, and can be accessed</summary>
    Public Event RetrievedFrames(sender As Object, cache As ImageCache, ranges As List(Of List(Of Integer)))

    ''' <summary>Event for when a frame that was requested to export is detected as having a file created for it</summary>
    Public Event ExportProgressed(sender As Object, frame As String)

    ''' <summary>Event for a scene frame value being retrieved. Frame -1 when finished</summary>
    Public Event ProcessedScene(sender As Object, frame As Integer)


    ''' <summary>
    ''' Gets metadata for video files using ffmpeg command line arguments, and parses it into an object
    ''' </summary>
    ''' <param name="fullPath"></param>
    Public Shared Function FromFile(ByVal fullPath As String, Optional inputMash As Boolean = False) As SimpleVideoEditor.VideoData
        'Request metadata from ffmpeg vis -i command
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += "-i " & """" & fullPath & """" & " -vsync 0 -c copy -f null null"
        processInfo.UseShellExecute = False
        processInfo.RedirectStandardError = True
        processInfo.CreateNoWindow = True
        Using tempProcess As Process = Process.Start(processInfo)
            'Swap output to inside this application
            Dim output As String
            Using streamReader As System.IO.StreamReader = tempProcess.StandardError
                output = streamReader.ReadToEnd()
            End Using
            tempProcess.WaitForExit(1000)
            tempProcess.Close()

            Return New SimpleVideoEditor.VideoData(fullPath, output) With {.mblnInputMash = inputMash}
        End Using
    End Function

    ''' <summary>
    ''' Parses metadata from a string. Generally given by "ffmpeg -i filename.ext"
    ''' </summary>
    ''' <param name="dataDump"></param>
    Public Sub New(ByVal file As String, ByVal dataDump As String)
        Me.FullPath = file
        dataDump = dataDump.ToLower
        Dim metaDataIndex As Integer = dataDump.ToLower.IndexOf("metadata:")
        dataDump = dataDump.Substring(metaDataIndex)
        'Get duration
        mobjMetaData.Duration = Regex.Match(dataDump, "(?<=duration: )\d\d:\d\d:\d\d\.\d\d").Groups(0).Value

        'Video Stream Info
        Dim newVideoData As VideoStreamData = VideoStreamData.FromDescription(dataDump)
        If newVideoData IsNot Nothing Then
            mobjMetaData.VideoStreams = New List(Of VideoStreamData)
            mobjMetaData.VideoStreams.Add(newVideoData)
        End If

        'Audio Stream Info
        Dim newAudioData As AudioStreamData = AudioStreamData.FromDescription(Regex.Match(dataDump, "stream.*audio.*").Groups(0).Value)
        If newAudioData IsNot Nothing Then
            mobjMetaData.AudioStreams = New List(Of AudioStreamData)
            mobjMetaData.AudioStreams.Add(newAudioData)
        End If

        'Subtitle stream info
        Dim newSubtitleData As SubtitleStreamData = SubtitleStreamData.FromDescription(Regex.Match(dataDump, "stream.*subtitle.*").Groups(0).Value)
        If newSubtitleData IsNot Nothing Then
            Try
                newSubtitleData.ExtractSrt(file)
                mobjMetaData.SubtitleStreams = New List(Of SubtitleStreamData)
                mobjMetaData.SubtitleStreams.Add(newSubtitleData)
            Catch ex As Exception
                'Failed to read subtitles
                'TODO Warning the user could be helpful, but I don't think it warrants a popup
                'Likely need some new logging capabilities, perhaps toasts
            End Try
        End If

        Dim frameRateGroups As MatchCollection = Regex.Matches(dataDump, "(?<=frame=)( )*\d*")
        If frameRateGroups.Count > 0 Then
            mobjMetaData.TotalFrames = Integer.Parse(frameRateGroups(frameRateGroups.Count - 1).Value.Trim()) + 1
        Else
            If mobjMetaData.Duration.Length = 0 Then
                'Ffmpeg 6.1 seems to tell the time of a frame
                Dim sizeGroups As MatchCollection = Regex.Matches(dataDump, "(?<=size).*(?<time>-?\d\d:\d\d:\d\d\.\d\d)")
                If sizeGroups.Count > 1 Then
                    Dim startTime As Double = HHMMSSssToSeconds(sizeGroups(0).Groups("time").Value)
                    Dim endTime As Double = HHMMSSssToSeconds(sizeGroups(sizeGroups.Count - 1).Groups("time").Value)
                    mobjMetaData.Duration = FormatHHMMSSm(endTime - startTime)
                ElseIf sizeGroups.Count > 0 Then
                    mobjMetaData.Duration = sizeGroups.Item(sizeGroups.Count - 1).Value
                End If
            End If
            'Can fail to find frame groups in ffmpeg 6.1
            mobjMetaData.TotalFrames = (HHMMSSssToSeconds(mobjMetaData.Duration) * newVideoData.Framerate) + 1
        End If

        'Failed to get duration, try getting it based on framerate and total frames
        If mobjMetaData.Duration.Length = 0 Then
            mobjMetaData.Duration = FormatHHMMSSm(mobjMetaData.TotalFrames / newVideoData.Framerate)
        End If

        Dim sizeCheck As Integer = 0
        Integer.TryParse(Regex.Match(dataDump, "video:(?<video>\d*)kb.*audio:(?<audio>\d*)").Groups("video").Value, sizeCheck)
        mobjMetaData.VideoSize = sizeCheck
        sizeCheck = 0
        Integer.TryParse(Regex.Match(dataDump, "video:(?<video>\d*)kb.*audio:(?<audio>\d*)").Groups("audio").Value, sizeCheck)
        mobjMetaData.AudioSize = sizeCheck

        mobjImageCache = New ImageCache(Me.TotalFrames)
        mobjTempCache = New ImageCache(Me.TotalFrames, True)
        mobjThumbCache = New ImageCache(Me.TotalFrames)
    End Sub


#Region "Data Extraction"
    ''' <summary>
    ''' Gets a list of frames where a scene has changed
    ''' </summary>
    Public Async Function ExtractSceneChanges(Optional eventFrequency As Integer = -1) As Task(Of Double())
        Dim tempWatch As New Stopwatch
        tempWatch.Start()
        'ffmpeg -i GEVideo.wmv -vf select=gt(scene\,0.2),showinfo -f null -
        'ffmpeg -i GEVideo.wmv -vf select='gte(scene,0)',metadata=print -an -f null -
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += Me.InputArgs
        processInfo.Arguments += " -vf select='gte(scene,0)',metadata=print -an -f null -"
        processInfo.UseShellExecute = False 'Must be false to redirect standard output
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True
        Dim tempProcess As Process = Process.Start(processInfo)
        TrackProcess(tempProcess)
#If DEBUG Then
        Dim fullDataRead As New StringBuilder
#End If

        Dim sceneValues(mobjMetaData.TotalFrames - 1) As Double
        Me.mdblSceneFrames = sceneValues
        Dim currentFrame As Integer = 0
        Dim readTask As Task = Task.Run(Sub()
                                            Dim dispatchedCount As Integer = 0
                                            Dim sceneMatcher As New Regex("(?<=.scene_score=)\d+\.\d+")
                                            Dim ptsMatcher As New Regex("pts:\s*(?<pts>(-|)\d+|nan)\s*pts_time:(?<pts_time>[\d\.]+|nan)")

                                            While True
                                                Dim currentLine As String = tempProcess.StandardError.ReadLine
                                                dispatchedCount += 1
                                                'Check end of stream
                                                If currentLine Is Nothing Then
                                                    Exit While
                                                End If
                                                Dim matchAttempt As Match = sceneMatcher.Match(currentLine)
                                                If matchAttempt.Success Then
                                                    If currentFrame >= sceneValues.Count Then
                                                        'Problem
                                                        Debug.Print($"Scene score parse issue. Number of scene scores {currentFrame} exceeds number of frames {sceneValues.Count}.")
                                                    Else
                                                        sceneValues(currentFrame) = Double.Parse(matchAttempt.Value)
                                                        If eventFrequency > 0 Then
                                                            If currentFrame Mod eventFrequency = 0 Then
                                                                RaiseEvent ProcessedScene(Me, currentFrame)
                                                            End If
                                                        End If
                                                        currentFrame += 1
                                                    End If
                                                Else
                                                    Dim ptsMatch As Match = ptsMatcher.Match(currentLine)
                                                    If ptsMatch.Success Then
                                                        Dim ptsTimeValue As Double = Double.NaN
                                                        Dim ptsValue As Integer = 0
                                                        If Integer.TryParse(ptsMatch.Groups("pts").Value, ptsValue) AndAlso mobjMetaData.VideoStreams(0).TimeBase > 0 Then
                                                            SyncLock ThumbFrames
                                                                If ThumbFrames().Item(currentFrame).PTSTime Is Nothing Then
                                                                    ThumbFrames().Item(currentFrame).PTSTime = Math.Max(0, ptsValue / mobjMetaData.VideoStreams(0).TimeBase)
                                                                End If
                                                            End SyncLock
                                                        ElseIf Double.TryParse(ptsMatch.Groups("pts_time").Value, ptsTimeValue) Then
                                                            SyncLock ThumbFrames
                                                                If ThumbFrames().Item(currentFrame).PTSTime Is Nothing Then
                                                                    ThumbFrames().Item(currentFrame).PTSTime = Math.Max(0, ptsTimeValue)
                                                                End If
                                                            End SyncLock
                                                        End If
                                                    End If
                                                End If
#If DEBUG Then
                                                fullDataRead.Append(currentLine & vbCrLf)
#End If
                                            End While
                                        End Sub)
        Await readTask
        OverrideTotalFrames(currentFrame) 'Currentframe is normally the index, but since it adds one in the last loop as well, it is the full size, so no need to add 1
        ReDim Preserve mdblSceneFrames(currentFrame - 1)
        mblnSceneFramesLoaded = True
        tempWatch.Stop()
        Debug.Print($"Extracted {currentFrame} scene frames in {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")
        RaiseEvent ProcessedScene(Me, -1)
        Return Me.mdblSceneFrames
    End Function

    Public Async Function ExtractThumbFrames(Optional thumbSize As Integer = 32) As Task(Of ImageCache)
        'TODO Merge this with scene changes
        Await GetFfmpegFrameAsync(0, -1, New Size(0, thumbSize), mobjThumbCache)
        Return mobjThumbCache
    End Function

    Public Function GetImageFromCache(imageIndex As Integer, targetCache As ImageCache) As Bitmap
        If targetCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return targetCache(imageIndex).GetImage
        Else
            Return Nothing
        End If
    End Function

    Public Function GetImageFromCache(imageIndex As Integer) As Bitmap
        If mobjImageCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return Me.mobjImageCache(imageIndex).GetImage
        Else
            Return Nothing
        End If
    End Function

    Public Function GetImageDataFromCache(imageIndex As Integer, targetCache As ImageCache) As ImageCache.CacheItem
        If targetCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return targetCache(imageIndex)
        Else
            Return Nothing
        End If
    End Function

    Public Function GetImageDataFromCache(imageIndex As Integer) As ImageCache.CacheItem
        If mobjImageCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return Me.mobjImageCache(imageIndex)
        Else
            Return Nothing
        End If
    End Function

    Public Function ImageCacheStatus(imageIndex As Integer) As ImageCache.CacheStatus
        Return mobjImageCache.ImageCacheStatus(imageIndex)
    End Function

    Public Function GetImageFromThumbCache(imageIndex As Integer) As Bitmap
        If mobjThumbCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return Me.mobjThumbCache(imageIndex).GetImage
        Else
            Return Nothing
        End If
    End Function

    Public Function GetAnyCachedData(imageIndex As Integer) As ImageCache.CacheItem
        If Me.ImageCacheStatus(imageIndex) = ImageCache.CacheStatus.Cached Then
            Return Me.GetImageDataFromCache(imageIndex, mobjImageCache)
        ElseIf Me.ThumbImageCacheStatus(imageIndex) = ImageCache.CacheStatus.Cached Then
            Return Me.GetImageDataFromCache(imageIndex, mobjThumbCache)
        Else
            Return Nothing
        End If
    End Function

    Public Function AnyImageCacheStatus(imageIndex As Integer) As ImageCache.CacheStatus
        If Me.ImageCacheStatus(imageIndex) <> ImageCache.CacheStatus.None Then
            Return Me.ImageCacheStatus(imageIndex)
        Else
            Return mobjThumbCache.ImageCacheStatus(imageIndex)
        End If
    End Function

    Public Function AnyImageCachePTS(imageIndex As Integer) As Double?
        Dim resultItem As CacheItem = Me.mobjImageCache.Item(imageIndex)
        If resultItem?.PTSTime IsNot Nothing Then
            Return resultItem.PTSTime
        Else
            Return Me.mobjThumbCache.Item(imageIndex).PTSTime
        End If
    End Function

    Public Function ThumbImageCacheStatus(imageIndex As Integer) As ImageCache.CacheStatus
        Return mobjThumbCache.ImageCacheStatus(imageIndex)
    End Function

    Public Function ThumbImageCachePTS(imageIndex As Integer) As Double?
        Dim resultPTS As Double? = 0
        If mobjThumbCache.Item(imageIndex).PTSTime Is Nothing Then
            resultPTS = mobjImageCache.Item(imageIndex).PTSTime
        Else
            resultPTS = mobjThumbCache.Item(imageIndex).PTSTime
        End If
        Return resultPTS
    End Function

    ''' <summary>
    ''' Returns the closest frame to the given PTS found in either image cache
    ''' Only checks 3 decimals of milliseconds
    ''' </summary>
    Public Function GetFrameByPTS(totalSeconds As Double) As Integer?
        Dim cacheItem As Double? = ThumbImageCachePTS(Me.TotalFrames - 1)
        If cacheItem Is Nothing Then
            Return Nothing
        End If
        Dim checkIndex As Integer = Math.Min(((totalSeconds / cacheItem.Value) * (Me.TotalFrames - 1)), Me.TotalFrames - 1)
        'Make an educated guess as to where we want to check first
        Dim resultIndex As Integer = checkIndex
        'Only search in the direction that could potentially have the value we need
        Dim backwards As Boolean = False
        If ThumbImageCachePTS(checkIndex).HasValue Then
            backwards = ThumbImageCachePTS(checkIndex) > totalSeconds
        Else
            checkIndex = 0
            backwards = False
        End If
        For index As Integer = checkIndex To If(backwards, 0, Me.TotalFrames - 1) Step If(backwards, -1, 1)
            Dim checkPTS As Double? = ThumbImageCachePTS(index)
            If Not checkPTS.HasValue Then
                resultIndex = index
                Continue For
            End If
            'checkPTS = Math.Truncate(checkPTS.Value * 1000) / 1000
            If backwards Then
                If checkPTS < totalSeconds Then
                    'Return closest value
                    If Math.Abs(ThumbImageCachePTS(resultIndex).Value - totalSeconds) < Math.Abs(checkPTS.Value - totalSeconds) Then
                        Return resultIndex
                    Else
                        Return index
                    End If
                ElseIf checkPTS = totalSeconds Then
                    Return index
                Else
                    resultIndex = index
                End If
            Else
                If checkPTS > totalSeconds Then
                    'Return closest value
                    If Math.Abs(ThumbImageCachePTS(resultIndex).Value - totalSeconds) < Math.Abs(checkPTS.Value - totalSeconds) Then
                        Return resultIndex
                    Else
                        Return index
                    End If
                ElseIf checkPTS = totalSeconds Then
                    Return index
                Else
                    resultIndex = index
                End If
            End If
        Next
        Return resultIndex
    End Function

    ''' <summary>
    ''' Polls ffmpeg for all frames between start and end (inclusive), all in the same ffmpeg call
    ''' </summary>
    Public Async Function GetFfmpegFramesAsync(startframe As Integer, endframe As Integer, Optional frameSize As Size = Nothing, Optional targetCache As ImageCache = Nothing) As Task(Of Boolean)
        'Generate list of frames
        Dim frames As New List(Of Integer)
        For index As Integer = startframe To endframe
            frames.Add(index)
        Next
        Return Await GetFfmpegFrameRangesAsync(frames, frameSize, targetCache)
    End Function

    ''' <summary>
    ''' Polls ffmpeg for each given frame, all in the same ffmpeg call
    ''' </summary>
    Public Async Function GetFfmpegFrameRangesAsync(ByVal frames As List(Of Integer), Optional frameSize As Size = Nothing, Optional targetCache As ImageCache = Nothing) As Task(Of Boolean)
        Dim ranges As List(Of List(Of Integer)) = frames.CreateRanges()

        If targetCache Is Nothing Then
            targetCache = mobjImageCache
        End If

        SyncLock targetCache
            For index As Integer = 0 To ranges.Count - 1
                ranges(index) = targetCache.TrimRange(ranges(index)(0), ranges(index)(1))
            Next
            ranges.RemoveAll(Function(obj) obj Is Nothing)
            If ranges.Count <= 0 Then
                Return True
            End If
            Dim discardedFrames As New List(Of Integer)
            For Each objRange In ranges
                targetCache.TryQueue(objRange(0), objRange(1))
            Next
            For Each objVal In frames
                Dim hasValue As Boolean = False
                For Each objRange In ranges
                    If objRange(0) <= objVal AndAlso objRange(1) >= objVal Then
                        hasValue = True
                        Exit For
                    End If
                Next
                If Not hasValue Then
                    discardedFrames.Add(objVal)
                End If
            Next
            For Each objDiscardedFrame In discardedFrames
                frames.Remove(objDiscardedFrame)
            Next
        End SyncLock
        RaiseEvent QueuedFrames(Me, targetCache, ranges)

        Dim tempWatch As New Stopwatch
        tempWatch.Start()

        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        'processInfo.Arguments += $" -ss {FormatHHMMSSm((startFrame) / Me.Framerate)}"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        If frameSize.Width = 0 AndAlso frameSize.Height = 0 Then
            frameSize.Width = Math.Min(Me.Width, 288)
        End If
        'processInfo.Arguments += $" -r {Me.Framerate} -vf scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vframes {cacheTotal} -f image2pipe -vcodec bmp -"
        'FFMPEG expression evaluation https://ffmpeg.org/ffmpeg-utils.html
        Dim rangeExpression As String = ""
        For Each objRange In ranges
            If objRange(0) = objRange(1) Then
                rangeExpression += $"eq(n,{objRange(0)})+"
            Else
                rangeExpression += $"between(n,{objRange(0)},{objRange(1)})+"
            End If
        Next
        rangeExpression = rangeExpression.Substring(0, rangeExpression.Length - 1)
        processInfo.Arguments += $" -vf select='{rangeExpression}',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vcodec png -f image2pipe -"
        'processInfo.Arguments += $" -vf select='between(n,{startFrame},{endFrame})*gte(scene,0)',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},metadata=print -vsync 0 -f image2pipe -vcodec bmp -"
        processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True
        processInfo.WindowStyle = ProcessWindowStyle.Hidden

        Dim tempProcess As Process = Process.Start(processInfo)
        TrackProcess(tempProcess)
        For Each objRange In ranges
            RaiseEvent QueuedFrames(Me, targetCache, ranges)
        Next

        'Grab each frame as they are output from ffmpeg in real time as fast as possible
        'Don't wait for the entire thing to complete
        'Dim fullText As String = Await tempProcess.StandardOutput.ReadToEndAsync
        'Dim endText As String = Await tempProcess.StandardError.ReadToEndAsync
#If DEBUG Then
        Dim fullDataRead As New StringBuilder
        'Do
        '    Debug.Print(tempProcess.StandardError.ReadLine)
        'Loop While Not tempProcess.StandardError.EndOfStream
#End If
        Dim currentFrame As Integer = 0
        Dim currentErrorFrame As Integer = 0
        Dim framesRetrieved As New List(Of Integer)
        'Dim frameRegex As New Regex("frame=\s*(\d*)")
        Dim dispatchedCount As Integer = 0
        dispatchedCount += 1
        Dim bytePosition As Integer = 0
        Dim errEnd As Boolean = False

        Dim standardOutTask As Task = Task.Run(Sub()

                                                   Do
                                                       Dim imageBuffer(65535) As Char
                                                       bytePosition = 0
                                                       'Read PNG signature
                                                       Dim charsRead As Integer = tempProcess.StandardOutput.ReadBlock(imageBuffer, bytePosition, 16)
                                                       bytePosition += 16
                                                       If charsRead <= 0 Then
                                                           Exit Do
                                                       End If

                                                       While True
                                                           'Read other chunks, while checking for IEND chunk to finish
                                                           Dim identifier As String = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition - 4, 4))
                                                           Dim chunkHeaderBytes() As Byte = System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition - 8, 4)
                                                           Dim chunkSize As Integer = BitConverter.ToUInt32({chunkHeaderBytes(3), chunkHeaderBytes(2), chunkHeaderBytes(1), chunkHeaderBytes(0)}, 0)
                                                           Dim newSize As Integer = bytePosition + chunkSize + 12 - 1
                                                           If imageBuffer.Count <= newSize Then
                                                               'Double the size to avoid rediming too much and wasting resources
                                                               ReDim Preserve imageBuffer(Math.Max(newSize, imageBuffer.Count * 2))
                                                           End If
                                                           Dim nextHeaderSize As Integer = 8
                                                           If identifier.Equals("IEND") Then
                                                               nextHeaderSize = 0
                                                           End If
                                                           'Read chunk, +4 for CRC + 8 for next header
                                                           Dim nextReadSize As Integer = chunkSize + 4 + nextHeaderSize
                                                           charsRead = tempProcess.StandardOutput.ReadBlock(imageBuffer, bytePosition, nextReadSize)
                                                           dispatchedCount += 1
                                                           bytePosition += nextReadSize
                                                           If nextHeaderSize = 0 Then
                                                               Exit While
                                                           End If
                                                       End While

                                                       SyncLock targetCache
                                                           If targetCache(currentFrame).Status = ImageCache.CacheStatus.Cached Then
                                                               'Don't cache stuff we already have cached
                                                           Else
                                                               targetCache(frames(currentFrame)).ImageData = System.Text.Encoding.Default.GetBytes(imageBuffer, 0, bytePosition - 2)
                                                           End If

                                                           targetCache(frames(Math.Min(currentFrame, currentErrorFrame))).QueueTime = Nothing
                                                           framesRetrieved.Add(frames(currentFrame))

                                                           'If we have grabbed and done a lot, it wouldn't hurt to update the UI
                                                           If framesRetrieved.Last > framesRetrieved.First + 10 Then
                                                               RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
                                                               framesRetrieved.Clear()
                                                           End If
                                                       End SyncLock
                                                       currentFrame += 1
                                                   Loop
                                               End Sub)


        Dim standardErrTask As Task = Task.Run(Sub()
                                                   Dim lineRead As String = tempProcess.StandardError.ReadLine
                                                   Dim ptsNumerator As Integer = 1
                                                   Dim ptsDenominator As Integer = 1
                                                   fullDataRead.AppendLine(processInfo.Arguments)
                                                   Do
                                                       'Read StandardError for the showinfo result for PTS_Time
                                                       If lineRead IsNot Nothing Then
#If DEBUG Then
                                                           fullDataRead.Append(lineRead + vbCrLf)
#End If
                                                           Dim infoMatch As Match = showInfoRegex.Match(lineRead)
                                                           Dim baseMatch As Match = showInfoBaseRegex.Match(lineRead)

                                                           If infoMatch.Success Then
                                                               Dim matchPTSTime As Double = 0
                                                               Dim matchPTS As Integer = -1
                                                               Double.TryParse(infoMatch.Groups("pts_time").Value, matchPTSTime)
                                                               If Integer.TryParse(infoMatch.Groups("pts").Value, matchPTS) Then
                                                                   matchPTSTime = (ptsNumerator * matchPTS) / ptsDenominator
                                                               End If
                                                               Dim matchValue As Integer = Integer.Parse(infoMatch.Groups("index").Value)
                                                               If frames(matchValue) = frames(currentErrorFrame) Then
                                                                   SyncLock targetCache
                                                                       targetCache(frames(currentErrorFrame)).PTSTime = Math.Max(0, matchPTSTime)
                                                                       targetCache(frames(Math.Min(currentFrame, currentErrorFrame))).QueueTime = Nothing
                                                                   End SyncLock
                                                                   currentErrorFrame += 1
                                                               End If
                                                           ElseIf baseMatch.Success Then
                                                               Integer.TryParse(baseMatch.Groups("numerator").Value, ptsNumerator)
                                                               Integer.TryParse(baseMatch.Groups("denominator").Value, ptsDenominator)
                                                           End If
                                                           lineRead = tempProcess.StandardError.ReadLine
                                                           dispatchedCount += 1
                                                       Else
                                                           errEnd = True
                                                       End If
                                                   Loop While Not errEnd
                                               End Sub)

        'Wait for reading to finish
        Dim taskList As New List(Of Task) From {standardOutTask, standardErrTask}
        taskList.RemoveAll(Function(obj) obj Is Nothing)

        Await Task.WhenAll(taskList)
        Debug.Print($"Finished frame grab with {dispatchedCount} dispatches.")
        tempWatch.Stop()
        Dim rangeText As String = ""
        For Each objRange In ranges
            If objRange(0) = objRange(1) Then
                rangeText += $"{objRange(0)}, "
            Else
                rangeText += $"{objRange(0)}-{objRange(1)}, "
            End If
            'unmark in case there was an issue
            For index As Integer = objRange(0) To objRange(1)
                targetCache(index).QueueTime = Nothing
            Next
        Next
        Debug.Print($"Grabbed frames {rangeText.Substring(0, rangeText.Length - 2)} In {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")
        If framesRetrieved.Count > 0 Then
            RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
        End If
        Return True
    End Function

    ''' <summary>
    ''' Polls ffmpeg for the given frame asynchrounously
    ''' </summary>
    Public Async Function GetFfmpegFrameAsync(ByVal frame As Integer, Optional cacheSize As Integer = 20, Optional frameSize As Size = Nothing, Optional targetCache As ImageCache = Nothing) As Task(Of CacheItem)
        If targetCache Is Nothing Then
            targetCache = mobjImageCache
        End If
        Dim upgradeImage As Boolean = False
        If targetCache(frame).ImageData IsNot Nothing AndAlso cacheSize >= 0 AndAlso targetCache(frame).Size.Width >= frameSize.Width Then
            If cacheSize > 5 Then
                'If we are at the edge of the cached items, try to expand it a little in advance
                If targetCache(Math.Min(frame + 4, Math.Max(0, mobjMetaData.TotalFrames - 4))).Status = ImageCache.CacheStatus.None Then
                    Dim tempTask As Task = Task.Run(Sub()
                                                        Dim seedFrame As Integer = Math.Min(frame + 1, mobjMetaData.TotalFrames - 1)
                                                        If seedFrame = frame Then
                                                            Exit Sub
                                                        End If
                                                        Dim getFrameTask As Task(Of CacheItem) = GetFfmpegFrameAsync(seedFrame, cacheSize, frameSize, targetCache)
                                                    End Sub)
                End If
                If targetCache(Math.Max(0, frame - 4)).Status = ImageCache.CacheStatus.None Then
                    Dim tempTask As Task = Task.Run(Sub()
                                                        Dim seedFrame As Integer = Math.Max(0, frame - 1)
                                                        If seedFrame = frame Then
                                                            Exit Sub
                                                        End If
                                                        Dim getFrameTask As Task(Of CacheItem) = GetFfmpegFrameAsync(seedFrame, cacheSize, frameSize, targetCache)
                                                    End Sub)
                End If
            End If
            Return targetCache(frame)
        End If
        Dim earlyFrame As Integer = Math.Max(0, frame - (cacheSize - 1) / 2)

        'Check what nearby frames need to be grabbed, don't re-grab ones we already have
        Dim startFrame As Integer = frame 'First frame that is not cached
        If cacheSize < 0 Then
            startFrame = 0
        End If
        Dim endFrame As Integer = frame 'Last frame that is not cached
        Dim alreadyQueued As Boolean = False
        SyncLock targetCache
            'Step backwards from the requested frame, preparing to get anything that hasn't been grabbed yet
            For index As Integer = frame To earlyFrame Step -1
                If targetCache(index).Status = ImageCache.CacheStatus.None Then
                    startFrame = index
                Else
                    Exit For
                End If
            Next
            Dim lateFrame As Integer = Math.Min(mobjMetaData.TotalFrames - 1, startFrame + cacheSize)
            If cacheSize < 0 Then
                endFrame = mobjMetaData.TotalFrames - 1
            End If
            'Step forwards from the current frame, preparing to get anything that hasn't been grabbed yet
            For index As Integer = frame To lateFrame
                If targetCache(index).Status = ImageCache.CacheStatus.None Then
                    endFrame = index
                Else
                    Exit For
                End If
            Next
            'Go from start and end of the range we are grabbing and trim it down if we already have some images on either end
            'Cut off the end first, because it is faster for ffmpeg to traverse to the beginning of the file than the end
            While targetCache(endFrame).Status <> ImageCache.CacheStatus.None
                endFrame = Math.Max(startFrame, endFrame - 1)
                If startFrame = endFrame Then
                    Exit While
                End If
            End While
            While targetCache(startFrame).Status <> ImageCache.CacheStatus.None
                startFrame = Math.Min(endFrame, startFrame + 1)
                If startFrame = endFrame Then
                    Exit While
                End If
            End While

            If startFrame = endFrame AndAlso targetCache(frame).Status = ImageCache.CacheStatus.Cached Then
                If targetCache(frame).Size.Width >= frameSize.Width Then
                    Return targetCache(frame)
                Else
                    upgradeImage = True
                End If
            End If
            alreadyQueued = (startFrame = endFrame AndAlso targetCache(frame).Status = ImageCache.CacheStatus.Queued)
            Debug.Print($"Working for frames:{startFrame}-{endFrame} (Size:{frameSize.ToString})")
            'Mark images that we are looking for
            If Not alreadyQueued Then
                targetCache.TryQueue(startFrame, endFrame)
            End If
        End SyncLock
        If upgradeImage OrElse targetCache Is mobjTempCache Then
            targetCache = mobjTempCache
        End If
        'If you are looking for only one frame, and it is queued, dont waste time trying to grab it again...
        If alreadyQueued Then
            Debug.Print($"Waiting on predicesor call for frame:{frame}")
            Return Await Task.Run(Function()
                                      Do
                                          If Not targetCache(frame).Status = ImageCache.CacheStatus.Queued Then
                                              Return targetCache(frame)
                                          Else
                                              Threading.Thread.Sleep(10)
                                          End If
                                      Loop
                                  End Function)
        End If

        Dim tempWatch As New Stopwatch
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        tempWatch.Start()
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        'processInfo.Arguments += $" -ss {FormatHHMMSSm((startFrame) / Me.Framerate)}"

        Dim ssFlag As Boolean = False
        'If we know the exact timestamp, we should be able to accurately seek to the exact frame
        Dim currentPTS As Double? = AnyImageCachePTS(startFrame)
        If currentPTS IsNot Nothing Then
            ssFlag = True
            Dim formattedPTS As String = FormatHHMMSSm(AnyImageCachePTS(startFrame))
            formattedPTS = FormatHHMMSSm(currentPTS)
            processInfo.Arguments += $" -ss {formattedPTS}"
        End If

        processInfo.Arguments += Me.InputArgs
        If frameSize.Width = 0 AndAlso frameSize.Height = 0 Then
            frameSize.Width = Math.Min(Me.Width, 288)
        End If
        'processInfo.Arguments += $" -r {Me.Framerate} -vf scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vframes {cacheTotal} -f image2pipe -vcodec bmp -"
        'FFMPEG expression evaluation https://ffmpeg.org/ffmpeg-utils.html
        If ssFlag Then
            processInfo.Arguments += $" -vf select='between(n,{0},{endFrame - startFrame})',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vcodec png -f image2pipe -"
        Else
            processInfo.Arguments += $" -vf select='between(n,{startFrame},{endFrame})',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vcodec png -f image2pipe -"
        End If
        'processInfo.Arguments += $" -vf select='between(n,{startFrame},{endFrame})*gte(scene,0)',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},metadata=print -vsync 0 -f image2pipe -vcodec bmp -"
        processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True
        'processInfo.RedirectStandardInput = True
        'processInfo.WindowStyle = ProcessWindowStyle.Hidden

        Dim tempProcess As Process = Process.Start(processInfo)
        TrackProcess(tempProcess)
        RaiseEvent QueuedFrames(Me, targetCache, New List(Of List(Of Integer))({New List(Of Integer)({startFrame, endFrame})}))

        'Grab each frame as they are output from ffmpeg in real time as fast as possible
        'Don't wait for the entire thing to complete
        'Dim fullText As String = Await tempProcess.StandardOutput.ReadToEndAsync
        'Dim endText As String = Await tempProcess.StandardError.ReadToEndAsync
#If DEBUG Then
        Dim fullDataRead As New StringBuilder
        'Do
        '    Debug.Print(tempProcess.StandardError.ReadLine)
        'Loop While Not tempProcess.StandardError.EndOfStream
#End If
        Dim currentFrame As Integer = startFrame
        Dim currentErrorFrame As Integer = startFrame
        Dim framesRetrieved As New List(Of Integer)
        'Dim frameRegex As New Regex("frame=\s*(\d*)")
        Dim dispatchedCount As Integer = 0
        dispatchedCount += 1
        Dim bytePosition As Integer = 0
        Dim errEnd As Boolean = False

        Dim standardOutTask As Task = Task.Run(Sub()

                                                   Do
                                                       Dim imageBuffer(65535) As Char
                                                       bytePosition = 0
                                                       'Read PNG signature
                                                       Dim charsRead As Integer = tempProcess.StandardOutput.ReadBlock(imageBuffer, bytePosition, 16)
                                                       bytePosition += 16
                                                       If charsRead <= 0 Then
                                                           Exit Do
                                                       End If

                                                       While True
                                                           'Read other chunks, while checking for IEND chunk to finish
                                                           Dim identifier As String = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition - 4, 4))
                                                           Dim chunkHeaderBytes() As Byte = System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition - 8, 4)
                                                           Dim chunkSize As Integer = BitConverter.ToUInt32({chunkHeaderBytes(3), chunkHeaderBytes(2), chunkHeaderBytes(1), chunkHeaderBytes(0)}, 0)
                                                           Dim newSize As Integer = bytePosition + chunkSize + 12 - 1
                                                           If imageBuffer.Count <= newSize Then
                                                               'Double the size to avoid rediming too much and wasting resources
                                                               ReDim Preserve imageBuffer(Math.Max(newSize, imageBuffer.Count * 2))
                                                           End If
                                                           Dim nextHeaderSize As Integer = 8
                                                           If identifier.Equals("IEND") Then
                                                               nextHeaderSize = 0
                                                           End If
                                                           'Read chunk, +4 for CRC + 8 for next header
                                                           Dim nextReadSize As Integer = chunkSize + 4 + nextHeaderSize
                                                           charsRead = tempProcess.StandardOutput.ReadBlock(imageBuffer, bytePosition, nextReadSize)
                                                           dispatchedCount += 1
                                                           bytePosition += nextReadSize
                                                           If nextHeaderSize = 0 Then
                                                               Exit While
                                                           End If
                                                       End While

                                                       SyncLock targetCache
                                                           If targetCache(currentFrame).Status = ImageCache.CacheStatus.Cached And Not upgradeImage Then
                                                               'Don't cache stuff we already have cached
                                                           Else
                                                               targetCache(currentFrame).ImageData = System.Text.Encoding.Default.GetBytes(imageBuffer, 0, bytePosition - 2)
                                                           End If

                                                           targetCache(currentFrame).QueueTime = Nothing
                                                           framesRetrieved.Add(currentFrame)

                                                           'If we have grabbed a few frames, it wouldn't hurt to update the UI
                                                           If framesRetrieved.Count > 10 Then
                                                               RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
                                                               framesRetrieved.Clear()
                                                           End If
                                                       End SyncLock
                                                       currentFrame += 1
                                                   Loop
                                               End Sub)


        Dim standardErrTask As Task = Task.Run(Sub()
                                                   Dim lineRead As String = tempProcess.StandardError.ReadLine
                                                   Dim ptsNumerator As Integer = 1
                                                   Dim ptsDenominator As Integer = 1
                                                   Do
                                                       'Read StandardError for the showinfo result for PTS_Time
                                                       If lineRead IsNot Nothing Then
#If DEBUG Then
                                                           fullDataRead.Append(lineRead + vbCrLf)
#End If
                                                           Dim infoMatch As Match = showInfoRegex.Match(lineRead)
                                                           Dim baseMatch As Match = showInfoBaseRegex.Match(lineRead)

                                                           If infoMatch.Success Then
                                                               Dim matchPTSTime As Double = 0
                                                               Dim matchPTS As Integer = -1
                                                               Double.TryParse(infoMatch.Groups("pts_time").Value, matchPTSTime)
                                                               If Integer.TryParse(infoMatch.Groups("pts").Value, matchPTS) Then
                                                                   matchPTSTime = (ptsNumerator * matchPTS) / ptsDenominator
                                                               End If
                                                               Dim matchValue As Integer = Integer.Parse(infoMatch.Groups("index").Value)
                                                               If (matchValue + startFrame) = currentErrorFrame Then
                                                                   SyncLock targetCache
                                                                       Dim existingPTS As Double? = AnyImageCachePTS(currentErrorFrame)
                                                                       If existingPTS IsNot Nothing Then
                                                                           targetCache(currentErrorFrame).PTSTime = existingPTS
                                                                       Else
                                                                           targetCache(currentErrorFrame).PTSTime = Math.Max(0, matchPTSTime)
                                                                       End If
                                                                   End SyncLock
                                                                   currentErrorFrame += 1
                                                               End If
                                                           ElseIf baseMatch.Success Then
                                                               Integer.TryParse(baseMatch.Groups("numerator").Value, ptsNumerator)
                                                               Integer.TryParse(baseMatch.Groups("denominator").Value, ptsDenominator)
                                                           End If
                                                           lineRead = tempProcess.StandardError.ReadLine
                                                           dispatchedCount += 1
                                                       Else
                                                           errEnd = True
                                                       End If
                                                   Loop While Not errEnd
                                               End Sub)

        'Wait for reading to finish
        Dim taskList As New List(Of Task) From {standardOutTask, standardErrTask}
        taskList.RemoveAll(Function(obj) obj Is Nothing)

        Await Task.WhenAll(taskList)
        Debug.Print($"Finished frame grab with {dispatchedCount} dispatches.")

        tempWatch.Stop()
        Debug.Print($"Grabbed frames {startFrame}-{endFrame} in {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")

        'With a video of 14.92fps and 35 total frames, the total returned images ended up being less than expected

        'unmark in case there was an issue
        SyncLock targetCache
            For index As Integer = startFrame To endFrame
                targetCache(index).QueueTime = Nothing
            Next
        End SyncLock
        If framesRetrieved.Count > 0 Then
            RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
        End If

        'CLear up no longer used images (dispose shouldn't happen, just setting to nothing)
        If upgradeImage OrElse targetCache Is mobjTempCache Then
            If mintTempIndex <> frame Then
                mobjTempCache(mintTempIndex).ClearImageData()
            End If
            mintTempIndex = frame
        End If

        Return targetCache(frame)
    End Function

    ''' <summary>
    ''' Immediately polls ffmpeg for the given frame
    ''' </summary>
    Public Function GetFfmpegFrame(ByVal frame As Integer, Optional cacheSize As Integer = 20, Optional frameSize As Size = Nothing, Optional temp As Boolean = False) As CacheItem
        If temp Then
            Return GetFfmpegFrameAsync(frame, cacheSize, frameSize, mobjTempCache).Result
        Else
            Return GetFfmpegFrameAsync(frame, cacheSize, frameSize).Result
        End If
    End Function

    ''' <summary>
    ''' Tells ffmpeg to make files for the given frame(s)
    ''' </summary>
    Public Sub ExportFfmpegFrames(ByVal frameStart As Integer, ByVal frameEnd As Integer, targetFilePath As String, Optional cropRect As Rectangle? = Nothing, Optional rotation As RotateFlipType = RotateFlipType.RotateNoneFlipNone)
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        'processInfo.Arguments = $" -ss {FormatHHMMSSm((frame) / Me.Framerate)}"
        processInfo.Arguments += " -y"
        processInfo.Arguments += Me.InputArgs
        'processInfo.Arguments += " -vf ""select=gte(n\," & frame.ToString & "), scale=228:-1"" -vframes 1 " & """" & targetFilePath & """"
        processInfo.Arguments += $" -vf ""select='between(n,{frameStart},{frameEnd})'"
        If cropRect?.Width > 0 AndAlso cropRect?.Height > 0 AndAlso cropRect?.Center <> New Point(0, 0) Then
            processInfo.Arguments += ($", crop={cropRect?.Width}:{cropRect?.Height}:{cropRect?.X}:{cropRect?.Y}""")
        Else
            processInfo.Arguments += """"
        End If
        If Not rotation = RotateFlipType.RotateNoneFlipNone Then
            processInfo.Arguments += "," + If(rotation = RotateFlipType.Rotate90FlipNone, "transpose=1", If(rotation = RotateFlipType.Rotate180FlipNone, """transpose=2,transpose=2""", If(rotation = RotateFlipType.Rotate270FlipNone, "transpose=2", "")))
        End If
        processInfo.Arguments += $" -vsync 0 ""{targetFilePath}"""

        processInfo.UseShellExecute = True
        processInfo.WindowStyle = ProcessWindowStyle.Hidden
        Exporting = True
        Using fsWatcher As New FileSystemWatcher(System.IO.Path.GetDirectoryName(targetFilePath))
            fsWatcher.EnableRaisingEvents = True
            AddHandler fsWatcher.Created, AddressOf ExportProgress
            AddHandler fsWatcher.Changed, AddressOf ExportProgress
            Dim tempProcess As Process = Process.Start(processInfo)
            TrackProcess(tempProcess)
            tempProcess.WaitForExit()
            'TODO Below is just a dirty way to try and ensure the filewatcher has had enough time to trigger everything
            Task.Run(Sub()
                         Dim timeout As Integer = 3000
                         While Exporting AndAlso timeout > 0
                             timeout -= 100
                             Threading.Thread.Sleep(100)
                         End While
                     End Sub).Wait()
            Exporting = False
            RemoveHandler fsWatcher.Created, AddressOf ExportProgress
            RemoveHandler fsWatcher.Changed, AddressOf ExportProgress
        End Using
    End Sub

    Private Sub ExportProgress(sender As Object, e As FileSystemEventArgs)
        RaiseEvent ExportProgressed(Me, e.FullPath)
    End Sub

    ''' <summary>
    ''' Tells ffmpeg to copy the loaded videos audio stream into a file
    ''' </summary>
    Public Function ExportFfmpegAudioStream(targetFilePath As String, detectedStreamExtension As String) As String
        'ffmpeg -i input-video.avi -vn -acodec copy output-audio.aac
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += Me.InputArgs
        If IO.Path.GetExtension(targetFilePath).Equals(detectedStreamExtension) Then
            processInfo.Arguments += " -vn -acodec copy"
        Else
            processInfo.Arguments += " -vn"
        End If
        processInfo.Arguments += $" ""{targetFilePath}"""

        processInfo.UseShellExecute = True
        processInfo.WindowStyle = ProcessWindowStyle.Hidden
        Dim tempProcess As Process = Process.Start(processInfo)
        tempProcess.WaitForExit(5000) 'Wait up to 5 seconds for the process to finish
        Return targetFilePath
    End Function
#End Region

#Region "Properties"
    ''' <summary>
    ''' Filename, with extension ex: chicken.mp4
    ''' </summary>
    Public ReadOnly Property Name As String
        Get
            Return mobjMetaData.Name
        End Get
    End Property

    ''' <summary>
    ''' Full path to file, including directory and extension ex: C:\Users\Neil\Videos\chicken.mp4
    ''' </summary>
    Public Property FullPath As String
        Get
            Return mobjMetaData.Location + "\" + mobjMetaData.Name
        End Get
        Set(value As String)
            mobjMetaData.Name = IO.Path.GetFileName(value)
            mobjMetaData.Location = IO.Path.GetDirectoryName(value)
        End Set
    End Property

    ''' <summary>
    ''' Crafted data for how ffmpeg should read the file in, with specific codec if needed
    ''' </summary>
    Public ReadOnly Property InputArgs(Optional path As String = "")
        Get
            Return $"{If(Me.FullPath.ToLower.EndsWith(".webm"), " -c:v libvpx-vp9 ", "")} -i """ & If(path.Length > 0, path, Me.FullPath) & """"
        End Get
    End Property

    ''' <summary>
    ''' Width of the video, or horizontal resolution
    ''' </summary>
    Public ReadOnly Property Width As Integer
        Get
            If mobjSizeOverride.HasValue Then
                Return mobjSizeOverride?.Width
            Else
                Return mobjMetaData.VideoStreams(0).Resolution.Width
            End If
        End Get
    End Property

    ''' <summary>
    ''' Height of the video, or horizontal resolution
    ''' </summary>
    Public ReadOnly Property Height As Integer
        Get
            If mobjSizeOverride.HasValue Then
                Return mobjSizeOverride?.Height
            Else
                Return mobjMetaData.VideoStreams(0).Resolution.Height
            End If
        End Get
    End Property

    ''' <summary>
    ''' Resolution of the content
    ''' </summary>
    Public ReadOnly Property Size As Size
        Get
            If mobjSizeOverride.HasValue Then
                Return mobjSizeOverride
            Else
                Return New Size(mobjMetaData.VideoStreams(0).Resolution.Width, mobjMetaData.VideoStreams(0).Resolution.Height)
            End If
        End Get
    End Property

    ''' <summary>
    ''' Sets a variable that will be used for future Size, Height, Width calls
    ''' Should only be used if the stream resolution is incorrect
    Public Sub OverrideResolution(newResolution As Size?)
        mobjSizeOverride = newResolution
    End Sub

    ''' <summary>
    ''' Frames per second of the video, fps or framerate
    ''' </summary>
    Public ReadOnly Property Framerate As Double
        Get
            Return mobjMetaData.VideoStreams(0).Framerate
        End Get
    End Property

    ''' <summary>
    ''' Duration of the video HH:MM:SS.ss
    ''' </summary>
    Public ReadOnly Property Duration As String
        Get
            Return mobjMetaData.Duration
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
            Return mobjMetaData.TotalFrames
        End Get
    End Property

    ''' <summary>
    ''' Used to manually set the value of total frames if ffmpeg reported it incorrectly
    ''' Be aware that many things that have already read the value may need to be changed
    ''' </summary>
    Public Sub OverrideTotalFrames(realFrames As Integer)
        mobjMetaData.TotalFrames = realFrames
        mblnTotalOk = True
    End Sub

    ''' <summary>
    ''' An indicator that the total frames is the correct number, set manually, or automatically through OverrideTotalFrames
    ''' </summary>
    Public Property TotalOk As Boolean
        Get
            Return mblnTotalOk
        End Get
        Set(value As Boolean)
            mblnTotalOk = value
        End Set
    End Property

    ''' <summary>
    ''' The raw stream data given by ffmpeg for stream 0
    ''' </summary>
    Public ReadOnly Property VideoStream As VideoStreamData
        Get
            Return mobjMetaData.VideoStreams?(0)
        End Get
    End Property

    ''' <summary>
    ''' The raw stream data given by ffmpeg for stream 0 audio
    ''' </summary>
    Public ReadOnly Property AudioStream As AudioStreamData
        Get
            Return mobjMetaData.AudioStreams?(0)
        End Get
    End Property

    ''' <summary>
    ''' The stream data given by ffmpeg for subtitle stream 0
    ''' </summary>
    Public ReadOnly Property SubtitleStream As SubtitleStreamData
        Get
            Return mobjMetaData.SubtitleStreams?(0)
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
    ''' Whether or not the scene scores have finished loading for each frame
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ScenesReady As Boolean
        Get
            Return mblnSceneFramesLoaded
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
                If index = Me.mdblSceneFrames.Count - 1 Then
                    'Don't add a carriage return for final line
                    streamWriter.Write(Me.mdblSceneFrames(index))
                Else
                    streamWriter.WriteLine(Me.mdblSceneFrames(index))
                End If
            Next
            streamWriter.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Reads scene frames from a file. Returns false on failure
    ''' </summary>
    Public Function ReadScenesFromFile() As Boolean
        If IO.File.Exists(Me.SceneFramesPath) Then
            Dim allLines As String() = IO.File.ReadAllLines(Me.SceneFramesPath)

            If allLines.Count > 0 Then
                If allLines.Count <> Me.TotalFrames Then
                    Me.OverrideTotalFrames(allLines.Count)
                End If
                ReDim mdblSceneFrames(allLines.Count - 1)
            Else
                Return False
            End If

            For index As Integer = 0 To Me.mdblSceneFrames.Count - 1
                Dim result As Double = 0
                Dim lastLine As String = allLines(index)
                If Double.TryParse(lastLine, result) Then
                    Me.mdblSceneFrames(index) = result
                Else
                    Return False
                End If
            Next
            mblnSceneFramesLoaded = True
            RaiseEvent ProcessedScene(Me, -1)
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Collection of cached images for slightly higher res images
    ''' </summary>
    Public ReadOnly Property ImageFrames As ImageCache
        Get
            Return mobjImageCache
        End Get
    End Property

    ''' <summary>
    ''' Collection of cached images for thumbnails
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
    ''' Whether or not the input file is a type where ffmpeg is going to try and read multiple files (specifically for things like image%d.png
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property InputMash As Boolean
        Get
            Return mblnInputMash
        End Get
    End Property

    ''' <summary>
    ''' Saves scene frames to file so they can be loaded back later without wasting processing power
    ''' </summary>
    Public Sub SaveThumbsToFile()
        mobjThumbCache.SaveToFile(Me.ThumbFramesPath)
    End Sub

    ''' <summary>
    ''' Reads thumbnails from a file. Returns false on failure
    ''' </summary>
    Public Function ReadThumbsFromFile() As Boolean
        If IO.File.Exists(Me.ThumbFramesPath) Then
            RaiseEvent QueuedFrames(Me, mobjThumbCache, New List(Of List(Of Integer))({New List(Of Integer)({0, mobjMetaData.TotalFrames - 1})}))
            mobjThumbCache = ImageCache.ReadFromFile(Me.ThumbFramesPath)

            Dim ranges As New List(Of List(Of Integer))
            Dim lastCached As Integer = mobjThumbCache.Items.ToList.FindLastIndex(Function(obj) obj.Status = CacheStatus.Cached)
            Me.OverrideTotalFrames(lastCached + 1)
            ranges.Add(New List(Of Integer)({0, lastCached})) 'Min and Max range
            RaiseEvent RetrievedFrames(Me, mobjThumbCache, ranges)
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Returns the estimated max file size in bytes, based on bitrate and duration
    ''' </summary>
    Public ReadOnly Property EstimatedFileSize() As Long
        Get
            Dim totalSize As Long = 0
            If mobjMetaData.AudioStreams IsNot Nothing Then
                For Each objStream In mobjMetaData.AudioStreams
                    totalSize += objStream.Bitrate * Me.DurationSeconds
                Next
            End If
            If mobjMetaData.VideoStreams IsNot Nothing Then
                For Each objStream In mobjMetaData.VideoStreams
                    If objStream.Bitrate > 0 Then
                        totalSize += objStream.Bitrate * Me.DurationSeconds
                    End If
                Next
                'Bitmap estimation when all else fails
                If totalSize = 0 Then
                    For Each objStream In mobjMetaData.VideoStreams
                        'Assume 3bytes(24bits) per pixel RGB, aka uncompressed bitmaps
                        totalSize += objStream.Resolution.Width * (objStream.Resolution.Height * 24) / 1024
                    Next
                End If
            End If
            Return totalSize / 8 'bits to Bytes
        End Get
    End Property

    ''' <summary>
    ''' Returns the size in kilobytes as reported by ffmpeg
    ''' </summary>
    Public ReadOnly Property FileSize As Long
        Get
            Return mobjMetaData.VideoSize + mobjMetaData.AudioSize
        End Get
    End Property

    ''' <summary>
    ''' Gets the overall status of all known frames
    ''' Returns Cached if all frames are cached
    ''' Returns Queued if any amount are not yet cached
    ''' Returns None if any amount are not yet queued
    ''' </summary>
    Public ReadOnly Property CacheStatus As ImageCache.CacheStatus
        Get
            Dim targetcache As ImageCache = mobjImageCache
            Dim resultStatus As ImageCache.CacheStatus = ImageCache.CacheStatus.Cached
            For frameIndex As Integer = 0 To mobjMetaData.TotalFrames - 1
                Select Case targetcache(frameIndex).Status
                    Case ImageCache.CacheStatus.Cached
                        'Continue
                    Case ImageCache.CacheStatus.Queued
                        If resultStatus = ImageCache.CacheStatus.Cached Then
                            resultStatus = ImageCache.CacheStatus.Queued
                        End If
                    Case ImageCache.CacheStatus.None
                        Return ImageCache.CacheStatus.None
                End Select
            Next
            Return resultStatus
        End Get
    End Property

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                mobjImageCache.ClearImageCache()
                mobjThumbCache.ClearImageCache()
            End If
        End If
        disposedValue = True
    End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub
#End Region
#End Region

    Public Function ShallowCopy()
        Return Me.MemberwiseClone
    End Function
End Class
