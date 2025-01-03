﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UEParser.Models;
using UEParser.Utils;
using UEParser.ViewModels;

namespace UEParser.APIComposers;

// This class is shit and should be reworked, but at least it works (kinda)
public class ModelData
{
    public static void CreateModelData(string modelDataPath, string cosmeticId, int characterIndex, string cosmeticType, dynamic accessoriesData, dynamic materialsMap, dynamic texturesMap)
    {
        // Ignore cosmetics that don't have model
        string[] cosmeticTypesToIgnore = ["Badge", "Banner"];
        if (cosmeticTypesToIgnore.Contains(cosmeticType)) return;
        
        string? skeletonPath = "";
        if (characterIndex != -1) skeletonPath = FindSkeletonPath(cosmeticId, characterIndex);

        bool isStaticMesh = false;
        if (characterIndex == -1) isStaticMesh = true;

        bool isWeapon = false;
        if (cosmeticType == "KillerWeapon") isWeapon = true;

        string meshPath = "/assets" + modelDataPath.Replace(".json", ".glb");

        string outputPath = Path.Combine(GlobalVariables.RootDir, "Output", "ModelsData", GlobalVariables.VersionWithBranch + modelDataPath);
        var modelData = new Dictionary<string, UEModelData>();

        // Check if the file already exists
        if (File.Exists(outputPath))
        {
            string existingData = File.ReadAllText(outputPath);
            Dictionary<string, UEModelData>? jsonModelData = JsonConvert.DeserializeObject<Dictionary<string, UEModelData>>(existingData);

            if (jsonModelData != null)
            {
                modelData = jsonModelData;

                if (jsonModelData.ContainsKey(cosmeticId))
                {
                    // If it exists, skip exporting the file
                    return;
                }
            }
        }

        List<AccessoryData> accessoryData = ParseAccessoriesData(accessoriesData, cosmeticId, texturesMap, characterIndex);
        Dictionary<string, Materials> materialsData = ParseMaterialsData(modelDataPath, materialsMap, texturesMap, cosmeticId, characterIndex);

        modelData[cosmeticId] = new UEModelData
        {
            ModelPath = meshPath,
            SkeletonPath = skeletonPath,
            IsStaticMesh = isStaticMesh,
            IsWeapon = isWeapon,
            Accessories = accessoryData,
            Materials = materialsData
        };

        string data = JsonConvert.SerializeObject(modelData, Formatting.Indented);

        var outputPathWithoutName = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputPathWithoutName))
        {
            Directory.CreateDirectory(outputPathWithoutName);
            File.WriteAllText(outputPath, data);
        }
    }

    private static string FindObjectName(dynamic material, bool isInSkeletalMaterial)
    {
        if (isInSkeletalMaterial)
        {
            return material["MaterialSlotName"];
        }
        else
        {
            string fullObjectName = material["ObjectName"];
            return StringUtils.ExtractObjectName(fullObjectName);
        }
    }

    private static string FindObjectPath(dynamic material, bool isInSkeletalMaterial)
    {
        if (isInSkeletalMaterial)
        {
            if (material["Material"] == null) return "";

            return material["Material"]["ObjectPath"];
        }
        else
        {
            return material["ObjectPath"];
        }
    }

    private static Dictionary<string, Materials> ParseMaterialsData(string modelDataPath, dynamic materialsMap, dynamic texturesMap, string cosmeticId, int characterIndex)
    {
        string meshPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "ExtractedAssets" + modelDataPath);
        dynamic meshData = FileUtils.LoadDynamicJson(meshPath);

        CharacterMaterialsOutput meshMaterialItems = FindCharacterMaterials(meshData);

        var materialsModel = new Dictionary<string, Materials>();

        foreach (var material in meshMaterialItems.Materials ?? Enumerable.Empty<dynamic>())
        {
            if (material != null)
            {
                //string fullObjectName = material["ObjectName"];
                string objectName = FindObjectName(material, meshMaterialItems.IsSkeletalMaterials);

                // Ignore materials that I don't need
                string[] objectsToIgnore = ["lambert1", "lambert2", "lambert3", "lambert4", "Default_Material"];
                string[] materialNamesToIgnore = ["lambert1", "lambert2", "lambert3", "lambert4", "Default_Material"];

                if (objectsToIgnore.Contains(objectName)) continue;

                string gameMaterialPath = FindObjectPath(material, meshMaterialItems.IsSkeletalMaterials);

                if (string.IsNullOrEmpty(gameMaterialPath)) continue;

                string modifiedMaterialPath = StringUtils.ModifyPath(gameMaterialPath, "json", false, characterIndex);

                // TODO: only way to fix this hardcoded override, is to mount plugin dir with main content dir
                if (gameMaterialPath == "/S45/ArtAssets/Materials/Outfit00/MI_Lashes00.0")
                {
                    modifiedMaterialPath = "/DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters/S45/Content/ArtAssets/Materials/Outfit00/MI_Lashes00.json";
                }

                string materialPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "ExtractedAssets/" + modifiedMaterialPath);

                dynamic meshMaterialData = FileUtils.LoadDynamicJson(materialPath);

                string materialName = meshMaterialData[0].Name;

                if (materialNamesToIgnore.Contains(materialName)) continue;

                // Re-textured cosmetics change materials
                if (materialsMap.Count > 0)
                {
                    foreach (var materialMap in materialsMap)
                    {
                        string materialFromPath = materialMap.From.AssetPathName;
                        string gameInitialMaterial = StringUtils.ModifyPath(materialFromPath, "json", false, characterIndex);
                        StringUtils.RemoveDoubleSlashes(gameInitialMaterial);

                        if (gameInitialMaterial == modifiedMaterialPath)
                        {
                            string gameNewMaterialPath = materialMap.To.AssetPathName;
                            string gameNewMaterial = StringUtils.ModifyPath(gameNewMaterialPath, "json", false, characterIndex);
                            string newMaterialPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "ExtractedAssets" + gameNewMaterial);

                            meshMaterialData = FileUtils.LoadDynamicJson(newMaterialPath);
                        }
                    }
                }

                string? replacementTexture = null;
                if (texturesMap.Count > 0)
                {
                    foreach (var textureMap in texturesMap)
                    {
                        string replacementTextureRaw = textureMap.ReplacementTexture.AssetPathName;
                        replacementTexture = StringUtils.ModifyPath("/assets" + replacementTextureRaw, "png", false, characterIndex);
                    }
                }

                string materialInstanceConstant = meshMaterialData[0].Properties.Parent.ObjectName;
                string materialInstance = StringUtils.ExtractStringInSingleQuotes(materialInstanceConstant);

                var textures = MapTextures(meshMaterialData[0].Properties.TextureParameterValues, cosmeticId, replacementTexture, characterIndex);
                var scalarParameters = MapScalarParameters(meshMaterialData[0].Properties.ScalarParameterValues);
                var vectorParameters = MapVectorParameters(meshMaterialData[0].Properties.VectorParameterValues);
                var staticParameters = MapStaticParameters(meshMaterialData[0].Properties.StaticParameters?.StaticSwitchParameters);
                var cachedReferencedTextures = MapCachedReferencedTextures(meshMaterialData[0].Properties.CachedReferencedTextures);

                materialsModel[materialName] = new Materials
                {
                    MaterialInstance = materialInstance,
                    Textures = textures,
                    ScalarParameters = scalarParameters,
                    VectorParameters = vectorParameters,
                    StaticParameters = staticParameters,
                    CachedTextures = cachedReferencedTextures
                };
            }

        }

        return materialsModel;
    }

    private static List<CachedTextures> MapCachedReferencedTextures(dynamic cachedTexturesData)
    {
        var cachedTexturesModel = new List<CachedTextures>();
        if (cachedTexturesData == null)
        {
            return cachedTexturesModel;
        }

        foreach (var cachedTexture in cachedTexturesData)
        {
            if (cachedTexture != null)
            {
                string fullName = cachedTexture.ObjectName;
                string name = StringUtils.SplitTextureName(fullName);

                // I only need tileable texture for now
                // Remove this if statement if more cached textures are needed
                // Update: After moving to UE5 they stopped using cached textures, lets keep it just in case
                if (name == "T_ETouch_Tile")
                {
                    string texturePath = cachedTexture.ObjectPath;
                    string path = StringUtils.ModifyPath("/assets/" + texturePath, "png");

                    var cachedTexturesInstance = new CachedTextures
                    {
                        Name = name,
                        Path = path
                    };

                    cachedTexturesModel.Add(cachedTexturesInstance);
                }
            }
        }

        return cachedTexturesModel;
    }

    private static Textures MapTextures(dynamic texturesData, string cosmeticId, string? replacementTexture, int characterIndex)
    {
        var texturesModel = new Textures();
        if (texturesData == null)
        {
            return texturesModel;
        }

        foreach (var texture in texturesData)
        {
            string textureName = texture.ParameterInfo.Name;
            if (texture.ParameterValue != null)
            {
                string gameTexturePath = texture.ParameterValue.ObjectPath;

                if (gameTexturePath == "None")
                {
                    continue;
                }

                string texturePathWithoutAssets = StringUtils.ModifyPath(gameTexturePath, "png", false, characterIndex);
                string texturePath = StringUtils.RemoveDoubleSlashes("/assets/" + texturePathWithoutAssets);

                string[] ignoreRecolorTexture = ["S25_Legs011_01"];
                string[] texturesToIgnoreBc = [
                    "/assets/DeadByDaylight/Content/Textures/Environment/Tiles/value_12.png",
                ];
                string[] texturesToIgnoreRc = [
                    "/assets/DeadByDaylight/Content/Textures/Environment/Tiles/Black.png"
                ]; // Add only if you're sure it's not needed in any cosmetics

                switch (textureName)
                {
                    case "Diffuse":
                    case "Base Color Texture":
                        if (!texturesToIgnoreBc.Contains(texturePath))
                        {
                            texturesModel.BaseColor = texturePath;
                        }
                        break;
                    case "GlassColorTexture":
                        texturesModel.GlassColorTexture = texturePath;
                        break;
                    case "AORoughnessMetallic":
                        texturesModel.ORM = texturePath;
                        break;
                    case "Normal":
                    case "NormalMap Texture":
                        texturesModel.NormalMap = texturePath;
                        break;
                    case "Alpha_Mask":
                    case "Alpha mask":
                        texturesModel.AlphaMask = texturePath;
                        break;
                    case "Global_Mask":
                        texturesModel.GlobalMask = texturePath;
                        break;
                    case "Opacity Mask Texture": // Texture used for killers only
                        texturesModel.OpacityMask = texturePath;
                        break;
                    case "MaskFromOtherPart":
                        texturesModel.MaskFromOtherPart = texturePath;
                        break;
                    case "RootTip_Mask":
                        texturesModel.Gradient = texturePath;
                        break;
                    case "Depth_Mask":
                        texturesModel.DepthMask = texturePath;
                        break;
                    case "Blood Dirt Emissive":
                        texturesModel.BDE = texturePath;
                        break;
                    case "ColorMask_Tint":
                        if (!ignoreRecolorTexture.Contains(cosmeticId) && !texturesToIgnoreRc.Contains(texturePath))
                        {
                            texturesModel.RecolorTexture = texturePath;
                        }
                        break;
                    case "Emissive_Tileable_Texture":
                        texturesModel.TileableTexture = texturePath;
                        break;
                    case "ReflectionTexture":
                        texturesModel.ReflectionTexture = texturePath;
                        break;
                    default:
                        break;
                }
            }
        }

        if (replacementTexture != null)
        {
            texturesModel.BaseColor = replacementTexture;
        }

        return texturesModel;
    }

    private static List<StaticParameters>? MapStaticParameters(dynamic staticParametersData)
    {
        var staticParametersModel = new List<StaticParameters>();
        if (staticParametersData == null)
        {
            return null;
        }

        foreach (var staticParameter in staticParametersData)
        {
            string parameterName = staticParameter.ParameterInfo.Name;
            bool value = staticParameter.Value;

            var staticParametersInstance = new StaticParameters
            {
                Name = parameterName,
                StaticParameterValue = value
            };

            staticParametersModel.Add(staticParametersInstance);
        }

        return staticParametersModel;
    }

    private static ScalarParameters MapScalarParameters(dynamic scalarParametersData)
    {
        var scalarParametersModel = new ScalarParameters();
        foreach (var scalarParameter in scalarParametersData)
        {
            string parameterName = scalarParameter.ParameterInfo.Name;
            float parameterValue = scalarParameter.ParameterValue;

            switch (parameterName)
            {
                case "RoughnessValue":
                    scalarParametersModel.Roughness = parameterValue;
                    break;
                case "SpecularValue":
                    scalarParametersModel.Specular = parameterValue;
                    break;
                case "EM Intensity":
                    scalarParametersModel.EmissiveIntensity = parameterValue;
                    break;
                case "GlassOpacity":
                    scalarParametersModel.GlassOpacity = parameterValue;
                    break;
                case "ReflectionsIntensity":
                    scalarParametersModel.ReflectionsIntensity = parameterValue;
                    break;
                case "Refraction":
                    scalarParametersModel.Refraction = parameterValue;
                    break;
                case "Intensity of mixing DIffuce and Tint":
                case "DiffuseTintMix":
                    scalarParametersModel.DiffuseTintMix = parameterValue;
                    break;
                case "Color desaturation":
                    scalarParametersModel.SaturationLevel = parameterValue;
                    break;
                case "Tip and root intensity":
                    scalarParametersModel.TipRootIntensity = parameterValue;
                    break;
                default:
                    break;
            }
        }

        return scalarParametersModel;
    }

    private static List<VectorParameters>? MapVectorParameters(dynamic vectorParametersData)
    {
        if (vectorParametersData == null)
        {
            return null;
        }

        var vectorParametersModel = new List<VectorParameters>();
        foreach (var vectorParameter in vectorParametersData)
        {
            string parameterName = vectorParameter.ParameterInfo.Name;
            var parameterValues = vectorParameter.ParameterValue;

            double redChannel = parameterValues.R;
            double greenChannel = parameterValues.G;
            double blueChannel = parameterValues.B;
            double alphaChannel = parameterValues.A;
            string? hexValue = parameterValues.Hex;

            var vectorParameterValue = new VectorParameterValue
            {
                R = redChannel,
                G = greenChannel,
                B = blueChannel,
                A = alphaChannel,
                Hex = hexValue
            };

            var vectorParametersInstance = new VectorParameters
            {
                Name = parameterName,
                VectorParameterValue = vectorParameterValue
            };

            vectorParametersModel.Add(vectorParametersInstance);
        }

        return vectorParametersModel;
    }

    private static string? FindSkeletonPath(string cosmeticId, int characterIndex)
    {
        string characterIndexString = characterIndex.ToString();

        string blueprintsPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "HelperComponents", "characterBlueprintsLinkage.json");
        dynamic blueprintsData = FileUtils.LoadDynamicJson(blueprintsPath);

        string? pathToGameBlueprint = null;
        if (blueprintsData["Cosmetics"].ContainsKey(cosmeticId))
        {
            pathToGameBlueprint = blueprintsData["Cosmetics"][cosmeticId].GameBlueprint;
        }
        else
        {
            foreach (var cosmeticEntry in blueprintsData["Cosmetics"])
            {
                var cosmeticData = cosmeticEntry.Value;
                JArray cosmeticItems = cosmeticData["CosmeticItems"];

                // Make sure they are compared as the same type and ignoring their case
                if (cosmeticItems.Any(item => item.ToString().Equals(cosmeticId, StringComparison.OrdinalIgnoreCase)))
                {
                    pathToGameBlueprint = cosmeticData["GameBlueprint"];
                    break;
                }
            }
        }

        if (pathToGameBlueprint == null || pathToGameBlueprint == "None")
        {
            if (blueprintsData["Characters"][characterIndexString] != null)
            {
                pathToGameBlueprint = blueprintsData["Characters"][characterIndexString]["GameBlueprint"];
            }
        }

        if (pathToGameBlueprint == null)
        {
#if DEBUG
            LogsWindowViewModel.Instance.AddLog($"Path to character blueprint for '{cosmeticId}' was null.", Logger.LogTags.Debug);
#endif
            return null;
        }

        pathToGameBlueprint = StringUtils.ModifyPath(pathToGameBlueprint, "json", false, characterIndex);

        dynamic skeletonBlueprintData = FileUtils.LoadDynamicJson(Path.Combine(GlobalVariables.RootDir, "Dependencies", "ExtractedAssets" + pathToGameBlueprint));
        string? gameSkeletonPath = FindCharacterMeshPath(skeletonBlueprintData);

        // These manual overrides should be resolved but I couldn't find any good solution
        // BHVR often sets skeletons to wrong cosmetic ID and only reason it works in-game
        // is because only one cosmetic piece needs to be set right in terms of linked cosmetics
        // ...

        #region Animations work-around
        // I don't allow multiple animations in the same skeleton (like it works in-game)
        // that's why below skeletons are customized manually by me
        string[] queenXenomorphOverride = ["K33_Head007", "K33_Body007", "K33_W007"];
        if (queenXenomorphOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Slashers/K33/Models/K33_DSkeleton_REF_QueenXenomorph.glb";
        }
        string[] cybilOverride = ["S22_Head008", "S22_Torso008", "S22_Legs008"];
        if (cybilOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Campers/S22/Models/S22_DSkeleton_REF_Cybil.glb";
        }
        string[] trueFormDarkLordOverride = ["K37_Head008", "K37_Body008", "K37_W008", "K37_Head008_01", "K37_Body008_01", "K37_W008_01"];
        if (trueFormDarkLordOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters/K37/Content/ArtAssets/Models/K37_DSkeleton_REF_TrueForm.glb";
        }
        string[] ultimateHoundMasterOverride = ["K38_Head006", "K38_Body006", "K38_W006"];
        if (ultimateHoundMasterOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters/K38/Content/ArtAssets/Models/K38_DSkeleton_REF_006.glb";
        }
        #endregion

        #region Bhvr's IDs mismatch
        if (cosmeticId == "TR_Mask018") // Give Naughty Bear skeleton manually because BHVR used wrong cosmetic ID "TR_Head018" instead of "TR_Mask018"
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Slashers/Trapper/Models/TR_018_DSkeleton_REF.glb";
        }
        if (characterIndex == 38) // Give Ripley skeleton manually as she doesnt have skeleton
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Campers/S39/Models/S39_DSkeleton_Menu_REF.glb";
        }
        string[] wereElkOverride = ["BE_Mask024", "BE_Body024", "BE_W024"];
        if (wereElkOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Slashers/Bear/Models/BE_DSkeleton_024_REF.glb";
        }
        if (cosmeticId == "S22_Legs012") // Set Maria's skeleton manually as they gave it to Cybil instead of Maria (legs only)
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Campers/S22/Models/S22_012_DSkeleton_REF.glb";
        }
        if (characterIndex == 41)
        {
            gameSkeletonPath = "DeadByDaylight/Plugins/Runtime/Bhvr/DBDCharacters/S42/Content/ArtAssets/Models/S42_DSkeleton_REF.glb";
        }
        string[] rainOverride = ["S39_Head009", "S39_Torso009", "S39_Legs009"];
        if (rainOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Content/Characters/Campers/S39/Models/S39_009_DSkeleton_Menu_REF.glb";
        }
        if (cosmeticId == "S44_Legs006") // Give Alucard's legs skeleton manually, they set it to S40_Legs006 instead of S44_Legs006..
        {
            gameSkeletonPath = "DeadByDaylight/Plugins/DBDCharacters/S44/Content/ArtAssets/Models/S44_006_DSkeleton_REF.glb";
        }
        string[] somaOverride = ["S44_Torso009_01", "S44_Legs009_01"]; // They set head cosmetic id three times :_:
        if (somaOverride.Contains(cosmeticId))
        {
            gameSkeletonPath = "DeadByDaylight/Plugins/DBDCharacters/S44/Content/ArtAssets/Models/S44_009_DSkeleton_REF.glb";
        }
        #endregion

#if DEBUG
        if (string.IsNullOrEmpty(gameSkeletonPath))
        {
            LogsWindowViewModel.Instance.AddLog($"Skeleton path for {cosmeticId} was null or empty, it needs to be provided manually.", Logger.LogTags.Debug);
            return "";
        }
#endif

        string skeletonPathWithoutAssets = StringUtils.ModifyPath(gameSkeletonPath, "glb", false, characterIndex);
        string skeletonPath = StringUtils.RemoveDoubleSlashes("/assets/" + skeletonPathWithoutAssets);

        return skeletonPath;
    }

    private static dynamic? FindByType(dynamic array, string typeName)
    {
        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]["Type"] != null && array[i]?["Type"] == typeName &&
                array[i]["Properties"] != null &&
                array[i]["Properties"]["SkeletalMesh"] != null &&
                array[i]["Properties"]["SkeletalMesh"]["ObjectPath"] != null)
            {
                return array[i]["Properties"]["SkeletalMesh"]["ObjectPath"];
            }
        }

        return null;
    }

    private class CharacterMaterialsOutput(dynamic materials, bool isSkeletalMaterials)
    {
        public dynamic Materials { get; } = materials;
        public bool IsSkeletalMaterials { get; } = isSkeletalMaterials;
    }

    private static dynamic? FindCharacterMaterials(dynamic array)
    {
        bool isSkeletalMaterials = false;
        dynamic? materials = null;

        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]["Type"] != null && array[i]?["Type"] == "SkeletalMesh" &&
                array[i]["Materials"] != null)
            {
                materials = array[i]?["Materials"];
                break;
            }
            else if (array[i]["Type"] != null && array[i]?["Type"] == "SkeletalMesh" &&
                array[i]["SkeletalMaterials"] != null)
            {
                materials = array[i]?["SkeletalMaterials"];
                isSkeletalMaterials = true;
            }
        }

        return materials != null ? new CharacterMaterialsOutput(materials, isSkeletalMaterials) : null;
    }

    private static string? FindCharacterMeshPath(dynamic array)
    {
        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]["Name"] != null && array[i]?["Name"] == "CharacterMesh0" &&
                array[i]["Properties"] != null &&
                array[i]?["Properties"]?["SkeletalMesh"] != null &&
                array[i]?["Properties"]?["SkeletalMesh"]?["ObjectPath"] != null)
            {
                return array[i]?["Properties"]?["SkeletalMesh"]?["ObjectPath"];
            }
        }

        return null;
    }

    private static List<AccessoryData> ParseAccessoriesData(dynamic accessoriesData, string cosmeticId, dynamic texturesMap, int characterIndex)
    {
        var accessoryData = new List<AccessoryData>();
        // Iterate through each item in "SocketAttachements"
        foreach (var attachment in accessoriesData)
        {
            // Check if "AccessoryBlueprint" exists for the current attachment
            if (attachment.AccessoryBlueprint != null)
            {
                string gameBlueprintPath = attachment.AccessoryBlueprint.AssetPathName;
                string skeletalMeshPath = attachment.SkeletalMesh.AssetPathName;

                string gameMeshPath;
                if (gameBlueprintPath == "None")
                {
                    gameMeshPath = skeletalMeshPath;
                }
                else
                {
                    string blueprintPath = StringUtils.ModifyPath(gameBlueprintPath, "json", false, characterIndex);
                    string assetBlueprintPath = Path.Combine(GlobalVariables.RootDir, "Dependencies", "ExtractedAssets" + blueprintPath);

                    dynamic data = FileUtils.LoadDynamicJson(assetBlueprintPath);

                    string objectType = StringUtils.GetSubstringAfterLastDot(gameBlueprintPath);

                    gameMeshPath = FindByType(data, objectType);
                }

                if (gameMeshPath == "None" || gameMeshPath == null)
                {
                    continue;
                }

                string jsonMeshPath = StringUtils.ModifyPath((gameMeshPath.StartsWith('/') ? "" : '/') + gameMeshPath, "json", false, characterIndex);
                string meshPathWithoutAssets = StringUtils.ModifyPath(gameMeshPath, "glb", false, characterIndex);
                string meshPath = StringUtils.RemoveDoubleSlashes("/assets/" + meshPathWithoutAssets);

                dynamic materialsMap = attachment.MaterialsMap;

                Dictionary<string, Materials> materials = ParseMaterialsData(jsonMeshPath, materialsMap, texturesMap, cosmeticId, characterIndex);

                accessoryData.Add(new AccessoryData
                {
                    SocketName = attachment.SocketName,
                    ModelPath = meshPath,
                    Materials = materials
                });

            }
        }

        return accessoryData;
    }
}