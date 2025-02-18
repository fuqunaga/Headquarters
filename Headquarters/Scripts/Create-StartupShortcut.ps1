<#
.SYNOPSIS
リモートPCのスタートアップフォルダにショートカットを作成します

.DESCRIPTION
リモートPCのスタートアップフォルダにショートカットを作成します
以下のファイルが作成されます
C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\<$ShortcutName>.lnk

.PARAMETER ShortcutName
ショートカットファイル名

.PARAMETER TargetPath
ショートカットのリンク先
#>

param($ShortcutName, $TargetPath, $TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
   $item = Get-Item $using:TargetPath
   $WSH = New-Object -ComObject 'WScript.Shell'
   $lnk = $WSH.CreateShortcut("C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\$using:ShortcutName.lnk")
   $lnk.TargetPath = $item.FullName
   $lnk.WorkingDirectory = $item.DirectoryName
   $lnk.Save()
}