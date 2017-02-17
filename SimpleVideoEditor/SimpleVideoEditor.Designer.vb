<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SimpleVideoEditor
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SimpleVideoEditor))
        Me.chkMute = New System.Windows.Forms.CheckBox()
        Me.ofdVideoIn = New System.Windows.Forms.OpenFileDialog()
        Me.txtFileName = New System.Windows.Forms.TextBox()
        Me.radUp = New System.Windows.Forms.RadioButton()
        Me.radRight = New System.Windows.Forms.RadioButton()
        Me.radDown = New System.Windows.Forms.RadioButton()
        Me.radLeft = New System.Windows.Forms.RadioButton()
        Me.grpRotation = New System.Windows.Forms.GroupBox()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.chkOverwriteOriginal = New System.Windows.Forms.CheckBox()
        Me.btnいくよ = New System.Windows.Forms.Button()
        Me.cmbDefinition = New System.Windows.Forms.ComboBox()
        Me.grpSettings = New System.Windows.Forms.GroupBox()
        Me.picFrame5 = New System.Windows.Forms.PictureBox()
        Me.picRangeSlider = New System.Windows.Forms.PictureBox()
        Me.picFrame4 = New System.Windows.Forms.PictureBox()
        Me.picFrame3 = New System.Windows.Forms.PictureBox()
        Me.picFrame2 = New System.Windows.Forms.PictureBox()
        Me.picFrame1 = New System.Windows.Forms.PictureBox()
        Me.picVideo = New System.Windows.Forms.PictureBox()
        Me.grpRotation.SuspendLayout()
        Me.grpSettings.SuspendLayout()
        CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picVideo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'chkMute
        '
        Me.chkMute.AutoSize = True
        Me.chkMute.Checked = True
        Me.chkMute.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkMute.Location = New System.Drawing.Point(13, 94)
        Me.chkMute.Name = "chkMute"
        Me.chkMute.Size = New System.Drawing.Size(50, 17)
        Me.chkMute.TabIndex = 0
        Me.chkMute.Text = "Mute"
        Me.chkMute.UseVisualStyleBackColor = True
        '
        'ofdVideoIn
        '
        Me.ofdVideoIn.FileName = "Video"
        '
        'txtFileName
        '
        Me.txtFileName.Enabled = False
        Me.txtFileName.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtFileName.Location = New System.Drawing.Point(11, 10)
        Me.txtFileName.Name = "txtFileName"
        Me.txtFileName.Size = New System.Drawing.Size(227, 22)
        Me.txtFileName.TabIndex = 1
        '
        'radUp
        '
        Me.radUp.AutoSize = True
        Me.radUp.Checked = True
        Me.radUp.Location = New System.Drawing.Point(23, 19)
        Me.radUp.Name = "radUp"
        Me.radUp.Size = New System.Drawing.Size(14, 13)
        Me.radUp.TabIndex = 2
        Me.radUp.TabStop = True
        Me.radUp.UseVisualStyleBackColor = True
        '
        'radRight
        '
        Me.radRight.AutoSize = True
        Me.radRight.Location = New System.Drawing.Point(37, 33)
        Me.radRight.Name = "radRight"
        Me.radRight.Size = New System.Drawing.Size(14, 13)
        Me.radRight.TabIndex = 3
        Me.radRight.UseVisualStyleBackColor = True
        '
        'radDown
        '
        Me.radDown.AutoSize = True
        Me.radDown.Location = New System.Drawing.Point(23, 47)
        Me.radDown.Name = "radDown"
        Me.radDown.Size = New System.Drawing.Size(14, 13)
        Me.radDown.TabIndex = 4
        Me.radDown.UseVisualStyleBackColor = True
        '
        'radLeft
        '
        Me.radLeft.AutoSize = True
        Me.radLeft.Location = New System.Drawing.Point(10, 33)
        Me.radLeft.Name = "radLeft"
        Me.radLeft.Size = New System.Drawing.Size(14, 13)
        Me.radLeft.TabIndex = 5
        Me.radLeft.UseVisualStyleBackColor = True
        '
        'grpRotation
        '
        Me.grpRotation.Controls.Add(Me.radLeft)
        Me.grpRotation.Controls.Add(Me.radRight)
        Me.grpRotation.Controls.Add(Me.radDown)
        Me.grpRotation.Controls.Add(Me.radUp)
        Me.grpRotation.Location = New System.Drawing.Point(13, 19)
        Me.grpRotation.Name = "grpRotation"
        Me.grpRotation.Size = New System.Drawing.Size(61, 69)
        Me.grpRotation.TabIndex = 6
        Me.grpRotation.TabStop = False
        Me.grpRotation.Text = "Rotation"
        '
        'btnBrowse
        '
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Location = New System.Drawing.Point(244, 9)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(88, 24)
        Me.btnBrowse.TabIndex = 7
        Me.btnBrowse.Text = "Browse..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'chkOverwriteOriginal
        '
        Me.chkOverwriteOriginal.AutoSize = True
        Me.chkOverwriteOriginal.Location = New System.Drawing.Point(13, 117)
        Me.chkOverwriteOriginal.Name = "chkOverwriteOriginal"
        Me.chkOverwriteOriginal.Size = New System.Drawing.Size(71, 17)
        Me.chkOverwriteOriginal.TabIndex = 8
        Me.chkOverwriteOriginal.Text = "Overwrite"
        Me.chkOverwriteOriginal.UseVisualStyleBackColor = True
        '
        'btnいくよ
        '
        Me.btnいくよ.BackColor = System.Drawing.SystemColors.Control
        Me.btnいくよ.Enabled = False
        Me.btnいくよ.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnいくよ.Location = New System.Drawing.Point(244, 214)
        Me.btnいくよ.Name = "btnいくよ"
        Me.btnいくよ.Size = New System.Drawing.Size(87, 35)
        Me.btnいくよ.TabIndex = 9
        Me.btnいくよ.Text = "いくよ!!"
        Me.btnいくよ.UseVisualStyleBackColor = False
        '
        'cmbDefinition
        '
        Me.cmbDefinition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbDefinition.FormattingEnabled = True
        Me.cmbDefinition.Items.AddRange(New Object() {"Original", "240p", "360p", "480p", "720p", "1080p"})
        Me.cmbDefinition.Location = New System.Drawing.Point(6, 140)
        Me.cmbDefinition.Name = "cmbDefinition"
        Me.cmbDefinition.Size = New System.Drawing.Size(75, 21)
        Me.cmbDefinition.TabIndex = 17
        '
        'grpSettings
        '
        Me.grpSettings.Controls.Add(Me.cmbDefinition)
        Me.grpSettings.Controls.Add(Me.chkOverwriteOriginal)
        Me.grpSettings.Controls.Add(Me.chkMute)
        Me.grpSettings.Controls.Add(Me.grpRotation)
        Me.grpSettings.Location = New System.Drawing.Point(244, 34)
        Me.grpSettings.Name = "grpSettings"
        Me.grpSettings.Size = New System.Drawing.Size(87, 175)
        Me.grpSettings.TabIndex = 19
        Me.grpSettings.TabStop = False
        Me.grpSettings.Text = "Settings"
        '
        'picFrame5
        '
        Me.picFrame5.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame5.Location = New System.Drawing.Point(196, 215)
        Me.picFrame5.Name = "picFrame5"
        Me.picFrame5.Size = New System.Drawing.Size(42, 33)
        Me.picFrame5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picFrame5.TabIndex = 20
        Me.picFrame5.TabStop = False
        '
        'picRangeSlider
        '
        Me.picRangeSlider.Enabled = False
        Me.picRangeSlider.Location = New System.Drawing.Point(11, 34)
        Me.picRangeSlider.Name = "picRangeSlider"
        Me.picRangeSlider.Size = New System.Drawing.Size(227, 17)
        Me.picRangeSlider.TabIndex = 18
        Me.picRangeSlider.TabStop = False
        '
        'picFrame4
        '
        Me.picFrame4.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame4.Location = New System.Drawing.Point(150, 215)
        Me.picFrame4.Name = "picFrame4"
        Me.picFrame4.Size = New System.Drawing.Size(42, 33)
        Me.picFrame4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picFrame4.TabIndex = 15
        Me.picFrame4.TabStop = False
        '
        'picFrame3
        '
        Me.picFrame3.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame3.Location = New System.Drawing.Point(104, 215)
        Me.picFrame3.Name = "picFrame3"
        Me.picFrame3.Size = New System.Drawing.Size(42, 33)
        Me.picFrame3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picFrame3.TabIndex = 14
        Me.picFrame3.TabStop = False
        '
        'picFrame2
        '
        Me.picFrame2.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame2.Location = New System.Drawing.Point(58, 215)
        Me.picFrame2.Name = "picFrame2"
        Me.picFrame2.Size = New System.Drawing.Size(42, 33)
        Me.picFrame2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picFrame2.TabIndex = 13
        Me.picFrame2.TabStop = False
        '
        'picFrame1
        '
        Me.picFrame1.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame1.Location = New System.Drawing.Point(12, 215)
        Me.picFrame1.Name = "picFrame1"
        Me.picFrame1.Size = New System.Drawing.Size(42, 33)
        Me.picFrame1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picFrame1.TabIndex = 12
        Me.picFrame1.TabStop = False
        '
        'picVideo
        '
        Me.picVideo.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picVideo.Location = New System.Drawing.Point(11, 52)
        Me.picVideo.Name = "picVideo"
        Me.picVideo.Size = New System.Drawing.Size(227, 157)
        Me.picVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picVideo.TabIndex = 10
        Me.picVideo.TabStop = False
        '
        'SimpleVideoEditor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(344, 261)
        Me.Controls.Add(Me.picFrame5)
        Me.Controls.Add(Me.btnいくよ)
        Me.Controls.Add(Me.grpSettings)
        Me.Controls.Add(Me.picRangeSlider)
        Me.Controls.Add(Me.picFrame4)
        Me.Controls.Add(Me.picFrame3)
        Me.Controls.Add(Me.picFrame2)
        Me.Controls.Add(Me.picFrame1)
        Me.Controls.Add(Me.picVideo)
        Me.Controls.Add(Me.btnBrowse)
        Me.Controls.Add(Me.txtFileName)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.HelpButton = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "SimpleVideoEditor"
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.Text = "Simple Video Editor"
        Me.grpRotation.ResumeLayout(False)
        Me.grpRotation.PerformLayout()
        Me.grpSettings.ResumeLayout(False)
        Me.grpSettings.PerformLayout()
        CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picVideo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents chkMute As System.Windows.Forms.CheckBox
    Friend WithEvents ofdVideoIn As System.Windows.Forms.OpenFileDialog
    Friend WithEvents txtFileName As System.Windows.Forms.TextBox
    Friend WithEvents radUp As System.Windows.Forms.RadioButton
    Friend WithEvents radRight As System.Windows.Forms.RadioButton
    Friend WithEvents radDown As System.Windows.Forms.RadioButton
    Friend WithEvents radLeft As System.Windows.Forms.RadioButton
    Friend WithEvents grpRotation As System.Windows.Forms.GroupBox
    Friend WithEvents btnBrowse As System.Windows.Forms.Button
    Friend WithEvents chkOverwriteOriginal As System.Windows.Forms.CheckBox
    Friend WithEvents btnいくよ As System.Windows.Forms.Button
    Friend WithEvents picVideo As System.Windows.Forms.PictureBox
    Friend WithEvents picFrame1 As System.Windows.Forms.PictureBox
    Friend WithEvents picFrame2 As System.Windows.Forms.PictureBox
    Friend WithEvents picFrame3 As System.Windows.Forms.PictureBox
    Friend WithEvents picFrame4 As System.Windows.Forms.PictureBox
    Friend WithEvents cmbDefinition As System.Windows.Forms.ComboBox
    Friend WithEvents picRangeSlider As System.Windows.Forms.PictureBox
    Friend WithEvents grpSettings As System.Windows.Forms.GroupBox
    Friend WithEvents picFrame5 As System.Windows.Forms.PictureBox

End Class
