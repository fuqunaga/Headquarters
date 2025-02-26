<#
.SYNOPSIS
指定したコマンドをリモートPCにインポートします
#>
function Import-CommandToSession
{
    param
    (
        $CommandName,
        $Session
    )

    $commandDefinition = Get-Command -Name $CommandName | Select-Object -ExpandProperty Definition
    $commandScript = "function $CommandName(){ $commandDefinition }"

    Invoke-Command -Session $Session -ScriptBlock ([ScriptBlock]::Create($commandScript))
}