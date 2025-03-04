<#
.SYNOPSIS
IPアドレス毎の処理の前後に１度だけ呼ばれる処理

.DESCRIPTION
通常Headquartersはスクリプトをそのまま実行しますが、
次の名前の関数を定義するとIPアドレス毎の処理と前後の処理を分けて記述することができます

- BeginTask: IPアドレス毎の処理の前に１度だけ呼ばれる処理
- IpAddressTask: IPアドレス毎の処理
- EndTask: IPアドレス毎の処理の後に１度だけ呼ばれる処理

$TaskContextについて
BeginTaskとEndTaskでも$TaskContextを受け取れますが$TaskContext.IpAddressは空文字になります
また、$TaskContext.SharedDictionaryはBeginTask、IpAddressTask、EndTaskで同じオブジェクトが渡されます
BeginTaskで値を設定しIpAddressTaskで利用するといったことが可能です
#>


function BeginTask()
{
    Write-Host "BeginTask"
}

function IpAddressTask()
{
    param(
        $TaskContext    
    )
    Write-Host "IpAddressTask : `$TaskContext.IpAddress = [$($TaskContext.IpAddress)]"
}

function EndTask() 
{
    Write-Host "EndTask"
}

