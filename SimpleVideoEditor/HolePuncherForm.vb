Public Class HolePuncherForm

    Public Structure Chain
        ''' <summary>
        ''' Which video index the frame that started this chain came from
        ''' </summary>
        Public MasterVideo As Integer

        ''' <summary>
        ''' Which frame of the master video started this chain
        ''' </summary>
        Public MasterFrame As Integer

        ''' <summary>
        ''' Which frame in the slave video the chain starts on
        ''' </summary>
        Public SlaveFrame As Integer

        ''' <summary>
        ''' How long the chain is
        ''' </summary>
        Public Length As Integer

        Public Sub New(masterVideo As Integer, masterFrame As Integer, slaveFrame As Integer, length As Integer)
            Me.MasterVideo = masterVideo
            Me.MasterFrame = masterFrame
            Me.SlaveFrame = slaveFrame
            Me.Length = length
        End Sub

        Public Overrides Function ToString() As String
            Return $"Vid:{MasterVideo} F: {MasterFrame}, @ {SlaveFrame} for {Length}"
        End Function
    End Structure

    Private mlstMetaDatas As New List(Of VideoData)
    Private mobjPuncherThread As Threading.Thread
    Private mobjChainList As New List(Of List(Of Chain)) 'List of videos, each with a list of frame chains found

    Private Sub HolePuncherForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ofdVideoIn.Multiselect = True
        ofdVideoIn.Filter = "Video Files (*.*)|*.*"
        ofdVideoIn.Title = "Select Video Files"
        ofdVideoIn.AddExtension = True
        mlstMetaDatas.Clear()
        pnlSeekers.Controls.Clear()
        mobjChainList.Clear()

        Try
            Select Case ofdVideoIn.ShowDialog()
                Case DialogResult.OK
                    'Load all of them
                    For Each strVideo In ofdVideoIn.FileNames
                        mlstMetaDatas.Add(VideoData.FromFile(strVideo))
                        mobjChainList.Add(New List(Of Chain))
                    Next
                    'Add controls backwards to maintain display order
                    For videoIndex As Integer = mlstMetaDatas.Count - 1 To 0 Step -1
                        Dim newSeeker As New VideoSeeker(mlstMetaDatas(videoIndex))
                        newSeeker.Dock = DockStyle.Top
                        newSeeker.Height = 24
                        pnlSeekers.Controls.Add(newSeeker)
                    Next
                    'Set up progress bar
                    Dim maxComparisons As Integer = (mlstMetaDatas.Count * (mlstMetaDatas.Count - 1)) / 2
                    pgbProgress.Maximum = maxComparisons
                    pgbProgress.Value = 0

                    CompareScenes()

                    mobjPuncherThread = New Threading.Thread(AddressOf DetectSimilarities)
                    mobjPuncherThread.Start(mobjChainList)
            End Select
        Catch ex As Exception
            MessageBox.Show(ex.StackTrace)
        End Try
    End Sub

    Private Async Sub CompareScenes()
        For Each objVideoData In mlstMetaDatas
            'Check if there is a save file so we don't have to recalculate the scene frames
            If Not objVideoData.ReadScenesFromFile Then
                Await objVideoData.ExtractSceneChanges()
                objVideoData.SaveScenesToFile()
            End If
            For Each objControl As Control In pnlSeekers.Controls
                If objControl.GetType = GetType(VideoSeeker) Then
                    If CType(objControl, VideoSeeker).MetaData.Equals(objVideoData) Then
                        CType(objControl, VideoSeeker).SceneFrames = MainForm.CompressSceneChanges(objVideoData.SceneFrames, objControl.Width)
                    End If
                End If
            Next
        Next
    End Sub

    Private mobjHolePunches As List(Of Double())

    'Metrics
    Dim totalComparisons As Integer = 0
    Dim frameComparisons As Long = 0
    Dim skipFrameComparisons As Long = 0
    Dim reverseFrameComparisons As Long = 0
    Dim totalChainAttempts As Long = 0
    Dim totalBadChainSkips As Long = 0
    Dim skipMatches As Long = 0

    Private Sub ResetMetrics()
        totalComparisons = 0
        frameComparisons = 0
        skipFrameComparisons = 0
        reverseFrameComparisons = 0
        totalChainAttempts = 0
        totalBadChainSkips = 0
        skipMatches = 0
    End Sub

    Private Sub RefreshProgress()
        pgbProgress.Value = totalComparisons
        Me.Refresh()
    End Sub

    Delegate Sub RefreshProgressDelegate()

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        ResetMetrics()
        btnいくよ.Enabled = False
        'Set up progress bar
        Dim maxComparisons As Integer = (mlstMetaDatas.Count * (mlstMetaDatas.Count - 1)) / 2
        pgbProgress.Maximum = maxComparisons
        pgbProgress.Value = 0
        For Each objChain In mobjChainList
            objChain.Clear()
        Next
        mobjPuncherThread = New Threading.Thread(AddressOf DetectSimilarities)
        mobjPuncherThread.Start(mobjChainList)
    End Sub

    Private Sub DetectSimilarities(chainList As List(Of List(Of Chain)))
        Try
            Dim threshold As Double = numThreshold.Value / 1000
            Dim minChainLength As Integer = numMinChain.Value 'At least this many frame in a row must be similar to remove
            Dim videoList As New List(Of Double())(mlstMetaDatas.Count)
            For Each objData In mlstMetaDatas
                Dim holePunch(objData.SceneFrames.Count - 1) As Double
                videoList.Add(holePunch)
            Next


            'Compare each array to see if they have similarities
            'Loop through each video, comparing scene frames until one is within the threshold
            For masterIndex As Integer = 0 To mlstMetaDatas.Count - 1
                Dim master As Double() = mlstMetaDatas(masterIndex).SceneFrames
                'Only compare videos moving forward, so all video content will be shown at least once
                For slaveIndex As Integer = masterIndex + 1 To mlstMetaDatas.Count - 1
                    Dim slave As Double() = mlstMetaDatas(slaveIndex).SceneFrames
                    Dim slaveChains As List(Of Chain) = chainList(slaveIndex)
                    Dim chainStart As Integer = 0
                    Dim chainLength As Integer = 0
                    Dim maxChain As Integer = 0 'Longest chain found
                    Dim slaveSkip As Integer = 0 'Frame to start searching in slave video, in case a skip found a valid frame

                    'Test any existing chains

                    For priorVideoIndex As Integer = 0 To slaveIndex - 1
                        Dim priorVideoChains As List(Of Chain) = mobjChainList(priorVideoIndex)
                        For Each objChain As Chain In priorVideoChains
                            'Debug.Print($"Frame check: F{objChain.MasterFrame} V{objChain.MasterVideo} returned: {TestFrame(objChain.MasterFrame, objChain.MasterVideo, slaveIndex, mlstMetaDatas, mobjChainList).ToString}")
                        Next
                    Next

                    'Loop through frame by frame to find chains
                    For masterFrame As Integer = 0 To master.Count - 1
                        Dim masterSubframe As Integer = masterFrame 'Keeps track of chain position
                        Dim foundSufficientChain As Boolean = False
                        For slaveFrame As Integer = Math.Max(0, slaveSkip) To slave.Count - 1
                            If slaveChains.Count > 0 Then
                                For Each slaveChain As Chain In slaveChains
                                    If slaveChain.SlaveFrame = slaveFrame Then
                                        'Skip this as we already have found it
                                        slaveFrame += (slaveChain.Length - 1)
                                        Continue For
                                    End If
                                Next
                                'Passed the end, we are done
                                If slaveFrame > slave.Count - 1 Then
                                    Exit For
                                End If
                            End If
                            frameComparisons += 1
                            If master(masterSubframe).EqualsWithin(slave(slaveFrame), threshold) Then
                                If chainLength = 0 Then
                                    chainStart = slaveFrame
                                    'Check that the farthest frame would be valid, because if its not, you don't
                                    'want to waste time comparing against everything inbetween
                                    Dim masterPeek As Integer = masterSubframe + minChainLength
                                    Dim slavePeek As Integer = slaveFrame + minChainLength
                                    If masterPeek > master.Count - 1 OrElse slavePeek > slave.Count - 1 OrElse Not master(masterPeek).EqualsWithin(slave(slavePeek), threshold) Then
                                        totalBadChainSkips += 1
                                        Continue For
                                    End If
                                End If
                                chainLength += 1
                                'Last element
                                If slaveFrame = slave.Count - 1 OrElse masterSubframe >= master.Count - 1 Then
                                    If chainLength >= minChainLength Then
                                        slaveChains.Add(New Chain(masterIndex, masterFrame, chainStart, chainLength))
                                        foundSufficientChain = True
                                    End If
                                    totalChainAttempts += chainLength
                                    chainLength = 0
                                    Exit For
                                End If
                                'Found master frame, look for the next master frame
                                masterSubframe += 1
                            Else
                                'Chain finished, check if its valid length
                                If chainLength >= minChainLength Then
                                    slaveChains.Add(New Chain(masterIndex, masterFrame, chainStart, chainLength))
                                    foundSufficientChain = True
                                ElseIf chainLength > 0 Then
                                    'Chain failed, go back to the start of the chain, offset 1
                                    slaveFrame = chainStart
                                End If
                                totalChainAttempts += chainLength
                                chainLength = 0
                                masterSubframe = masterFrame
                            End If
                        Next
                        slaveSkip = 0
                        If Not foundSufficientChain Then
                            'If there is no possibility of finding a long enough chain, quit early
                            If (master.Count) - masterFrame < minChainLength Then
                                masterFrame = master.Count - 1
                                Continue For
                            End If
                            'Skip ahead by the minChainLength since that many frames away must be common
                            'if the next frames are to make a valid chain
                            Dim skipFrame As Integer = masterFrame + minChainLength
                            Dim farthestReversal As Integer = skipFrame 'Keeps track of the earliest master reversal frame found
                            Dim farthestSlaveskip As Integer = 0 'Keeps track of the location to continue chaining the reversal forwards
                            Dim farthestMasterFrame As Integer = skipFrame 'Farthest back master frame that started the chain
                            'If its valid, reverse to the start of the chain and run normally
                            For slaveFrame As Integer = 0 To slave.Count - 1
                                skipFrameComparisons += 1
                                'TODO Do not check equals here, find the match with the longest forward chain, then go backwards from there
                                'Actually that might not work, you could skip to just the end of a OP, but match something else
                                If master(skipFrame).EqualsWithin(slave(slaveFrame), threshold) Then
                                    Dim slaveOffset As Integer = 0
                                    skipMatches += 1
                                    For reverseIndex As Integer = skipFrame To masterFrame Step -1
                                        reverseFrameComparisons += 1
                                        If (slaveFrame - slaveOffset < 0) OrElse Not master(reverseIndex).EqualsWithin(slave(slaveFrame - slaveOffset), threshold) Then
                                            slaveSkip = slaveFrame + 1
                                            chainStart = (slaveFrame - slaveOffset) + 1
                                            If slaveFrame - chainStart > farthestSlaveskip - farthestReversal Then
                                                farthestReversal = chainStart
                                                farthestSlaveskip = slaveSkip
                                                farthestMasterFrame = reverseIndex + 1
                                            End If
                                            Exit For
                                        End If
                                        slaveOffset += 1
                                    Next
                                End If
                            Next
                            chainStart = farthestReversal
                            slaveSkip = farthestSlaveskip
                            chainLength = (slaveSkip - chainStart)
                            masterFrame = skipFrame 'Automaticall increments by 1 when it hits the loop "Next"
                        End If
                    Next
                    Debug.Print("Completed search from: " + masterIndex.ToString + " to " + slaveIndex.ToString)
                    Debug.Print("Cost: " + frameComparisons.ToString + " main comparisons. " + skipFrameComparisons.ToString + " skip comparisons. " + reverseFrameComparisons.ToString + " reverse comparisons.")
                    Debug.Print("Matched on " + skipMatches.ToString + " skips. With " + totalChainAttempts.ToString + " total chain attempts. Skipping " + totalBadChainSkips.ToString + " bad chain attempts.")
                    totalComparisons += 1
                    BeginInvoke(New RefreshProgressDelegate(AddressOf RefreshProgress))
                    'Assign chains to hole punches
                    For Each slaveChain As Chain In slaveChains
                        videoList(slaveIndex).FillRange(1, slaveChain.SlaveFrame, slaveChain.Length)
                    Next
                    'Update controls
                    For Each objControl As Control In pnlSeekers.Controls
                        If objControl.GetType = GetType(VideoSeeker) Then
                            If CType(objControl, VideoSeeker).MetaData.Equals(mlstMetaDatas(slaveIndex)) Then
                                CType(objControl, VideoSeeker).HolePunches = MainForm.CompressSceneChanges(videoList(slaveIndex), objControl.Width)
                            End If
                        End If
                    Next
                Next
            Next
            For Each objVid In mobjChainList
                For Each objChain In objVid
                    Debug.Print(objChain.ToString)
                Next
            Next
            BeginInvoke(New SubDelegate(AddressOf DetectionComplete))
        Catch ex As Exception
            MessageBox.Show(ex.Message + vbNewLine + ex.StackTrace)
        End Try
    End Sub

    Private Delegate Sub SubDelegate()

    Private Sub DetectionComplete()
        btnいくよ.Enabled = True
    End Sub

    Private Function TestFrame(masterFrame As Integer, masterIndex As Integer, slaveIndex As Integer, ByRef metaDatas As List(Of VideoData), ByRef objChainList As List(Of List(Of Chain))) As Boolean
        Try
            Dim chainLength = 0
            Dim chainStart = 0
            Dim master As Double() = metaDatas(masterIndex).SceneFrames
            Dim slave As Double() = metaDatas(slaveIndex).SceneFrames

            Dim threshold As Double = numThreshold.Value / 1000
            Dim minChainLength As Integer = numMinChain.Value 'At least this many frame in a row must be similar to remove
            Dim masterSubframe As Integer = masterFrame 'Keeps track of chain position
            Dim foundSufficientChain As Boolean = False
            For slaveFrame As Integer = 0 To slave.Count - 1
                If objChainList(slaveIndex).Count > 0 Then
                    For Each slaveChain As Chain In objChainList(slaveIndex)
                        If slaveChain.SlaveFrame = slaveFrame Then
                            'Skip this as we already have found it
                            slaveFrame += (slaveChain.Length - 1)
                            Continue For
                        End If
                    Next
                    'Passed the end, we are done
                    If slaveFrame > slave.Count - 1 Then
                        Exit For
                    End If
                End If
                frameComparisons += 1
                If master(masterSubframe).EqualsWithin(slave(slaveFrame), threshold) Then
                    If chainLength = 0 Then
                        chainStart = slaveFrame
                        'Check that the farthest frame would be valid, because if its not, you don't
                        'want to waste time comparing against everything inbetween
                        Dim masterPeek As Integer = masterSubframe + minChainLength
                        Dim slavePeek As Integer = slaveFrame + minChainLength
                        If masterPeek > master.Count - 1 OrElse slavePeek > slave.Count - 1 OrElse Not master(masterPeek).EqualsWithin(slave(slavePeek), threshold) Then
                            totalBadChainSkips += 1
                            Continue For
                        End If
                    End If
                    chainLength += 1
                    'Last element
                    If slaveFrame = slave.Count - 1 OrElse masterSubframe >= master.Count - 1 Then
                        If chainLength >= minChainLength Then
                            objChainList(slaveIndex).Add(New Chain(masterIndex, masterFrame, chainStart, chainLength))
                            foundSufficientChain = True
                        End If
                        totalChainAttempts += chainLength
                        chainLength = 0
                        Exit For
                    End If
                    'Found master frame, look for the next master frame
                    masterSubframe += 1
                Else
                    'Chain finished, check if its valid length
                    If chainLength >= minChainLength Then
                        objChainList(slaveIndex).Add(New Chain(masterIndex, masterFrame, chainStart, chainLength))
                        foundSufficientChain = True
                    ElseIf chainLength > 0 Then
                        'Chain failed, go back to the start of the chain, offset 1
                        slaveFrame = chainStart
                    End If
                    totalChainAttempts += chainLength
                    chainLength = 0
                    masterSubframe = masterFrame
                End If
                Dim asdf As String = "ASDF"
            Next
            Return foundSufficientChain
        Catch ex As Exception
            Return False
        End Try
    End Function

    Structure GoodChain
        Public startFrame As Integer
        Public endFrame As Integer
    End Structure

    Private Sub btnいくよ_Click(sender As Object, e As EventArgs) Handles btnいくよ.Click
        Dim blnUserInjection = My.Computer.Keyboard.CtrlKeyDown
        'Save all the stuff via ffmpeg
        Dim videoIndex As Integer = 0
        For Each objVid In mobjChainList

            'Only modify the video if chains are detected
            If objVid.Count > 0 Then
                Dim goodPortions As New List(Of GoodChain)

                Dim firstGoodFrame As Integer = If(objVid(0).SlaveFrame = 0, objVid(0).Length, 0)
                For Each objChain In objVid
                    If firstGoodFrame >= mlstMetaDatas(videoIndex).TotalFrames Then
                        Exit For
                    End If
                    'Keep track of chains that overlap so we don't duplicate unessecary video
                    If firstGoodFrame > objChain.SlaveFrame Then
                        firstGoodFrame = objChain.SlaveFrame + (objChain.Length - 1)
                        Continue For
                    End If
                    goodPortions.Add(New GoodChain With {.startFrame = firstGoodFrame, .endFrame = objChain.SlaveFrame - 1})
                    firstGoodFrame = objChain.SlaveFrame + (objChain.Length)
                Next
                If (mlstMetaDatas(videoIndex).TotalFrames - 1) - firstGoodFrame > 0 Then
                    goodPortions.Add(New GoodChain With {.startFrame = firstGoodFrame, .endFrame = mlstMetaDatas(videoIndex).TotalFrames - 1})
                End If

                Dim processInfo As New ProcessStartInfo
                processInfo.FileName = Application.StartupPath & "\ffmpeg.exe"

                processInfo.Arguments = "-i """ & mlstMetaDatas(videoIndex).FullPath & """"
                processInfo.Arguments += " -filter_complex ""[0:v]split = " & goodPortions.Count
                '-filter_complex"
                '[0:v]split = 3[vcopy1][vcopy2][vcopy3], 
                '[vcopy1] trim=10:20,setpts=PTS-STARTPTS[v1], 
                '[vcopy2] trim=30:40,setpts=PTS-STARTPTS[v2], 
                '[vcopy3] trim=60:80,setpts=PTS-STARTPTS[v3],

                '[0:a]asplit = 3[acopy1][acopy2][acopy3], 
                '[acopy1] atrim=10:20,asetpts=PTS-STARTPTS[a1], 
                '[acopy2] atrim=30:40,asetpts=PTS-STARTPTS[a2], 
                '[acopy3] atrim=60:80,asetpts=PTS-STARTPTS[a3],

                '[v1] [a1] [v2] [a2] [v3] [a3] concat=n=3:v=1:a=1[v][a]"

                'Build video trim
                For goodIndex As Integer = 0 To goodPortions.Count - 1
                    processInfo.Arguments += "[vidChunk" & goodIndex & "]"
                Next
                processInfo.Arguments += ","
                For goodIndex As Integer = 0 To goodPortions.Count - 1
                    Dim startHHMMSS As String = FormatHHMMSSm(goodPortions(goodIndex).startFrame / mlstMetaDatas(videoIndex).Framerate)
                    startHHMMSS = startHHMMSS.Insert(0, "'")
                    startHHMMSS = startHHMMSS.Insert(3, "\")
                    startHHMMSS = startHHMMSS.Insert(7, "\")
                    startHHMMSS += "'"
                    Dim endHHMMSS As String = FormatHHMMSSm((goodPortions(goodIndex).endFrame + 1) / mlstMetaDatas(videoIndex).Framerate)
                    endHHMMSS = endHHMMSS.Insert(0, "'")
                    endHHMMSS = endHHMMSS.Insert(3, "\")
                    endHHMMSS = endHHMMSS.Insert(7, "\")
                    endHHMMSS += "'"
                    '"trim='00\:01\:40.5':'00\:00\:04.20'"
                    processInfo.Arguments += "[vidChunk" & goodIndex & "] trim=" & startHHMMSS & ":" & endHHMMSS & "," & "setpts=PTS-STARTPTS[v" & goodIndex & "],"
                Next


                'Build audio trim
                processInfo.Arguments += "[0:a]asplit = " & goodPortions.Count
                For goodIndex As Integer = 0 To goodPortions.Count - 1
                    processInfo.Arguments += "[audioChunk" & goodIndex & "]"
                Next
                processInfo.Arguments += ","
                For goodIndex As Integer = 0 To goodPortions.Count - 1
                    Dim startHHMMSS As String = FormatHHMMSSm(goodPortions(goodIndex).startFrame / mlstMetaDatas(videoIndex).Framerate)
                    startHHMMSS = startHHMMSS.Insert(0, "'")
                    startHHMMSS = startHHMMSS.Insert(3, "\")
                    startHHMMSS = startHHMMSS.Insert(7, "\")
                    startHHMMSS += "'"
                    Dim endHHMMSS As String = FormatHHMMSSm((goodPortions(goodIndex).endFrame + 1) / mlstMetaDatas(videoIndex).Framerate)
                    endHHMMSS = endHHMMSS.Insert(0, "'")
                    endHHMMSS = endHHMMSS.Insert(3, "\")
                    endHHMMSS = endHHMMSS.Insert(7, "\")
                    endHHMMSS += "'"
                    '"trim='00\:01\:40.5':'00\:00\:04.20'"
                    processInfo.Arguments += "[audioChunk" & goodIndex & "] atrim=" & startHHMMSS & ":" & endHHMMSS & "," & "asetpts=PTS-STARTPTS[a" & goodIndex & "],"
                Next

                'Build concatenate
                For goodIndex As Integer = 0 To goodPortions.Count - 1
                    processInfo.Arguments += "[v" & goodIndex & "] [a" & goodIndex & "] "
                Next
                processInfo.Arguments += "concat=n=" & goodPortions.Count & ":v=1:a=1:s=1[v][a]"" -map ""[v]"" -map ""[a]"""

                'OUTPUT TO FILE
                processInfo.Arguments += " """ & MainForm.FileNameAppend(mlstMetaDatas(videoIndex).FullPath, "-SHINY") & """"
                If blnUserInjection Then
                    'Show a form where the user can modify the arguments manually
                    Dim manualEntryForm As New ManualEntryForm(processInfo.Arguments)
                    manualEntryForm.ShowDialog()
                    processInfo.Arguments = manualEntryForm.ModifiedText
                End If
                processInfo.UseShellExecute = True
                processInfo.WindowStyle = ProcessWindowStyle.Normal
                Process.Start(processInfo)
            Else
                'TODO Copy the video with no changes?
            End If
            videoIndex += 1
        Next
    End Sub
End Class

