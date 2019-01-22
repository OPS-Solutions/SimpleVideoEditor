<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me.btnいくよ = New System.Windows.Forms.Button()
        Me.numThreshold = New System.Windows.Forms.NumericUpDown()
        Me.lblThreshold = New System.Windows.Forms.Label()
        Me.pnlSeekers = New System.Windows.Forms.Panel()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.ofdVideoIn = New System.Windows.Forms.OpenFileDialog()
        Me.lblMinChain = New System.Windows.Forms.Label()
        Me.numMinChain = New System.Windows.Forms.NumericUpDown()
        Me.pgbProgress = New System.Windows.Forms.ProgressBar()
        CType(Me.numThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.numMinChain, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'btnいくよ
        '
        Me.btnいくよ.BackColor = System.Drawing.SystemColors.Control
        Me.btnいくよ.Enabled = False
        Me.btnいくよ.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnいくよ.Image = Global.SimpleVideoEditor.My.Resources.Resources.Save
        Me.btnいくよ.Location = New System.Drawing.Point(260, 215)
        Me.btnいくよ.Name = "btnいくよ"
        Me.btnいくよ.Size = New System.Drawing.Size(72, 35)
        Me.btnいくよ.TabIndex = 10
        Me.btnいくよ.UseVisualStyleBackColor = False
        '
        'numThreshold
        '
        Me.numThreshold.Location = New System.Drawing.Point(260, 156)
        Me.numThreshold.Name = "numThreshold"
        Me.numThreshold.Size = New System.Drawing.Size(72, 20)
        Me.numThreshold.TabIndex = 11
        Me.numThreshold.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'lblThreshold
        '
        Me.lblThreshold.Location = New System.Drawing.Point(257, 140)
        Me.lblThreshold.Name = "lblThreshold"
        Me.lblThreshold.Size = New System.Drawing.Size(75, 13)
        Me.lblThreshold.TabIndex = 12
        Me.lblThreshold.Text = "Threshold"
        Me.lblThreshold.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'pnlSeekers
        '
        Me.pnlSeekers.AutoScroll = True
        Me.pnlSeekers.Location = New System.Drawing.Point(12, 10)
        Me.pnlSeekers.Name = "pnlSeekers"
        Me.pnlSeekers.Size = New System.Drawing.Size(239, 240)
        Me.pnlSeekers.TabIndex = 13
        '
        'btnBrowse
        '
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
        Me.lblMinChain.Location = New System.Drawing.Point(257, 77)
        Me.lblMinChain.Name = "lblMinChain"
        Me.lblMinChain.Size = New System.Drawing.Size(75, 13)
        Me.lblMinChain.TabIndex = 16
        Me.lblMinChain.Text = "Min Chain"
        Me.lblMinChain.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'numMinChain
        '
        Me.numMinChain.Location = New System.Drawing.Point(260, 93)
        Me.numMinChain.Maximum = New Decimal(New Integer() {3600, 0, 0, 0})
        Me.numMinChain.Minimum = New Decimal(New Integer() {30, 0, 0, 0})
        Me.numMinChain.Name = "numMinChain"
        Me.numMinChain.Size = New System.Drawing.Size(72, 20)
        Me.numMinChain.TabIndex = 15
        Me.numMinChain.Value = New Decimal(New Integer() {600, 0, 0, 0})
        '
        'pgbProgress
        '
        Me.pgbProgress.Location = New System.Drawing.Point(260, 39)
        Me.pgbProgress.Name = "pgbProgress"
        Me.pgbProgress.Size = New System.Drawing.Size(72, 23)
        Me.pgbProgress.TabIndex = 18
        '
        'HolePuncherForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(344, 262)
        Me.Controls.Add(Me.pgbProgress)
        Me.Controls.Add(Me.lblMinChain)
        Me.Controls.Add(Me.numMinChain)
        Me.Controls.Add(Me.btnBrowse)
        Me.Controls.Add(Me.pnlSeekers)
        Me.Controls.Add(Me.lblThreshold)
        Me.Controls.Add(Me.numThreshold)
        Me.Controls.Add(Me.btnいくよ)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "HolePuncherForm"
        Me.Text = "HolePuncherForm"
        CType(Me.numThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.numMinChain, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnいくよ As Button
    Friend WithEvents numThreshold As NumericUpDown
    Friend WithEvents lblThreshold As Label
    Friend WithEvents pnlSeekers As Panel
    Friend WithEvents btnBrowse As Button
    Friend WithEvents ofdVideoIn As OpenFileDialog
    Friend WithEvents lblMinChain As Label
    Friend WithEvents numMinChain As NumericUpDown
    Friend WithEvents pgbProgress As ProgressBar
End Class
