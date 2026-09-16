var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/UI/MatchMenu.prefab");
var checks = new System.Collections.Generic.List<string>();
foreach (ulong local in new ulong[] { 0, 1 })
foreach (var wins in new[] { new UnityEngine.Vector2Int(0, 0), new UnityEngine.Vector2Int(0, 1), new UnityEngine.Vector2Int(1, 1), new UnityEngine.Vector2Int(2, 1), new UnityEngine.Vector2Int(1, 2) })
{
    var root = UnityEngine.Object.Instantiate(prefab);
    try
    {
        var ui = root.GetComponentInChildren<SSW.RoundUI>(true);
        var state = new SSW.MatchState { First = 0, Second = 1, Set = 1, Round = 3, Phase = SSW.MatchPhase.RoundEnd, FirstWins = (byte)wins.x, SecondWins = (byte)wins.y, FirstMarks = 5, SecondMarks = 2 };
        ui.Show(state, local);
        var data = new UnityEditor.SerializedObject(ui);
        foreach (string side in new[] { "_left", "_right" })
        {
            var lamps = data.FindProperty(side);
            if (lamps.arraySize != 2) throw new System.Exception("Expected two lamps per player");
            int expected = (side == "_left") == (local == 0) ? wins.x : wins.y;
            for (int i = 0; i < 2; i++)
            {
                var lamp = new UnityEditor.SerializedObject(lamps.GetArrayElementAtIndex(i).objectReferenceValue);
                var fill = (UnityEngine.UI.Image)lamp.FindProperty("_fill").objectReferenceValue;
                if (fill.fillAmount != (i < expected ? 1f : 0f)) throw new System.Exception("Win lamp order mismatch");
            }
        }
        checks.Add($"PASS Player {local}: {wins.x}:{wins.y} lights wins in order across two lamps");
        state = SSW.Rounds.NextSet(new SSW.MatchState { First = 0, Second = 1, Set = 1, Round = 3, Phase = SSW.MatchPhase.SetEnd, FirstWins = 2, SecondWins = 1, FirstSets = 1 });
        ui.Show(state, local);
        if (ui.GetComponentsInChildren<UnityEngine.UI.Image>(true).Any(image => image.name == "Fill" && image.fillAmount != 0f)) throw new System.Exception("New set did not clear lamps");
    }
    finally { UnityEngine.Object.DestroyImmediate(root); }
}
checks.Add("PASS New set clears both players' lamps");
System.IO.File.WriteAllLines("Logs/Skate/LampChecks.txt", checks);
return "PASS 11 win lamp checks";
