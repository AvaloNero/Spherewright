[CmdletBinding()]
param(
    [string]$DspDir,
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-DspRoot {
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    return (Test-Path -LiteralPath (Join-Path $fullPath 'DSPGAME.exe')) `
        -and (Test-Path -LiteralPath (Join-Path $fullPath 'DSPGAME_Data\Managed\Assembly-CSharp.dll'))
}

function New-Result {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Source
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    return [pscustomobject]@{
        path = $fullPath
        source = $Source
        executable = Join-Path $fullPath 'DSPGAME.exe'
        assemblyCSharp = Join-Path $fullPath 'DSPGAME_Data\Managed\Assembly-CSharp.dll'
        bepInEx = Join-Path $fullPath 'BepInEx\core\BepInEx.dll'
    }
}

if ($DspDir) {
    if (-not (Test-DspRoot -Path $DspDir)) {
        throw "The explicit DSP directory is invalid: $DspDir"
    }

    $result = New-Result -Path $DspDir -Source 'explicit'
} elseif ($env:SPHEREWRIGHT_DSP_DIR) {
    if (-not (Test-DspRoot -Path $env:SPHEREWRIGHT_DSP_DIR)) {
        throw 'SPHEREWRIGHT_DSP_DIR is set but does not point to a valid DSP installation.'
    }

    $result = New-Result -Path $env:SPHEREWRIGHT_DSP_DIR -Source 'environment'
} else {
    $steamRoots = [System.Collections.Generic.List[string]]::new()
    $registryEntries = @(
        @('HKCU:\Software\Valve\Steam', 'SteamPath'),
        @('HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'InstallPath'),
        @('HKLM:\SOFTWARE\Valve\Steam', 'InstallPath')
    )

    foreach ($entry in $registryEntries) {
        try {
            $value = (Get-ItemProperty -LiteralPath $entry[0] -ErrorAction Stop).($entry[1])
            if ($value) {
                $steamRoots.Add([string]$value)
            }
        } catch {
            continue
        }
    }

    $libraryRoots = [System.Collections.Generic.List[string]]::new()
    foreach ($steamRoot in @($steamRoots | Select-Object -Unique)) {
        $libraryRoots.Add($steamRoot)
        $vdfPath = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (-not (Test-Path -LiteralPath $vdfPath)) {
            continue
        }

        $vdf = Get-Content -LiteralPath $vdfPath -Raw
        foreach ($match in [regex]::Matches($vdf, '"path"\s+"([^"]+)"')) {
            $libraryRoots.Add(($match.Groups[1].Value -replace '\\\\', '\'))
        }
    }

    $matches = @(
        foreach ($libraryRoot in @($libraryRoots | Select-Object -Unique)) {
            $candidate = Join-Path $libraryRoot 'steamapps\common\Dyson Sphere Program'
            if (Test-DspRoot -Path $candidate) {
                New-Result -Path $candidate -Source 'steam-auto-detected'
            }
        }
    )

    if ($matches.Count -eq 0) {
        throw 'No valid Dyson Sphere Program installation was found.'
    }

    if ($matches.Count -gt 1) {
        $paths = ($matches | ForEach-Object path) -join '; '
        throw "Multiple DSP installations were found. Pass -DspDir explicitly. Candidates: $paths"
    }

    $result = $matches[0]
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 3 -Compress
} else {
    $result
}
