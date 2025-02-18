<#
.SYNOPSIS
リモートPCをシャットダウンします
#>


param($TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
   shutdown /f /s
}