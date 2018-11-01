Imports System.Text.RegularExpressions

Public Class MetaData

	Private Structure MetaData
		Dim major_brand As String
		Dim minor_version As String
		Dim compatible_brands As String
		Dim creation_time As String
		Dim location As String
		Dim location_eng As String
		Dim comAndroidVersion As String
		Dim comAndroidCaptureFps As Double
		Dim duration As String 'HH:MM:SS.ss
		Dim bitrate As Integer 'kb/s
		Dim stream0 As VideoStreamData
		Dim totalFrames As Integer
	End Structure

	Private Structure VideoStreamData
		Dim raw As String
		Dim video As String
		Dim resolution As System.Drawing.Size
		Dim framerate As Double
	End Structure

	Private mobjMetaData As New MetaData

	''' <summary>
	''' Parses metadata from a string. Generally given by "ffmpeg -i filename.ext"
	''' </summary>
	''' <param name="dataDump"></param>
	Public Sub New(ByVal dataDump As String)
		Dim newVideoData As New VideoStreamData
		dataDump = dataDump.ToLower
		Dim metaDataIndex As Integer = dataDump.ToLower.IndexOf("metadata:")
		dataDump = dataDump.Substring(metaDataIndex)
		'Get duration
		mobjMetaData.duration = Regex.Match(dataDump, "(?<=duration: )\d\d:\d\d:\d\d\.\d\d").Groups(0).Value
		'Find video stream line
		Dim streamString As String = Regex.Match(dataDump, "stream.*video.*").Groups(0).Value
		Dim resolutionString As String = Regex.Match(streamString, "(?<=, )\d*x\d*").Groups(0).Value
		newVideoData.resolution = New System.Drawing.Size(Integer.Parse(resolutionString.Split("x")(0)), Integer.Parse(resolutionString.Split("x")(1)))
		'Get framerate from "30.00 fps"
		newVideoData.framerate = Double.Parse(Regex.Match(streamString, "\d*(\.\d*)? fps").Groups(0).Value.Split(" ")(0))
		newVideoData.raw = streamString
		Dim frameRateGroups As MatchCollection = Regex.Matches(dataDump, "(?<=frame=)( )*\d*")
		mobjMetaData.totalFrames = Integer.Parse(frameRateGroups(frameRateGroups.Count - 1).Value.Trim())
		mobjMetaData.stream0 = newVideoData
		'Failed to get duration, try getting it based on framerate and total frames
		If mobjMetaData.duration.Length = 0 Then
			mobjMetaData.duration = MainForm.FormatHHMMSSss(mobjMetaData.totalFrames / newVideoData.framerate)
		End If
	End Sub

	''' <summary>
	''' Width of the video, or horizontal resolution
	''' </summary>
	Public ReadOnly Property Width As Integer
		Get
			Return mobjMetaData.stream0.resolution.Width
		End Get
	End Property

	''' <summary>
	''' Height of the video, or horizontal resolution
	''' </summary>
	Public ReadOnly Property Height As Integer
		Get
			Return mobjMetaData.stream0.resolution.Height
		End Get
	End Property

	''' <summary>
	''' Frames per second of the video, fps or framerate
	''' </summary>
	Public ReadOnly Property Framerate As Double
		Get
			Return mobjMetaData.stream0.framerate
		End Get
	End Property

	''' <summary>
	''' Duration of the video HH:MM:SS.ss
	''' </summary>
	Public ReadOnly Property Duration As String
		Get
			Return mobjMetaData.duration
		End Get
	End Property

	''' <summary>
	''' Duration of the video in seconds
	''' </summary>
	''' <returns></returns>
	Public ReadOnly Property DurationSeconds As Double
		Get
			Dim durationArray() As String = Me.Duration.Split(":")
			Return UShort.Parse(durationArray(0)) * 60 * 60 + UShort.Parse(durationArray(1)) * 60 + Double.Parse(durationArray(2))
		End Get
	End Property

	''' <summary>
	''' The total number of frames in the video, ex: 60 for a 2 second 30fps video
	''' </summary>
	Public ReadOnly Property TotalFrames As Double
		Get
			Return mobjMetaData.totalFrames
		End Get
	End Property

	''' <summary>
	''' The raw stream data given by ffmpeg for stream 0
	''' </summary>
	''' <returns></returns>
	Public ReadOnly Property StreamData As String
		Get
			Return mobjMetaData.stream0.raw
		End Get
	End Property
End Class
