$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'SpherewrightFactoryEvidence.ps1')
$script:checks=0
function Assert-Factory([bool]$Condition,[string]$Name){if(-not $Condition){throw "Factory evidence regression: $Name"};$script:checks++}
function New-FixturePage([long]$Tick,[int]$Count=2) {
    $entities=@(foreach($id in 1..$Count){
        $entry=[ordered]@{}
        foreach($field in ($script:swFactoryStatic+$script:swFactoryDynamic)){$entry[$field]=$null}
        $entry.sessionId='fixture-session';$entry.planetId=104;$entry.capturedAtGameTick=$Tick;$entry.objectId=$id;$entry.itemId=2302;$entry.objectKind='entity';$entry.componentKind='assembler';$entry.recipeId=17
        $entry.position=[pscustomobject]@{x=$id;y=0;z=0};$entry.rotation=[pscustomobject]@{x=0;y=0;z=0;w=1}
        $entry.connections=@();$entry.resourceNodeIds=@(1,2);$entry.buffers=@([pscustomobject]@{role='output';itemId=1109;count=10;inc=0});$entry.logisticsStation=$null
        [pscustomobject]$entry
    })
    [pscustomobject]@{sessionId='fixture-session';planetId=104;capturedAtGameTick=$Tick;snapshotId="fixture-$Tick";entities=$entities;nextCursor=$null}
}
$a=New-FixturePage 100;$b=New-FixturePage 200
$a.entities[0].connections=@([pscustomobject]@{slot=1;isOutput=$true;otherObjectId=2;otherSlot=0})
$a.entities[1].connections=@([pscustomobject]@{slot=0;isOutput=$false;otherObjectId=1;otherSlot=1})
$b.entities[0].connections=$a.entities[0].connections;$b.entities[1].connections=$a.entities[1].connections
$b.entities[0].buffers[0].count=15
$before=New-SpherewrightFactoryEvidenceView -Pages @($a) -ExpectedEntityCount 2
$after=New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 2
$delta=Compare-SpherewrightFactoryEvidence $before $after
Assert-Factory (@($delta.staticChanges).Count -eq 0 -and $delta.dynamicChangedObjectIds -join ',' -ceq '1' -and @($delta.nonreciprocalEdges).Count -eq 0) 'buffer delta separated, exact reciprocal edges retained'
Assert-Factory (-not $delta.independentAcceptance -and $before.notFreshPreflight) 'derived evidence grants neither fresh preflight nor independent acceptance'
$b.entities[0].resourceNodeIds=@(2)
$after=New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 2
$delta=Compare-SpherewrightFactoryEvidence $before $after
Assert-Factory ($delta.staticChanges.Count -eq 1 -and -not $delta.staticChanges[0].allowed) 'resource exhaustion never blanket-ignored'
$rule=[pscustomobject]@{objectId=1;field='resourceNodeIds';before=@(1,2);after=@(2);reason='explicit exhausted resource evidence';evidenceRef='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/0001'}
$delta=Compare-SpherewrightFactoryEvidence $before $after -AllowedStaticChanges @($rule)
Assert-Factory ($delta.staticChanges.Count -eq 1 -and $delta.staticChanges[0].allowed) 'exact rule leaves the actual change and evidence visible'
$rule.after=@(3)
try { Compare-SpherewrightFactoryEvidence $before $after -AllowedStaticChanges @($rule) | Out-Null;throw 'not rejected' } catch { Assert-Factory ($_.Exception.Message -like '*exact before/after*') 'mismatched allowance rejected' }
foreach($fault in @('count','cursor','mixed','duplicate','unknown','missing','null')) {
    $page=New-FixturePage 100
    $pages=@($page);$count=2
    switch($fault){count{$count=3}cursor{$page.nextCursor='unfinished'}mixed{$other=New-FixturePage 200;$page.nextCursor='first';$pages=@($page,$other)}duplicate{$page.entities[1].objectId=1}unknown{$page.entities[0]|Add-Member -NotePropertyName futureConfig -NotePropertyValue 1}missing{$page.entities[0].PSObject.Properties.Remove('buffers')}null{$page.entities[0].buffers=$null}}
    $rejected=$false;try{New-SpherewrightFactoryEvidenceView -Pages $pages -ExpectedEntityCount $count|Out-Null}catch{$rejected=$true}
    Assert-Factory $rejected "$fault rejects before declaring coverage"
}
$b.entities[1].connections=@()
$after=New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 2
$delta=Compare-SpherewrightFactoryEvidence $before $after
Assert-Factory ($delta.nonreciprocalEdges.Count -eq 1) 'missing reverse edge reported'

function New-FixtureStation {
    $station=[ordered]@{}
    foreach($field in ($script:swStationStatic+$script:swStationDynamic)){$station[$field]=0}
    $station.neededItemIds=@();$station.energy=10;$station.storageSlots=@();$station.beltSlots=@([pscustomobject]@{index=0;direction='output';beltEntityId=1;beltComponentId=1;storageIndex=0;counter=0})
    [pscustomobject]$station
}
$a=New-FixturePage 100;$b=New-FixturePage 200
$a.entities[0].logisticsStation=New-FixtureStation;$b.entities[0].logisticsStation=New-FixtureStation
$b.entities[0].logisticsStation.energy=20
$before=New-SpherewrightFactoryEvidenceView -Pages @($a) -ExpectedEntityCount 2;$after=New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 2
$delta=Compare-SpherewrightFactoryEvidence $before $after
Assert-Factory ($delta.staticChanges.Count -eq 0 -and $delta.dynamicChangedObjectIds.Count -eq 1) 'station energy is dynamic, not a configuration false blocker'
$b.entities[0].logisticsStation.beltSlots[0].storageIndex=1
$after=New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 2
$delta=Compare-SpherewrightFactoryEvidence $before $after
Assert-Factory ($delta.staticChanges.Count -eq 1 -and $delta.staticChanges[0].field -ceq 'logisticsStation') 'native lowercase output selector retained in static evidence'
$b.entities[0].logisticsStation|Add-Member -NotePropertyName futureSetting -NotePropertyValue 1
$rejected=$false;try{New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 2|Out-Null}catch{$rejected=$true}
Assert-Factory $rejected 'unknown station field cannot be silently discarded'

# Same immutable inputs and three identical offline questions; only parse/project
# reuse changes. No transport or game-time speedup is inferred from this fixture.
$a=New-FixturePage 100 64;$b=New-FixturePage 200 64
$null=Compare-SpherewrightFactoryEvidence (New-SpherewrightFactoryEvidenceView -Pages @($a) -ExpectedEntityCount 64) (New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 64)
$watch=[Diagnostics.Stopwatch]::StartNew()
for($i=0;$i -lt 3;$i++){ $baseline=Compare-SpherewrightFactoryEvidence (New-SpherewrightFactoryEvidenceView -Pages @($a) -ExpectedEntityCount 64) (New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 64) }
$beforeMs=$watch.Elapsed.TotalMilliseconds
$watch.Restart();$left=New-SpherewrightFactoryEvidenceView -Pages @($a) -ExpectedEntityCount 64;$right=New-SpherewrightFactoryEvidenceView -Pages @($b) -ExpectedEntityCount 64
for($i=0;$i -lt 3;$i++){ $cached=Compare-SpherewrightFactoryEvidence $left $right }
$afterMs=$watch.Elapsed.TotalMilliseconds
Assert-Factory ((ConvertTo-SpherewrightEvidenceJson $baseline) -ceq (ConvertTo-SpherewrightEvidenceJson $cached)) 'cached and repeated projections give identical evidence'
[pscustomobject]@{passed=$script:checks;gameCalls=0;fixtureEntities=64;questions=3;projectionsBefore=6;projectionsAfter=2;beforeMs=[math]::Round($beforeMs,3);afterMs=[math]::Round($afterMs,3)}|ConvertTo-Json -Compress
