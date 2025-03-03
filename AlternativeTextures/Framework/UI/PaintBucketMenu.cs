﻿using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.Buildings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.UI
{
    internal class PaintBucketMenu : IClickableMenu
    {
        public ClickableTextureComponent hovered;
        public ClickableTextureComponent forwardButton;
        public ClickableTextureComponent backButton;
        public ClickableTextureComponent queryButton;

        public List<Item> filteredTextureOptions = new List<Item>();
        public List<Item> cachedTextureOptions = new List<Item>();
        public List<ClickableTextureComponent> availableTextures = new List<ClickableTextureComponent>();

        // Textbox
        protected TextBox _searchBox;
        protected ClickableComponent _searchBoxCC;

        protected string _title;
        protected string _cachedTextBoxValue;

        protected int _startingRow = 0;
        protected int _texturesPerRow = 6;
        protected int _maxRows = 4;
        protected float _buildingScale = 3f;

        protected string _modelName;
        protected Object _textureTarget;
        protected Vector2 _position;
        protected TextureType _textureType;

        private bool _isSprayCan;
        protected Dictionary<string, SelectedTextureModel> _selectedIdsToModels;

        public PaintBucketMenu(Object target, Vector2 position, TextureType textureType, string modelName, string uiTitle = "Paint Bucket", int textureTileWidth = -1, bool isSprayCan = false) : base(0, 0, 832, 576, showUpperRightCloseButton: true)
        {
            if (!target.modData.ContainsKey("AlternativeTextureOwner") || !target.modData.ContainsKey("AlternativeTextureName"))
            {
                this.exitThisMenu();
                return;
            }
            _title = uiTitle;
            _isSprayCan = isSprayCan;

            // Set up menu structure
            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.fr)
            {
                base.height += 64;
            }

            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
            base.xPositionOnScreen = (int)topLeft.X;
            base.yPositionOnScreen = (int)topLeft.Y;

            // Populate the texture selection components
            var availableModels = AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation));
            for (int m = 0; m < availableModels.Count; m++)
            {
                var manualVariations = availableModels[m].ManualVariations.Where(v => v.Id != -1).ToList();
                if (manualVariations.Count() > 0)
                {
                    for (int v = 0; v < manualVariations.Count(); v++)
                    {
                        var objectWithVariation = target.getOne();
                        objectWithVariation.modData["AlternativeTextureOwner"] = availableModels[m].Owner;
                        objectWithVariation.modData["AlternativeTextureName"] = availableModels[m].GetId();
                        objectWithVariation.modData["AlternativeTextureVariation"] = manualVariations[v].Id.ToString();
                        objectWithVariation.modData["AlternativeTextureSeason"] = availableModels[m].Season;
                        objectWithVariation.modData["AlternativeTextureDisplayName"] = manualVariations[v].Name;

                        if (AlternativeTextures.modConfig.IsTextureVariationDisabled(objectWithVariation.modData["AlternativeTextureName"], manualVariations[v].Id))
                        {
                            continue;
                        }

                        if (target is Furniture furniture)
                        {
                            (objectWithVariation as Furniture).currentRotation.Value = furniture.currentRotation;
                            (objectWithVariation as Furniture).updateRotation();
                        }

                        this.filteredTextureOptions.Add(objectWithVariation);
                        this.cachedTextureOptions.Add(objectWithVariation);
                    }
                }
                else
                {
                    for (int v = 0; v < availableModels[m].Variations; v++)
                    {
                        var objectWithVariation = target.getOne();
                        objectWithVariation.modData["AlternativeTextureOwner"] = availableModels[m].Owner;
                        objectWithVariation.modData["AlternativeTextureName"] = availableModels[m].GetId();
                        objectWithVariation.modData["AlternativeTextureVariation"] = v.ToString();
                        objectWithVariation.modData["AlternativeTextureSeason"] = availableModels[m].Season;
                        objectWithVariation.modData["AlternativeTextureDisplayName"] = String.Empty;

                        if (AlternativeTextures.modConfig.IsTextureVariationDisabled(objectWithVariation.modData["AlternativeTextureName"], v))
                        {
                            continue;
                        }

                        if (target is Furniture furniture)
                        {
                            (objectWithVariation as Furniture).currentRotation.Value = furniture.currentRotation;
                            (objectWithVariation as Furniture).updateRotation();
                        }

                        this.filteredTextureOptions.Add(objectWithVariation);
                        this.cachedTextureOptions.Add(objectWithVariation);
                    }
                }
            }

            // Add the vanilla version
            if (textureType is TextureType.Decoration)
            {
                int index = 0;
                foreach (Wallpaper decoration in Utility.getAllWallpapersAndFloorsForFree().Keys.Where(d => d is Wallpaper wallpaper && wallpaper.isFloor.Value == modelName.Contains("Floor")))
                {
                    if (!String.IsNullOrEmpty(decoration.modDataID.Value))
                    {
                        continue;
                    }

                    decoration.modData["AlternativeTextureOwner"] = AlternativeTextures.DEFAULT_OWNER;
                    decoration.modData["AlternativeTextureName"] = $"{decoration.modData["AlternativeTextureOwner"]}.{modelName}";
                    decoration.modData["AlternativeTextureVariation"] = decoration.ParentSheetIndex.ToString();
                    decoration.modData["AlternativeTextureSeason"] = String.Empty;

                    if (AlternativeTextures.modConfig.IsTextureVariationDisabled(decoration.modData["AlternativeTextureName"], decoration.ParentSheetIndex))
                    {
                        continue;
                    }

                    this.filteredTextureOptions.Insert(index, decoration);
                    this.cachedTextureOptions.Insert(index, decoration);

                    index++;
                }
            }
            else
            {
                var vanillaObject = target.getOne();
                vanillaObject.modData["AlternativeTextureOwner"] = AlternativeTextures.DEFAULT_OWNER;
                vanillaObject.modData["AlternativeTextureName"] = $"{vanillaObject.modData["AlternativeTextureOwner"]}.{modelName}";
                vanillaObject.modData["AlternativeTextureVariation"] = $"{-1}";
                vanillaObject.modData["AlternativeTextureSeason"] = String.Empty;

                if (target is Furniture)
                {
                    (vanillaObject as Furniture).currentRotation.Value = (target as Furniture).currentRotation;
                    (vanillaObject as Furniture).updateRotation();
                }

                this.filteredTextureOptions.Insert(0, vanillaObject);
                this.cachedTextureOptions.Insert(0, vanillaObject);
            }

            _textureType = textureType;

            var drawingScale = 4f;
            var widthOffsetScale = 2;
            var xOffset = 0;
            var sourceRect = GetSourceRectangle(availableModels.First(), target, availableModels.First().TextureWidth, availableModels.First().TextureHeight, -1);
            switch (_textureType)
            {
                case TextureType.Craftable:
                    if (sourceRect.Height <= 16)
                    {
                        _maxRows = 8;
                    }
                    break;
                case TextureType.Flooring:
                    sourceRect = new Rectangle(0, 0, 16, 32);
                    break;
                case TextureType.Character:
                    sourceRect = new Rectangle(0, 0, 32, 32);
                    break;
                case TextureType.Tree:
                    _maxRows = 1;
                    _texturesPerRow = 3;
                    widthOffsetScale = 4;
                    sourceRect = new Rectangle(0, 0, 48, 96);
                    break;
                case TextureType.FruitTree:
                    _maxRows = 1;
                    _texturesPerRow = 3;
                    widthOffsetScale = 4;
                    sourceRect = new Rectangle(0, 0, 48, 80);
                    break;
                case TextureType.Crop:
                    _maxRows = 4;
                    _texturesPerRow = 1;
                    widthOffsetScale = 4;
                    xOffset = 96;
                    sourceRect = new Rectangle(0, 0, 128, 32);
                    break;
                case TextureType.Grass:
                    _maxRows = 6;
                    _texturesPerRow = 4;
                    widthOffsetScale = 3;
                    xOffset = 32;
                    sourceRect = new Rectangle(0, 0, 15, 20);
                    break;
                case TextureType.Bush:
                    _maxRows = 6;
                    _texturesPerRow = 4;
                    widthOffsetScale = 3;
                    xOffset = 32;
                    sourceRect = new Rectangle(0, 0, 15, 20);
                    break;
                case TextureType.Furniture:
                    if (sourceRect.Height >= 64)
                    {
                        _maxRows = 2;
                    }
                    else if (sourceRect.Height >= 32)
                    {
                        _maxRows = 3;
                    }
                    else if (sourceRect.Height <= 16)
                    {
                        sourceRect.Height = 32;
                    }

                    break;
                case TextureType.Building:
                    _maxRows = 1;
                    _texturesPerRow = 3;
                    widthOffsetScale = 4;
                    sourceRect = new Rectangle(0, 0, 48, 160);

                    switch (textureTileWidth)
                    {
                        case int w when w > 4 && w < 8:
                            _buildingScale = 2f;
                            break;
                        case int w when w >= 8:
                            _buildingScale = 1f;
                            break;
                    }

                    drawingScale = _buildingScale;
                    break;
                case TextureType.Decoration:
                    widthOffsetScale = 3;
                    _texturesPerRow = 4;
                    _maxRows = 2;
                    sourceRect = new Rectangle(0, 0, 32, 64);
                    break;
            }

            for (int r = 0; r < _maxRows; r++)
            {
                for (int c = 0; c < _texturesPerRow; c++)
                {
                    var componentId = c + r * _texturesPerRow;
                    this.availableTextures.Add(new ClickableTextureComponent(new Rectangle(base.xPositionOnScreen + IClickableMenu.borderWidth + componentId % _texturesPerRow * 64 * widthOffsetScale + xOffset, base.yPositionOnScreen + sourceRect.Height + componentId / _texturesPerRow * (4 * sourceRect.Height), 4 * sourceRect.Width, 4 * sourceRect.Height), availableModels.First().GetTexture(0), new Rectangle(), drawingScale, false)
                    {
                        myID = componentId,
                        downNeighborID = componentId + _texturesPerRow,
                        upNeighborID = r >= _texturesPerRow ? componentId - _texturesPerRow : -1,
                        rightNeighborID = c == 5 ? 9997 : componentId + 1,
                        leftNeighborID = c > 0 ? componentId - 1 : 9998
                    });
                }
            }

            if (isSprayCan)
            {
                _selectedIdsToModels = new Dictionary<string, SelectedTextureModel>();
                if (Game1.player.modData.ContainsKey(AlternativeTextures.ENABLED_SPRAY_CAN_TEXTURES) && String.IsNullOrEmpty(Game1.player.modData[AlternativeTextures.ENABLED_SPRAY_CAN_TEXTURES]) is false)
                {
                    _selectedIdsToModels = JsonConvert.DeserializeObject<Dictionary<string, SelectedTextureModel>>(Game1.player.modData[AlternativeTextures.ENABLED_SPRAY_CAN_TEXTURES]);
                }
            }

            // Cache the input object to easily reference the vanilla texture
            _modelName = modelName;
            _textureTarget = target;
            _position = position;

            this.backButton = new ClickableTextureComponent(new Rectangle(base.xPositionOnScreen - 64, base.yPositionOnScreen + 8, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
            {
                myID = 9998,
                rightNeighborID = 0
            };
            this.forwardButton = new ClickableTextureComponent(new Rectangle(base.xPositionOnScreen + base.width + 64 - 48, base.yPositionOnScreen + base.height - 48, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
            {
                myID = 9997
            };

            // Textbox related
            var xTextbox = base.xPositionOnScreen + 64 + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth + 320;
            var yTextbox = base.yPositionOnScreen - 58;
            _searchBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
            {
                X = xTextbox,
                Y = yTextbox,
                Width = 384,
                limitWidth = false,
                Text = String.Empty
            };

            _searchBoxCC = new ClickableComponent(new Rectangle(xTextbox, yTextbox, 192, 48), "")
            {
                myID = 9999,
                upNeighborID = -99998,
                leftNeighborID = -99998,
                rightNeighborID = -99998,
                downNeighborID = -99998
            };
            Game1.keyboardDispatcher.Subscriber = this._searchBox;
            _searchBox.Selected = true;

            this.queryButton = new ClickableTextureComponent(new Rectangle(xTextbox - 32, base.yPositionOnScreen - 48, 48, 44), Game1.mouseCursors, new Rectangle(208, 320, 16, 16), 2f)
            {
                myID = -1
            };

            // Call snap functions
            if (Game1.options.SnappyMenus)
            {
                base.populateClickableComponentList();
                this.snapToDefaultClickableComponent();
            }
        }

        public override void performHoverAction(int x, int y)
        {
            this.hovered = null;
            if (Game1.IsFading())
            {
                return;
            }

            var maxScale = _textureType == TextureType.Building ? _buildingScale : 4f;
            foreach (ClickableTextureComponent c in this.availableTextures)
            {
                if (c.containsPoint(x, y))
                {
                    c.scale = Math.Min(c.scale + 0.05f, maxScale + 0.1f);
                    this.hovered = c;
                }
                else
                {
                    c.scale = Math.Max(maxScale, c.scale - 0.025f);
                }
            }

            this.forwardButton.tryHover(x, y, 0.2f);
            this.backButton.tryHover(x, y, 0.2f);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape)
            {
                base.receiveKeyPress(key);
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (_searchBox.Text != _cachedTextBoxValue)
            {
                _startingRow = 0;
                _cachedTextBoxValue = _searchBox.Text;

                if (String.IsNullOrEmpty(_searchBox.Text))
                {
                    filteredTextureOptions = cachedTextureOptions;
                }
                else
                {
                    filteredTextureOptions = cachedTextureOptions.Where(i => !i.modData["AlternativeTextureName"].Contains(AlternativeTextures.DEFAULT_OWNER) && AlternativeTextures.textureManager.GetSpecificTextureModel(i.modData["AlternativeTextureName"]) is AlternativeTextureModel model && model.HasKeyword(i.modData["AlternativeTextureVariation"], _searchBox.Text)).ToList();
                }
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = false)
        {
            base.receiveLeftClick(x, y, playSound);
            if (Game1.activeClickableMenu == null)
            {
                return;
            }

            foreach (ClickableTextureComponent c in this.availableTextures)
            {
                if (c.containsPoint(x, y) && c.item != null)
                {
                    if (_textureType is TextureType.Character && PatchTemplate.GetCharacterAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Character character && character != null)
                    {
                        foreach (string key in c.item.modData.Keys)
                        {
                            character.modData[key] = c.item.modData[key];
                        }
                    }
                    else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
                    {
                        foreach (string key in c.item.modData.Keys)
                        {
                            _textureTarget.modData[key] = c.item.modData[key];
                        }
                    }
                    else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is TerrainFeature feature)
                    {
                        foreach (string key in c.item.modData.Keys)
                        {
                            feature.modData[key] = c.item.modData[key];
                        }
                    }
                    else if (PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Building building)
                    {
                        foreach (string key in c.item.modData.Keys)
                        {
                            building.modData[key] = c.item.modData[key];
                        }

                        building.resetTexture();

                        if (building is ShippingBin shippingBin && shippingBin.modData["AlternativeTextureOwner"] == AlternativeTextures.DEFAULT_OWNER)
                        {
                            shippingBin.initLid();
                        }
                    }
                    else if (Game1.currentLocation is Farm farm && farm.GetHouseRect().Contains(new Vector2(_position.X, _position.Y) / 64))
                    {
                        foreach (string key in c.item.modData.Keys)
                        {
                            farm.modData[key] = c.item.modData[key];
                        }

                        farm.houseSource.Value = new Rectangle(0, 144 * (((int)Game1.MasterPlayer.houseUpgradeLevel == 3) ? 2 : ((int)Game1.MasterPlayer.houseUpgradeLevel)), 160, 144);
                        farm.ApplyHousePaint();
                    }
                    else if (Game1.currentLocation is DecoratableLocation decoratableLocation && (decoratableLocation.getFloorAt(new Point((int)_position.X, (int)_position.Y)) != -1 || decoratableLocation.getWallForRoomAt(new Point((int)_position.X, (int)_position.Y)) != -1))
                    {
                        var room = 0;
                        var isFloor = _modelName.Contains("Floor");
                        if (isFloor)
                        {
                            room = decoratableLocation.getFloorAt(new Point((int)_position.X, (int)_position.Y));
                        }
                        else
                        {
                            room = decoratableLocation.getWallForRoomAt(new Point((int)_position.X, (int)_position.Y));
                        }

                        if (room != -1)
                        {
                            int variation = Int32.Parse(c.item.modData["AlternativeTextureVariation"]);
                            var decorationKey = c.item.modData["AlternativeTextureOwner"] == AlternativeTextures.DEFAULT_OWNER ? variation.ToString() : $"{c.item.modData["AlternativeTextureName"]}:{variation}";
                            if (isFloor)
                            {
                                if (variation == -1)
                                {
                                    decorationKey = decoratableLocation.GetFirstFlooringTile().ToString();
                                }
                                decoratableLocation.SetFloor(decorationKey, decoratableLocation.floorIDs[room]);
                            }
                            else
                            {
                                if (variation == -1)
                                {
                                    decorationKey = "0";
                                }
                                decoratableLocation.SetWallpaper(decorationKey, decoratableLocation.wallpaperIDs[room]);
                            }
                        }
                    }

                    // Draw coloring animation
                    for (int j = 0; j < 12; j++)
                    {
                        var randomColor = new Color(Game1.random.Next(256), Game1.random.Next(256), Game1.random.Next(256));
                        AlternativeTextures.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(6, _textureTarget.tileLocation.Value * 64f, randomColor, 8, flipped: false, 50f)
                        {
                            motion = new Vector2(Game1.random.Next(-10, 11) / 10f, -Game1.random.Next(1, 3)),
                            acceleration = new Vector2(0f, Game1.random.Next(1, 3) / 100f),
                            accelerationChange = new Vector2(0f, -0.001f),
                            scale = 0.8f,
                            layerDepth = (_textureTarget.tileLocation.Y + 1f) * 64f / 10000f,
                            interval = Game1.random.Next(20, 90)
                        });
                    }
                    //Game1.player.currentLocation.localSound("crit");
                    this.exitThisMenu();
                    return;
                }
            }

            if (_startingRow > 0 && this.backButton.containsPoint(x, y))
            {
                _startingRow--;
                Game1.playSound("shiny4");
                return;
            }
            if ((_maxRows + _startingRow) * _texturesPerRow < this.filteredTextureOptions.Count && this.forwardButton.containsPoint(x, y))
            {
                _startingRow++;
                Game1.playSound("shiny4");
                return;
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            if (direction > 0 && _startingRow > 0)
            {
                _startingRow--;
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && (_maxRows + _startingRow) * _texturesPerRow < this.filteredTextureOptions.Count)
            {
                _startingRow++;
                Game1.playSound("shiny4");
            }
        }

        public override void draw(SpriteBatch b)
        {
            if (!Game1.dialogueUp && !Game1.IsFading())
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
                SpriteText.drawStringWithScrollCenteredAt(b, _title, base.xPositionOnScreen + base.width / 4, base.yPositionOnScreen - 64);
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), base.xPositionOnScreen, base.yPositionOnScreen, base.width, base.height, Color.White, 4f);

                for (int i = 0; i < this.availableTextures.Count; i++)
                {
                    this.availableTextures[i].item = null;
                    this.availableTextures[i].texture = null;

                    // Note: The ordering of the PatchTemplate.GetObject & PatchTemplate.GetTerrainFeatureAt is import, as it prioritizes objects (like Mini-Obelisks) over terrain (like craftable paths)
                    var textureIndex = i + _startingRow * _texturesPerRow;
                    if (textureIndex < filteredTextureOptions.Count)
                    {
                        var target = filteredTextureOptions[textureIndex];
                        var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(target.modData["AlternativeTextureName"]);
                        var variation = Int32.Parse(target.modData["AlternativeTextureVariation"]);

                        Color colorOverlay = Color.White;
                        if (_selectedIdsToModels is not null && (_selectedIdsToModels.ContainsKey(target.modData["AlternativeTextureName"]) is false || _selectedIdsToModels[target.modData["AlternativeTextureName"]].Owner != target.modData["AlternativeTextureOwner"] || _selectedIdsToModels[target.modData["AlternativeTextureName"]].Variations.Contains(variation) is false))
                        {
                            colorOverlay = new Color(128, 128, 128, 100);
                        }

                        this.availableTextures[i].item = target;
                        if (variation == -1 || target.modData["AlternativeTextureOwner"] == AlternativeTextures.DEFAULT_OWNER)
                        {
                            if (PatchTemplate.IsDGAUsed() && PatchTemplate.IsDGAObject(PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y)))
                            {
                                this.availableTextures[i].item.drawInMenu(b, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y + 32f), 2, 1f, 0.87f, StackDrawType.Hide, colorOverlay, false);
                            }
                            else if (_textureTarget is Fence)
                            {
                                this.availableTextures[i].texture = (_textureTarget as Fence).loadFenceTexture();
                                this.availableTextures[i].sourceRect = this.GetFenceSourceRect(textureModel, _textureTarget as Fence, this.availableTextures[i].sourceRect.Height, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (_textureType is TextureType.Character && PatchTemplate.GetCharacterAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Character character && character != null)
                            {
                                character.Sprite.loadedTexture = String.Empty;
                                this.availableTextures[i].texture = character.Sprite.Texture;
                                this.availableTextures[i].sourceRect = character.Sprite.SourceRect;
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (_textureType is TextureType.Craftable && PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
                            {
                                this.availableTextures[i].texture = _textureTarget.bigCraftable ? Game1.bigCraftableSpriteSheet : Game1.objectSpriteSheet;
                                this.availableTextures[i].sourceRect = _textureTarget.bigCraftable ? Object.getSourceRectForBigCraftable(_textureTarget.parentSheetIndex) : GameLocation.getSourceRectForObject(_textureTarget.ParentSheetIndex);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
                            {
                                this.availableTextures[i].item.drawInMenu(b, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y + 32f), 2, 1f, 0.87f, StackDrawType.Hide, colorOverlay, false);
                            }
                            else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Tree tree)
                            {
                                this.availableTextures[i].texture = tree.texture.Value;
                                this.availableTextures[i].sourceRect = GetTreeSourceRect(textureModel, tree, 0, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is FruitTree fruitTree)
                            {
                                this.availableTextures[i].texture = FruitTree.texture;
                                this.availableTextures[i].sourceRect = GetFruitTreeSourceRect(textureModel, fruitTree, 0, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Flooring flooring)
                            {
                                this.availableTextures[i].texture = Game1.GetSeasonForLocation(flooring.currentLocation)[0] == 'w' && (flooring.currentLocation == null || !flooring.currentLocation.isGreenhouse) ? Flooring.floorsTextureWinter : Flooring.floorsTexture;
                                this.availableTextures[i].sourceRect = this.GetFlooringSourceRect(textureModel, flooring, this.availableTextures[i].sourceRect.Height, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is HoeDirt hoeDirt)
                            {
                                this.availableTextures[i].texture = Game1.cropSpriteSheet;
                                this.availableTextures[i].sourceRect = this.GetCropSourceRect(textureModel, hoeDirt.crop, 0, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Grass grass)
                            {
                                this.availableTextures[i].texture = grass.texture.Value;
                                this.availableTextures[i].sourceRect = this.GetGrassSourceRect(textureModel, grass, 0, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Bush bush)
                            {
                                this.availableTextures[i].texture = Bush.texture.Value;
                                this.availableTextures[i].sourceRect = this.GetBushSourceRect(textureModel, bush, 0, -1);
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                            else if (PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Building building)
                            {
                                BuildingPatch.ResetTextureReversePatch(building);
                                BuildingPatch.CondensedDrawInMenu(building, building.texture.Value, b, this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y, _buildingScale);

                                if (building is ShippingBin shippingBin)
                                {
                                    b.Draw(Game1.mouseCursors, new Vector2(this.availableTextures[i].bounds.X + 4, this.availableTextures[i].bounds.Y - 20), new Rectangle(134, 226, 30, 25), colorOverlay, 0f, Vector2.Zero, _buildingScale, SpriteEffects.None, 1f);
                                }
                            }
                            else if (Game1.currentLocation is Farm farm && farm.GetHouseRect().Contains(new Vector2(_position.X, _position.Y) / 64))
                            {
                                var targetedBuilding = new Building();
                                targetedBuilding.buildingType.Value = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
                                targetedBuilding.tilesWide.Value = farm.GetHouseRect().Width;
                                targetedBuilding.tilesHigh.Value = farm.GetHouseRect().Height;

                                Texture2D house_texture = BuildingPainter.Apply(Farm.houseTextures, "Buildings\\houses_PaintMask", farm.housePaintColor);
                                if (house_texture is null)
                                {
                                    house_texture = Farm.houseTextures;
                                }

                                BuildingPatch.ResetTextureReversePatch(targetedBuilding);
                                b.Draw(house_texture, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y), farm.houseSource, targetedBuilding.color, 0f, new Vector2(0f, 0f), _buildingScale, SpriteEffects.None, 0.89f);
                            }
                            else if (Game1.currentLocation is DecoratableLocation decoratableLocation && (decoratableLocation.getFloorAt(new Point((int)_position.X, (int)_position.Y)) != -1 || decoratableLocation.getWallForRoomAt(new Point((int)_position.X, (int)_position.Y)) != -1))
                            {
                                var which = variation;
                                var isFloor = _modelName.Contains("Floor");

                                this.availableTextures[i].texture = Game1.content.Load<Texture2D>("Maps\\walls_and_floors");
                                this.availableTextures[i].sourceRect = (isFloor ? new Rectangle(which % 8 * 32, 336 + which / 8 * 32, 32, 32) : new Rectangle(which % 16 * 16, which / 16 * 48, 16, 48));
                                this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                            }
                        }
                        else if (PatchTemplate.IsDGAUsed() && PatchTemplate.IsDGAObject(PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y)) && PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Furniture)
                        {
                            var offset = textureModel.TextureHeight <= 16 ? 32 : 0;
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = GetSourceRectangle(textureModel, _textureTarget, textureModel.TextureWidth, textureModel.TextureHeight, variation);
                            b.Draw(this.availableTextures[i].texture, new Vector2(this.availableTextures[i].bounds.X + this.availableTextures[i].sourceRect.Width / 2 * this.availableTextures[i].baseScale, this.availableTextures[i].bounds.Y + this.availableTextures[i].sourceRect.Height / 2 * this.availableTextures[i].baseScale + offset), this.availableTextures[i].sourceRect, colorOverlay, 0f, new Vector2(this.availableTextures[i].sourceRect.Width / 2, this.availableTextures[i].sourceRect.Height / 2), this.availableTextures[i].scale, SpriteEffects.None, 0.87f);
                        }
                        else if (_textureType is TextureType.Character && PatchTemplate.GetCharacterAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Character character && character != null)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = GetCharacterSourceRectangle(textureModel, character, textureModel.TextureWidth, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Furniture)
                        {
                            this.availableTextures[i].item.drawInMenu(b, new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y + 32f), 2f, 1f, 0.87f, StackDrawType.Hide, colorOverlay, false);
                        }
                        else if (PatchTemplate.GetObjectAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) != null)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = GetSourceRectangle(textureModel, _textureTarget, textureModel.TextureWidth, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Tree tree)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = GetTreeSourceRect(textureModel, tree, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is FruitTree fruitTree)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = GetFruitTreeSourceRect(textureModel, fruitTree, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Flooring flooring)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = GetFlooringSourceRect(textureModel, flooring, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is HoeDirt hoeDirt)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = this.GetCropSourceRect(textureModel, hoeDirt.crop, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Grass grass)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = this.GetGrassSourceRect(textureModel, grass, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Bush bush)
                        {
                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = this.GetBushSourceRect(textureModel, bush, textureModel.TextureHeight, variation);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                        else if (PatchTemplate.GetBuildingAt(Game1.currentLocation, (int)_position.X, (int)_position.Y) is Building building)
                        {
                            BuildingPatch.CondensedDrawInMenu(building, BuildingPatch.GetBuildingTextureWithPaint(building, textureModel, variation), b, this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y, _buildingScale);

                            if (building is ShippingBin shippingBin)
                            {
                                b.Draw(textureModel.GetTexture(variation), new Vector2(this.availableTextures[i].bounds.X + 4, this.availableTextures[i].bounds.Y - 20), new Rectangle(32, textureModel.GetTextureOffset(variation), 30, 25), colorOverlay, 0f, Vector2.Zero, _buildingScale, SpriteEffects.None, 1f);
                            }
                        }
                        else if (Game1.currentLocation is Farm farm && farm.GetHouseRect().Contains(new Vector2(_position.X, _position.Y) / 64))
                        {
                            var targetedBuilding = new Building();
                            targetedBuilding.buildingType.Value = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
                            targetedBuilding.netBuildingPaintColor = farm.housePaintColor;
                            targetedBuilding.tileX.Value = farm.GetHouseRect().X;
                            targetedBuilding.tileY.Value = farm.GetHouseRect().Y;
                            targetedBuilding.tilesWide.Value = farm.GetHouseRect().Width + 1;
                            targetedBuilding.tilesHigh.Value = farm.GetHouseRect().Height + 1;

                            b.Draw(BuildingPatch.GetBuildingTextureWithPaint(targetedBuilding, textureModel, variation, true), new Vector2(this.availableTextures[i].bounds.X, this.availableTextures[i].bounds.Y), new Rectangle(0, 0, farm.houseSource.Width, farm.houseSource.Height), targetedBuilding.color, 0f, new Vector2(0f, 0f), _buildingScale, SpriteEffects.None, 0.89f);
                        }
                        else if (Game1.currentLocation is DecoratableLocation decoratableLocation && (decoratableLocation.getFloorAt(new Point((int)_position.X, (int)_position.Y)) != -1 || decoratableLocation.getWallForRoomAt(new Point((int)_position.X, (int)_position.Y)) != -1))
                        {
                            var isFloor = _modelName.Contains("Floor");
                            var decorationOffset = isFloor ? 8 : 16;

                            this.availableTextures[i].texture = textureModel.GetTexture(variation);
                            this.availableTextures[i].sourceRect = new Rectangle((variation % decorationOffset) * textureModel.TextureWidth, (variation / decorationOffset) * textureModel.TextureHeight, textureModel.TextureWidth, textureModel.TextureHeight);
                            this.availableTextures[i].draw(b, colorOverlay, 0.87f);
                        }
                    }
                }

                _searchBox.Draw(b);
                queryButton.draw(b);
            }

            var hoverInfoText = String.Empty;
            var hoverDisplayName = "Hover over an item to see its texture name!";
            if (this.hovered != null && this.hovered.item != null)
            {
                if (this.hovered.item.modData.ContainsKey("AlternativeTextureOwner") && this.hovered.item.modData.ContainsKey("AlternativeTextureVariation"))
                {
                    if (this.hovered.item.modData.ContainsKey("AlternativeTextureDisplayName") && !String.IsNullOrEmpty(this.hovered.item.modData["AlternativeTextureDisplayName"]))
                    {
                        hoverInfoText = String.Concat(this.hovered.item.modData["AlternativeTextureOwner"], " > ", Int32.Parse(this.hovered.item.modData["AlternativeTextureVariation"]) + 1);
                        hoverDisplayName = this.hovered.item.modData["AlternativeTextureDisplayName"];
                    }
                    else
                    {
                        hoverDisplayName = String.Concat(this.hovered.item.modData["AlternativeTextureOwner"], " > ", Int32.Parse(this.hovered.item.modData["AlternativeTextureVariation"]) + 1);
                    }
                }
            }
            SpriteText.drawStringWithScrollCenteredAt(b, hoverDisplayName, Game1.uiViewport.Width / 2, base.yPositionOnScreen + base.height + 16, "Hover over an item to see its texture name!");

            if (_startingRow > 0)
            {
                this.backButton.draw(b);
            }
            if ((_maxRows + _startingRow) * _texturesPerRow < this.filteredTextureOptions.Count)
            {
                this.forwardButton.draw(b);
            }

            if (!String.IsNullOrEmpty(hoverInfoText))
            {
                IClickableMenu.drawHoverText(b, hoverInfoText, Game1.smallFont);
            }

            if (_isSprayCan is false)
            {
                Game1.mouseCursorTransparency = 1f;
                base.drawMouse(b);
            }
        }

        private Rectangle GetSourceRectangle(AlternativeTextureModel textureModel, Object target, int textureWidth, int textureHeight, int variation)
        {
            var textureOffset = variation > 0 ? textureModel.GetTextureOffset(variation) : 0;
            var sourceRect = new Rectangle(0, textureOffset, textureWidth, textureHeight);
            if (target is Fence fence)
            {
                sourceRect = this.GetFenceSourceRect(textureModel, fence, textureHeight, variation);
            }
            else if (target is Furniture furniture)
            {
                sourceRect = furniture.sourceRect.Value;
                sourceRect.X -= furniture.defaultSourceRect.X;
                sourceRect.Y = textureOffset;
            }

            if (sourceRect.Width > 32)
            {
                sourceRect.Width = 32;
            }
            return sourceRect;
        }

        private Rectangle GetFenceSourceRect(AlternativeTextureModel textureModel, Fence fence, int textureHeight, int variation)
        {
            int sourceRectPosition = 1;
            var textureOffset = variation == -1 ? 0 : textureModel.GetTextureOffset(variation);
            if ((float)fence.health > 1f || fence.repairQueued.Value)
            {
                int drawSum = fence.getDrawSum(Game1.currentLocation);
                sourceRectPosition = Fence.fenceDrawGuide[drawSum];

                var gateOffset = fence.isGate && variation != -1 ? 128 : 0;
                if ((bool)fence.isGate)
                {
                    Vector2 offset = new Vector2(0f, 0f);
                    switch (drawSum)
                    {
                        case 10:
                            return new Rectangle(((int)fence.gatePosition == 88) ? 24 : 0, textureOffset + (192 - gateOffset) + 16, 24, 32);
                        case 100:
                            return new Rectangle(((int)fence.gatePosition == 88) ? 24 : 0, textureOffset + (240 - gateOffset) + 16, 24, 32);
                        case 1000:
                            return new Rectangle(((int)fence.gatePosition == 88) ? 24 : 0, textureOffset + (288 - gateOffset), 24, 32);
                        case 500:
                            return new Rectangle(((int)fence.gatePosition == 88) ? 24 : 0, textureOffset + (320 - gateOffset), 24, 32);
                        case 110:
                            return new Rectangle(((int)fence.gatePosition == 88) ? 24 : 0, textureOffset + (128 - gateOffset), 24, 32);
                        case 1500:
                            return new Rectangle(((int)fence.gatePosition == 88) ? 16 : 0, textureOffset + (160 - gateOffset), 16, 16);
                    }
                    sourceRectPosition = 5;
                }
            }

            return new Rectangle((sourceRectPosition * Fence.fencePieceWidth % fence.fenceTexture.Value.Bounds.Width), textureOffset + (sourceRectPosition * Fence.fencePieceWidth / fence.fenceTexture.Value.Bounds.Width * Fence.fencePieceHeight), Fence.fencePieceWidth, Fence.fencePieceHeight);
        }

        private Rectangle GetFlooringSourceRect(AlternativeTextureModel textureModel, Flooring flooring, int textureHeight, int variation)
        {
            int sourceRectOffset = variation == -1 ? (int)flooring.whichFloor * 4 * 64 : textureModel.GetTextureOffset(variation);
            byte drawSum = 0;
            Vector2 surroundingLocations = flooring.currentTileLocation;
            surroundingLocations.X += 1f;
            if (Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations) && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring)
            {
                drawSum = (byte)(drawSum + 2);
            }
            surroundingLocations.X -= 2f;
            if (Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations) && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring)
            {
                drawSum = (byte)(drawSum + 8);
            }
            surroundingLocations.X += 1f;
            surroundingLocations.Y += 1f;
            if (Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations) && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring)
            {
                drawSum = (byte)(drawSum + 4);
            }
            surroundingLocations.Y -= 2f;
            if (Game1.currentLocation.terrainFeatures.ContainsKey(surroundingLocations) && Game1.currentLocation.terrainFeatures[surroundingLocations] is Flooring)
            {
                drawSum = (byte)(drawSum + 1);
            }

            int sourceRectPosition = Flooring.drawGuide[drawSum];
            if ((bool)flooring.isSteppingStone)
            {
                sourceRectPosition = Flooring.drawGuideList[flooring.whichView.Value];
            }

            if (variation == -1)
            {
                return new Rectangle((int)flooring.whichFloor % 4 * 64 + sourceRectPosition * 16 % 256, sourceRectPosition / 16 * 16 + (int)flooring.whichFloor / 4 * 64, 16, 16);
            }

            return new Rectangle(sourceRectPosition % 16 * 16, sourceRectPosition / 16 * 16 + sourceRectOffset, 16, 16);
        }

        private Rectangle GetTreeSourceRect(AlternativeTextureModel textureModel, Tree tree, int textureHeight, int variation)
        {
            int sourceRectOffset = variation == -1 ? 0 : textureModel.GetTextureOffset(variation);
            Rectangle source_rect = tree.treeTopSourceRect;
            if (tree.treeType.Value == 9)
            {
                if (tree.hasSeed.Value)
                {
                    source_rect.X = 48;
                }
                else
                {
                    source_rect.X = 0;
                }
            }

            source_rect.Y += sourceRectOffset;
            return source_rect;
        }

        private Rectangle GetFruitTreeSourceRect(AlternativeTextureModel textureModel, FruitTree fruitTree, int textureHeight, int variation)
        {
            if (variation == -1)
            {
                return new Rectangle((12 + (fruitTree.greenHouseTree ? 1 : Utility.getSeasonNumber(Game1.GetSeasonForLocation(Game1.currentLocation))) * 3) * 16, (int)fruitTree.treeType * 5 * 16, 48, 80);
            }

            int sourceRectOffset = variation == -1 ? 0 : textureModel.GetTextureOffset(variation);
            Rectangle source_rect = new Rectangle((12 + (fruitTree.greenHouseTree ? 1 : Utility.getSeasonNumber(Game1.GetSeasonForLocation(Game1.currentLocation))) * 3) * 16, 0, 48, 80);

            source_rect.Y += sourceRectOffset;
            return source_rect;
        }

        private Rectangle GetCropSourceRect(AlternativeTextureModel textureModel, Crop crop, int textureHeight, int variation)
        {
            if (variation == -1)
            {
                var vanillaRectangle = AlternativeTextures.modHelper.Reflection.GetField<Rectangle>(crop, "sourceRect").GetValue();
                return new Rectangle(vanillaRectangle.X >= 128 ? 128 : 0, vanillaRectangle.Y, 128, 32);
            }

            Rectangle source_rect = new Rectangle(0, 0, 128, 32);
            return source_rect;
        }

        private Rectangle GetGrassSourceRect(AlternativeTextureModel textureModel, Grass grass, int textureHeight, int variation)
        {
            if (variation == -1)
            {
                return new Rectangle(0, grass.grassSourceOffset.Value, 15, 20);
            }

            Rectangle source_rect = new Rectangle(0, 0, 15, 20);
            return source_rect;
        }

        private Rectangle GetBushSourceRect(AlternativeTextureModel textureModel, Bush bush, int textureHeight, int variation)
        {
            if (variation == -1)
            {
                bush.setUpSourceRect();
                return AlternativeTextures.modHelper.Reflection.GetField<NetRectangle>(bush, "sourceRect").GetValue();
            }
            return new Rectangle(Math.Min(2, bush.getAge() / 10) * 16 + bush.tileSheetOffset.Value * 16, 0, 16, 32);
        }

        private Rectangle GetCharacterSourceRectangle(AlternativeTextureModel textureModel, Character character, int textureWidth, int textureHeight, int variation)
        {
            int sourceRectOffset = textureModel.GetTextureOffset(variation);
            var sourceRect = character.Sprite.sourceRect;

            sourceRect.Y = sourceRectOffset + (character.Sprite.currentFrame * character.Sprite.SpriteWidth / character.Sprite.Texture.Width * character.Sprite.SpriteHeight);
            return sourceRect;
        }
    }
}