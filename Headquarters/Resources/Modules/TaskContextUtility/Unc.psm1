<#
.SYNOPSIS
TaskContextから$(IP)付きのパスをUNC形式にし、アクセスできるよう認証も通す
#>

function Convert-PathToUncAndAuth()
{
    param(
        [ValidateNotNullOrEmpty()]
        $Path,
        [Headquarters.TaskContext]
        $TaskContext
    )

    $outputPath = $Path.Replace("`$(IP)", $TaskContext.IpAddress)
    
    if (-not ([System.Uri]$outputPath).IsUnc)
    {
        return $outputPath
    }
    
    $root = [System.IO.Path]::GetPathRoot($outputPath)
    $cred = $TaskContext.Credential

    # UNC認証が必要な場合、一時的なドライブを作成して認証を通すハック
    # https://stackoverflow.com/questions/67469217/powershell-unc-path-with-credentials
    if (-not (Test-Path $root)) {
        $driveName = "Headquarters_TempDrive_$($root -replace '[\\.]', '')"

        # 文字列をOutputしちゃうので > $null で抑制
        New-PSDrive -Name $driveName -PSProvider FileSystem -Root $root -Credential $cred -ErrorAction SilentlyContinue > $null
        if (-not $?) {
            throw "UNCパスの認証に失敗しました。共有フォルダの設定がされていないかもしれません $root"
        }
    }

    return $outputPath
}