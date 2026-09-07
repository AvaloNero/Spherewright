# Offline regression; no descriptor discovery or game request is made.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')
$script:checks = 0
function Assert-Count($State, [int]$Id, [long]$Expected) {
    $actual = Get-SpherewrightInventoryCount -PlayerState $State -ItemId $Id
    if ($actual -ne $Expected) { throw "Inventory count expected $Expected, got $actual." }
    $script:checks++
}
function Assert-Rejected([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw 'Malformed or unknown inventory was silently accepted.' }
    $script:checks++
}
Assert-Count ([pscustomobject]@{inventory=@()}) 1104 0
$before = '{"inventory":[{"itemId":2012,"count":2}]}' | ConvertFrom-Json
$after = '{"inventory":[{"itemId":1104,"count":1},{"itemId":2012,"count":3}]}' | ConvertFrom-Json
Assert-Count $before 2012 2
Assert-Count $before 1104 0
Assert-Count $after 2012 3
Assert-Count $after 1104 1
Assert-Count ('{"inventory":[{"itemId":1104,"count":2},{"itemId":1104,"count":3}]}'|ConvertFrom-Json) 1104 5
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState ([pscustomobject]@{}) -ItemId 1104 }
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState ([pscustomobject]@{inventory=$null}) -ItemId 1104 }
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState ([pscustomobject]@{inventory=@($null)}) -ItemId 1104 }
foreach ($json in @(
    '{"inventory":[{"itemId":1104}]}',
    '{"inventory":[{"itemId":1104,"count":null}]}',
    '{"inventory":[{"itemId":1104,"count":-1}]}',
    '{"inventory":[{"itemId":1104,"count":1.5}]}',
    '{"inventory":[{"itemId":1104,"count":"1"}]}',
    '{"inventory":[{"itemId":1104,"count":true}]}',
    '{"inventory":[{"count":1}]}',
    '{"inventory":[{"itemId":0,"count":1}]}',
    '{"inventory":[{"itemId":"1104","count":1}]}',
    '{"inventory":[{"itemId":1104,"count":2147483647},{"itemId":1104,"count":1}]}',
    '{"inventory":[{"itemId":9999,"count":null}]}'
)) {
    $state = $json | ConvertFrom-Json
    Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState $state -ItemId 1104 }
}
Assert-Rejected { Get-SpherewrightInventoryCount -PlayerState $before -ItemId 0 }
[pscustomobject]@{passed=$script:checks;gameCalls=0} | ConvertTo-Json -Compress
