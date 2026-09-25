var channel=UnityEngine.ScriptableObject.CreateInstance<NKY.Lib.EventChannel.VoidEventChannelSO>();
var root=new UnityEngine.GameObject("FeedbackLifetime");
var module=root.AddComponent<NKY.Scripts.FeedBack.FeedBackModule>();
var so=new UnityEditor.SerializedObject(module);so.FindProperty("feedBackEventChannel").objectReferenceValue=channel;so.ApplyModifiedPropertiesWithoutUndo();
root.AddComponent<Unity.Cinemachine.CinemachineImpulseSource>();var shake=root.AddComponent<NKY.Scripts.FeedBack.CameraShakeFeedBack>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(NKY.Scripts.FeedBack.FeedBackModule).GetMethod("Awake",flags).Invoke(module,null);
typeof(NKY.Scripts.FeedBack.FeedBackModule).GetMethod("Start",flags).Invoke(module,null);
typeof(NKY.Scripts.FeedBack.CameraShakeFeedBack).GetMethod("Awake",flags).Invoke(shake,null);
UnityEngine.Object.DestroyImmediate(root);string error="";try{channel.Raise();}catch(System.Exception e){error=e.GetType().Name+": "+e.Message;}finally{UnityEngine.Object.DestroyImmediate(channel);}
System.IO.File.WriteAllText("Logs/SceneMatch25/FeedbackBefore.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error}));
return new{error};