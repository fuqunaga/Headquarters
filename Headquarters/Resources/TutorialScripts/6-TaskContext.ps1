<#
.SYNOPSIS
TaskContext解説用のスクリプトです

.DESCRIPTION
$TaskContextというパラメーターを指定すると特殊なHeadquarters.TaskContext型の値が渡されます
TaskContextはHeadquartersからスクリプトに渡される特殊な値を取得するためのオブジェクトです
このパラメーターはUIに表示されません

- $TaskContext.IpAddress: 現在のタスクのIPアドレス
- $TaskContext.UserName: SettingWindowかIP Listで設定したユーザー名
- $TaskContext.UserPassword: SettingWindowかIP Listで設定したユーザーパスワード
- $TaskContext.Credential: 上記UserNameとUserPasswordを元に作成されたPSCredentialオブジェクト
- $TaskContext.SharedDictionary:
      タスク間で共有されるConcurrentDictionary<string, string>オブジェクト
      使用用途は決まっておらず自由に利用できます
#>


param(
    $TaskContext
)

Write-Host "`$TaskContext.IpAddress: [$($TaskContext.IpAddress)]
`$TaskContext.UserName: [$($TaskContext.UserName)]
`$TaskContext.UserPassword: [$($TaskContext.UserPassword)]
`$TaskContext.Credential: [$($TaskContext.Credential)]
`$TaskContext.SharedDictionary: [$($TaskContext.SharedDictionary)]"
