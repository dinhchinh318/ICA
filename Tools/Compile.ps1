$ErrorActionPreference='Stop'
$editor='C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data'
$refs=@(Get-ChildItem "$editor\Managed\UnityEngine\UnityEngine.*Module.dll") + @(Get-ChildItem "$editor\Managed\UnityEngine\UnityEditor.*Module.dll") + @(Get-ChildItem "$editor\NetStandard\ref\2.1.0\*.dll") + @(Get-ChildItem "$editor\NetStandard\compat\2.1.0\shims\netstandard\*.dll")
$refs+=Get-ChildItem 'Library\ScriptAssemblies\*.dll' | Where-Object { $_.Name -in @('Unity.InputSystem.dll','UnityEngine.UI.dll','Unity.RenderPipelines.Universal.Runtime.dll','Unity.RenderPipelines.Core.Runtime.dll') }
$lines=@('-nologo','-target:library','-out:Temp/LumaReef.Validation.dll','-langversion:latest','-define:UNITY_EDITOR,UNITY_6000_0_OR_NEWER')
$lines+= $refs | ForEach-Object { '-r:"'+$_.FullName+'"' }
$lines+=Get-ChildItem 'Assets\Scripts' -Recurse -Filter '*.cs' | ForEach-Object { '"'+$_.FullName+'"' }
$lines | Set-Content 'Temp\LumaReef.rsp'
& "$editor\DotNetSdk\dotnet.exe" "$editor\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll" '@Temp/LumaReef.rsp'
exit $LASTEXITCODE
