<#
.SYNOPSIS
リモートPCを再起動します
#>

param($Session)

Invoke-Command $Session -ScriptBlock {
   shutdown /r
}