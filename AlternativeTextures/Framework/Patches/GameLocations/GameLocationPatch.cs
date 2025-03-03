﻿using AlternativeTextures;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities.Extensions;

using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches.GameLocations
{
    internal class GameLocationPatch : PatchTemplate
    {
        private readonly Type _object = typeof(GameLocation);

        internal GameLocationPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.checkAction), new[] { typeof(xTile.Dimensions.Location), typeof(xTile.Dimensions.Rectangle), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(CheckActionPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.LowPriorityLeftClick), new[] { typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(LowPriorityLeftClickPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.leftClick), new[] { typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(LowPriorityLeftClickPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(GameLocation.seasonUpdate), new[] { typeof(string), typeof(bool) }), postfix: new HarmonyMethod(GetType(), nameof(SeasonUpdatePostfix)));
        }

        private static bool CheckActionPrefix(GameLocation __instance, ref bool __result, xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            if (Game1.didPlayerJustRightClick())
            {
                return true;
            }

            if (who.CurrentTool is GenericTool tool && (tool.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG) || tool.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG) || tool.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG)))
            {
                Vector2 position = ((!Game1.wasMouseVisibleThisFrame) ? Game1.player.GetToolLocation() : new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y));
                tool.beginUsing(__instance, (int)position.X, (int)position.Y, who);
                __result = false;
                return false;
            }

            return true;
        }

        private static bool LowPriorityLeftClickPrefix(GameLocation __instance, ref bool __result, int x, int y, Farmer who)
        {
            if (who.CurrentTool is GenericTool tool && (tool.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG) || tool.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG) || tool.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG)))
            {
                __result = false;
                return false;
            }

            return true;
        }

        internal static void SeasonUpdatePostfix(GameLocation __instance, string season, bool onLoad = false)
        {
            if (__instance is null)
            {
                return;
            }

            if (__instance.objects != null)
            {
                for (int k = __instance.objects.Count() - 1; k >= 0; k--)
                {
                    var obj = __instance.objects.Pairs.ElementAt(k).Value;
                    if (obj.modData.ContainsKey("AlternativeTextureOwner") && obj.modData.ContainsKey("AlternativeTextureName"))
                    {
                        var instanceName = GetObjectName(obj);
                        if (obj is Fence fence && fence.isGate.Value)
                        {
                            instanceName = Game1.objectInformation[325].GetNthChunk('/', Object.objectInfoNameIndex).ToString();
                        }

                        var seasonalName = String.Concat(obj.modData["AlternativeTextureOwner"], ".", $"{AlternativeTextureModel.TextureType.Craftable}_{instanceName}_{season}");
                        if ((obj.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(obj.modData["AlternativeTextureSeason"]) && !String.Equals(obj.modData["AlternativeTextureSeason"], Game1.currentSeason, StringComparison.OrdinalIgnoreCase)) || AlternativeTextures.textureManager.DoesObjectHaveAlternativeTextureById(seasonalName))
                        {
                            obj.modData["AlternativeTextureSeason"] = season;
                            obj.modData["AlternativeTextureName"] = seasonalName;
                        }
                    }
                }
            }

            if (__instance.characters is not null)
            {
                for (int k = __instance.characters.Count() - 1; k >= 0; k--)
                {
                    var character = __instance.characters.ElementAt(k);
                    if (character.modData.ContainsKey("AlternativeTextureOwner") && character.modData.ContainsKey("AlternativeTextureName"))
                    {
                        var instanceName = GetCharacterName(character);

                        var seasonalName = String.Concat(character.modData["AlternativeTextureOwner"], ".", $"{AlternativeTextureModel.TextureType.Character}_{instanceName}_{season}");
                        if ((character.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(character.modData["AlternativeTextureSeason"]) && !String.Equals(character.modData["AlternativeTextureSeason"], Game1.currentSeason, StringComparison.OrdinalIgnoreCase)) || AlternativeTextures.textureManager.DoesObjectHaveAlternativeTextureById(seasonalName))
                        {
                            character.modData["AlternativeTextureSeason"] = season;
                            character.modData["AlternativeTextureName"] = seasonalName;
                        }
                    }
                }
            }

            // Check for animals, if __instance is an applicable location
            if ((__instance is Farm farm && farm.animals != null) || (__instance is AnimalHouse animalHouse && animalHouse.animals != null))
            {
                var animals = __instance is Farm ? (__instance as Farm).animals.Values : (__instance as AnimalHouse).animals.Values;
                for (int k = animals.Count() - 1; k >= 0; k--)
                {
                    var farmAnimal = animals.ElementAt(k);
                    if (farmAnimal.modData.ContainsKey("AlternativeTextureOwner") && farmAnimal.modData.ContainsKey("AlternativeTextureName"))
                    {
                        var instanceName = GetCharacterName(farmAnimal);

                        var seasonalName = String.Concat(farmAnimal.modData["AlternativeTextureOwner"], ".", $"{AlternativeTextureModel.TextureType.Character}_{instanceName}_{season}");
                        if ((farmAnimal.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(farmAnimal.modData["AlternativeTextureSeason"]) && !String.Equals(farmAnimal.modData["AlternativeTextureSeason"], Game1.currentSeason, StringComparison.OrdinalIgnoreCase)) || AlternativeTextures.textureManager.DoesObjectHaveAlternativeTextureById(seasonalName))
                        {
                            farmAnimal.modData["AlternativeTextureSeason"] = season;
                            farmAnimal.modData["AlternativeTextureName"] = seasonalName;
                        }
                    }
                }
            }

            if (__instance is Farm houseFarm && __instance.modData.ContainsKey("AlternativeTextureOwner") is true && __instance.modData.ContainsKey("AlternativeTextureName") is true)
            {
                var buildingType = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
                if (!__instance.modData["AlternativeTextureName"].Contains(buildingType))
                {
                    return;
                }

                var instanceName = String.Concat(__instance.modData["AlternativeTextureOwner"], ".", $"{AlternativeTextureModel.TextureType.Building}_{buildingType}");
                var instanceSeasonName = $"{instanceName}_{Game1.currentSeason}";

                if (!String.Equals(__instance.modData["AlternativeTextureName"], instanceName, StringComparison.OrdinalIgnoreCase) && !String.Equals(__instance.modData["AlternativeTextureName"], instanceSeasonName, StringComparison.OrdinalIgnoreCase))
                {                    
                    __instance.modData["AlternativeTextureName"] = String.Concat(__instance.modData["AlternativeTextureOwner"], ".", $"{AlternativeTextureModel.TextureType.Building}_{buildingType}");
                    if (__instance.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(__instance.modData["AlternativeTextureSeason"]))
                    {
                        __instance.modData["AlternativeTextureSeason"] = Game1.currentSeason;
                        __instance.modData["AlternativeTextureName"] = String.Concat(__instance.modData["AlternativeTextureName"], "_", __instance.modData["AlternativeTextureSeason"]);

                        houseFarm.houseSource.Value = new Rectangle(0, 144 * (((int)Game1.MasterPlayer.HouseUpgradeLevel == 3) ? 2 : ((int)Game1.MasterPlayer.HouseUpgradeLevel)), 160, 144);
                        houseFarm.ApplyHousePaint();
                    }
                }
            }
        }
    }
}