bool original = UnityEngine.Cursor.visible;
var checks = new System.Collections.Generic.List<string>();
try
{
    UnityEngine.Cursor.visible = true;
    var first = new SSW.DraftPointer();
    var second = new SSW.DraftPointer();
    first.Hide();
    if (UnityEngine.Cursor.visible) throw new System.Exception("Native cursor was not hidden");
    checks.Add("PASS Custom cursor hides the native cursor");
    second.Hide();
    first.Dispose();
    if (UnityEngine.Cursor.visible) throw new System.Exception("Old screen restored a cursor owned by the next screen");
    checks.Add("PASS Closing the previous choice does not restore the next choice cursor");
    second.Dispose();
    if (!UnityEngine.Cursor.visible) throw new System.Exception("Native cursor was not restored");
    checks.Add("PASS Closing the choice restores the native cursor");
    UnityEngine.Cursor.visible = false;
    first.Hide();
    first.Dispose();
    if (UnityEngine.Cursor.visible) throw new System.Exception("Original cursor visibility was lost");
    checks.Add("PASS Original cursor visibility is preserved");
    System.IO.File.WriteAllLines("Logs/Skate/PointerChecks.txt", checks);
}
finally { UnityEngine.Cursor.visible = original; }
return "PASS 4 cursor lifecycle checks";
