using System.Drawing;

namespace refactor
{
    public class AntiCC
    {
        /// <summary>
        /// SLOW LOOP: Checks if Cleanse or Mercurial are available. Runs on background thread.
        /// </summary>
        public static void UpdateAvailability(GameState gameState)
        {
            gameState.IsCleanseReady =
                ScreenCapture.ContainsPattern(Values.Summoner1Area, Values.CleansePattern) ||
                ScreenCapture.ContainsPattern(Values.Summoner2Area, Values.CleansePattern);

            gameState.IsMercurialReady = false;
            foreach (var slot in Values.ItemSlots)
            {
                if (ScreenCapture.ContainsPattern(slot.Value, Values.MercurialPattern))
                {
                    gameState.IsMercurialReady = true;
                    break;
                }
            }

            GameState.Current.IsCleanseReady  = gameState.IsCleanseReady;
            GameState.Current.IsMercurialReady = gameState.IsMercurialReady;
        }

        /// <summary>
        /// FAST LOOP: Detects stun and reacts. Runs on main thread for minimal latency.
        /// Returns true if an action was taken.
        /// </summary>
        public static bool CheckForStunAndReact(GameState gameState)
        {
            bool isStunned = ScreenCapture.ContainsPattern(Values.StunArea, Values.StunPattern);
            gameState.IsStunned        = isStunned;
            GameState.Current.IsStunned = isStunned;

            // Only react on the exact frame the stun begins
            if (isStunned && !Values.wasStunnedLastTick)
            {
                bool canAct = Environment.TickCount > Values._lastActionTimestamp + Values.ActionCooldownMs;
                if (canAct)
                {
                    if (gameState.IsMercurialReady)
                    {
                        foreach (var slot in Values.ItemSlots)
                        {
                            if (ScreenCapture.ContainsPattern(slot.Value, Values.MercurialPattern))
                            {
                                Logger.Action($"[Anti-CC] Stun detected — using Mercurial (slot {slot.Key})");
                                InputSimulator.PressItemKey(slot.Key);
                                Values._lastActionTimestamp = Environment.TickCount;
                                Values.wasStunnedLastTick   = isStunned;
                                return true;
                            }
                        }
                    }
                    else if (gameState.IsCleanseReady)
                    {
                        Logger.Action("[Anti-CC] Stun detected — using Cleanse");
                        InputSimulator.SendKeyDown(InputSimulator.ScanCodeShort.KEY_D);
                        InputSimulator.SendKeyUp(InputSimulator.ScanCodeShort.KEY_D);
                        Values._lastActionTimestamp = Environment.TickCount;
                        Values.wasStunnedLastTick   = isStunned;
                        return true;
                    }
                }
            }

            Values.wasStunnedLastTick = isStunned;
            return false;
        }
    }
}
