using HarmonyLib;
using RagebateMobs.Network;

namespace RagebateMobs.Patches
{
    // ZRoutedRpc is created by ZNet, so register our RPC after ZRoutedRpc.Awake.
    // This fires on both server and clients when their network stack initializes.
    [HarmonyPatch(typeof(ZRoutedRpc), "Awake")]
    public static class ZRoutedRpcAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            RoastRpc.Register();
        }
    }
}
