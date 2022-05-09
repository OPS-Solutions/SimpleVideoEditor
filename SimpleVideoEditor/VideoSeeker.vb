Public Class VideoSeeker
    Inherits UserControl
    Public Event SeekChanged(ByVal newVal As Integer)

    Private pRangeMinValue As Integer = 0 'Value that should only be accessed by the property below
    Private mobjPreviewForm As Form
    Private mintHoveredFrameIndex As Integer? = Nothing

    ''' <summary>
    ''' Current value of the leftmost slider on the control
    ''' </summary>
    Property RangeMinValue As Integer
        Get
            Return pRangeMinValue
        End Get
        Set(value As Integer)
            pRangeMinValue = Math.Min(Math.Max(value, mRangeMin), RangeMax)
            If pRangeMinValue > RangeMaxValue Then
                RangeMaxValue = pRangeMinValue
            End If
            PreviewLocation = pRangeMinValue
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
            pRangeMaxValue = Math.Max(0, Math.Min(value, mRangeMax))
            If pRangeMaxValue < RangeMinValue Then
                RangeMinValue = pRangeMaxValue
            End If
            PreviewLocation = pRangeMaxValue
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
    ''' Movable marker position for what frame should be displayed
    ''' </summary>
    Public Property PreviewLocation As Integer
        Get
            Return mintPreviewLocation
        End Get
        Set(value As Integer)
            Dim newVal As Integer = Math.Min(Math.Max(value, mRangeMin), mRangeMax)
            If newVal <> mintPreviewLocation Then
                mintPreviewLocation = newVal
                RaiseEvent SeekChanged(mintPreviewLocation)
                Me.Invalidate()
            End If
        End Set
    End Property
    Private mintPreviewLocation As Integer = 0

    Public Enum SliderID
        None = 0
        LeftTrim = 1
        RightTrim = 2
        Preview = 3
        Hover = 4
    End Enum

    Private menmSelectedSlider As SliderID

    ''' <summary>
    ''' Keeps track of which slider is currently selected
    ''' </summary>
    Public ReadOnly Property SelectedSlider As SliderID
        Get
            Return menmSelectedSlider
        End Get
    End Property


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

    Private maryHolePunches() As Double

    ''' <summary>
    ''' Each of these frames is shown as things to be cut out
    ''' </summary>\
    Public Property HolePunches As Double()
        Get
            Return maryHolePunches
        End Get
        Set(value As Double())
            maryHolePunches = value
            Me.Invalidate()
        End Set
    End Property

    Private WithEvents mobjMetaData As VideoData

    Public Property MetaData As VideoData
        Get
            Return mobjMetaData
        End Get
        Set(value As VideoData)
            If mobjMetaData?.Equals(value) Then
                'Don't update to self and avoid resetting controls when not needed
                Exit Property
            End If
            mobjMetaData = value
            If mobjMetaData IsNot Nothing Then
                Me.RangeMax = mobjMetaData.TotalFrames - 1
                Dim previewAspect As Double = Math.Min(42 / mobjMetaData.Size.Width, 34 / mobjMetaData.Size.Height)
                mobjPreviewForm.MinimumSize = New Size(mobjMetaData.Size.Width * previewAspect, mobjMetaData.Size.Height * previewAspect)
                mobjPreviewForm.MaximumSize = mobjPreviewForm.MinimumSize
            End If
            Me.RangeMinValue = 0
            Me.RangeMaxValue = Me.RangeMax
            Me.PreviewLocation = 0
        End Set
    End Property

    Public Sub New()
        Me.DoubleBuffered = True
        InitializePreviewForm()
    End Sub

    Public Sub New(metaData As VideoData)
        Me.MetaData = metaData
        Me.DoubleBuffered = True
        InitializePreviewForm()
    End Sub

    Private Sub InitializePreviewForm()
        mobjPreviewForm = New Form With {.FormBorderStyle = FormBorderStyle.None, .MinimumSize = New Size(32, 32), .TopMost = True, .MaximumSize = New Size(32, 32)}
        mobjPreviewForm.Controls.Add(New PictureBox With {.SizeMode = PictureBoxSizeMode.Zoom, .Dock = DockStyle.Fill})
    End Sub

    Private Const COLOR_HEIGHT As Integer = 12

    ''' <summary>
    ''' Paints over the control with custom dual trackbar looking graphics.
    ''' </summary>
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim numberOfTicks As Integer = Math.Min((Me.Width - 1) \ 2, mRangeMax - mRangeMin + 2) 'Tick represents start or end of a frame, number of frames + 1
        Dim distanceBetweenPoints As Double = (Me.Width - 1) / (numberOfTicks - 1)
        Dim leftPreview As Single = ((PreviewLocation / FullRange) * (Me.Width - 1))
        Dim rightPreview As Single = (((PreviewLocation + 1) / FullRange) * (Me.Width - 1))
        'Draw background
        Using contentBrush As New SolidBrush(If(Me.Enabled, Color.Green, Color.Gray))
            e.Graphics.FillRectangle(contentBrush, LeftSeekPixel, 3, RightSeekPixel - LeftSeekPixel, Me.Height - 6)
        End Using

        'Draw scene changes
        Using pen As New Pen(Color.DarkSeaGreen, 1)
            If mdblSceneChanges IsNot Nothing Then
                Dim frameIndex As Integer = 0
                For Each sceneChange As Single In mdblSceneChanges
                    e.Graphics.DrawLine(pen, New Point(frameIndex, Me.Height - 4), New Point(frameIndex, (Me.Height - 4) - (sceneChange * COLOR_HEIGHT)))
                    frameIndex += 1
                Next
            End If
        End Using

        'Draw hole punches
        Using pen As New Pen(Color.Gold, 1)
            If maryHolePunches IsNot Nothing Then
                Dim frameIndex As Integer = 0
                For Each holePunch As Single In maryHolePunches
                    e.Graphics.DrawLine(pen, New Point(frameIndex, Me.Height - 4), New Point(frameIndex, (Me.Height - 4) - (holePunch * COLOR_HEIGHT)))
                    frameIndex += 1
                Next
            End If
        End Using

        'Draw frame ticks, color based on if the frame has been cached or not
        If Me.mobjMetaData IsNot Nothing Then
            Dim currentFrameTick As Integer = 0
            For index As Integer = 0 To numberOfTicks - 1
                currentFrameTick = (Me.mobjMetaData.TotalFrames / numberOfTicks) * index
                Dim drawPen As Pen = Pens.Gray
                Select Case Me.mobjMetaData.ImageCacheStatus(currentFrameTick)
                    Case ImageCache.CacheStatus.None
                        drawPen = Pens.DarkGray
                    Case ImageCache.CacheStatus.Queued
                        drawPen = Pens.Orange
                    Case ImageCache.CacheStatus.Cached
                        drawPen = Pens.Black
                End Select
                e.Graphics.DrawLine(drawPen, New Point(index * distanceBetweenPoints, Me.Height), New Point(index * distanceBetweenPoints, Me.Height - 2))
            Next
        End If

        'Draw range sliders
        Using pen As New Pen(Color.Black, 1)
            e.Graphics.DrawLine(pen, New Point(LeftSeekPixel, Me.Height - 3), New Point(LeftSeekPixel, 0))
            e.Graphics.DrawLine(pen, New Point(RightSeekPixel, Me.Height - 3), New Point(RightSeekPixel, 0))
        End Using

        'Draw preview frame
        Using pen As New Pen(Color.LimeGreen, 1)
            'Draw preview differently if it is attached to a trim bar
            If LeftSeekPixel = leftPreview Then
                e.Graphics.DrawLine(pen, New Point(leftPreview, Me.Height - 3), New Point(leftPreview, 0))
            ElseIf RightSeekPixel = rightPreview Then
                e.Graphics.DrawLine(pen, New Point(rightPreview, Me.Height - 3), New Point(rightPreview, 0))
            Else
                e.Graphics.DrawLine(pen, New Point(leftPreview, Me.Height - 3), New Point(leftPreview, 0))
                e.Graphics.DrawLine(pen, New Point(rightPreview, Me.Height - 3), New Point(rightPreview, 0))
                e.Graphics.DrawLine(pen, New Point(leftPreview, 0), New Point(rightPreview, 0))
                e.Graphics.DrawLine(pen, New Point(leftPreview, Me.Height - 3), New Point(rightPreview, Me.Height - 3))
            End If
        End Using

        'Draw preview hover
        If mintHoveredFrameIndex IsNot Nothing Then
            Dim hoveredFrameIndex As Integer = Math.Floor(((mRangeMax - mRangeMin + 1) / Me.Width) * Me.PointToClient(Cursor.Position).X)
            Dim leftHoverPreview As Single = ((hoveredFrameIndex / FullRange) * (Me.Width - 1))
            Dim rightHoverPreview As Single = (((hoveredFrameIndex + 1) / FullRange) * (Me.Width - 1))
            Using pen As New Pen(Color.Red, 1)
                e.Graphics.DrawLine(pen, New Point(leftHoverPreview, Me.Height - 3), New Point(leftHoverPreview, 0))
                e.Graphics.DrawLine(pen, New Point(rightHoverPreview, Me.Height - 3), New Point(rightHoverPreview, 0))
                e.Graphics.DrawLine(pen, New Point(leftHoverPreview, 0), New Point(rightHoverPreview, 0))
                e.Graphics.DrawLine(pen, New Point(leftHoverPreview, Me.Height - 3), New Point(rightHoverPreview, Me.Height - 3))
            End Using
        End If
    End Sub

    Private ReadOnly Property FullRange As Integer
        Get
            Return (mRangeMax - mRangeMin) + 1
        End Get
    End Property

    Private ReadOnly Property LeftSeekPixel As Single
        Get
            Return ((RangeMinValue / FullRange) * (Me.Width - 1))
        End Get
    End Property

    Private ReadOnly Property RightSeekPixel As Single
        Get
            Return (((RangeMaxValue + 1) / FullRange) * (Me.Width - 1))
        End Get
    End Property

    Private ReadOnly Property PreviewBounds As RectangleF
        Get
            Dim leftPreview As Single = ((PreviewLocation / FullRange) * (Me.Width - 1))
            Dim rightPreview As Single = (((PreviewLocation + 1) / FullRange) * (Me.Width - 1))
            Return New RectangleF(leftPreview, 0, rightPreview - leftPreview, Me.Height)
        End Get
    End Property

#Region "Collision"
    Private Const SLIDER_COLLISION_WIDTH As Integer = 20
    Private Function CollisionRect(targetSlider As SliderID) As RectangleF
        Select Case targetSlider
            Case SliderID.LeftTrim
                Dim leftPixel As Single = Math.Min(LeftSeekPixel - (SLIDER_COLLISION_WIDTH / 2), Me.Width - SLIDER_COLLISION_WIDTH - 1)
                Dim rightpixel As Single = leftPixel + (SLIDER_COLLISION_WIDTH)
                Return New RectangleF(leftPixel, 0, rightpixel - leftPixel, Me.Height)
            Case SliderID.RightTrim
                Dim rightpixel As Single = RightSeekPixel + (SLIDER_COLLISION_WIDTH / 2)
                Dim leftPixel As Single = rightpixel - (SLIDER_COLLISION_WIDTH)
                Return New RectangleF(leftpixel, 0, rightPixel - leftpixel, Me.Height)
            Case SliderID.Preview
                Dim previewRect As RectangleF = PreviewBounds
                previewRect.X -= (SLIDER_COLLISION_WIDTH - previewRect.Width) / 2
                previewRect.Width = SLIDER_COLLISION_WIDTH - previewRect.Width
                Return PreviewBounds
            Case Else
                Return Nothing
        End Select
    End Function

    Private Function PotentialCollisions(location As Point) As List(Of SliderID)
        Dim resultList As New List(Of SliderID)

        If CollisionRect(SliderID.LeftTrim).Contains(location) Then
            resultList.Add(SliderID.LeftTrim)
        End If
        If CollisionRect(SliderID.RightTrim).Contains(location) Then
            resultList.Add(SliderID.RightTrim)
        End If
        If CollisionRect(SliderID.Preview).Contains(location) Then
            resultList.Add(SliderID.Preview)
        End If
        'TODO Hover
        Return resultList
    End Function
#End Region

#Region "Mouse Interaction"
    ''' <summary>
    ''' Searches for the nearest slider in the control, and selects it for use.
    ''' </summary>
    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
		'Convert mouse coordinates to increments, Grab closest slider
		If Me.Enabled AndAlso e.Button = MouseButtons.Left Then
            Dim newValue As Integer = Math.Floor(((mRangeMax - mRangeMin) / Me.Width) * e.X)

            Dim potentialCollisions As List(Of SliderID) = Me.PotentialCollisions(e.Location)

			If potentialCollisions.Count > 0 Then
				potentialCollisions.Sort(Function(obj1, obj2) CollisionRect(obj1).DistanceToCenter(e.Location).CompareTo(CollisionRect(obj2).DistanceToCenter(e.Location)))
                menmSelectedSlider = potentialCollisions(0)
                If menmSelectedSlider = SliderID.Preview Then
                    'Give priority to trim slider
                    If RangeMinValue = PreviewLocation Then
                        menmSelectedSlider = SliderID.LeftTrim
                    ElseIf RangeMaxValue = SliderID.Preview Then
                        menmSelectedSlider = SliderID.RightTrim
                    End If
                End If
            Else
                menmSelectedSlider = SliderID.Preview
			End If
            If menmSelectedSlider <> SliderID.Preview AndAlso Me.Cursor = Cursors.Hand Then
                Me.Cursor = Cursors.SizeWE
            End If

            Me.OnMouseMove(e)
			Me.Invalidate()
		End If
	End Sub

    ''' <summary>
    ''' Changes the corresponding range values for the sliders in the control.
    ''' </summary>
    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If Me.Enabled Then
            If menmSelectedSlider = SliderID.None Then
                Dim potentialCollisions As List(Of SliderID) = Me.PotentialCollisions(e.Location)
                If potentialCollisions.Count > 0 Then
                    If Not (potentialCollisions.Count = 1 AndAlso potentialCollisions(0) = SliderID.Preview) AndAlso Me.Cursor = Cursors.Hand Then
                        Me.Cursor = Cursors.SizeWE
                    End If
                Else
                    If Not Me.Cursor = Cursors.Hand Then
                        Me.Cursor = Cursors.Hand
                    End If
                End If
            End If
            Dim newHoverIndex As Integer = Math.Floor(((mRangeMax - mRangeMin + 1) / Me.Width) * e.X)
            If mintHoveredFrameIndex Is Nothing OrElse newHoverIndex <> mintHoveredFrameIndex Then
                mintHoveredFrameIndex = newHoverIndex
                Me.Invalidate()
            End If
            If e.Button = Windows.Forms.MouseButtons.Left Then
                'Move range sliders
                Select Case menmSelectedSlider
                    Case SliderID.LeftTrim
                        If Not RangeMinValue = mintHoveredFrameIndex Then
                            RangeMinValue = mintHoveredFrameIndex
                        End If
                    Case SliderID.RightTrim
                        If Not RangeMaxValue = mintHoveredFrameIndex Then
                            RangeMaxValue = mintHoveredFrameIndex
                        End If
                    Case SliderID.Preview
                        If Not PreviewLocation = mintHoveredFrameIndex Then
                            PreviewLocation = mintHoveredFrameIndex
                        End If
                    Case SliderID.Hover
                        'TODO Maybe have a teeny display for cached images, scene change info, etc.
                    Case Else
                        'Ignore if nothing is selected
                End Select
            End If

            If Me.mobjMetaData IsNot Nothing Then
                Dim imageToPreview As Image = Nothing
                If Me.mobjMetaData.ImageCacheStatus(mintHoveredFrameIndex) = ImageCache.CacheStatus.Cached Then
                    imageToPreview = Me.mobjMetaData.GetImageFromCache(mintHoveredFrameIndex)
                ElseIf Me.mobjMetaData.ThumbImageCacheStatus(mintHoveredFrameIndex) = ImageCache.CacheStatus.Cached Then
                    imageToPreview = Me.mobjMetaData.GetImageFromThumbCache(mintHoveredFrameIndex)
                End If
                If imageToPreview IsNot Nothing Then
                    'Setup preview frame
                    CType(mobjPreviewForm.Controls(0), PictureBox).Image = imageToPreview
                    mobjPreviewForm.Visible = True
                    mobjPreviewForm.Location = Me.PointToScreen(New Point(e.X - mobjPreviewForm.Width / 2, 0 - mobjPreviewForm.Height))
                Else
                    mobjPreviewForm.Visible = False
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
			menmSelectedSlider = SliderID.None
			RaiseEvent SeekChanged(mintPreviewLocation) 'Not actually changed, but we are done manipulating at least
		End If
    End Sub
#End Region


    Private Sub CacheUpdated(sender As Object, objCache As ImageCache, ranges As List(Of List(Of Integer))) Handles mobjMetaData.QueuedFrames, mobjMetaData.RetrievedFrames
        Me.Invalidate()
    End Sub


    Private Sub VideoSeeker_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        Me.Invalidate()
    End Sub

    Private Sub VideoSeeker_MouseLeave(sender As Object, e As EventArgs) Handles MyBase.MouseLeave
        mobjPreviewForm.Visible = False
        mintHoveredFrameIndex = Nothing
        Me.Invalidate()
    End Sub
End Class
