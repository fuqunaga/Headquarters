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
    $Path="C:\",

    [string]
    $SharedFolderPath="\\`$(IP)\",

    [Headquarters.TaskContext]
    $TaskContext
)


function LocalFunction()
{
    Write-Output "LocalFunction が呼び出されました`n"
}

# Test-PathExistence
Write-Output "> Test-PathExistence -Path `$Path`n$(Test-PathExistence -Path $Path)`n"


# TaskContextUtility
Write-Output "> `$session = New-PSSessionFromTaskContext -TaskContext `$TaskContext`n"
$session = New-PSSessionFromTaskContext -TaskContext $TaskContext

Write-Output "> Convert-PathToUncAndAuth -Path `$SharedFolderPath -TaskContext `$TaskContext"
try
{
    Convert-PathToUncAndAuth -Path $SharedFolderPath -TaskContext $TaskContext
}
catch
{
    # ここのエラーは許容
    Write-Output $_.Exception.Message
}
# 改行
Write-Output ""


# Import-CommandToSession
Write-Output "> Import-CommandToSession -CommandName ""LocalFunction"" -Session `$session"
Import-CommandToSession -CommandName "LocalFunction" -Session $session

Write-Output "> Invoke-Command -Session `$session -ScriptBlock { LocalFunction }"
Invoke-Command -Session $session -ScriptBlock { LocalFunction }


# Install-ModuleIfNotYet:
Write-Output "> Install-ModuleIfNotYet -Name ""7Zip4Powershell""`n"
Install-ModuleIfNotYet -Name "7Zip4Powershell"


#7Zip
Write-Output "> `$sourceFilePath = ""C:\Windows\Temp\HeadquartersTest\TestFile.txt"""
Write-Output "> `$compressedFilePath = ""C:\Windows\Temp\HeadquartersTest\TestFile.7z"""
Write-Output "> `$expandFolderPath = ""C:\Windows\Temp\HeadquartersTest\TestFileExpanded"""
$sourceFilePath = "C:\Windows\Temp\HeadquartersTest\TestFile.txt"
$compressedFilePath = "C:\Windows\Temp\HeadquartersTest\TestFile.7z"
$expandFolderPath = "C:\Windows\Temp\HeadquartersTest\TestFileExpanded"

Write-Output "> Invoke-Command -Session `$session -ScriptBlock {
>     New-Item -Path ""C:\Windows\Temp\HeadquartersTest"" -ItemType Directory -Force
>     ""テストファイルです"" | Out-File -FilePath `$using:sourceFilePath
> }"
Invoke-Command -Session $session -ScriptBlock {
    New-Item -Path "C:\Windows\Temp\HeadquartersTest" -ItemType Directory -Force
    "テストファイルです" | Out-File -FilePath $using:sourceFilePath
}


Write-Output "> Compress-7ZipExt -OutputFilePath `$compressedFilePath -SourcePath `$sourceFilePath -Session `$session"
Compress-7ZipExt -OutputFilePath $compressedFilePath -SourcePath $sourceFilePath -Session $session

Write-Output "> Expand-7ZipExt -ArchiveFilePath `$compressedFilePath -OutputFolderPath `$expandFolderPath -Session `$session"
Expand-7ZipExt -ArchiveFilePath $compressedFilePath -OutputFolderPath $expandFolderPath -Session $session