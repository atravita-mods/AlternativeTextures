using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities.Extensions;

using HarmonyLib;

using StardewModdingAPI;

using StardewValley;
using StardewValley.TerrainFeatures;

using System;

namespace AlternativeTextures.Framework.Patches.StandardObjects
{
    internal class HoeDirtPatch : PatchTemplate
    {
        private readonly Type _object = typeof(HoeDirt);

        internal HoeDirtPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(HoeDirt.plant), new[] { typeof(int), typeof(int), typeof(int), typeof(Farmer), typeof(bool), typeof(GameLocation) }), postfix: new HarmonyMethod(GetType(), nameof(PlantPostfix)));
        }

        private static void PlantPostfix(HoeDirt __instance, int index)
        {
            var instanceName = index.GetObjectNameFromID().ToString();
            instanceName = $"{AlternativeTextureModel.TextureType.Crop}_{instanceName}";
            var instanceSeasonName = $"{instanceName}_{Game1.GetSeasonForLocation(__instance.currentLocation)}";

            if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceName) && AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceSeasonName))
            {
                _ = Game1.random.Next(2) > 0 ? AssignModData(__instance, instanceSeasonName, true) : AssignModData(__instance, instanceName, false);
                return;
            }
            else
            {
                if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceName))
                {
                    AssignModData(__instance, instanceName, false);
                    return;
                }

                if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceSeasonName))
                {
                    AssignModData(__instance, instanceSeasonName, true);
                    return;
                }
            }

            AssignDefaultModData(__instance, instanceSeasonName, true);
        }
    }
}
