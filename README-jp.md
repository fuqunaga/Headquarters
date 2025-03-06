# HQ(Headquarters)

HQはPowerShellを用いて複数のPCをリモートで操作するためのGUIツールです  

<img src="Documents/top.png"/>


# クイックスタート

## 準備
### リモートPC
1. PowerShellを管理者として起動、以下のコマンドを実行  

```
Enable-PSRemoting
```  

<img src="Documents/EnablePSRemoting.png" height="200px"/>


### ローカルPC
1. PowerShellを管理者として起動し、以下のコマンドを実行  

```
Set-Item WSMan:\localhost\Client\TrustedHosts -Value *
```
<img src="Documents/trustedhosts.png" height="200px" />

## 実行
1. [Release](https://github.com/fuqunaga/Headquaters/releases)ページからHQをダウンロード
1. 解凍し、HQを起動（管理者権限が必要）
1. ローカルPCとリモートPCのユーザーが異なる場合はリモートPCのアカウント情報を入力
      1. 右上の歯車マーク（⚙）をクリック
      2. Settingsウィンドウが開くので、ユーザー名とパスワードを入力
2. Scriptsから使用するスクリプトを選んでクリック
3. スクリプトにパラメータが必要な場合は入力
4. IP List表に対象PCのIPを記入しチェック
5. 実行ボタン（▶）でスクリプトを実行
6. 画面下部の枠に出力が表示されます  
  
![alt throuth](Documents/throuth.gif)
  

# IP List
対象となるPCのIPアドレスと、対象ごとのパラメータを記したデータ  
ここで選択したIPアドレスごとにスクリプトが実行されます

![alt editIPList](Documents/editIPList.gif)

* IPは範囲指定できます（[IPAddressRange](https://github.com/jsakamoto/ipaddressrange/)）
  * `192.168.10.10-20`
  * `192.168.0.10 - 192.168.10.2`
* 右上の三点メニューからCSVファイルをインポート、エクスポートできます
  * 一行目はパラメータ名になります


# スクリプト
場所
```
.\HeadquartersData\Profile\Scripts\*.ps1
```

 * 上記場所にスクリプトファイルを置くことでユーザ独自のスクリプトを追加できます
 * 詳しくは[wiki](https://github.com/fuqunaga/Headquarters/wiki/Script)


# パラメーター
HQに入力した値やタブの状態などは次のファイルに保存されます
```
.\HeadquartersData\Profile\setting.json
```

# TIPS

### PCごとにアカウントが異なる
IP Listに`UserName`、`UserPassword`というパラメータを用意するとそちらが反映されます

### ⚠セキュリティに注意
`UserPassword`を含め、入力した値はすべて平文で保存されるのでご注意ください

### System.OutOfMemoryExceptionが出る
IP Listで複数のIPを指定している場合は同時実行するタスク数を制限することで回避できることがあります  
実行ボタン（▶）横のメニューから設定できます


# ライブラリ
* [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
* [Dragablz](https://dragablz.net/)
* [IPAddressRange](https://github.com/jsakamoto/ipaddressrange)
* [Json<span />.NET](https://www.newtonsoft.com/json)
* [Costura.Fody](https://github.com/Fody/Costura)
* [Gu.Wpf.NumericInput](https://github.com/GuOrg/Gu.Wpf.NumericInput)
* [MinVer](https://github.com/adamralph/minver)
* [PowerShell Standard.Library](https://github.com/PowerShell/PowerShellStandard)