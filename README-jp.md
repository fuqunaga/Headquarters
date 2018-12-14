# HQ(Headquaters)
HQはPowerShellを用いて複数のPCをリモートで操作するためのGUIツールです


# クイックスタート
1. リモートPC上でリモート操作の許可
   1. PowerShellを管理者として起動
   1. `Enable-PSRemoting`
1. [Release]ページからダウンロード
1. 解凍し、HQを起動
1. リモートPCのユーザ名、パスワードを入力
1. Scriptsから使用するスクリプトを選んでクリック
1. スクリプトにパラメータが必要な場合は入力
1. IP List表に対象PCのIPを記入しチェック
1. 実行ボタン（▶）でスクリプトを実行
1. 画面下部の枠に出力が表示されます  
   正常終了時は`☑[IPアドレス]:` のように表示されます
  
  
# スクリプト
場所
```
.\*.ps1
.\Scripts\*.ps1
```

 * IP ListのIPごとにスクリプトが呼ばれます
 * $session でリモートPCのPSSessionを受け取れます
 * param() で指定した変数がHQ上で表示され編集できます

 ## 例
  
# IP List
対象となるPCのIPアドレスと、対象別のパラメータを記したデータ
* `.\ipList.csv` に保存される
* 一行目はパラメータ名
* IPは範囲指定可（[IPAddressRange](https://github.com/jsakamoto/ipaddressrange/)）
  * 192.168.10.10-20
  * 192.168.0.10 - 192.168.10.20
  
HQ上で編集可能
[gif]


# パラメータ
* IP Listにないパラメータは`.\param.json`に保存される
* ユーザ名とパスワードも保存される。セキュリティに注意

