<#
.SYNOPSIS
TaskContextからSessionを作成
#>

function New-PSSessionFromTaskContext() {
    param(
        [Headquarters.TaskContext]
        $TaskContext
    )

    return New-PSSession -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential
}