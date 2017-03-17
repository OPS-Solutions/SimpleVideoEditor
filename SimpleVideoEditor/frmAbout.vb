Public NotInheritable Class frmAbout

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
End Class
