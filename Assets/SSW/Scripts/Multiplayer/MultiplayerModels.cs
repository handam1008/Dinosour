using System;

namespace SSW
{
    public readonly struct MultiplayerRoomInfo
    {
        public MultiplayerRoomInfo(
            string id,
            string name,
            int playerCount,
            bool hasPassword)
        {
            Id = id;
            Name = name;
            PlayerCount = playerCount;
            HasPassword = hasPassword;
        }

        public string Id { get; }
        public string Name { get; }
        public int PlayerCount { get; }
        public bool HasPassword { get; }
    }

    public readonly struct MultiplayerRoomRequest
    {
        public MultiplayerRoomRequest(
            string name,
            string password,
            bool hiddenFromList)
        {
            Name = name;
            Password = password;
            HiddenFromList = hiddenFromList;
        }

        public string Name { get; }
        public string Password { get; }
        public bool HiddenFromList { get; }
    }

    public interface IHealthNetworkBridge
    {
        bool TryForwardDamage(DamageRequest request, out DamageResult pendingResult);
        bool TryForwardHeal(float amount);
    }
}
