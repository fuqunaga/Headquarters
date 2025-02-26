<#
.SYNOPSIS
リモートPCを再起動します
#>

param($TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
   shutdown /r
}