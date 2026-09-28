using System.Collections.Generic;
using Unity.Services.Multiplayer;

namespace SSW
{
    public readonly struct RoomPlayer
    {
        const string NameKey = "name";
        const string JobKey = "job";

        public string Id { get; }
        public string Name { get; }
        public PlayerJob Job { get; }
        public bool IsHost { get; }

        public RoomPlayer(IReadOnlyPlayer player, string hostId, int slot)
        {
            Id = player.Id;
            IsHost = Id == hostId;
            player.Properties.TryGetValue(NameKey, out PlayerProperty name);
            player.Properties.TryGetValue(JobKey, out PlayerProperty job);
            Name = Fighter.Create(name?.Value).DisplayName(slot);
            Job = int.TryParse(job?.Value, out int value) && PlayerJobStorage.IsSelectable((PlayerJob)value)
                ? (PlayerJob)value : PlayerJob.None;
        }

        public static Dictionary<string, PlayerProperty> Properties(string name, PlayerJob job)
        {
            return new Dictionary<string, PlayerProperty>
            {
                { NameKey, new PlayerProperty(Fighter.Create(name).Name.ToString(), VisibilityPropertyOptions.Member) },
                { JobKey, new PlayerProperty(((int)job).ToString(), VisibilityPropertyOptions.Member) }
            };
        }
    }
}
