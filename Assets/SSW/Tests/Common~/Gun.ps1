$gunCards=Eval 'var deck=UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");var result=new System.Collections.Generic.List<object>();for(int i=0;i<deck.Count;i++)if(deck.At(i) is SSW.GunnerArgument item)result.Add(new{id=i,key=item.type.ToString()});return result;'
function Gun-Grant($peer,$key){
    $id=@($gunCards|Where-Object {$_.key -eq $key})[0].id
    if($null -eq $id){throw "Missing gun card $key"}
    Send host grant $id $(if($peer -eq 'host'){0}else{1})
    Await "$peer gun card $key replicated" {(Player $h $peer).augments -contains $id -and (Player $c $peer).augments -contains $id}
}
function Gun-Fill($peer,[bool]$charged=$false){
    if($charged){Await "$peer charge clock ready" {(Player $h $peer).processed -ge 165 -and (Player $c $peer).processed -ge 165} 8}
    $loaded=if($charged){'0u'}else{'p.InputSequence'}
    Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids[$peer]+'UL);var field=typeof(SSW.JobCast).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var network=(Unity.Netcode.NetworkVariable<SSW.WeaponState>)field.GetValue(p.Cast.Weapon);var state=network.Value;state.Ammo=9;state.Ready=0;state.Filled=p.InputSequence;state.Reload=p.InputSequence+125;state.Loaded.Clear();for(int i=0;i<9;i++)state.Loaded.Add('+$loaded+');network.Value=state;return true;')|Out-Null
    Await "$peer nine ammo sprites synchronized" {(Player $h $peer).visibleAmmo -eq 9 -and (Player $c $peer).visibleAmmo -eq 9 -and (Player $h $peer).ammo -eq 9 -and (Player $c $peer).ammo -eq 9}
    if($charged){Await "$peer charged ammo sprites synchronized" {(Player $h $peer).chargedAmmo -eq 9 -and (Player $c $peer).chargedAmmo -eq 9}}
}
function Gun-Health{
    Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{5000f});p.Health.Heal(5000f);}return true;'|Out-Null
    Await 'gun fixture health synchronized' {(Near (Player $h host).hp 5000) -and (Near (Player $c client).hp 5000)}
}
function Gun-Shot($peer){
    $target=if($peer -eq 'host'){'client'}else{'host'}
    $a=Player (Read host) $peer;$b=Player (Read host) $target
    $aimY=$b.position.y-0.7*$a.size
    $dx=$b.position.x-$a.position.x;$dy=$aimY-$a.position.y
    Send $peer aimpoint 0 $b.position.x $aimY
    Send $peer cast 1 $dx $dy
}
foreach($peer in @('host','client')){Send $peer lag 0 0 0}
Reset-Pair -HostJob Gunner -ClientJob Gunner -HostX 990 -ClientX 1010|Out-Null
Await 'both gunner pools prewarmed' {(Player $h host).pooledEffects -gt 20 -and (Player $c host).pooledEffects -gt 20 -and (Player $h client).pooledEffects -gt 20 -and (Player $c client).pooledEffects -gt 20}
Await 'automatic ammo fill visible on both peers' {(Player $h host).visibleAmmo -ge 1 -and (Player $c host).visibleAmmo -ge 1 -and (Player $h client).visibleAmmo -ge 1 -and (Player $c client).visibleAmmo -ge 1} 6
Check $true 'host and client gunner have working ammo display and independent effect pools'
foreach($peer in @('host','client')){Gun-Fill $peer $true}
Check $true 'all nine individual loaded ammo indicators become charged on both peers'
Eval ('var game=SSW.NetGame.Current;var p=System.Linq.Enumerable.First(game.Players,x=>x.OwnerClientId=='+$ids.client+'UL);var camera=game.Arena.View;var point=camera.transform.position;var size=camera.orthographicSize;var target=camera.targetTexture;var active=UnityEngine.RenderTexture.active;var texture=UnityEngine.RenderTexture.GetTemporary(1280,720,24);var image=new UnityEngine.Texture2D(1280,720,UnityEngine.TextureFormat.RGB24,false);try{camera.transform.position=new UnityEngine.Vector3(p.View.position.x,p.View.position.y+0.5f,point.z);camera.orthographicSize=2.5f;camera.targetTexture=texture;camera.Render();UnityEngine.RenderTexture.active=texture;image.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes("'+$root+'/GunAmmo.png",image.EncodeToPNG());return true;}finally{camera.targetTexture=target;camera.transform.position=point;camera.orthographicSize=size;UnityEngine.RenderTexture.active=active;UnityEngine.RenderTexture.ReleaseTemporary(texture);UnityEngine.Object.Destroy(image);}')|Out-Null
foreach($peer in @('host','client')){
    Send $peer aimpoint 0 1000 10
    Await "$peer remote aim replicated" {(Player $h $peer).aim.y -gt 0.25 -and (Player $c $peer).aim.y -gt 0.25}
}
Check $true 'remote gun aim follows continuous motion snapshots'
foreach($peer in @('host','client')){
    $args=if($peer -eq 'host'){@{HostJob='Gunner';ClientJob='Swordsman'}}else{@{HostJob='Swordsman';ClientJob='Gunner'}}
    Reset-Pair @args|Out-Null
    Gun-Health
    Gun-Fill $peer
    $other=if($peer -eq 'host'){'client'}else{'host'}
    Gun-Shot $peer
    Await "$peer normal bullet deals fifteen" {(Near (Player $h $other).hp 4985) -and (Near (Player $c $other).hp 4985)}
    Check $true "$peer gunner normal bullet applies exactly fifteen damage once"
    Check ((Player (Read $peer) $peer).visibleAmmo -le 8) "$peer ammo display decreases after actual fire"
    Gun-Fill $peer $true
    Gun-Shot $peer
    Await "$peer charged bullet deals thirty" {(Near (Player $h $other).hp 4955) -and (Near (Player $c $other).hp 4955)}
    Check $true "$peer gunner charged bullet applies exactly thirty damage once"
}
Reset-Pair -ClientJob Gunner|Out-Null
Gun-Health
foreach($key in @('FireBullet','IceBullet','PoisonBullet','LightningBullet','Quest_EvolutionAbility','BeautifulFootStepAbility','ShrinkingDeviceAbility','GravityBullet','AirBullet','ShurikenBullet')){Gun-Grant client $key}
Advance 'original gun perk initial cooldowns' 5.1
Gun-Fill client $true
Eval ('System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.host+'UL).Drive.Freeze(10f);return true;')|Out-Null
Gun-Shot client
Await 'all gun hit effects visible on both peers' {(Player $h client).activeEffects -ge 6 -and (Player $c client).activeEffects -ge 6 -and (Player $h client).progress -eq 1 -and (Player $c client).progress -eq 1}
Check $true 'charged elemental hit effects and quest hit replicate from actual client shot'
Await 'haste and shrinking replicate' {(Player $h client).shrunk -and (Player $c client).shrunk -and (Player $h client).speed -gt 10 -and (Player $c client).speed -gt 10}
Check $true 'original footstep and shrinking procs affect both peer views'
Gun-Shot client
Await 'evolution completes in two charged hits' {(Player $h client).progress -eq 2 -and (Player $c client).progress -eq 2}
Check $true 'evolution completes after two charged hits'
$beforeThird=(Player (Read host) host).hp
Gun-Shot client
Advance 'third bullet reaches target' 0.2
$lightning=Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,x=>x.OwnerClientId=='+$ids.client+'UL);var f=typeof(SSW.GunCast).GetField("_lightningAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);return (float)f.GetValue(p.Cast.Weapon)>UnityEngine.Time.time;')
Check $lightning 'three actual elemental hits trigger lightning cooldown'
$beforeDot=(Read host).players|Where-Object {$_.id -eq $ids.host}|Select-Object -ExpandProperty hp
Await 'poison fire and lightning replicated tick' {(Player $h host).hp -lt $beforeDot-20 -and (Near (Player $h host).hp (Player $c host).hp 0.15)}
Check $true 'poison fire and lightning damage continue and synchronize'
Await 'pooled hit effects return after lifetime' {(Player $h client).activeEffects -eq 0 -and (Player $c client).activeEffects -eq 0} 12
Check $true 'gun trails and hit effects return to both local pools'
Reset-Pair -HostAugments @('DeathWaltz') -ClientJob Gunner|Out-Null
Gun-Grant client IceBullet
Gun-Fill client
Gun-Shot client
Await 'deferred target still receives ice proc' {(Player $h host).speed -lt 6.5 -and (Player $c host).speed -lt 6.5 -and (Player $h client).activeEffects -gt 0 -and (Player $c client).activeEffects -gt 0}
Check $true 'DeathWaltz accepted hit retains gun augment and effect'
Reset-Pair -ClientJob Gunner -HostX 990 -ClientX 1010 -HostY 5 -ClientY 5|Out-Null
Gun-Grant client FireBullet
Gun-Fill client $true
foreach($peer in @('host','client')){Send $peer lag 80 20 2}
Send client trace 81 3
Send host trace 81 3
Send client cast 1 -1 1
Await 'impaired gun preview adopted' {(Player $c client).matches -gt 0} 5
Await 'gun flight trace saved' {(Test-Path -LiteralPath "$root/client.trace.81.json")} 5
$trace=Get-Content -LiteralPath "$root/client.trace.81.json" -Raw|ConvertFrom-Json
$shots=@($trace.frames|ForEach-Object shots|Where-Object {$_.caster -eq (Player $c client).objectId -and -not $_.preview -and -not $_.ending})
Check ($shots.Count -ge 3) 'gun authoritative flight remains visible under delay jitter and loss'
Check (@($shots|Where-Object {$_.gravity.y -lt -15}).Count -gt 0) 'remote gun flight uses original bullet gravity'
Check ((Player $c client).rejected -eq 0 -and $c.castDelay -lt 0.05) 'impaired client gun fires immediately without rejected shot'
Await 'impaired gun trail returns to pool' {(Player $h client).activeEffects -eq 0 -and (Player $c client).activeEffects -eq 0} 8
Check $true 'predicted and authoritative gun trail lifetimes end under impairment'
foreach($peer in @('host','client')){Send $peer lag 0 0 0}
Save GunResult @{host=$h;client=$c;trace=$shots.Count}
