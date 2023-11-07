Imports System.Text
Imports System.Text.RegularExpressions

Public Class SubRip
    Public Shared SubRipMatcher As Regex = New Regex("(?<id>\d*)\s*\n\s*(?<time>\d+:\d+:\d+,\d+ --> \d+:\d+:\d+,\d+)\s?\n(?<text>.*)\s*")
    Public Entries As New List(Of SubEntry)

    Public Sub New()

    End Sub

    ''' <summary>
    ''' Reads entries from .srt formatted file text
    ''' </summary>
    Public Shared Function FromString(fileText As String) As SubRip
        Dim newSubrip As New SubRip
        Dim subSections As MatchCollection = SubRipMatcher.Matches(fileText)
        Dim charCount As Integer = 0
        For Each strSection As Match In subSections
            charCount += strSection.Length
            newSubrip.Entries.Add(SubEntry.FromString(strSection.Value.Trim))
            If newSubrip.Entries.Last Is Nothing Then
                'Failed
                Return Nothing
            End If
        Next
        If charCount < fileText.Trim.Length Then
            'Junk data at end
            Return Nothing
        End If
        Return newSubrip
    End Function

    ''' <summary>
    ''' Inserts a new entry into the list at the specified time between other nearby entries
    ''' Returns the created entry
    ''' </summary>
    Public Function InsertAt(seconds As Double) As SubEntry
        Dim previewTime As TimeSpan = New TimeSpan(TimeSpan.TicksPerSecond * seconds)
        Dim insertionPoint As Integer = Me.Entries.Count
        For index As Integer = 0 To Me.Entries.Count - 1
            If Me.Entries(index).StartTime > previewTime Then
                insertionPoint = index
                Exit For
            End If
        Next

        'Going with simple duration of 2s seems fine, but maybe we could be smart and adjust duration based on text contents
        Dim newEntry As SubEntry = New SubEntry() With {
                           .SectionID = insertionPoint + 1,
                           .StartTime = New TimeSpan(TimeSpan.TicksPerSecond * seconds),
                           .EndTime = New TimeSpan(TimeSpan.TicksPerSecond * (seconds + 2)),
                           .Text = "Test"
                           }
        newEntry.SeedString = newEntry.ToString.Replace(vbCrLf, vbLf)
        Me.Entries.Insert(insertionPoint, newEntry)
        Return newEntry
    End Function

    ''' <summary>
    ''' Gets the first entry that has a timespan the given time falls within
    ''' </summary>
    Public Function FindByTime(priorityIndex As Integer, targetTimeSeconds As Double)
        Dim priorityEntry As SubEntry = Nothing
        If priorityIndex >= 0 AndAlso priorityIndex < Me.Entries.Count Then
            priorityEntry = Me.Entries(priorityIndex)
            If priorityEntry.StartTime.TotalSeconds <= targetTimeSeconds AndAlso priorityEntry.EndTime.TotalSeconds >= targetTimeSeconds Then
                Return priorityEntry
            End If
        End If
        Return Me.Entries.Find(Function(obj) obj.StartTime.TotalSeconds <= targetTimeSeconds AndAlso obj.EndTime.TotalSeconds >= targetTimeSeconds)
    End Function

    ''' <summary>
    ''' Checks if the given character is within a time line
    ''' </summary>
    Public Function CharInTime(charIndex As Integer) As Boolean
        Dim srtText As String = Me.ToString.Replace(vbCrLf, vbLf)
        Dim timeMatches As MatchCollection = Regex.Matches(srtText, "(?<=\n)\s*(\d*):(\d*):(\d*),(\d\d\d) --> (\d*):(\d*):(\d*),(\d\d\d)")
        For Each timeMatch As Match In timeMatches
            If timeMatch.Index <= charIndex AndAlso timeMatch.Index + timeMatch.Length > charIndex Then
                Return True
            End If
        Next
        Return False
    End Function

    ''' <summary>
    ''' Finds the index of a the beginning of the text line for the given entry in the full .srt formatted text string
    ''' </summary>
    Public Function CharIndexOf(entry As SubEntry) As Integer
        Dim sectionMatch As Match = Regex.Match(Me.ToString.Replace(vbCrLf, vbLf), $"{entry.SectionID}\s*\n\s*.*\n")
        Return sectionMatch.Index + sectionMatch.Length
    End Function

    ''' <summary>
    ''' Finds a specific entry based on a character within the .srt formatted text
    ''' </summary>
    Public Function FindByChar(selectionStart As String) As SubEntry
        'Dim startSubstring As String = Me.ToString.Substring(0, selectionStart + 1)
        'Dim lostChars As Integer = startSubstring.Length - startSubstring.Replace(vbCrLf, vbLf).Length
        'selectionStart -= lostChars
        Dim srtText As String = Me.ToString.Replace(vbCrLf, vbLf)
        If Me.Entries Is Nothing Then
            Return Nothing
        End If
        Dim subSections As MatchCollection = SubRipMatcher.Matches(srtText)
        Dim charCount As Integer = 0
        Dim sectionLength As Integer = 0
        Dim resultEntry As SubEntry = Nothing
        For sectionIndex As Integer = 0 To subSections.Count - 1
            sectionLength = subSections(sectionIndex).Length
            If selectionStart >= charCount AndAlso selectionStart < charCount + sectionLength Then
                resultEntry = Me.Entries(sectionIndex)
                Exit For
            End If
            charCount += sectionLength
        Next
        If resultEntry Is Nothing Then
            If selectionStart >= charCount Then
                resultEntry = Me.Entries.Last
            ElseIf selectionStart <= 0 Then
                resultEntry = Me.Entries.First
            End If
        End If
        Return resultEntry
    End Function

    ''' <summary>
    ''' Converts the file into its .srt formatted text representation
    ''' </summary>
    Public Overrides Function ToString() As String
        'Clean up numbers for if entries are added or removed
        For index As Integer = 0 To Me.Entries.Count - 1
            Me.Entries(index).SectionID = index + 1
        Next
        Dim srtBuilder As New StringBuilder
        For Each objEntry In Me.Entries
            srtBuilder.AppendLine(objEntry.ToString)
        Next
        Return srtBuilder.ToString
    End Function
End Class
