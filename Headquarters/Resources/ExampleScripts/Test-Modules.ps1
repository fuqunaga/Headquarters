<#
.SYNOPSIS
HeadquartersData\Modules のテストスクリプト

.DESCRIPTION
Test-PathExistence:
指定されたパスが存在するかどうかを確認しまファイルの存在を確認し、存在しない場合はエラーをスローします
主に引数のValidateScript属性でエラーメッセージがわかりやすくなります
例: param([ValidateScript({Test-PathExistence $_})] $Path)

TaskContextUtility:
TaskContextを用いたユーティリティ関数
- New-PSSessionFromTaskContext - TaskContextを用いてPSSessionを作成します
- Convert-PathToUncAndAuth - TaskContextを用いて$(IP)付きのパスをUNCパスに変換します
- Get-PoolFromTaskContext - TaskContextのSharedDictionaryを利用したマルチスレッド対応のオブジェクトプールを取得します

Import-CommandToSession:
ローカルで定義した関数をリモートセッションにインポートします

Install-ModuleIfNotYet:
対象のモジュールが見つかれなければ対話しないようにInstall-Moduleを実行します
インターネットに接続された環境でのみ有効です

7Zip:
指定したセッションで7Zip4Powershellを使用可能な状態にするラッパー

.PARAMETER Path
テストするパス
#>


param(
    [Headquarters.Path()]
    [string]
    $Path="C:\Windows\Temp",

    [string]
    $SharedFolderPath="\\`$(IP)\",

    [Headquarters.TaskContext]
    $TaskContext
)


function Out-Title($title)
{
    Write-Host "■ $title"
}

function Out-Code($code)
{
    Write-Host "> $code"
}

function Out-NewLine($count=1)
{
    Write-Host ("`n" * $($count-1))
}

function RunAndOut-Command($command)
{
    Out-Code $command
    Invoke-Expression $command
}

function LocalFunction()
{
    Write-Host "LocalFunction が呼び出されました"
}

Out-Title "Test-PathExistence"
try {
    $result = RunAndOut-Command "Test-PathExistence -Path `$Path"
    Write-Host $result.ToString()
}
catch
{
    # ここのエラーは許容
    Write-Host $_.Exception.Message
}
Out-NewLine 2


Out-Title "TaskContextUtility"
$code = "`$session = New-PSSessionFromTaskContext -TaskContext `$TaskContext"
Out-Code $code
Invoke-Expression $code
try
{
    RunAndOut-Command "Convert-PathToUncAndAuth -Path `$SharedFolderPath -TaskContext `$TaskContext"
}
catch
{
    # ここのエラーは許容
    Write-Host $_.Exception.Message
}
Out-NewLine 2


Out-Title "Import-CommandToSession"
RunAndOut-Command "Import-CommandToSession -CommandName ""LocalFunction"" -Session `$session"
RunAndOut-Command "Invoke-Command -Session `$session -ScriptBlock { LocalFunction }"
Out-NewLine 2


Out-Title "Install-ModuleIfNotYet"
RunAndOut-Command "Install-ModuleIfNotYet -Name ""7Zip4Powershell"""
Out-NewLine 2


Out-Title "7Zip"
$codes = @(
    "`$sourceFilePath = ""C:\Windows\Temp\HeadquartersTest\TestFile.txt""",
    "`$compressedFilePath = ""C:\Windows\Temp\HeadquartersTest\TestFile.7z""",
    "`$expandFolderPath = ""C:\Windows\Temp\HeadquartersTest\TestFileExpanded"""
)
foreach($code in $codes)
{
    Out-Code $code
    Invoke-Expression $code
}

RunAndOut-Command "Invoke-Command -Session `$session -ScriptBlock {
    New-Item -Path ""C:\Windows\Temp\HeadquartersTest"" -ItemType Directory -Force
    ""テストファイルです"" | Out-File -FilePath `$using:sourceFilePath
}"
Out-NewLine

RunAndOut-Command "Compress-7ZipExt -OutputFilePath `$compressedFilePath -SourcePath `$sourceFilePath -Session `$session"
Write-Host ""

RunAndOut-Command "Expand-7ZipExt -ArchiveFilePath `$compressedFilePath -OutputFolderPath `$expandFolderPath -Session `$session"