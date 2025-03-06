# HQ(Headquarters)
HQ is a GUI tool for remotely operating multiple windows PCs using PowerShell.

[日本語](./README-jp.md)  


<img src="Documents/top.png" />


# QuickStart

## Preparation

### Remote PC
1. Run PowerShell as an administrator, execute the following command.

```
Enable-PSRemoting
```  

<img src="Documents/EnablePSRemoting.png" height="200px"/>


### Local PC
1. Run PowerShell as an administrator, execute the following command.

```
Set-Item WSMan:\localhost\Client\TrustedHosts -Value *
```

<img src="Documents/trustedhosts.png" height="200px" />

1. Download HQ from [Release](https://github.com/fuqunaga/Headquaters/releases) page.
1. Extract and start HQ (requires administrator authority)
1.  If the user on the local PC is different from the one on the remote PC, enter the remote PC's account information:
    1.  Click the gear icon （⚙） in the top right.
    1.  In the Settings window that opens, enter the UserName and UserPassword.
1. Select the script to use from the Scripts menu and click it.
1. If the script requires parameters, enter them.
1. Enter the IP addresses of the target PCs in the IP List table and check them.
1. Click run button(▶).
1. Execute the script by clicking the execute button （▶）.
1. The output will be displayed in the frame at the bottom of the screen.

![alt throuth](Documents/throuth.gif)
  
  
# IP List
A table of IP addresses of target PCs and parameters for each target.  
The script will be executed for each selected IP address.

![alt editIPList](Documents/editIPList.gif)

* IP ranges can be specified using IPAddressRange.
  *  `192.168.10.10-20`
  *  `192.168.0.10 - 192.168.10.2`
* You can import and export CSV files from the three-dot menu in the top right.
  * The first row is the parameter name.



# Scripts
Location:
```
.\HeadquartersData\Profile\Scripts\*.ps1
```
*  You can add your own scripts by placing script files in the above location.
*  For more details, see the [wiki](https://github.com/fuqunaga/Headquarters/wiki/Script).

# Parameter
The state of HQ is saved in the following file:
```
.\HeadquartersData\Profile\setting.json
```


# Tips

### Different account for each PC
If you prepare parameters called `UserName` and `UserPassword` in the IP List, they will be reflected.

### ⚠ Be cautious of security
Please note that all entered values, including `UserPassword`, are saved in plain text.

### System.OutOfMemoryException Occurs
If you specify multiple IPs in the IP List, you may be able to avoid this by limiting the number of tasks executed simultaneously.  
This can be set from the menu next to the execute button (▶).


# Libraries:
* [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
* [Dragablz](https://dragablz.net/)
* [IPAddressRange](https://github.com/jsakamoto/ipaddressrange)
* [Json<span />.NET](https://www.newtonsoft.com/json)
* [Costura.Fody](https://github.com/Fody/Costura)
* [Gu.Wpf.NumericInput](https://github.com/GuOrg/Gu.Wpf.NumericInput)
* [MinVer](https://github.com/adamralph/minver)
* [PowerShell Standard.Library](https://github.com/PowerShell/PowerShellStandard)