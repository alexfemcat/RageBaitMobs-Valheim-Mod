using HarmonyLib;
using RagebateMobs.Network;

namespace RagebateMobs.Patches
{
    // ZRoutedRpc is a plain class created during ZNet's lifecycle.
    // ZNet.Awake fires on both server and clients — registering here is the
    // earliest reliable point at which ZRoutedRpc.instance is non-null.
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class ZNetAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            RoastRpc.Register();
        }
    }
}
