using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VirtualStumpRestored;

internal sealed class TeleporterView : IDisposable
{
    private readonly GameObject root;
    private readonly TeleporterBinding binding;
    private readonly Collider[] colliders;
    private readonly FixedGrip[] grips;
    private readonly CountdownDisplay countdown;
    private readonly NativePlacementLease placement;
    private readonly bool originalActive;
    private readonly NativeEffectsLease effects;
    private bool physical;

    public bool Valid => root != null && binding.Valid && countdown.Valid;
    public bool Active => Valid && root.activeInHierarchy;

    public TeleporterView(TeleporterBinding source, Mesh suppliedMesh, Shader countdownShader, LobbyMonitor lobby)
    {
        binding = source;
        originalActive = source.Root.gameObject.activeSelf;
        root = new GameObject("Virtual Stump Restored");
        root.SetActive(false);
        TeleporterGeometry.PlaceAtDesk(root.transform, source.Monitor);
        colliders = new Collider[5];
        grips = new FixedGrip[2];
        try
        {
            if (Vector3.Distance(suppliedMesh.bounds.center, source.Body.sharedMesh.bounds.center) > 0.002f ||
                Vector3.Distance(suppliedMesh.bounds.size, source.Body.sharedMesh.bounds.size) > 0.002f)
                throw new InvalidOperationException("The supplied model no longer matches the scene's teleporter coordinate system.");
            colliders[0] = CreateSurface(source.Body, suppliedMesh, "Supplied FBX");
            var handles = new[] { source.LeftHandle, source.RightHandle };
            for (int i = 0; i < handles.Length; i++)
            {
                colliders[i + 1] = CreateSurface(handles[i], handles[i].sharedMesh, handles[i].name);
                var zone = TeleporterGeometry.AddGrabZone(colliders[i + 1].transform, handles[i].sharedMesh.bounds);
                colliders[i + 3] = zone;
                grips[i] = zone.gameObject.AddComponent<FixedGrip>();
                grips[i].Initialize(lobby, zone);
                grips[i].enabled = false;
            }
            countdown = new CountdownDisplay(source.FontSource.font, countdownShader);
            effects = new NativeEffectsLease(source.NativeTeleporter);
            source.Root.gameObject.SetActive(false);
            placement = new NativePlacementLease(source.Root, root.transform);
            root.SetActive(true);
        }
        catch
        {
            if (effects != null) effects.Dispose();
            if (placement != null) placement.Dispose();
            if (countdown != null) countdown.Dispose();
            if (source.Root != null) source.Root.gameObject.SetActive(originalActive);
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }

    private MeshCollider CreateSurface(MeshFilter source, Mesh mesh, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        CopyLocal(source.transform, go.transform);
        go.layer = source.gameObject.layer;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var nativeRenderer = source.GetComponent<MeshRenderer>();
        if (nativeRenderer == null || nativeRenderer.sharedMaterial == null || !nativeRenderer.sharedMaterial.shader.isSupported)
            throw new InvalidOperationException("The installed game's teleporter material is unavailable.");
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = nativeRenderer.sharedMaterials;
        renderer.shadowCastingMode = nativeRenderer.shadowCastingMode;
        renderer.receiveShadows = nativeRenderer.receiveShadows;
        renderer.lightProbeUsage = nativeRenderer.lightProbeUsage;
        var properties = new MaterialPropertyBlock();
        nativeRenderer.GetPropertyBlock(properties);
        renderer.SetPropertyBlock(properties);
        var collider = go.AddComponent<MeshCollider>();
        collider.enabled = false;
        collider.sharedMesh = mesh;
        collider.convex = false;
        collider.isTrigger = false;
        var originalCollider = source.GetComponent<Collider>();
        if (originalCollider != null) collider.sharedMaterial = originalCollider.sharedMaterial;
        return collider;
    }

    public void SetPhysics(bool enabled)
    {
        // Keep the retained original nonphysical even if another game lifecycle step reactivates it.
        if (binding.Root != null && binding.Root.gameObject.activeSelf) binding.Root.gameObject.SetActive(false);
        if (!Valid) enabled = false;
        if (physical == enabled) return;
        physical = enabled;
        foreach (var collider in colliders) if (collider != null) collider.enabled = enabled;
        if (!enabled) ReleaseHands();
        foreach (var grip in grips) if (grip != null) grip.enabled = enabled;
    }

    public void ReleaseHands()
    {
        foreach (var grip in grips) if (grip != null) grip.ReleaseAll();
    }

    public bool HeadOverlaps(SphereCollider head)
    {
        if (!Active || head == null || !head.enabled) return false;
        placement.Refresh();
        Vector3 headCenter = head.transform.TransformPoint(head.center);
        Vector3 scale = head.transform.lossyScale;
        float radius = head.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return HeadVolume.Overlaps(binding.HeadArea, headCenter, radius);
    }

    public void ShowCountdown(int number, Camera? camera) => countdown.Show(number, camera);

    private static void CopyLocal(Transform source, Transform target)
    {
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    public void Dispose()
    {
        if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
        ReleaseHands();
        countdown.Dispose();
        placement.Dispose();
        effects.Dispose();
        if (originalActive && binding.Root != null) binding.Root.gameObject.SetActive(true);
    }
}
