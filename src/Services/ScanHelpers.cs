using System.Collections.Generic;

namespace RagebateMobs.Services
{
    public static class ScanHelpers
    {
        public const float CallResponseRadius = 20f;
        public const float RivalryRadius = 30f;
        public const float HypeRadius = 25f;
        public const float BettingRadius = 25f;

        // Mob-type rivalry pairs. Keys reference the prefab name on the Character GameObject
        // (Character.name) — not the localized display name.
        private static readonly Dictionary<string, string[]> Rivals = new Dictionary<string, string[]>
        {
            { "Greydwarf",       new[] { "Skeleton", "Skeleton_NoArcher", "Skeleton_Poison" } },
            { "GreydwarfBrute",  new[] { "Skeleton", "Skeleton_NoArcher", "Skeleton_Poison" } },
            { "GreydwarfShaman", new[] { "Skeleton", "Skeleton_NoArcher", "Skeleton_Poison" } },
            { "Skeleton",        new[] { "Greydwarf", "GreydwarfBrute", "GreydwarfShaman" } },
            { "Skeleton_NoArcher", new[] { "Greydwarf", "GreydwarfBrute", "GreydwarfShaman" } },
            { "Skeleton_Poison", new[] { "Greydwarf", "GreydwarfBrute", "GreydwarfShaman" } },
            { "Draugr",          new[] { "Wraith", "Ghost" } },
            { "Draugr_Elite",    new[] { "Wraith", "Ghost" } },
            { "Draugr_Ranged",   new[] { "Wraith", "Ghost" } },
            { "Wraith",          new[] { "Draugr", "Draugr_Elite", "Draugr_Ranged" } },
            { "Ghost",           new[] { "Draugr", "Draugr_Elite", "Draugr_Ranged" } },
            { "Goblin",          new[] { "Wolf", "Fenring" } },
            { "GoblinBrute",     new[] { "Wolf", "Fenring" } },
            { "GoblinShaman",    new[] { "Wolf", "Fenring" } },
            { "Wolf",            new[] { "Goblin", "GoblinBrute", "GoblinShaman" } },
            { "Fenring",         new[] { "Goblin", "GoblinBrute", "GoblinShaman" } },
        };

        public static (ZDOID id, string name, string type) FindBuddy(Character original)
        {
            var buddy = NearbyMobScanner.FindNearbySameType(original, CallResponseRadius);
            return Identify(buddy);
        }

        public static (ZDOID id, string name, string type) FindRival(Character original)
        {
            if (original == null) return (ZDOID.None, "", "");
            if (!Rivals.TryGetValue(original.name, out var rivalTypes)) return (ZDOID.None, "", "");
            var rival = NearbyMobScanner.FindNearbyByTypes(original, rivalTypes, RivalryRadius);
            return Identify(rival);
        }

        public static (ZDOID id, string name, string type) Identify(Character c)
        {
            if (c == null) return (ZDOID.None, "", "");
            var nv = c.GetComponent<ZNetView>();
            if (nv == null || nv.GetZDO() == null) return (ZDOID.None, "", "");

            string display = global::Localization.instance != null
                ? global::Localization.instance.Localize(c.m_name)
                : c.m_name;
            if (string.IsNullOrWhiteSpace(display)) display = c.m_name;
            return (nv.GetZDO().m_uid, display, c.name);
        }
    }
}
