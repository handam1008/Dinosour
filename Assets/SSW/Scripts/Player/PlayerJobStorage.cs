using UnityEngine;

namespace SSW
{
    public static class PlayerJobStorage
    {
        public const string PreferenceKey = "SSW.EquippedJob.v1";

        public static PlayerJob Load()
        {
            int storedValue = PlayerPrefs.GetInt(PreferenceKey, (int)PlayerJob.Magician);
            return ToSelectableJob(storedValue);
        }

        public static void Save(PlayerJob job)
        {
            PlayerPrefs.SetInt(PreferenceKey, (int)ToSelectableJob((int)job));
            PlayerPrefs.Save();
        }

        public static bool IsSelectable(PlayerJob job)
        {
            return job is PlayerJob.Magician
                or PlayerJob.Swordsman
                or PlayerJob.Assassin
                or PlayerJob.Gambler
                or PlayerJob.Gunner
                or PlayerJob.Witch;
        }

        static PlayerJob ToSelectableJob(int value)
        {
            PlayerJob job = (PlayerJob)value;
            return IsSelectable(job) ? job : PlayerJob.Magician;
        }
    }
}
