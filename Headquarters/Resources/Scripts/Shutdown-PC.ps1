<#
.SYNOPSIS
リモートPCをシャットダウンします
#>


param($TaskContext)

Stop-Computer -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -Force