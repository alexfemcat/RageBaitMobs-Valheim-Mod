using System.Collections.Generic;
using RagebateMobs.Network;
using RagebateMobs.Services;
using UnityEngine;

namespace RagebateMobs.Managers
{
    // Phase 12.3 — Betting.
    // Tracks open bets keyed by the fighter mob's ZDOID. A bet is opened when a same-type
    // bystander is nearby; resolved when the fighter mob dies (player won) or the player
    // dies to that mob (player lost). Stale bets are pruned via a maximum lifetime.
    public class BettingManager
    {
        private const float BetMaxLifetimeSeconds = 60f;

        private readonly Dictionary<ZDOID, BetSession> _open = new Dictionary<ZDOID, BetSession>();
        private readonly object _lock = new object();

        public class BetSession
        {
            public string FighterMobName;
            public string FighterMobType;
            public string PlayerName;
            public ZDOID BettorId;
            public string BettorName;
            public string BettorType;
            public bool BetPlayerWillWin;
            public float OpenedAt;
        }

        public bool HasOpenBet(ZDOID fighterMobId)
        {
            lock (_lock) return _open.ContainsKey(fighterMobId);
        }

        public void OpenBet(BetSession session, ZDOID fighterMobId)
        {
            if (session == null || fighterMobId == ZDOID.None) return;
            session.OpenedAt = Time.time;
            lock (_lock)
            {
                _open[fighterMobId] = session;
                if (_open.Count > 200) Prune();
            }
        }

        public void Resolve(ZDOID fighterMobId, bool playerWon, long targetPeer)
        {
            BetSession session;
            lock (_lock)
            {
                if (!_open.TryGetValue(fighterMobId, out session)) return;
                _open.Remove(fighterMobId);
            }

            bool wonBet = (session.BetPlayerWillWin == playerWon);
            string intensity = RagebateMobsPlugin.Config.InsultIntensity.Value;
            string outcomePrompt = PromptBuilder.BuildBetOutcomePrompt(
                session.BettorName, session.BettorType, session.FighterMobName, session.PlayerName, wonBet, intensity);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] Bet resolved: {session.BettorName} {(wonBet ? "WON" : "LOST")} bet on {session.FighterMobName} vs {session.PlayerName}");
            RoastRpc.GenerateAndBroadcastFromPrompt(session.BettorId, session.BettorName, outcomePrompt, targetPeer, wonBet ? "bet-win" : "bet-lose");
        }

        public void Clear()
        {
            lock (_lock) _open.Clear();
        }

        private void Prune()
        {
            float now = Time.time;
            var stale = new List<ZDOID>();
            foreach (var kvp in _open)
            {
                if (now - kvp.Value.OpenedAt > BetMaxLifetimeSeconds)
                    stale.Add(kvp.Key);
            }
            foreach (var key in stale) _open.Remove(key);
        }
    }
}
