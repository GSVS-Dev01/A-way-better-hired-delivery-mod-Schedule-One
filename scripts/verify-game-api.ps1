[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $MelonLoaderRoot,

    [string] $ExpectedAssemblySha256 = "0D2EB364F3E84120AF7CCC9FA6BAFD597D42D495EBACC3A260CB4CA0CF0513DA"
)

$ErrorActionPreference = "Stop"
$assemblyPath = Join-Path $MelonLoaderRoot "Il2CppAssemblies\Assembly-CSharp.dll"
$cecilPath = Join-Path $MelonLoaderRoot "net6\Mono.Cecil.dll"

if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) { throw "Missing game assembly: $assemblyPath" }
if (-not (Test-Path -LiteralPath $cecilPath -PathType Leaf)) { throw "Missing Mono.Cecil reference: $cecilPath" }

$actualHash = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
if ($ExpectedAssemblySha256 -and $actualHash -ne $ExpectedAssemblySha256) {
    throw "Assembly-CSharp.dll hash mismatch. Expected $ExpectedAssemblySha256, found $actualHash."
}

[void] [System.Reflection.Assembly]::LoadFrom($cecilPath)
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)

try {
    function Get-RequiredType([string] $FullName) {
        $type = $assembly.MainModule.GetTypes() | Where-Object { $_.FullName -ceq $FullName } | Select-Object -First 1
        if (-not $type) { throw "Missing type: $FullName" }
        return $type
    }

    function Assert-Method([string] $TypeName, [string] $MethodName, [string[]] $ParameterTypes = @()) {
        $type = Get-RequiredType $TypeName
        $matches = @($type.Methods | Where-Object {
            if ($_.Name -cne $MethodName -or $_.Parameters.Count -ne $ParameterTypes.Count) { return $false }
            for ($index = 0; $index -lt $ParameterTypes.Count; $index++) {
                if ($_.Parameters[$index].ParameterType.FullName -cne $ParameterTypes[$index]) { return $false }
            }
            return $true
        })
        if ($matches.Count -ne 1) {
            throw "Expected one method: $TypeName::$MethodName($($ParameterTypes -join ', ')); found $($matches.Count)."
        }
        Write-Output "PASS method $TypeName::$MethodName($($ParameterTypes -join ', '))"
    }

    function Assert-Property([string] $TypeName, [string] $PropertyName, [string] $PropertyType) {
        $type = Get-RequiredType $TypeName
        $matches = @($type.Properties | Where-Object { $_.Name -ceq $PropertyName -and $_.PropertyType.FullName -ceq $PropertyType })
        if ($matches.Count -ne 1) { throw "Expected property $TypeName::$PropertyName of type $PropertyType; found $($matches.Count)." }
        Write-Output "PASS property $TypeName::$PropertyName"
    }

    function Assert-EnumValue([string] $TypeName, [string] $Name, [int] $Value) {
        $type = Get-RequiredType $TypeName
        $field = $type.Fields | Where-Object { $_.Name -ceq $Name } | Select-Object -First 1
        if (-not $field -or [int] $field.Constant -ne $Value) { throw "Expected enum value $TypeName::$Name = $Value." }
        Write-Output "PASS enum $TypeName::$Name = $Value"
    }

    Assert-EnumValue "Il2CppScheduleOne.Employees.EEmployeeType" "Handler" 1
    Assert-Method "Il2CppScheduleOne.Employees.EmployeeManager" "CreateNewEmployee" @(
        "Il2CppScheduleOne.Property.Property", "Il2CppScheduleOne.Employees.EEmployeeType"
    )
    Assert-Method "Il2CppScheduleOne.Employees.EmployeeManager" "GetEmployeePrefab" @(
        "Il2CppScheduleOne.Employees.EEmployeeType"
    )
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "EmployeeType" "Il2CppScheduleOne.Employees.EEmployeeType"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "PaidForToday" "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "Fire"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "LeavePropertyAndDespawn"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "GetHome"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "RemoveDailyWage"
    Assert-Method "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" "Confirm"
    Assert-Property "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" "selectedEmployeeType" "Il2CppScheduleOne.Employees.EEmployeeType"
    Assert-Method "Il2CppScheduleOne.Delivery.DeliveryManager" "IsLoadingBayFree" @(
        "Il2CppScheduleOne.Property.Property", "System.Int32"
    )
    Assert-Method "Il2CppScheduleOne.Delivery.LoadingDock" "SetOccupant" @(
        "Il2CppScheduleOne.Vehicles.LandVehicle"
    )
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "DynamicOccupant" "Il2CppScheduleOne.Vehicles.LandVehicle"
    Assert-Property "Il2CppScheduleOne.Property.Property" "LoadingDocks" 'Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray`1<Il2CppScheduleOne.Delivery.LoadingDock>'
    Assert-Property "Il2CppScheduleOne.Property.Property" "PropertyCode" "System.String"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "IsPlayerOwned" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "GUID" "Il2CppSystem.Guid"
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "SetTransform" @("UnityEngine.Vector3", "UnityEngine.Quaternion")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "SetVisible" @("System.Boolean")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "ExitPark" @("System.Boolean")
    Assert-Method "Il2CppScheduleOne.Map.ParkingSpot" "SetOccupant" @("Il2CppScheduleOne.Vehicles.LandVehicle")
    Assert-Method "Il2CppScheduleOne.NPCs.NPC" "EnterVehicle" @(
        "Il2CppFishNet.Connection.NetworkConnection", "Il2CppScheduleOne.Vehicles.LandVehicle"
    )
    Assert-Method "Il2CppScheduleOne.NPCs.NPC" "ExitVehicle"
    Assert-Method "Il2CppScheduleOne.Persistence.SaveManager" "Save" @("System.String")

    Write-Output "PASS Assembly-CSharp.dll SHA256 $actualHash"
    Write-Output "Schedule I 0.4.6f13 IL2CPP Vehicle Handlers API verification passed."
}
finally {
    $assembly.Dispose()
}
