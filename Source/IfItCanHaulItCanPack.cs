using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IfItCanHaulItCanPack
{
    // Runs once after all defs are loaded. Flags every haul-trainable animal as a
    // pack animal so MassUtility.Capacity gives it caravan carrying capacity.
    [StaticConstructorOnStartup]
    public static class Startup
    {
        // Every haul-trainable animal that wasn't already a pack animal, including
        // the ones turned off in settings. This is what the settings list shows.
        public static readonly List<ThingDef> candidates = new List<ThingDef>();

        // Animals we flagged. They weren't pack animals, so they have no
        // "<body>Pack" texture; vanilla would build a Graphic_Multi for them
        // anyway, logging an error and drawing a pink square.
        public static readonly HashSet<ThingDef> flagged = new HashSet<ThingDef>();

        static Startup()
        {
            TrainableDef haul = DefDatabase<TrainableDef>.GetNamedSilentFail("Haul");
            if (haul == null)
            {
                return;
            }
            HashSet<string> disabled = HaulPackMod.settings.disabled;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race == null || !def.race.Animal || def.race.packAnimal)
                {
                    continue;
                }
                if (!Pawn_TrainingTracker.CanAssignToTrain(haul, def, out _).Accepted)
                {
                    continue;
                }
                candidates.Add(def);
                if (disabled.Contains(def.defName))
                {
                    continue;
                }
                def.race.packAnimal = true;
                flagged.Add(def);
            }
            candidates.SortBy(d => d.label);

            if (flagged.Count > 0)
            {
                new Harmony("DanZinagri.IfItCanHaulItCanPack").PatchAll();
            }
            Log.Message("[If It Can Haul, It Can Pack] Flagged " + flagged.Count + " haul-trainable animals as pack animals (" + (candidates.Count - flagged.Count) + " disabled in settings).");
        }
    }

    // GraphicFor only runs when a pawn's render tree is (re)built, not per frame.
    // Returning null is what vanilla already does for non-pack animals.
    // An animal with real pack art should set packAnimal in XML; we then never flag it.
    [HarmonyPatch(typeof(PawnRenderNode_AnimalPack), nameof(PawnRenderNode_AnimalPack.GraphicFor))]
    public static class PawnRenderNode_AnimalPack_GraphicFor_Patch
    {
        public static bool Prefix(Pawn pawn, ref Graphic __result)
        {
            if (Startup.flagged.Contains(pawn.def))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
