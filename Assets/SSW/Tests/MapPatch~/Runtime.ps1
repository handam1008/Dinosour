$ErrorActionPreference='Stop'
foreach($command in @('Read','Await','Send','Check','Eval','Map')){
    if(-not(Get-Command $command -CommandType Function -ErrorAction SilentlyContinue)){throw 'Run inside the Hazards host/client fixture.'}
}
$catalog=(Get-Content "$Project/Logs/MapPatch/Check.json" -Raw|ConvertFrom-Json).maps
$cases=[Collections.Generic.List[object]]::new()
foreach($peer in @('host','client')){Send $peer lag 60 10 0;Send $peer move}
foreach($entry in $catalog){
    Map $entry.index|Out-Null
    Eval 'var g=SSW.NetGame.Current;foreach(var p in g.Players){p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));p.Drive.Freeze(40f);}return true;'|Out-Null
    Await 'patched map present on both peers' {$h.mapObject -eq $c.mapObject -and $h.mapState.ids.Count -eq 1 -and $c.mapState.ids.Count -eq 1} 8
    Check $true "Map$($entry.number) source asset spawns on both peers"
    if($entry.number -in @(2,15)){
        $code=[IO.File]::ReadAllText("$PSScriptRoot/Water.cs").Replace('@ROOT@',$root).Replace('@NUMBER@',[string]$entry.number)
        $water=Eval $code
        $snapshots=[Collections.Generic.List[object]]::new()
        $until=[DateTime]::UtcNow.AddSeconds(25)
        do{
            $h=Read host;$c=Read client
            if($h.error -or $c.error){throw "Runtime error: $($h.error) $($c.error)"}
            $snapshots.Add(@{host=$h;client=$c})
            if(Test-Path $water.path){break}
            Start-Sleep -Milliseconds 50
        }while([DateTime]::UtcNow -lt $until)
        Check (Test-Path $water.path) "Map$($entry.number) waterfall sample completed"
        $sample=Get-Content $water.path -Raw|ConvertFrom-Json
        Check (-not $sample.error) "Map$($entry.number) waterfall runs without exception"
        if($entry.number -eq 2){
            Check (@($sample.samples|Where-Object {$_.age -lt 9.9 -and ($_.active -or $_.effects -ne 0)}).Count -eq 0) 'Map2 waits ten seconds before force and effects'
            Check (@($sample.samples|Where-Object {$_.age -gt 10.1 -and $_.age -lt 14.9 -and (-not $_.active -or $_.effects -ne 2)}).Count -eq 0) 'Map2 force and both effects share the five second phase'
            Check (@($sample.samples|Where-Object {$_.age -gt 15.1 -and ($_.active -or $_.effects -ne 0)}).Count -eq 0) 'Map2 ends force and emission together'
        }
        foreach($key in $sample.bases.PSObject.Properties.Name){
            Check ($sample.peaks.$key -gt $sample.bases.$key+1) "Map$($entry.number) actor $key rises through native movement"
            foreach($peer in @('host','client')){
                $up=@($snapshots|Where-Object {($_.$peer.players|Where-Object id -eq $key).position.y -gt $sample.bases.$key+1})
                Check ($up.Count -gt 0) "Map$($entry.number) actor $key rise is observed on $peer"
            }
        }
        $cases.Add(@{number=$entry.number;water=$sample;peers=$snapshots.ToArray()})
    }
    if($entry.number -in @(11,16)){
        $expected=if($entry.number -eq 11){3}else{11}
        Check ($h.mapState.hangs.Count -eq $expected -and $c.mapState.hangs.Count -eq $expected) "Map$($entry.number) all $expected hanging blocks synchronize"
        $cut=Eval 'var g=SSW.NetGame.Current;var p=g.Arena.Map.GetComponentInChildren<SSW.Pin>();var s=(SSW.Swing)new UnityEditor.SerializedObject(p).FindProperty("_target").objectReferenceValue;float y=s.transform.position.y;p.TakeDamage(1f);return new{y};'
        Await 'cut pin and falling platform are replicated' {$h.mapState.hangs[0].health -eq 0 -and $c.mapState.hangs[0].health -eq 0 -and $h.mapState.hangs[0].position.y -lt $cut.y-0.1 -and $c.mapState.hangs[0].position.y -lt $cut.y-0.1} 5
        Check (-not $h.mapState.hangs[0].rope -and -not $c.mapState.hangs[0].rope) "Map$($entry.number) cut block falls and rope hides on both peers"
        $cases.Add(@{number=$entry.number;host=$h;client=$c;cut=$cut})
    }
    $code=@'
var g=SSW.NetGame.Current;
var edge=new UnityEditor.SerializedObject(g.Arena.Map.GetComponent<SSW.MapEdges>()).FindProperty("_edges").GetArrayElementAtIndex(0);
var shape=(UnityEngine.Collider2D)edge.FindPropertyRelative("Shape").objectReferenceValue;
var settings=typeof(SSW.MotionView).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
var values=new System.Collections.Generic.List<object>();
foreach(var player in g.Players){float before=player.Health.Current;player.Drive.Teleport(shape.bounds.center);values.Add(new{id=player.OwnerClientId,before});}
return new{damage=edge.FindPropertyRelative("Damage").floatValue,players=values};
'@
    $contact=Eval $code
    Await 'boundary damage reaches both peers' {$ok=$true;foreach($p in $contact.players){foreach($view in @($h,$c)){if(($view.players|Where-Object id -eq $p.id).hp -gt $p.before-$contact.damage+0.01){$ok=$false}}};$ok} 4
    Check $true "Map$($entry.number) source boundary damage reaches both actors and peers"
    $cases.ToArray()|ConvertTo-Json -Depth 22|Set-Content "$root/MapPatch.json"
}
$next=@($catalog|Where-Object number -eq 11)[0].index
$oldMap=$h.mapObject
$oldPlayers=@($h.players.objectId)
$code=@'
var g=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var rotation=g.GetComponent<SSW.MapRotation>();var bag=(SSW.MapBag)typeof(SSW.MapRotation).GetField("_bag",flags).GetValue(rotation);var order=(int[])typeof(SSW.MapBag).GetField("_order",flags).GetValue(bag);typeof(SSW.MapBag).GetField("_next",flags).SetValue(bag,0);order[0]=@NEXT@;var p=g.Players[0];p.Health.ReceiveDamage(new SSW.DamageRequest(null,p.Health.Max*10f,SSW.DamageTag.Environment|SSW.DamageTag.IgnoreDefense));return true;
'@
Eval $code.Replace('@NEXT@',[string]$next)|Out-Null
Await 'death replaces the map and actors' {$h.mapObject -ne $oldMap -and $h.mapObject -eq $c.mapObject -and $h.players.Count -eq 2 -and $c.players.Count -eq 2} 20
foreach($peer in @('host','client')){
    $snapshot=Read $peer
    Check ($snapshot.mapState.hangs.Count -eq 3 -and @($snapshot.mapState.hangs|Where-Object {$_.health -ne 1 -or -not $_.rope -or -not $_.hit -or -not $_.face}).Count -eq 0) "round change restores new map pins and ropes on $peer"
    Check ($snapshot.mapState.ids.Count -eq 1 -and $oldMap -notin $snapshot.mapState.ids -and @($snapshot.players|Where-Object {$_.objectId -in $oldPlayers}).Count -eq 0) "round change removes old map and actors on $peer"
}
@{checks=$checks.Count;scope='Requested four maps, host/client local transport with 60 ms latency and 10 ms jitter. Boundaries tested by authoritative teleport; pins cut directly through existing damage path; next map fixed to Map11 for actual death transition.';host=$h;client=$c}|ConvertTo-Json -Depth 18|Set-Content "$root/MapPatchResult.json"
Write-Output "PASS $($checks.Count) requested map checks: $root"
