var rooms = new SSW.RoomCatalog();
var live = new SSW.MultiplayerRoomInfo("live", "Live", 1, false);
var dead = new SSW.MultiplayerRoomInfo("dead", "Dead", 1, false);
int passed = 0;
void Check(bool valid, string label)
{
    if (!valid) throw new System.InvalidOperationException(label);
    passed++;
}
rooms.Replace(new[] {live, dead, new SSW.MultiplayerRoomInfo("empty", "Empty", 0, false),
    new SSW.MultiplayerRoomInfo("full", "Full", 2, false)}, 0);
Check(rooms.Items.Count == 2, "Only occupied rooms with an available player slot are listed");
int version = rooms.Version;
rooms.Replace(new[] {live, dead}, 1);
Check(rooms.Version == version, "Unchanged queries preserve the displayed rows");
rooms.Reject("dead", 2);
Check(rooms.Items.Count == 1 && rooms.Items[0].Id == "live", "Failed joins remove only the unusable room");
rooms.Replace(new[] {live, dead}, 31);
Check(rooms.Items.Count == 1, "Service results cannot immediately reintroduce a failed room");
rooms.Replace(new[] {live, dead}, 32);
Check(rooms.Items.Count == 2, "A temporary connection failure can be retried after its cooldown");
rooms.Clear();
Check(rooms.Items.Count == 0, "Failed queries can clear all stale rows");
version = rooms.Version;
rooms.Clear();
Check(rooms.Version == version, "Empty refreshes do not rebuild the menu");
rooms.Replace(new[] {live}, 40);
rooms.Replace(new[] {new SSW.MultiplayerRoomInfo("live", "Renamed", 1, true)}, 41);
Check(rooms.Items[0].Name == "Renamed" && rooms.Items[0].HasPassword, "Room edits propagate");
return Newtonsoft.Json.JsonConvert.SerializeObject(new {passed});
