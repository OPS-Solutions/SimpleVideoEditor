Public Class VideoSeeker
	Inherits UserControl
	Public Event RangeChanged(ByVal newVal As Integer, ByVal ChangeMin As Boolean)

	Private pRangeMinValue As Integer = 0 'Value that should only be accessed by the property below
	''' <summary>
	''' Current value of the leftmost slider on the control
	''' </summary>
	Property RangeMinValue As Integer
		Get
			Return pRangeMinValue
		End Get
		Set(value As Integer)
			pRangeMinValue = Math.Max(value, mRangeMin)
			If pRangeMinValue >= RangeMaxValue Then
				RangeMaxValue = pRangeMinValue + 1
				If RangeMaxValue = pRangeMinValue Then
					pRangeMinValue = RangeMaxValue - 1
				End If
			End If
			RaiseEvent RangeChanged(pRangeMinValue, True)
		End Set
	End Property

	Private pRangeMaxValue As Integer = 100 'Value that should only be accessed by the property below

	''' <summary>
	''' Current value of the rightmost slider on the control
	''' </summary>
	Property RangeMaxValue As Integer
		Get
			Return pRangeMaxValue
		End Get
		Set(value As Integer)
			pRangeMaxValue = Math.Min(value, mRangeMax)
			If pRangeMaxValue <= RangeMinValue Then
				RangeMinValue = pRangeMaxValue - 1
				If RangeMinValue = pRangeMaxValue Then
					pRangeMaxValue = RangeMinValue + 1
				End If
			End If
			RaiseEvent RangeChanged(pRangeMaxValue, False)
		End Set
	End Property

	''' <summary>
	''' Minimum limit for the slider. Starts at 0
	''' </summary>
	Public Property RangeMin As Integer
		Get
			Return mRangeMin
		End Get
		Set(value As Integer)
			mRangeMin = value
		End Set
	End Property
	Private mRangeMin As Integer = 0

	''' <summary>
	''' Maximum limit for the slider. Starts at 100
	''' </summary>
	Public Property RangeMax As Integer
		Get
			Return mRangeMax
		End Get
		Set(value As Integer)
			mRangeMax = value
		End Set
	End Property
	Private mRangeMax As Integer = 100

	''' <summary>
	''' Storage for the last range values for the control
	''' </summary>
	Public Property RangeValues As Integer()
		Get
			Return mRangeValues
		End Get
		Set(value As Integer())
			mRangeValues = value
		End Set
	End Property

	Private mRangeValues As Integer() = {0, 100}

	Private marySceneChanges() As Double
	''' <summary>
	''' Special marks will be shown for each of these frames
	''' </summary>\
	Public Property SceneFrames As Double()
		Get
			Return marySceneChanges
		End Get
		Set(value As Double())
			marySceneChanges = value
			Me.Refresh()
		End Set
	End Property

	''' <summary>
	''' Paints over the control with custom dual trackbar looking graphics.
	''' </summary>
	Protected Overrides Sub OnPaint(e As PaintEventArgs)
		'MyBase.OnPaint(e)
		Dim numberOfTicks As Integer = Math.Min(Me.Width \ 2, mRangeMax - mRangeMin)
		Dim distanceBetweenPoints As Double = Me.Width / numberOfTicks
		Dim fullrange As Integer = mRangeMax - mRangeMin
		'Draw background
		e.Graphics.DrawRectangle(New Pen(If(Me.Enabled, Color.Green, Color.Gray), 6), CType((RangeMinValue * ((Me.Width - 1) / fullrange)) + 3, Integer), 6, CType(((RangeMaxValue - RangeMinValue) * ((Me.Width - 1) / fullrange)) - 6, Integer), Me.Height - 12)
		Dim colorHeight As Integer = 12
		'Draw scene changes
		Using pen As New Pen(Color.DarkSeaGreen, 1)
			If marySceneChanges IsNot Nothing Then
				Dim frameIndex As Integer = 0
				For Each sceneChange As Single In marySceneChanges
					e.Graphics.DrawLine(pen, New Point(frameIndex, Me.Height - 4), New Point(frameIndex, (Me.Height - 4) - (sceneChange * colorHeight)))
					frameIndex += 1
				Next
			End If
		End Using

		Using pen As New Pen(Color.Black, 1)
			'Draw ticks
			For index As Integer = 0 To numberOfTicks - 1
				e.Graphics.DrawLine(pen, New Point(index * distanceBetweenPoints, Me.Height), New Point(index * distanceBetweenPoints, Me.Height - 2))
			Next
			'Draw current points
			e.Graphics.DrawLine(pen, New Point((RangeMinValue * ((Me.Width - 1) / fullrange)), Me.Height - 3), New Point((RangeMinValue * ((Me.Width - 1) / fullrange)), 0))
			e.Graphics.DrawLine(pen, New Point((RangeMaxValue * ((Me.Width - 1) / fullrange)), Me.Height - 3), New Point((RangeMaxValue * ((Me.Width - 1) / fullrange)), 0))
		End Using
	End Sub

	''' <summary>
	''' Searches for the nearest slider in the control, and selects it for use.
	''' </summary>
	Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
		MyBase.OnMouseDown(e)
		'Convert mouse coordinates to increments, Grab closest slider
		If Me.Enabled Then
			Dim newValue As Integer = ((mRangeMax - mRangeMin) / Me.Width) * e.X
			If Math.Abs(newValue - RangeMinValue) < Math.Abs(newValue - RangeMaxValue) Then
				RangeSliderMinSelected = True
			Else
				RangeSliderMaxSelected = True
			End If
			Me.OnMouseMove(e)
			Me.Refresh()
		End If
	End Sub


	Private RangeSliderMinSelected As Boolean = False
	Private RangeSliderMaxSelected As Boolean = False

	''' <summary>
	''' Changes the corresponding range values for the sliders in the control.
	''' </summary>
	Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
		MyBase.OnMouseMove(e)
		If Me.Enabled Then
			If e.Button = Windows.Forms.MouseButtons.Left Then
				'Move range sliders
				Dim newValue As Integer = ((mRangeMax - mRangeMin) / Me.Width) * e.X
				If RangeSliderMinSelected Then
					RangeMinValue = newValue
				ElseIf RangeSliderMaxSelected Then
					RangeMaxValue = newValue
				End If
				Me.Refresh()
			End If
		End If
	End Sub

	''' <summary>
	''' Updates the image only when mouse up occurs so your CPU doesn't get burned up with too many reads/writes.
	''' </summary>
	Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
		MyBase.OnMouseUp(e)
		If Me.Enabled Then
			RangeSliderMinSelected = False
			RangeSliderMaxSelected = False
			Dim minChanged As Boolean = Not RangeMinValue = mRangeValues(0)
			mRangeValues(0) = RangeMinValue
			Dim maxChanged As Boolean = Not RangeMaxValue = mRangeValues(1)
			mRangeValues(1) = RangeMaxValue
			If minChanged Or maxChanged Then
				RaiseEvent RangeChanged(If(minChanged, RangeMinValue, RangeMaxValue), False)
				Me.Refresh()
			End If
		End If
	End Sub
End Class
