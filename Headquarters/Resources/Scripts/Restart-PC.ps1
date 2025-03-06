<#
.SYNOPSIS
リモートPCを再起動します
#>

param($TaskContext)

Restart-Computer -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -Force