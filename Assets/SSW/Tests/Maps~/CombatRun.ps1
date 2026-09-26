. "$PSScriptRoot/CombatSetup.ps1"
$cases=[Collections.Generic.List[object]]::new()
Send host lag 60 10 0
Send client lag 60 10 0
foreach($entry in $CombatJobs){
    $knife=$entry -in @('Knife','KnifeZero')
    $job=if($knife){'Assassin'}else{$entry}
    $mode=if($knife){'throw'}else{'basic'}
    foreach($peer in @('host','client')){
        Map 11|Out-Null
        CombatSpawn $job ($entry -eq 'KnifeZero')
        $setup=CombatAim $peer $mode
        $before=@{host=(Read host);client=(Read client)}
        if($mode -eq 'throw'){Send $peer cycle 1 $setup.ax $setup.ay}
        else{Send $peer press 0 $setup.ax $setup.ay;Send $peer release 0 $setup.ax $setup.ay}
        if($entry -ne 'KnifeZero'){
            Await "$entry $peer native attack cuts pin" {$h.mapState.hangs[$setup.index].health -eq 0 -and $c.mapState.hangs[$setup.index].health -eq 0} 5
            foreach($view in @('host','client')){
                $snapshot=if($view -eq 'host'){$h}else{$c}
                $pin=$snapshot.mapState.hangs[$setup.index]
                Check (-not $pin.hit -and -not $pin.face -and -not $pin.rope) "$entry $peer native attack removes pin and rope on $view"
            }
        }
        if($knife){
            Await "$entry $peer knife continues beyond pin on both peers" {
                foreach($snapshot in @($h,$c)){
                    if(@($snapshot.shots|Where-Object {$_.type -eq 'bolt' -and $_.caster -eq $setup.objectId -and (($_.position.x-$setup.px)*$setup.ax+($_.position.y-$setup.py)*$setup.ay) -gt 0.3}).Count -eq 0){return $false}
                }
                return $true
            } 3
            Check $true "$entry $peer native thrown knife passes the pin on both peers"
            if($entry -eq 'KnifeZero'){
                Check ($h.mapState.hangs[$setup.index].health -eq 1 -and $c.mapState.hangs[$setup.index].health -eq 1) "$peer zero-damage knife leaves pin alive without trapping its client view"
            }
        }
        $cases.Add(@{entry=$entry;owner=$peer;setup=$setup;before=$before;host=$h;client=$c})
        $cases|ConvertTo-Json -Depth 16|Set-Content "$root/CombatCases.json"
    }
}
@{checks=$checks.Count;control='Actor gravity is zero in runtime cloned profiles. KnifeZero additionally sets skill damage to zero. Map geometry, projectile physics and attack input stay native. Both transport directions simulate 60 ms delay and 10 ms jitter.';host=$h;client=$c}|ConvertTo-Json -Depth 16|Set-Content "$root/CombatResult.json"
Write-Output "PASS $($checks.Count) map attack checks: $root"
