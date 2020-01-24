Imports System.Runtime.CompilerServices

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
    ''' Converts a double like 100.5 seconds to HHMMSSm... like "00:01:40.5"
    ''' </summary>
    Public Function FormatHHMMSSm(ByVal totalSS As Double) As String
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
End Module
