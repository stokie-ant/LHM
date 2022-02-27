# LCD Smartie Plugin For Libre Hardware Monitor

Includes binaries from Libre Hardware Monitor https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
Libre Hardware Monitor is licenced under MPL 2.0

#### Notes
LCD Smartie has to be run as administrator to use this plugin.
DNBridge.dll is required.
Copy LHM.dll, LibreHardwareMonitorLib.dll and HidSharp.dll to LCD Smartie plugins dir.
On first run it should create a report called LHMreport.txt in the plugins folder. It will have basic variables for accessing each sensor. Delete this file to have it re-created.

- function 1 gets hardware or sensor name
- function 2 gets subhardware name
- function 3 gets sensor value

#### Syntax:
##### function 1
###### takes 1 parameter:

>HardwareIndex

```sh
$dll(LHM.dll,1,0,0)
```
or
>HardwareIndex#SensorIndex
```sh
$dll(LHM.dll,1,1#0,0)
```
or
>HardwareIndex#SubHardwareIndex#SensorIndex
```sh
$dll(LHM.dll,1,0#0#0,0)
```
`second parameter ignored but must be present (,0)`

##### function 2
###### takes 1 parameter for cases where name is sub hardware not sensor

>HardwareIndex#SubHardware
```sh
$dll(LHM.dll,2,0#0,0)
```
`second parameter ignored but must be present (,0)`

##### function 3
###### takes 2 parameters
Param1: sensor:

>HardwareIndex#SensorIndex
```sh
$dll(LHM.dll,3,1#0,0)
```
or
>HardwareIndex#SubHardwareIndex#SensorIndex
```sh
$dll(LHM.dll,3,0#0#0,0)
```
param2: maths for value

>0 Direct value as is

or
>operator#operand

operator:
index one of
| Index | Operator | effect|
| ----- | -------- |------ |
1|+|addition|
2|-|subtraction|
3|/|division|
4|*|multiplication|

operand:
Value, value to add, subtract, divide or multiply by

or
>Operator#Operand#DecimalPlaces

`Operator and operand can be zero to just trim the decimal places of a direct number`

Examples:
get name, hardware index, 
```sh
$dll(LHM.dll,1,0,0)
```
get value, hardware index 0# sub hardware 0# sensor 4, multiply# by 6# trim decimal to 2
```sh
$dll(LHM.dll,3,0#0#4,4#6#2)
```
