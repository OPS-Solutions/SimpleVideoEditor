Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.RegularExpressions

Module Globals
    Public TempPath As String = System.IO.Path.Combine(System.IO.Path.GetTempPath, "SimpleVideoEditor")
    Public CacheFullBitmaps As Boolean = True

    ''' <summary>
    ''' Returns an .srt filepath for use by this process in the temp folder
    ''' Uniquely ID'ed by ProcessID
    ''' </summary>
    Public Function GetTempSrt() As String
        Return IO.Path.Combine(Globals.TempPath, $"tempSubs{Process.GetCurrentProcess.Id}.srt")
    End Function


    ''' <summary>
    ''' Recursively deletes directories in as safe a manner as possible.
    ''' </summary>
    Public Sub DeleteDirectory(ByVal directoryPath As String)
        If System.IO.Directory.Exists(directoryPath) Then
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
        End If
    End Sub

    ''' <summary>
    ''' Add a little bit of text to the end of a file name string between its extension like "-temp" or "-SHINY".
    ''' </summary>
    Public Function FileNameAppend(ByVal fullPath As String, ByVal newEnd As String)
        Return System.IO.Path.GetDirectoryName(fullPath) & "\" & System.IO.Path.GetFileNameWithoutExtension(fullPath) & newEnd & System.IO.Path.GetExtension(fullPath)
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Function GetKeyState(ByVal nVirtKey As Integer) As Short
    End Function

    ''' <summary>
    ''' Polls ffmpeg for the given frame asynchrounously
    ''' </summary>
    Public Async Function GetFfmpegMuxers() As Task(Of List(Of String))
        Dim muxerNames As New List(Of String)
        Dim tempWatch As New Stopwatch
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        tempWatch.Start()
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += " -muxers"

        processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True

        Using tempProcess As Process = Process.Start(processInfo)
            Dim outText As Task(Of String) = tempProcess.StandardOutput.ReadToEndAsync
            Dim errText As Task(Of String) = tempProcess.StandardError.ReadToEndAsync

            Task.WaitAll(outText, errText)

            Dim muxMatcher As New Regex("^\s*E (?<muxer>\w*)")
            For Each objLine In outText.Result.Split(vbLf)
                Dim muxMatch As Match = muxMatcher.Match(objLine.Trim)
                If muxMatch.Success Then
                    muxerNames.Add(muxMatch.Groups("muxer").Value)
                End If
            Next
            Dim muxerExtensions As New List(Of String)
            For Each objName In muxerNames
                muxerExtensions.Add(Await GetMuxerInfo(objName))
            Next
            tempWatch.Stop()
            Debug.Print($"Grabbed ffmpeg muxers in {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")
        End Using
        Return muxerNames
    End Function

    Public Async Function GetMuxerInfo(muxerName As String) As Task(Of String)
        Dim commonExtension As String = Nothing
        Dim tempWatch As New Stopwatch
        'ffmpeg -i video.mp4 -vf "select=gte(n\,100), scale=800:-1" -vframes 1 image.jpg
        tempWatch.Start()
        Dim processInfo As New ProcessStartInfo
        processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"
        processInfo.Arguments += $" -h muxer={muxerName}"

        processInfo.UseShellExecute = False
        processInfo.CreateNoWindow = True
        processInfo.RedirectStandardOutput = True
        processInfo.RedirectStandardError = True

        Using tempProcess As Process = Process.Start(processInfo)
            Dim outText As Task(Of String) = tempProcess.StandardOutput.ReadToEndAsync
            Dim errText As Task(Of String) = tempProcess.StandardError.ReadToEndAsync

            Task.WaitAll(outText, errText)

            Dim muxMatcher As New Regex("extensions: (?<extension>\w*)")
            For Each objLine In outText.Result.Split(vbLf)
                Dim muxMatch As Match = muxMatcher.Match(objLine.Trim)
                If muxMatch.Success Then
                    commonExtension = muxMatch.Groups("extension").Value
                End If
            Next

            tempWatch.Stop()
            Debug.Print($"Grabbed ffmpeg muxer info for {muxerName} in {tempWatch.ElapsedTicks} ticks. ({tempWatch.ElapsedMilliseconds}ms)")
        End Using
        Return commonExtension
    End Function
End Module
