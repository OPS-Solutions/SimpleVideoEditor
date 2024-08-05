
''' <summary>
''' Class to allow better control of tool tips, specifically so activation can be disabled independantly
''' </summary>
Public Class ToolTipPlus
    Private WithEvents myTip As New ToolTip
    Private mblnAssociatedControls As New List(Of Control)
    Private mblnManualShow As Boolean = False
    Private mblnVisible As Boolean = False
    Private mdatLastPopup As DateTime = Now
    Private mdatLastMove As DateTime = Now
    Private WithEvents mobjArmTimer As New System.Timers.Timer
    Private mobjLastControl As Control = Nothing
    Private mobjControlLock As New Object
    Private mptLastMouseMove As Point? 'Keeps track of the last position of the mousemove event to avoid it firing while not moving (don't know why it does that)

    Public Sub New()
        mobjArmTimer.Interval = 500
        mobjArmTimer.AutoReset = False
        myTip.UseFading = False
    End Sub

    ''' <summary>
    ''' Function replacement for SetToolTip that connects handlers for OnHover
    ''' </summary>
    ''' <param name="control"></param>
    ''' <param name="toolTip"></param>
    Public Sub SetToolTip(control As Control, toolTip As String)
        'Check if tip is already up to date
        If Not myTip.GetToolTip(control) = toolTip Then
            myTip.SetToolTip(control, toolTip)
        End If
        If Not mblnAssociatedControls.Contains(control) Then
            mblnAssociatedControls.Add(control)
            AddHandler control.MouseMove, AddressOf control_OnMouseMove
            AddHandler control.MouseLeave, AddressOf control_OnMouseLeave
            AddHandler control.MouseDown, AddressOf control_OnMouseDown
            AddHandler control.MouseEnter, AddressOf control_OnMouseEnter
            AddHandler control.MouseCaptureChanged, AddressOf control_OnMouseCaptureChanged
        End If
    End Sub

    Public Sub RemoveAll()
        For Each objControl In mblnAssociatedControls
            RemoveHandler objControl.MouseMove, AddressOf control_OnMouseMove
            RemoveHandler objControl.MouseLeave, AddressOf control_OnMouseLeave
            RemoveHandler objControl.MouseDown, AddressOf control_OnMouseDown
            RemoveHandler objControl.MouseEnter, AddressOf control_OnMouseEnter
            RemoveHandler objControl.MouseCaptureChanged, AddressOf control_OnMouseCaptureChanged
        Next
        mblnAssociatedControls.Clear()
    End Sub

    ''' <summary>
    ''' Ensures the popup does not appear from duration within control
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub tooltip_popup(sender As Object, e As PopupEventArgs) Handles myTip.Popup
        If mblnManualShow Then
            mblnVisible = True
        Else
            e.Cancel = True
        End If
    End Sub

    Private debugMove As Boolean = False

    ''' <summary>
    ''' Ensures the tip disappears with mouse movement, and whatever control is being moved on is set as the thing we monitor for future hovers
    ''' </summary>
    Private Sub control_OnMouseMove(sender As Object, e As EventArgs)
        Dim positionChanged As Boolean = mptLastMouseMove Is Nothing OrElse (mptLastMouseMove.Value.X <> CType(e, MouseEventArgs).X OrElse mptLastMouseMove.Value.Y <> CType(e, MouseEventArgs).Y)
        If Not positionChanged Then
            Exit Sub
        End If
        mptLastMouseMove = CType(e, MouseEventArgs).Location
        SyncLock mobjControlLock
            mobjLastControl = sender
        End SyncLock
        'Arm a timer that when elapses with no motion event occuring, will show the tooltip
        mobjArmTimer.Interval = 500
        mobjArmTimer.Start()
        ResetMotion(sender)
    End Sub

    ''' <summary>
    ''' Sets up the entered control to monitor for hovering
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub control_OnMouseEnter(sender As Object, e As EventArgs)
        DisableTip()
        SyncLock mobjControlLock
            mobjLastControl = sender
        End SyncLock
        ResetMotion(sender)
    End Sub

    Private Sub control_OnMouseLeave(sender As Object, e As EventArgs)
        DisableTip()
        ResetMotion(sender)
    End Sub

    ''' <summary>
    ''' Disables the tooltip when mouse capture changes, which can happen in cases like the context menu opening
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub control_OnMouseCaptureChanged(sender As Object, e As EventArgs)
        DisableTip()
        ResetMotion(sender)
    End Sub

    Private Sub control_OnMouseDown(sender As Object, e As EventArgs)
        DisableTip()
        ResetMotion(sender)
    End Sub

    ''' <summary>
    ''' Clears whatever control was last set to monitor for hovers
    ''' </summary>
    Private Sub DisableTip()
        mptLastMouseMove = Nothing
        SyncLock mobjControlLock
            mobjLastControl = Nothing
        End SyncLock
    End Sub

    Private Sub ResetMotion(sender As Object)
        mdatLastMove = Now
        If mblnVisible Then
            myTip.Hide(sender)
            mblnVisible = False
        End If
    End Sub

    ''' <summary>
    ''' Periodically checks if enough time has passed since the last mouse move on the target control before a tooltip is displayed
    ''' </summary>
    Private Sub CheckTipOK() Handles mobjArmTimer.Elapsed
        Dim msElapsed As Integer = Now.Subtract(mdatLastMove).TotalMilliseconds
        If msElapsed < 500 Then
            mobjArmTimer.Interval = 500 - msElapsed
            mobjArmTimer.Start()
        Else
            debugMove = True
            SyncLock mobjControlLock
                If mobjLastControl IsNot Nothing Then
                    If mobjLastControl.InvokeRequired Then
                        mobjLastControl.BeginInvoke(Sub()
                                                        'Done in 2 stages to avoid deadlocking
                                                        SyncLock mobjControlLock
                                                            If mobjLastControl IsNot Nothing Then
                                                                Dim associatedTip As String = myTip.GetToolTip(mobjLastControl)
                                                                mblnManualShow = True
                                                                Dim controlBased As Point = CType(mobjLastControl, Control).PointToClient(Cursor.Position())
                                                                myTip.Show(associatedTip, mobjLastControl, controlBased.X, controlBased.Y + Cursor.Current.Size.Height)
                                                                mdatLastPopup = Now
                                                                mblnManualShow = False
                                                            End If
                                                        End SyncLock
                                                    End Sub)
                    End If
                End If
            End SyncLock
        End If
    End Sub
End Class
