using HarmonyLib;

namespace GroundControlRts;

/// <summary>
/// IGNORE ARTILLERY on a radar SAM. Every way a SAM picks a target (its own turret scan, the
/// fire-control truck's radar hand-out, the datalink salvo planner) scores candidates through
/// <c>CombatAI.AnalyzeTarget</c>, so a zero score there drops artillery rounds from all of them.
/// The analyzer is the fire-control truck for a StratoLance, so the station's launchers are asked too.
/// </summary>
internal static class CommanderIgnoreArtilleryPatches
{
    [HarmonyPatch(typeof(CombatAI), nameof(CombatAI.AnalyzeTarget))]
    internal static class AnalyzeTargetPatch
    {
        internal static void Postfix(WeaponStation weaponStation, Unit analyzer, TrackingInfo trackingInfo, ref OpportunityThreat __result)
        {
            if (CommanderRadarService.IgnoreArtilleryUnits.Count == 0
                || !Ignores(analyzer, weaponStation)
                || !trackingInfo.TryGetUnit(out Unit target)
                || !CommanderRadarService.IsArtilleryRound(target))
            {
                return;
            }

            __result = new OpportunityThreat(0f, 0f);
        }

        private static bool Ignores(Unit analyzer, WeaponStation? station)
        {
            if (CommanderRadarService.IgnoreArtilleryUnits.Contains(analyzer))
            {
                return true;
            }

            if (station?.Turrets == null)
            {
                return false;
            }

            for (int i = 0; i < station.Turrets.Count; i++)
            {
                if (station.Turrets[i] != null && CommanderRadarService.IgnoreArtilleryUnits.Contains(station.Turrets[i].GetAttachedUnit()))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>A fire-control truck feeding several launchers scores against one launcher's station,
    /// so a round it hands to a launcher set to ignore artillery is turned away here.</summary>
    [HarmonyPatch(typeof(Turret), nameof(Turret.SetTargetFromController))]
    internal static class ControllerTargetPatch
    {
        internal static void Prefix(Turret __instance, ref Unit target)
        {
            if (target != null
                && CommanderRadarService.IgnoreArtilleryUnits.Count > 0
                && CommanderRadarService.IgnoreArtilleryUnits.Contains(__instance.GetAttachedUnit())
                && CommanderRadarService.IsArtilleryRound(target))
            {
                target = null!;
            }
        }
    }
}
