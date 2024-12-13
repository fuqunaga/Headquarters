<#
.SYNOPSIS
対象のモジュールが見つかれなければ対話しないようにInstall-Moduleを実行します
#>
function Install-ModuleIfNotYet {
    param
    (
        [ValidateNotNullOrEmpty()]
        $ModuleName
    )

    if (-not (Get-Module -ListAvailable $ModuleName)) {

        # NuGetがインストールされていない場合はインストールする
        Get-PackageProvider -Name "NuGet" -ForceBootstrap > $null

        Write-Output "モジュールをインストールします : [$ModuleName]"
        Install-Module -Name $ModuleName -Force -Scope CurrentUser
    }
}