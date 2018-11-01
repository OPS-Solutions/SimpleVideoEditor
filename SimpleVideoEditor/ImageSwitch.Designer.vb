<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ImageSwitch
	Inherits System.Windows.Forms.PictureBox

	'UserControl overrides dispose to clean up the component list.
	<System.Diagnostics.DebuggerNonUserCode()>
	Protected Overrides Sub Dispose(ByVal disposing As Boolean)
		Try
			If disposing AndAlso components IsNot Nothing Then
				components.Dispose()
			End If
		Finally
			MyBase.Dispose(disposing)
		End Try
	End Sub

	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer

	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.  
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()>
	Private Sub InitializeComponent()
		Me.SuspendLayout()
		'
		'ImageSwitch
		'
		Me.Name = "ImageSwitch"
		Me.Size = New System.Drawing.Size(16, 16)
		Me.ResumeLayout(False)
	End Sub
End Class
