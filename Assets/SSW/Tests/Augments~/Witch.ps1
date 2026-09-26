$replay=unity command eval_file --file 'Assets/SSW/Tests/Augments~/PocketReplay.cs' --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
$replay|ConvertTo-Json -Depth 10|Set-Content "$root/PocketReplay.json"
Check ($replay.data.result.success -and $replay.data.result.result.passed -eq 13) 'all thirteen reordered stock replay cases pass'
$catalog=Eval @'
var g=SSW.NetGame.Current;
var deck=UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");
var stock=UnityEngine.Resources.Load<SSW.NetStock>("Network/Potions");
var result=new System.Collections.Generic.List<object>();
foreach(var p in g.Players)
{
    var witch=p.GetComponent<RYU._01.Script.Argument.WitchAugmentController>();
    if(witch.Unlocked.Count!=0)throw new System.InvalidOperationException("Witch must start without unlocks");
    p.Draft.Restore(new[]{82});
    if(witch.Unlocked.Count!=1)throw new System.InvalidOperationException("Poison unlock failed");
    p.Draft.Restore(new[]{83});
    if(witch.Unlocked.Count!=2 || !witch.Has(RYU._01.Script.Argument.WitchAugmentType.reproductionUnlock))throw new System.InvalidOperationException("Regeneration unlock failed");
    p.Draft.Restore(new[]{84});
    if(witch.Unlocked.Count!=2 || UnityEngine.Mathf.Abs(witch.CycleInterval(1.5f)-0.9f)>0.001f)throw new System.InvalidOperationException("All unlock must not duplicate potions and must shorten brewing");
    foreach(var potion in witch.Unlocked)if(stock.IndexOf(potion)<0)throw new System.InvalidOperationException("Unlocked potion not in network stock");
    result.Add(new{p.OwnerClientId,unlocked=witch.Unlocked.Select(x=>new{x.name,id=stock.IndexOf(x)}).ToArray()});
}
return result;
'@
$catalog|ConvertTo-Json -Depth 6|Set-Content "$root/Unlocks.json"
Await 'all missing witch cards replicated' {$h.players.Count -eq 2 -and $c.players.Count -eq 2 -and @($c.players|Where-Object {$_.augments.Count -ne 3}).Count -eq 0}
Check ($true) 'poison and regeneration unlock distinct network potions; all unlock speeds brewing on both players'
foreach($peer in @('host','client')){
    $id=((Read $peer).players|Where-Object owner).id
    Eval ('var p=SSW.NetGame.Current.Players.First(x=>x.OwnerClientId=='+$id+'UL);if(p.Guard!=p.GetComponent<SSW.BuffGuard>())throw new System.InvalidOperationException("Guard reference is not bound");var d=UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");int id=Enumerable.Range(0,d.Count).First(i=>d.At(i) is SSW.CommonAugment a && a.type==SSW.CommonAugmentType.Invincible);p.Draft.Restore(new[]{id});return true;')|Out-Null
    Send $peer guard
    Await "$peer guard reached server" {($h.players|Where-Object id -eq $id).guarding}
    $blocked=Eval ('var p=SSW.NetGame.Current.Players.First(x=>x.OwnerClientId=='+$id+'UL);float before=p.Health.Current;p.Health.TakeDamage(10f);return new{before,after=p.Health.Current,ready=p.Guard.ReadyAt};')
    Check ($blocked.before -eq $blocked.after) "$peer actual guard input blocks damage with restored reference"
    Send $peer guard
    $ready=Eval ('return SSW.NetGame.Current.Players.First(x=>x.OwnerClientId=='+$id+'UL).Guard.ReadyAt;')
    Check ($ready -eq $blocked.ready) "$peer guard cannot restart during cooldown"
    Await "$peer guard expires" {-not ($h.players|Where-Object id -eq $id).guarding}
    $hit=Eval ('var p=SSW.NetGame.Current.Players.First(x=>x.OwnerClientId=='+$id+'UL);float before=p.Health.Current;p.Health.TakeDamage(10f);return before-p.Health.Current;')
    Check ($hit -eq 10) "$peer damage resumes when guard ends"
}
$natural=Eval ('var g=SSW.NetGame.Current;var samples=new System.Collections.Generic.List<object>();var latest=new System.Collections.Generic.Dictionary<ulong,uint>();var f=typeof(SSW.NetCast).GetField("_potions",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);System.Collections.IEnumerator Sample(){float end=UnityEngine.Time.realtimeSinceStartup+8f;while(UnityEngine.Time.realtimeSinceStartup<end){foreach(var p in g.Players){var s=((Unity.Netcode.NetworkVariable<SSW.NetCast.PotionState>)f.GetValue(p.Cast)).Value;if(latest.TryGetValue(p.OwnerClientId,out uint revision)&&revision==s.Revision)continue;latest[p.OwnerClientId]=s.Revision;samples.Add(new{id=p.OwnerClientId,time=g.ServerTime,s.Revision,s.Held,s.Next});}yield return null;}System.IO.File.WriteAllText("'+$root+'/Natural.json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));}g.StartCoroutine(Sample());return true;')
$deadline=[DateTime]::UtcNow.AddSeconds(12)
while(-not(Test-Path "$root/Natural.json")){if([DateTime]::UtcNow -gt $deadline){throw 'Natural brew observation timed out'};Start-Sleep -Milliseconds 200}
$natural=Get-Content "$root/Natural.json" -Raw|ConvertFrom-Json
Check ($natural.Count -ge 12 -and @($natural|Where-Object {$_.Held -lt 0 -or $_.Held -gt 5 -or $_.Next -lt 0 -or $_.Next -gt 5}).Count -eq 0) 'unforced timed brewing chooses only unlocked source potions; small sample is not a distribution proof'
Send host lag 100 20 0
Send client lag 100 20 0
foreach($peer in @('host','client')){
    $owner=(Read $peer).players|Where-Object owner
    $id=$owner.id
    $setup=Eval ('var g=SSW.NetGame.Current;var p=g.Players.First(x=>x.OwnerClientId=='+$id+'UL);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var bag=(SSW.CastStock)typeof(SSW.NetCast).GetField("_inventory",flags).GetValue(p.Cast);while(bag.Held.Id!=0)bag.Take(bag.Held.Id,p.Epoch,g.ServerTime,out _);bag.Add(1,g.ServerTime,1.25);bag.Add(2,g.ServerTime,1.25);typeof(SSW.NetCast).GetField("_brewAt",flags).SetValue(p.Cast,g.ServerTime+1000d);typeof(SSW.NetCast).GetMethod("PublishStock",flags).Invoke(p.Cast,null);return new{fired=p.Cast.Shots,held=bag.Held.Kind,next=bag.Next.Kind};')
    Await "$peer setup replicated" {$p=(Read $peer).players|Where-Object owner;$p.heldView -eq 1 -and $p.pocket -eq -1}
    Send $peer cycle 1 1 0
    Await "$peer stores held potion" {foreach($snapshot in @($h,$c)){$p=$snapshot.players|Where-Object id -eq $id;if($p.potion -ne 2 -or $p.pocket -ne 1 -or -not $p.pocketUsed){return $false}};return $true}
    Send $peer cycle 1 1 0
    Start-Sleep -Milliseconds 400
    $p=(Read host).players|Where-Object id -eq $id
    Check ($p.pocket -eq 1 -and $p.potion -eq 2 -and $p.pocketUsed) "$peer cannot swap twice before throwing"
    Send $peer press 0 1 0
    Await "$peer throws reserve and unlocks pocket" {$p=$h.players|Where-Object id -eq $id;$p.fired -eq $setup.fired+1 -and $p.shotKind -eq 2 -and -not $p.pocketUsed}
    Await "$peer empty hand replicated" {$p=(Read $peer).players|Where-Object owner;$p.heldView -eq -1 -and -not $p.pocketUsed}
    Send $peer cycle 1 1 0
    Await "$peer retrieves pocket" {foreach($snapshot in @($h,$c)){$p=$snapshot.players|Where-Object id -eq $id;if($p.potion -ne 1 -or $p.pocket -ne -1){return $false}};return $true}
    Send $peer press 0 1 0
    Await "$peer throws retrieved potion" {$p=$h.players|Where-Object id -eq $id;$p.fired -eq $setup.fired+2 -and $p.shotKind -eq 1}
    Send $peer press 0 1 0
    Start-Sleep -Milliseconds 400
    $p=(Read host).players|Where-Object id -eq $id
    Check ($p.fired -eq $setup.fired+2) "$peer storage, retrieve and throw work with simulated delay without extra shots"
    @{host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 12|Set-Content "$root/Pocket-$peer.json"
}
Send host lag 0 0 0
Send client lag 0 0 0
Write-Output "PASS $($checks.Count) witch checks: $root"
