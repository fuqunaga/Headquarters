<#
.SYNOPSIS
7Zipの圧縮、解凍

.DESCRIPTION
7Zip4Powershellを使用してファイルを圧縮、解凍します
リモート用に$Sessionを受け付けます
#>


function Compress-7ZipExt()
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()]
        $OutputFilePath,
        [ValidateNotNullOrEmpty()]
        $SourcePath,
        [bool]$EnableOutput=$true,
        $Session
    )


    Invoke-CommandExt -Session $Session -ScriptBlock {
        param($OutputFilePath, $SourcePath)
        Install-ModuleIfNotYet -Name "7Zip4Powershell"
        Compress-7Zip -ArchiveFileName $OutputFilePath -Path $SourcePath
    } -ArgumentList $OutputFilePath, $SourcePath

    if($EnableOutput)
    {
        $optionString = ""
        if($Session)
        {
            $optionString = "(Remote)"
        }
        Write-Host "圧縮$optionString $SourcePath -> $OutputFilePath"
    }
}


function Expand-7ZipExt()
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()]
        $ArchiveFilePath,
        [ValidateNotNullOrEmpty()]
        $OutputFolderPath,
        [bool]$EnableOutput=$true,
        $Session
    )


    Invoke-CommandExt -Session $Session -ScriptBlock {
        Install-ModuleIfNotYet -Name "7Zip4Powershell"
        Expand-7Zip -ArchiveFileName $using:ArchiveFilePath -TargetPath $using:OutputFolderPath
    }

    if($EnableOutput)
    {
        $optionString = ""
        if($Session)
        {
            $optionString = "(Remote)"
        }
        Write-Host "解凍$optionString $ArchiveFilePath -> $OutputFolderPath"
    }
}


function Invoke-CommandExt()
{
    param
    (
        $Session,
        $ScriptBlock,
        $ArgumentList
    )

    if($Session)
    {
        Import-CommandToSession -CommandName "Install-ModuleIfNotYet" -Session $Session
        Invoke-Command -Session $Session -ScriptBlock $ScriptBlock -ArgumentList $ArgumentList
    }
    else
    {
        Invoke-Command -ScriptBlock $ScriptBlock -ArgumentList $ArgumentList
    }
}

Expose-ModuleMember -Function Compress-7ZipExt, Expand-7ZipExt