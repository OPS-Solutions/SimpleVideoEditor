﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class HolePuncherForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(HolePuncherForm))
        Me.btnSaveHolePunch = New System.Windows.Forms.Button()
        Me.numStdDev = New System.Windows.Forms.NumericUpDown()
        Me.lblThreshold = New System.Windows.Forms.Label()
        Me.pnlSeekers = New System.Windows.Forms.Panel()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.ofdVideoIn = New System.Windows.Forms.OpenFileDialog()
        Me.lblMinChain = New System.Windows.Forms.Label()
        Me.numMinChain = New System.Windows.Forms.NumericUpDown()
        Me.btnDetect = New System.Windows.Forms.Button()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.ToolStripStatusLabel1 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.pgbProgress = New System.Windows.Forms.ToolStripProgressBar()
        Me.lblDeltaE = New System.Windows.Forms.Label()
        Me.numDeltaE = New System.Windows.Forms.NumericUpDown()
        CType(Me.numStdDev, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.numMinChain, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.StatusStrip1.SuspendLayout()
        CType(Me.numDeltaE, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'btnSaveHolePunch
        '
        Me.btnSaveHolePunch.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSaveHolePunch.BackColor = System.Drawing.SystemColors.Control
        Me.btnSaveHolePunch.Enabled = False
        Me.btnSaveHolePunch.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSaveHolePunch.Image = Global.SimpleVideoEditor.My.Resources.Resources.Save
        Me.btnSaveHolePunch.Location = New System.Drawing.Point(260, 201)
        Me.btnSaveHolePunch.Name = "btnSaveHolePunch"
        Me.btnSaveHolePunch.Size = New System.Drawing.Size(72, 35)
        Me.btnSaveHolePunch.TabIndex = 10
        Me.btnSaveHolePunch.UseVisualStyleBackColor = False
        '
        'numStdDev
        '
        Me.numStdDev.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.numStdDev.Location = New System.Drawing.Point(260, 91)
        Me.numStdDev.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.numStdDev.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.numStdDev.Name = "numStdDev"
        Me.numStdDev.Size = New System.Drawing.Size(72, 20)
        Me.numStdDev.TabIndex = 11
        Me.numStdDev.Value = New Decimal(New Integer() {6, 0, 0, 0})
        '
        'lblThreshold
        '
        Me.lblThreshold.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblThreshold.Location = New System.Drawing.Point(257, 75)
        Me.lblThreshold.Name = "lblThreshold"
        Me.lblThreshold.Size = New System.Drawing.Size(75, 13)
        Me.lblThreshold.TabIndex = 12
        Me.lblThreshold.Text = "StdDev"
        Me.lblThreshold.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'pnlSeekers
        '
        Me.pnlSeekers.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlSeekers.AutoScroll = True
        Me.pnlSeekers.Location = New System.Drawing.Point(12, 10)
        Me.pnlSeekers.Name = "pnlSeekers"
        Me.pnlSeekers.Size = New System.Drawing.Size(239, 226)
        Me.pnlSeekers.TabIndex = 13
        '
        'btnBrowse
        '
        Me.btnBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowse.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnBrowse.Image = Global.SimpleVideoEditor.My.Resources.Resources.Folder
        Me.btnBrowse.Location = New System.Drawing.Point(260, 9)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(72, 24)
        Me.btnBrowse.TabIndex = 14
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'ofdVideoIn
        '
        Me.ofdVideoIn.FileName = "Video"
        '
        'lblMinChain
        '
        Me.lblMinChain.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblMinChain.Location = New System.Drawing.Point(257, 36)
        Me.lblMinChain.Name = "lblMinChain"
        Me.lblMinChain.Size = New System.Drawing.Size(75, 13)
        Me.lblMinChain.TabIndex = 16
        Me.lblMinChain.Text = "Min Chain"
        Me.lblMinChain.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'numMinChain
        '
        Me.numMinChain.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.numMinChain.Location = New System.Drawing.Point(260, 52)
        Me.numMinChain.Maximum = New Decimal(New Integer() {3600, 0, 0, 0})
        Me.numMinChain.Minimum = New Decimal(New Integer() {30, 0, 0, 0})
        Me.numMinChain.Name = "numMinChain"
        Me.numMinChain.Size = New System.Drawing.Size(72, 20)
        Me.numMinChain.TabIndex = 15
        Me.numMinChain.Value = New Decimal(New Integer() {600, 0, 0, 0})
        '
        'btnDetect
        '
        Me.btnDetect.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnDetect.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnDetect.Image = Global.SimpleVideoEditor.My.Resources.Resources.HolePuncher
        Me.btnDetect.Location = New System.Drawing.Point(260, 157)
        Me.btnDetect.Name = "btnDetect"
        Me.btnDetect.Size = New System.Drawing.Size(72, 24)
        Me.btnDetect.TabIndex = 19
        Me.btnDetect.UseVisualStyleBackColor = True
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripStatusLabel1, Me.pgbProgress})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 239)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(344, 22)
        Me.StatusStrip1.TabIndex = 20
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'ToolStripStatusLabel1
        '
        Me.ToolStripStatusLabel1.Name = "ToolStripStatusLabel1"
        Me.ToolStripStatusLabel1.Size = New System.Drawing.Size(0, 17)
        '
        'pgbProgress
        '
        Me.pgbProgress.Name = "pgbProgress"
        Me.pgbProgress.Size = New System.Drawing.Size(100, 16)
        '
        'lblDeltaE
        '
        Me.lblDeltaE.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblDeltaE.Location = New System.Drawing.Point(257, 115)
        Me.lblDeltaE.Name = "lblDeltaE"
        Me.lblDeltaE.Size = New System.Drawing.Size(75, 13)
        Me.lblDeltaE.TabIndex = 22
        Me.lblDeltaE.Text = "DeltaE"
        Me.lblDeltaE.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'numDeltaE
        '
        Me.numDeltaE.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.numDeltaE.Location = New System.Drawing.Point(260, 131)
        Me.numDeltaE.Maximum = New Decimal(New Integer() {20, 0, 0, 0})
        Me.numDeltaE.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.numDeltaE.Name = "numDeltaE"
        Me.numDeltaE.Size = New System.Drawing.Size(72, 20)
        Me.numDeltaE.TabIndex = 21
        Me.numDeltaE.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'HolePuncherForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(344, 261)
        Me.Controls.Add(Me.lblDeltaE)
        Me.Controls.Add(Me.numDeltaE)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.btnDetect)
        Me.Controls.Add(Me.lblMinChain)
        Me.Controls.Add(Me.numMinChain)
        Me.Controls.Add(Me.btnBrowse)
        Me.Controls.Add(Me.pnlSeekers)
        Me.Controls.Add(Me.lblThreshold)
        Me.Controls.Add(Me.numStdDev)
        Me.Controls.Add(Me.btnSaveHolePunch)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "HolePuncherForm"
        Me.Text = "Hole Puncher Tool"
        CType(Me.numStdDev, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.numMinChain, System.ComponentModel.ISupportInitialize).EndInit()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        CType(Me.numDeltaE, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnSaveHolePunch As Button
    Friend WithEvents numStdDev As NumericUpDown
    Friend WithEvents lblThreshold As Label
    Friend WithEvents pnlSeekers As Panel
    Friend WithEvents btnBrowse As Button
    Friend WithEvents ofdVideoIn As OpenFileDialog
    Friend WithEvents lblMinChain As Label
    Friend WithEvents numMinChain As NumericUpDown
    Friend WithEvents btnDetect As Button
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents ToolStripStatusLabel1 As ToolStripStatusLabel
    Friend WithEvents pgbProgress As ToolStripProgressBar
    Friend WithEvents lblDeltaE As Label
    Friend WithEvents numDeltaE As NumericUpDown
End Class
