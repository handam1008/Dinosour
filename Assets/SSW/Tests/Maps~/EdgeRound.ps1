Send host lag 60 10 0
Send client lag 60 10 0
Map 11|Out-Null
$before=@{host=(Read host);client=(Read client)}
$oldMap=$before.host.mapObject
$oldPlayers=@($before.host.players.objectId)
Eval 'var g=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var r=g.GetComponent<SSW.MapRotation>();var bag=(SSW.MapBag)typeof(SSW.MapRotation).GetField("_bag",flags).GetValue(r);var order=(int[])typeof(SSW.MapBag).GetField("_order",flags).GetValue(bag);typeof(SSW.MapBag).GetField("_next",flags).SetValue(bag,0);order[0]=12;var p=g.Players[0];p.Health.ReceiveDamage(new SSW.DamageRequest(null,p.Health.Max*10f,SSW.DamageTag.Environment|SSW.DamageTag.IgnoreDefense));return true;'|Out-Null
Await 'death advances to another map and new actors' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and $h.mapObject -ne $oldMap -and $h.mapObject -eq $c.mapObject -and @($h.players|Where-Object objectId -in $oldPlayers).Count -eq 0} 20
@{before=$before;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Round.json"
Check ($h.map -ne $before.host.map) 'normal death starts another map before boundary checks'
Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000f);}return true;'|Out-Null
$cases=[Collections.Generic.List[object]]::new()
$directions=@(@{x=1;y=0},@{x=-1;y=0},@{x=0;y=1},@{x=0;y=-1})
for($index=$EdgeFrom;$index -lt $directions.Count;$index++){
    $normal=$directions[$index]
    Send host move
    Send client move
    if($normal.x -ne 0){foreach($peer in @('host','client')){$actor=(Read $peer).players|Where-Object owner;Send $peer move 0 (-$normal.x*$actor.side) 0}}
    foreach($peer in @('host','client')){Send $peer trace $index 5 0}
    $code=[IO.File]::ReadAllText("$PSScriptRoot/EdgeApproach.cs").Replace('@X@',[string]$normal.x).Replace('@Y@',[string]$normal.y).Replace('@PATH@',"$root/Contact$index.json")
    $setup=Eval $code
    Await 'approach trace completed' {(Test-Path "$root/Contact$index.json") -and (Test-Path "$root/host.trace.$index.json") -and (Test-Path "$root/client.trace.$index.json")} 8
    $contact=Get-Content "$root/Contact$index.json" -Raw|ConvertFrom-Json
    foreach($actor in $setup.actors){
        $samples=@($contact.samples|Where-Object id -eq $actor.id)
        $hit=@($samples|Where-Object hp -le ($actor.hp-$setup.damage))
        $near=@($samples|Where-Object gap -le 0.04)
        Check ($hit.Count -gt 0 -and $near.Count -gt 0) "$($setup.edge) actor $($actor.id) receives native contact damage after round change"
        if($normal.x -ne 0){
            $at=$hit[0].time
            $prior=@($samples|Where-Object {$_.time -lt $at -and $_.gap -gt 0.035})
            Check ($prior.Count -gt 0 -and -not $prior[-1].grounded -and -not $hit[0].grounded) "$($setup.edge) actor $($actor.id) approaches and hits while airborne"
        }
        $peer=if($actor.owner){'host'}else{'client'}
        $trace=Get-Content "$root/$peer.trace.$index.json" -Raw|ConvertFrom-Json
        $rebound=@($trace.frames|Where-Object {$_.epoch -eq $actor.epoch -and $_.hp -le $actor.hp-$setup.damage -and ($_.velocity.x*$normal.x+$_.velocity.y*$normal.y) -gt 1})
        Check ($rebound.Count -gt 0) "$($setup.edge) $peer observes inward rebound after damage"
        foreach($view in @('host','client')){
            $snapshot=Read $view
            $player=$snapshot.players|Where-Object id -eq $actor.id
            Check ($player.hp -le $actor.hp-$setup.damage) "$($setup.edge) actor $($actor.id) damage replicated to $view"
        }
    }
    $cases.Add(@{setup=$setup;contact=$contact;host=(Read host);client=(Read client)})
    $cases|ConvertTo-Json -Depth 16|Set-Content "$root/EdgeRound.json"
}
Send host move
Send client move
@{commit=$head;checks=$checks.Count;scope='Native airborne horizontal approach, vertical launch and fall, after real death and map replacement';fixture='Next map fixed to Map17; players start clear of native boundaries. Vertical velocity supplied through Drive.Launch. Map geometry and gravity unchanged.'}|ConvertTo-Json|Set-Content "$root/Result.json"
Write-Output "PASS $($checks.Count) residual boundary checks: $root"
