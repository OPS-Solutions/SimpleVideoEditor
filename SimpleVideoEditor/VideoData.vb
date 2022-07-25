Imports System.Text.RegularExpressions
Imports System.Threading

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
        Dim Bitrate As Integer 'kb/s
        Dim VideoStreams As List(Of VideoStreamData)
        Dim AudioStreams As List(Of AudioStreamData)
        Dim TotalFrames As Integer
    End Structure

    Public Class VideoStreamData
        Public ReadOnly Property Raw As String
        Public ReadOnly Property Type As String
        Public ReadOnly Property Resolution As System.Drawing.Size
        Public ReadOnly Property Framerate As Double
        Private Sub New(streamDescription As String)
            _Raw = streamDescription
            Dim resolutionString As String = Regex.Match(streamDescription, "(?<=, )\d*x\d*").Groups(0).Value
            _Resolution = New System.Drawing.Size(Integer.Parse(resolutionString.Split("x")(0)), Integer.Parse(resolutionString.Split("x")(1)))
            'Get framerate from "30.00 fps"
            _Framerate = Double.Parse(Regex.Match(streamDescription, "\d*(\.\d*)? fps").Groups(0).Value.Split(" ")(0))
            _Type = Regex.Match(streamDescription, "stream.*video.*? (?<Type>.*?) .*").Groups("Type").Value
        End Sub
        Public Shared Function FromDescription(streamDescription As String)
            If streamDescription?.Length > 0 Then
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
            Dim audioMatch As Match = Regex.Match(streamDescription, "stream.*audio.*? (?<Type>.*?) .*?(?<SampleRate>\d*) hz, (?<Channel>.*?),.*?(?<Bitrate>\d*) kb\/s.*")
            _Type = audioMatch.Groups("Type").Value
            _SampleRate = Double.Parse(audioMatch.Groups("SampleRate").Value)
            _Channel = audioMatch.Groups("Channel").Value
            _Bitrate = Double.Parse(audioMatch.Groups("Bitrate").Value)
        End Sub

        Public Shared Function FromDescription(streamDescription As String)
            If streamDescription?.Length > 0 Then
                Return New AudioStreamData(streamDescription)
            Else
                Return Nothing
            End If
        End Function
    End Class

    Private mobjMetaData As New MetaData
    Private mdblSceneFrames As Double()


    Private mobjImageCache As ImageCache
    Private mobjThumbCache As ImageCache
    Private mblnInputMash As Boolean 'Data we set manaully saying the type of input we are using is actually a way to specify multiple files

    Private Shared mobjLock As String = "Lock"


    ''' <summary>Event for when some number of frames have been queued for retrieval</summary>
    Public Event QueuedFrames(sender As Object, cache As ImageCache, ranges As List(Of List(Of Integer)))

    ''' <summary>Event for when some number of frames has finished retrieval, and can be accessed</summary>
    Public Event RetrievedFrames(sender As Object, cache As ImageCache, ranges As List(Of List(Of Integer)))


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
        Dim tempProcess As Process = Process.Start(processInfo)

        'Swap output to inside this application
        Dim output As String
        Using streamReader As System.IO.StreamReader = tempProcess.StandardError
            output = streamReader.ReadToEnd()
        End Using
        tempProcess.WaitForExit(1000)
        tempProcess.Close()

        Return New SimpleVideoEditor.VideoData(fullPath, output) With {.mblnInputMash = inputMash}
    End Function

    ''' <summary>
    ''' Parses metadata from a string. Generally given by "ffmpeg -i filename.ext"
    ''' </summary>
    ''' <param name="dataDump"></param>
    Public Sub New(ByVal file As String, ByVal dataDump As String)
        mobjMetaData.Name = IO.Path.GetFileName(file)
        mobjMetaData.Location = IO.Path.GetDirectoryName(file)
        dataDump = dataDump.ToLower
        Dim metaDataIndex As Integer = dataDump.ToLower.IndexOf("metadata:")
        dataDump = dataDump.Substring(metaDataIndex)
        'Get duration
        mobjMetaData.Duration = Regex.Match(dataDump, "(?<=duration: )\d\d:\d\d:\d\d\.\d\d").Groups(0).Value

        'Video Stream Info
        Dim newVideoData As VideoStreamData = VideoStreamData.FromDescription(Regex.Match(dataDump, "stream.*video.*").Groups(0).Value)
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

        Dim frameRateGroups As MatchCollection = Regex.Matches(dataDump, "(?<=frame=)( )*\d*")
        mobjMetaData.TotalFrames = Integer.Parse(frameRateGroups(frameRateGroups.Count - 1).Value.Trim())
        'Failed to get duration, try getting it based on framerate and total frames
        If mobjMetaData.Duration.Length = 0 Then
            mobjMetaData.Duration = FormatHHMMSSm(mobjMetaData.TotalFrames / newVideoData.Framerate)
        End If
        mobjImageCache = New ImageCache(Me.TotalFrames)
        mobjThumbCache = New ImageCache(Me.TotalFrames)
    End Sub


#Region "Data Extraction"
    ''' <summary>
    ''' Gets a list of frames where a scene has changed
    ''' </summary>
    Public Async Function ExtractSceneChanges() As Task(Of Double())
        Dim tempWatch As New Stopwatch
        tempWatch.Start()
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

#If DEBUG Then
        Dim fullDataRead As String = ""
#End If

        Dim sceneValues(mobjMetaData.TotalFrames - 1) As Double
        Dim currentFrame As Integer = 0
        Using recievedStream As New System.IO.MemoryStream
            Dim sceneMatcher As New Regex("(?<=.scene_score=)\d+\.\d+")
            While True
                Dim currentLine As String = Await tempProcess.StandardError.ReadLineAsync()
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
                        currentFrame += 1
                    End If
                End If
#If DEBUG Then
                fullDataRead += currentLine & vbCrLf
#End If
            End While
        End Using
        Me.mdblSceneFrames = sceneValues
        tempWatch.Stop()
        Debug.Print($"Extracted {currentFrame} scene frames in {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")
        Return Me.mdblSceneFrames
    End Function

    Public Async Function ExtractThumbFrames() As Task(Of ImageCache)
        'TODO merge this with scene changes, also maybe ignore it and just grab all frames at full size if the video is short enough
        Await GetFfmpegFrameAsync(0, -1, New Size(0, 10), mobjThumbCache)
        Return mobjThumbCache
    End Function

    Public Function GetImageFromCache(imageIndex As Integer, targetCache As ImageCache) As Bitmap
        If targetCache?.Items.Length > imageIndex AndAlso imageIndex >= 0 Then
            Return targetCache(imageIndex).Image
        Else
            Return Nothing
        End If
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

    Public Function ThumbImageCachePTS(imageIndex As Integer) As Double
        If mobjThumbCache.Item(imageIndex).PTSTime Is Nothing Then
            Return mobjImageCache.Item(imageIndex).PTSTime
        End If
        Return mobjThumbCache.Item(imageIndex).PTSTime
    End Function

    ''' <summary>
    ''' Polls ffmpeg for each given frame, all in the same ffmpeg call
    ''' </summary>
    Public Async Function GetFfmpegFrameRangesAsync(ByVal frames As List(Of Integer), Optional frameSize As Size = Nothing, Optional targetCache As ImageCache = Nothing) As Task(Of Boolean)
        If targetCache Is Nothing Then
            targetCache = mobjImageCache
        End If
        Dim ranges As List(Of List(Of Integer)) = frames.CreateRanges()

        SyncLock targetCache
            For Each objRange In ranges
                targetCache.TryQueue(objRange(0), objRange(1))
            Next
        End SyncLock

        Dim tempWatch As New Stopwatch
        tempWatch.Start()

        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        'processInfo.Arguments += $" -ss {FormatHHMMSSm((startFrame) / Me.Framerate)}"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        If frameSize.Width = 0 AndAlso frameSize.Height = 0 Then
            frameSize.Width = 288
        End If
        'processInfo.Arguments += $" -r {Me.Framerate} -vf scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vframes {cacheTotal} -f image2pipe -vcodec bmp -"
        'FFMPEG expression evaluation https://ffmpeg.org/ffmpeg-utils.html
        Dim rangeExpression As String = ""
        For Each objRange In ranges
            rangeExpression += $"between(n,{objRange(0)},{objRange(1)})+"
        Next
        rangeExpression = rangeExpression.Substring(0, rangeExpression.Length - 1)
        processInfo.Arguments += $" -vf select='{rangeExpression}',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vcodec png -f image2pipe -"
        'processInfo.Arguments += $" -vf select='between(n,{startFrame},{endFrame})*gte(scene,0)',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},metadata=print -vsync 0 -f image2pipe -vcodec bmp -"
        processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True
        processInfo.WindowStyle = ProcessWindowStyle.Hidden

        Using tempProcess As Process = Process.Start(processInfo)
            For Each objRange In ranges
                RaiseEvent QueuedFrames(Me, targetCache, ranges)
            Next

            'Grab each frame as they are output from ffmpeg in real time as fast as possible
            'Don't wait for the entire thing to complete
            'Dim fullText As String = Await tempProcess.StandardOutput.ReadToEndAsync
            'Dim endText As String = Await tempProcess.StandardError.ReadToEndAsync
#If DEBUG Then
            Dim fullDataRead As String = ""
            'Do
            '    Debug.Print(tempProcess.StandardError.ReadLine)
            'Loop While Not tempProcess.StandardError.EndOfStream
#End If
            Dim currentFrame As Integer = frames(0)
            Dim currentErrorFrame As Integer = frames(0)
            Dim showInfoRegex As New Regex("n:\s*(\d*).*pts_time:([\d\.]*)")
            Dim framesRetrieved As New List(Of Integer)
            'Dim frameRegex As New Regex("frame=\s*(\d*)")
            'tempProcess.BeginErrorReadLine()

            While True
                Dim imageBuffer(15) As Char

                'TODO Maybe we have to read progressively on error and output at the same time

                Dim readOutputHeader As Task(Of Integer) = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, 0, 8)
                Dim readOutputChunk As Task(Of Integer) = Nothing
                Dim readError As Task(Of String) = tempProcess.StandardError.ReadLineAsync

                'TODO This seems to work, maybe we can use this method to increase stability
                Dim outputText As String = Nothing
                Dim errorText As String = Nothing
                Dim bytePosition As Integer = 0
                Dim errEnd As Boolean = False
                Dim outEnd As Boolean = False
                Dim gotAll As Boolean = False
                Do
                    If readOutputHeader?.IsCompleted Then
                        If readOutputHeader.Result < 8 Then
                            outEnd = True
                            readOutputHeader = Nothing
                        Else
                            ReDim Preserve imageBuffer(15)
                            If imageBuffer(1) = "P" AndAlso imageBuffer(2) = "N" AndAlso imageBuffer(3) = "G" Then
                                bytePosition = 8
                                readOutputChunk = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, bytePosition, 8)
                            End If
                            readOutputHeader = Nothing
                        End If
                    End If
                    If readOutputChunk?.IsCompleted And Not gotAll Then
                        Dim identifier As String = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition + 4, 4))
                        If identifier.Equals("IEND") Then
                            'IEND
                            gotAll = True
                            bytePosition += 8
                            ReDim Preserve imageBuffer(imageBuffer.Count + 4 - 1)
                            readOutputChunk = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, bytePosition, 4)
                        Else
                            Dim chunkHeaderBytes() As Byte = System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition, 4)
                            Dim chunkSize As Integer = BitConverter.ToUInt32({chunkHeaderBytes(3), chunkHeaderBytes(2), chunkHeaderBytes(1), chunkHeaderBytes(0)}, 0)
                            bytePosition += 8
                            ReDim Preserve imageBuffer(imageBuffer.Count + chunkSize + 12 - 1)
                            readOutputChunk = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, bytePosition, chunkSize + 12)
                            bytePosition += chunkSize + 4
                        End If
                    End If
                    If gotAll AndAlso readOutputChunk?.IsCompleted Then
                        Dim imageBytes(imageBuffer.Count - 1) As Byte
                        imageBytes = System.Text.Encoding.Default.GetBytes(imageBuffer)

                        Using recievedstream As New System.IO.MemoryStream
                            recievedstream.Write(imageBytes, 0, imageBytes.Count)

                            'Ensure 32bppArgb because some code depends on it like autocrop or just getting bytes of the image
                            'We want raw format to be memoryBmp, because otherwise things like lockbits may toss generic GDI+ errors
                            Dim incomingBitmap As Bitmap = New Bitmap(recievedstream)
                            If incomingBitmap.PixelFormat <> Imaging.PixelFormat.Format32bppArgb OrElse Not incomingBitmap.RawFormat.Equals(Imaging.ImageFormat.MemoryBmp) Then
                                Dim newBitmap As New Bitmap(incomingBitmap.Width, incomingBitmap.Height, Imaging.PixelFormat.Format32bppArgb)
                                Using g As Graphics = Graphics.FromImage(newBitmap)
                                    g.DrawImage(incomingBitmap, New Point(0, 0))
                                End Using
                                incomingBitmap.Dispose()
                                incomingBitmap = newBitmap
                            End If

                            targetCache(frames(currentFrame)).Image = incomingBitmap
                            targetCache(frames(Math.Min(currentFrame, currentErrorFrame))).QueueTime = Nothing
                            framesRetrieved.Add(frames(currentFrame))

                            'If we have grabbed a few frames, it wouldn't hurt to update the UI
                            If framesRetrieved.Count > 10 Then
                                RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
                                framesRetrieved.Clear()
                            End If
                        End Using
                        currentFrame += 1
                        ReDim imageBuffer(15)
                        readOutputChunk = Nothing
                        readOutputHeader = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, 0, 8)
                        gotAll = False
                    End If
                    If readError?.IsCompleted Then
                        'Read StandardError for the showinfo result for PTS_Time
                        Dim lineRead As String = readError.Result
                        If lineRead IsNot Nothing Then
#If DEBUG Then
                            fullDataRead += lineRead + vbCrLf
#End If
                            Dim infoMatch As Match = showInfoRegex.Match(lineRead)
                            If infoMatch.Success Then
                                Dim matchPTS As Double = Double.Parse(infoMatch.Groups(2).Value)
                                Dim matchValue As Integer = Integer.Parse(infoMatch.Groups(1).Value)
                                If frames(matchValue) = frames(currentErrorFrame) Then
                                    targetCache(frames(currentErrorFrame)).PTSTime = matchPTS
                                    targetCache(frames(Math.Min(currentFrame, currentErrorFrame))).QueueTime = Nothing
                                    currentErrorFrame += 1
                                End If
                            End If
                            readError = tempProcess.StandardError.ReadLineAsync
                        Else
                            readError = Nothing
                            errEnd = True
                        End If
                    End If
                    'Must check end of stream, because otherwise, reablockasync can potentially hang the application due to the process failing to grab the frame
                    If outEnd AndAlso errEnd Then
                        Exit While
                    End If

                    'Wait for one of them to be finished
                    Dim taskList As New List(Of Task) From {readOutputHeader, readOutputChunk, readError} ', timeoutTask}
                    taskList.RemoveAll(Function(obj) obj Is Nothing)

                    Await Task.WhenAny(taskList)
                Loop
            End While

            tempWatch.Stop()
            For Each objRange In ranges
                Debug.Print($"Grabbed frames {objRange(0)}-{objRange(1)} in {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")

                'unmark in case there was an issue
                For index As Integer = objRange(0) To objRange(1)
                    targetCache(index).QueueTime = Nothing
                Next
            Next
            If framesRetrieved.Count > 0 Then
                RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
            End If
        End Using
        Return True
    End Function

    ''' <summary>
    ''' Polls ffmpeg for the given frame asynchrounously
    ''' </summary>
    Public Async Function GetFfmpegFrameAsync(ByVal frame As Integer, Optional cacheSize As Integer = 20, Optional frameSize As Size = Nothing, Optional targetCache As ImageCache = Nothing) As Task(Of Bitmap)
        If targetCache Is Nothing Then
            targetCache = mobjImageCache
        End If
        If targetCache(frame).Image IsNot Nothing AndAlso cacheSize >= 0 Then
            If cacheSize > 5 Then
                'If we are at the edge of the cached items, try to expand it a little in advance
                If targetCache(Math.Min(frame + 4, Math.Max(0, mobjMetaData.TotalFrames - 4))).Status = ImageCache.CacheStatus.None Then
                    Dim tempTask As Task = Task.Run(Sub()
                                                        Dim seedFrame As Integer = Math.Min(frame + 1, mobjMetaData.TotalFrames - 1)
                                                        If seedFrame = frame Then
                                                            Exit Sub
                                                        End If
                                                        Dim getFrameTask As Task(Of Bitmap) = GetFfmpegFrameAsync(seedFrame, cacheSize, frameSize, targetCache)
                                                    End Sub)
                End If
                If targetCache(Math.Max(0, frame - 4)).Status = ImageCache.CacheStatus.None Then
                    Dim tempTask As Task = Task.Run(Sub()
                                                        Dim seedFrame As Integer = Math.Max(0, frame - 1)
                                                        If seedFrame = frame Then
                                                            Exit Sub
                                                        End If
                                                        Dim getFrameTask As Task(Of Bitmap) = GetFfmpegFrameAsync(seedFrame, cacheSize, frameSize, targetCache)
                                                    End Sub)
                End If
            End If
            Return targetCache(frame).Image
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
                Return targetCache(frame).Image
            End If
            alreadyQueued = (startFrame = endFrame AndAlso targetCache(frame).Status = ImageCache.CacheStatus.Queued)
            Debug.Print($"Working for frames:{startFrame}-{endFrame} (Size:{frameSize.ToString})")
            'Mark images that we are looking for
            If Not alreadyQueued Then
                targetCache.TryQueue(startFrame, endFrame)
            End If
        End SyncLock
        'If you are looking for only one frame, and it is queued, dont waste time trying to grab it again...
        If alreadyQueued Then
            Debug.Print($"Waiting on predicesor call for frame:{frame}")
            Return Await Task.Run(Function()
                                      Do
                                          If Not targetCache(frame).Status = ImageCache.CacheStatus.Queued Then
                                              Return targetCache(frame).Image
                                          Else
                                              Threading.Thread.Sleep(10)
                                          End If
                                      Loop
                                  End Function)
        End If

        Dim cacheTotal As Integer = endFrame - startFrame + 1
        Dim tempWatch As New Stopwatch
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        tempWatch.Start()
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        'processInfo.Arguments += $" -ss {FormatHHMMSSm((startFrame) / Me.Framerate)}"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        If frameSize.Width = 0 AndAlso frameSize.Height = 0 Then
            frameSize.Width = 288
        End If
        'processInfo.Arguments += $" -r {Me.Framerate} -vf scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vframes {cacheTotal} -f image2pipe -vcodec bmp -"
        'FFMPEG expression evaluation https://ffmpeg.org/ffmpeg-utils.html
        processInfo.Arguments += $" -vf select='between(n,{startFrame},{endFrame})',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},showinfo -vsync 0 -vcodec png -f image2pipe -"
        'processInfo.Arguments += $" -vf select='between(n,{startFrame},{endFrame})*gte(scene,0)',scale={If(frameSize.Width = 0, -1, frameSize.Width)}:{If(frameSize.Height = 0, -1, frameSize.Height)},metadata=print -vsync 0 -f image2pipe -vcodec bmp -"
        processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True
        'processInfo.RedirectStandardInput = True
        'processInfo.WindowStyle = ProcessWindowStyle.Hidden

        Using tempProcess As Process = Process.Start(processInfo)
            RaiseEvent QueuedFrames(Me, targetCache, New List(Of List(Of Integer))({New List(Of Integer)({startFrame, endFrame})}))

            'Grab each frame as they are output from ffmpeg in real time as fast as possible
            'Don't wait for the entire thing to complete
            'Dim fullText As String = Await tempProcess.StandardOutput.ReadToEndAsync
            'Dim endText As String = Await tempProcess.StandardError.ReadToEndAsync
#If DEBUG Then
            Dim fullDataRead As String = ""
            'Do
            '    Debug.Print(tempProcess.StandardError.ReadLine)
            'Loop While Not tempProcess.StandardError.EndOfStream
#End If
            Dim currentFrame As Integer = startFrame
            Dim currentErrorFrame As Integer = startFrame
            Dim showInfoRegex As New Regex("n:\s*(\d*).*pts_time:([\d\.]*)")
            Dim framesRetrieved As New List(Of Integer)
            'Dim frameRegex As New Regex("frame=\s*(\d*)")
            'tempProcess.BeginErrorReadLine()

            While True
                Dim imageBuffer(15) As Char

                'TODO Maybe we have to read progressively on error and output at the same time

                Dim readOutputHeader As Task(Of Integer) = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, 0, 8)
                Dim readOutputChunk As Task(Of Integer) = Nothing
                Dim readError As Task(Of String) = tempProcess.StandardError.ReadLineAsync

                'TODO This seems to work, maybe we can use this method to increase stability
                Dim outputText As String = Nothing
                Dim errorText As String = Nothing
                Dim bytePosition As Integer = 0
                Dim errEnd As Boolean = False
                Dim outEnd As Boolean = False
                Dim gotAll As Boolean = False
                Do
                    If readOutputHeader?.IsCompleted Then
                        If readOutputHeader.Result < 8 Then
                            outEnd = True
                            readOutputHeader = Nothing
                        Else
                            ReDim Preserve imageBuffer(15)
                            If imageBuffer(1) = "P" AndAlso imageBuffer(2) = "N" AndAlso imageBuffer(3) = "G" Then
                                bytePosition = 8
                                readOutputChunk = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, bytePosition, 8)
                            End If
                            readOutputHeader = Nothing
                        End If
                    End If
                    If readOutputChunk?.IsCompleted And Not gotAll Then
                        Dim identifier As String = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition + 4, 4))
                        If identifier.Equals("IEND") Then
                            'IEND
                            gotAll = True
                            bytePosition += 8
                            ReDim Preserve imageBuffer(imageBuffer.Count + 4 - 1)
                            readOutputChunk = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, bytePosition, 4)
                        Else
                            Dim chunkHeaderBytes() As Byte = System.Text.Encoding.Default.GetBytes(imageBuffer, bytePosition, 4)
                            Dim chunkSize As Integer = BitConverter.ToUInt32({chunkHeaderBytes(3), chunkHeaderBytes(2), chunkHeaderBytes(1), chunkHeaderBytes(0)}, 0)
                            bytePosition += 8
                            ReDim Preserve imageBuffer(imageBuffer.Count + chunkSize + 12 - 1)
                            readOutputChunk = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, bytePosition, chunkSize + 12)
                            bytePosition += chunkSize + 4
                        End If
                    End If
                    SyncLock targetCache
                        If gotAll AndAlso readOutputChunk?.IsCompleted Then
                            Dim imageBytes(imageBuffer.Count - 1) As Byte
                            imageBytes = System.Text.Encoding.Default.GetBytes(imageBuffer)

                            Using recievedstream As New System.IO.MemoryStream
                                recievedstream.Write(imageBytes, 0, imageBytes.Count)

                                If targetCache(currentFrame).Status = ImageCache.CacheStatus.Cached Then
                                    'Don't cache stuff we already have cached
                                Else
                                    Dim incomingBitmap As Bitmap = New Bitmap(recievedstream)

                                    'Ensure 32bppArgb because some code depends on it like autocrop or just getting bytes of the image
                                    'We want raw format to be memoryBmp, because otherwise things like lockbits may toss generic GDI+ errors
                                    If incomingBitmap.PixelFormat <> Imaging.PixelFormat.Format32bppArgb OrElse Not incomingBitmap.RawFormat.Equals(Imaging.ImageFormat.MemoryBmp) Then
                                        Dim newBitmap As New Bitmap(incomingBitmap.Width, incomingBitmap.Height, Imaging.PixelFormat.Format32bppArgb)
                                        Using g As Graphics = Graphics.FromImage(newBitmap)
                                            g.DrawImage(incomingBitmap, New Point(0, 0))
                                        End Using
                                        incomingBitmap.Dispose()
                                        incomingBitmap = newBitmap
                                    End If
                                    targetCache(currentFrame).Image = incomingBitmap
                                End If

                                targetCache(currentFrame).QueueTime = Nothing
                                framesRetrieved.Add(currentFrame)

                                'If we have grabbed a few frames, it wouldn't hurt to update the UI
                                If framesRetrieved.Count > 10 Then
                                    RaiseEvent RetrievedFrames(Me, targetCache, framesRetrieved.CreateRanges)
                                    framesRetrieved.Clear()
                                End If
                            End Using
                            currentFrame += 1
                            ReDim imageBuffer(15)
                            readOutputChunk = Nothing
                            readOutputHeader = tempProcess.StandardOutput.ReadBlockAsync(imageBuffer, 0, 8)
                            gotAll = False
                        End If
                        If readError?.IsCompleted Then
                            'Read StandardError for the showinfo result for PTS_Time
                            Dim lineRead As String = readError.Result
                            If lineRead IsNot Nothing Then
#If DEBUG Then
                                fullDataRead += lineRead + vbCrLf
#End If
                                Dim infoMatch As Match = showInfoRegex.Match(lineRead)
                                If infoMatch.Success Then
                                    Dim matchPTS As Double = Double.Parse(infoMatch.Groups(2).Value)
                                    Dim matchValue As Integer = Integer.Parse(infoMatch.Groups(1).Value)
                                    If (matchValue + startFrame) = currentErrorFrame Then
                                        targetCache(currentErrorFrame).PTSTime = matchPTS
                                        currentErrorFrame += 1
                                    End If
                                End If
                                readError = tempProcess.StandardError.ReadLineAsync
                            Else
                                readError = Nothing
                                errEnd = True
                            End If
                        End If
                    End SyncLock

                    'Must check end of stream, because otherwise, reablockasync can potentially hang the application due to the process failing to grab the frame
                    If outEnd AndAlso errEnd Then
                        Exit While
                    End If

                    'Wait for one of them to be finished
                    Dim taskList As New List(Of Task) From {readOutputHeader, readOutputChunk, readError} ', timeoutTask}
                    taskList.RemoveAll(Function(obj) obj Is Nothing)

                    Await Task.WhenAny(taskList)
                Loop
            End While

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
        End Using

        Return targetCache(frame).Image
    End Function

    ''' <summary>
    ''' Immediately polls ffmpeg for the given frame
    ''' </summary>
    Public Function GetFfmpegFrame(ByVal frame As Integer, Optional cacheSize As Integer = 20) As Bitmap
        Return GetFfmpegFrameAsync(frame, cacheSize).Result
    End Function

    ''' <summary>
    ''' Tells ffmpeg to make files for the given frame(s)
    ''' </summary>
    Public Sub ExportFfmpegFrames(ByVal frameStart As Integer, ByVal frameEnd As Integer, targetFilePath As String, Optional cropRect As Rectangle? = Nothing)
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        'processInfo.Arguments = $" -ss {FormatHHMMSSm((frame) / Me.Framerate)}"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
        'processInfo.Arguments += " -vf ""select=gte(n\," & frame.ToString & "), scale=228:-1"" -vframes 1 " & """" & targetFilePath & """"
        processInfo.Arguments += $" -vf ""select='between(n,{frameStart},{frameEnd})'"
        If cropRect?.Width > 0 AndAlso cropRect?.Height > 0 AndAlso cropRect?.Center <> New Point(0, 0) Then
            processInfo.Arguments += ($", crop={cropRect?.Width}:{cropRect?.Height}:{cropRect?.X}:{cropRect?.Y}""")
        Else
            processInfo.Arguments += """"
        End If
        processInfo.Arguments += $" -vsync 0 ""{targetFilePath}"""

        processInfo.UseShellExecute = True
        processInfo.WindowStyle = ProcessWindowStyle.Hidden
        Dim tempProcess As Process = Process.Start(processInfo)
        'TODO Make this work indefinitely until user cancels
        If frameEnd - frameStart > 1 Then
            tempProcess.WaitForExit(20000) 'Wait up to 20 seconds for the process to finish
        Else
            tempProcess.WaitForExit(5000) 'Wait up to 5 seconds for the process to finish
        End If
    End Sub

    ''' <summary>
    ''' Tells ffmpeg to copy the loaded videos audio stream into a file
    ''' </summary>
    Public Function ExportFfmpegAudioStream(targetFilePath As String, detectedStreamExtension As String) As String
        'ffmpeg -i input-video.avi -vn -acodec copy output-audio.aac
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += " -i """ & Me.FullPath & """"
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
    Public ReadOnly Property FullPath As String
        Get
            Return mobjMetaData.Location + "\" + mobjMetaData.Name
        End Get
    End Property

    ''' <summary>
    ''' Argument formatted for input to ffmpeg. Normally just -i "FullPath", but when inputMash is in use, prepend with -framerate 20 to ensure all frames are used
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property InputArg As String
        Get
            Return " -framerate 20 -i """ & Me.FullPath & """"
        End Get
    End Property

    ''' <summary>
    ''' Width of the video, or horizontal resolution
    ''' </summary>
    Public ReadOnly Property Width As Integer
        Get
            Return mobjMetaData.VideoStreams(0).Resolution.Width
        End Get
    End Property

    ''' <summary>
    ''' Height of the video, or horizontal resolution
    ''' </summary>
    Public ReadOnly Property Height As Integer
        Get
            Return mobjMetaData.VideoStreams(0).Resolution.Height
        End Get
    End Property

    ''' <summary>
    ''' Resolution of the content
    ''' </summary>
    Public ReadOnly Property Size As Size
        Get
            Return New Size(mobjMetaData.VideoStreams(0).Resolution.Width, mobjMetaData.VideoStreams(0).Resolution.Height)
        End Get
    End Property

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
    End Sub

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
