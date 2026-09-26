$bookJson = Eval 'return UnityEngine.JsonUtility.ToJson(UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.StatsBook>("Assets/SSW/Resources/Network/Stats.asset"));'
$jobNames=@('None','Witch','Magician','Swordsman','Assassin','Gambler','Gunner')
$values=@(($bookJson|ConvertFrom-Json)._fighters|ForEach-Object {[pscustomobject]@{job=$jobNames[$_.Job];stats=$_}})
if($values.Count -ne 6){throw 'Six base profiles are required'}
$baseline=[pscustomobject]@{json=$bookJson;values=$values}
$baseline | ConvertTo-Json -Depth 8 | Set-Content "$root/Baseline.json"
[IO.File]::WriteAllText("$root/Book.json", $baseline.json)
function SameStats($a,$b){
    foreach($property in $a.PSObject.Properties){
        $left=$property.Value;$right=$b.($property.Name)
        if($null -eq $right){return $false}
        if($left -is [pscustomobject]){if(-not(SameStats $left $right)){return $false}}
        elseif([Math]::Abs([double]$left-[double]$right) -gt 0.0001){return $false}
    }
    return $true
}
function SpawnJob($job){
    $code=@'
var g=SSW.NetGame.Current;
var prefab=(SSW.NetPlayer)new UnityEditor.SerializedObject(g).FindProperty("_playerPrefab").objectReferenceValue;
var players=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{p.OwnerClientId,p.Side,p.Info}));
typeof(SSW.NetGame).GetMethod("ClearShots",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(g,null);
foreach(var p in System.Linq.Enumerable.ToArray(g.Players))p.NetworkObject.Despawn();
foreach(var old in players){var p=UnityEngine.Object.Instantiate(prefab);p.Init(SSW.PlayerJob.@JOB@,old.Side,old.Info);p.NetworkObject.SpawnAsPlayerObject(old.OwnerClientId,true);p.Draft.Restore(System.Array.Empty<int>());}
UnityEngine.Physics2D.IgnoreCollision(g.Players[0].Collider,g.Players[1].Collider);
foreach(var p in g.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -5f,5f));
return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,objectId=p.NetworkObjectId}));
'@
    $spawn=Eval $code.Replace('@JOB@',$job)
    Await "$job both spawns" {foreach($p in $spawn){$remote=$c.players|Where-Object id -eq $p.id;if($null -eq $remote -or $remote.objectId -ne $p.objectId -or $remote.job -ne $job){return $false}};return $true} 10
    return $spawn
}
function Isolate{
    foreach($peer in @('host','client')){Send $peer isolate;Send $peer wideground 0 0 3}
    Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -5f,5f));return true;'|Out-Null
    Await 'both grounded on probe floor' {$h.players.Count -eq 2 -and $c.players.Count -eq 2 -and $h.players[0].grounded -and $h.players[1].grounded -and $c.players[0].grounded -and $c.players[1].grounded} 6
}
try{
    Isolate
    Send host lag 60 10 0
    Send client lag 60 10 0
    $entries=if($StatsProfiles){@()}else{$baseline.values}
    foreach($entry in $entries){
        $job=$entry.job
        $spawn=SpawnJob $job
        Await "$job health and profile replicated" {foreach($p in $h.players){$remote=$c.players|Where-Object id -eq $p.id;if(-not(SameStats $p.stats $entry.stats) -or -not(SameStats $remote.stats $entry.stats) -or [Math]::Abs($p.max-$entry.stats.Health) -gt 0.01 -or [Math]::Abs($remote.max-$entry.stats.Health) -gt 0.01){return $false}};return $true} 8
        Check $true "$job source base stats identical on host and client"
        foreach($p in @($h.players)+@($c.players)){
            Check ($p.bodyMaterial -eq 'Player Physics' -and $p.colliderMaterial -eq 'Player Physics') "$job player $($p.id) body and collider material"
            Check ([Math]::Abs($p.jumpSpeed-$entry.stats.JumpSpeed) -lt 0.001) "$job player $($p.id) jump setting"
        }
        Await "$job grounded" {$h.players[0].grounded -and $h.players[1].grounded} 6
        foreach($peer in @('host','client')){
            $actor=(Read $peer).players|Where-Object owner
            Send $peer move 0 1 0
            Await "$job $peer movement" {$p=$h.players|Where-Object id -eq $actor.id;[Math]::Abs([Math]::Abs($p.velocity.x)-$entry.stats.MoveSpeed) -lt 0.1} 3
            Check $true "$job $peer actual movement uses source speed"
            Send $peer move
            Send $peer jump
            Await "$job $peer jump" {$p=$h.players|Where-Object id -eq $actor.id;$p.velocity.y -gt $entry.stats.JumpSpeed*0.5} 3
            Check $true "$job $peer actual jump accepted"
            Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -5f,5f));return true;'|Out-Null
            Await "$job grounded again" {$h.players[0].grounded -and $h.players[1].grounded} 6
        }
        if($job -eq 'Swordsman' -or $job -eq 'Assassin'){
            Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -0.6f,5f));return true;'|Out-Null
            Await 'melee targets grounded' {$h.players[0].grounded -and $h.players[1].grounded} 6
            foreach($peer in @('host','client')){
                $actor=(Read $peer).players|Where-Object owner
                $before=(Read host).players|Where-Object id -ne $actor.id
                $expected=$before.hp-$entry.stats.Damage
                Send $peer press 0 $actor.side 0
                Send $peer release 0 $actor.side 0
                Await "$job melee damage" {$p=$c.players|Where-Object id -eq $before.id;[Math]::Abs($p.hp-$expected) -lt 0.01} 4
                Start-Sleep -Milliseconds 250
                Await 'melee damage remains stable after attack window' {$p=$h.players|Where-Object id -eq $before.id;$null -ne $p -and [Math]::Abs($p.hp-$expected) -lt 0.01} 2
                Check $true "$job $peer basic attack deals source damage once"
            }
        }else{
            if($job -eq 'Gunner'){Await 'one natural gun round loaded' {$h.players[0].ammo -gt 0 -and $h.players[1].ammo -gt 0} 5}
            if($job -eq 'Witch'){Await 'natural potion brewing' {$h.players[0].heldView -ge 0 -and $h.players[1].heldView -ge 0} 5}
            foreach($peer in @('host','client')){
                $actor=(Read $peer).players|Where-Object owner
                $server=(Read host).players|Where-Object id -eq $actor.id
                Send $peer press 0 0 1
                Send $peer release 0 0 1
                Await "$job $peer projectile spawned" {($h.players|Where-Object id -eq $actor.id).fired -gt $server.fired} 4
                Check $true "$job $peer projectile spawned through normal input"
                if($job -eq 'Gunner' -or $job -eq 'Gambler'){
                    Await "$job $peer projectile stats arrived" {@($c.shots|Where-Object {$_.caster -eq $actor.objectId -and $_.type -eq 'bolt'}).Count -gt 0} 3
                    $shot=$c.shots|Where-Object {$_.caster -eq $actor.objectId -and $_.type -eq 'bolt'}|Select-Object -Last 1
                    Check ([Math]::Abs($shot.spec.Speed-$entry.stats.Flight.Speed) -lt 0.001 -and [Math]::Abs($shot.spec.Gravity-$entry.stats.Flight.Gravity) -lt 0.001 -and [Math]::Abs($shot.spec.Life-$entry.stats.Flight.Life) -lt 0.001) "$job projectile speed gravity lifetime replicated"
                }
            }
        }
        @{host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 14|Set-Content "$root/$job.json"
    }
    $changed=(Eval 'var book=UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.StatsBook>("Assets/SSW/Resources/Network/Stats.asset");var values=(SSW.FighterStats[])typeof(SSW.StatsBook).GetField("_fighters",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(book);values=(SSW.FighterStats[])values.Clone();for(int i=0;i<values.Length;i++){if(values[i].Job==SSW.PlayerJob.Swordsman){values[i].Health=137f;values[i].MoveSpeed=8.5f;values[i].JumpSpeed=15f;}if(values[i].Job==SSW.PlayerJob.Gunner){values[i].Capacity=15;values[i].Reload=0.12f;values[i].ChargeTime=0.2f;}}book.Replace(values);return UnityEngine.JsonUtility.ToJson(book.At(SSW.PlayerJob.Swordsman));')|ConvertFrom-Json
    SpawnJob Swordsman|Out-Null
    Await 'server profile replaces client bundled defaults' {foreach($p in $c.players){if(-not(SameStats $p.stats $changed)){return $false}};return $true} 6
    Check $true 'client uses server selected profile instead of its bundled defaults'
    Await 'changed profiles grounded' {$h.players[0].grounded -and $h.players[1].grounded} 6
    foreach($peer in @('host','client')){
        $actor=(Read $peer).players|Where-Object owner
        Send $peer move 0 1 0
        Await 'changed movement speed applied' {$p=$h.players|Where-Object id -eq $actor.id;[Math]::Abs([Math]::Abs($p.velocity.x)-8.5) -lt 0.1} 3
        Send $peer move
        $takeoff=((Read host).players|Where-Object id -eq $actor.id).position.y
        Send $peer jump
        Await 'changed jump height applied' {$p=$h.players|Where-Object id -eq $actor.id;$p.position.y -gt $takeoff+3.2} 3
        Check $true "$peer movement and jump use changed server profile"
        Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -5f,5f));return true;'|Out-Null
        Await 'changed profiles grounded again' {$h.players[0].grounded -and $h.players[1].grounded} 6
    }
    $giant=Eval 'var deck=UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");for(int i=0;i<deck.Count;i++)if(deck.At(i) is SSW.CommonAugment a && a.type==SSW.CommonAugmentType.Giant)return i;throw new System.InvalidOperationException("Giant missing");'
    foreach($side in @(0,1)){Send host grant $giant $side 0}
    $health=137*1.55
    Await 'giant derives from changed base' {[Math]::Abs($h.players[0].max-$health) -lt 0.01 -and [Math]::Abs($h.players[1].max-$health) -lt 0.01} 4
    Check $true 'max health augment multiplies the source base once'
    for($round=0;$round -lt 3;$round++){
        $old=Read host
        Eval 'var target=SSW.NetGame.Current.Players[0];target.Health.TakeDamage(9999f);return true;'|Out-Null
        Await 'actual death respawns players' {$h.players.Count -eq 2 -and $c.players.Count -eq 2 -and $h.players[0].objectId -ne $old.players[0].objectId -and $c.players[0].objectId -ne $old.players[0].objectId} 12
        Await 'respawn health replicated' {foreach($p in $c.players){if([Math]::Abs($p.max-$health) -gt 0.01 -or -not(SameStats $p.stats $changed)){return $false}};return $true} 6
        Check $true "actual round respawn $round preserves augments without stacking base stats"
        if((Read host).phase -eq 'Draft'){Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Draft.Restore(System.Linq.Enumerable.ToArray(p.Draft.Owned));SSW.NetGame.Current.Match.Picked();return true;'|Out-Null}
        Await 'next round playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 12
        Isolate
    }
    SpawnJob Gunner|Out-Null
    Await '15 rounds naturally loaded and rendered' {$h.players[0].ammo -eq 15 -and $h.players[1].ammo -eq 15 -and $h.players[0].visibleAmmo -eq 15 -and $c.players[1].visibleAmmo -eq 15} 8
    Check $true 'changed capacity reload and ammo view use 15 slots on both peers'
    foreach($peer in @('host','client')){
        $actor=(Read $peer).players|Where-Object owner
        for($shotIndex=0;$shotIndex -lt 15;$shotIndex++){Send $peer press 0 0 1;Send $peer release 0 0 1}
        Await 'expanded magazine refills' {($h.players|Where-Object id -eq $actor.id).ammo -eq 15} 6
        Check $true "$peer expanded magazine can fire and refill"
    }
    @{checks=$checks.Count;host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) stats runtime checks: $root"
}finally{
    Eval ('var book=UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.StatsBook>("Assets/SSW/Resources/Network/Stats.asset");UnityEngine.JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText("'+$root+'/Book.json"),book);return true;')|Out-Null
}
