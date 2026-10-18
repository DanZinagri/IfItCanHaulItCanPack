using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace IfItCanHaulItCanPack
{
    public class HaulPackSettings : ModSettings
    {
        // defNames, so an entry survives its mod being temporarily unloaded.
        public HashSet<string> disabled = new HashSet<string>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref disabled, "disabled", LookMode.Value);
            if (disabled == null)
            {
                disabled = new HashSet<string>();
            }
        }
    }

    public class HaulPackMod : Mod
    {
        public static HaulPackSettings settings;

        private const float RowHeight = 30f;

        private class Row
        {
            public ThingDef def;
            public string label;
            public string info;
        }

        private List<Row> rows;
        private readonly List<Row> filtered = new List<Row>();
        private string search = "";
        private string appliedSearch;
        private Vector2 scroll;

        public HaulPackMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<HaulPackSettings>();
        }

        public override string SettingsCategory()
        {
            return "If It Can Haul, It Can Pack";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (rows == null)
            {
                rows = new List<Row>();
                foreach (ThingDef def in Startup.candidates)
                {
                    rows.Add(new Row
                    {
                        def = def,
                        label = def.LabelCap,
                        info = (def.race.baseBodySize * MassUtility.MassCapacityPerBodySize).ToString("0.#") + " kg  -  " + (def.modContentPack?.Name ?? "?")
                    });
                }
            }
            if (appliedSearch != search)
            {
                appliedSearch = search;
                filtered.Clear();
                string needle = search.ToLowerInvariant();
                foreach (Row row in rows)
                {
                    if (needle.Length == 0 || row.label.ToLowerInvariant().Contains(needle) || row.info.ToLowerInvariant().Contains(needle))
                    {
                        filtered.Add(row);
                    }
                }
            }

            Rect header = new Rect(inRect.x, inRect.y, inRect.width, 44f);
            Text.Font = GameFont.Small;
            Widgets.Label(header, "Ticked animals become pack animals. Animals that are already pack animals are not listed.\nChanges take effect after restarting the game.");

            Rect bar = new Rect(inRect.x, header.yMax + 4f, inRect.width, 28f);
            search = Widgets.TextField(new Rect(bar.x, bar.y, 240f, bar.height), search);
            if (Widgets.ButtonText(new Rect(bar.xMax - 250f, bar.y, 120f, bar.height), "Enable all"))
            {
                foreach (Row row in filtered)
                {
                    settings.disabled.Remove(row.def.defName);
                }
            }
            if (Widgets.ButtonText(new Rect(bar.xMax - 120f, bar.y, 120f, bar.height), "Disable all"))
            {
                foreach (Row row in filtered)
                {
                    settings.disabled.Add(row.def.defName);
                }
            }

            Rect outRect = new Rect(inRect.x, bar.yMax + 6f, inRect.width, inRect.yMax - bar.yMax - 6f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, filtered.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);
            // Only the rows on screen are drawn.
            int first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / RowHeight));
            int last = Mathf.Min(filtered.Count - 1, Mathf.CeilToInt((scroll.y + outRect.height) / RowHeight));
            for (int i = first; i <= last; i++)
            {
                Row row = filtered[i];
                Rect rect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                if (i % 2 == 1)
                {
                    Widgets.DrawLightHighlight(rect);
                }
                Widgets.DrawHighlightIfMouseover(rect);

                Widgets.DefIcon(new Rect(rect.x + 2f, rect.y + 1f, RowHeight - 2f, RowHeight - 2f), row.def);
                GUI.color = Color.white;

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(rect.x + RowHeight + 6f, rect.y, 260f, RowHeight), row.label);
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x + RowHeight + 270f, rect.y, rect.width - RowHeight - 310f, RowHeight), row.info);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                bool enabled = !settings.disabled.Contains(row.def.defName);
                bool wasEnabled = enabled;
                Widgets.Checkbox(rect.xMax - 28f, rect.y + 3f, ref enabled);
                if (enabled != wasEnabled)
                {
                    if (enabled)
                    {
                        settings.disabled.Remove(row.def.defName);
                    }
                    else
                    {
                        settings.disabled.Add(row.def.defName);
                    }
                }
            }
            Widgets.EndScrollView();
        }
    }
}
