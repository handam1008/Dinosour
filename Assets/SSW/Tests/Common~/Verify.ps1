param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/Common/Game.exe',[switch]$SkipGun,[switch]$SkipCommon,[switch]$ResumeMovement)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/Common/$Run"
if(Test-Path -LiteralPath $root){throw 'Use a new run name.'}
if(-not(Test-Path -LiteralPath (Join-Path $Project $Build))){throw "Build not found: $Build"}
[IO.Directory]::CreateDirectory($root)|Out-Null
$seq=@{host=0;client=0}
$menuSeq=@{host=0;client=0}
$ids=@{host=0;client=1}
$checks=[Collections.Generic.List[string]]::new()
$clientProcess=$null
$editorStarted=$false
$scenario=0
function Eval([string]$code){
    [IO.File]::WriteAllText("$root/Eval.cs",$code)
    $reply=unity command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 8)}
    return $reply.data.result.result
}
function Save($name,$value){$value|ConvertTo-Json -Depth 18|Set-Content -LiteralPath "$root/$name.json"}
function Read($peer){try{return Get-Content -LiteralPath "$root/$peer.json" -Raw|ConvertFrom-Json}catch{return $null}}
function Player($snapshot,$peer){return @($snapshot.players|Where-Object {$_.id -eq $ids[$peer]})[0]}
function Near($left,$right,$tolerance=0.08){return [Math]::Abs([double]$left-[double]$right) -le $tolerance}
function Await($label,[scriptblock]$condition,$timeout=12){
    $timeoutAt=[DateTime]::UtcNow.AddSeconds($timeout)
    do{
        $script:h=Read host
        $script:c=Read client
        if($h.error -or $c.error){throw "Runtime error: $($h.error) $($c.error)"}
        if($h -and $c -and (& $condition)){return}
        Start-Sleep -Milliseconds 45
    }while([DateTime]::UtcNow -lt $timeoutAt)
    Save Failure @{label=$label;host=$h;client=$c}
    throw "Timeout: $label (host $($h.phase), client $($c.phase))"
}
function Check($condition,$label){
    if(-not $condition){Save Failure @{label=$label;host=$h;client=$c};throw $label}
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/Checks.txt",$checks)
}
function WriteCommand($path,$json){
    $temporary=$path+'.tmp'
    [IO.File]::WriteAllText($temporary,$json)
    $until=[DateTime]::UtcNow.AddSeconds(3)
    do{
        try{[IO.File]::Move($temporary,$path,$true);return}
        catch [IO.IOException]{if([DateTime]::UtcNow -ge $until){throw};Start-Sleep -Milliseconds 20}
    }while($true)
}
function Send($peer,$op,$value=0,$x=0,$y=0){
    $seq[$peer]++
    WriteCommand "$root/$peer.cmd.json" (@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress)
    if($op -eq 'quit'){return}
    $until=[DateTime]::UtcNow.AddSeconds(6)
    while((Read $peer).seq -lt $seq[$peer]){
        if([DateTime]::UtcNow -gt $until){throw "Command timeout: $peer $op"}
        Start-Sleep -Milliseconds 25
    }
}
function Menu($peer){try{return Get-Content -LiteralPath "$root/$peer.menu.json" -Raw|ConvertFrom-Json}catch{return $null}}
function MenuCmd($peer,$op,$target='',$text='',$value=0){
    $menuSeq[$peer]++
    WriteCommand "$root/$peer.menu.cmd.json" (@{seq=$menuSeq[$peer];op=$op;target=$target;text=$text;value=$value}|ConvertTo-Json -Compress)
    $until=[DateTime]::UtcNow.AddSeconds(6)
    while((Menu $peer).seq -lt $menuSeq[$peer]){
        if([DateTime]::UtcNow -gt $until){throw "Menu command timeout: $peer $op"}
        Start-Sleep -Milliseconds 40
    }
}
function Click($peer,$target){
    $until=[DateTime]::UtcNow.AddSeconds(8)
    while(@((Menu $peer).buttons|Where-Object {$_.name -eq $target -and $_.enabled}).Count -eq 0){
        if([DateTime]::UtcNow -gt $until){throw "Button unavailable: $peer $target"}
        Start-Sleep -Milliseconds 60
    }
    MenuCmd $peer click $target
}
function PickAll{
    $until=[DateTime]::UtcNow.AddSeconds(30)
    do{
        $script:h=Read host;$script:c=Read client
        if($h.error -or $c.error){throw "Draft runtime error: $($h.error) $($c.error)"}
        if($h.phase -eq 'Playing' -and $c.phase -eq 'Playing'){return}
        foreach($peer in @('host','client')){
            $state=Read $peer
            $local=@($state.players|Where-Object owner)[0]
            if($state.phase -eq 'Draft' -and $local.draftUnlocked -and -not $local.ready){Send $peer choose 0}
        }
        Start-Sleep -Milliseconds 90
    }while([DateTime]::UtcNow -lt $until)
    throw 'Draft did not finish.'
}
function F($value){return ([double]$value).ToString('0.########',[Globalization.CultureInfo]::InvariantCulture)}
function Card($name){
    if($name -match '^\d+$'){return [int]$name}
    $found=@($catalog.cards|Where-Object {$_.key -eq $name -or $_.name -eq $name}|Sort-Object @{Expression='common';Descending=$true},id)
    if($found.Count -eq 0){throw "Card missing from deck: $name"}
    return [int]$found[0].id
}
function Grant($peer,$name){
    $card=Card $name
    Send host grant $card $(if($peer -eq 'host'){0}else{1})
    Await "$peer card $name replicated" {(Player $h $peer).augments -contains $card -and (Player $c $peer).augments -contains $card}
    return $card
}
function Set-Hp($peer,$value){
    $id=$ids[$peer];$number=F $value
    Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$id+'UL);typeof(SSW.Health).GetMethod("ApplyNetworkState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{'+$number+'f,p.Health.Max});return p.Health.Current;')|Out-Null
    Await "$peer health setup replicated" {(Near (Player $h $peer).hp $value 0.12) -and (Near (Player $c $peer).hp $value 0.12)}
}
function Damage($from,$to,$amount,$tags='BasicAttack'){
    $target=$ids[$to];$number=F $amount
    $tagCode=(@($tags -split '\|'|ForEach-Object {'SSW.DamageTag.'+$_.Trim()}) -join '|')
    $source=if($from -eq 'none' -or [string]::IsNullOrEmpty($from)){'null'}else{'System.Linq.Enumerable.First(game.Players,x=>x.OwnerClientId=='+$ids[$from]+'UL).Health'}
    return Eval ('var game=SSW.NetGame.Current;var target=System.Linq.Enumerable.First(game.Players,x=>x.OwnerClientId=='+$target+'UL);SSW.Health source='+$source+';float before=source!=null?source.Current:0f;float beforeTarget=target.Health.Current;var hit=SSW.CombatDamage.Deal(source,target.Health,'+$number+'f,'+$tagCode+');return new{applied=hit.AppliedAmount,lethal=hit.WasLethal,deferred=hit.WasDeferred,accepted=hit.WasAccepted,blocked=hit.WasBlocked,sourceBefore=before,sourceHp=source!=null?source.Current:0f,targetBefore=beforeTarget,targetHp=target.Health.Current};')
}
function Warp($peer,$x,$y){Send host warp $(if($peer -eq 'host'){0}else{1}) $x $y}
function Move-World($peer,$x){Send $peer move 0 ($x/(Player (Read $peer) $peer).side) 0}
function Advance($label,$seconds){$targetTime=(Read host).time+$seconds;Await $label {$h.time -ge $targetTime} ($seconds+8)}
function Reset-Pair([string[]]$HostAugments=@(),[string[]]$ClientAugments=@(),[string]$HostJob='Swordsman',[string]$ClientJob='Swordsman',[float]$HostX=1000,[float]$ClientX=1002,[float]$HostY=3,[float]$ClientY=3){
    $script:scenario++
    $ha=(@($HostAugments|ForEach-Object {Card $_}) -join ',')
    $ca=(@($ClientAugments|ForEach-Object {Card $_}) -join ',')
    if($HostJob -notmatch '^(Witch|Magician|Swordsman|Assassin|Gambler|Gunner)$' -or $ClientJob -notmatch '^(Witch|Magician|Swordsman|Assassin|Gambler|Gunner)$'){throw 'Unsupported scenario job.'}
    foreach($peer in @('host','client')){Send $peer move;Send $peer cancel}
    $code='var game=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var prefab=(SSW.NetPlayer)typeof(SSW.NetGame).GetField("_playerPrefab",flags).GetValue(game);var old=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(game.Players,p=>new{p.OwnerClientId,p.Side,p.Info}));typeof(SSW.NetGame).GetMethod("ClearShots",flags).Invoke(game,null);foreach(var p in System.Linq.Enumerable.ToArray(game.Players))p.NetworkObject.Despawn();var made=new System.Collections.Generic.List<object>();foreach(var saved in old){bool host=saved.OwnerClientId==@HOST@UL;var player=UnityEngine.Object.Instantiate(prefab,new UnityEngine.Vector3(host?@HX@f:@CX@f,host?@HY@f:@CY@f,0f),UnityEngine.Quaternion.identity);player.Init(host?SSW.PlayerJob.@HJ@:SSW.PlayerJob.@CJ@,saved.Side,saved.Info);player.NetworkObject.SpawnAsPlayerObject(saved.OwnerClientId,true);typeof(SSW.BuffHealth).GetField("_baseMax",flags).SetValue(player.GetComponent<SSW.BuffHealth>(),100f);typeof(SSW.Health).GetMethod("SetMax",flags).Invoke(player.Health,new object[]{100f});player.Draft.Restore(host?new int[]{@HA@}:new int[]{@CA@});made.Add(new{id=player.OwnerClientId,objectId=player.NetworkObjectId,job=player.Job.ToString(),name=player.Info.Name.ToString(),owned=player.Draft.Owned});}UnityEngine.Physics2D.IgnoreCollision(game.Players[0].Collider,game.Players[1].Collider);typeof(SSW.BattleMap).GetField("_fallY",flags).SetValue(game.Arena.Map,-100000f);return made;'
    foreach($entry in @{HOST=$ids.host;HX=(F $HostX);CX=(F $ClientX);HY=(F $HostY);CY=(F $ClientY);HJ=$HostJob;CJ=$ClientJob;HA=$ha;CA=$ca}.GetEnumerator()){$code=$code.Replace('@'+$entry.Key+'@',[string]$entry.Value)}
    $script:pair=@(Eval $code)
    Save ("Pair-"+$scenario) $pair
    Await "scenario $scenario fresh player objects replicated" {
        if(@($h.players).Count -ne 2 -or @($c.players).Count -ne 2){return $false}
        foreach($expected in $pair){
            $a=@($h.players|Where-Object {$_.id -eq $expected.id})[0];$b=@($c.players|Where-Object {$_.id -eq $expected.id})[0]
            if($a.objectId -ne $expected.objectId -or $b.objectId -ne $expected.objectId -or $a.job -ne $expected.job -or $b.job -ne $expected.job){return $false}
            if(@($a.augments).Count -ne @($expected.owned).Count -or @($b.augments).Count -ne @($expected.owned).Count){return $false}
        }
        return $true
    }
    if($HostY -le 3 -and $ClientY -le 3){Await "scenario $scenario both players landed" {(Player $h host).grounded -and (Player $h client).grounded -and (Player $c host).grounded -and (Player $c client).grounded}}
    return $pair
}
function Sword-Damage($peer,$value){
    $id=$ids[$peer];$number=F $value
    Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$id+'UL);typeof(SSW.SwordCast).GetField("_damage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p.Cast.Weapon,'+$number+'f);return true;')|Out-Null
}
try{
    $before=Eval 'if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("An existing play session is active");var scenes=new System.Collections.Generic.List<object>();for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);scenes.Add(new{path=s.path,dirty=s.isDirty});}return scenes;'
    Save EditorBefore $before
    $play=unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $play.success){throw ($play|ConvertTo-Json -Depth 8)}
    $editorStarted=$true
    $until=[DateTime]::UtcNow.AddSeconds(45)
    do{
        try{$scene=Eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'}catch{$scene=''}
        if($scene -eq 'MainMenu'){break}
        if([DateTime]::UtcNow -ge $until){throw 'MainMenu startup timeout'}
        Start-Sleep -Milliseconds 300
    }while($true)
    Eval ('var game=SSW.NetGame.GetOrCreate();game.SetLocalJob(SSW.PlayerJob.Swordsman);game.SetProfile(SSW.Fighter.Create("CommonHost"));game.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");game.gameObject.AddComponent<SSW.MenuProbe>().Init("'+$root+'/host");return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-profile','common-client','--net-name','CommonClient','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','960','-screen-height','540') -WindowStyle Hidden -PassThru
    Await 'both menus' {(Menu host).scene -eq 'MainMenu' -and (Menu client).scene -eq 'MainMenu'} 50
    foreach($peer in @('host','client')){MenuCmd $peer job '' '' 3;Send $peer fps 60;Click $peer 'Button_Play'}
    Click host 'Button_방 만들기'
    MenuCmd host text '방 이름Input' '공용 증강 검증'
    MenuCmd host toggle '방 목록에서 숨기기Toggle' '' 1
    Click host '방 만들기Button'
    Await 'private Relay room' {(Menu host).joined -and (Menu host).code.Length -gt 0 -and -not(Menu host).busy} 90
    Click client 'Button_방 들어가기'
    MenuCmd client text '참가 코드Input' (Menu host).code
    Click client '코드 참가Button'
    Await 'Relay pair ready' {(Menu host).canStart -and (Menu client).joined -and (Menu host).count -eq 2} 90
    Click host '게임 시작Button'
    Await 'initial draft' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 45
    PickAll
    $ids.host=@($h.players|Where-Object owner)[0].id
    $ids.client=@($h.players|Where-Object {-not $_.owner})[0].id
    $catalog=Eval 'var deck=UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");var cards=new System.Collections.Generic.List<object>();for(int i=0;i<deck.Count;i++){var a=deck.At(i);cards.Add(new{id=i,name=a.name,key=a is SSW.CommonAugment c&&System.Enum.IsDefined(typeof(SSW.CommonAugmentType),c.type)?c.type.ToString():a.name,common=deck.IsCommon(a)});}var jobs=new System.Collections.Generic.List<object>();foreach(SSW.PlayerJob j in System.Enum.GetValues(typeof(SSW.PlayerJob)))jobs.Add(new{name=j.ToString(),count=deck.Candidates(j,new int[0],true).Count});return new{cards,jobs,protocol=SSW.NetGame.Protocol};'
    Save Catalog $catalog
    Check (@($catalog.cards|Where-Object common).Count -eq 30) 'RealCommonAugmentPool has 30 network cards'
    Check (@($catalog.jobs).Count -eq 7 -and @($catalog.jobs|Where-Object {$_.count -ne 30}).Count -eq 0) 'all seven enum job entries see the same common pool'
    foreach($peer in @('host','client')){Send $peer wideground 0 1000 0}
    Eval 'typeof(SSW.BattleMap).GetField("_fallY",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(SSW.NetGame.Current.Arena.Map,-100000f);return true;'|Out-Null
    $all=@($catalog.cards|Where-Object common|ForEach-Object key)
    if(-not $SkipCommon){
    if(-not $ResumeMovement){
    Reset-Pair -HostAugments $all -ClientAugments $all -HostX 980 -ClientX 1020|Out-Null
    Await 'all cards and combined body scale synchronized' {@((Player $h host).augments).Count -eq 30 -and @((Player $c client).augments).Count -eq 30 -and (Near (Player $h host).size (Player $c host).size 0.001) -and (Near (Player $h client).size (Player $c client).size 0.001)}
    Check ((Near (Player $h host).max 10) -and (Near (Player $c client).max 10)) 'all thirty cards restore with TenLives maximum on both peers'
    $refs=Eval 'var bad=new System.Collections.Generic.List<string>();foreach(var p in SSW.NetGame.Current.Players)foreach(var type in new[]{typeof(SSW.BuffHealth),typeof(SSW.BuffSkill),typeof(SSW.BuffGuard),typeof(SSW.BuffArea),typeof(SSW.BuffFx),typeof(SSW.NetBuff)}){var c=p.GetComponent(type);if(c==null){bad.Add(type.Name);continue;}var so=new UnityEditor.SerializedObject(c);var prop=so.GetIterator();while(prop.NextVisible(true))if(prop.propertyType==UnityEditor.SerializedPropertyType.ObjectReference&&prop.objectReferenceValue==null)bad.Add(type.Name+"."+prop.propertyPath);}return bad;'
    Check (@($refs).Count -eq 0) 'spawned common modules retain every serialized reference'
    Reset-Pair -ClientAugments @('Giant','GlassCannon','Versatile')|Out-Null
    Await 'stat maximum and scale synchronized' {(Near (Player $h client).max 119.35) -and (Near (Player $c client).max 119.35) -and (Near (Player $c client).size 0.99 0.001)}
    Send client press 1 -1 0
    Await 'client melee applies combined damage' {(Near (Player $h host).hp 40.6) -and (Near (Player $c host).hp 40.6)}
    Check $true 'client sword attack applies GlassCannon and Versatile on server'
    $hp=(Player $h client).hp
    Send client damage 90
    Advance 'client unauthorized damage observation' 0.35
    Check ((Near (Player $h client).hp $hp) -and (Near (Player $c client).hp $hp)) 'client cannot author health changes'
    Reset-Pair -ClientAugments @('Vampire','Confidence','Berserker')|Out-Null
    Sword-Damage client 10
    Set-Hp client 60
    Send client press 1 -1 0
    Await 'vampire and confidence from real owner hit' {(Near (Player $h client).hp 68.8) -and (Near (Player $c client).hp 68.8) -and (Player $c client).speed -gt 13}
    Check ((Near (Player $h host).hp 84) -and (Near (Player $h client).speed 13.195 0.03)) 'Berserker damage and Vampire healing combine with Confidence speed'
    Await 'confidence expires on both peers' {(Near (Player $h client).speed 10.15 0.03) -and (Near (Player $c client).speed 10.15 0.03)}
    Check $true 'temporary confidence expires without clearing Berserker'
    Reset-Pair -ClientAugments @('Multiscale')|Out-Null
    $first=Damage host client 20
    $second=Damage host client 20
    Check ((Near $first.applied 10) -and (Near $second.applied 20)) 'Multiscale reduces only the full health hit'
    Await 'multiscale health synchronized' {(Near (Player $h client).hp 70) -and (Near (Player $c client).hp 70)}
    Grant client Regeneration|Out-Null
    Await 'regeneration reaches both peers' {(Player $h client).hp -ge 71 -and (Player $c client).hp -ge 71}
    Check $true 'Regeneration heals on the server and replicates'
    Reset-Pair -ClientAugments @('TenLives')|Out-Null
    $fixed=Damage host client 40
    Check ((Near $fixed.applied 1) -and (Near $fixed.targetHp 9)) 'TenLives converts a normal hit into one health'
    Reset-Pair -ClientAugments @('Phoenix')|Out-Null
    Send host remote 1000
    Await 'phoenix lock observed on both peers' {(Player $h client).frozen -and (Player $c client).frozen}
    Send client guard
    Send host remote 20
    Check (-not(Player (Read host) client).guarding) 'frozen player cannot start guarding'
    Await 'phoenix fully revived' {(Near (Player $h client).hp 75) -and (Near (Player $c client).hp 75)}
    Await 'phoenix lock ends' {-not(Player $h client).frozen -and -not(Player $c client).frozen}
    Check $true 'Phoenix revives once and lock releases on both peers'
    Reset-Pair -ClientAugments @('DeathWaltz')|Out-Null
    $deferred=Damage host client 50
    Check ((Near $deferred.applied 0) -and (Near $deferred.targetHp 130)) 'DeathWaltz initially defers the full hit'
    Await 'death waltz finishes five ticks' {(Near (Player $h client).hp 80) -and (Near (Player $c client).hp 80)}
    Check $true 'DeathWaltz applies original total once on both peers'
    Reset-Pair -HostAugments @('BestOffense') -ClientAugments @('DeathWaltz','CounterAttack')|Out-Null
    $reserved=Damage host client 50
    Check ($reserved.deferred -and $reserved.accepted -and -not $reserved.blocked -and (Near $reserved.sourceHp 100)) 'DeathWaltz accepted damage does not trigger CounterAttack without a guard'
    Send host guard
    Await 'old deferred damage ticks after new empowerment' {(Player $h client).hp -le 120 -and (Player $c client).hp -le 120}
    $empowered=Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.host+'UL);return p.Guard.Empowered;')
    Check $empowered 'old DeathWaltz ticks cannot consume newly acquired BestOffense empowerment'
    Reset-Pair -HostAugments @('DeathWaltz') -ClientAugments @('NomNom')|Out-Null
    Eval ('var game=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var target=System.Linq.Enumerable.First(game.Players,x=>x.OwnerClientId=='+$ids.host+'UL);var owner=System.Linq.Enumerable.First(game.Players,x=>x.OwnerClientId=='+$ids.client+'UL);((System.Collections.IList)typeof(SSW.BuffHealth).GetField("_pending",flags).GetValue(target.GetComponent<SSW.BuffHealth>())).Clear();typeof(SSW.BuffSkill).GetField("_nom",flags).SetValue(owner.GetComponent<SSW.BuffSkill>(),0f);var set=typeof(SSW.Health).GetMethod("ApplyNetworkState",flags);set.Invoke(target.Health,new object[]{target.Health.Max,target.Health.Max});set.Invoke(owner.Health,new object[]{40f,owner.Health.Max});return true;')|Out-Null
    Await 'NomNom heals through deferred damage' {(Player $h host).hp -lt 130 -and (Player $h client).hp -gt 40 -and (Player $c client).hp -gt 40}
    $gain=(Player $h client).hp-40
    $loss=130-(Player $h host).hp
    Check ((Near $gain $loss 0.12)) 'NomNom deferred ticks retain their full self-heal credit'
    Reset-Pair -ClientAugments @('DevilsDeal') -HostX 990 -ClientX 1010|Out-Null
    Await 'devil drain begins' {(Player $h client).hp -lt 100 -and (Player $c client).hp -lt 100}
    $immune=Damage none client 25 Environment
    Check ((Near $immune.applied 0)) 'DevilsDeal blocks terrain damage'
    Set-Hp client 10
    Advance 'devil drain threshold observation' 1.15
    Check ((Near (Player $h client).hp 10) -and (Near (Player $c client).hp 10)) 'DevilsDeal stops draining at ten percent health'
    $steal=Damage client host 20
    Check ((Near ($steal.sourceHp-$steal.sourceBefore) 5)) 'DevilsDeal heals one quarter of actual damage'
    Reset-Pair -ClientAugments @('HealField','Invincible','GuardMastery','Recharge') -HostX 990 -ClientX 1010|Out-Null
    Set-Hp client 40
    $guardStart=(Read host).time
    Send client guard
    Await 'extended guard starts' {(Player $h client).guarding -and (Player $c client).guarding}
    Await 'first healing field' {(Player $h client).hp -ge 60 -and (Player $c client).hp -ge 60}
    Await 'automatic second guard' {$h.time -gt $guardStart+1.8 -and (Player $h client).guarding -and (Player $c client).guarding}
    Await 'second healing field' {(Player $h client).hp -ge 80 -and (Player $c client).hp -ge 80}
    Check $true 'Invincible duration and Recharge create two replicated healing fields'
    $cooldown=Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.client+'UL);return p.Guard.Cooldown;')
    Check ((Near $cooldown 2.94 0.001)) 'GuardMastery and Recharge cooldown multipliers compose'
    Reset-Pair -ClientAugments @('CounterAttack','Invincible')|Out-Null
    Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.client+'UL);typeof(SSW.BuffGuard).GetField("_barrierContinue",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p.Guard,3f);return true;')|Out-Null
    Send client guard
    Await 'counter guard acknowledged' {(Player $h client).guarding -and (Player $c client).guarding}
    $counter=Damage host client 20
    Check ((Near $counter.applied 0) -and (Near $counter.sourceHp 88)) 'CounterAttack reflects sixty percent of a blocked attack'
    Reset-Pair -ClientAugments @('BestOffense')|Out-Null
    Send client guard
    Await 'best offense guard acknowledged' {(Player $h client).guarding -and (Player $c client).guarding}
    $boost=Damage client host 10
    $plain=Damage client host 10
    Check ((Near $boost.applied 30) -and (Near $plain.applied 10)) 'BestOffense amplifies only the first subsequent attack'
    Reset-Pair -ClientAugments @('IceAge','Nuclear')|Out-Null
    Send client guard
    Await 'IceAge freeze on both peers' {(Player $h host).frozen -and (Player $c host).frozen}
    Await 'nuclear damage and post-freeze slow' {(Near (Player $h host).hp 80) -and (Near (Player $c host).hp 80) -and (Near (Player $h host).speed 2.45 0.05) -and (Near (Player $c host).speed 2.45 0.05)}
    Check $true 'IceAge freeze slow and Nuclear damage replicate'
    Reset-Pair -ClientAugments @('Blink')|Out-Null
    $beforeBlink=(Player $h client).position.x
    Send client guard
    Await 'guard blink finished on both peers' {[Math]::Abs((Player $h client).position.x-$beforeBlink) -gt 3.7 -and (Near (Player $h client).position.x (Player $c client).position.x 0.25)}
    Check ([Math]::Abs((Player $h client).position.x-$beforeBlink) -lt 4.15) 'guard Blink travels four units without overshoot'
    Reset-Pair -ClientAugments @('LeapBomb')|Out-Null
    Send client guard
    Await 'leap bombs visible on both peers' {(Player $h client).areas -gt 0 -and (Player $c client).areaViews -gt 0 -and (Player $h client).velocity.y -gt 2}
    Await 'leap bomb views removed after explosion' {(Player $h client).areas -eq 0 -and (Player $c client).areaViews -eq 0}
    Check $true 'LeapBomb spawns jumps and cleans up replicated bomb views'
    Reset-Pair -ClientAugments @('Magnet') -ClientX 1004|Out-Null
    $startX=(Player $h host).position.x
    Await 'magnet pulls opponent' {(Player $h host).position.x -gt $startX+0.04 -and (Player $c host).position.x -gt $startX+0.02}
    Check $true 'Magnet force affects both views'
    Reset-Pair -HostAugments @('SwiftApproach') -ClientAugments @('SlowAura') -ClientX 1004|Out-Null
    Move-World host 1
    Await 'swift and slow movement compose' {(Near (Player $h host).speed 7.28 0.06) -and (Near (Player $c host).speed 7.28 0.06)}
    Send host move
    Check $true 'SwiftApproach speed and SlowAura combine in movement snapshots'
    Reset-Pair -ClientAugments @('Slam') -ClientX 1001 -ClientY 9|Out-Null
    Await 'slam landing damages nearby enemy' {(Player $h client).grounded -and (Player $h host).hp -lt 100 -and (Near (Player $h host).hp (Player $c host).hp 0.12)}
    Check ((Player $h host).hp -ge 60) 'Slam applies bounded landing damage on both peers'
    Reset-Pair -ClientAugments @('Minefield') -ClientX 1005|Out-Null
    Await 'mine production replicated' {(Player $h client).areas -ge 1 -and (Player $c client).areaViews -ge 1}
    $minePoint=Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.client+'UL);var point=p.GetComponent<SSW.BuffArea>().Position(0);return new{x=point.x,y=point.y};')
    Warp host $minePoint.x 1
    Await 'mine explosion damages entrant' {(Player $h host).hp -lt 100 -and (Player $c host).hp -lt 100}
    Check $true 'Minefield creates authoritative mine hits and synchronized views'
    }
    Reset-Pair -ClientAugments @('ShrinkEngine','DoubleJump','Versatile') -HostX 990 -ClientX 1010|Out-Null
    Await 'shrink and movement multipliers synchronized' {(Near (Player $h client).size 0.75 0.001) -and (Near (Player $c client).size 0.75 0.001) -and (Near (Player $c client).speed 9.625 0.03)}
    Send client jump
    Await 'first owner jump' {(Player $h client).velocity.y -gt 4 -and (Player $c client).velocity.y -gt 4}
    Await 'first jump descending before air jump' {(Player $h client).velocity.y -lt 0 -and (Player $c client).velocity.y -lt 0 -and -not(Player $h client).grounded}
    Send client jump
    Await 'second owner jump' {(Player $h client).velocity.y -gt 2 -and (Player $c client).velocity.y -gt 2}
    $air=Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.client+'UL);var s=(SSW.MotionState)typeof(SSW.MotionView).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(p.Drive);return new{used=s.AirUsed,jump=s.Jump};')
    Check ($air.used -eq 1 -and $air.jump -eq 2) 'second jump consumes exactly one server air jump charge'
    Check $true 'common air jump survives owner prediction and server reconciliation'
    foreach($peer in @('host','client')){Send $peer lag 80 20 2}
    Reset-Pair -ClientAugments @('DoubleJump','Invincible') -HostX 990 -ClientX 1010|Out-Null
    Send client metrics
    $moving=(Player $h client).position.x
    Move-World client -1
    Await 'impaired owner movement' {(Player $h client).position.x -lt $moving-0.8 -and (Player $c client).position.x -lt $moving-0.8}
    Send client move
    $script:ownerRise=$false;$script:serverRise=$false
    Send client jump
    Await 'impaired owner jump' {if((Player $c client).velocity.y -gt 2){$script:ownerRise=$true};if((Player $h client).velocity.y -gt 2){$script:serverRise=$true};return $ownerRise -and $serverRise}
    Send client guard
    Await 'impaired owner guard' {(Player $h client).guarding -and (Player $c client).guarding}
    Check $true 'owner movement jump and guard work with 80ms delay 20ms jitter and 2 percent loss'
    foreach($peer in @('host','client')){Send $peer lag 0 0 0}
    Reset-Pair -HostJob Magician -ClientJob Magician -HostX 990 -ClientX 1010|Out-Null
    Send client press 1 -1 0
    Await 'magician begins rolling' {(Player $h client).charging -and (Player $c client).charging}
    Eval ('System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.client+'UL).Drive.Freeze(3f);return true;')|Out-Null
    Await 'magician roll interrupted by freeze' {(Player $h client).frozen -and (Player $c client).frozen -and -not(Player $h client).charging -and -not(Player $c client).charging}
    Send client release 0 -1 0
    Await 'magician thaws' {-not(Player $h client).frozen -and -not(Player $c client).frozen}
    Check (-not(Player $h client).charging -and -not(Player $c client).charging) 'release during freeze cannot leave magician charging'
    Send client press 1 -1 0
    Await 'magician can roll again' {(Player $h client).charging -and (Player $c client).charging}
    Send client release 0 -1 0
    Await 'magician fires after thaw' {(Player $h client).fired -gt 0 -and -not(Player $h client).charging -and -not(Player $c client).charging}
    Check $true 'magician resumes normal press release after freeze'
    Reset-Pair -HostAugments $all -ClientAugments $all -HostX 980 -ClientX 1020|Out-Null
    Await 'round fixture has mines to clear' {(Player $h host).areas -gt 0 -and (Player $c host).areaViews -gt 0}
    $oldHost=(Player $h host).objectId;$oldClient=(Player $h client).objectId;$oldMap=$h.mapObject
    $firstDeath=Damage none client 100000 'Environment|IgnoreDefense'
    Check (-not $firstDeath.lethal) 'fresh restored Phoenix still has its revive'
    Advance 'phoenix invulnerability before round end' 2.7
    $lastDeath=Damage none client 100000 'Environment|IgnoreDefense'
    Check ($lastDeath.lethal) 'second lethal hit ends the round after Phoenix was spent'
    Await 'fresh next round actors and map' {$h.mapObject -ne $oldMap -and $c.mapObject -eq $h.mapObject -and (Player $h host).objectId -ne $oldHost -and (Player $c host).objectId -eq (Player $h host).objectId -and (Player $h client).objectId -ne $oldClient -and (Player $c client).objectId -eq (Player $h client).objectId} 25
    Check (@((Player $h host).augments).Count -eq 30 -and @((Player $c client).augments).Count -eq 30) 'all thirty owned cards survive the real round reset'
    Await 'old area views removed from new players' {(Player $h host).areas -eq 0 -and (Player $c host).areaViews -eq 0 -and (Player $h client).areas -eq 0 -and (Player $c client).areaViews -eq 0}
    Check $true 'round reset cleans previous mines and area views'
    Await 'next round enters play' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 20
    Eval 'typeof(SSW.BattleMap).GetField("_fallY",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(SSW.NetGame.Current.Arena.Map,-100000f);return true;'|Out-Null
    }
    if(-not $SkipGun -and (Test-Path -LiteralPath "$PSScriptRoot/Gun.ps1")){. "$PSScriptRoot/Gun.ps1"}
    Save Result @{checks=$checks.Count;host=$h;client=$c;build=$Build;protocol=$catalog.protocol;scenarios=$scenario}
    Write-Output "PASS $($checks.Count) common Relay checks"
}finally{
    if($editorStarted){
        foreach($peer in @('client','host')){try{if((Read $peer).listening){Send $peer exit}}catch{Write-Warning $_.Exception.Message}}
        try{Send client quit}catch{}
        if($clientProcess -and -not $clientProcess.WaitForExit(5000)){Stop-Process -Id $clientProcess.Id}
        unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
        try{
            $after=Eval 'var scenes=new System.Collections.Generic.List<object>();for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);scenes.Add(new{path=s.path,dirty=s.isDirty});}return scenes;'
            Save EditorAfter $after
            if(($before|ConvertTo-Json -Compress) -ne ($after|ConvertTo-Json -Compress)){Write-Warning 'Editor scene state differs; inspect EditorBefore.json and EditorAfter.json.'}
        }catch{Write-Warning $_.Exception.Message}
    }
}
