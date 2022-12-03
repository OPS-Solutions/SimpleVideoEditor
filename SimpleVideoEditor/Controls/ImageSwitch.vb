Public Class ImageSwitch
	Inherits PictureBox

	Public Event CheckChanged(sender As Object, e As EventArgs)

	Private mimgTrue As Image
	Private mimgFalse As Image
	Private mblnChecked As Boolean

	''' <summary>
	''' Stored image for when the checked state is true
	''' </summary>
	Property TrueImage As Image
		Get
			Return mimgTrue
		End Get
		Set(value As Image)
			mimgTrue = value
			Me.Image = If(mblnChecked, mimgTrue, mimgFalse)
		End Set
	End Property

	''' <summary>
	''' Stored image for when the checked state is false
	''' </summary>
	Property FalseImage As Image
		Get
			Return mimgFalse
		End Get
		Set(value As Image)
			mimgFalse = value
			Me.Image = If(mblnChecked, mimgTrue, mimgFalse)
		End Set
	End Property

	''' <summary>
	''' State of the control
	''' </summary>
	Property Checked As Boolean
		Get
			Return mblnChecked
		End Get
		Set(value As Boolean)
			mblnChecked = value
			Me.Image = If(mblnChecked, mimgTrue, mimgFalse)
			Me.Invalidate()
			RaiseEvent CheckChanged(Me, New EventArgs)
		End Set
	End Property


	''' <summary>
	''' Paints the control graphics
	''' </summary>
	Protected Overrides Sub OnPaint(e As PaintEventArgs)
		MyBase.OnPaint(e)
	End Sub

	''' <summary>
	''' Toggles the control
	''' </summary>
	Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
		MyBase.OnMouseClick(e)
		If Me.Enabled Then
			Checked = Not Checked
			Me.Refresh()
		End If
	End Sub
End Class
