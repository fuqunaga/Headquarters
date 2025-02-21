<#
.SYNOPSIS
7Zipの圧縮、解凍

.DESCRIPTION
7Zip4Powershellを使用してファイルを圧縮、解凍します
リモート用に$Sessionを受け付けます
#>


function Compress-7ZipExt()
{
    param
    (
        [ValidateNotNullOrEmpty()]
        $ArchiveFileName,
        [ValidateNotNullOrEmpty()]
        $Path,
        [bool]$EnableOutput=$true,
        $Session
    )


    Invoke-CommandExt -Session $Session -ScriptBlock {
        Install-ModuleIfNotYet -Name "7Zip4Powershell"
        Compress-7Zip -ArchiveFileName $ArchiveFileName -Path $Path
    }

    if($EnableOutput)
    {
        $optionString = ""
        if($Session)
        {
            $optionString = "(Remote)"
        }
        Write-Output "圧縮$optionString $Path -> $ArchiveFileName"
    }
}


function Expand-7ZipExt()
{
    param
    (
        [ValidateNotNullOrEmpty()]
        $ArchiveFileName,
        [ValidateNotNullOrEmpty()]
        $TargetPath,
        [bool]$EnableOutput=$true,
        $Session
    )


    Invoke-CommandExt -Session $Session -ScriptBlock {
        Install-ModuleIfNotYet -Name "7Zip4Powershell"
        Expand-7Zip -ArchiveFileName $using:ArchiveFileName -TargetPath $using:TargetPath
    }

    if($EnableOutput)
    {
        $optionString = ""
        if($Session)
        {
            $optionString = "(Remote)"
        }
        Write-Output "解凍$optionString $ArchiveFileName -> $TargetPath"
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