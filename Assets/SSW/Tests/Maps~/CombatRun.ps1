. "$PSScriptRoot/CombatSetup.ps1"
$cases=[Collections.Generic.List[object]]::new()
Send host lag 60 10 0
Send client lag 60 10 0
foreach($entry in $CombatJobs){
    $job=if($entry -eq 'Knife'){'Assassin'}else{$entry}
    $mode=if($entry -eq 'Knife'){'throw'}else{'basic'}
    foreach($peer in @('host','client')){
        Map 11|Out-Null
        CombatSpawn $job
        $setup=CombatAim $peer $mode
        $before=@{host=(Read host);client=(Read client)}
        if($mode -eq 'throw'){Send $peer cycle 1 $setup.ax $setup.ay}
        else{Send $peer press 0 $setup.ax $setup.ay;Send $peer release 0 $setup.ax $setup.ay}
        Await "$entry $peer native attack cuts pin" {$h.mapState.hangs[$setup.index].health -eq 0 -and $c.mapState.hangs[$setup.index].health -eq 0} 5
        foreach($view in @('host','client')){
            $snapshot=if($view -eq 'host'){$h}else{$c}
            $pin=$snapshot.mapState.hangs[$setup.index]
            Check (-not $pin.hit -and -not $pin.face -and -not $pin.rope) "$entry $peer native attack removes pin and rope on $view"
        }
        if($entry -eq 'Knife'){
            Await "$peer knife continues beyond pin" {@($h.shots|Where-Object {$_.type -eq 'bolt' -and $_.caster -eq $setup.objectId -and (($_.position.x-$setup.px)*$setup.ax+($_.position.y-$setup.py)*$setup.ay) -gt 0.3}).Count -gt 0} 3
            Check $true "$peer native thrown knife penetrates the pin and remains in flight"
        }
        $cases.Add(@{entry=$entry;owner=$peer;setup=$setup;before=$before;host=$h;client=$c})
        $cases|ConvertTo-Json -Depth 16|Set-Content "$root/CombatCases.json"
    }
}
@{checks=$checks.Count;control='Only actor gravity is zero in runtime cloned profiles. Map geometry, projectile physics and attack input stay native. Both transport directions simulate 60 ms delay and 10 ms jitter.';host=$h;client=$c}|ConvertTo-Json -Depth 16|Set-Content "$root/CombatResult.json"
Write-Output "PASS $($checks.Count) map attack checks: $root"
