using RagebateMobs.Services;
using UnityEngine;

namespace RagebateMobs.Network
{
    // Hybrid network protocol:
    //   Client (modded) --[RagebateMobs_RequestRoast]--> Server
    //   Server                                          --> calls LLM
    //   Server         --[ChatMessage (vanilla RPC)]--> All clients (incl. vanilla)
    //
    // Vanilla clients can still join (soft dep): they don't send the request RPC,
    // but they DO receive the broadcast because it uses Valheim's built-in chat RPC.
    public static class RoastRpc
    {
        public const string RequestRpc = "RagebateMobs_RequestRoast";
        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            if (ZRoutedRpc.instance == null) return;

            ZRoutedRpc.instance.Register<ZPackage>(RequestRpc, OnRequest);
            _registered = true;
            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] Registered RPC: {RequestRpc}");
        }

        // Called from CLIENT-side patches.
        public static void SendRequest(ZDOID mobId, string mobName, string playerName, string triggerType)
        {
            if (ZRoutedRpc.instance == null) return;
            if (mobId == ZDOID.None) return;

            var pkg = new ZPackage();
            pkg.Write(mobId);
            pkg.Write(mobName ?? "");
            pkg.Write(playerName ?? "");
            pkg.Write(triggerType ?? "");

            // 2-arg overload routes to the server peer automatically.
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, pkg);
        }

        // Server-side handler.
        private static void OnRequest(long sender, ZPackage pkg)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer()) return;
            if (!RagebateMobsPlugin.Config.Enabled.Value) return;
            if (pkg == null) return;

            var mobId = pkg.ReadZDOID();
            var mobName = pkg.ReadString();
            var playerName = pkg.ReadString();
            var triggerType = pkg.ReadString();

            if (mobId == ZDOID.None) return;

            if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mobId)) return;
            RagebateMobsPlugin.CooldownManager.RecordMobSpeak(mobId);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} -> {playerName} ({triggerType}) requested by peer {sender}, generating roast");

            string prompt = PromptBuilder.BuildInsultPrompt(mobName, triggerType, playerName);

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (string.IsNullOrWhiteSpace(insult))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty insult from LLM for {mobName}");
                    return;
                }

                MainThreadDispatcher.Enqueue(() => Broadcast(mobId, mobName, insult));
            });
        }

        // Server-side broadcast on the main thread.
        // Uses Valheim's vanilla ChatMessage routed RPC so unmodded clients also see the speech bubble.
        private static void Broadcast(ZDOID mobId, string mobName, string insult)
        {
            Vector3 position;
            if (ZNetScene.instance != null)
            {
                var go = ZNetScene.instance.FindInstance(mobId);
                if (go != null)
                {
                    position = go.transform.position;
                }
                else
                {
                    var zdo = ZDOMan.instance?.GetZDO(mobId);
                    if (zdo == null)
                    {
                        RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Mob {mobId} not found, dropping roast");
                        return;
                    }
                    position = zdo.GetPosition();
                }
            }
            else
            {
                return;
            }

            var userInfo = new UserInfo { Name = mobName ?? "" };

            ZRoutedRpc.instance.InvokeRoutedRPC(
                ZRoutedRpc.Everybody,
                "ChatMessage",
                position,
                (int)Talker.Type.Shout,
                userInfo,
                insult);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName}: {insult}");
        }
    }
}
