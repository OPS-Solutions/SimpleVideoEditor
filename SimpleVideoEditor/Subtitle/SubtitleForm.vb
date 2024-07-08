Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions

Public Class SubtitleForm
    Private mobjGenericToolTip As ToolTipPlus = New ToolTipPlus() 'Tooltip object required for setting tootips on controls
    Public FilePath As String = ""
    Private lastText As String = ""
    Public CurrentSubrip As SubRip
    Public _currentEntryIndex As Integer = -1
    ''' <summary>
    ''' For keeping track of when a selection change happens due to typing, but we don't want to handle it until the textchanged event occurs
    ''' </summary>
    Private _selectionChangeFlag As Boolean = False
    Const DEFAULT_TEXT As String = "1
00:00:00,000 --> 00:00:01,500
This is an example subtitle

2
00:00:02,000 --> 00:00:04,000
Each subtitle has its own section and appearance time

3
00:00:04,000 --> 00:00:07,000
They can be styled with tags to make <b>bolded</b>, <i>italic</i>,<u>underlined</u>, or <font color=""red"">colored text</font>
"

    ''' <summary>
    ''' Event for when the subtitles have changed and been saved by the user
    ''' </summary>
    Public Event SubChanged()

#Region "Properties"
    ''' <summary>
    ''' Occurs when any slider changes, requesting a change to the main forms preview slider to display the frame
    ''' </summary>
    Public Event PreviewChanged(newFrame As Integer)

    Public ReadOnly Property Seeker As VideoSeeker
        Get
            Return ctlSubtitleSeeker
        End Get
    End Property


    ''' <summary>
    ''' An object representation of the current subtitle entry
    ''' Modification will not be reflected by the editor
    ''' returns nothing if no selection
    ''' </summary>
    Private Property CurrentEntry As SubEntry
        Get
            If _currentEntryIndex < 0 Then
                Return Nothing
            End If
            Return CurrentSubrip.Entries(_currentEntryIndex)
        End Get
        Set(value As SubEntry)
            Dim foundIndex As Integer = CurrentSubrip.Entries.IndexOf(value)
            If foundIndex > -1 AndAlso _currentEntryIndex <> foundIndex Then
                _currentEntryIndex = CurrentSubrip.Entries.IndexOf(value)
                CurrentSubrip.Entries(_currentEntryIndex) = value
                If CurrentSubrip.FindByChar(txtEditor.SelectionStart)?.ToString.Equals(value.ToString) Then
                Else
                    txtEditor.SelectionStart = CurrentSubrip.CharIndexOf(CurrentEntry)
                End If
                UpdateEditorSelection(value)
            End If
            Dim currentLine As String = txtEditor.Lines(Math.Min(txtEditor.Lines.Count - 1, txtEditor.GetLineFromCharIndexUnwrapped(txtEditor.SelectionStart)))
            If Regex.IsMatch(currentLine, "(\d*):(\d*):(\d*),(\d\d\d) --> (\d*):(\d*):(\d*),(\d\d\d)") Then
                If (txtEditor.GetFirstCharIndexOfCurrentLineUnwrapped + "00:00:00,000 -".Length) > txtEditor.SelectionStart Then
                    'Set seeker to start time
                    ctlSubtitleSeeker.PreviewLocation = ctlSubtitleSeeker.RangeMinValue
                Else
                    'Set seeker to endtime
                    ctlSubtitleSeeker.PreviewLocation = ctlSubtitleSeeker.RangeMaxValue
                End If
                RaiseEvent PreviewChanged(ctlSubtitleSeeker.PreviewLocation)
            End If
        End Set
    End Property
#End Region


#Region "UI Handlers"
    Private Sub SubtitleForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FilePath = GetTempSrt()
        lastText = txtEditor.Text
        mobjGenericToolTip.SetToolTip(btnBrowse, $"Browse for an existing .srt file to load in and use/edit.")
        mobjGenericToolTip.SetToolTip(btnSave, $"Save to .srt file.{vbNewLine}This is not required to add subtitles to the loaded video, but may be helpful as the data will otherwise be lost when closing SVE.")
        mobjGenericToolTip.SetToolTip(btnAdd, $"Add new subtitle entry starting at the current frame.")
        mobjGenericToolTip.SetToolTip(btnDelete, $"Delete the current selected subtitle entry.")
        mobjGenericToolTip.SetToolTip(ctlSubtitleSeeker, $"While focused or selecting a timespan line.{vbNewLine}Use [A][D][←][→] to move subtitle sliders frame by frame.{vbNewLine}Hold [Shift] to move preview slider instead.")
        SetSRT(True)
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        Try
            Using srtOpen As New OpenFileDialog
                srtOpen.Multiselect = False
                srtOpen.Filter = "SubRip (*.srt)|*.srt"
                srtOpen.Title = "Select Subtitle File - (.srt)"
                srtOpen.AddExtension = True
                Select Case srtOpen.ShowDialog
                    Case DialogResult.OK
                        FilePath = srtOpen.FileName
                        SetSRT(IO.File.ReadAllText(srtOpen.FileName))
                    Case Else
                        'User did not attempt opening
                End Select
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            Using srtSave As New SaveFileDialog
                srtSave.Title = "Select Subtitle Save Location"
                srtSave.Filter = "SubRip (*.srt)|*.srt|All files (*.*)|*.*"
                Dim validExtensions() As String = srtSave.Filter.Split("|")
                If FilePath?.Length > 0 AndAlso IO.File.Exists(FilePath) Then
                    srtSave.FileName = IO.Path.GetFileName(FilePath)
                End If
                srtSave.OverwritePrompt = True
                Select Case srtSave.ShowDialog
                    Case DialogResult.OK
                        IO.File.WriteAllText(srtSave.FileName, txtEditor.Text)
                        RaiseEvent SubChanged()
                    Case Else
                        'User did not attempt saving
                End Select
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        If ctlSubtitleSeeker.MetaData Is Nothing Then
            Exit Sub
        End If
        'Check current previewframe, starting a new subtitle there
        Dim newEntry As SubEntry = CurrentSubrip.InsertAt(ctlSubtitleSeeker.MetaData.ThumbImageCachePTS(ctlSubtitleSeeker.PreviewLocation))
        Dim newIndex As Integer = CurrentSubrip.Entries.IndexOf(newEntry)
        SetSRT()
        CurrentEntry = CurrentSubrip.Entries(newIndex)
        txtEditor.SelectionStart = CurrentSubrip.CharIndexOf(CurrentEntry)
        txtEditor.SelectionLength = CurrentEntry.Text.Length
        SetSRT()
        txtEditor.Focus()
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        'Remove current subtitle
        If CurrentSubrip.Entries.Count = 1 Then
            Exit Sub
        End If
        CurrentSubrip.Entries.Remove(CurrentEntry)
        CurrentEntry = CurrentSubrip.Entries(Math.Min(_currentEntryIndex, CurrentSubrip.Entries.Count - 1))
        SetSRT()
    End Sub

    Private Sub SubtitleForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Select Case e.CloseReason
            Case CloseReason.UserClosing
                If Me.Visible Then
                    'Don't actually close, as we use this form to store the file data for future use
                    e.Cancel = True
                    Me.Hide()
                End If
        End Select
    End Sub

    Private Sub txtEditor_TextChanged(sender As Object, e As EventArgs) Handles txtEditor.TextChanged
        'Sanitize the file to ensure users don't break section numbers, whitespace, or time lines
        If SubRip.FromString(txtEditor.Text) Is Nothing Then
            txtEditor.SetText(lastText)
            UpdateSeeker()
            CurrentEntry = CurrentSubrip.FindByChar(txtEditor.SelectionStart)
            UpdateEditorSelection(CurrentEntry)
            Exit Sub
        End If
        CurrentSubrip = SubRip.FromString(txtEditor.Text)

        RaiseEvent SubChanged()
        'Reset last known working text
        lastText = txtEditor.Text
        If _selectionChangeFlag Then
            txtEditor_SelectionChanged(Me, Nothing)
        End If
    End Sub

    Private Sub txtEditor_SelectionChanged(sender As Object, e As EventArgs) Handles txtEditor.SelectionChanged
        If txtEditor.SelectionLength > 0 Then
            'Don't update while user is selecting large portions of text
            Exit Sub
        End If
        If Not lastText.Equals(txtEditor.Text) Then
            'Selection must have been a result of typing
            _selectionChangeFlag = True
            Exit Sub
        End If
        CurrentEntry = CurrentSubrip.FindByChar(txtEditor.SelectionStart)
    End Sub

    Private Sub ctlSubtitleSeeker_SeekChanged(newVal As Integer) Handles ctlSubtitleSeeker.SeekChanged
        'Ensure the current selection is where the preview points to
        If ctlSubtitleSeeker.MetaData?.ScenesReady Then
            If ctlSubtitleSeeker.PreviewLocation <> ctlSubtitleSeeker.RangeMinValue AndAlso ctlSubtitleSeeker.PreviewLocation <> ctlSubtitleSeeker.RangeMaxValue Then
                Dim foundEntry As SubEntry = CurrentSubrip?.FindByTime(_currentEntryIndex, Me.ctlSubtitleSeeker.MetaData.ThumbImageCachePTS(Me.ctlSubtitleSeeker.PreviewLocation))
                If foundEntry IsNot Nothing Then
                    CurrentEntry = foundEntry
                End If
            End If
            UpdateSRT()
        End If
        RaiseEvent PreviewChanged(newVal)
    End Sub

    ''' <summary>
    ''' Captures key events before everything else, and uses them to modify the video trimming picRangeSlider control.
    ''' </summary>
    Protected Overrides Function ProcessCmdKey(ByRef message As Message, ByVal keys As Keys) As Boolean
        If ctlSubtitleSeeker.Focused OrElse CurrentSubrip.CharInTime(txtEditor.SelectionStart) Then
            'Check number key states for frameskip
            Dim skipValue As Integer = 1
            For index As Integer = 0 To 9
                If My.Computer.Keyboard.KeyPressed(Keys.D0 + index) Then
                    If index = 0 Then
                        skipValue = 10
                    Else
                        skipValue = index
                    End If
                    Exit For
                End If
            Next

            'Check for slider motion
            Select Case keys
                Case Keys.A
                    ctlSubtitleSeeker.RangeMinValue = ctlSubtitleSeeker.RangeMinValue - skipValue
                    ctlSubtitleSeeker.Invalidate()
                Case Keys.D
                    ctlSubtitleSeeker.RangeMinValue = ctlSubtitleSeeker.RangeMinValue + skipValue
                    ctlSubtitleSeeker.Invalidate()
                Case Keys.Left
                    ctlSubtitleSeeker.RangeMaxValue = ctlSubtitleSeeker.RangeMaxValue - skipValue
                    ctlSubtitleSeeker.Invalidate()
                Case Keys.Right
                    ctlSubtitleSeeker.RangeMaxValue = ctlSubtitleSeeker.RangeMaxValue + skipValue
                    ctlSubtitleSeeker.Invalidate()
                Case Keys.A Or Keys.Shift, Keys.Left Or Keys.Shift
                    ctlSubtitleSeeker.PreviewLocation = ctlSubtitleSeeker.PreviewLocation - skipValue
                    ctlSubtitleSeeker.Invalidate()
                Case Keys.D Or Keys.Shift, Keys.Right Or Keys.Shift
                    ctlSubtitleSeeker.PreviewLocation = ctlSubtitleSeeker.PreviewLocation + skipValue
                    ctlSubtitleSeeker.Invalidate()
                Case Else
                    Return MyBase.ProcessCmdKey(message, keys)
            End Select
            Return True
        End If
        Return False
    End Function
#End Region

    ''' <summary>
    ''' Saves current editor text into temp folder so it can be grabbed by ffmpeg
    ''' </summary>
    Public Sub SaveToTemp(seek As Double, playbackSpeed As Double)
        If Not IO.Directory.Exists(Globals.TempPath) Then
            IO.Directory.CreateDirectory(Globals.TempPath)
        End If
        If Not IO.File.Exists(FilePath) Then
            IO.File.Create(FilePath).Dispose()
        End If
        Dim modifiedSubrip As SubRip = SubRip.FromString(CurrentSubrip.ToString)
        For Each objEntry In modifiedSubrip.Entries
            objEntry.StartTime = New TimeSpan(objEntry.StartTime.Ticks / playbackSpeed)
            objEntry.StartTime = objEntry.StartTime.Subtract(New TimeSpan(Math.Max(0, (seek / playbackSpeed) * 10000000)))
            objEntry.EndTime = New TimeSpan(objEntry.EndTime.Ticks / playbackSpeed)
            objEntry.EndTime = objEntry.EndTime.Subtract(New TimeSpan(Math.Max(0, (seek / playbackSpeed) * 10000000)))
        Next
        IO.File.WriteAllText(GetTempSrt, modifiedSubrip.ToString)
    End Sub

    ''' <summary>
    ''' Parses the given text into a subrip and tries to update the UI to match
    ''' </summary>
    Public Sub SetSRT(newText As String)
        If newText Is Nothing Then
            newText = DEFAULT_TEXT
        End If
        CurrentSubrip = SubRip.FromString(newText)
        SetSRT()
    End Sub

    ''' <summary>
    ''' Sets the current text to an srt format string created from the current collection of entries
    ''' </summary>
    Private Sub SetSRT(Optional forceLoad As Boolean = False)
        Dim newText As String = CurrentSubrip.ToString.Replace(vbCrLf, vbLf)
        If Not txtEditor.Text.Equals(newText) OrElse forceLoad Then
            txtEditor.SetText(newText)
            CurrentSubrip = SubRip.FromString(txtEditor.Text)
            CurrentEntry = CurrentSubrip.FindByChar(txtEditor.SelectionStart)
            UpdateEditorSelection(CurrentEntry)
            UpdateSeeker()
            RaiseEvent SubChanged()
        End If
    End Sub
    ''' <summary>
    ''' Updates the editor by changing the text color of the target entry to blue
    ''' </summary>
    Private Sub UpdateEditorSelection(entry As SubEntry)
        Try
            If entry Is Nothing Then
                Exit Sub
            End If
            lastText = txtEditor.Text
            Dim sectionStart As Integer = txtEditor.Text.IndexOf(entry.SeedString)
            'Syntax highlighting
            txtEditor.BeginUpdate()
            txtEditor.EventsEnabled = False

            Dim lastSelection As Integer = txtEditor.SelectionStart
            Dim lastSelectionLength As Integer = txtEditor.SelectionLength
            'If txtEditor.Text.Equals(lastText) AndAlso txtEditor.SelectionColor = Color.Blue Then
            '    'Already highlighted, don't mess with the controll
            '    Exit Sub
            'End If

            'Clear formatting
            txtEditor.SelectionStart = 0
            txtEditor.SelectionLength = txtEditor.Text.Length
            txtEditor.SelectionColor = Color.Black
            'txtEditor.SelectionFont = New Font(txtEditor.Font, FontStyle.Regular)

            'Select section
            txtEditor.SelectionStart = sectionStart
            txtEditor.SelectionLength = entry.SeedString.Count

            'Apply selection formatting
            'txtEditor.SelectionFont = New Font(txtEditor.Font, FontStyle.Bold)
            txtEditor.SelectionColor = Color.Blue

            txtEditor.SelectionStart = lastSelection
            txtEditor.SelectionLength = lastSelectionLength
            UpdateSeeker()
        Catch
            Throw
        Finally
            txtEditor.EventsEnabled = True
            txtEditor.EndUpdate()
        End Try
    End Sub

    Private Sub UpdateSeeker()
        'Find nearest frame timestamp
        Dim metaObject As VideoData = ctlSubtitleSeeker.MetaData
        If metaObject Is Nothing Then
            Exit Sub
        End If
        Dim startFrame As Integer? = metaObject.GetFrameByPTS(CurrentEntry.StartTime.TotalSeconds)
        Dim endFrame As Integer? = metaObject.GetFrameByPTS(CurrentEntry.EndTime.TotalSeconds)
        If startFrame Is Nothing OrElse endFrame Is Nothing Then
            Exit Sub
        End If
        Dim newVal As Integer = -1
        ctlSubtitleSeeker.EventsEnabled = False
        If ctlSubtitleSeeker.RangeMinValue <> startFrame Then
            newVal = startFrame
            ctlSubtitleSeeker.RangeMinValue = startFrame
        End If
        If ctlSubtitleSeeker.RangeMaxValue <> endFrame Then
            'Prioritize start frame
            If newVal = -1 Then
                newVal = endFrame
            End If
            ctlSubtitleSeeker.RangeMaxValue = endFrame
        End If
        ctlSubtitleSeeker.EventsEnabled = True
        ctlSubtitleSeeker.Invalidate()

        If newVal <> -1 Then
            RaiseEvent PreviewChanged(newVal)
        End If
    End Sub

    ''' <summary>
    ''' Checks the current timespan values for the selected entry, updating it to reflect the selected range from the seeker
    ''' </summary>
    Private Sub UpdateSRT()
        If Me.ctlSubtitleSeeker.MetaData Is Nothing OrElse Not Me.Visible Then
            Exit Sub
        End If

        'Set srt selection based on current seek selection
        Dim current As SubEntry = CurrentEntry
        If CurrentEntry IsNot Nothing Then
            Dim startPTS As Double? = Me.ctlSubtitleSeeker.MetaData.ThumbImageCachePTS(Me.ctlSubtitleSeeker.RangeMinValue)
            Dim endPTS As Double? = Me.ctlSubtitleSeeker.MetaData.ThumbImageCachePTS(Me.ctlSubtitleSeeker.RangeMaxValue)
            If startPTS.HasValue Then
                current.StartTime = New TimeSpan(TimeSpan.TicksPerSecond * Me.ctlSubtitleSeeker.MetaData.ThumbImageCachePTS(Me.ctlSubtitleSeeker.RangeMinValue))
            End If
            If endPTS.HasValue Then
                current.EndTime = New TimeSpan(TimeSpan.TicksPerSecond * Me.ctlSubtitleSeeker.MetaData.ThumbImageCachePTS(Me.ctlSubtitleSeeker.RangeMaxValue))
            End If

            SetSRT()
        End If
    End Sub
End Class