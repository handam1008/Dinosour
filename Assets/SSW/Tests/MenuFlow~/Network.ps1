Await 'new round is playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 10
Eval 'var g=SSW.NetGame.Current;foreach(var p in g.Players){p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));p.Drive.Freeze(30f);}return true;'|Out-Null
Send host escape
Await 'host escape menu opens' {$h.menuOpen -and -not $c.menuOpen} 5
$clock=$c.physicsTime
Send client escape
Await 'client escape menu opens' {$h.menuOpen -and $c.menuOpen -and $c.physicsTime -gt $clock+0.1} 5
Check ($h.timeScale -eq 1 -and $c.timeScale -eq 1) 'multiplayer Escape does not pause either simulation'
$ui=Eval @'
var g=SSW.NetGame.Current;
var menu=g.GetComponentInChildren<SSW.MatchUI>(true);
if(menu==null)menu=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SSW.MatchUI>(true)).Single();
var data=new UnityEditor.SerializedObject(menu);
var volume=(SSW.VolumePanel)data.FindProperty("_volume").objectReferenceValue;
var sliders=volume.GetComponentsInChildren<UnityEngine.UI.Slider>();
var audio=SSW.GameAudio.Current;
float bgm=audio.BgmVolume,sfx=audio.SfxVolume;
sliders[0].value=0.21f;sliders[1].value=0.43f;
bool applied=Mathf.Abs(audio.BgmVolume-0.21f)<0.001f&&Mathf.Abs(audio.SfxVolume-0.43f)<0.001f;
sliders[0].value=bgm;sliders[1].value=sfx;
return new{applied,blocked=(bool)typeof(SSW.NetPlayer).GetField("_blocked",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g.Local),labels=menu.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text).ToArray()};
'@
Check ($ui.applied -and $ui.blocked) 'multiplayer menu applies separate volumes and blocks the local input'
Send host capture
Send client capture
Send host resume
Send client resume
Await 'both continue' {-not $h.menuOpen -and -not $c.menuOpen} 5
Check ($h.phase -eq 'Playing' -and $c.phase -eq 'Playing') 'both players continue the current round'
Send client escape
Await 'client menu reopened' {$c.menuOpen} 5
Send client surrender
Await 'client leaves once and host receives result' {$c.scene -eq 'MainMenu' -and -not $c.listening -and $h.phase -eq 'Finished' -and $h.menuOpen} 12
Check ($h.winner -eq $h.first -and -not $h.canResume) 'client exit completes the match for the remaining host'
Send host surrender
Await 'both return to main menu' {$h.scene -eq 'MainMenu' -and $c.scene -eq 'MainMenu' -and -not $h.listening -and -not $c.listening} 12
Check $true 'one exit click returns each peer to main menu and stops networking'
Send host start 1 0 30716
Send client start 0 0 30716
Await 'new match reaches draft' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 45
Send host escape
Await 'host menu opens in draft' {$h.menuOpen} 5
Send host surrender
Await 'host leaves and client receives result' {$h.scene -eq 'MainMenu' -and -not $h.listening -and $c.phase -eq 'Finished' -and $c.menuOpen} 12
Check (-not $c.canResume) 'host can leave during draft and remaining client receives a terminal result'
Send client surrender
Await 'client returns after host exit' {$c.scene -eq 'MainMenu' -and -not $c.listening} 12
@{checks=$checks;scope='Editor host and standalone client. ESC input injection, audio callbacks, continue, both leave directions, and reconnect.';host=$h;client=$c}|ConvertTo-Json -Depth 18|Set-Content "$root/MenuResult.json"
Write-Output "PASS menu networking: $root"
