var cast = SSW.NetGame.Current.Local.Cast;
uint epoch = SSW.NetGame.Current.Local.Epoch;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = typeof(SSW.NetCast);
var pending = (System.Collections.IList)type.GetField("_stockPending", flags).GetValue(cast);
if (pending.Count != 0) throw new System.InvalidOperationException("Test requires an idle host");
string[] names = { "_seenStock", "_localHeld", "_localNext", "_localPocket", "_localHeldId", "_localNextId", "_localPocketId", "_localPocketUsed" };
var saved = names.ToDictionary(n => n, n => type.GetField(n, flags).GetValue(cast));
var sync = type.GetMethod("SyncStock", flags);
var entryType = type.GetNestedType("StockInput", System.Reflection.BindingFlags.NonPublic);
int checks = 0;
void Check(bool value, string name)
{
    if (!value) throw new System.InvalidOperationException(name);
    checks++;
}
void Pending(uint action, SSW.CastKind kind, SSW.CastStock.Item item)
{
    var value = System.Activator.CreateInstance(entryType);
    entryType.GetField("Input").SetValue(value, new SSW.CastInput { Action = action, Kind = kind, Stock = item.Id, Epoch = epoch });
    entryType.GetField("Kind").SetValue(value, item.Kind);
    pending.Add(value);
}
SSW.NetCast.PotionState State(SSW.CastStock bag, uint revision, uint action) => new SSW.NetCast.PotionState
{
    Revision = revision, Action = action, Epoch = epoch,
    HeldId = bag.Held.Id, Held = bag.Held.Kind, NextId = bag.Next.Id, Next = bag.Next.Kind,
    PocketId = bag.Pocket.Id, Pocket = bag.Pocket.Kind, PocketUsed = bag.PocketUsed
};
void Apply(SSW.NetCast.PotionState state) => sync.Invoke(cast, new object[] { state });
bool Same(uint held, uint next, uint pocket, bool used) =>
    (uint)type.GetField("_localHeldId", flags).GetValue(cast) == held
    && (uint)type.GetField("_localNextId", flags).GetValue(cast) == next
    && (uint)type.GetField("_localPocketId", flags).GetValue(cast) == pocket
    && (bool)type.GetField("_localPocketUsed", flags).GetValue(cast) == used;
SSW.CastStock Setup(bool used)
{
    pending.Clear();
    type.GetField("_seenStock", flags).SetValue(cast, 0u);
    var bag = new SSW.CastStock();
    bag.Reset(epoch);
    bag.Add(0, 0, 1.25);
    bag.Swap(bag.Held.Id, epoch, 0);
    if (!used)
    {
        bag.Add(5, 0, 1.25);
        bag.Take(bag.Held.Id, epoch, 0, out _);
    }
    bag.Add(1, 0, 1.25);
    bag.Add(2, 0, 1.25);
    return bag;
}
try
{
    var bag = Setup(true);
    var a = bag.Held;
    var b = bag.Next;
    var p = bag.Pocket;
    Pending(1, SSW.CastKind.Press, a);
    Pending(2, SSW.CastKind.Cycle, b);
    bag.Add(3, 0.1, 1.25);
    var c = bag.Next;
    Apply(State(bag, 1, 0));
    Check(Same(p.Id, c.Id, b.Id, true), "brewing snapshot replays retired throw then swap");
    Check(bag.Take(a.Id, epoch, 0.2, out _), "server accepts delayed issued throw");
    Apply(State(bag, 2, 1));
    Check(Same(p.Id, c.Id, b.Id, true), "throw acknowledgement retains predicted swap");
    Check(bag.Swap(b.Id, epoch, 0.3), "server accepts swap after throw");
    Apply(State(bag, 3, 2));
    Check(Same(p.Id, c.Id, b.Id, true) && pending.Count == 0, "swap acknowledgement matches prediction");
    bag = Setup(false);
    a = bag.Held;
    b = bag.Next;
    p = bag.Pocket;
    Pending(1, SSW.CastKind.Cycle, a);
    Pending(2, SSW.CastKind.Press, p);
    bag.Add(3, 0.1, 1.25);
    c = bag.Next;
    Apply(State(bag, 1, 0));
    Check(Same(b.Id, c.Id, a.Id, false), "brewing snapshot replays retired swap then throw");
    Check(bag.Swap(a.Id, epoch, 0.2), "server swaps retired held item");
    Apply(State(bag, 2, 1));
    Check(Same(b.Id, c.Id, a.Id, false), "swap acknowledgement retains predicted pocket throw");
    Check(bag.Take(p.Id, epoch, 0.3, out _) && !bag.Take(p.Id, epoch, 0.4, out _), "pocket item fires exactly once");
    Apply(State(bag, 3, 2));
    Check(Same(b.Id, c.Id, a.Id, false) && pending.Count == 0, "pocket throw acknowledgement matches prediction");
    bag = Setup(true);
    a = bag.Held;
    b = bag.Next;
    p = bag.Pocket;
    Pending(1, SSW.CastKind.Press, a);
    Pending(2, SSW.CastKind.Cycle, b);
    bag.Add(3, 0.1, 1.25);
    c = bag.Next;
    var stale = State(bag, 1, 0);
    Apply(stale);
    Check(!bag.Take(a.Id, epoch, 2, out _) && !bag.Swap(b.Id, epoch, 2), "expired throw and locked swap are rejected");
    Apply(State(bag, 2, 1));
    Check(Same(b.Id, c.Id, p.Id, true), "rejected throw restores authoritative pocket lock");
    Apply(State(bag, 3, 2));
    Apply(stale);
    Check(Same(b.Id, c.Id, p.Id, true) && pending.Count == 0, "stale acknowledgement cannot restore spent items");
    return new { passed = checks };
}
finally
{
    pending.Clear();
    foreach (var item in saved) type.GetField(item.Key, flags).SetValue(cast, item.Value);
}
