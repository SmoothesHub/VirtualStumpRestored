using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace VirtualStumpRestored;

internal sealed class ModelAssets : IDisposable
{
    private readonly AssetBundle bundle;
    public Mesh Mesh { get; }
    public Shader CountdownShader { get; }

    public ModelAssets()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VirtualStumpRestored.Assets.virtualstumpassets")
            ?? throw new FileNotFoundException("The supplied FBX bundle is missing from the plugin.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        bundle = AssetBundle.LoadFromMemory(buffer.ToArray());
        if (bundle == null) throw new InvalidDataException("Unity could not load the teleporter asset bundle.");
        var prefab = bundle.LoadAsset<GameObject>("teleporter-model");
        var filters = prefab != null ? prefab.GetComponentsInChildren<MeshFilter>(true) : Array.Empty<MeshFilter>();
        if (filters.Length != 1 || filters[0].sharedMesh == null)
        {
            bundle.Unload(true);
            throw new InvalidDataException("Expected the supplied FBX's single teleporter mesh.");
        }
        Mesh = filters[0].sharedMesh;
        CountdownShader = bundle.LoadAsset<Shader>("countdown-overlay");
        if (CountdownShader == null || !CountdownShader.isSupported)
        {
            bundle.Unload(true);
            throw new InvalidDataException("The VR countdown shader is missing or unsupported. Rebuild the asset bundle.");
        }
        // The FBX's local mesh is correct; its scene-export root rotation is not the in-game rotation.
        if (Mesh.triangles.Length != 2688 || Vector3.Distance(Mesh.bounds.size, new Vector3(1.111147f, 0.475369f, 0.582564f)) > 0.002f)
        {
            bundle.Unload(true);
            throw new InvalidDataException("Unexpected model bounds or triangle count. Rebuild using the original supplied FBX.");
        }
    }
    public void Dispose() { if (bundle != null) bundle.Unload(true); }
}
