Imports System.Text.RegularExpressions

Public Class VideoData

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
            mobjMetaData.duration = MainForm.FormatHHMMSSm(mobjMetaData.totalFrames / newVideoData.framerate)
        End If
    End Sub

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
            Dim dataRead As String = Await tempProcess.StandardError.ReadToEndAsync()
            Dim matches As MatchCollection = Regex.Matches(dataRead, "(?<=.scene_score=)\d+\.\d+")
            Dim frameIndex As Integer = 0
            For Each sceneString As Match In matches
                sceneValues(frameIndex) = Double.Parse(sceneString.Value)
                frameIndex += 1
            Next
        End Using
        Me.mdblSceneFrames = sceneValues
        Return Me.mdblSceneFrames
    End Function

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
    Public Sub SaveSceneseToFile()
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
#End Region

End Class
