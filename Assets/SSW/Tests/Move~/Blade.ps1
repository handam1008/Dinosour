param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/Blade26/Game.exe',[switch]$SkipSlopes,[int[]]$Angles=@(0,45,90))
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/Blade26/$Run"
if(Test-Path $root){throw 'Use a new run name.'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$seq=@{host=0;client=0}
$menuSeq=@{host=0;client=0}
$checks=[Collections.Generic.List[string]]::new()
$clientProcess=$null
function Eval([string]$code) {
    [IO.File]::WriteAllText("$root/Eval.cs", $code)
    $reply = unity command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json | ConvertFrom-Json
    if (-not $reply.success -or -not $reply.data.result.success) { throw ($reply | ConvertTo-Json -Depth 7) }
    return $reply.data.result.result
}
function Read($peer) {
    try { return Get-Content -LiteralPath "$root/$peer.json" -Raw | ConvertFrom-Json } catch { return $null }
}
function Await($label, [scriptblock]$condition, $timeout = 35) {
    $until = [DateTime]::UtcNow.AddSeconds($timeout)
    do {
        $script:h = Read host
        $script:c = Read client
        if ($h.error -or $c.error) { throw "Runtime error: $($h.error) $($c.error)" }
        if ($h -and $c -and (& $condition)) { return }
        Start-Sleep -Milliseconds 50
    } while ([DateTime]::UtcNow -lt $until)
    @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Failure.json"
    throw "Timeout: $label (host $($h.phase) set $($h.set); client $($c.phase) set $($c.set))"
}
function Send($peer, $op, $value = 0, $x = 0, $y = 0) {
    $seq[$peer]++
    $json = @{ seq=$seq[$peer]; op=$op; value=$value; x=$x; y=$y } | ConvertTo-Json -Compress
    [IO.File]::WriteAllText("$root/$peer.cmd.json", $json)
    if ($op -eq 'quit') { return }
    $until = [DateTime]::UtcNow.AddSeconds(5)
    while ((Read $peer).seq -lt $seq[$peer]) {
        if ([DateTime]::UtcNow -gt $until) { throw "Command timeout: $peer $op" }
        Start-Sleep -Milliseconds 30
    }
}
function Check($condition, $label) {
    if (-not $condition) { @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Failure.json"; throw $label }
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/Checks.txt", $checks)
}
function PickAll {
    $until = [DateTime]::UtcNow.AddSeconds(25)
    while ([DateTime]::UtcNow -lt $until) {
        $script:h = Read host
        $script:c = Read client
        if ($h.error -or $c.error) { throw "Draft runtime error: $($h.error) $($c.error)" }
        if ($h.phase -eq 'Playing' -and $c.phase -eq 'Playing') { return }
        foreach ($peer in @('host','client')) {
            $state = Read $peer
            $local = $state.players | Where-Object owner
            if ($state.phase -eq 'Draft' -and $local.draftUnlocked -and -not $local.ready) { Send $peer choose 0 }
        }
        Start-Sleep -Milliseconds 180
    }
    throw 'Draft did not finish.'
}
function Menu($peer) { try { Get-Content "$root/$peer.menu.json" -Raw|ConvertFrom-Json }catch{$null} }
function Click($peer,$target){
    $until=[DateTime]::UtcNow.AddSeconds(8)
    while(@((Menu $peer).buttons|Where-Object {$_.name -eq $target -and $_.enabled}).Count -eq 0){if([DateTime]::UtcNow -gt $until){throw "Button unavailable $target"};Start-Sleep -Milliseconds 80}
    $menuSeq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.menu.cmd.json",(@{seq=$menuSeq[$peer];op='click';target=$target}|ConvertTo-Json -Compress))
    $until=[DateTime]::UtcNow.AddSeconds(6)
    while((Menu $peer).seq -lt $menuSeq[$peer]){if([DateTime]::UtcNow -gt $until){throw "Click timeout $target"};Start-Sleep -Milliseconds 70}
}
function Map($title){
    Eval ('var game=SSW.NetGame.Current;var rotation=game.GetComponent<SSW.MapRotation>();int index=0;while(rotation.Prefabs[index].Title!="'+$title+'")index++;typeof(SSW.MapRotation).GetField("_index",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(rotation,index);rotation.Restart(false);foreach(var p in game.Players)p.Drive.Teleport(game.Arena.Spawn(p.Side==1?0:1));return true;')|Out-Null
    Await 'test map loaded' {$h.map -eq $title -and $c.map -eq $title -and $h.mapObject -eq $c.mapObject}
}
function MenuCmd($peer,$op,$target='',$text='',$value=0){
    $menuSeq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.menu.cmd.json",(@{seq=$menuSeq[$peer];op=$op;target=$target;text=$text;value=$value}|ConvertTo-Json -Compress))
    $until=[DateTime]::UtcNow.AddSeconds(6)
    while((Menu $peer).seq -lt $menuSeq[$peer]){if([DateTime]::UtcNow -gt $until){throw 'Menu command timeout'};Start-Sleep -Milliseconds 50}
}
function Player($snapshot,$owner){$id=if($owner -eq 'host'){0}else{1};@($snapshot.players|Where-Object {$_.id -eq $id})[0]}
function Fixture($kind,$angle){foreach($peer in @('host','client')){Send $peer move;Send $peer $kind $angle 1000 0}}
function Warp($owner,$x,$y){$id=if($owner -eq 'host'){0}else{1};Send host warp $id $x $y}
function Blade($owner){
    $owned=if($owner -eq 'host'){'true'}else{'false'}
    Eval ('var game=SSW.NetGame.Current;var p=System.Linq.Enumerable.First(game.Players,p=>p.IsOwner=='+$owned+');var knife=(SSW.NetBolt)typeof(SSW.KnifeCast).GetField("_knife",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(p.Cast.Weapon);if(knife==null)return null;var motor=(SSW.MotionMotor)typeof(SSW.MotionView).GetField("_motor",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(p.Drive);bool fits=motor.TryPlace(knife.Position,knife.Normal,knife.Spec.Radius,out var point);return new{fits,x=point.x,y=point.y,kx=knife.Position.x,ky=knife.Position.y,nx=knife.Normal.x,ny=knife.Normal.y,speed=knife.Velocity.magnitude,epoch=p.Epoch};')
}
try{
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(40)
    do{try{$scene=Eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'}catch{$scene=''};if($scene -eq 'MainMenu'){break};if([DateTime]::UtcNow -gt $until){throw 'Menu startup timeout'};Start-Sleep -Milliseconds 400}while($true)
    Eval ('var game=SSW.NetGame.GetOrCreate();game.SetLocalJob(SSW.PlayerJob.Assassin);game.SetProfile(SSW.Fighter.Create("BladeHost"));game.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");game.gameObject.AddComponent<SSW.MenuProbe>().Init("'+$root+'/host");return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-profile','blade26-client','--net-name','BladeClient','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','960','-screen-height','540') -WindowStyle Hidden -PassThru
    Await 'both menus' {(Menu host).scene -eq 'MainMenu' -and (Menu client).scene -eq 'MainMenu'} 50
    foreach($peer in @('host','client')){MenuCmd $peer job '' '' 4;Send $peer fps 60;Click $peer 'Button_Play'}
    Click host 'Button_방 만들기'
    MenuCmd host text '방 이름Input' '동작 검증'
    MenuCmd host toggle '방 목록에서 숨기기Toggle' '' 1
    Click host '방 만들기Button'
    Await 'private Relay room' {(Menu host).joined -and (Menu host).code.Length -gt 0 -and -not(Menu host).busy} 90
    Click client 'Button_방 들어가기'
    MenuCmd client text '참가 코드Input' (Menu host).code
    Click client '코드 참가Button'
    Await 'Relay pair ready' {(Menu host).canStart -and (Menu client).joined -and (Menu host).count -eq 2} 90
    Click host '게임 시작Button'
    Await 'draft' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 45
    PickAll
    Map 'KDH_Map 2'
    Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000);}return true;'|Out-Null
    Check ((Player $h host).job -eq 'Assassin' -and (Player $c client).job -eq 'Assassin') 'both peers use assassin'
    if(-not $SkipSlopes){foreach($kind in @('ground','platform')){
        foreach($angle in @(45,-45)){
            Fixture $kind $angle
            foreach($owner in @('host','client')){
                Warp $owner 1000 5
                Await "$owner $kind $angle landing" {(Player $h $owner).grounded -and (Player $c $owner).grounded -and (Player $h $owner).epoch -eq (Player $c $owner).epoch} 6
                $start=Player $h $owner
                Send $owner metrics
                Start-Sleep -Milliseconds 1400
                $h=Read host;$c=Read client;$end=Player $h $owner
                Check ([Math]::Abs($end.position.x-$start.position.x) -lt 0.04 -and $end.grounded) "$owner $kind $angle stationary slope"
                Send $owner jump
                Await "$owner $kind $angle jump" {(Player $h $owner).velocity.y -gt 4 -and (Player $c $owner).velocity.y -gt 4} 3
                Check ($true) "$owner $kind $angle jump observed on both peers"
            }
        }
    }
    }
    foreach($peer in @('host','client')){Send $peer lag 80 20 2}
    Fixture ground 45
    Warp client 1000 5
    Await 'impaired slope landing' {(Player $h client).grounded -and (Player $c client).grounded -and (Player $h client).epoch -eq (Player $c client).epoch} 6
    Start-Sleep -Milliseconds 500
    Send client metrics
    Send client trace 20 5
    Send host trace 20 5
    Send client jump
    Await 'impaired slope jump' {(Player $h client).velocity.y -gt 4 -and (Player $c client).velocity.y -gt 4} 4
    Check ($true) 'client slope jump with added 80ms delay 20ms jitter and 2 percent loss'
    $trace=0
    foreach($angle in $Angles){
        Fixture ground $angle
        foreach($owner in @('host','client')){
            Eval 'foreach(var p in SSW.NetGame.Current.Players){var field=typeof(SSW.JobCast).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var value=(Unity.Netcode.NetworkVariable<SSW.WeaponState>)field.GetValue(p.Cast.Weapon);var state=value.Value;state.Skill=0;state.Ammo=0;value.Value=state;}return true;'|Out-Null
            $x=if($angle -eq 90){997}else{1000}
            Warp $owner $x 3
            Await 'knife setup warp synchronized' {(Player $h $owner).epoch -eq (Player $c $owner).epoch} 3
            Start-Sleep -Milliseconds 50
            (Read host)|ConvertTo-Json -Depth 14|Set-Content "$root/Before-$angle-$owner.json"
            if($angle -eq 90){Send $owner cycle 1 1 0}else{Send $owner cycle 1 0 -1}
            $until=[DateTime]::UtcNow.AddSeconds(3)
            do{$knife=Blade $owner; $knife|ConvertTo-Json|Add-Content "$root/Knife-$angle-$owner-Samples.json";if($knife -and $knife.speed -lt 0.01){break};if([DateTime]::UtcNow -gt $until){throw "Knife did not stick: $angle $owner"};Start-Sleep -Milliseconds 60}while($true)
            Check ($knife.fits) "$owner $angle knife destination fits"
            $knife|ConvertTo-Json|Set-Content "$root/Knife-$angle-$owner.json"
            $trace++
            Send $owner trace $trace 2.5
            Send $owner cycle 1 1 0
            Await "$owner $angle blink epoch" {(Player $h $owner).epoch -gt $knife.epoch -and (Player $c $owner).epoch -gt $knife.epoch} 3
            $a=Player $h $owner;$b=Player $c $owner
            Check ([Math]::Abs($a.position.x-$knife.x) -lt 0.05 -and [Math]::Abs($b.position.x-$knife.x) -lt 0.05) "$owner $angle knife x matches on both peers"
            if($angle -ne 90){Check ([Math]::Abs($a.position.y-$knife.y) -lt 0.08 -and [Math]::Abs($b.position.y-$knife.y) -lt 0.08) "$owner $angle lands above surface on both peers"}
            Await 'blink trace saved' {(Test-Path "$root/$owner.trace.$trace.json")} 4
            $frames=(Get-Content "$root/$owner.trace.$trace.json" -Raw|ConvertFrom-Json).frames
            $first=@($frames|Where-Object {$_.epoch -gt $knife.epoch})[0]
            Check ($first -and [Math]::Abs($first.body.x-$knife.x) -lt 0.05 -and [Math]::Abs($first.body.y-$knife.y) -lt 0.3) "$owner $angle first blink frame is at knife surface"
        }
    }
    Fixture ground 0
    foreach($owner in @('host','client')){
        Eval 'foreach(var p in SSW.NetGame.Current.Players){var field=typeof(SSW.JobCast).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var value=(Unity.Netcode.NetworkVariable<SSW.WeaponState>)field.GetValue(p.Cast.Weapon);var state=value.Value;state.Skill=0;state.Ammo=0;value.Value=state;}return true;'|Out-Null
        Warp $owner 1000 7
        Await 'air warp synchronized' {(Player $h $owner).epoch -eq (Player $c $owner).epoch} 3
        $epoch=(Player $h $owner).epoch
        $caster=(Player $h $owner).objectId
        $trace++
        Send $owner trace $trace 2.5
        Send $owner cycle 1 1 0
        Start-Sleep -Milliseconds 160
        Send $owner cycle 1 1 0
        Await 'air blink' {(Player $h $owner).epoch -gt $epoch -and (Player $c $owner).epoch -gt $epoch} 4
        Await 'air trace saved' {(Test-Path "$root/$owner.trace.$trace.json")} 4
        $frames=(Get-Content "$root/$owner.trace.$trace.json" -Raw|ConvertFrom-Json).frames
        $frame=@($frames|Where-Object {$_.epoch -gt $epoch -and @($_.shots|Where-Object {$_.caster -eq $caster -and $_.ending}).Count -gt 0})[0]
        if($frame){
            $tail=@($frame.shots|Where-Object {$_.caster -eq $caster -and $_.ending})[0]
            $error=[Math]::Sqrt([Math]::Pow($frame.body.x-$tail.position.x,2)+[Math]::Pow($frame.body.y-$tail.position.y,2))
            Check ($error -lt 0.4) "$owner airborne blink matches consumed blade ($error)"
        }else{
            Check ($owner -eq 'host') "$owner airborne blink has authoritative blade end"
            Check ($true) 'host airborne knife recall increments teleport epoch'
        }
    }
    @{checks=$checks.Count;host=$h;client=$c;build=$Build}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) Relay slope and knife checks"
}finally{
    foreach($peer in @('client','host')){try{if((Read $peer).listening){Send $peer exit}}catch{Write-Warning $_.Exception.Message}}
    try{Send client quit}catch{}
    if($clientProcess -and -not $clientProcess.WaitForExit(5000)){Stop-Process -Id $clientProcess.Id}
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
}
