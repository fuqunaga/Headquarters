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
        Install-ModuleIfNotYet -Name "7Zip4Powershell"
        Compress-7Zip -ArchiveFileName $using:OutputFilePath -Path $using:SourcePath
    }

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
        $ScriptBlock
    )

    if($Session)
    {
        Import-CommandToSession -CommandName "Install-ModuleIfNotYet" -Session $Session
        Invoke-Command -Session $Session -ScriptBlock $ScriptBlock
    }
    else
    {
        Invoke-Command -ScriptBlock $ScriptBlock
    }
}

Expose-ModuleMember -Function Compress-7ZipExt, Expand-7ZipExt