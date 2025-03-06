<#
.SYNOPSIS
対象のモジュールが見つかれなければ対話しないようにInstall-Moduleを実行します
#>
function Install-ModuleIfNotYet 
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()]
        $Name
    )

    if (-not (Get-Module -ListAvailable $Name)) {

        # NuGetがインストールされていない場合はインストールする
        Get-PackageProvider -Name "NuGet" -ForceBootstrap > $null

        Write-Host "モジュールをインストールします : [$Name]"
        Install-Module -Name $Name -Force -Scope CurrentUser
    }
}