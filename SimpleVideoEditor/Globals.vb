Imports System.Runtime.InteropServices

Module Globals
    Public TempPath As String = System.IO.Path.Combine(System.IO.Path.GetTempPath, "SimpleVideoEditor")

    ''' <summary>
    ''' Recursively deletes directories in as safe a manner as possible.
    ''' </summary>
    Public Sub DeleteDirectory(ByVal directoryPath As String)
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
    ''' Add a little bit of text to the end of a file name string between its extension like "-temp" or "-SHINY".
    ''' </summary>
    Public Function FileNameAppend(ByVal fullPath As String, ByVal newEnd As String)
        Return System.IO.Path.GetDirectoryName(fullPath) & "\" & System.IO.Path.GetFileNameWithoutExtension(fullPath) & newEnd & System.IO.Path.GetExtension(fullPath)
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Function GetKeyState(ByVal nVirtKey As Integer) As Short
    End Function
End Module
