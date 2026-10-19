// ============================================================================
// Crops.cs - Procedural Vegetable Crop Spawner Generation
//
// Origin: TheForging by Triberius-Rex
// Source: https://github.com/Triberius-Rex/TheForging/blob/main/Scripts/Custom/Crops.cs
//
// Adapted for FesterUO:
// - Decoupled hardcoded spawner timers into Config/FesterUO/Crops.cfg
//   (MinRespawnMinutes=6.0, MaxRespawnMinutes=12.0) to eliminate cosmetic overlap
//   with ServUO's 5.0-minute picked crop stub decay delay.
// - Parameterized configurable crop types in Crops.cfg with compile-time
//   nameof(...) type validation for default fallback types.
// - Parameterized per-tile field spawn probability (SpawnChance=0.50).
// - Parameterized per-facet map enablement via Crops.cfg (Trammel, and
//   Tokuno enabled by default; Felucca, Ilshenar, Malas, and TerMur disabled).
// - Added boundary checks for map tile queries and safe IPooledEnumerable cleanup.
// ============================================================================

using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

namespace Server.Misc
{
    public class GenCrops
    {
        private static readonly string[] DefaultCropTypes = new string[]
        {
            nameof(FarmableCarrot), nameof(FarmableCabbage), nameof(FarmableLettuce),
            nameof(FarmableOnion),  nameof(FarmablePumpkin), nameof(FarmableCotton),
            nameof(FarmableFlax),   nameof(FarmableTurnip),  nameof(FarmableWheat)
        };

        private const int NPCCount = 1;
        private const int Team = 0;
        private const int HomeRange = 0;
        private const int SpawnRange = 0;
        private const bool TotalRespawn = true;

        public static TimeSpan MinDelay => TimeSpan.FromMinutes(Math.Max(0.1, Config.Get("Crops.MinRespawnMinutes", 6.0)));
        public static TimeSpan MaxDelay => TimeSpan.FromMinutes(Math.Max(MinDelay.TotalMinutes, Config.Get("Crops.MaxRespawnMinutes", 12.0)));
        public static double SpawnChance => Math.Max(0.0, Math.Min(1.0, Config.Get("Crops.SpawnChance", 0.50)));

        public static List<string> GetCropTypes()
        {
            string raw = Config.Get("Crops.CropTypes", string.Empty);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                List<string> validated = new List<string>();
                string[] entries = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string rawEntry in entries)
                {
                    string entry = rawEntry.Trim();
                    if (string.IsNullOrEmpty(entry))
                        continue;

                    Type t = ScriptCompiler.FindTypeByName(entry);
                    if (t != null && typeof(Item).IsAssignableFrom(t))
                    {
                        validated.Add(entry);
                    }
                    else
                    {
                        Console.WriteLine($"[Crops] Warning: Unknown or invalid crop type '{entry}' in Crops.cfg; skipping.");
                    }
                }

                if (validated.Count > 0)
                    return validated;
            }

            return new List<string>(DefaultCropTypes);
        }

        public static void Initialize()
        {
            CommandSystem.Register("GenCrops", AccessLevel.Administrator, new CommandEventHandler(Generate_OnCommand));
        }

        [Usage("GenCrops")]
        [Description("Generates vegetable spawners on dirt tiles across all maps")]
        private static void Generate_OnCommand(CommandEventArgs e)
        {
            Parse(e.Mobile);
        }

        public static void Parse(Mobile from)
        {
            List<Map> maps = new List<Map>();

            if (Config.Get("Crops.SpawnTrammel", true)) maps.Add(Map.Trammel);
            if (Config.Get("Crops.SpawnFelucca", true)) maps.Add(Map.Felucca);
            if (Config.Get("Crops.SpawnIlshenar", false)) maps.Add(Map.Ilshenar);
            if (Config.Get("Crops.SpawnMalas", false)) maps.Add(Map.Malas);
            if (Config.Get("Crops.SpawnTokuno", true)) maps.Add(Map.Tokuno);
            if (Config.Get("Crops.SpawnTerMur", false)) maps.Add(Map.TerMur);

            if (maps.Count == 0)
            {
                from?.SendMessage("No maps enabled for crop generation in Crops.cfg.");
                return;
            }

            from?.SendMessage($"Generating vegetable spawners on dirt tiles (respawn: {MinDelay.TotalMinutes:F1} - {MaxDelay.TotalMinutes:F1} min, density: {SpawnChance * 100:F0}%, maps: {string.Join(", ", maps)})...");

            foreach (Map map in maps)
            {
                GenerateCropsOnMap(from, map);
            }

            from?.SendMessage("Vegetable generation complete on all enabled maps.");
            Console.WriteLine("Vegetable generation complete on all enabled maps.");
        }

        public static void GenerateCropsOnMap(Mobile from, Map map)
        {
            int mapWidth = map.Width;
            int mapHeight = map.Height;
            Random random = new Random();
            int spawnerCount = 0;
            int cropIndex = 0;
            double spawnChance = SpawnChance;
            List<string> cropTypes = GetCropTypes();

            HashSet<Point2D> visited = new HashSet<Point2D>();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Point2D point = new Point2D(x, y);
                    if (!visited.Contains(point) && IsDirtTile(map, x, y))
                    {
                        List<Point2D> field = new List<Point2D>();
                        FindField(map, x, y, visited, field);

                        if (field.Count > 0)
                        {
                            string cropType = cropTypes[cropIndex];
                            cropIndex = (cropIndex + 1) % cropTypes.Count; // Move to the next crop type

                            foreach (var tile in field)
                            {
                                // Configurable chance to place a spawner on this tile
                                if (random.NextDouble() < spawnChance)
                                {
                                    MakeSpawner(new string[] { cropType }, tile.X, tile.Y, map.Tiles.GetLandTile(tile.X, tile.Y).Z, map);
                                    spawnerCount++;
                                }
                            }
                        }
                    }
                }
            }

            from?.SendMessage(String.Format("{0} vegetable spawners generated on {1}.", spawnerCount, map.Name));
            Console.WriteLine(String.Format("{0} vegetable spawners generated on {1}.", spawnerCount, map.Name));
        }

        private static bool IsDirtTile(Map map, int x, int y)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);
            return landTile.ID == 0x9;
        }

        private static void FindField(Map map, int x, int y, HashSet<Point2D> visited, List<Point2D> field)
        {
            Stack<Point2D> stack = new Stack<Point2D>();
            stack.Push(new Point2D(x, y));

            while (stack.Count > 0)
            {
                Point2D point = stack.Pop();
                if (!visited.Contains(point) && IsDirtTile(map, point.X, point.Y))
                {
                    visited.Add(point);
                    field.Add(point);

                    foreach (var neighbor in GetNeighbors(point))
                    {
                        if (!visited.Contains(neighbor) && IsDirtTile(map, neighbor.X, neighbor.Y))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }

        private static IEnumerable<Point2D> GetNeighbors(Point2D point)
        {
            yield return new Point2D(point.X + 1, point.Y);
            yield return new Point2D(point.X - 1, point.Y);
            yield return new Point2D(point.X, point.Y + 1);
            yield return new Point2D(point.X, point.Y - 1);
        }

        private static void MakeSpawner(string[] types, int x, int y, int z, Map map)
        {
            if (types.Length == 0)
                return;

            ClearSpawners(x, y, z, map);

            for (int i = 0; i < types.Length; ++i)
            {
                XmlSpawner xs = new XmlSpawner(types[i]);

                xs.MaxCount = NPCCount;
                xs.MinDelay = MinDelay;
                xs.MaxDelay = MaxDelay;
                xs.Team = Team;
                xs.HomeRange = HomeRange;
                xs.SpawnRange = SpawnRange;

                xs.MoveToWorld(new Point3D(x, y, z), map);

                if (TotalRespawn)
                {
                    xs.Respawn();
                    xs.BringToHome();
                }
            }
        }

        private static void ClearSpawners(int x, int y, int z, Map map)
        {
            List<Item> toDelete = null;
            IPooledEnumerable eable = map.GetItemsInRange(new Point3D(x, y, z), 0);
            foreach (Item item in eable)
            {
                if (item is XmlSpawner)
                {
                    if (toDelete == null)
                        toDelete = new List<Item>();
                    toDelete.Add(item);
                }
            }
            eable.Free();

            if (toDelete != null)
            {
                for (int i = 0; i < toDelete.Count; i++)
                {
                    toDelete[i].Delete();
                }
            }
        }
    }
}
