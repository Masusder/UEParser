using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UEParser.Models;

/// <summary>
/// Represents various textures used for defining the appearance of materials.
/// </summary>
public class Textures
{
    /// <summary>
    /// Gets or sets the base color texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? BaseColor { get; set; }

    /// <summary>
    /// Gets or sets the texture containing occlusion, roughness, and metallic information.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ORM { get; set; }

    /// <summary>
    /// Gets or sets the normal map texture used for adding surface details.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? NormalMap { get; set; }

    /// <summary>
    /// Gets or sets the alpha mask texture for transparency.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? AlphaMask { get; set; }

    /// <summary>
    /// Gets or sets the global mask texture, primarily used for special hair effects like Spirit/Oni hair.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? GlobalMask { get; set; }

    /// <summary>
    /// Gets or sets the blood, dirt, and emissive texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? BDE { get; set; }

    /// <summary>
    /// Gets or sets the gradient texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Gradient { get; set; }

    /// <summary>
    /// Gets or sets the depth mask texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DepthMask { get; set; }

    /// <summary>
    /// Gets or sets the texture used for recoloring cosmetics.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? RecolorTexture { get; set; }

    /// <summary>
    /// Gets or sets the tileable texture, mainly used for emission effects.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? TileableTexture { get; set; }

    /// <summary>
    /// Gets or sets the glass color texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? GlassColorTexture { get; set; }

    /// <summary>
    /// Gets or sets the reflection texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ReflectionTexture { get; set; }

    /// <summary>
    /// Gets or sets another alpha mask texture.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? MaskFromOtherPart { get; set; }

    /// <summary>
    /// Gets or sets the opacity mask texture, used exclusively for Killers!!!
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? OpacityMask { get; set; }
}

/// <summary>
/// Represents a collection of scalar parameters used for various material properties.
/// </summary>
public class ScalarParameters
{
    /// <summary>
    /// Gets or sets the roughness of the material.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float Roughness { get; set; }

    /// <summary>
    /// Gets or sets the specular reflection intensity of the material.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float Specular { get; set; }

    /// <summary>
    /// Gets or sets the emissive intensity of the material.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float EmissiveIntensity { get; set; }

    /// <summary>
    /// Gets or sets the opacity level for glass materials.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float GlassOpacity { get; set; }

    /// <summary>
    /// Gets or sets the intensity of reflections on the material.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float ReflectionsIntensity { get; set; }

    /// <summary>
    /// Gets or sets the refraction index of the material.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float Refraction { get; set; }

    /// <summary>
    /// Gets or sets the mix value between diffuse and tint colors.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float DiffuseTintMix { get; set; }

    /// <summary>
    /// Gets or sets the saturation level of the material.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float SaturationLevel { get; set; }

    /// <summary>
    /// Gets or sets the intensity of the color gradient from tip to root.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float TipRootIntensity { get; set; }
}


/// <summary>
/// Represents cached texture information including its name and file path.
/// </summary>
public class CachedTextures
{
    /// <summary>
    /// Gets or sets the name of the cached texture.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the file path of the cached texture.
    /// </summary>
    public string? Path { get; set; }
}

/// <summary>
/// Represents a static parameter with a name and its boolean value.
/// </summary>
public class StaticParameters
{
    /// <summary>
    /// Gets or sets the name of the static parameter.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the static parameter.
    /// </summary>
    public bool StaticParameterValue { get; set; }
}

/// <summary>
/// Represents a color value in RGBA format and its hexadecimal representation.
/// </summary>
public class VectorParameterValue
{
    /// <summary>
    /// Gets or sets the red component of the color.
    /// </summary>
    public double R { get; set; }

    /// <summary>
    /// Gets or sets the green component of the color.
    /// </summary>
    public double G { get; set; }

    /// <summary>
    /// Gets or sets the blue component of the color.
    /// </summary>
    public double B { get; set; }

    /// <summary>
    /// Gets or sets the alpha (transparency) component of the color.
    /// </summary>
    public double A { get; set; }

    /// <summary>
    /// Gets or sets the hexadecimal representation of the color.
    /// </summary>
    public string? Hex { get; set; }
}

/// <summary>
/// Represents a vector parameter with a name and its value.
/// </summary>
public class VectorParameters
{
    /// <summary>
    /// Gets or sets the name of the vector parameter.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the vector parameter.
    /// </summary>
    public VectorParameterValue? VectorParameterValue { get; set; }
}

/// <summary>
/// Represents material properties including instance, textures, scalar parameters, vector parameters, static parameters, and cached textures.
/// </summary>
public class Materials
{
    /// <summary>
    /// Gets or sets the instance of the material.
    /// </summary>
    public required string MaterialInstance { get; set; }

    /// <summary>
    /// Gets or sets the textures associated with the material.
    /// </summary>
    public Textures? Textures { get; set; }

    /// <summary>
    /// Gets or sets the scalar parameters of the material.
    /// </summary>
    public ScalarParameters? ScalarParameters { get; set; }

    /// <summary>
    /// Gets or sets the vector parameters of the material.
    /// </summary>
    public List<VectorParameters>? VectorParameters { get; set; }

    /// <summary>
    /// Gets or sets the static parameters of the material.
    /// </summary>
    public List<StaticParameters>? StaticParameters { get; set; }

    /// <summary>
    /// Gets or sets the cached textures associated with the material.
    /// </summary>
    public List<CachedTextures>? CachedTextures { get; set; }
}

/// <summary>
/// Represents accessory data including the socket name, model path, and associated materials.
/// </summary>
public class AccessoryData
{
    /// <summary>
    /// Gets or sets the name of the socket to which the accessory is attached.
    /// </summary>
    public string? SocketName { get; set; }

    /// <summary>
    /// Gets or sets the file path of the accessory model.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Gets or sets the materials associated with the accessory.
    /// </summary>
    public Dictionary<string, Materials>? Materials { get; set; }
}

/// <summary>
/// Represents model data including its path, skeleton path, and various properties indicating its type and associated materials and accessories.
/// </summary>
public class UEModelData
{
    /// <summary>
    /// Gets or sets the file path of the model.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Gets or sets the file path of the skeleton.
    /// </summary>
    public string? SkeletonPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model is a static mesh.
    /// Based on cosmetic type (charm = static mesh).
    /// </summary>
    public bool IsStaticMesh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model is a weapon.
    /// </summary>
    public bool IsWeapon { get; set; }

    /// <summary>
    /// Gets or sets the materials associated with the model.
    /// </summary>
    public Dictionary<string, Materials>? Materials { get; set; }

    /// <summary>
    /// Gets or sets the accessories associated with the model.
    /// </summary>
    public List<AccessoryData>? Accessories { get; set; }
}