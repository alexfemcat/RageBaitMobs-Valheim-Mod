using System;
using System.Threading.Tasks;
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

        private const float CallResponseDelaySeconds = 1.5f;
        private const float CallResponseGroupCooldown = 30f;
        private const float RivalryGroupCooldown = 45f;
        private const float HypeGroupCooldown = 20f;
        private const float BetGroupCooldown = 60f;
        private const float BetChance = 0.35f; // 35% of qualifying spotted_player triggers open a bet
        private const float BetDelaySeconds = 0.8f;

        private static readonly System.Random _rng = new System.Random();

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
        // Optional candidate is a nearby same-type buddy used for call-and-response chaining.
        // Optional rival is a nearby enemy-species mob used for rivalry trash-talk.
        public static void SendRequest(
            ZDOID mobId,
            string mobName,
            string playerName,
            string triggerType,
            string mobType = "",
            ZDOID candidateMobId = default,
            string candidateMobName = "",
            string candidateMobType = "",
            ZDOID rivalMobId = default,
            string rivalMobName = "",
            string rivalMobType = "")
        {
            if (ZRoutedRpc.instance == null) return;
            if (mobId == ZDOID.None) return;

            var pkg = new ZPackage();
            pkg.Write(mobId);
            pkg.Write(mobName ?? "");
            pkg.Write(playerName ?? "");
            pkg.Write(triggerType ?? "");
            pkg.Write(mobType ?? "");
            pkg.Write(candidateMobId);
            pkg.Write(candidateMobName ?? "");
            pkg.Write(candidateMobType ?? "");
            pkg.Write(rivalMobId);
            pkg.Write(rivalMobName ?? "");
            pkg.Write(rivalMobType ?? "");

            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, pkg);
        }

        // Server-side handler.
        private static void OnRequest(long sender, ZPackage pkg)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer()) return;
            if (!RagebateMobsPlugin.Config.Enabled.Value) return;
            if (pkg == null) return;

            ZDOID mobId, candidateMobId, rivalMobId;
            string mobName, playerName, triggerType, mobType;
            string candidateMobName, candidateMobType;
            string rivalMobName, rivalMobType;
            try
            {
                mobId = pkg.ReadZDOID();
                mobName = pkg.ReadString();
                playerName = pkg.ReadString();
                triggerType = pkg.ReadString();
                mobType = pkg.ReadString();
                candidateMobId = pkg.ReadZDOID();
                candidateMobName = pkg.ReadString();
                candidateMobType = pkg.ReadString();
                rivalMobId = pkg.ReadZDOID();
                rivalMobName = pkg.ReadString();
                rivalMobType = pkg.ReadString();
            }
            catch (Exception ex)
            {
                RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Malformed roast request: {ex.Message}");
                return;
            }

            if (mobId == ZDOID.None) return;

            if (triggerType == "player_died")
            {
                RagebateMobsPlugin.KillCountManager.RecordKill(mobType, playerName);
                RagebateMobsPlugin.BettingManager?.Resolve(mobId, playerWon: false, sender);
            }

            if (triggerType == "player_killed_mob")
            {
                RagebateMobsPlugin.BettingManager?.Resolve(mobId, playerWon: true, sender);
            }

            string intensity = RagebateMobsPlugin.Config.InsultIntensity.Value;

            // Phase 12.2 — hype_man fires its own dedicated flow and short-circuits.
            if (triggerType == "hype_man")
            {
                if (!RagebateMobsPlugin.CooldownManager.CanGroupSpeak("hype:" + mobType, HypeGroupCooldown)) return;
                RagebateMobsPlugin.CooldownManager.RecordGroupSpeak("hype:" + mobType);

                string hypePrompt = PromptBuilder.BuildHypeManPrompt(mobName, mobType, candidateMobName, playerName, intensity);
                GenerateAndBroadcastFromPrompt(mobId, mobName, hypePrompt, sender, "hype");
                return;
            }

            // Mob's last-words on death bypasses the per-mob cooldown — they only get one
            // chance to speak, and they're about to despawn anyway.
            bool bypassCooldown = triggerType == "player_killed_mob";
            if (!bypassCooldown)
            {
                if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mobId)) return;
                RagebateMobsPlugin.CooldownManager.RecordMobSpeak(mobId);
            }

            string shameContext = RagebateMobsPlugin.KillCountManager.GetShameContext(mobType, playerName);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} ({mobType}) -> {playerName} ({triggerType}) requested by peer {sender}, generating roast");

            string prompt = PromptBuilder.BuildInsultPrompt(mobName, triggerType, playerName, mobType, shameContext, intensity);

            // Capture closure-locals so the candidate values are available after the await.
            ZDOID capturedCandidateId = candidateMobId;
            string capturedCandidateName = candidateMobName;
            string capturedCandidateType = candidateMobType;
            ZDOID capturedRivalId = rivalMobId;
            string capturedRivalName = rivalMobName;
            string capturedRivalType = rivalMobType;

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (string.IsNullOrWhiteSpace(insult))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty insult from LLM for {mobName}");
                    return;
                }

                MainThreadDispatcher.Enqueue(() => Broadcast(mobId, mobName, insult, sender));

                // Phase 11.2 — call & response: same-type buddy follows up shortly.
                if (capturedCandidateId != ZDOID.None &&
                    !string.IsNullOrEmpty(capturedCandidateType) &&
                    triggerType != "player_killed_mob" &&
                    triggerType != "player_died" &&
                    RagebateMobsPlugin.CooldownManager.CanGroupSpeak("response:" + mobType, CallResponseGroupCooldown))
                {
                    RagebateMobsPlugin.CooldownManager.RecordGroupSpeak("response:" + mobType);
                    ScheduleCallResponse(capturedCandidateId, capturedCandidateName, capturedCandidateType, mobName, insult, playerName, intensity, sender);
                }

                // Phase 12.1 — rivalry: enemy-species mob nearby fires off insult at this mob's species.
                if (capturedRivalId != ZDOID.None &&
                    !string.IsNullOrEmpty(capturedRivalType) &&
                    triggerType != "player_killed_mob" &&
                    triggerType != "player_died" &&
                    RagebateMobsPlugin.CooldownManager.CanGroupSpeak("rivalry:" + RivalryKey(mobType, capturedRivalType), RivalryGroupCooldown))
                {
                    RagebateMobsPlugin.CooldownManager.RecordGroupSpeak("rivalry:" + RivalryKey(mobType, capturedRivalType));
                    ScheduleRivalry(capturedRivalId, capturedRivalName, capturedRivalType, mobName, mobType, playerName, intensity, sender);
                }

                // Phase 12.3 — open a bet. Only on spotted_player so it lines up with fight start.
                // Bettor is the same nearby same-type buddy used for call-response (capturedCandidate).
                if (triggerType == "spotted_player" &&
                    capturedCandidateId != ZDOID.None &&
                    !string.IsNullOrEmpty(capturedCandidateType) &&
                    !RagebateMobsPlugin.BettingManager.HasOpenBet(mobId) &&
                    RagebateMobsPlugin.CooldownManager.CanGroupSpeak("bet:" + mobType, BetGroupCooldown) &&
                    _rng.NextDouble() < BetChance)
                {
                    RagebateMobsPlugin.CooldownManager.RecordGroupSpeak("bet:" + mobType);
                    bool betPlayerWins = _rng.NextDouble() < 0.5;

                    var session = new Managers.BettingManager.BetSession
                    {
                        FighterMobName = mobName,
                        FighterMobType = mobType,
                        PlayerName = playerName,
                        BettorId = capturedCandidateId,
                        BettorName = capturedCandidateName,
                        BettorType = capturedCandidateType,
                        BetPlayerWillWin = betPlayerWins,
                    };
                    RagebateMobsPlugin.BettingManager.OpenBet(session, mobId);

                    ScheduleBetPlacement(session, intensity, sender);
                }
            }, RagebateMobsPlugin.LlmSemaphore);
        }

        private static void ScheduleCallResponse(ZDOID responderId, string responderName, string responderType, string callerName, string callerInsult, string playerName, string intensity, long targetPeer)
        {
            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(CallResponseDelaySeconds));

                string responsePrompt = PromptBuilder.BuildCallResponsePrompt(responderName, responderType, callerName, callerInsult, playerName, intensity);
                var response = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(responsePrompt);
                if (string.IsNullOrWhiteSpace(response))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty call-response insult from LLM for {responderName}");
                    return;
                }

                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] CALL-RESPONSE {responderName}: {response}");
                MainThreadDispatcher.Enqueue(() => Broadcast(responderId, responderName, response, targetPeer));
            }, RagebateMobsPlugin.LlmSemaphore);
        }

        private static void ScheduleRivalry(ZDOID responderId, string responderName, string responderType, string rivalName, string rivalType, string playerName, string intensity, long targetPeer)
        {
            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2.0));

                string prompt = PromptBuilder.BuildRivalryPrompt(responderName, responderType, rivalName, rivalType, playerName, intensity);
                var line = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (string.IsNullOrWhiteSpace(line))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty rivalry insult from LLM for {responderName}");
                    return;
                }

                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] RIVALRY {responderName} vs {rivalName}: {line}");
                MainThreadDispatcher.Enqueue(() => Broadcast(responderId, responderName, line, targetPeer));
            }, RagebateMobsPlugin.LlmSemaphore);
        }

        private static void ScheduleBetPlacement(Managers.BettingManager.BetSession session, string intensity, long targetPeer)
        {
            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(BetDelaySeconds));

                string betPrompt = PromptBuilder.BuildBetPrompt(
                    session.BettorName, session.BettorType, session.FighterMobName,
                    session.PlayerName, session.BetPlayerWillWin, intensity);
                var line = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(betPrompt);
                if (string.IsNullOrWhiteSpace(line))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty bet line from LLM for {session.BettorName}");
                    return;
                }

                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] BET {session.BettorName} bets {(session.BetPlayerWillWin ? "PLAYER" : "MOB")}: {line}");
                MainThreadDispatcher.Enqueue(() => Broadcast(session.BettorId, session.BettorName, line, targetPeer));
            }, RagebateMobsPlugin.LlmSemaphore);
        }

        // Stable rivalry key irrespective of which side fired first.
        private static string RivalryKey(string typeA, string typeB)
        {
            if (string.Compare(typeA, typeB, StringComparison.OrdinalIgnoreCase) <= 0)
                return typeA + "|" + typeB;
            return typeB + "|" + typeA;
        }

        // Used by ad-hoc, server-already-validated insult generations (hype, betting outcome).
        // The triggering peer should already be filtered upstream; this is only invoked from
        // server-side flows that build their own prompts.
        public static void GenerateAndBroadcastFromPrompt(ZDOID broadcasterId, string broadcasterName, string prompt, long targetPeer = -1, string label = "custom")
        {
            if (broadcasterId == ZDOID.None || string.IsNullOrEmpty(prompt)) return;

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var line = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (string.IsNullOrWhiteSpace(line))
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty {label} line for {broadcasterName}");
                    return;
                }

                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {label.ToUpperInvariant()} {broadcasterName}: {line}");
                MainThreadDispatcher.Enqueue(() => Broadcast(broadcasterId, broadcasterName, line, targetPeer));
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
