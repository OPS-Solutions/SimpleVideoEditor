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
        Me.cmbDefinition = New System.Windows.Forms.ComboBox()
        Me.cmsFrameRate = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.DefaultToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TenFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FifteenFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TwentyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TwntyFiveFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ThirtyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FiftyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SixtyFPSToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.MotionInterpolationToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.grpSettings = New System.Windows.Forms.GroupBox()
        Me.picPlaybackSpeed = New System.Windows.Forms.PictureBox()
        Me.picColorKey = New System.Windows.Forms.PictureBox()
        Me.cmsColorKey = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.ClearToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsPlaybackVolume = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.MuteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem10 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem11 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem12 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem13 = New System.Windows.Forms.ToolStripMenuItem()
        Me.UnmuteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.ExportAudioToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.imgRotate = New System.Windows.Forms.PictureBox()
        Me.cmsRotation = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.ToolStripMenuItem14 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem15 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem16 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem17 = New System.Windows.Forms.ToolStripMenuItem()
        Me.sfdVideoOut = New System.Windows.Forms.SaveFileDialog()
        Me.cmsPicVideo = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.cmsPicVideoClear = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsPicVideoExportFrame = New System.Windows.Forms.ToolStripMenuItem()
        Me.CurrentToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SelectedRangeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsAutoCrop = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContractToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExpandToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.dlgColorKey = New System.Windows.Forms.ColorDialog()
        Me.cmsPlaybackSpeed = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.QuarterSpeedToolstripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OneThirdSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.HalfSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ThreeQuarterSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.DefaultSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OneAndAQuarterSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OneAndAHalfSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OneAndThreeQuarterSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.DoubleSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CustomToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
        Me.CustomSpeedTextToolStripMenuItem = New System.Windows.Forms.ToolStripTextBox()
        Me.cmsBrowse = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.HolePuncherToolToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsVideoSeeker = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.CacheAllFramesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.cmsSaveOptions = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.InjectCustomArgumentsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.lblStatusMousePosition = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblStatusCropRect = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblStatusResolution = New System.Windows.Forms.ToolStripStatusLabel()
        Me.pgbOperationProgress = New System.Windows.Forms.ToolStripProgressBar()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.cmsCrop = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.LoadFromClipboardToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CopyToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ctlVideoSeeker = New SimpleVideoEditor.VideoSeeker()
        Me.picFrame5 = New SimpleVideoEditor.PictureBoxPlus()
        Me.chkQuality = New SimpleVideoEditor.ImageSwitch()
        Me.chkDeleteDuplicates = New SimpleVideoEditor.ImageSwitch()
        Me.chkMute = New SimpleVideoEditor.ImageSwitch()
        Me.picFrame4 = New SimpleVideoEditor.PictureBoxPlus()
        Me.picFrame3 = New SimpleVideoEditor.PictureBoxPlus()
        Me.picFrame2 = New SimpleVideoEditor.PictureBoxPlus()
        Me.picFrame1 = New SimpleVideoEditor.PictureBoxPlus()
        Me.picVideo = New SimpleVideoEditor.PictureBoxPlus()
        Me.cmsFrameRate.SuspendLayout()
        Me.grpSettings.SuspendLayout()
        CType(Me.picPlaybackSpeed, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.picColorKey, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.cmsColorKey.SuspendLayout()
        Me.cmsPlaybackVolume.SuspendLayout()
        CType(Me.imgRotate, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.cmsRotation.SuspendLayout()
        Me.cmsPicVideo.SuspendLayout()
        Me.cmsPlaybackSpeed.SuspendLayout()
        Me.cmsBrowse.SuspendLayout()
        Me.cmsVideoSeeker.SuspendLayout()
        Me.cmsSaveOptions.SuspendLayout()
        Me.StatusStrip1.SuspendLayout()
        Me.cmsCrop.SuspendLayout()
        CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.chkQuality, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.chkDeleteDuplicates, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.chkMute, System.ComponentModel.ISupportInitialize).BeginInit()
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
        Me.ofdVideoIn.Multiselect = True
        '
        'cmbDefinition
        '
        Me.cmbDefinition.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmbDefinition.ContextMenuStrip = Me.cmsFrameRate
        Me.cmbDefinition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbDefinition.FormattingEnabled = True
        Me.cmbDefinition.Items.AddRange(New Object() {"Original", "120p", "240p", "360p", "480p", "720p", "1080p"})
        Me.cmbDefinition.Location = New System.Drawing.Point(6, 128)
        Me.cmbDefinition.Name = "cmbDefinition"
        Me.cmbDefinition.Size = New System.Drawing.Size(75, 21)
        Me.cmbDefinition.TabIndex = 9
        '
        'cmsFrameRate
        '
        Me.cmsFrameRate.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.DefaultToolStripMenuItem, Me.TenFPSToolStripMenuItem, Me.FifteenFPSToolStripMenuItem, Me.TwentyFPSToolStripMenuItem, Me.TwntyFiveFPSToolStripMenuItem, Me.ThirtyFPSToolStripMenuItem, Me.FiftyFPSToolStripMenuItem, Me.SixtyFPSToolStripMenuItem, Me.ToolStripSeparator1, Me.MotionInterpolationToolStripMenuItem})
        Me.cmsFrameRate.Name = "cmsFrameRate"
        Me.cmsFrameRate.ShowCheckMargin = True
        Me.cmsFrameRate.ShowImageMargin = False
        Me.cmsFrameRate.Size = New System.Drawing.Size(185, 208)
        '
        'DefaultToolStripMenuItem
        '
        Me.DefaultToolStripMenuItem.Checked = True
        Me.DefaultToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.DefaultToolStripMenuItem.Name = "DefaultToolStripMenuItem"
        Me.DefaultToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.DefaultToolStripMenuItem.Text = "Default"
        '
        'TenFPSToolStripMenuItem
        '
        Me.TenFPSToolStripMenuItem.Name = "TenFPSToolStripMenuItem"
        Me.TenFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.TenFPSToolStripMenuItem.Text = "10 FPS"
        '
        'FifteenFPSToolStripMenuItem
        '
        Me.FifteenFPSToolStripMenuItem.Name = "FifteenFPSToolStripMenuItem"
        Me.FifteenFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.FifteenFPSToolStripMenuItem.Text = "15 FPS"
        '
        'TwentyFPSToolStripMenuItem
        '
        Me.TwentyFPSToolStripMenuItem.Name = "TwentyFPSToolStripMenuItem"
        Me.TwentyFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.TwentyFPSToolStripMenuItem.Text = "20 FPS"
        '
        'TwntyFiveFPSToolStripMenuItem
        '
        Me.TwntyFiveFPSToolStripMenuItem.Name = "TwntyFiveFPSToolStripMenuItem"
        Me.TwntyFiveFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.TwntyFiveFPSToolStripMenuItem.Text = "25 FPS"
        '
        'ThirtyFPSToolStripMenuItem
        '
        Me.ThirtyFPSToolStripMenuItem.Name = "ThirtyFPSToolStripMenuItem"
        Me.ThirtyFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.ThirtyFPSToolStripMenuItem.Text = "30 FPS"
        '
        'FiftyFPSToolStripMenuItem
        '
        Me.FiftyFPSToolStripMenuItem.Name = "FiftyFPSToolStripMenuItem"
        Me.FiftyFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.FiftyFPSToolStripMenuItem.Text = "50 FPS"
        '
        'SixtyFPSToolStripMenuItem
        '
        Me.SixtyFPSToolStripMenuItem.Name = "SixtyFPSToolStripMenuItem"
        Me.SixtyFPSToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.SixtyFPSToolStripMenuItem.Text = "60 FPS"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(181, 6)
        '
        'MotionInterpolationToolStripMenuItem
        '
        Me.MotionInterpolationToolStripMenuItem.CheckOnClick = True
        Me.MotionInterpolationToolStripMenuItem.Name = "MotionInterpolationToolStripMenuItem"
        Me.MotionInterpolationToolStripMenuItem.Size = New System.Drawing.Size(184, 22)
        Me.MotionInterpolationToolStripMenuItem.Text = "Motion Interpolation"
        '
        'grpSettings
        '
        Me.grpSettings.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpSettings.Controls.Add(Me.chkQuality)
        Me.grpSettings.Controls.Add(Me.picPlaybackSpeed)
        Me.grpSettings.Controls.Add(Me.picColorKey)
        Me.grpSettings.Controls.Add(Me.chkDeleteDuplicates)
        Me.grpSettings.Controls.Add(Me.chkMute)
        Me.grpSettings.Controls.Add(Me.imgRotate)
        Me.grpSettings.Controls.Add(Me.cmbDefinition)
        Me.grpSettings.Location = New System.Drawing.Point(244, 39)
        Me.grpSettings.Name = "grpSettings"
        Me.grpSettings.Size = New System.Drawing.Size(87, 155)
        Me.grpSettings.TabIndex = 17
        Me.grpSettings.TabStop = False
        Me.grpSettings.Text = "Settings"
        '
        'picPlaybackSpeed
        '
        Me.picPlaybackSpeed.Cursor = System.Windows.Forms.Cursors.Hand
        Me.picPlaybackSpeed.Image = Global.SimpleVideoEditor.My.Resources.Resources.StopWatch
        Me.picPlaybackSpeed.Location = New System.Drawing.Point(52, 59)
        Me.picPlaybackSpeed.Margin = New System.Windows.Forms.Padding(0)
        Me.picPlaybackSpeed.Name = "picPlaybackSpeed"
        Me.picPlaybackSpeed.Size = New System.Drawing.Size(18, 18)
        Me.picPlaybackSpeed.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
        Me.picPlaybackSpeed.TabIndex = 24
        Me.picPlaybackSpeed.TabStop = False
        '
        'picColorKey
        '
        Me.picColorKey.BackColor = System.Drawing.Color.Lime
        Me.picColorKey.ContextMenuStrip = Me.cmsColorKey
        Me.picColorKey.Cursor = System.Windows.Forms.Cursors.Hand
        Me.picColorKey.Image = Global.SimpleVideoEditor.My.Resources.Resources.ColorKey
        Me.picColorKey.Location = New System.Drawing.Point(17, 95)
        Me.picColorKey.Name = "picColorKey"
        Me.picColorKey.Size = New System.Drawing.Size(18, 18)
        Me.picColorKey.TabIndex = 23
        Me.picColorKey.TabStop = False
        '
        'cmsColorKey
        '
        Me.cmsColorKey.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ClearToolStripMenuItem})
        Me.cmsColorKey.Name = "cmsColorKey"
        Me.cmsColorKey.Size = New System.Drawing.Size(102, 26)
        '
        'ClearToolStripMenuItem
        '
        Me.ClearToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.Eraser
        Me.ClearToolStripMenuItem.Name = "ClearToolStripMenuItem"
        Me.ClearToolStripMenuItem.Size = New System.Drawing.Size(101, 22)
        Me.ClearToolStripMenuItem.Text = "Clear"
        '
        'cmsPlaybackVolume
        '
        Me.cmsPlaybackVolume.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MuteToolStripMenuItem, Me.ToolStripMenuItem10, Me.ToolStripMenuItem11, Me.ToolStripMenuItem12, Me.ToolStripMenuItem13, Me.UnmuteToolStripMenuItem, Me.ToolStripSeparator2, Me.ExportAudioToolStripMenuItem})
        Me.cmsPlaybackVolume.Name = "cmsAudioVolume"
        Me.cmsPlaybackVolume.Size = New System.Drawing.Size(144, 164)
        '
        'MuteToolStripMenuItem
        '
        Me.MuteToolStripMenuItem.Checked = True
        Me.MuteToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.MuteToolStripMenuItem.Name = "MuteToolStripMenuItem"
        Me.MuteToolStripMenuItem.Size = New System.Drawing.Size(143, 22)
        Me.MuteToolStripMenuItem.Text = "0.0 (Mute)"
        '
        'ToolStripMenuItem10
        '
        Me.ToolStripMenuItem10.Name = "ToolStripMenuItem10"
        Me.ToolStripMenuItem10.Size = New System.Drawing.Size(143, 22)
        Me.ToolStripMenuItem10.Text = "0.1"
        '
        'ToolStripMenuItem11
        '
        Me.ToolStripMenuItem11.Name = "ToolStripMenuItem11"
        Me.ToolStripMenuItem11.Size = New System.Drawing.Size(143, 22)
        Me.ToolStripMenuItem11.Text = "0.3"
        '
        'ToolStripMenuItem12
        '
        Me.ToolStripMenuItem12.Name = "ToolStripMenuItem12"
        Me.ToolStripMenuItem12.Size = New System.Drawing.Size(143, 22)
        Me.ToolStripMenuItem12.Text = "0.5"
        '
        'ToolStripMenuItem13
        '
        Me.ToolStripMenuItem13.Name = "ToolStripMenuItem13"
        Me.ToolStripMenuItem13.Size = New System.Drawing.Size(143, 22)
        Me.ToolStripMenuItem13.Text = "0.7"
        '
        'UnmuteToolStripMenuItem
        '
        Me.UnmuteToolStripMenuItem.Name = "UnmuteToolStripMenuItem"
        Me.UnmuteToolStripMenuItem.Size = New System.Drawing.Size(143, 22)
        Me.UnmuteToolStripMenuItem.Text = "1.0 (Full)"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(140, 6)
        '
        'ExportAudioToolStripMenuItem
        '
        Me.ExportAudioToolStripMenuItem.Enabled = False
        Me.ExportAudioToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.ExportAudio
        Me.ExportAudioToolStripMenuItem.Name = "ExportAudioToolStripMenuItem"
        Me.ExportAudioToolStripMenuItem.Size = New System.Drawing.Size(143, 22)
        Me.ExportAudioToolStripMenuItem.Text = "Export Audio"
        '
        'imgRotate
        '
        Me.imgRotate.ContextMenuStrip = Me.cmsRotation
        Me.imgRotate.Cursor = System.Windows.Forms.Cursors.Hand
        Me.imgRotate.Image = Global.SimpleVideoEditor.My.Resources.Resources.Rotate
        Me.imgRotate.Location = New System.Drawing.Point(17, 23)
        Me.imgRotate.Margin = New System.Windows.Forms.Padding(0)
        Me.imgRotate.Name = "imgRotate"
        Me.imgRotate.Size = New System.Drawing.Size(18, 18)
        Me.imgRotate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
        Me.imgRotate.TabIndex = 20
        Me.imgRotate.TabStop = False
        '
        'cmsRotation
        '
        Me.cmsRotation.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem14, Me.ToolStripMenuItem15, Me.ToolStripMenuItem16, Me.ToolStripMenuItem17})
        Me.cmsRotation.Name = "cmsRotation"
        Me.cmsRotation.Size = New System.Drawing.Size(135, 92)
        '
        'ToolStripMenuItem14
        '
        Me.ToolStripMenuItem14.Checked = True
        Me.ToolStripMenuItem14.CheckState = System.Windows.Forms.CheckState.Checked
        Me.ToolStripMenuItem14.Name = "ToolStripMenuItem14"
        Me.ToolStripMenuItem14.Size = New System.Drawing.Size(134, 22)
        Me.ToolStripMenuItem14.Text = "0° (Default)"
        '
        'ToolStripMenuItem15
        '
        Me.ToolStripMenuItem15.Name = "ToolStripMenuItem15"
        Me.ToolStripMenuItem15.Size = New System.Drawing.Size(134, 22)
        Me.ToolStripMenuItem15.Text = "90°"
        '
        'ToolStripMenuItem16
        '
        Me.ToolStripMenuItem16.Name = "ToolStripMenuItem16"
        Me.ToolStripMenuItem16.Size = New System.Drawing.Size(134, 22)
        Me.ToolStripMenuItem16.Text = "180"
        '
        'ToolStripMenuItem17
        '
        Me.ToolStripMenuItem17.Name = "ToolStripMenuItem17"
        Me.ToolStripMenuItem17.Size = New System.Drawing.Size(134, 22)
        Me.ToolStripMenuItem17.Text = "270°"
        '
        'sfdVideoOut
        '
        '
        'cmsPicVideo
        '
        Me.cmsPicVideo.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.cmsPicVideoClear, Me.cmsPicVideoExportFrame, Me.cmsAutoCrop})
        Me.cmsPicVideo.Name = "cmsPicVideo"
        Me.cmsPicVideo.Size = New System.Drawing.Size(145, 70)
        '
        'cmsPicVideoClear
        '
        Me.cmsPicVideoClear.Image = Global.SimpleVideoEditor.My.Resources.Resources.Eraser
        Me.cmsPicVideoClear.Name = "cmsPicVideoClear"
        Me.cmsPicVideoClear.Size = New System.Drawing.Size(144, 22)
        Me.cmsPicVideoClear.Text = "Clear"
        '
        'cmsPicVideoExportFrame
        '
        Me.cmsPicVideoExportFrame.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CurrentToolStripMenuItem, Me.SelectedRangeToolStripMenuItem})
        Me.cmsPicVideoExportFrame.Enabled = False
        Me.cmsPicVideoExportFrame.Image = Global.SimpleVideoEditor.My.Resources.Resources.Picture
        Me.cmsPicVideoExportFrame.Name = "cmsPicVideoExportFrame"
        Me.cmsPicVideoExportFrame.Size = New System.Drawing.Size(144, 22)
        Me.cmsPicVideoExportFrame.Text = "Export Frame"
        '
        'CurrentToolStripMenuItem
        '
        Me.CurrentToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.Picture
        Me.CurrentToolStripMenuItem.Name = "CurrentToolStripMenuItem"
        Me.CurrentToolStripMenuItem.Size = New System.Drawing.Size(154, 22)
        Me.CurrentToolStripMenuItem.Text = "Current"
        '
        'SelectedRangeToolStripMenuItem
        '
        Me.SelectedRangeToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.ExportRange
        Me.SelectedRangeToolStripMenuItem.Name = "SelectedRangeToolStripMenuItem"
        Me.SelectedRangeToolStripMenuItem.Size = New System.Drawing.Size(154, 22)
        Me.SelectedRangeToolStripMenuItem.Text = "Selected Range"
        '
        'cmsAutoCrop
        '
        Me.cmsAutoCrop.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ContractToolStripMenuItem, Me.ExpandToolStripMenuItem})
        Me.cmsAutoCrop.Enabled = False
        Me.cmsAutoCrop.Image = Global.SimpleVideoEditor.My.Resources.Resources.AutoCrop
        Me.cmsAutoCrop.Name = "cmsAutoCrop"
        Me.cmsAutoCrop.Size = New System.Drawing.Size(144, 22)
        Me.cmsAutoCrop.Text = "Auto Crop"
        '
        'ContractToolStripMenuItem
        '
        Me.ContractToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.AutoCropContract
        Me.ContractToolStripMenuItem.Name = "ContractToolStripMenuItem"
        Me.ContractToolStripMenuItem.Size = New System.Drawing.Size(120, 22)
        Me.ContractToolStripMenuItem.Text = "Contract"
        '
        'ExpandToolStripMenuItem
        '
        Me.ExpandToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.AutoCropExpand
        Me.ExpandToolStripMenuItem.Name = "ExpandToolStripMenuItem"
        Me.ExpandToolStripMenuItem.Size = New System.Drawing.Size(120, 22)
        Me.ExpandToolStripMenuItem.Text = "Expand"
        '
        'dlgColorKey
        '
        Me.dlgColorKey.Color = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(1, Byte), Integer), CType(CType(1, Byte), Integer), CType(CType(1, Byte), Integer))
        Me.dlgColorKey.FullOpen = True
        Me.dlgColorKey.SolidColorOnly = True
        '
        'cmsPlaybackSpeed
        '
        Me.cmsPlaybackSpeed.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.QuarterSpeedToolstripMenuItem, Me.OneThirdSpeedToolStripMenuItem, Me.HalfSpeedToolStripMenuItem, Me.ThreeQuarterSpeedToolStripMenuItem, Me.DefaultSpeedToolStripMenuItem, Me.OneAndAQuarterSpeedToolStripMenuItem, Me.OneAndAHalfSpeedToolStripMenuItem, Me.OneAndThreeQuarterSpeedToolStripMenuItem, Me.DoubleSpeedToolStripMenuItem, Me.CustomToolStripMenuItem, Me.ToolStripSeparator3, Me.CustomSpeedTextToolStripMenuItem})
        Me.cmsPlaybackSpeed.Name = "cmsPlaybackSpeed"
        Me.cmsPlaybackSpeed.Size = New System.Drawing.Size(161, 255)
        '
        'QuarterSpeedToolstripMenuItem
        '
        Me.QuarterSpeedToolstripMenuItem.Name = "QuarterSpeedToolstripMenuItem"
        Me.QuarterSpeedToolstripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.QuarterSpeedToolstripMenuItem.Text = "0.25"
        '
        'OneThirdSpeedToolStripMenuItem
        '
        Me.OneThirdSpeedToolStripMenuItem.Name = "OneThirdSpeedToolStripMenuItem"
        Me.OneThirdSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.OneThirdSpeedToolStripMenuItem.Text = "0.333"
        '
        'HalfSpeedToolStripMenuItem
        '
        Me.HalfSpeedToolStripMenuItem.Name = "HalfSpeedToolStripMenuItem"
        Me.HalfSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.HalfSpeedToolStripMenuItem.Text = "0.5"
        '
        'ThreeQuarterSpeedToolStripMenuItem
        '
        Me.ThreeQuarterSpeedToolStripMenuItem.Name = "ThreeQuarterSpeedToolStripMenuItem"
        Me.ThreeQuarterSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.ThreeQuarterSpeedToolStripMenuItem.Text = "0.75"
        '
        'DefaultSpeedToolStripMenuItem
        '
        Me.DefaultSpeedToolStripMenuItem.Checked = True
        Me.DefaultSpeedToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.DefaultSpeedToolStripMenuItem.Name = "DefaultSpeedToolStripMenuItem"
        Me.DefaultSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.DefaultSpeedToolStripMenuItem.Text = "1 (Default)"
        '
        'OneAndAQuarterSpeedToolStripMenuItem
        '
        Me.OneAndAQuarterSpeedToolStripMenuItem.Name = "OneAndAQuarterSpeedToolStripMenuItem"
        Me.OneAndAQuarterSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.OneAndAQuarterSpeedToolStripMenuItem.Text = "1.25"
        '
        'OneAndAHalfSpeedToolStripMenuItem
        '
        Me.OneAndAHalfSpeedToolStripMenuItem.Name = "OneAndAHalfSpeedToolStripMenuItem"
        Me.OneAndAHalfSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.OneAndAHalfSpeedToolStripMenuItem.Text = "1.5"
        '
        'OneAndThreeQuarterSpeedToolStripMenuItem
        '
        Me.OneAndThreeQuarterSpeedToolStripMenuItem.Name = "OneAndThreeQuarterSpeedToolStripMenuItem"
        Me.OneAndThreeQuarterSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.OneAndThreeQuarterSpeedToolStripMenuItem.Text = "1.75"
        '
        'DoubleSpeedToolStripMenuItem
        '
        Me.DoubleSpeedToolStripMenuItem.Name = "DoubleSpeedToolStripMenuItem"
        Me.DoubleSpeedToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.DoubleSpeedToolStripMenuItem.Text = "2"
        '
        'CustomToolStripMenuItem
        '
        Me.CustomToolStripMenuItem.Name = "CustomToolStripMenuItem"
        Me.CustomToolStripMenuItem.Size = New System.Drawing.Size(160, 22)
        Me.CustomToolStripMenuItem.Text = "Custom"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(157, 6)
        '
        'CustomSpeedTextToolStripMenuItem
        '
        Me.CustomSpeedTextToolStripMenuItem.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.CustomSpeedTextToolStripMenuItem.Name = "CustomSpeedTextToolStripMenuItem"
        Me.CustomSpeedTextToolStripMenuItem.Size = New System.Drawing.Size(100, 23)
        Me.CustomSpeedTextToolStripMenuItem.Text = "0.1666"
        Me.CustomSpeedTextToolStripMenuItem.ToolTipText = "Custom playback speed"
        '
        'cmsBrowse
        '
        Me.cmsBrowse.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.HolePuncherToolToolStripMenuItem})
        Me.cmsBrowse.Name = "cmsBrowse"
        Me.cmsBrowse.Size = New System.Drawing.Size(172, 26)
        '
        'HolePuncherToolToolStripMenuItem
        '
        Me.HolePuncherToolToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.HolePuncher
        Me.HolePuncherToolToolStripMenuItem.Name = "HolePuncherToolToolStripMenuItem"
        Me.HolePuncherToolToolStripMenuItem.Size = New System.Drawing.Size(171, 22)
        Me.HolePuncherToolToolStripMenuItem.Text = "Hole Puncher Tool"
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
        'cmsSaveOptions
        '
        Me.cmsSaveOptions.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.InjectCustomArgumentsToolStripMenuItem})
        Me.cmsSaveOptions.Name = "cmsSaveOptions"
        Me.cmsSaveOptions.Size = New System.Drawing.Size(211, 26)
        '
        'InjectCustomArgumentsToolStripMenuItem
        '
        Me.InjectCustomArgumentsToolStripMenuItem.Image = Global.SimpleVideoEditor.My.Resources.Resources.UserInjectionIcon
        Me.InjectCustomArgumentsToolStripMenuItem.Name = "InjectCustomArgumentsToolStripMenuItem"
        Me.InjectCustomArgumentsToolStripMenuItem.Size = New System.Drawing.Size(210, 22)
        Me.InjectCustomArgumentsToolStripMenuItem.Text = "Inject Custom Arguments"
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblStatusMousePosition, Me.lblStatusCropRect, Me.lblStatusResolution, Me.pgbOperationProgress})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 236)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.ShowItemToolTips = True
        Me.StatusStrip1.Size = New System.Drawing.Size(344, 25)
        Me.StatusStrip1.TabIndex = 22
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'lblStatusMousePosition
        '
        Me.lblStatusMousePosition.AutoSize = False
        Me.lblStatusMousePosition.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right
        Me.lblStatusMousePosition.Image = Global.SimpleVideoEditor.My.Resources.Resources.Cross
        Me.lblStatusMousePosition.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.lblStatusMousePosition.Name = "lblStatusMousePosition"
        Me.lblStatusMousePosition.Size = New System.Drawing.Size(80, 20)
        '
        'lblStatusCropRect
        '
        Me.lblStatusCropRect.AutoSize = False
        Me.lblStatusCropRect.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right
        Me.lblStatusCropRect.Image = Global.SimpleVideoEditor.My.Resources.Resources.Crop
        Me.lblStatusCropRect.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.lblStatusCropRect.Name = "lblStatusCropRect"
        Me.lblStatusCropRect.Size = New System.Drawing.Size(90, 20)
        '
        'lblStatusResolution
        '
        Me.lblStatusResolution.AutoSize = False
        Me.lblStatusResolution.Image = Global.SimpleVideoEditor.My.Resources.Resources.Resolution
        Me.lblStatusResolution.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.lblStatusResolution.Name = "lblStatusResolution"
        Me.lblStatusResolution.Size = New System.Drawing.Size(90, 20)
        Me.lblStatusResolution.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'pgbOperationProgress
        '
        Me.pgbOperationProgress.Name = "pgbOperationProgress"
        Me.pgbOperationProgress.Size = New System.Drawing.Size(64, 19)
        Me.pgbOperationProgress.Visible = False
        '
        'btnSave
        '
        Me.btnSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSave.BackColor = System.Drawing.SystemColors.Control
        Me.btnSave.ContextMenuStrip = Me.cmsSaveOptions
        Me.btnSave.Enabled = False
        Me.btnSave.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSave.Image = Global.SimpleVideoEditor.My.Resources.Resources.Save
        Me.btnSave.Location = New System.Drawing.Point(244, 198)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(88, 35)
        Me.btnSave.TabIndex = 19
        Me.btnSave.UseVisualStyleBackColor = False
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
        'cmsCrop
        '
        Me.cmsCrop.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.LoadFromClipboardToolStripMenuItem, Me.CopyToolStripMenuItem})
        Me.cmsCrop.Name = "cmsCrop"
        Me.cmsCrop.Size = New System.Drawing.Size(187, 48)
        '
        'LoadFromClipboardToolStripMenuItem
        '
        Me.LoadFromClipboardToolStripMenuItem.Name = "LoadFromClipboardToolStripMenuItem"
        Me.LoadFromClipboardToolStripMenuItem.Size = New System.Drawing.Size(186, 22)
        Me.LoadFromClipboardToolStripMenuItem.Text = "Load From Clipboard"
        '
        'CopyToolStripMenuItem
        '
        Me.CopyToolStripMenuItem.Name = "CopyToolStripMenuItem"
        Me.CopyToolStripMenuItem.Size = New System.Drawing.Size(186, 22)
        Me.CopyToolStripMenuItem.Text = "Copy"
        '
        'ctlVideoSeeker
        '
        Me.ctlVideoSeeker.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctlVideoSeeker.ContextMenuStrip = Me.cmsVideoSeeker
        Me.ctlVideoSeeker.Cursor = System.Windows.Forms.Cursors.Default
        Me.ctlVideoSeeker.Enabled = False
        Me.ctlVideoSeeker.HolePunches = Nothing
        Me.ctlVideoSeeker.Location = New System.Drawing.Point(12, 10)
        Me.ctlVideoSeeker.MetaData = Nothing
        Me.ctlVideoSeeker.Name = "ctlVideoSeeker"
        Me.ctlVideoSeeker.PreviewLocation = 0
        Me.ctlVideoSeeker.RangeMax = 100
        Me.ctlVideoSeeker.RangeMaxValue = 100
        Me.ctlVideoSeeker.RangeMin = 0
        Me.ctlVideoSeeker.RangeMinValue = 0
        Me.ctlVideoSeeker.SceneFrames = Nothing
        Me.ctlVideoSeeker.Size = New System.Drawing.Size(227, 23)
        Me.ctlVideoSeeker.TabIndex = 21
        '
        'picFrame5
        '
        Me.picFrame5.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.picFrame5.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame5.Cursor = System.Windows.Forms.Cursors.Hand
        Me.picFrame5.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.[Default]
        Me.picFrame5.Location = New System.Drawing.Point(196, 200)
        Me.picFrame5.Name = "picFrame5"
        Me.picFrame5.Size = New System.Drawing.Size(42, 33)
        Me.picFrame5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picFrame5.TabIndex = 20
        Me.picFrame5.TabStop = False
        '
        'chkQuality
        '
        Me.chkQuality.Checked = False
        Me.chkQuality.Cursor = System.Windows.Forms.Cursors.Hand
        Me.chkQuality.FalseImage = Global.SimpleVideoEditor.My.Resources.Resources.qscaleOff
        Me.chkQuality.Image = Global.SimpleVideoEditor.My.Resources.Resources.qscaleOff
        Me.chkQuality.Location = New System.Drawing.Point(52, 95)
        Me.chkQuality.Name = "chkQuality"
        Me.chkQuality.Size = New System.Drawing.Size(18, 18)
        Me.chkQuality.TabIndex = 25
        Me.chkQuality.TabStop = False
        Me.chkQuality.TrueImage = Global.SimpleVideoEditor.My.Resources.Resources.qscaleOn
        '
        'chkDeleteDuplicates
        '
        Me.chkDeleteDuplicates.Checked = False
        Me.chkDeleteDuplicates.Cursor = System.Windows.Forms.Cursors.Hand
        Me.chkDeleteDuplicates.FalseImage = Global.SimpleVideoEditor.My.Resources.Resources.DuplicatesOn
        Me.chkDeleteDuplicates.Image = Global.SimpleVideoEditor.My.Resources.Resources.DuplicatesOn
        Me.chkDeleteDuplicates.Location = New System.Drawing.Point(17, 59)
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
        Me.chkMute.Location = New System.Drawing.Point(52, 23)
        Me.chkMute.Name = "chkMute"
        Me.chkMute.Size = New System.Drawing.Size(18, 18)
        Me.chkMute.TabIndex = 21
        Me.chkMute.TabStop = False
        Me.chkMute.TrueImage = Global.SimpleVideoEditor.My.Resources.Resources.SpeakerOff
        '
        'picFrame4
        '
        Me.picFrame4.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.picFrame4.BackColor = System.Drawing.SystemColors.ControlDark
        Me.picFrame4.Cursor = System.Windows.Forms.Cursors.Hand
        Me.picFrame4.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.[Default]
        Me.picFrame4.Location = New System.Drawing.Point(150, 200)
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
        Me.picFrame3.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.[Default]
        Me.picFrame3.Location = New System.Drawing.Point(104, 200)
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
        Me.picFrame2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.[Default]
        Me.picFrame2.Location = New System.Drawing.Point(58, 200)
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
        Me.picFrame1.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.[Default]
        Me.picFrame1.Location = New System.Drawing.Point(12, 200)
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
        Me.picVideo.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.[Default]
        Me.picVideo.Location = New System.Drawing.Point(11, 38)
        Me.picVideo.Name = "picVideo"
        Me.picVideo.Size = New System.Drawing.Size(227, 156)
        Me.picVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picVideo.TabIndex = 10
        Me.picVideo.TabStop = False
        '
        'MainForm
        '
        Me.AllowDrop = True
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.SystemColors.Control
        Me.ClientSize = New System.Drawing.Size(344, 261)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.ctlVideoSeeker)
        Me.Controls.Add(Me.picFrame5)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.grpSettings)
        Me.Controls.Add(Me.picFrame4)
        Me.Controls.Add(Me.picFrame3)
        Me.Controls.Add(Me.picFrame2)
        Me.Controls.Add(Me.picFrame1)
        Me.Controls.Add(Me.picVideo)
        Me.Controls.Add(Me.btnBrowse)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.SystemColors.ControlText
        Me.HelpButton = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(360, 300)
        Me.Name = "MainForm"
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.Text = "Simple Video Editor"
        Me.cmsFrameRate.ResumeLayout(False)
        Me.grpSettings.ResumeLayout(False)
        CType(Me.picPlaybackSpeed, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picColorKey, System.ComponentModel.ISupportInitialize).EndInit()
        Me.cmsColorKey.ResumeLayout(False)
        Me.cmsPlaybackVolume.ResumeLayout(False)
        CType(Me.imgRotate, System.ComponentModel.ISupportInitialize).EndInit()
        Me.cmsRotation.ResumeLayout(False)
        Me.cmsPicVideo.ResumeLayout(False)
        Me.cmsPlaybackSpeed.ResumeLayout(False)
        Me.cmsPlaybackSpeed.PerformLayout()
        Me.cmsBrowse.ResumeLayout(False)
        Me.cmsVideoSeeker.ResumeLayout(False)
        Me.cmsSaveOptions.ResumeLayout(False)
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.cmsCrop.ResumeLayout(False)
        CType(Me.picFrame5, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.chkQuality, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.chkDeleteDuplicates, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.chkMute, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picFrame1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.picVideo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ofdVideoIn As System.Windows.Forms.OpenFileDialog
    Friend WithEvents btnBrowse As System.Windows.Forms.Button
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents picVideo As PictureBoxPlus
    Friend WithEvents picFrame1 As PictureBoxPlus
    Friend WithEvents picFrame2 As PictureBoxPlus
    Friend WithEvents picFrame3 As PictureBoxPlus
    Friend WithEvents picFrame4 As PictureBoxPlus
    Friend WithEvents cmbDefinition As System.Windows.Forms.ComboBox
    Friend WithEvents grpSettings As System.Windows.Forms.GroupBox
    Friend WithEvents picFrame5 As PictureBoxPlus
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
    Friend WithEvents picColorKey As PictureBox
    Friend WithEvents dlgColorKey As ColorDialog
    Friend WithEvents picPlaybackSpeed As PictureBox
    Friend WithEvents cmsPlaybackSpeed As ContextMenuStrip
    Friend WithEvents QuarterSpeedToolstripMenuItem As ToolStripMenuItem
    Friend WithEvents HalfSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ThreeQuarterSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DefaultSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OneAndAQuarterSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OneAndAHalfSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OneAndThreeQuarterSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DoubleSpeedToolStripMenuItem As ToolStripMenuItem
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
    Friend WithEvents cmsSaveOptions As ContextMenuStrip
    Friend WithEvents InjectCustomArgumentsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CurrentToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SelectedRangeToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents cmsAutoCrop As ToolStripMenuItem
    Friend WithEvents cmsRotation As ContextMenuStrip
    Friend WithEvents ToolStripMenuItem14 As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem15 As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem16 As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem17 As ToolStripMenuItem
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents lblStatusMousePosition As ToolStripStatusLabel
    Friend WithEvents lblStatusCropRect As ToolStripStatusLabel
    Friend WithEvents lblStatusResolution As ToolStripStatusLabel
    Friend WithEvents ContractToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ExpandToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents cmsColorKey As ContextMenuStrip
    Friend WithEvents ClearToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents TwntyFiveFPSToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents FiftyFPSToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OneThirdSpeedToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents MotionInterpolationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents ExportAudioToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents cmsCrop As ContextMenuStrip
    Friend WithEvents LoadFromClipboardToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CopyToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents pgbOperationProgress As ToolStripProgressBar
    Friend WithEvents ToolStripSeparator3 As ToolStripSeparator
    Friend WithEvents CustomToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CustomSpeedTextToolStripMenuItem As ToolStripTextBox
End Class
