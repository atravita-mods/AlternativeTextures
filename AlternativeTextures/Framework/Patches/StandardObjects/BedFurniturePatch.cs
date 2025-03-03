﻿using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Objects;

using System;

namespace AlternativeTextures.Framework.Patches.StandardObjects
{
    internal class BedFurniturePatch : PatchTemplate
    {
        private readonly Type _object = typeof(BedFurniture);

        internal BedFurniturePatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(BedFurniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));

            if (PatchTemplate.IsDGAUsed())
            {
                try
                {
                    if (Type.GetType("DynamicGameAssets.Game.CustomBedFurniture, DynamicGameAssets") is Type dgaBedFurnitureType && dgaBedFurnitureType != null)
                    {
                        harmony.Patch(AccessTools.Method(dgaBedFurnitureType, nameof(BedFurniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));
                    }
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Failed to patch Dynamic Game Assets in {this.GetType().Name}: AT may not be able to override certain DGA object types!", LogLevel.Warn);
                    _monitor.Log($"Patch for DGA failed in {this.GetType().Name}: {ex}", LogLevel.Trace);
                }
            }
        }

        private static bool DrawPrefix(BedFurniture __instance, NetVector2 ___drawPosition, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            if (__instance.modData.TryGetValue("AlternativeTextureName", out string textureId))
            {
                var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(textureId);
                if (textureModel is null)
                {
                    return true;
                }

                var textureVariation = Int32.Parse(__instance.modData["AlternativeTextureVariation"]);
                if (textureVariation == -1 || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation))
                {
                    return true;
                }
                var textureOffset = textureModel.GetTextureOffset(textureVariation);

                if (!__instance.isTemporarilyInvisible)
                {
                    if (Furniture.isDrawingLocationFurniture)
                    {

                        Rectangle sourceRect = __instance.sourceRect.Value;
                        sourceRect.X -= __instance.defaultSourceRect.X;
                        sourceRect.Y = textureOffset;

                        spriteBatch.Draw(textureModel.GetTexture(textureVariation), Game1.GlobalToLocal(Game1.viewport, ___drawPosition + ((__instance.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero)), sourceRect, Color.White * alpha, 0f, Vector2.Zero, 4f, __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (__instance.boundingBox.Value.Top + 1) / 10000f);
                        sourceRect.X += sourceRect.Width;
                        spriteBatch.Draw(textureModel.GetTexture(textureVariation), Game1.GlobalToLocal(Game1.viewport, ___drawPosition + ((__instance.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero)), sourceRect, Color.White * alpha, 0f, Vector2.Zero, 4f, __instance.flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (__instance.boundingBox.Value.Bottom - 1) / 10000f);
                    }
                    else
                    {
                        __instance.draw(spriteBatch, x, y, alpha);
                    }
                }
                return false;
            }
            return true;
        }
    }
}
