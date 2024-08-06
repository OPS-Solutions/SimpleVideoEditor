Imports System.Text
Imports System.Text.RegularExpressions

Public Class SubEntry
    Public SectionID As Integer
    Public StartTime As TimeSpan
    Public EndTime As TimeSpan
    Public Text As String
    Public SeedString As String

    Public Shared Function FromString(sectionText As String) As SubEntry
        Try
            Dim sectionLines() As String = Regex.Split(sectionText, "\r\n|\n|\r")
            Dim id As Integer = Integer.Parse(sectionLines(0).Trim)
            Dim timeRegex As New RegularExpressions.Regex("(\d*):(\d*):(\d*),(\d\d\d) --> (\d*):(\d*):(\d*),(\d\d\d)")
            Dim timeMatch As RegularExpressions.Match = timeRegex.Match(sectionLines(1).Trim)

            Dim startTime As New TimeSpan(0, Integer.Parse(timeMatch.Groups(1).Value), Integer.Parse(timeMatch.Groups(2).Value), Integer.Parse(timeMatch.Groups(3).Value), Integer.Parse(timeMatch.Groups(4).Value))
            Dim endTime As New TimeSpan(0, Integer.Parse(timeMatch.Groups(5).Value), Integer.Parse(timeMatch.Groups(6).Value), Integer.Parse(timeMatch.Groups(7).Value), Integer.Parse(timeMatch.Groups(8).Value))
            Dim fullText As New StringBuilder
            If sectionLines.Count > 2 Then
                For index As Integer = 2 To sectionLines.Count - 1
                    fullText.AppendLine(sectionLines(index))
                Next
            End If
            Return New SubEntry With {
            .SectionID = id,
            .StartTime = startTime,
            .EndTime = endTime,
            .Text = If(sectionLines.Count > 2, fullText.ToString.Trim, ""),
            .SeedString = sectionText.Replace(vbCrLf, vbLf)
            }
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Overrides Function ToString() As String
        Dim subBuilder As New StringBuilder
        subBuilder.AppendLine(SectionID)
        subBuilder.AppendLine($"{StartTime.ToString("hh\:mm\:ss\,fff")} --> {EndTime.ToString("hh\:mm\:ss\,fff")}")
        subBuilder.AppendLine(Text)
        Return subBuilder.ToString()
    End Function
End Class
