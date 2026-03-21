using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;

namespace BiggerOnTheInsideSMAPI
{
    // Unpatches SVE's Premium Barn second-door patches (SVE ModEntry.cs L620–696 Meow =^..^=) so the building matches vanilla Deluxe Barn layout.
    internal sealed class ModEntry : Mod
    {
#if DEBUG
        private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
        private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
        private const string SVEHarmonyId = "FlashShifter.SVECode";
        private static IMonitor mon = null!;
        private static Harmony harm = null!;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            harm = new Harmony(ModManifest.UniqueID);

            // Skip if SVE not loaded
            if (!helper.ModRegistry.IsLoaded(SVEHarmonyId))
            {
                Log("Could not find SVE C# mod (FlashShifter.SVECode); Premium Barn door removal will not be applied.", LogLevel.Warn);
                return;
            }

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private static void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            try
            {
                // Unpatch all three methods
                UnpatchSvePostfix(typeof(Building), nameof(Building.doesTileHaveProperty), new[] { typeof(int), typeof(int), typeof(string), typeof(string), typeof(string).MakeByRefType() });
                UnpatchSvePostfix(typeof(Building), nameof(Building.doAction), new[] { typeof(Vector2), typeof(Farmer) });
                UnpatchSvePostfix(typeof(Building), nameof(Building.updateInteriorWarps), new[] { typeof(GameLocation) });
            }
            catch (Exception err)
            {
                Log($"Failed to unpatch SVE Premium Barn door patches:\n{err}", LogLevel.Error);
            }
        }

        private static void UnpatchSvePostfix(Type type, string methodName, Type[] argTypes)
        {
            var method = AccessTools.Method(type, methodName, argTypes);
            if (method == null)
            {
                Log($"Could not find {type.Name}.{methodName} to unpatch; SVE or game may have changed.", LogLevel.Warn);
                return;
            }

            var patchInfo = Harmony.GetPatchInfo(method);
            if (patchInfo?.Postfixes == null || !patchInfo.Postfixes.Any(p => p.owner == SVEHarmonyId))
            {
                return;
            }

            // Remove only SVE's postfix
            harm.Unpatch(method, HarmonyPatchType.Postfix, SVEHarmonyId);
            Log($"Removed SVE postfix from {type.Name}.{methodName}", DEFAULT_LOG_LEVEL);
        }

        private static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
        {
            mon.Log(msg, level);
        }
    }
}
