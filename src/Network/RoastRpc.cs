using System;
using RagebateMobs.Services;
using UnityEngine;

namespace RagebateMobs.Network
{
    // Hybrid network protocol:
    //   Modded client --[RagebateMobs_RequestRoast]--> Server (calls LLM)
    //   Server        --[RagebateMobs_RoastBroadcast]--> Modded clients (Chat.SetNpcText bubble)
    //                  --[ChatMessage]----------------> Vanilla clients (local chat message)
    //
    // Vanilla clients can still join the server (soft dep). They don't send request RPCs and
    // they don't render the bubble (no handler), but they can play normally.
    // We send them a local ChatMessage instead of our custom RPC so they see the insult.
    public static class RoastRpc
    {
        public const string RequestRpc = "RagebateMobs_RequestRoast";
        public const string BroadcastRpc = "RagebateMobs_RoastBroadcast";

        private const float BubbleCullDistance = 30f;
        private const float BubbleTtlSeconds = 5f;

        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            if (ZRoutedRpc.instance == null) return;

            ZRoutedRpc.instance.Register<ZPackage>(RequestRpc, OnRequest);
            ZRoutedRpc.instance.Register<ZPackage>(BroadcastRpc, OnBroadcast);
            _registered = true;
            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] Registered RPCs: {RequestRpc}, {BroadcastRpc}");
        }

        // Called from CLIENT-side patches.
        public static void SendRequest(ZDOID mobId, string mobName, string playerName, string triggerType, string mobType = "")
        {
            if (ZRoutedRpc.instance == null) return;
            if (mobId == ZDOID.None) return;

            var pkg = new ZPackage();
            pkg.Write(mobId);
            pkg.Write(mobName ?? "");
            pkg.Write(playerName ?? "");
            pkg.Write(triggerType ?? "");
            pkg.Write(mobType ?? "");

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
            var mobType = pkg.ReadString();

            if (mobId == ZDOID.None) return;

            if (triggerType == "player_died")
            {
                RagebateMobsPlugin.KillCountManager.RecordKill(mobType, playerName);
            }

            if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mobId)) return;
            RagebateMobsPlugin.CooldownManager.RecordMobSpeak(mobId);

            string shameContext = RagebateMobsPlugin.KillCountManager.GetShameContext(mobType, playerName);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} ({mobType}) -> {playerName} ({triggerType}) requested by peer {sender}, generating roast");

            string prompt = PromptBuilder.BuildInsultPrompt(mobName, triggerType, playerName, mobType, shameContext, RagebateMobsPlugin.Config.InsultIntensity.Value);

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (string.IsNullOrWhiteSpace(insult))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty insult from LLM for {mobName}");
                    return;
                }

                MainThreadDispatcher.Enqueue(() => Broadcast(mobId, mobName, insult, sender));
            }, RagebateMobsPlugin.LlmSemaphore);
        }

        // Server-side broadcast on the main thread.
        // Custom routed RPC; modded clients render via Chat.SetNpcText. Vanilla clients ignore it.
        // For vanilla clients, we send a local ChatMessage to the target player only.
        private static void Broadcast(ZDOID mobId, string mobName, string insult, long targetPeer = -1)
        {
            if (ZRoutedRpc.instance == null) return;

            // Send custom RPC for modded clients (bubble)
            var pkg = new ZPackage();
            pkg.Write(mobId);
            pkg.Write(mobName ?? "");
            pkg.Write(insult ?? "");

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, BroadcastRpc, pkg);

            // For vanilla clients, send a local chat message to the target player only
            if (targetPeer > 0)
            {
                SendVanillaChatMessage(targetPeer, mobName, insult);
            }

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName}: {insult}");
        }

        // Send a vanilla ChatMessage RPC specifically to a single peer
        // This makes the insult appear in that player's local chat
        private static void SendVanillaChatMessage(long targetPeer, string senderName, string message)
        {
            try
            {
                var pkg = new ZPackage();
                pkg.Write(senderName ?? "Mob");
                pkg.Write(message ?? "");
                pkg.Write((long)0); // senderSteamID = 0 for system/mob messages

                // Route to specific peer only, not everybody
                ZRoutedRpc.instance.InvokeRoutedRPC(targetPeer, "ChatMessage", pkg);
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] Sent vanilla chat to peer {targetPeer}: {senderName}: {message}");
            }
            catch (Exception ex)
            {
                RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Failed to send vanilla chat: {ex.Message}");
            }
        }

        // Client-side handler. Renders an NPC speech bubble above the mob's head.
        // Server peers also receive their own broadcast; the FindInstance call returns null
        // on a headless dedicated server (no rendering), so this is a safe no-op there.
        private static void OnBroadcast(long sender, ZPackage pkg)
        {
            if (pkg == null) return;

            var mobId = pkg.ReadZDOID();
            var mobName = pkg.ReadString();
            var insult = pkg.ReadString();

            if (Chat.instance == null) return;
            if (ZNetScene.instance == null) return;

            var go = ZNetScene.instance.FindInstance(mobId);
            if (go == null) return;

            // Bubble above the mob's head, cull-distance limited so it doesn't spam the whole server.
            Chat.instance.SetNpcText(
                go,
                Vector3.up * 1.5f,
                BubbleCullDistance,
                BubbleTtlSeconds,
                mobName ?? "",
                insult ?? "",
                false);
        }
    }
}
