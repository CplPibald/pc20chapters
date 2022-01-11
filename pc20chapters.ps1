using namespace System.Windows.Forms
param([string] $file = "")

#[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")
#[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing")
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$PC20CHAPTERSVERSION = "1.2.0"

###################
# Globals
###################
$SCRIPT:saveFileName = $file
$GLOBAL:chapters = @()
$SCRIPT:mainForm = New-Object System.Windows.Forms.Form
$SCRIPT:chaptersPanel = New-Object System.Windows.Forms.Panel

# TODO: change "hhmmss" in label to actual time
# BUG: on open, save button prompts
# TODO: on new chapter, move scroll bar to bottom
# BUG: Why all the empty spaces at the top of the panel when it's scrolled?
# BUG: Assembly problem.  See https://stackoverflow.com/questions/42837447/powershell-unable-to-find-type-when-using-ps-5-classes
# TODO: make delete chapter button work
# TODO: change title bar to include filename and * if modified
# TODO: Support optional global attributes author, title, podcastName, description, fileName
# TODO: maybe support locations/waypoints?

function main {

    $mainForm.Text = "Podcasting 2.0 Chapters Generator by SirBemrose"
    $mainForm.KeyPreview = $True

    $chaptersPanel.AutoScroll = $True
    $chaptersPanel.Location = xy 0 40
    $mainForm.Controls.Add($chaptersPanel)
    $mainForm.add_Resize( { $s = $args[0].ClientSize; $chaptersPanel.Size = xy $s.Width ($s.Height - 40) } )

    # global buttons
    $null = createButton "Open..." -pos (xy 10 5) -size (xy 75 25) -click { load } -parent $mainForm
    $null = createButton "Save" -pos (xy 95 5) -size (xy 75 25) -click { save $SCRIPT:saveFileName } -parent $mainForm
    $null = createButton "Save As..." -pos (xy 180 5) -size (xy 75 25) -click { save } -parent $mainForm
    $nchBtn = createButton "New Chapter" -pos (xy 265 5) -size (xy 95 25) -click { $ch = createChapter; adjustRows; $ch.startTimeBox.Focus() } -parent $mainForm
    $mainForm.AcceptButton = $nchBtn

    $mainForm.Size = xy 900 600

    if ($file) { load $file }

    $mainForm.Add_Shown({$MainForm.Activate()})
    [void] $mainForm.ShowDialog()

}

function load ($file) {
    $SCRIPT:saveFileName = $file

    $validpath = $file -and (Test-Path $file)
    if (-not $validpath ) {
        $ofd = New-Object System.Windows.Forms.OpenFileDialog
        $ofd.InitialDirectory = $PSScriptRoot
        $ofd.DefaultExt = 'json'
        $ofd.Filter = "JSON files (*.json)|*.json|All files|*"
        
        if ($ofd.ShowDialog() -ne [DialogResult]::OK) {
            return
        }
        $file = $ofd.FileName
    }

    $GLOBAL:chapters = @()
    $chaptersPanel.Controls.Clear()

    $json = Get-Content $file | ConvertFrom-Json
    $json.chapters | foreach { $null = createChapter $_ }
    adjustRows
}

function save ($file) {
    # check for invalid chapters
    if ($GLOBAL:chapters | where { -not $_.startTimeBox.Text }) {
        $msg = "Chapters with a blank start time will not be saved.  Save file anyway?"
        if ([DialogResult]::Cancel -eq [MessageBox]::Show($mainForm, $msg, "Saving", [MessageBoxButtons]::OkCancel, [MessageBoxIcon]::Exclamation)) {
            return
        }
    }

    $validpath = $file -and (Test-Path $file -IsValid) -and (Test-Path (Split-Path $file -Parent))
    if (-not $validpath ) {
        $sfd = New-Object System.Windows.Forms.SaveFileDialog
        $sfd.InitialDirectory = $PSScriptRoot
        
        if ($sfd.ShowDialog() -ne [DialogResult]::OK) {
            return
        }
        $file = $sfd.FileName
    }

    $rows = $GLOBAL:chapters | where { $_.startTimeBox.Text } | Sort-Object { [int]$_.startTimeBox.Text }
    $chapterData = foreach ($ch in $rows) {
        $d = @{startTime = [int]$ch.startTimeBox.Text}
        if ($ch.endTimeBox.Text) { $d.endTime = [int]$ch.endTimeBox.Text }
        if ($ch.titleBox.Text) { $d.title = $ch.titleBox.Text }
        if ($ch.urlBox.Text) { $d.url = $ch.urlBox.Text }
        if ($ch.imgBox.Text) { $d.img = $ch.imgBox.Text }
        if (-not $ch.tocBox.Checked) { $d.toc = $false }
        $d
    }

    @{chapters=$chapterData; version=$PC20CHAPTERSVERSION} | ConvertTo-Json | Out-File -Encoding utf8 $file

    $SCRIPT:saveFileName = $file
}

###################
# Classes
###################
class Chapter {
    [GroupBox] $box
    [Button] $deleteButton
    [Button] $dropButton
    [bool] $expanded
    [TextBox] $startTimeBox
    [TextBox] $endTimeBox
    [TextBox] $titleBox
    [TextBox] $urlBox
    [TextBox] $imgBox
    [CheckBox] $tocBox

    Chapter () {
        $this.box = New-Object System.Windows.Forms.GroupBox
        $this.box.Location = xy 10 10
        $this.box.Size = xy 800 60

        $ypx = 10
        $xpx = 5
        $boxheight = 25
        $this.expanded = $false
        $this.deleteButton = createButton "x" -pos (xy 5 0) -size (xy 15 15) -parent $this.box -tag $this -click { removeChapter $args[0].Tag }
        $this.dropButton = createButton "v" -pos (xy 5 25) -size (xy 15 15) -parent $this.box -tag $this -click { toggleExpanded $args[0].Tag }

        # Disabling expand functionality, because the form doesn't need that much space right now
        # But leaving the code in because I sweated over it and it works
        $this.dropButton.Visible = $false 
        $this.deleteButton.Visible = $false 
       

        $null = createLabel "Start (hh:mm:ss):" -pos (xy ($xpx += 20) $ypx) -size (xy 100 $boxheight) -parent $this.box
        $this.startTimeBox = createInput -pos (xy ($xpx += 100) $ypx) -size (xy 50 $boxheight) -parent $this.box
        $this.startTimeBox.add_LostFocus( { validateTimestamp $args[0] } )

        $null = createLabel "Title:" -pos (xy ($xpx += 60) $ypx) -size (xy 40 $boxheight) -parent $this.box
        $this.titleBox = createInput -pos (xy ($xpx += 40) $ypx) -size (xy 240 $boxheight) -parent $this.box

        $null = createLabel "Url:" -pos (xy ($xpx += 250) $ypx) -size (xy 35 $boxheight) -parent $this.box
        $this.urlBox = createInput -pos (xy ($xpx += 35) $ypx) -size (xy 240 $boxheight) -parent $this.box

        $ypx += $boxheight
        $xpx = 5
        $null = createLabel "End (hh:mm:ss):" -pos (xy ($xpx += 20) $ypx) -size (xy 100 $boxheight) -parent $this.box
        $this.endTimeBox = createInput -pos (xy ($xpx += 100) $ypx) -size (xy 50 $boxheight) -parent $this.box
        $this.endTimeBox.add_LostFocus( { validateTimestamp $args[0] } )

        $null = createLabel "Img:" -pos (xy ($xpx += 60) $ypx) -size (xy 40 $boxheight) -parent $this.box
        $this.imgBox = createInput -pos (xy ($xpx += 40) $ypx) -size (xy 240 $boxheight) -parent $this.box

        $this.tocBox = createCheckbox "Toc" -pos (xy ($xpx += 250) $ypx) -size (xy 60 $boxheight) -parent $this.box -value $true
        
    }
}

###################
# Event handlers
###################
function createChapter($values = $null) {
    $ch = New-Object Chapter
    if ($values) {
        if ($values.startTime)  { $ch.startTimeBox.Text = [string]$values.startTime }
        if ($values.endTime)    { $ch.endTimeBox.Text = [string]$values.endTime }
        if ($values.title)      { $ch.titleBox.Text = $values.title }
        if ($values.img)        { $ch.imgBox.Text = $values.img }
        if ($values.url)        { $ch.urlBox.Text = $values.url }
        if ($values.toc)        { $ch.tocBox.Checked = $values.toc }
    }
    $chaptersPanel.Controls.Add($ch.box)
    $GLOBAL:chapters += $ch
    $ch
} 

function adjustRows {
    $rows = $GLOBAL:chapters | Sort-Object { $t = $_.startTimeBox.Text; if ($t) { [int]$t } else { [Int]::MaxValue } }
    $rowYpx = 10
    foreach ($row in $rows) {
        $h = if ($row.expanded) { 140 } else { 65 }
        $row.box.Location = xy 10 $rowYpx
        $row.box.Size = xy 800 $h
        $rowYpx += $h
    }
}

function toggleExpanded ($ch) {
    $ch.expanded = -not $ch.expanded
    # TODO: Toggle visibility of any controls that are in bottom half of box
    adjustRows
}

function removeChapter ($ch) {
    # Not implemented.  Something something race condition
    # For now set the startTime to blank and the chapter won't be saved
}

function validateTimestamp($inputbox) {
    $t = $inputbox.Text
    switch -regex ($t) {
        # integers are cool
        '^\d+$' { }
        # convert from HH:MM:SS
        '^\d+\:\d+(\:\d+)?$' {
            $sec = 0
            $t -split ':' | foreach { $sec = $sec * 60 + $_ }
            $inputbox.Text = $sec
        }
        #everything else is wrong
        default { $inputbox.Text = "" }
    }
}

###################
# Helper functions
###################
function xy($x, $y) { New-Object System.Drawing.Size($x, $y) }

function createButton ($label, $pos, $size, $click, $parent, $tag = $null) {
    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = $label
    $btn.Location = $pos
    $btn.Size = $size
    $btn.add_click($click)
    if ($tag) { $btn.Tag = $tag }
    $parent.Controls.Add($btn)
    $btn
}

function createLabel ($text, $pos, $size = $null, $align = [HorizontalAlignment]::Right, $parent) {
    $t = New-Object System.Windows.Forms.Label
    $t.Text = $text
    $t.Location = $pos
    $t.TextAlign = $align
    $t.AutoSize = $false
    if ($size) { $t.Size = $size }
    $parent.Controls.Add($t)
    $t
}

function createInput ($pos, $size, $text = "", $parent, $tag = $null) {
    $t = New-Object System.Windows.Forms.TextBox
    $t.Text = $text
    $t.Location = $pos
    $t.Size = $size
    if ($tag) { $t.Tag = $tag }
    $parent.Controls.Add($t)
    $t
}

function createCheckbox ($label = "", $pos, $size, $click, $parent, $value = $false, $tag = $null) {
    $btn = New-Object System.Windows.Forms.CheckBox
    $btn.Text = $label
    $btn.Location = $pos
    $btn.Size = $size
    $btn.Checked = $value
    $btn.add_click($click)
    if ($tag) { $btn.Tag = $tag }
    $parent.Controls.Add($btn)
    $btn
}

main
