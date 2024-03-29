﻿''' <summary>
''' A form for modification of a string
''' </summary>
Public Class ManualEntryForm
    Public ModifiedText As String = ""

    Public Property Persistent As Boolean = False

    Public Sub New(ByRef text As String)
		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		ModifiedText = text
		txtConsole.Text = text
	End Sub
	Private Sub txtConsole_PreviewKeyDown(sender As Object, e As PreviewKeyDownEventArgs) Handles txtConsole.PreviewKeyDown
		ModifiedText = txtConsole.Text
		Select Case e.KeyCode
            Case Keys.Enter
                If Not My.Computer.Keyboard.ShiftKeyDown Then
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                End If
            Case Keys.Return
                If Not My.Computer.Keyboard.ShiftKeyDown Then
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                End If
            Case Keys.Escape
                Me.DialogResult = DialogResult.Cancel
				Me.Close()
		End Select
	End Sub

    Private Sub txtConsole_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtConsole.KeyPress
        'Manually implement select all because multiline texboxes for some reason disable this functionality
        Select Case e.KeyChar
            Case ChrW(1)
                If My.Computer.Keyboard.CtrlKeyDown Then
                    txtConsole.SelectAll()
                End If
        End Select
    End Sub

    ''' <summary>
    ''' Sets the console textbox to display the current text, scrolled to the bottom
    ''' </summary>
    Public Sub SetText(newText As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() SetText(newText))
        Else
            txtConsole.Text = newText
            txtConsole.SelectionStart = newText.Length
            txtConsole.ScrollToCaret()
        End If
    End Sub

    Private Sub ManualEntryForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Persistent AndAlso e.CloseReason = CloseReason.UserClosing Then
            Me.Hide()
            e.Cancel = True
        End If
    End Sub
End Class