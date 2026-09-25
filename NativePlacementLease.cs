using System;
using UnityEngine;

namespace VirtualStumpRestored;

internal sealed class NativePlacementLease : IDisposable
{
    private readonly Transform root;
    private readonly Transform parent;
    private readonly Vector3 position;
    private readonly Quaternion rotation;
    private readonly Vector3 scale;
    private readonly Transform model;

    public NativePlacementLease(Transform source, Transform placedModel)
    {
        root = source;
        parent = source.parent;
        position = source.localPosition;
        rotation = source.localRotation;
        scale = source.localScale;
        model = placedModel;
        // The inactive native trigger and return point must follow the restored model too.
        Refresh();
    }

    public void Refresh()
    {
        if (root == null || model == null || parent == null) return;
        if (root.position != model.position || root.rotation != model.rotation)
            root.SetPositionAndRotation(model.position, model.rotation);
        Vector3 targetScale = model.lossyScale;
        Vector3 parentScale = parent.lossyScale;
        Vector3 localScale = new Vector3(targetScale.x / parentScale.x, targetScale.y / parentScale.y, targetScale.z / parentScale.z);
        if (root.localScale != localScale) root.localScale = localScale;
    }

    public void Dispose()
    {
        if (root == null || parent == null) return;
        root.SetParent(parent, false);
        root.localPosition = position;
        root.localRotation = rotation;
        root.localScale = scale;
    }
}
