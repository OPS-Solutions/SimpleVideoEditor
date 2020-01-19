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
        Me.cmsFrameRate = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.DefaultToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TenFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FifteenFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TwentyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ThirtyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SixtyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.grpSettings = New System.Windows.Forms.GroupBox()
        Me.chkQuality = New SimpleVideoEditor.ImageSwitch()
        Me.picPlaybackSpeed = New System.Windows.Forms.PictureBox()
        Me.picChromaKey = New System.Windows.Forms.PictureBox()
        Me.chkDeleteDuplicates = New SimpleVideoEditor.ImageSwitch()
        Me.chkMute = New SimpleVideoEditor.ImageSwitch()
        Me.cmsPlaybackVolume = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.MuteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem10 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem11 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem12 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem13 = New System.Windows.Forms.ToolStripMenuItem()
        Me.UnmuteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.imgRotate = New System.Windows.Forms.PictureBox()
        Me.sfdVideoOut = New System.Windows.Forms.SaveFileDialog()
        Me.cmsPicVideo = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.cmsPicVideoClear = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsPicVideoExportFrame = New System.Windows.Forms.ToolStripMenuItem()
        Me.dlgChromaColor = New System.Windows.Forms.ColorDialog()
        Me.cmsPlaybackSpeed = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.ToolStripMenuItem2 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem3 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem4 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem5 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem6 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem7 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem8 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem9 = New System.Windows.Forms.ToolStripMenuItem()
        Me.picFrame5 = New System.Windows.Forms.PictureBox()
        Me.btnいくよ = New System.Windows.Forms.Button()
        Me.picFrame4 = New System.Windows.Forms.PictureBox()
        Me.picFrame3 = New System.Windows.Forms.PictureBox()
        Me.picFrame2 = New System.Windows.Forms.PictureBox()
        Me.picFrame1 = New System.Windows.Forms.PictureBox()
        Me.picVideo = New System.Windows.Forms.PictureBox()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.cmsVideoSeeker = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.CacheAllFramesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ctlVideoSeeker = New SimpleVideoEditor.VideoSeeker()
        Me.cmsBrowse = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.HolePuncherToolToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsFrameRate.SuspendLayout()
        Me.grpSettings.SuspendLayout()
        CType(Me.chkQuality, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picPlaybackSpeed, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picChromaKey, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.chkDeleteDuplicates, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.chkMute, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.cmsPlaybackVolume.SuspendLayout()
        CType(Me.imgRotate, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.cmsPicVideo.SuspendLayout()
        Me.cmsPlaybackSpeed.SuspendLayout()
        CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picVideo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.cmsVideoSeeker.SuspendLayout()
        Me.cmsBrowse.SuspendLayout()
        Me.SuspendLayout()
        '
        'ofdVideoIn
        '
        Me.ofdVideoIn.FileName = "Video"
        '
        'lblFileName
        '
        Me.lblFileName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblFileName.BackColor = System.Drawing.SystemColors.ControlLight
        Me.lblFileName.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblFileName.Location = New System.Drawing.Point(11, 10)
        Me.lblFileName.Name = "lblFileName"
        Me.lblFileName.Size = New System.Drawing.Size(227, 22)
        Me.lblFileName.TabIndex = 1
        '
        'cmbDefinition
        '
        Me.cmbDefinition.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmbDefinition.ContextMenuStrip = Me.cmsFrameRate
        Me.cmbDefinition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbDefinition.FormattingEnabled = True
        Me.cmbDefinition.Items.AddRange(New Object() {"Original", "120p", "240p", "360p", "480p", "720p", "1080p"})
        Me.cmbDefinition.Location = New System.Drawing.Point(6, 140)
        Me.cmbDefinition.Name = "cmbDefinition"
        Me.cmbDefinition.Size = New System.Drawing.Size(75, 21)
        Me.cmbDefinition.TabIndex = 9
        '
        'cmsFrameRate
        '
        Me.cmsFrameRate.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.DefaultToolStripMenuItem, Me.TenFPSToolStripMenuItem, Me.FifteenFPSToolStripMenuItem, Me.TwentyFPSToolStripMenuItem, Me.ThirtyFPSToolStripMenuItem, Me.SixtyFPSToolStripMenuItem})
        Me.cmsFrameRate.Name = "cmsFrameRate"
        Me.cmsFrameRate.ShowCheckMargin = True
        Me.cmsFrameRate.ShowImageMargin = False
        Me.cmsFrameRate.Size = New System.Drawing.Size(113, 136)
        '
        'DefaultToolStripMenuItem
        '
        Me.DefaultToolStripMenuItem.Checked = True
        Me.DefaultToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.DefaultToolStripMenuItem.Name = "DefaultToolStripMenuItem"
        Me.DefaultToolStripMenuItem.Size = New System.Drawing.Size(112, 22)
        Me.DefaultToolStripMenuItem.Text = "Default"
        '
        'TenFPSToolStripMenuItem
        '
        Me.TenFPSToolStripMenuItem.Name = "TenFPSToolStripMenuItem"
        Me.TenFPSToolStripMenuItem.Size = New System.Drawing.Size(112, 22)
        Me.TenFPSToolStripMenuItem.Text = "10 FPS"
        '
        'FifteenFPSToolStripMenuItem
        '
        Me.FifteenFPSToolStripMenuItem.Name = "FifteenFPSToolStripMenuItem"
        Me.FifteenFPSToolStripMenuItem.Size = New System.Drawing.Size(112, 22)
        Me.FifteenFPSToolStripMenuItem.Text = "15 FPS"
        '
        'TwentyFPSToolStripMenuItem
        '
        Me.TwentyFPSToolStripMenuItem.Name = "TwentyFPSToolStripMenuItem"
        Me.TwentyFPSToolStripMenuItem.Size = New System.Drawing.Size(112, 22)
        Me.TwentyFPSToolStripMenuItem.Text = "20 FPS"
        '
        'ThirtyFPSToolStripMenuItem
        '
        Me.ThirtyFPSToolStripMenuItem.Name = "ThirtyFPSToolStripMenuItem"
        Me.ThirtyFPSToolStripMenuItem.Size = New System.Drawing.Size(112, 22)
        Me.ThirtyFPSToolStripMenuItem.Text = "30 FPS"
        '
        'SixtyFPSToolStripMenuItem
        '
        Me.SixtyFPSToolStripMenuItem.Name = "SixtyFPSToolStripMenuItem"
        Me.SixtyFPSToolStripMenuItem.Size = New System.Drawing.Size(112, 22)
        Me.SixtyFPSToolStripMenuItem.Text = "60 FPS"
        '
        'grpSettings
        '
        Me.grpSettings.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpSettings.Controls.Add(Me.chkQuality)
        Me.grpSettings.Controls.Add(Me.picPlaybackSpeed)
        Me.grpSettings.Controls.Add(Me.picChromaKey)
        Me.grpSettings.Controls.Add(Me.chkDeleteDuplicates)
        Me.grpSettings.Controls.Add(Me.chkMute)
        Me.grpSettings.Controls.Add(Me.imgRotate)
        Me.grpSettings.Controls.Add(Me.cmbDefinition)
        Me.grpSettings.Location = New System.Drawing.Point(244, 39)
        Me.grpSettings.Name = "grpSettings"
        Me.grpSettings.Size = New System.Drawing.Size(87, 170)
        Me.grpSettings.TabIndex = 17
        Me.grpSettings.TabStop = False
        Me.grpSettings.Text = "Settings"
        '
        'chkQuality
        '
        Me.chkQuality.Checked = False
        Me.chkQuality.Cursor = System.Windows.Forms.Cursors.Hand
        Me.chkQuality.FalseImage = Global.SimpleVideoEditor.My.Resources.Resources.qscaleOff
        Me.chkQuality.Image = Global.SimpleVideoEditor.My.Resources.Resources.qscaleOff
        Me.chkQuality.Location = New System.Drawing.Point(52, 102)
        Me.chkQuality.Name = "chkQuality"
        Me.chkQuality.Size = New System.Drawing.Size(18, 18)
        Me.chkQuality.TabIndex = 25
        Me.chkQuality.TabStop = False
        Me.chkQuality.TrueImage = Global.SimpleVideoEditor.My.Resources.Resources.qscaleOn
        '
        'picPlaybackSpeed
        '
        Me.picPlaybackSpeed.Cursor = System.Windows.Forms.Cursors.Hand
        Me.picPlaybackSpeed.Image = Global.SimpleVideoEditor.My.Resources.Resources.StopWatch
        Me.picPlaybackSpeed.Location = New System.Drawing.Point(52, 65)
        Me.picPlaybackSpeed.Margin = New System.Windows.Forms.Padding(0)
        Me.picPlaybackSpeed.Name = "picPlaybackSpeed"
        Me.picPlaybackSpeed.Size = New System.Drawing.Size(18, 18)
        Me.picPlaybackSpeed.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
        Me.picPlaybackSpeed.TabIndex = 24
        Me.picPlaybackSpeed.TabStop = False
        '
        'picChromaKey
        '
        Me.picChromaKey.BackColor = System.Drawing.Color.Lime
        Me.picChromaKey.Enabled = False
        Me.picChromaKey.Image = Global.SimpleVideoEditor.My.Resources.Resources.ChromaKey
        Me.picChromaKey.Location = New System.Drawing.Point(17, 102)
        Me.picChromaKey.Name = "picChromaKey"
        Me.picChromaKey.Size = New System.Drawing.Size(18, 18)
        Me.picChromaKey.TabIndex = 23
        Me.picChromaKey.TabStop = False
        Me.picChromaKey.Visible = False
        '
        'chkDeleteDuplicates
        '
        Me.chkDeleteDuplicates.Checked = False
        Me.chkDeleteDuplicates.Cursor = System.Windows.Forms.Cursors.Hand
        Me.chkDeleteDuplicates.FalseImage = Global.SimpleVideoEditor.My.Resources.Resources.DuplicatesOn
        Me.chkDeleteDuplicates.Image = Global.SimpleVideoEditor.My.Resources.Resources.DuplicatesOn
        Me.chkDeleteDuplicates.Location = New System.Drawing.Point(17, 65)
        Me.chkDeleteDuplicates.Name = "chkDeleteDuplicates"
        Me.chkDeleteDuplicates.Size = New System.Drawing.Size(18, 18)
        Me.chkDeleteDuplicates.TabIndex = 22
        Me.chkDeleteDuplicates.TabStop = False
        Me.chkDeleteDuplicates.TrueImage = Global.SimpleVideoEditor.My.Resources.Resources.DuplicatesOff
        '
        'chkMute
        '
        Me.chkMute.Checked = True
        Me.chkMute.ContextMenuStrip = Me.cmsPlaybackVolume
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
        'cmsPlaybackVolume
        '
        Me.cmsPlaybackVolume.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MuteToolStripMenuItem, Me.ToolStripMenuItem10, Me.ToolStripMenuItem11, Me.ToolStripMenuItem12, Me.ToolStripMenuItem13, Me.UnmuteToolStripMenuItem})
        Me.cmsPlaybackVolume.Name = "cmsAudioVolume"
        Me.cmsPlaybackVolume.Size = New System.Drawing.Size(129, 136)
        '
        'MuteToolStripMenuItem
        '
        Me.MuteToolStripMenuItem.Checked = True
        Me.MuteToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.MuteToolStripMenuItem.Name = "MuteToolStripMenuItem"
        Me.MuteToolStripMenuItem.Size = New System.Drawing.Size(128, 22)
        Me.MuteToolStripMenuItem.Text = "0.0 (Mute)"
        '
        'ToolStripMenuItem10
        '
        Me.ToolStripMenuItem10.Name = "ToolStripMenuItem10"
        Me.ToolStripMenuItem10.Size = New System.Drawing.Size(128, 22)
        Me.ToolStripMenuItem10.Text = "0.1"
        '
        'ToolStripMenuItem11
        '
        Me.ToolStripMenuItem11.Name = "ToolStripMenuItem11"
        Me.ToolStripMenuItem11.Size = New System.Drawing.Size(128, 22)
        Me.ToolStripMenuItem11.Text = "0.3"
        '
        'ToolStripMenuItem12
        '
        Me.ToolStripMenuItem12.Name = "ToolStripMenuItem12"
        Me.ToolStripMenuItem12.Size = New System.Drawing.Size(128, 22)
        Me.ToolStripMenuItem12.Text = "0.5"
        '
        'ToolStripMenuItem13
        '
        Me.ToolStripMenuItem13.Name = "ToolStripMenuItem13"
        Me.ToolStripMenuItem13.Size = New System.Drawing.Size(128, 22)
        Me.ToolStripMenuItem13.Text = "0.7"
        '
        'UnmuteToolStripMenuItem
        '
        Me.UnmuteToolStripMenuItem.Name = "UnmuteToolStripMenuItem"
        Me.UnmuteToolStripMenuItem.Size = New System.Drawing.Size(128, 22)
        Me.UnmuteToolStripMenuItem.Text = "1.0 (Full)"
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
        Me.cmsPicVideoExportFrame.Enabled = False
        Me.cmsPicVideoExportFrame.Image = Global.SimpleVideoEditor.My.Resources.Resources.Picture
        Me.cmsPicVideoExportFrame.Name = "cmsPicVideoExportFrame"
        Me.cmsPicVideoExportFrame.Size = New System.Drawing.Size(143, 22)
        Me.cmsPicVideoExportFrame.Text = "Export Frame"
        '
        'dlgChromaColor
        '
        Me.dlgChromaColor.SolidColorOnly = True
        '
        'cmsPlaybackSpeed
        '
        Me.cmsPlaybackSpeed.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem2, Me.ToolStripMenuItem3, Me.ToolStripMenuItem4, Me.ToolStripMenuItem5, Me.ToolStripMenuItem6, Me.ToolStripMenuItem7, Me.ToolStripMenuItem8, Me.ToolStripMenuItem9})
        Me.cmsPlaybackSpeed.Name = "cmsPlaybackSpeed"
        Me.cmsPlaybackSpeed.Size = New System.Drawing.Size(139, 180)
        '
        'ToolStripMenuItem2
        '
        Me.ToolStripMenuItem2.Name = "ToolStripMenuItem2"
        Me.ToolStripMenuItem2.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem2.Text = "0.25"
        '
        'ToolStripMenuItem3
        '
        Me.ToolStripMenuItem3.Name = "ToolStripMenuItem3"
        Me.ToolStripMenuItem3.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem3.Text = "0.5"
        '
        'ToolStripMenuItem4
        '
        Me.ToolStripMenuItem4.Name = "ToolStripMenuItem4"
        Me.ToolStripMenuItem4.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem4.Text = "0.75"
        '
        'ToolStripMenuItem5
        '
        Me.ToolStripMenuItem5.Checked = True
        Me.ToolStripMenuItem5.CheckState = System.Windows.Forms.CheckState.Checked
        Me.ToolStripMenuItem5.Name = "ToolStripMenuItem5"
        Me.ToolStripMenuItem5.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem5.Text = "1.0 (Default)"
        '
        'ToolStripMenuItem6
        '
        Me.ToolStripMenuItem6.Name = "ToolStripMenuItem6"
        Me.ToolStripMenuItem6.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem6.Text = "1.25"
        '
        'ToolStripMenuItem7
        '
        Me.ToolStripMenuItem7.Name = "ToolStripMenuItem7"
        Me.ToolStripMenuItem7.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem7.Text = "1.5"
        '
        'ToolStripMenuItem8
        '
        Me.ToolStripMenuItem8.Name = "ToolStripMenuItem8"
        Me.ToolStripMenuItem8.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem8.Text = "1.75"
        '
        'ToolStripMenuItem9
        '
        Me.ToolStripMenuItem9.Name = "ToolStripMenuItem9"
        Me.ToolStripMenuItem9.Size = New System.Drawing.Size(138, 22)
        Me.ToolStripMenuItem9.Text = "2.0"
        '
        'picFrame5
        '
        Me.picFrame5.Anchor = System.Windows.Forms.AnchorStyles.Bottom
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
        Me.btnいくよ.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnいくよ.BackColor = System.Drawing.SystemColors.Control
        Me.btnいくよ.Enabled = False
        Me.btnいくよ.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnいくよ.Image = Global.SimpleVideoEditor.My.Resources.Resources.Save
        Me.btnいくよ.Location = New System.Drawing.Point(244, 214)
        Me.btnいくよ.Name = "btnいくよ"
        Me.btnいくよ.Size = New System.Drawing.Size(88, 35)
        Me.btnいくよ.TabIndex = 19
        Me.btnいくよ.UseVisualStyleBackColor = False
        '
        'picFrame4
        '
        Me.picFrame4.Anchor = System.Windows.Forms.AnchorStyles.Bottom
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
        Me.picFrame3.Anchor = System.Windows.Forms.AnchorStyles.Bottom
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
        Me.picFrame2.Anchor = System.Windows.Forms.AnchorStyles.Bottom
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
        Me.picFrame1.Anchor = System.Windows.Forms.AnchorStyles.Bottom
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
        Me.picVideo.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
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
        Me.btnBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowse.ContextMenuStrip = Me.cmsBrowse
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Image = Global.SimpleVideoEditor.My.Resources.Resources.Folder
        Me.btnBrowse.Location = New System.Drawing.Point(244, 9)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(88, 24)
        Me.btnBrowse.TabIndex = 7
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'cmsVideoSeeker
        '
        Me.cmsVideoSeeker.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CacheAllFramesToolStripMenuItem})
        Me.cmsVideoSeeker.Name = "cmsVideoSeeker"
        Me.cmsVideoSeeker.Size = New System.Drawing.Size(166, 26)
        '
        'CacheAllFramesToolStripMenuItem
        '
        Me.CacheAllFramesToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.Picture
        Me.CacheAllFramesToolStripMenuItem.Name = "CacheAllFramesToolStripMenuItem"
        Me.CacheAllFramesToolStripMenuItem.Size = New System.Drawing.Size(165, 22)
        Me.CacheAllFramesToolStripMenuItem.Text = "Cache All Frames"
        '
        'ctlVideoSeeker
        '
        Me.ctlVideoSeeker.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctlVideoSeeker.ContextMenuStrip = Me.cmsVideoSeeker
        Me.ctlVideoSeeker.Cursor = System.Windows.Forms.Cursors.Arrow
        Me.ctlVideoSeeker.Enabled = False
        Me.ctlVideoSeeker.HolePunches = Nothing
        Me.ctlVideoSeeker.Location = New System.Drawing.Point(11, 33)
        Me.ctlVideoSeeker.MetaData = Nothing
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
        'cmsBrowse
        '
        Me.cmsBrowse.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.HolePuncherToolToolStripMenuItem})
        Me.cmsBrowse.Name = "cmsBrowse"
        Me.cmsBrowse.Size = New System.Drawing.Size(174, 26)
        '
        'HolePuncherToolToolStripMenuItem
        '
        Me.HolePuncherToolToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.HolePuncher
        Me.HolePuncherToolToolStripMenuItem.Name = "HolePuncherToolToolStripMenuItem"
        Me.HolePuncherToolToolStripMenuItem.Size = New System.Drawing.Size(173, 22)
        Me.HolePuncherToolToolStripMenuItem.Text = "Hole Puncher Tool"
        '
        'MainForm
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.SystemColors.Control
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
        Me.cmsFrameRate.ResumeLayout(False)
        Me.grpSettings.ResumeLayout(False)
        CType(Me.chkQuality, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picPlaybackSpeed, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picChromaKey, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.chkDeleteDuplicates, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.chkMute, System.ComponentModel.ISupportInitialize).EndInit()
        Me.cmsPlaybackVolume.ResumeLayout(False)
        CType(Me.imgRotate, System.ComponentModel.ISupportInitialize).EndInit()
        Me.cmsPicVideo.ResumeLayout(False)
        Me.cmsPlaybackSpeed.ResumeLayout(False)
        CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picVideo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.cmsVideoSeeker.ResumeLayout(False)
        Me.cmsBrowse.ResumeLayout(False)
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
	Friend WithEvents chkDeleteDuplicates As ImageSwitch
	Friend WithEvents cmsFrameRate As ContextMenuStrip
	Friend WithEvents DefaultToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents TenFPSToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents FifteenFPSToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents TwentyFPSToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents ThirtyFPSToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents SixtyFPSToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents picChromaKey As PictureBox
	Friend WithEvents dlgChromaColor As ColorDialog
	Friend WithEvents picPlaybackSpeed As PictureBox
	Friend WithEvents cmsPlaybackSpeed As ContextMenuStrip
	Friend WithEvents ToolStripMenuItem2 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem3 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem4 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem5 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem6 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem7 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem8 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem9 As ToolStripMenuItem
	Friend WithEvents cmsPlaybackVolume As ContextMenuStrip
	Friend WithEvents MuteToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem10 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem11 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem12 As ToolStripMenuItem
	Friend WithEvents UnmuteToolStripMenuItem As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem13 As ToolStripMenuItem
	Friend WithEvents chkQuality As ImageSwitch
    Friend WithEvents cmsVideoSeeker As ContextMenuStrip
    Friend WithEvents CacheAllFramesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents cmsBrowse As ContextMenuStrip
    Friend WithEvents HolePuncherToolToolStripMenuItem As ToolStripMenuItem
End Class
