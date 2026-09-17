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

    function Assert-Method(
        [string] $TypeName,
        [string] $MethodName,
        [string[]] $ParameterTypes = @(),
        [string] $ReturnType = ""
    ) {
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
        if ($ReturnType -and $matches[0].ReturnType.FullName -cne $ReturnType) {
            throw "Expected $TypeName::$MethodName to return $ReturnType; found $($matches[0].ReturnType.FullName)."
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
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.EmployeeManager" -MethodName "CreateEmployee" -ParameterTypes @(
        "Il2CppScheduleOne.Property.Property",
        "Il2CppScheduleOne.Employees.EEmployeeType",
        "System.String",
        "System.String",
        "System.String",
        "System.Boolean",
        "System.Int32",
        "UnityEngine.Vector3",
        "UnityEngine.Quaternion",
        "System.String"
    ) -ReturnType "System.Void"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.EmployeeManager" -MethodName "CreateEmployee_Server" -ParameterTypes @(
        "Il2CppScheduleOne.Property.Property",
        "Il2CppScheduleOne.Employees.EEmployeeType",
        "System.String",
        "System.String",
        "System.String",
        "System.Boolean",
        "System.Int32",
        "UnityEngine.Vector3",
        "UnityEngine.Quaternion",
        "System.String"
    ) -ReturnType "Il2CppScheduleOne.Employees.Employee"
    Assert-Method "Il2CppScheduleOne.Employees.EmployeeManager" "GetEmployeePrefab" @(
        "Il2CppScheduleOne.Employees.EEmployeeType"
    )
    Assert-Property "Il2CppScheduleOne.Employees.EmployeeManager" "PackagerPrefab" "Il2CppScheduleOne.Employees.Packager"
    Assert-Property "Il2CppScheduleOne.Employees.EmployeeManager" "AllEmployees" 'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Employees.Employee>'
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "Type" "Il2CppScheduleOne.Employees.EEmployeeType"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "EmployeeType" "Il2CppScheduleOne.Employees.EEmployeeType"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "SigningFee" "System.Single"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "DailyWage" "System.Single"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "AssignedProperty" "Il2CppScheduleOne.Property.Property"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "PaidForToday" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Employees.Employee" "Fired" "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "AssignProperty" @("Il2CppScheduleOne.Property.Property", "System.Boolean")
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "UnassignProperty"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Employee" -MethodName "CanWork" -ReturnType "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "UpdateBehaviour"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Employee" -MethodName "IsAnyWorkInProgress" -ReturnType "System.Boolean"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Employee" -MethodName "ShouldIdle" -ReturnType "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "SetIdle" @("System.Boolean")
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "SetIsPaid"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Employee" -MethodName "IsPayAvailable" -ReturnType "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "Fire"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "LeavePropertyAndDespawn"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "GetHome"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Employee" -MethodName "GetNPCData" -ReturnType "Il2CppScheduleOne.Persistence.Datas.NPCData"
    Assert-Method "Il2CppScheduleOne.Employees.Employee" "RemoveDailyWage"
    Assert-Property "Il2CppScheduleOne.Employees.EmployeeHome" "Storage" "Il2CppScheduleOne.Storage.StorageEntity"
    Assert-Property "Il2CppScheduleOne.Employees.EmployeeHome" "AssignedEmployee" "Il2CppScheduleOne.Employees.Employee"
    Assert-Method "Il2CppScheduleOne.Employees.EmployeeHome" "SetAssignedEmployee" @("Il2CppScheduleOne.Employees.Employee")
    Assert-Method "Il2CppScheduleOne.Employees.Packager" "UpdateBehaviour"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Packager" -MethodName "IsAnyWorkInProgress" -ReturnType "System.Boolean"
    Assert-Method -TypeName "Il2CppScheduleOne.Employees.Packager" -MethodName "ShouldIdle" -ReturnType "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.Packager" "ResetConfiguration"
    Assert-Method "Il2CppScheduleOne.Employees.Packager" "UnassignProperty"
    Assert-Method "Il2CppScheduleOne.Employees.Packager" "Fire"
    Assert-Property "Il2CppScheduleOne.Employees.Packager" "PackagingBehaviour" "Il2CppScheduleOne.NPCs.Behaviour.PackagingStationBehaviour"
    Assert-Property "Il2CppScheduleOne.Employees.Packager" "BrickPressBehaviour" "Il2CppScheduleOne.NPCs.Behaviour.BrickPressBehaviour"
    Assert-Property "Il2CppScheduleOne.NPCs.Behaviour.Behaviour" "Enabled" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.NPCs.Behaviour.Behaviour" "EnabledOnAwake" "System.Boolean"
    Assert-Method "Il2CppScheduleOne.NPCs.Behaviour.Behaviour" "Disable"
    Assert-Method "Il2CppScheduleOne.Management.EntityConfiguration" "Reset"
    Assert-Method "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" "Confirm"
    Assert-Method "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" "ChoiceCallback" @("System.String")
    Assert-Method -TypeName "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" -MethodName "CheckChoice" -ParameterTypes @(
        "System.String", "System.String&"
    ) -ReturnType "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" "ModifyChoiceList" @(
        "System.String", 'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Dialogue.DialogueChoiceData>&'
    )
    Assert-Method -TypeName "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" -MethodName "ModifyDialogueText" -ParameterTypes @(
        "System.String", "System.String"
    ) -ReturnType "System.String"
    Assert-Property "Il2CppScheduleOne.Dialogue.DialogueController_Fixer" "selectedEmployeeType" "Il2CppScheduleOne.Employees.EEmployeeType"
    Assert-Property "Il2CppScheduleOne.Dialogue.DialogueChoiceData" "Guid" "System.String"
    Assert-Property "Il2CppScheduleOne.Dialogue.DialogueChoiceData" "ChoiceText" "System.String"
    Assert-Property "Il2CppScheduleOne.Dialogue.DialogueChoiceData" "ChoiceLabel" "System.String"
    Assert-Property "Il2CppScheduleOne.Dialogue.DialogueChoiceData" "ShowWorldspaceDialogue" "System.Boolean"
    Assert-Method "Il2CppScheduleOne.Employees.EmployeeManager" "RpcLogic___CreateEmployee_311954683" @(
        "Il2CppScheduleOne.Property.Property",
        "Il2CppScheduleOne.Employees.EEmployeeType",
        "System.String",
        "System.String",
        "System.String",
        "System.Boolean",
        "System.Int32",
        "UnityEngine.Vector3",
        "UnityEngine.Quaternion",
        "System.String"
    )
    Assert-Property "Il2CppScheduleOne.Management.IConfigurable" "Configuration" "Il2CppScheduleOne.Management.EntityConfiguration"
    Assert-Property "Il2CppScheduleOne.Management.IConfigurable" "ConfigurableType" "Il2CppScheduleOne.Management.EConfigurableType"
    Assert-Property "Il2CppScheduleOne.Management.IConfigurable" "CanBeSelected" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Management.IConfigurable" "ParentProperty" "Il2CppScheduleOne.Property.Property"
    Assert-Method "Il2CppScheduleOne.Management.IConfigurable" "SetConfigurer" @("Il2CppFishNet.Object.NetworkObject")
    Assert-Method "Il2CppScheduleOne.Management.ManagementInterface" "Open" @(
        'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Management.IConfigurable>',
        "Il2CppScheduleOne.Tools.ManagementClipboard_Equippable"
    )
    Assert-Method -TypeName "Il2CppScheduleOne.Management.ManagementInterface" -MethodName "GetConfigPanelPrefab" -ParameterTypes @(
        "Il2CppScheduleOne.Management.EConfigurableType"
    ) -ReturnType "Il2CppScheduleOne.Management.UI.ConfigPanel"
    Assert-Method "Il2CppScheduleOne.UI.Management.PackagerConfigPanel" "BindInternal" @(
        'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Management.EntityConfiguration>'
    )
    Assert-Property "Il2CppScheduleOne.UI.Management.PackagerConfigPanel" "BedUI" "Il2CppScheduleOne.UI.Management.ObjectFieldUI"
    Assert-Property "Il2CppScheduleOne.UI.Management.PackagerConfigPanel" "StationsUI" "Il2CppScheduleOne.UI.Management.ObjectListFieldUI"
    Assert-Property "Il2CppScheduleOne.UI.Management.PackagerConfigPanel" "RoutesUI" "Il2CppScheduleOne.UI.Management.RouteListFieldUI"
    Assert-Property "Il2CppScheduleOne.Management.PackagerConfiguration" "packager" "Il2CppScheduleOne.Employees.Packager"
    Assert-Method "Il2CppScheduleOne.Delivery.DeliveryManager" "IsLoadingBayFree" @(
        "Il2CppScheduleOne.Property.Property", "System.Int32"
    )
    Assert-Method "Il2CppScheduleOne.Delivery.LoadingDock" "SetOccupant" @(
        "Il2CppScheduleOne.Vehicles.LandVehicle"
    )
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "DynamicOccupant" "Il2CppScheduleOne.Vehicles.LandVehicle"
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "StaticOccupant" "Il2CppScheduleOne.Vehicles.LandVehicle"
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "ParentProperty" "Il2CppScheduleOne.Property.Property"
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "Parking" "Il2CppScheduleOne.Map.ParkingLot"
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "IsInUse" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "GUID" "Il2CppSystem.Guid"
    Assert-Property "Il2CppScheduleOne.Delivery.LoadingDock" "Name" "System.String"
    Assert-Method "Il2CppScheduleOne.Delivery.LoadingDock" "SetStaticOccupant" @("Il2CppScheduleOne.Vehicles.LandVehicle")
    Assert-Method "Il2CppScheduleOne.Delivery.LoadingDock" "RefreshOccupant"
    Assert-Property "Il2CppScheduleOne.Delivery.DeliveryVehicle" "Vehicle" "Il2CppScheduleOne.Vehicles.LandVehicle"
    Assert-Property "Il2CppScheduleOne.Delivery.DeliveryVehicle" "ActiveDelivery" "Il2CppScheduleOne.Delivery.DeliveryInstance"
    Assert-Property "Il2CppScheduleOne.Property.Property" "LoadingDocks" 'Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray`1<Il2CppScheduleOne.Delivery.LoadingDock>'
    Assert-Property "Il2CppScheduleOne.Property.Property" "PropertyCode" "System.String"
    Assert-Property "Il2CppScheduleOne.Property.Property" "PropertyName" "System.String"
    Assert-Property "Il2CppScheduleOne.Property.Property" "IsOwned" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Property.Property" "LoadingDockCount" "System.Int32"
    Assert-Property "Il2CppScheduleOne.Property.Property" "OwnedProperties" 'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Property.Property>'
    Assert-Property "Il2CppScheduleOne.Vehicles.VehicleManager" "PlayerOwnedVehicles" 'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Vehicles.LandVehicle>'
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "IsPlayerOwned" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "VehicleName" "System.String"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "VehicleCode" "System.String"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "Storage" "Il2CppScheduleOne.Storage.StorageEntity"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "GUID" "Il2CppSystem.Guid"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "IsOccupied" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "IsVisible" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "CurrentParkingLot" "Il2CppScheduleOne.Map.ParkingLot"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "CurrentParkingSpot" "Il2CppScheduleOne.Map.ParkingSpot"
    Assert-Property "Il2CppScheduleOne.Vehicles.LandVehicle" "CurrentParkData" "Il2CppScheduleOne.Vehicles.ParkData"
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "SetTransform" @("UnityEngine.Vector3", "UnityEngine.Quaternion")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "SetVisible" @("System.Boolean")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "AlignTo" @("UnityEngine.Transform", "Il2CppScheduleOne.Vehicles.EParkingAlignment", "System.Boolean")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "SetObstaclesActive" @("System.Boolean")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "UpdatePhysicallySimulated" @("System.Boolean")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "AddNPCOccupant" @("Il2CppScheduleOne.NPCs.NPC")
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "RemoveNPCOccupant" @("Il2CppScheduleOne.NPCs.NPC")
    Assert-Method -TypeName "Il2CppScheduleOne.Vehicles.LandVehicle" -MethodName "GetVehicleData" -ReturnType "Il2CppScheduleOne.Persistence.Datas.VehicleData"
    Assert-Method "Il2CppScheduleOne.Vehicles.LandVehicle" "ExitPark" @("System.Boolean")
    Assert-Property "Il2CppScheduleOne.Map.ParkingLot" "GUID" "Il2CppSystem.Guid"
    Assert-Property "Il2CppScheduleOne.Map.ParkingLot" "ParkingSpots" 'Il2CppSystem.Collections.Generic.List`1<Il2CppScheduleOne.Map.ParkingSpot>'
    Assert-Property "Il2CppScheduleOne.Map.ParkingLot" "EntryPoint" "UnityEngine.Transform"
    Assert-Property "Il2CppScheduleOne.Map.ParkingLot" "HiddenVehicleAccessPoint" "UnityEngine.Transform"
    Assert-Method -TypeName "Il2CppScheduleOne.Map.ParkingLot" -MethodName "GetRandomFreeSpot" -ReturnType "Il2CppScheduleOne.Map.ParkingSpot"
    Assert-Property "Il2CppScheduleOne.Map.ParkingSpot" "AlignmentPoint" "UnityEngine.Transform"
    Assert-Property "Il2CppScheduleOne.Map.ParkingSpot" "Alignment" "Il2CppScheduleOne.Vehicles.EParkingAlignment"
    Assert-Property "Il2CppScheduleOne.Map.ParkingSpot" "OccupantVehicle" "Il2CppScheduleOne.Vehicles.LandVehicle"
    Assert-Method "Il2CppScheduleOne.Map.ParkingSpot" "SetOccupant" @("Il2CppScheduleOne.Vehicles.LandVehicle")
    Assert-Method "Il2CppScheduleOne.NPCs.NPC" "EnterVehicle" @(
        "Il2CppFishNet.Connection.NetworkConnection", "Il2CppScheduleOne.Vehicles.LandVehicle"
    )
    Assert-Method "Il2CppScheduleOne.NPCs.NPC" "ExitVehicle"
    Assert-Property "Il2CppScheduleOne.GameTime.TimeManager" "CurrentTime" "System.Int32"
    Assert-Method -TypeName "Il2CppScheduleOne.GameTime.TimeManager" -MethodName "GetTotalMinSum" -ReturnType "System.Int32"
    Assert-Property "Il2CppScheduleOne.Persistence.LoadManager" "IsGameLoaded" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Persistence.LoadManager" "IsLoading" "System.Boolean"
    Assert-Property "Il2CppScheduleOne.Persistence.LoadManager" "ActiveSaveInfo" "Il2CppScheduleOne.Persistence.SaveInfo"
    Assert-Property "Il2CppScheduleOne.Persistence.LoadManager" "onLoadComplete" "UnityEngine.Events.UnityEvent"
    Assert-Method "Il2CppScheduleOne.Persistence.LoadManager" "StartGame" @(
        "Il2CppScheduleOne.Persistence.SaveInfo", "System.Boolean", "System.Boolean"
    )
    Assert-Method "Il2CppScheduleOne.Persistence.LoadManager" "Update"
    Assert-Method "Il2CppScheduleOne.Persistence.LoadManager" "CleanUp"
    Assert-Property "Il2CppScheduleOne.Persistence.SaveInfo" "SaveSlotNumber" "System.Int32"
    Assert-Method "Il2CppScheduleOne.Persistence.SaveManager" "Save"
    Assert-Method "Il2CppScheduleOne.Persistence.SaveManager" "Save" @("System.String")

    Write-Output "PASS Assembly-CSharp.dll SHA256 $actualHash"
    Write-Output "Schedule I 0.4.6f13 IL2CPP Vehicle Handlers API verification passed."
}
finally {
    $assembly.Dispose()
}
