using System.Drawing;

namespace refactor
{
    public class AntiCC
    {
        /// <summary>
        /// SLOW LOOP: Checks availability of Cleanse and Mercurial. Runs on background thread.
        /// Also tracks WHICH summoner slot has Cleanse so the fast loop presses the right key.
        /// </summary>
        public static void UpdateAvailability(GameState gameState)
        {
            // Detect Cleanse and record which slot it's on (slot 1 = D, slot 2 = F)
            if (ScreenCapture.ContainsPattern(Values.Summoner1Area, Values.CleansePattern))
            {
                gameState.IsCleanseReady = true;
                gameState.CleanseSlot    = 1;
            }
            else if (ScreenCapture.ContainsPattern(Values.Summoner2Area, Values.CleansePattern))
            {
                gameState.IsCleanseReady = true;
                gameState.CleanseSlot    = 2;
            }
            else
            {
                gameState.IsCleanseReady = false;
                gameState.CleanseSlot    = 0;
            }

            // Detect Mercurial in any configured item slot
            gameState.IsMercurialReady = false;
            foreach (var slot in Values.ItemSlots)
            {
                if (ScreenCapture.ContainsPattern(slot.Value, Values.MercurialPattern))
                {
                    gameState.IsMercurialReady = true;
                    break;
                }
            }

            // Sync to shared state for drawing thread
            GameState.Current.IsCleanseReady  = gameState.IsCleanseReady;
            GameState.Current.IsMercurialReady = gameState.IsMercurialReady;
            GameState.Current.CleanseSlot      = gameState.CleanseSlot;
        }

        /// <summary>
        /// FAST LOOP: Detects stun onset and reacts instantly. Runs on main thread.
        /// Returns true if an action was taken.
        /// </summary>
        public static bool CheckForStunAndReact(GameState gameState)
        {
            bool isStunned = ScreenCapture.ContainsPattern(Values.StunArea, Values.StunPattern);
            gameState.IsStunned         = isStunned;
            GameState.Current.IsStunned = isStunned;

            // Only react on the rising edge (first frame of stun)
            if (isStunned && !Values.wasStunnedLastTick)
            {
                bool canAct = Environment.TickCount > Values._lastActionTimestamp + Values.ActionCooldownMs;
                if (canAct)
                {
                    // Priority 1: Mercurial — re-scan to confirm it's still available and find the exact slot
                    if (gameState.IsMercurialReady)
                    {
                        foreach (var slot in Values.ItemSlots)
                        {
                            if (ScreenCapture.ContainsPattern(slot.Value, Values.MercurialPattern))
                            {
                                Logger.Action($"[Anti-CC] Stun — using Mercurial (slot {slot.Key})");
                                InputSimulator.PressItemKey(slot.Key);
                                Values._lastActionTimestamp = Environment.TickCount;
                                Values.wasStunnedLastTick   = true;
                                return true;
                            }
                        }
                        // Mercurial was flagged ready but re-scan found nothing (used between ticks).
                        // Fall through to try Cleanse instead.
                        Logger.Warning("[Anti-CC] Mercurial re-scan failed — falling back to Cleanse");
                    }

                    // Priority 2: Cleanse — press the correct summoner key (D or F)
                    if (gameState.IsCleanseReady && gameState.CleanseSlot != 0)
                    {
                        var cleanseKey = gameState.CleanseSlot == 1
                            ? InputSimulator.ScanCodeShort.KEY_D
                            : InputSimulator.ScanCodeShort.KEY_F;

                        Logger.Action($"[Anti-CC] Stun — using Cleanse (slot {gameState.CleanseSlot}, key {(gameState.CleanseSlot == 1 ? "D" : "F")})");
                        InputSimulator.SendKeyDown(cleanseKey);
                        InputSimulator.SendKeyUp(cleanseKey);
                        Values._lastActionTimestamp = Environment.TickCount;
                        Values.wasStunnedLastTick   = true;
                        return true;
                    }
                }
            }

            Values.wasStunnedLastTick = isStunned;
            return false;
        }
    }
}
