var checks = new System.Collections.Generic.List<string>();
{
float t;
if (!SSW.ShotQuery.Capsule(new UnityEngine.Vector2(-2,0), new UnityEngine.Vector2(2,0), UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, UnityEngine.Vector2.up, 0.5f, 0.5f, out t) || UnityEngine.Mathf.Abs(t-0.375f)>0.0001f) throw new System.Exception("stationary capsule");
checks.Add("stationary capsule");
if (SSW.ShotQuery.Capsule(new UnityEngine.Vector2(-2,2), new UnityEngine.Vector2(2,2), UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, UnityEngine.Vector2.up, 0.5f, 0.5f, out t)) throw new System.Exception("miss");
checks.Add("miss");
if (!SSW.ShotQuery.Capsule(UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, new UnityEngine.Vector2(-2,0), new UnityEngine.Vector2(2,0), UnityEngine.Vector2.up, 0.5f, 0.5f, out t) || UnityEngine.Mathf.Abs(t-0.375f)>0.0001f) throw new System.Exception("moving target");
checks.Add("moving target");
if (!SSW.ShotQuery.Capsule(UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, UnityEngine.Vector2.up, 0.5f, 0.5f, out t) || t!=0) throw new System.Exception("overlap");
checks.Add("overlap");
if (!SSW.ShotQuery.Capsule(new UnityEngine.Vector2(-2,1), new UnityEngine.Vector2(2,1), UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, UnityEngine.Vector2.up, 0.5f, 0.5f, out t) || UnityEngine.Mathf.Abs(t-0.5f)>0.0001f) throw new System.Exception("tangent");
checks.Add("tangent");
}
{
var track = new SSW.HitTrack();

UnityEngine.Vector2 point;
track.Store(1, 1, UnityEngine.Vector2.zero);
track.Store(2, 1, new UnityEngine.Vector2(2,0));
if (!track.Read(1.5,1,out point) || UnityEngine.Vector2.Distance(point,UnityEngine.Vector2.right)>0.0001f) throw new System.Exception("interpolation");
checks.Add("interpolation");
if (track.Read(0,1,out point)) throw new System.Exception("expired");
checks.Add("expired");
track.Store(3,2,new UnityEngine.Vector2(10,0));
if (track.Read(2.5,2,out point)) throw new System.Exception("epoch crossing");
checks.Add("epoch crossing rejected");
if (!track.Read(3,2,out point) || point.x!=10) throw new System.Exception("new epoch");
checks.Add("new epoch");
track.Store(3,2,new UnityEngine.Vector2(12,0));
if (!track.Read(3,2,out point) || point.x!=12) throw new System.Exception("replace");
checks.Add("replace");
for (int i=4;i<304;i++) track.Store(i,2,new UnityEngine.Vector2(i,0));
if (track.Read(4,2,out point) || !track.Read(302.5,2,out point) || point.x!=302.5f) throw new System.Exception("ring");
checks.Add("ring rollover");
}
return checks;
