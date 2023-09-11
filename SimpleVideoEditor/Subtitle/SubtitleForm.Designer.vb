<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SubtitleForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SubtitleForm))
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.ctlSubtitleSeeker = New SimpleVideoEditor.VideoSeeker()
        Me.txtEditor = New SimpleVideoEditor.RichTextBoxPlus()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'btnDelete
        '
        Me.btnDelete.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnDelete.Font = New System.Drawing.Font("Microsoft Sans Serif", 24.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnDelete.Image = Global.SimpleVideoEditor.My.Resources.Resources.SubtitleRemove
        Me.btnDelete.Location = New System.Drawing.Point(257, 124)
        Me.btnDelete.Name = "btnDelete"
        Me.btnDelete.Size = New System.Drawing.Size(64, 64)
        Me.btnDelete.TabIndex = 22
        Me.btnDelete.UseVisualStyleBackColor = True
        '
        'ctlSubtitleSeeker
        '
        Me.ctlSubtitleSeeker.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctlSubtitleSeeker.BarBackColor = System.Drawing.Color.Blue
        Me.ctlSubtitleSeeker.BarSceneColor = System.Drawing.Color.LightSkyBlue
        Me.ctlSubtitleSeeker.Cursor = System.Windows.Forms.Cursors.Default
        Me.ctlSubtitleSeeker.HolePunches = Nothing
        Me.ctlSubtitleSeeker.Location = New System.Drawing.Point(12, 10)
        Me.ctlSubtitleSeeker.MetaData = Nothing
        Me.ctlSubtitleSeeker.Name = "ctlSubtitleSeeker"
        Me.ctlSubtitleSeeker.PreviewLocation = 0
        Me.ctlSubtitleSeeker.RangeMax = 100
        Me.ctlSubtitleSeeker.RangeMaxValue = 100
        Me.ctlSubtitleSeeker.RangeMin = 0
        Me.ctlSubtitleSeeker.RangeMinValue = 0
        Me.ctlSubtitleSeeker.SceneFrames = Nothing
        Me.ctlSubtitleSeeker.Size = New System.Drawing.Size(227, 23)
        Me.ctlSubtitleSeeker.TabIndex = 23
        '
        'txtEditor
        '
        Me.txtEditor.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtEditor.Location = New System.Drawing.Point(12, 37)
        Me.txtEditor.Name = "txtEditor"
        Me.txtEditor.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical
        Me.txtEditor.Size = New System.Drawing.Size(227, 207)
        Me.txtEditor.TabIndex = 0
        Me.txtEditor.Text = resources.GetString("txtEditor.Text")
        '
        'btnAdd
        '
        Me.btnAdd.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnAdd.Font = New System.Drawing.Font("Microsoft Sans Serif", 24.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnAdd.Image = Global.SimpleVideoEditor.My.Resources.Resources.SubtitleAdd
        Me.btnAdd.Location = New System.Drawing.Point(257, 54)
        Me.btnAdd.Name = "btnAdd"
        Me.btnAdd.Size = New System.Drawing.Size(64, 64)
        Me.btnAdd.TabIndex = 21
        Me.btnAdd.UseVisualStyleBackColor = True
        '
        'btnSave
        '
        Me.btnSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSave.BackColor = System.Drawing.SystemColors.Control
        Me.btnSave.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSave.Image = Global.SimpleVideoEditor.My.Resources.Resources.Save
        Me.btnSave.Location = New System.Drawing.Point(245, 209)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(88, 35)
        Me.btnSave.TabIndex = 20
        Me.btnSave.UseVisualStyleBackColor = False
        '
        'btnBrowse
        '
        Me.btnBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Image = Global.SimpleVideoEditor.My.Resources.Resources.Folder
        Me.btnBrowse.Location = New System.Drawing.Point(244, 9)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(88, 24)
        Me.btnBrowse.TabIndex = 8
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'SubtitleForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(344, 261)
        Me.Controls.Add(Me.ctlSubtitleSeeker)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.btnBrowse)
        Me.Controls.Add(Me.txtEditor)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(360, 300)
        Me.Name = "SubtitleForm"
        Me.Text = "Simple Video Editor - Subtitles"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents txtEditor As RichTextBoxPlus
    Friend WithEvents btnBrowse As Button
    Friend WithEvents btnSave As Button
    Friend WithEvents btnAdd As Button
    Friend WithEvents btnDelete As Button
    Friend WithEvents ctlSubtitleSeeker As VideoSeeker
End Class
