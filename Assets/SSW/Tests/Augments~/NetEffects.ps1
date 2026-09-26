. "$PSScriptRoot/../Maps~/CombatSetup.ps1"
Map 0|Out-Null
foreach($peer in @('host','client')){Send $peer isolate;Send $peer lag 60 10 0}
$cases=[Collections.Generic.List[object]]::new()
function SaveCase($name){$cases.Add(@{name=$name;host=(Read host);client=(Read client)});$cases|ConvertTo-Json -Depth 14|Set-Content "$root/NetEffects.json"}
function Places{
    $actors=Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(-p.Side*0.6f,0f));return SSW.NetGame.Current.Players.Select(p=>new{id=p.OwnerClientId,epoch=p.Epoch}).ToArray();'
    Await 'effect fixture epochs settled' {foreach($a in $actors){$p=$c.players|Where-Object id -eq $a.id;if($p.epoch -ne $a.epoch){return $false}};return $true} 5
    Start-Sleep -Milliseconds 250
}
function InputHit($peer){$actor=(Read $peer).players|Where-Object owner;Send $peer press 0 $actor.side 0;Send $peer release 0 $actor.side 0}
if(-not $EffectRange){
CombatSpawn Magician
Eval 'foreach(var p in SSW.NetGame.Current.Players){p.Draft.Restore(new[]{7,8,10});var suit=(Unity.Netcode.NetworkVariable<int>)typeof(SSW.NetCast).GetField("_suit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(p.Cast);suit.Value=(int)SSW.Suit.Heart;typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{1000f});p.Health.Heal(1000f);p.Health.TakeDamage(100f);}return true;'|Out-Null
Await 'Joker charge shown on server and client' {@($h.players+$c.players|Where-Object magicJoker -gt 10).Count -eq 4} 5
Check $true 'both owners show the initial server Joker recharge'
SaveCase 'initial charge'
foreach($peer in @('host','client')){
    Places
    $id=((Read $peer).players|Where-Object owner).id
    InputHit $peer
    Await 'native heart card starts cooldown displays' {$a=$h.players|Where-Object id -eq $id;$b=$c.players|Where-Object id -eq $id;$a.magicEmergency -gt 0 -and $b.magicEmergency -gt 0 -and $a.magicMirror -gt 0 -and $b.magicMirror -gt 0} 4
    $a=$h.players|Where-Object id -eq $id;$b=$c.players|Where-Object id -eq $id
    Check ([Math]::Abs($a.magicEmergency-$b.magicEmergency) -lt 0.35 -and [Math]::Abs($a.magicMirror-$b.magicMirror) -lt 0.35) "$peer native hit synchronizes emergency and mirror HUD timers"
    SaveCase "$peer emergency mirror"
}
foreach($peer in @('host','client')){Send $peer tempo 0 0.5 0}
$slowBefore=@{host=(Read host);client=(Read client)}
Start-Sleep -Milliseconds 1500
$slowAfter=@{host=(Read host);client=(Read client)}
foreach($view in @('host','client')){
    $a=$slowBefore.$view.players|Where-Object id -eq $id
    $b=$slowAfter.$view.players|Where-Object id -eq $id
    $elapsed=$a.magicJoker-$b.magicJoker
    Check ($elapsed -gt 0.4 -and $elapsed -lt 1.2 -and $b.magicEmergency -gt 0) "$view cooldowns retain scaled gameplay timing at half speed"
}
@{before=$slowBefore;after=$slowAfter}|ConvertTo-Json -Depth 14|Set-Content "$root/SlowClock.json"
foreach($peer in @('host','client')){Send $peer tempo 0 1 0}
Await 'initial Joker charge completes naturally' {@($h.players+$c.players|Where-Object magicJoker -gt 0.05).Count -eq 0} 18
Check $true 'Joker becomes ready after its natural charge on both peers'
foreach($peer in @('host','client')){
    Places
    $id=((Read $peer).players|Where-Object owner).id
    InputHit $peer
    Await 'native Joker use restarts both HUD timers' {$a=$h.players|Where-Object id -eq $id;$b=$c.players|Where-Object id -eq $id;$a.magicJoker -gt 10 -and $b.magicJoker -gt 10} 4
    $a=$h.players|Where-Object id -eq $id;$b=$c.players|Where-Object id -eq $id
    Check ([Math]::Abs($a.magicJoker-$b.magicJoker) -lt 0.35) "$peer native Joker use restarts matching HUD recharge"
    SaveCase "$peer Joker consumed"
}
}
CombatSpawn Assassin
Eval 'foreach(var p in SSW.NetGame.Current.Players){p.Draft.Restore(new[]{49,52});typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{1000f});p.Health.Heal(1000f);}return true;'|Out-Null
$owners=if($EffectRange){@('client')}else{@('host','client')}
foreach($peer in $owners){
    Places
    $actor=(Read $peer).players|Where-Object owner
    $id=$actor.id
    if(-not $EffectRange){
    Send $peer cycle 1 0 1
    Send $peer cycle 0 0 1
    Await 'native skill displays the original aura on both peers' {($h.players|Where-Object id -eq $id).auraView -and ($c.players|Where-Object id -eq $id).auraView} 3
    Check $true "$peer native knife skill displays aura on both peers"
    SaveCase "$peer aura"
    if($peer -eq 'client'){Send client capture}
    Await 'aura visual ends with server duration' {-not ($h.players|Where-Object id -eq $id).auraView -and -not ($c.players|Where-Object id -eq $id).auraView} 8
    Check $true "$peer aura visual is removed at the server deadline"
    }
    Places
    Eval ('var p=SSW.NetGame.Current.Players.First(p=>p.OwnerClientId=='+$id+'ul);p.Health.Heal(p.Health.Max);p.Health.TakeDamage(p.Health.Max*0.69f);return true;')|Out-Null
    $attacker=if($peer -eq 'host'){'client'}else{'host'}
    InputHit $attacker
    Await 'native hit triggers low health range visual' {($h.players|Where-Object id -eq $id).rangeView -and ($c.players|Where-Object id -eq $id).rangeView} 3
    Check $true "$peer low health from actual melee shows range on both peers"
    SaveCase "$peer range"
    Await 'range visual clears' {-not ($h.players|Where-Object id -eq $id).rangeView -and -not ($c.players|Where-Object id -eq $id).rangeView} 3
    Check $true "$peer range visual ends naturally"
}
@{commit=$head;checks=$checks.Count;scope='Real owner input, host/client HUD cooldowns and original Assassin visuals';fixture='Native attacks on isolated map; only actor gravity cloned to zero and starting health/suit controlled';host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
Write-Output "PASS $($checks.Count) network effect checks: $root"
