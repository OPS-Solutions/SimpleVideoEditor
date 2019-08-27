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
			If pRangeMinValue > RangeMaxValue Then
				RangeMaxValue = pRangeMinValue
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
			If pRangeMaxValue < RangeMinValue Then
				RangeMinValue = pRangeMaxValue
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

	Private mdblSceneChanges() As Double
	''' <summary>
	''' Special marks will be shown for each of these frames
	''' </summary>\
	Public Property SceneFrames As Double()
		Get
			Return mdblSceneChanges
		End Get
		Set(value As Double())
			mdblSceneChanges = value
			If mdblSceneChanges?.Count > 0 Then
				Dim scale As Double = 1
				Dim max As Double = mdblSceneChanges.Max()
				If max > 0 Then
					scale = 1 / mdblSceneChanges.Max()
					'Scale so that we always try to show something
					For index As Integer = 0 To mdblSceneChanges.Count - 1
						mdblSceneChanges(index) *= scale
					Next
				End If
			End If

			Me.Invalidate()
		End Set
	End Property

	''' <summary>
	''' Paints over the control with custom dual trackbar looking graphics.
	''' </summary>
	Protected Overrides Sub OnPaint(e As PaintEventArgs)
		'MyBase.OnPaint(e)
		Dim numberOfTicks As Integer = Math.Min((Me.Width - 1) \ 2, mRangeMax - mRangeMin + 2) 'Tick represents start or end of a frame, number of frames + 1
		Dim distanceBetweenPoints As Double = (Me.Width - 1) / (numberOfTicks - 1)
		Dim fullrange As Integer = mRangeMax - mRangeMin
		Dim leftSeek As Single = ((RangeMinValue / fullrange) * (Me.Width - 1))
		Dim rightSeek As Single = (((RangeMaxValue + 1) / fullrange) * (Me.Width - 1))
		'Draw background
		Using contentBrush As New SolidBrush(If(Me.Enabled, Color.Green, Color.Gray))
			e.Graphics.FillRectangle(contentBrush, leftSeek, 3, rightSeek - leftSeek, Me.Height - 6)
		End Using
		Dim colorHeight As Integer = 12
		'Draw scene changes
		Using pen As New Pen(Color.DarkSeaGreen, 1)
			If mdblSceneChanges IsNot Nothing Then
				Dim frameIndex As Integer = 0
				For Each sceneChange As Single In mdblSceneChanges
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
			e.Graphics.DrawLine(pen, New Point(leftSeek, Me.Height - 3), New Point(leftSeek, 0))
			e.Graphics.DrawLine(pen, New Point(rightSeek, Me.Height - 3), New Point(rightSeek, 0))
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
			Me.Invalidate()
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
					If Not RangeMinValue = newValue Then
						RangeMinValue = newValue
						Me.Invalidate()
					End If
				ElseIf RangeSliderMaxSelected Then
					If Not RangeMaxValue = newValue Then
						RangeMaxValue = newValue
						Me.Invalidate()
					End If
				End If
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
				Me.Invalidate()
			End If
		End If
	End Sub
End Class
