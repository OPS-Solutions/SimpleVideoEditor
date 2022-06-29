Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.IO.Compression

Public NotInheritable Class AboutForm

	Private mstrReleasePage As String
	Private mstrLatestVersion As String

	Private Sub frmAbout_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
		' Set the title of the form.
		Dim ApplicationTitle As String
		If My.Application.Info.Title <> "" Then
			ApplicationTitle = My.Application.Info.Title
		Else
			ApplicationTitle = System.IO.Path.GetFileNameWithoutExtension(My.Application.Info.AssemblyName)
		End If
		Me.Text = String.Format("About {0}", ApplicationTitle)
		' Initialize all of the text displayed on the About Box.
		Me.LabelProductName.Text = My.Application.Info.ProductName
		Me.LabelVersion.Text = String.Format("Version {0}", My.Application.Info.Version.ToString)
		Me.LabelCopyright.Text = My.Application.Info.Copyright
		Me.LabelCompanyName.Text = My.Application.Info.CompanyName
		Me.TextBoxDescription.Text = My.Application.Info.Description
		'Assure the link is clickable
		If Not Me.lblGithubLink.Links.Count > 0 Then
			Me.lblGithubLink.Links.Add(New LinkLabel.Link(0, lblGithubLink.Text.Length, "https://www.github.com/OPS-Solutions/SimpleVideoEditor"))
		Else
			Me.lblGithubLink.Links(0) = (New LinkLabel.Link(0, lblGithubLink.Text.Length, "https://www.github.com/OPS-Solutions/SimpleVideoEditor"))
		End If
		Dim badExePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly.Location) + "\DeletableSimpleVideoEditor.exe"
		If System.IO.File.Exists(badExePath) Then
			RefreshUpdateButton("Restart.", True)
		Else
			ThreadPool.QueueUserWorkItem(Sub()
											 RetrieveCurrentVersion()
										 End Sub)
		End If
	End Sub

	Private Sub RefreshUpdateInfo()
		If Me.InvokeRequired Then
			Me.BeginInvoke(Sub() RefreshUpdateInfo())
		Else
			Try
				Dim versionTagRegex As New Regex("(?<=\/OPS-Solutions\/SimpleVideoEditor\/releases\/tag\/)(\d+?\.\d+?\.\d+?\.\d+)")
				Dim allMatches As MatchCollection = versionTagRegex.Matches(mstrReleasePage)
				If allMatches.Count > 0 Then
					mstrLatestVersion = allMatches(0).Value
					lblLatestVersion.Text = $"Latest Version: {allMatches(0).Value}"
                    If My.Application.Info.Version.ToString.CompareNatural(allMatches(0).Value) < 0 Then
                        btnUpdate.Enabled = True
                        btnUpdate.Text = "Update"
                    Else
                        btnUpdate.Enabled = False
						btnUpdate.Text = "Up to date"
					End If
				Else
					lblLatestVersion.Text = "Failed to connect."
					btnUpdate.Enabled = False
					btnUpdate.Text = "No connection"
				End If
			Catch ex As Exception
				lblLatestVersion.Text = ""
				btnUpdate.Enabled = False
				btnUpdate.Text = ""
			End Try
		End If
	End Sub

	Private Sub RetrieveCurrentVersion()
		Try
			mstrReleasePage = ""
			Dim versionTagRegex As New Regex("(?<=\/OPS-Solutions\/SimpleVideoEditor\/releases\/tag\/)(\d+?\.\d+?\.\d+?\.\d+)")
			'Check github for latest version
			ServicePointManager.Expect100Continue = True
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
			mstrReleasePage = New System.Net.WebClient().DownloadString("https://github.com/OPS-Solutions/SimpleVideoEditor/releases")

			RefreshUpdateInfo()
		Catch ex As Exception
			RefreshUpdateInfo()
		End Try
	End Sub

	Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
		Me.Close()
	End Sub

	''' <summary>
	''' Starts a process to open the link
	''' </summary>
	Private Sub lblGithubLink_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lblGithubLink.LinkClicked
		System.Diagnostics.Process.Start(e.Link.LinkData.ToString())
	End Sub

	Private Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
		If btnUpdate.Text.Contains("Restart") Then
			Process.Start("SimpleVideoEditor.exe")
			End
		Else
			ThreadPool.QueueUserWorkItem(Sub()
											 DownloadUpdate()
										 End Sub)
		End If
	End Sub

	Private Sub DownloadUpdate()
		Try
			'Download latest release zip
			Dim remoteUri As String = $"https://github.com/OPS-Solutions/SimpleVideoEditor/releases/download/{mstrLatestVersion}/Simple.Video.Editor.zip"
			Dim exePath As String = System.Reflection.Assembly.GetExecutingAssembly.Location
			Dim updateExtractPath As String = System.IO.Path.GetTempPath + "SimpleVideoEditorUpdateFiles"
			'Clear anything that was in there before
			If System.IO.Directory.Exists(updateExtractPath) Then
				For Each objFile In System.IO.Directory.GetFiles(updateExtractPath)
					System.IO.File.Delete(objFile)
				Next
				System.IO.Directory.Delete(updateExtractPath)
			End If
			System.IO.Directory.CreateDirectory(updateExtractPath)
			Dim downloadedZipPath As String = updateExtractPath + "\" + "Simple.Video.Editor.zip"
			Dim downloadBarrier As New Barrier(2)
			ThreadPool.QueueUserWorkItem(Sub()
											 Dim dotCount As Integer = 1
											 While downloadBarrier.ParticipantsRemaining > 1
												 RefreshUpdateButton($"Downloading{String.Join("", Enumerable.Repeat(Of String)(".", dotCount))}")
												 Threading.Thread.Sleep(300)
												 dotCount = (dotCount Mod 3) + 1
											 End While
											 downloadBarrier.SignalAndWait()
										 End Sub)
			Try
				Using client As New WebClient()
					client.DownloadFile(remoteUri, downloadedZipPath) 'Overwrites whatever is already there
				End Using
			Catch ex As Exception
				Throw ex
			Finally
				downloadBarrier.SignalAndWait()
			End Try
			RefreshUpdateButton("Extracting...")

			'Use shell32 unzip to avoid needing .net 4.5 System.IO.Compression.dll
			Dim doubleBarrier As New Threading.Barrier(2)
			Dim unzipThread As New Thread(Sub()
											  Dim objShell As New Shell32.Shell
											  Dim targetDir As Shell32.Folder = objShell.NameSpace(updateExtractPath)
											  Dim zipFolder As Shell32.Folder = objShell.NameSpace(downloadedZipPath)
											  For index As Integer = 0 To zipFolder.Items.Count - 1
												  RefreshUpdateButton($"Extracting({index + 1}/{zipFolder.Items.Count})")
												  targetDir.CopyHere(zipFolder.Items(index), 0)
											  Next
											  doubleBarrier.SignalAndWait()
										  End Sub)
			unzipThread.SetApartmentState(ApartmentState.STA)
			unzipThread.Start()
			doubleBarrier.SignalAndWait()

			System.IO.File.Delete(downloadedZipPath)
			'Must rename the current running .exe because otherwise we can't put anything there
			RefreshUpdateButton("Renaming...")
			System.IO.File.Move(exePath, System.IO.Path.GetDirectoryName(exePath) + "\DeletableSimpleVideoEditor.exe")
			RefreshUpdateButton("Replacing...")
			For Each objFile In System.IO.Directory.EnumerateFiles(updateExtractPath)
				System.IO.File.Copy(objFile, System.IO.Path.GetDirectoryName(exePath) + "\" + System.IO.Path.GetFileName(objFile), True)
				System.IO.File.Delete(objFile)
			Next
			System.IO.Directory.Delete(updateExtractPath)
			RefreshUpdateButton("Restart", True)
		Catch ex As Exception
			RefreshUpdateButton("Error", False)
			Me.Invoke(Sub()
						  MessageBox.Show(Me, ex.Message, "Error While Updating", MessageBoxButtons.OK, MessageBoxIcon.Error)
					  End Sub)
		End Try
	End Sub

	Private Sub RefreshUpdateButton(strStatus As String, Optional enable As Boolean = False)
		If Me.InvokeRequired Then
			Me.BeginInvoke(Sub() RefreshUpdateButton(strStatus, enable))
		Else
			btnUpdate.Text = strStatus
			btnUpdate.Enabled = enable
		End If
	End Sub

	Private Sub tmrButtonFlicker_Tick(sender As Object, e As EventArgs) Handles tmrButtonFlicker.Tick
		If btnUpdate.Text.Contains("Restart") Then
			If btnUpdate.BackColor = Color.Yellow Then
				btnUpdate.BackColor = Color.FromArgb(240, 240, 240)
			Else
				btnUpdate.BackColor = Color.Yellow
			End If
		Else
			btnUpdate.BackColor = Color.FromArgb(240, 240, 240)
		End If
	End Sub
End Class
