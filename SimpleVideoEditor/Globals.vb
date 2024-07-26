Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.RegularExpressions

Module Globals
    Public TempPath As String = System.IO.Path.Combine(System.IO.Path.GetTempPath, "SimpleVideoEditor")
    Public CacheFullBitmaps As Boolean = True

#Region "Process Tracking"
    ''' <summary>
    ''' Keeps track of any processes started by this application
    ''' </summary>
    Private mlstProcesses As New List(Of Process)

    ''' <summary>
    ''' Adds a process to track, to be later closed by CloseProcesses in case they don't close themselves
    ''' Use for all processes that may take seconds or longer, to ensure user closing application is not negatively impacted
    ''' </summary>
    Public Sub TrackProcess(objProcess As Process)
        SyncLock mlstProcesses
            mlstProcesses.Add(objProcess)
            PruneProcesses()
        End SyncLock
    End Sub

    ''' <summary>
    ''' Checks the process list for anything that has already exited on its own, forgetting about them
    ''' </summary>
    Private Sub PruneProcesses()
        Dim removableProcesses As New List(Of Process)
        For Each objProcess In mlstProcesses
            If objProcess.HasExited Then
                removableProcesses.Add(objProcess)
            End If
        Next
        For Each removableProcess In removableProcesses
            removableProcess.Close()
            removableProcess.Dispose()
            mlstProcesses.Remove(removableProcess)
        Next
    End Sub

    ''' <summary>
    ''' Closes all tracked processes
    ''' </summary>
    Public Sub CloseProcesses()
        SyncLock mlstProcesses
            PruneProcesses()
            For Each objProcess In mlstProcesses
                objProcess.Kill()
            Next
        End SyncLock
    End Sub
#End Region


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

            Await Task.WhenAll(outText, errText)

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

    ''' <summary>
    ''' Concatenates files using the near instant method, under the assumption they are of the same type
    ''' Blocks until finished
    ''' </summary>
    Public Sub ConcatFiles(files As List(Of String), outPath As String)
        'Generate temporary text file containing the files to concatenate
        'This method avoids a problem I ran into with some ftypisom error, allows audio to get though, and is WAY faster than complex filter. Though I'm not sure about file type support.
        Dim filesBuilder As New StringBuilder()
        For Each objFile In files
            filesBuilder.AppendLine($"file '{objFile}'")
        Next
        Dim textPath As String = Path.Combine(Globals.TempPath, $"concatFiles {Now.ToString("yyyy-MM-dd hh-mm-ss")}.txt")
        File.WriteAllText(textPath, filesBuilder.ToString)
        Dim startInfo As New ProcessStartInfo("ffmpeg.exe", $" -safe 0 -f concat -i ""{textPath}"" -c copy ""{outPath}""")

        'If you add a=1, you get audo concatenation, but then it seems stuff without audio can't concatenate
        'Dim startInfo As New ProcessStartInfo("ffmpeg.exe", argInputs + $" -filter_complex ""concat=n={args.Count - 1}:v=1:a=1"" ""{outPath}""")
        'Dim startInfo As New ProcessStartInfo("ffmpeg.exe", argInputs + $" -filter_complex ""concat=n={args.Count - 1}"" ""{outPath}""")
        'Dim manualEntryForm As New ManualEntryForm(startInfo.Arguments)
        'Select Case manualEntryForm.ShowDialog()
        '    Case DialogResult.Cancel
        '        Return Nothing
        'End Select
        'startInfo.Arguments = ManualEntryForm.ModifiedText

        Dim concatProcess As Process = Process.Start(startInfo)
        concatProcess.WaitForExit()
    End Sub
End Module
