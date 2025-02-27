<#
.SYNOPSIS
TaskContextからSessionを作成
#>

function New-PSSessionFromTaskContext() 
{
    [CmdletBinding()]
    param(
        $TaskContext
    )

    return New-PSSession -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential
}