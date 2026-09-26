$cases=[Collections.Generic.List[object]]::new()
$style=Eval ('var p=SSW.NetGame.Current.Players[0];p.Health.TakeDamage(7.5f);p.Health.Heal(7.5f);var values=new System.Collections.Generic.List<object>();foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()){if(!root.TryGetComponent<SSW.DamageNumberDisplay>(out var popup))continue;var text=popup.GetComponentInChildren<UnityEngine.UI.Text>();if(text.color.g>text.color.r && text.text=="7.5")values.Add(new{text=text.text,color=text.color.ToString()});}if(values.Count!=1)throw new System.InvalidOperationException("Heal must show green 7.5 without prefix");UnityEngine.ScreenCapture.CaptureScreenshot("'+$root+'/HealStyle.png");return values;')
$style|ConvertTo-Json|Set-Content "$root/HealStyle.json"
foreach($mode in $EdgeModes){
    for($mapIndex=0;$mapIndex -lt 14;$mapIndex++){
        $map=Map $mapIndex
        foreach($normal in @(1,-1)){
            Send host move
            Send client move
            $setup=@'
var g=SSW.NetGame.Current;
UnityEngine.Physics2D.SyncTransforms();
foreach(var effect in g.Arena.Map.GetComponentsInChildren<UnityEngine.ParticleSystem>())effect.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
var entries=new UnityEditor.SerializedObject(g.Arena.Map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");
UnityEngine.Collider2D wall=null;
float damage=0f;
for(int i=0;i<entries.arraySize;i++)
{
    var edge=entries.GetArrayElementAtIndex(i);
    if(edge.FindPropertyRelative("Force").vector2Value.x*@NORMAL@<=0f)continue;
    wall=(UnityEngine.Collider2D)edge.FindPropertyRelative("Shape").objectReferenceValue;
    damage=edge.FindPropertyRelative("Damage").floatValue;
}
var result=new System.Collections.Generic.List<object>();
foreach(var p in g.Players)
{
    var state=p.Cast.Weapon.Status;
    state.Skill=0;
    typeof(SSW.JobCast).GetProperty("State",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p.Cast.Weapon,state);
    float surface=@NORMAL@>0?wall.bounds.max.x:wall.bounds.min.x;
    float x=surface+@NORMAL@*(p.Collider.bounds.extents.x+@DISTANCE@f);
    float y=wall.bounds.max.y-(p.IsOwner?2.5f:5f);
    p.Drive.Teleport(new UnityEngine.Vector2(x,y));
    result.Add(new{id=p.OwnerClientId,side=p.Side,hp=p.Health.Current,start=x,edge=surface,damage,wall=wall.name,center=wall.bounds.center.ToString(),size=wall.bounds.size.ToString()});
}
return result;
'@
            $distance='0.8'
            $actors=Eval $setup.Replace('@NORMAL@',"($normal)").Replace('@DISTANCE@',$distance)
            Start-Sleep -Milliseconds 200
            foreach($peer in @('host','client')){
                $actor=(Read $peer).players|Where-Object owner
                $seq[$peer]++
                $op=if($mode -eq 'dash'){'dashout'}else{'move'}
                $x=if($mode -eq 'dash'){-1*$normal}else{-1*$normal*$actor.side}
                [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=$seq[$peer];op=$op;value=1;x=$x;y=0}|ConvertTo-Json -Compress))
            }
            $observed=@{}
            foreach($peer in @('host','client')){foreach($actor in $actors){$observed["$peer-$($actor.id)"]=@{hit=$false;inward=$false;peak=0.0}}}
            $until=[DateTime]::UtcNow.AddSeconds(3)
            do{
                foreach($peer in @('host','client')){
                    $snapshot=Read $peer
                    foreach($actor in $actors){
                        $p=$snapshot.players|Where-Object id -eq $actor.id
                        if(-not $p){continue}
                        $entry=$observed["$peer-$($actor.id)"]
                        if($p.hp -le $actor.hp-$actor.damage){$entry.hit=$true}
                        if($entry.hit){
                            $speed=$p.velocity.x*$normal
                            $entry.peak=[Math]::Max($entry.peak,$speed)
                            if(($speed -gt 1 -and ($p.position.x-$actor.edge)*$normal -gt 0.55) -or ($p.position.x-$actor.start)*$normal -gt 0.1){$entry.inward=$true}
                        }
                    }
                }
                if(@($observed.Values|Where-Object {-not $_.hit -or -not $_.inward}).Count -eq 0){break}
                Start-Sleep -Milliseconds 25
            }while([DateTime]::UtcNow -lt $until)
            $h=Read host;$c=Read client
            $cases.Add(@{map=$map.title;mode=$mode;normal=$normal;actors=$actors;observed=$observed;host=$h;client=$c})
            $cases|ConvertTo-Json -Depth 15|Set-Content "$root/EdgesMotion.json"
            Check (@($observed.Values|Where-Object {-not $_.hit -or -not $_.inward}).Count -eq 0) "$($map.title) $mode inward=$normal both actors rebound on both peers while holding outward input"
        }
    }
}
Send host move
Send client move
Write-Output "PASS $($checks.Count) movement edge cases: $root"
