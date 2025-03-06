<#
.SYNOPSIS
文字列出力の例です

.DESCRIPTION
Headquartersは出力ストリームに文字列が渡されるとフィールドに表示します
各コマンドの詳細はPowerShell公式ドキュメントを参照してください

- Write-Information: Headquartersに表示されます
  一般的なメッセージ出力におすすめです

- Write-Host: Write-Infomationのラッパーです

- Write-Output: PowerShellのパイプラインに出力されます
  パイプラインの出力がHeadquartersまで渡り、オブジェクトがstring型であった場合のみHeadquartersに表示されます

- Write-Warning: "[Warning]"付きでHeadquartersに表示されます

- Write-Verbose: "[Verbose]"付きでHeadquartersに表示されます
  $VerbosePreferenceがデフォルトでは"SilentlyContinue"のためそのままでは表示されません

- Write-Debug: "[Debug]"付きでHeadquartersに表示されます
  $DebugPreferenceがデフォルトでは"SilentlyContinue"のためそのまま表示されません

- Write-Progress: "-Activity"と"-PercentComplete"がHeadquartersに表示されます
  同一の"-Id"で呼ぶと表示が更新されます
  "-Completed"で表示が削除されます

- Write-Error: "[Error]"付きでHeadquartersに表示されます
  Headquartersはエラーが発生するとそのタスクを終了します

#>

$DebugPreference = "Continue"
$VerbosePreference = "Continue"

Write-Information "Write-Information"
Write-Host "Write-Host"
Write-Output "Write-Output"

Write-Warning "Write-Warning"
Write-Verbose "Write-Verbose"
Write-Debug "Write-Debug"

Write-Progress "Write-Progress0"
for($i = 0; $i -lt 100; $i++){
    Write-Progress "Write-Progress1" -Id 1 -PercentComplete ($i)
    Start-Sleep -Milliseconds 100
}
Write-Progress "Write-Progress1" -Id 1 -Completed

Write-Error "Write-Error"