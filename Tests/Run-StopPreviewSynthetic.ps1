# Signature: OpenAI-Codex_Automated_Public_Transport_Planning
# In-memory pure-data checks, not a module/game build or workflow runner.
$ErrorActionPreference = 'Stop'
$checkout = Split-Path -Parent $PSScriptRoot
$engine = Get-Content -LiteralPath (Join-Path $checkout 'Source/Planning/PlanningEngine.cs') -Raw
$marker = '    /// <summary>A single immutable route proposal'
$end = $engine.IndexOf($marker, [StringComparison]::Ordinal)
if ($end -lt 0) { throw 'Cannot isolate actual production stop-site rules.' }
$rules = $engine.Substring(0, $end) + "`n}"
# Only the vector arithmetic boundary is substituted; validation is production source.
$vectorBoundary = @'
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
        public static Vector3 operator -(Vector3 a, Vector3 b)
        { return new Vector3(a.x-b.x, a.y-b.y, a.z-b.z); }
        public float sqrMagnitude { get { return x*x + y*y + z*z; } }
    }
}
'@
$sources = @($vectorBoundary, $rules,
    (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'RouteStopPreviewChecks.cs') -Raw))
$preview = Join-Path $checkout 'Source/Planning/RouteStopPreview.cs'
if (Test-Path -LiteralPath $preview) { $sources += Get-Content -LiteralPath $preview -Raw }
$imports = "using System;`nusing System.Collections.Generic;`nusing System.Collections.ObjectModel;`nusing System.Globalization;`nusing System.Threading;`nusing UnityEngine;`nusing AutomatedPublicTransportPlanning.Planning;`n"
$combined = $imports + (($sources | ForEach-Object { [regex]::Replace($_, '(?m)^using [^\r\n]+;\r?$', '') }) -join "`n")
Add-Type -TypeDefinition $combined -ErrorAction Stop
[RouteStopPreviewChecks]::Run()
Write-Output "PASS actual stop-site rules/preview: $([RouteStopPreviewChecks]::Assertions) assertions (synthetic vector boundary)."
