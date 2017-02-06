Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

$AddonPath = "C:\Users\Barkz\Box Sync\barkz\Visual Studio\PureStorage_ISE_Addon\PureStorage_ISE_Addon\bin\Debug\PureStorage_ISE_Addon.dll"
Add-Type -Path $AddonPath 

$psISE.CurrentPowerShellTab.VerticalAddOnTools.Add('Pure Storage FlashArray ISE Addon', [PureStorage_ISE_Addon.UserControl1], $true)