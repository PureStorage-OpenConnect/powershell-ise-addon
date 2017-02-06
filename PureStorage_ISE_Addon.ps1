add-type -path "C:\Users\Barkz\Box Sync\barkz\Visual Studio\PureStorage_ISE_Addon\PureStorage_ISE_Addon\bin\Debug\PureStorage_ISE_Addon.dll"

$psISE.CurrentPowerShellTab.VerticalAddOnTools.Add('PureStorage_ISE_Addon', [PureStorage_ISE_Addon.UserControl1], $true)