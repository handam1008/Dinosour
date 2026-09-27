var player = UnityEngine.Resources.Load<SSW.NetPlayer>("Network/Player");
var coin = player.GetComponent<SSW.CoinCast>();
if (coin.Effects == null || !coin.Effects.transform.IsChildOf(player.View)) throw new System.InvalidOperationException("Coin effects must follow the rendered player");
var data = new UnityEditor.SerializedObject(coin.Effects);
foreach (string name in new[] { "_reels", "_damage", "_speed", "_heal", "_shield", "_screenPrefab" })
    if (data.FindProperty(name).objectReferenceValue == null) throw new System.InvalidOperationException("Missing " + name);
var screen = (SSW.CoinScreen)data.FindProperty("_screenPrefab").objectReferenceValue;
if (screen.GetComponentsInChildren<UnityEngine.Collider2D>(true).Length > 0 || screen.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true).Length > 0) throw new System.InvalidOperationException("Screen effects must not contain physics");
int missing = 0;
foreach (var item in player.GetComponentsInChildren<UnityEngine.Transform>(true)) missing += UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
foreach (var item in screen.GetComponentsInChildren<UnityEngine.Transform>(true)) missing += UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
if (missing != 0) throw new System.InvalidOperationException("Missing scripts: " + missing);
if (player.GetComponentsInChildren<JJW.Script.Jackpot.JackpotDivision>(true).Length != 0 || player.GetComponentsInChildren<GamblerJackpotVFX>(true).Length != 0) throw new System.InvalidOperationException("Offline result handlers must not run on the network player");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return new { success = true, spin = coin.Effects.SpinDuration, interval = coin.Effects.SpinInterval, energy = data.FindProperty("_energy").arraySize, missing, scene = scene.path, dirty = scene.isDirty, protocol = SSW.NetGame.Protocol };
