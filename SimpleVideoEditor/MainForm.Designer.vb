<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
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
		Me.components = New System.ComponentModel.Container()
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
		Me.ofdVideoIn = New System.Windows.Forms.OpenFileDialog()
		Me.lblFileName = New System.Windows.Forms.Label()
		Me.cmbDefinition = New System.Windows.Forms.ComboBox()
		Me.grpSettings = New System.Windows.Forms.GroupBox()
		Me.chkMute = New SimpleVideoEditor.ImageSwitch()
		Me.imgRotate = New System.Windows.Forms.PictureBox()
		Me.sfdVideoOut = New System.Windows.Forms.SaveFileDialog()
		Me.cmsPicVideo = New System.Windows.Forms.ContextMenuStrip(Me.components)
		Me.cmsPicVideoClear = New System.Windows.Forms.ToolStripMenuItem()
		Me.cmsPicVideoExportFrame = New System.Windows.Forms.ToolStripMenuItem()
		Me.picFrame5 = New System.Windows.Forms.PictureBox()
		Me.btnいくよ = New System.Windows.Forms.Button()
		Me.picFrame4 = New System.Windows.Forms.PictureBox()
		Me.picFrame3 = New System.Windows.Forms.PictureBox()
		Me.picFrame2 = New System.Windows.Forms.PictureBox()
		Me.picFrame1 = New System.Windows.Forms.PictureBox()
		Me.picVideo = New System.Windows.Forms.PictureBox()
		Me.btnBrowse = New System.Windows.Forms.Button()
		Me.ctlVideoSeeker = New SimpleVideoEditor.VideoSeeker()
		Me.grpSettings.SuspendLayout()
		CType(Me.chkMute, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.imgRotate, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.cmsPicVideo.SuspendLayout()
		CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.picVideo, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SuspendLayout()
		'
		'ofdVideoIn
		'
		Me.ofdVideoIn.FileName = "Video"
		'
		'lblFileName
		'
		Me.lblFileName.BackColor = System.Drawing.SystemColors.ControlLight
		Me.lblFileName.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.lblFileName.Location = New System.Drawing.Point(11, 10)
		Me.lblFileName.Name = "lblFileName"
		Me.lblFileName.Size = New System.Drawing.Size(227, 22)
		Me.lblFileName.TabIndex = 1
		'
		'cmbDefinition
		'
		Me.cmbDefinition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
		Me.cmbDefinition.FormattingEnabled = True
		Me.cmbDefinition.Items.AddRange(New Object() {"Original", "120p", "240p", "360p", "480p", "720p", "1080p"})
		Me.cmbDefinition.Location = New System.Drawing.Point(6, 140)
		Me.cmbDefinition.Name = "cmbDefinition"
		Me.cmbDefinition.Size = New System.Drawing.Size(75, 21)
		Me.cmbDefinition.TabIndex = 17
		'
		'grpSettings
		'
		Me.grpSettings.Controls.Add(Me.chkMute)
		Me.grpSettings.Controls.Add(Me.imgRotate)
		Me.grpSettings.Controls.Add(Me.cmbDefinition)
		Me.grpSettings.Location = New System.Drawing.Point(244, 39)
		Me.grpSettings.Name = "grpSettings"
		Me.grpSettings.Size = New System.Drawing.Size(87, 170)
		Me.grpSettings.TabIndex = 19
		Me.grpSettings.TabStop = False
		Me.grpSettings.Text = "Settings"
		'
		'chkMute
		'
		Me.chkMute.Checked = True
		Me.chkMute.Cursor = System.Windows.Forms.Cursors.Hand
		Me.chkMute.FalseImage = Global.SimpleVideoEditor.My.Resources.Resources.SpeakerOn
		Me.chkMute.Image = Global.SimpleVideoEditor.My.Resources.Resources.SpeakerOff
		Me.chkMute.Location = New System.Drawing.Point(52, 28)
		Me.chkMute.Name = "chkMute"
		Me.chkMute.Size = New System.Drawing.Size(18, 18)
		Me.chkMute.TabIndex = 21
		Me.chkMute.TabStop = False
		Me.chkMute.TrueImage = Global.SimpleVideoEditor.My.Resources.Resources.SpeakerOff
		'
		'imgRotate
		'
		Me.imgRotate.Cursor = System.Windows.Forms.Cursors.Hand
		Me.imgRotate.Image = Global.SimpleVideoEditor.My.Resources.Resources.Rotate
		Me.imgRotate.Location = New System.Drawing.Point(17, 28)
		Me.imgRotate.Margin = New System.Windows.Forms.Padding(0)
		Me.imgRotate.Name = "imgRotate"
		Me.imgRotate.Size = New System.Drawing.Size(18, 18)
		Me.imgRotate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
		Me.imgRotate.TabIndex = 20
		Me.imgRotate.TabStop = False
		'
		'sfdVideoOut
		'
		'
		'cmsPicVideo
		'
		Me.cmsPicVideo.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.cmsPicVideoClear, Me.cmsPicVideoExportFrame})
		Me.cmsPicVideo.Name = "cmsPicVideo"
		Me.cmsPicVideo.Size = New System.Drawing.Size(144, 48)
		'
		'cmsPicVideoClear
		'
		Me.cmsPicVideoClear.Image = Global.SimpleVideoEditor.My.Resources.Resources.Eraser
		Me.cmsPicVideoClear.Name = "cmsPicVideoClear"
		Me.cmsPicVideoClear.Size = New System.Drawing.Size(143, 22)
		Me.cmsPicVideoClear.Text = "Clear"
		'
		'cmsPicVideoExportFrame
		'
		Me.cmsPicVideoExportFrame.Image = Global.SimpleVideoEditor.My.Resources.Resources.Picture
		Me.cmsPicVideoExportFrame.Name = "cmsPicVideoExportFrame"
		Me.cmsPicVideoExportFrame.Size = New System.Drawing.Size(143, 22)
		Me.cmsPicVideoExportFrame.Text = "Export Frame"
		'
		'picFrame5
		'
		Me.picFrame5.BackColor = System.Drawing.SystemColors.ControlDark
		Me.picFrame5.Cursor = System.Windows.Forms.Cursors.Hand
		Me.picFrame5.Location = New System.Drawing.Point(196, 215)
		Me.picFrame5.Name = "picFrame5"
		Me.picFrame5.Size = New System.Drawing.Size(42, 33)
		Me.picFrame5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
		Me.picFrame5.TabIndex = 20
		Me.picFrame5.TabStop = False
		'
		'btnいくよ
		'
		Me.btnいくよ.BackColor = System.Drawing.SystemColors.Control
		Me.btnいくよ.Enabled = False
		Me.btnいくよ.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.btnいくよ.Image = Global.SimpleVideoEditor.My.Resources.Resources.Save
		Me.btnいくよ.Location = New System.Drawing.Point(244, 214)
		Me.btnいくよ.Name = "btnいくよ"
		Me.btnいくよ.Size = New System.Drawing.Size(88, 35)
		Me.btnいくよ.TabIndex = 9
		Me.btnいくよ.UseVisualStyleBackColor = False
		'
		'picFrame4
		'
		Me.picFrame4.BackColor = System.Drawing.SystemColors.ControlDark
		Me.picFrame4.Cursor = System.Windows.Forms.Cursors.Hand
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
		Me.picFrame3.Cursor = System.Windows.Forms.Cursors.Hand
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
		Me.picFrame2.Cursor = System.Windows.Forms.Cursors.Hand
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
		Me.picFrame1.Cursor = System.Windows.Forms.Cursors.Hand
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
		Me.picVideo.ContextMenuStrip = Me.cmsPicVideo
		Me.picVideo.Cursor = System.Windows.Forms.Cursors.Cross
		Me.picVideo.Location = New System.Drawing.Point(11, 52)
		Me.picVideo.Name = "picVideo"
		Me.picVideo.Size = New System.Drawing.Size(227, 157)
		Me.picVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
		Me.picVideo.TabIndex = 10
		Me.picVideo.TabStop = False
		'
		'btnBrowse
		'
		Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.btnBrowse.Image = Global.SimpleVideoEditor.My.Resources.Resources.Folder
		Me.btnBrowse.Location = New System.Drawing.Point(244, 9)
		Me.btnBrowse.Name = "btnBrowse"
		Me.btnBrowse.Size = New System.Drawing.Size(88, 24)
		Me.btnBrowse.TabIndex = 7
		Me.btnBrowse.UseVisualStyleBackColor = True
		'
		'ctlVideoSeeker
		'
		Me.ctlVideoSeeker.Cursor = System.Windows.Forms.Cursors.Arrow
		Me.ctlVideoSeeker.Enabled = False
		Me.ctlVideoSeeker.Location = New System.Drawing.Point(11, 33)
		Me.ctlVideoSeeker.Name = "ctlVideoSeeker"
		Me.ctlVideoSeeker.RangeMax = 100
		Me.ctlVideoSeeker.RangeMaxValue = 100
		Me.ctlVideoSeeker.RangeMin = 0
		Me.ctlVideoSeeker.RangeMinValue = 0
		Me.ctlVideoSeeker.RangeValues = New Integer() {0, 100}
		Me.ctlVideoSeeker.SceneFrames = Nothing
		Me.ctlVideoSeeker.Size = New System.Drawing.Size(227, 18)
		Me.ctlVideoSeeker.TabIndex = 21
		'
		'MainForm
		'
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
		Me.ClientSize = New System.Drawing.Size(344, 261)
		Me.Controls.Add(Me.ctlVideoSeeker)
		Me.Controls.Add(Me.picFrame5)
		Me.Controls.Add(Me.btnいくよ)
		Me.Controls.Add(Me.grpSettings)
		Me.Controls.Add(Me.picFrame4)
		Me.Controls.Add(Me.picFrame3)
		Me.Controls.Add(Me.picFrame2)
		Me.Controls.Add(Me.picFrame1)
		Me.Controls.Add(Me.picVideo)
		Me.Controls.Add(Me.btnBrowse)
		Me.Controls.Add(Me.lblFileName)
		Me.ForeColor = System.Drawing.SystemColors.ControlText
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.HelpButton = True
		Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
		Me.KeyPreview = True
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.Name = "MainForm"
		Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
		Me.Text = "Simple Video Editor - Open Source"
		Me.grpSettings.ResumeLayout(False)
		CType(Me.chkMute, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.imgRotate, System.ComponentModel.ISupportInitialize).EndInit()
		Me.cmsPicVideo.ResumeLayout(False)
		CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.picVideo, System.ComponentModel.ISupportInitialize).EndInit()
		Me.ResumeLayout(False)

	End Sub
	Friend WithEvents ofdVideoIn As System.Windows.Forms.OpenFileDialog
	Friend WithEvents lblFileName As System.Windows.Forms.Label
	Friend WithEvents btnBrowse As System.Windows.Forms.Button
	Friend WithEvents btnいくよ As System.Windows.Forms.Button
	Friend WithEvents picVideo As System.Windows.Forms.PictureBox
	Friend WithEvents picFrame1 As System.Windows.Forms.PictureBox
	Friend WithEvents picFrame2 As System.Windows.Forms.PictureBox
	Friend WithEvents picFrame3 As System.Windows.Forms.PictureBox
	Friend WithEvents picFrame4 As System.Windows.Forms.PictureBox
	Friend WithEvents cmbDefinition As System.Windows.Forms.ComboBox
	Friend WithEvents grpSettings As System.Windows.Forms.GroupBox
	Friend WithEvents picFrame5 As System.Windows.Forms.PictureBox
	Friend WithEvents sfdVideoOut As System.Windows.Forms.SaveFileDialog
	Friend WithEvents cmsPicVideo As ContextMenuStrip
	Friend WithEvents cmsPicVideoClear As ToolStripMenuItem
	Friend WithEvents cmsPicVideoExportFrame As ToolStripMenuItem
	Friend WithEvents ctlVideoSeeker As VideoSeeker
	Friend WithEvents imgRotate As PictureBox
	Friend WithEvents chkMute As ImageSwitch
End Class
