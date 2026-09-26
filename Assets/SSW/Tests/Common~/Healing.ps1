Map 0|Out-Null
Await 'initial healing events empty' {$h.players.Count -eq 2 -and $c.players.Count -eq 2 -and @($h.players|Where-Object {$_.heals.Count -gt 0}).Count -eq 0 -and @($c.players|Where-Object {$_.heals.Count -gt 0}).Count -eq 0} 4
Check ($true) 'spawn and maximum initialization do not display healing'
Eval 'foreach(var p in SSW.NetGame.Current.Players){p.Drive.Teleport(new UnityEngine.Vector2(p.Side*4f,1f));p.Drive.Freeze(30f);p.Health.TakeDamage(30f);}return true;'|Out-Null
Await 'damage reaches both peers' {@($h.players|Where-Object hp -ne 9970).Count -eq 0 -and @($c.players|Where-Object hp -ne 9970).Count -eq 0} 4
Start-Sleep -Milliseconds 1000
Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Health.Heal(7.5f);return true;'|Out-Null
Await 'healing events reach both peers once' {foreach($snapshot in @($h,$c)){foreach($p in $snapshot.players){if($p.hp -ne 9977.5 -or $p.heals.Count -ne 1 -or $p.heals[0] -ne 7.5){return $false}}};return $true} 3
Check ($true) 'host and client each display server-confirmed actual recovery once'
Send host capture
Send client capture
@{host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/HealActual.json"
Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Health.Heal(100f);return true;'|Out-Null
Await 'overheal capped on both peers' {foreach($snapshot in @($h,$c)){foreach($p in $snapshot.players){if($p.hp -ne 10000 -or $p.heals.Count -ne 2 -or $p.heals[1] -ne 22.5){return $false}}};return $true} 3
Check ($true) 'overheal displays only remaining missing health'
Eval 'foreach(var p in SSW.NetGame.Current.Players){p.Health.Heal(100f);p.Health.Heal(-5f);p.Health.Heal(float.NaN);typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{12000f});}return true;'|Out-Null
Await 'maximum update reaches both peers' {@($c.players|Where-Object hp -ne 12000).Count -eq 0} 3
Check (@($h.players|Where-Object {$_.heals.Count -ne 2}).Count -eq 0 -and @($c.players|Where-Object {$_.heals.Count -ne 2}).Count -eq 0) 'full health invalid amounts and maximum changes do not display recovery'
Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Health.TakeDamage(10f);return true;'|Out-Null
Await 'client heal rejection fixture' {@($c.players|Where-Object hp -ne 11990).Count -eq 0} 3
Send client heal 100
Start-Sleep -Milliseconds 250
$h=Read host;$c=Read client
Check (@($h.players|Where-Object hp -ne 11990).Count -eq 0 -and @($c.players|Where-Object {$_.heals.Count -ne 2}).Count -eq 0) 'client cannot create unconfirmed healing'
$oldIds=$h.players.objectId
Eval 'var p=SSW.NetGame.Current.Players[0];p.Health.ReceiveDamage(new SSW.DamageRequest(null,p.Health.Max*10f,SSW.DamageTag.Environment|SSW.DamageTag.IgnoreDefense));return true;'|Out-Null
Await 'new round respawns both players' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and @($c.players|Where-Object {$_.objectId -in $oldIds}).Count -eq 0} 15
Check (@($h.players|Where-Object {$_.heals.Count -gt 0}).Count -eq 0 -and @($c.players|Where-Object {$_.heals.Count -gt 0}).Count -eq 0) 'round respawn does not display fake recovery'
@{host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/HealFinal.json"
Write-Output "PASS $($checks.Count) healing runtime checks: $root"
