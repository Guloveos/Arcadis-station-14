using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.Client.Parallax.Data;
using Content.Shared;
using Content.Shared.CCVar;
using Nett;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Parallax.Managers;

internal sealed class ParallaxManager : IParallaxManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private string _parallaxName = "";
    public string ParallaxName
    {
        get => _parallaxName;
        set
        {
            LoadParallaxByName(value);
        }
    }

    public Vector2 ParallaxAnchor { get; set; }

    private ParallaxLayerPrepared[] _parallaxLayersHQ = {};
    private ParallaxLayerPrepared[] _parallaxLayersLQ = {};

    public ParallaxLayerPrepared[] ParallaxLayers => _configurationManager.GetCVar(CCVars.ParallaxLowQuality) ? _parallaxLayersLQ : _parallaxLayersHQ;

    public async void LoadParallax()
    {
        await LoadParallaxByName("default");
    }

    private async Task LoadParallaxByName(string name)
    {
        _parallaxName = name;
        Logger.InfoS("parallax", $"Loading parallax {name}");

        var parallaxPrototype = _prototypeManager.Index<ParallaxPrototype>(name);

        var hq = await LoadParallaxLayers(parallaxPrototype.Layers.ToArray());
        var lq = parallaxPrototype.LayersLQUseHQ ? hq : await LoadParallaxLayers(parallaxPrototype.LayersLQ.ToArray());

        if (_parallaxName == name)
        {
            _parallaxLayersHQ = hq;
            _parallaxLayersLQ = lq;
            Logger.InfoS("parallax", $"Loaded parallax {name}");
        }
        else
        {
            Logger.InfoS("parallax", $"Loaded parallax {name}, but the target changed while it was being loaded.");
        }
    }

    private async Task<ParallaxLayerPrepared[]> LoadParallaxLayers(ParallaxLayerConfig[] layersIn)
    {
        var layers = new ParallaxLayerPrepared[layersIn.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            layers[i] = await LoadParallaxLayer(layersIn[i]);
        }
        return layers;
    }

    private async Task<ParallaxLayerPrepared> LoadParallaxLayer(ParallaxLayerConfig config)
    {
        return new ParallaxLayerPrepared()
        {
            Texture = await config.Texture.GenerateTexture(),
            Config = config
        };
    }
}

