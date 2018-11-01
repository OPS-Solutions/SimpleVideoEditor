''' <summary>
''' A form for modification of a string
''' </summary>
Public Class ManualEntryForm
	Public ModifiedText As String = ""
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
				Me.Close()
			Case Keys.Return
				Me.Close()
		End Select
	End Sub
End Class