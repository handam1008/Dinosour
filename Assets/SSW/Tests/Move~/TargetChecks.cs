var game = SSW.NetGame.Current;
if (!UnityEngine.Application.isPlaying || game == null || !game.CanFight || !game.Manager.IsServer)
    throw new System.InvalidOperationException("Run in a playing host match.");
var caster = game.Players[0];
var target = game.Players[1];
var saved = target.View.position;
float health = target.Health.Current;
var checks = new System.Collections.Generic.List<string>();
try
{
    target.View.position = new UnityEngine.Vector3(1000f, 1000f);
    var contact = new SSW.ShotContact(caster, game.Players);
    var from = new UnityEngine.Vector2(998f, 1000f);
    var to = new UnityEngine.Vector2(1002f, 1000f);
    if (!contact.TryBlock(from, to, 0.14f, default, 0.2f, 12f) || contact.Point.x >= 1000f)
        throw new System.Exception("A visible target crossing was not stopped on the near side.");
    checks.Add("Visible capsule contact stops before the target center.");
    var pose = new SSW.ShotPose { Time = 10.1d, Position = to, Velocity = UnityEngine.Vector2.right * 12f };
    if (contact.Advance(pose, 10d) || !contact.Blocked)
        throw new System.Exception("An old server sample released an unconfirmed contact.");
    pose.Time = 10.4d;
    if (!contact.Advance(pose, 10d) || contact.Blocked)
        throw new System.Exception("An authoritative miss did not resume flight.");
    if (contact.TryBlock(from, to, 0.14f, default, 0.5f, 12f))
        throw new System.Exception("The same rejected contact blocked flight again.");
    checks.Add("Only a newer server sample beyond contact releases a miss without blocking it again.");
    pose.Turn = 1;
    contact.Advance(pose, 10d);
    if (!contact.TryBlock(to, from, 0.14f, default, 0.6f, 12f))
        throw new System.Exception("A changed trajectory cannot contact the target again.");
    pose.Turn = 2;
    if (!contact.Advance(pose, 10d) || contact.Blocked)
        throw new System.Exception("A server redirect did not release contact.");
    checks.Add("Return and bounce turns reset and release predicted contact.");
    if (target.Health.Current != health)
        throw new System.Exception("Presentation contact changed authoritative health.");
    checks.Add("Presentation contact does not apply damage.");
    return checks;
}
finally
{
    target.View.position = saved;
}
