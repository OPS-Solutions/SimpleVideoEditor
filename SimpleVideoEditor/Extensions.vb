Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Shell32

Module Extensions
    ''' <summary>
    ''' Checks that a given value is equal to another within a given margen of error
    ''' </summary>
    <Extension>
    Public Function EqualsWithin(value1 As Double, value2 As Double, margin As Double)
        Return value2 <= value1 + margin AndAlso value2 >= value1 - margin
    End Function

    ''' <summary>
    ''' Fills a range of values of an array with a given value
    ''' </summary>
    <Extension>
    Public Sub FillRange(array As Double(), value As Double, start As Integer, length As Integer)
        For index As Integer = start To start + length - 1
            array(index) = value
        Next
    End Sub

    ''' <summary>
    ''' Distance from a point to the center of the given rectangle
    ''' </summary>
    <Extension>
    Public Function DistanceToCenter(rect As RectangleF, location As Point) As Single
        Dim rectCenterX As Single = (rect.X + rect.Width / 2)
        Dim rectCenterY As Single = (rect.Y + rect.Height / 2)
        Return Math.Sqrt(Math.Pow(location.X - rectCenterX, 2) + Math.Pow(location.Y - rectCenterY, 2))
    End Function

    ''' <summary>
    ''' Distance from a point to the center of the given rectangle
    ''' </summary>
    <Extension>
    Public Function DistanceTo(pt1 As Point, pt2 As Point) As Single
        Return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2))
    End Function

    ''' <summary>
    ''' Compares two byte arrays(must be of same length), and returns their average difference between bytes with a value between 0 and 1
    ''' </summary>
    <Extension>
    Public Function CompareArraysAvg(array1 As Byte(), array2 As Byte()) As Double
        If array1.Count <> array2.Count Then
            Return False
        End If

        Dim difSum As Double = 0
        For index As Integer = 0 To array1.Count - 1
            difSum += Math.Abs(CType(array1(index), Integer) - array2(index)) / 255
        Next
        Dim difAvg As Double = difSum / array1.Count
        Return difAvg
    End Function

    ''' <summary>
    ''' Compares two double arrays(must be of same length), and returns their average difference
    ''' </summary>
    <Extension>
    Public Function CompareArrays(array1 As Double(), array2 As Double()) As Double
        If array1.Count <> array2.Count Then
            Return False
        End If

        Dim difSum As Double = 0
        For index As Integer = 0 To array1.Count - 1
            difSum += Math.Abs(array1(index) - array2(index))
        Next
        Dim difAvg As Double = difSum / array1.Count
        Return difAvg
    End Function

    ''' <summary>
    ''' Converts a double like 100.5 seconds to HHMMSSm... like "00:01:40.5"
    ''' </summary>
    Public Function FormatHHMMSSm(ByVal totalSS As Decimal) As String
        Dim hours As Double = ((totalSS / 60) / 60)
        Dim minutes As Double = (hours Mod 1) * 60
        Dim seconds As Double = (minutes Mod 1) * 60
        Dim millisecond As Double = (totalSS Mod 1) 'Math.Round(totalSS Mod 1, 2, MidpointRounding.AwayFromZero) * 100
        Dim hrString As String = Math.Truncate(hours).ToString.PadLeft(2, "0")
        Dim minString As String = Math.Truncate(minutes).ToString.PadLeft(2, "0")
        Dim secString As String = Math.Truncate(seconds).ToString.PadLeft(2, "0")
        Dim milliString As String = millisecond.ToString("R")
        If milliString.Length > 2 Then
            milliString = milliString.Substring(2)
        End If
        Return $"{hrString}:{minString}:{secString}.{milliString}"
    End Function

    ''' <summary>
    ''' Converts a time like "00:01:40.5" to the total number of seconds
    ''' </summary>
    ''' <param name="duration"></param>
    ''' <returns></returns>
    Public Function HHMMSSssToSeconds(ByVal duration As String) As Double
        Dim totalSeconds As Double = 0
        totalSeconds += Integer.Parse(duration.Substring(0, 2)) * 60 * 60 'Hours
        totalSeconds += Integer.Parse(duration.Substring(3, 2)) * 60 'Minutes
        totalSeconds += Integer.Parse(duration.Substring(6, 2)) 'Seconds
        totalSeconds += Integer.Parse(duration.Substring(9, 2)) / 100.0 'Milliseconds
        Return totalSeconds
    End Function

    ''' <summary>
    ''' Takes a number like 123 and forces it to be divisible by 2, either by returning 122 or 124
    ''' </summary>
    Public Function ForceEven(ByVal number As Integer, Optional ByVal forceDown As Boolean = True) As String
        If number \ 2 = (number - 1) \ 2 Then
            Return number + If(forceDown, -1, 1)
        Else
            Return number
        End If
    End Function

    <DllImport("shell32.dll", EntryPoint:="SHOpenFolderAndSelectItems")>
    Private Function SHOpenFolderAndSelectItems(<[In]> pidlFolder As IntPtr, cidl As UInteger, <[In], [Optional]> apidl As IntPtr, dwFlags As Integer) As Integer
    End Function

    <DllImport("shell32.dll", CharSet:=CharSet.Unicode)>
    Public Function ILCreateFromPath(<[In], MarshalAs(UnmanagedType.LPWStr)> pszPath As String) As IntPtr
    End Function

    <DllImport("shell32.dll")>
    Public Sub ILFree(<[In]> pidl As IntPtr)
    End Sub

    ''' <summary>
    ''' Opens a file explorer window targeting the given filepath. Will use an existing explorer window of the same folder and retarget if one exists.
    ''' </summary>
    ''' <param name="filePath"></param>
    Public Sub OpenOrFocusFile(ByVal filePath As String)
        Dim idStructure As Object = ILCreateFromPath(filePath)
        If idStructure <> IntPtr.Zero Then
            SHOpenFolderAndSelectItems(idStructure, 0, 0, 0)
            ILFree(idStructure)
        End If
    End Sub

    ''' <summary>
    ''' Creates a list of ranges (Min,Max) for the given list of integers
    ''' </summary>
    <Extension>
    Public Function CreateRanges(numbers As List(Of Integer)) As List(Of List(Of Integer))
        'Sanitize frames list to be in order
        numbers.Sort()
        'Extract continuous ranges like 4-17, 21-25
        Dim ranges As New List(Of List(Of Integer))
        ranges.Add(New List(Of Integer)({numbers(0), numbers(0)})) 'Min and Max range
        Dim rangeIndex As Integer = 0
        For Each intValue In numbers
            If intValue = ranges(rangeIndex)(1) + 1 Then
                ranges(rangeIndex)(1) = intValue
            ElseIf intValue > ranges(rangeIndex)(1) + 1 Then
                ranges.Add(New List(Of Integer)({intValue, intValue}))
                rangeIndex += 1
            End If
        Next
        Return ranges
    End Function
End Module
