<#
.SYNOPSIS
パラメーターの型によるUIを確認するためのスクリプトです

.DESCRIPTION
bool型、数値型はUIの表示が変わります

.PARAMETER BooleanParameter
bool
param(
    [bool]$BooleanParameter
)

.PARAMETER SwitchParameter
switch
param(
    [switch]$SwitchParameter
)

.PARAMETER IntParameter
int
param(
    [int]$IntParameter
)

.PARAMETER UInt32Parameter
Uint32
param(
    [Uint32]$UInt32Parameter
)
＊PowerShell5.1ではuintは定義されていないのでUint32を使用してください

.PARAMETER FloatParameter
float
param(
    [float]$FloatParameter
)

.PARAMETER DoubleParameter
double
param(
    [double]$DoubleParameter
)
#>


param(
    [bool]$BooleanParameter,
    [switch]$SwitchParameter,
    [int]$IntParameter,
    [Uint32]$UInt32Parameter,
    [float]$FloatParameter,
    [double]$DoubleParameter
)

Write-Host "BooleanParameter: [$BooleanParameter]
SwitchParameter: [$SwitchParameter]
IntParameter: [$IntParameter]
UInt32Parameter: [$UInt32Parameter]
FloatParameter: [$FloatParameter]
DoubleParameter: [$DoubleParameter]"
