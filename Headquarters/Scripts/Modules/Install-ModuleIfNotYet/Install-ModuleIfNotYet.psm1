<#
.SYNOPSIS
対象のモジュールが見つかれなければ対話しないようにInstall-Moduleを実行します
#>
function Install-ModuleIfNotYet {
    param
    (
        [ValidateNotNullOrEmpty()]
        $Name
    )

    if (-not (Get-Module -ListAvailable $Name)) {

        # NuGetがインストールされていない場合はインストールする
        Get-PackageProvider -Name "NuGet" -ForceBootstrap > $null

        Write-Output "モジュールをインストールします : [$Name]"
        Install-Module -Name $Name -Force -Scope CurrentUser
    }
}