# Tests the actual IUserMod entry point with synthetic dependencies.
# Signature: OpenAI-Codex_AI_Improve
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Release', 'Development')]
    [string] $BuildKind
)

$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'AIImproveMod.cs'
$source = [IO.File]::ReadAllText($sourcePath)
$stubs = @'
namespace ColossalFramework.UI { public class Placeholder {} }
namespace ColossalFramework {
    public class Singleton<T> where T : new() { public static T instance = new T(); }
}
namespace ICities {
    public class UIHelperBase { }
    public interface IUserMod { string Name { get; } string Description { get; } void OnSettingsUI(UIHelperBase helper); }
}
namespace UnityEngine { public static class Debug { public static void Log(object value) { } } }
public class SimulationManager { public enum UpdateMode { } }
public class LoadingManager {
    public System.Action<SimulationManager.UpdateMode> m_levelLoaded;
    public System.Action m_levelUnloaded;
}
namespace CitiesHarmony.API {
    public static class HarmonyHelper {
        public static bool IsHarmonyInstalled { get { return false; } }
        public static void DoOnHarmonyReady(System.Action action) { }
    }
}
namespace AIImprove {
    public class SavedBoolStub { public bool value; }
    public static class ModSettings {
        public static SavedBoolStub TmpeEmergencyCostAdjustmentEnabled = new SavedBoolStub();
        public static void ApplySchemaMigrations() { }
    }
    public static class TmpeEmergencyCongestionPatch { public static void SetAdjustmentEnabled(bool value) { } }
    public static class Patcher {
        public static void PatchAll() { }
        public static void UnpatchAll() { }
        public static void ReportSharedMethods() { }
    }
    public static class IngameUI {
        public static void OnLevelLoaded(SimulationManager.UpdateMode mode) { }
        public static void OnLevelUnloading() { }
    }
    public static class CitizenCarProbabilityPatch {
        public static void ResetForNewLevel(SimulationManager.UpdateMode mode) { }
        public static void ReportAndResetForLevel() { }
    }
    public static class IntercityServiceInventory { public static void OnLevelLoaded(SimulationManager.UpdateMode mode) { } }
    public static class TrackerReset { public static void ResetAll() { } }
    public static class SettingsPageUI { public static void Build(ICities.UIHelperBase helper) { } }
}
'@

$compilerOptions = if ($BuildKind -eq 'Development') { @('/define:AIIMPROVE_DEVTOOLS') } else { @() }
$addTypeArgs = @{ TypeDefinition = ($source + "`n" + $stubs); PassThru = $true }
if ($compilerOptions.Count -gt 0) { $addTypeArgs.CompilerOptions = $compilerOptions }
$types = Add-Type @addTypeArgs
$entryPoint = $types | Where-Object FullName -EQ 'AIImprove.AIImproveMod'
$mod = [Activator]::CreateInstance($entryPoint)

if ($BuildKind -eq 'Development') {
    $expectedName = 'AI_Improve'
    $expectedDescription = 'Development Version. This version has DevTool. Be careful; the development version can let your save be broken.'
    $expectedDisplay = 'AI_Improve - Development Version. This version has DevTool. Be careful; the development version can let your save be broken.'
} else {
    $expectedName = 'AI_Improve'
    $expectedDescription = 'Improves traffic, citizen and service vehicle AI decision quality. By Kingsley.'
    $expectedDisplay = 'AI_Improve - Improves traffic, citizen and service vehicle AI decision quality. By Kingsley.'
}
$actualDisplay = $mod.Name + ' - ' + $mod.Description
$failures = @()
if ($mod.Name -cne $expectedName) { $failures += 'name mismatch' }
if ($mod.Description -cne $expectedDescription) { $failures += 'description mismatch' }
if ($actualDisplay -cne $expectedDisplay) { $failures += 'combined display mismatch' }
if ($failures.Count -gt 0) { throw "${BuildKind}: $($failures -join ', ')" }
Write-Output "${BuildKind}: name, description and combined display checks passed."
