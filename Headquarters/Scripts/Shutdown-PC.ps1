<#
.SYNOPSIS
リモートPCをシャットダウンします
#>


param($Session)

Invoke-Command $Session -ScriptBlock {
   shutdown /f /s
}