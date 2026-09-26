$swingCases=[Collections.Generic.List[object]]::new()
. "$PSScriptRoot/CombatSetup.ps1"
function SwingSave($name,$value){$value|ConvertTo-Json -Depth 16|Set-Content "$root/$name.json"}
function SwingSetup($mapIndex,$peer){
    Map $mapIndex|Out-Null
    foreach($view in @('host','client')){Send $view move}
    $code=@'
var g=SSW.NetGame.Current;
var pins=g.Arena.Map.GetComponentsInChildren<SSW.Pin>(true);
var pin=System.Linq.Enumerable.OrderBy(pins,p=>UnityEngine.Mathf.Abs(p.transform.position.x)).First();
var swing=(SSW.Swing)new UnityEditor.SerializedObject(pin).FindProperty("_target").objectReferenceValue;
var body=(UnityEngine.Rigidbody2D)new UnityEditor.SerializedObject(swing).FindProperty("_body").objectReferenceValue;
var shape=body.GetComponent<UnityEngine.Collider2D>();
var player=System.Linq.Enumerable.First(g.Players,p=>p.IsOwner==@OWNER@);
foreach(var p in g.Players){var field=typeof(SSW.MotionView).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var state=(SSW.MotionState)field.GetValue(p.Drive);state.FreezeTime=0f;field.SetValue(p.Drive,state);p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));if(p!=player)p.Drive.Freeze(60f);}
float foot=player.Body.position.y-player.Collider.bounds.min.y;
player.Drive.Teleport(new UnityEngine.Vector2(shape.bounds.center.x+0.2f,shape.bounds.max.y+foot+0.8f));
return new{map=g.Arena.Map.Title,index=System.Array.IndexOf(pins,pin),count=pins.Length,id=player.OwnerClientId,x=body.position.x,y=body.position.y,angle=body.rotation};
'@
    $setup=Eval $code.Replace('@OWNER@',$(if($peer -eq 'host'){'true'}else{'false'}))
    Await 'actor lands on hanging block' {($h.players|Where-Object id -eq $setup.id).grounded -and ($c.players|Where-Object id -eq $setup.id).grounded} 5
    $contact=Eval ('var g=SSW.NetGame.Current;var p=System.Linq.Enumerable.First(g.Players,x=>x.OwnerClientId=='+[string]$setup.id+'ul);var pin=g.Arena.Map.GetComponentsInChildren<SSW.Pin>(true)['+$setup.index+'];var swing=(SSW.Swing)new UnityEditor.SerializedObject(pin).FindProperty("_target").objectReferenceValue;var body=(UnityEngine.Rigidbody2D)new UnityEditor.SerializedObject(swing).FindProperty("_body").objectReferenceValue;return p.Collider.Distance(body.GetComponent<UnityEngine.Collider2D>()).distance;')
    Check ([Math]::Abs($contact) -lt 0.12) "$($setup.map) $peer lands on the selected hanging block"
    return $setup
}
if(-not $SwingCutOnly){
    foreach($mapIndex in $SwingMaps){foreach($peer in @('host','client')){
        $setup=SwingSetup $mapIndex $peer
        $before=@{host=(Read host);client=(Read client)}
        Send $peer move 0 1 0
        Start-Sleep -Milliseconds 60
        Send $peer move
        $samples=[Collections.Generic.List[object]]::new()
        $until=[DateTime]::UtcNow.AddSeconds(0.75)
        do{$samples.Add(@{host=(Read host);client=(Read client)});Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $until)
        $h=Read host;$c=Read client
        SwingSave "SwingRide-$mapIndex-$peer" @{setup=$setup;before=$before;samples=$samples.ToArray()}
        foreach($view in @('host','client')){
            $moved=@($samples|Where-Object {$b=$_.$view.mapState.hangs[$setup.index];[Math]::Abs($b.position.x-$setup.x) -gt 0.001 -or [Math]::Abs($b.angle-$setup.angle) -gt 0.05}).Count
            Check ($moved -gt 0) "$($setup.map) $peer landing and walking push the hanging block on $view"
        }
        Await 'selected hanging block motion reaches client' {$a=$h.mapState.hangs[$setup.index];$b=$c.mapState.hangs[$setup.index];[Math]::Abs($a.position.x-$b.position.x) -lt 0.35 -and [Math]::Abs($a.position.y-$b.position.y) -lt 0.35} 4
        Check $true "$($setup.map) $peer hanging block motion agrees across peers within 0.35 units"
        $swingCases.Add(@{kind='ride';setup=$setup;before=$before;host=$h;client=$c})
        SwingSave 'SwingCases' $swingCases.ToArray()
    }}
}
$cutMaps=if($SwingCutOnly){$SwingMaps}else{@(11)}
foreach($mapIndex in $cutMaps){foreach($peer in @('host','client')){
    Map $mapIndex|Out-Null
    CombatSpawn Gunner
    $setup=CombatAim $peer
    $aim=@{x=$setup.ax;y=$setup.ay}
    $before=@{host=(Read host);client=(Read client)}
    $fired=($before.host.players|Where-Object id -eq $setup.id).fired
    Send $peer press 0 $aim.x $aim.y
    Send $peer release 0 $aim.x $aim.y
    Await 'native attack is accepted' {($h.players|Where-Object id -eq $setup.id).fired -gt $fired} 3
    $samples=[Collections.Generic.List[object]]::new()
    $until=[DateTime]::UtcNow.AddSeconds(2)
    do{$h=Read host;$c=Read client;$samples.Add(@{host=$h;client=$c});Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $until)
    SwingSave "SwingShot-$mapIndex-$peer" @{setup=$setup;aim=$aim;before=$before;samples=$samples.ToArray()}
    foreach($view in @('host','client')){
        $block=$(if($view -eq 'host'){$h}else{$c}).mapState.hangs[$setup.index]
        Check ($block.health -eq 0 -and -not $block.rope -and -not $block.hit -and -not $block.face) "$($setup.map) $peer native projectile cuts pin and hides rope on $view"
        Check ($block.position.y -lt $setup.y-0.2) "$($setup.map) $peer cut block falls on $view"
    }
    $oldMap=$h.mapObject;$oldPlayers=@($h.players.objectId)
    $code=@'
var g=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var rotation=g.GetComponent<SSW.MapRotation>();var bag=(SSW.MapBag)typeof(SSW.MapRotation).GetField("_bag",flags).GetValue(rotation);var order=(int[])typeof(SSW.MapBag).GetField("_order",flags).GetValue(bag);typeof(SSW.MapBag).GetField("_next",flags).SetValue(bag,0);order[0]=@INDEX@;var loser=System.Linq.Enumerable.First(g.Players,p=>p.OwnerClientId==@LOSER@ul);loser.Health.ReceiveDamage(new SSW.DamageRequest(null,loser.Health.Max*10f,SSW.DamageTag.Environment|SSW.DamageTag.IgnoreDefense));return true;
'@
    Eval $code.Replace('@INDEX@',[string]$mapIndex).Replace('@LOSER@',[string]$setup.id)|Out-Null
    Await 'real death replaces the map and players' {$h.mapObject -ne $oldMap -and $h.mapObject -eq $c.mapObject -and $h.players.Count -eq 2 -and $c.players.Count -eq 2 -and $h.phase -in @('Draft','Countdown','Playing')} 18
    Eval 'var g=SSW.NetGame.Current;if(g.State.Phase==SSW.MatchPhase.Draft){foreach(var p in g.Players)p.Draft.Restore(System.Linq.Enumerable.ToArray(p.Draft.Owned));g.Match.Picked();}return true;'|Out-Null
    Await 'next round is playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 18
    foreach($view in @('host','client')){
        $snapshot=if($view -eq 'host'){$h}else{$c}
        Check ($snapshot.mapState.hangs.Count -eq $setup.count -and @($snapshot.mapState.hangs|Where-Object {$_.health -ne 1 -or -not $_.rope -or -not $_.hit -or -not $_.face -or -not $_.simulated}).Count -eq 0) "$($setup.map) real round reset restores all pins ropes and blocks on $view"
        Check ($snapshot.mapState.ids.Count -eq 1 -and $oldMap -notin $snapshot.mapState.ids -and @($snapshot.players|Where-Object {$_.objectId -in $oldPlayers}).Count -eq 0) "$($setup.map) real round reset removes prior map and players on $view"
    }
    $swingCases.Add(@{kind='native-cut-and-death';control='Actor gravity is zero only for attack fixture, with native weapons and map physics. Next MapBag index is fixed to the same map only to inspect reset; death and ResetRound use normal game flow.';setup=$setup;before=$before;oldMap=$oldMap;oldPlayers=$oldPlayers;host=$h;client=$c})
    SwingSave 'SwingCases' $swingCases.ToArray()
}}
SwingSave 'SwingResult' @{checks=$checks.Count;scope='Native landing/walking, attack cutting, falling and actual round reset';host=$h;client=$c}
Write-Output "PASS $($checks.Count) hanging block checks: $root"
