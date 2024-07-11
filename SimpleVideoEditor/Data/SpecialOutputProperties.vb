Imports System.Text.RegularExpressions

Public Class SpecialOutputProperties
    Implements ICloneable

    Public Decimate As Boolean
    Public FPS As Double
    Public ColorKey As Color
    Public PlaybackSpeed As Double = 1
    Public PlaybackVolume As Double = 1
    Public QScale As Double
    Public Rotation As System.Drawing.RotateFlipType = RotateFlipType.RotateNoneFlipNone 'Keeps track of how the user wants to rotate the image
    Public Subtitles As String = ""
    Public BakeSubs As Boolean = True
    Public Trim As TrimData = Nothing
    Public Crop As Rectangle?
    Public VerticalResolution As Integer
    ''' <summary>
    ''' Angle of rotation in degrees
    ''' </summary>
    ''' <returns></returns>
    Public Property RotationAngle As Integer
        Get
            Select Case Rotation
                Case RotateFlipType.RotateNoneFlipNone
                    Return 0
                Case RotateFlipType.Rotate90FlipNone
                    Return 90
                Case RotateFlipType.Rotate180FlipNone
                    Return 180
                Case RotateFlipType.Rotate270FlipNone
                    Return 270
                Case Else
                    Return 0
            End Select
        End Get
        Set(value As Integer)
            Select Case value
                Case 0
                    Rotation = RotateFlipType.RotateNoneFlipNone
                Case 90
                    Rotation = RotateFlipType.Rotate90FlipNone
                Case 180
                    Rotation = RotateFlipType.Rotate180FlipNone
                Case 270
                    Rotation = RotateFlipType.Rotate270FlipNone
                Case Else
                    Rotation = 0
            End Select
        End Set
    End Property

    ''' <summary>
    ''' Sets the vertical resolution based on a string like 120p, setting to 0 otherwise
    ''' </summary>
    ''' <param name="resolutionString"></param>
    Public Sub SetResolution(resolutionString As String)
        Dim resMatch As Match = Regex.Match(resolutionString, "\d+")
        If resMatch.Success Then
            VerticalResolution = Integer.Parse(resMatch.Value)
        Else
            VerticalResolution = 0
        End If
    End Sub


    Public Function Clone() As Object Implements ICloneable.Clone
        Return Me.MemberwiseClone
    End Function
End Class

Public Class TrimData
    Public StartFrame As Integer
    Public EndFrame As Integer
    Public StartPTS As Decimal
    Public EndPTS As Decimal
End Class