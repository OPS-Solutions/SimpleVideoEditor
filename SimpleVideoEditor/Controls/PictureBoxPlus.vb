Imports System.Drawing.Drawing2D
Imports System.Runtime.CompilerServices

Public Class PictureBoxPlus
    Inherits PictureBox
    Public Property InterpolationMode As InterpolationMode
    ''' <summary>
    ''' Keeps track of whether or not the image currently stored in .Image should be disposed when we are done with it
    ''' </summary>
    Private mblnDisposeLast As Boolean = False

    Protected Overrides Sub OnPaint(pe As PaintEventArgs)
        pe.Graphics.InterpolationMode = Me.InterpolationMode
        pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half
        MyBase.OnPaint(pe)
    End Sub

    ''' <summary>
    ''' Sets the image of a picturebox, ensuring the previous image is disposed
    ''' </summary>
    Public Sub SetImage(newImage As Image, Optional disposeOld As Boolean = False)
        If mblnDisposeLast AndAlso Me.Image IsNot Nothing Then
            Me.Image.Dispose()
        End If
        If newImage Is Nothing Then
            mblnDisposeLast = False
        Else
            mblnDisposeLast = disposeOld
        End If
        If newImage IsNot Nothing Then
            If newImage.Size.FitScale(Me.Size) > 1 Then
                Me.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            Else
                Me.InterpolationMode = Drawing2D.InterpolationMode.Default
            End If
            Me.Image = newImage
        Else
            Me.Image = Nothing
        End If
    End Sub

    Public Sub SetImageData(newData As ImageCache.CacheItem)
        SetImage(newData.GetImage, newData.Disposable)
    End Sub
End Class
