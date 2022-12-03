Imports System.Drawing.Drawing2D

Public Class PictureBoxPlus
    Inherits PictureBox
    Public Property InterpolationMode As InterpolationMode

    Protected Overrides Sub OnPaint(pe As PaintEventArgs)
        pe.Graphics.InterpolationMode = Me.InterpolationMode
        MyBase.OnPaint(pe)
    End Sub
End Class
