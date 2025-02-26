<#
.SYNOPSIS
HeadquartersData\Modules 内のモジュールのテストスクリプト

.DESCRIPTION
Test-PathExistence:
指定されたパスが存在するかどうかを確認しまファイルの存在を確認し、存在しない場合はエラーをスローします
主に引数のValidateScript属性でエラーメッセージがわかりやすくなります
例: param([ValidateScript({Test-PathExistence $_})] $Path)

TaskContextUtility:
TaskContextを用いたユーティリティ関数
- Create-PSSession : TaskContextを用いてPSSessionを作成します

7Zip:
7Zip4Powershellのラッパー
指定したセッションで7Zip4Powershellのインストールと操作を行います

Import-CommandToSession:
指定したコマンドをリモートPCにインポートします
#>


param(
    [Headquarters.Path()]
    $Path,
    $TaskContext)


function LocalFunction()
{
    Write-Output "LocalFunction was called"
}

# Test-PathExistence
Write-Output "> Test-PathExistence -Path `$Path`n$(Test-PathExistence -Path $Path)"

# TaskContextUtility
Write-Output "> New-PSSessionFromTaskContext -TaskContext `$TaskContext`n$(New-PSSessionFromTaskContext -TaskContext $TaskContext)"

#7Zip
#Write-Output "7Zip : $(7Zip -Path $Path -DestinationPath 'C:\temp')"


# Import-CommandToSession
Invoke-Command -ComputerName $TaskContext.IPAddress -Credential $TaskContext.Credential -ScriptBlock {
    LocalFunction
}

