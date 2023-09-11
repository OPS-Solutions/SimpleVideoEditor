﻿Imports System.Runtime.InteropServices

Module Globals
    Public TempPath As String = System.IO.Path.Combine(System.IO.Path.GetTempPath, "SimpleVideoEditor")

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
End Module
