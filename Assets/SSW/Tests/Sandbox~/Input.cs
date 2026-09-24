var game = SSW.NetGame.Current;
string output = "D:/unity_project/Mushrooms/Logs/Sandbox/input.txt";
System.IO.File.WriteAllText(output, "");
void Check(bool ok, string text)
{
    System.IO.File.AppendAllText(output, (ok ? "PASS " : "FAIL ") + text + "\n");
    if (!ok) throw new System.InvalidOperationException(text);
}
void Mouse(ushort buttons)
{
    var point = game.Arena.View.WorldToScreenPoint(game.Local.View.position + UnityEngine.Vector3.right * 3f);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,
        new UnityEngine.InputSystem.LowLevel.MouseState { position = point, buttons = buttons });
}
System.Collections.IEnumerator Run()
{
    yield return new UnityEngine.WaitForSecondsRealtime(0.3f);
    var practice = game.Practice;
    var player = game.Local;
    Check(player.Job == SSW.PlayerJob.Swordsman, "sword selected through menu");
    player.Drive.Teleport(new UnityEngine.Vector2(-6.6f, -3.16f));
    practice.Target.Drive.Teleport(new UnityEngine.Vector2(-5.1f, -3.16f));
    yield return new UnityEngine.WaitForSecondsRealtime(0.5f);
    float hp = practice.Target.Health.Current;
    Mouse(1);
    yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
    Mouse(0);
    Check(practice.Target.Health.Current < hp, "left mouse attacks through PlayerInput");
    var keyboard = UnityEngine.InputSystem.Keyboard.current;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E));
    yield return new UnityEngine.WaitForSecondsRealtime(0.04f);
    Check(player.GetComponent<SSW.SwordCast>().Parrying, "E activates parry");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    float x = player.Body.position.x;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.LeftShift));
    yield return new UnityEngine.WaitForSecondsRealtime(0.15f);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    Check(player.Body.position.x > x + 1.5f, "Left Shift activates dash");
    practice.ResetMap();
    yield return null;
    var target = practice.Target;
    target.Health.TakeDamage(10f);
    yield return new UnityEngine.WaitForSecondsRealtime(2.15f);
    Check(target.Health.Current == target.Health.Max, "dummy heals after idle delay");
    target.Health.TakeDamage(10f);
    var pause = UnityEngine.Object.FindFirstObjectByType<SSW.PauseMenu>();
    pause.Open();
    yield return new UnityEngine.WaitForSecondsRealtime(2.15f);
    Check(target.Health.Current < target.Health.Max, "dummy healing pauses");
    pause.Resume();
    yield return new UnityEngine.WaitForSecondsRealtime(2.1f);
    Check(target.Health.Current == target.Health.Max, "dummy healing resumes");
    target.Health.TakeDamage(10f);
    practice.ResetMap();
    yield return new UnityEngine.WaitForSecondsRealtime(2.2f);
    Check(practice.Target.Health.Current == practice.Target.Health.Max, "reset cleans old healing callback");
    SSW.MapTravel.LoadScene("MainMenu");
    yield return new UnityEngine.WaitForSecondsRealtime(0.8f);
    Check(!game.Connected && game.Practice == null, "input test session released");
    System.IO.File.AppendAllText(output, "DONE\n");
}
game.StartCoroutine(Run());
return output;
