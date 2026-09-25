using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VirtualStumpRestored;

internal sealed class FixedGrip : HandHold
{
    private static readonly FieldInfo GrabbersField = typeof(HandHold).GetField("currentGrabbers", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(HandHold).FullName, "currentGrabbers");
    private static readonly FieldInfo MomentaryField = typeof(HandHold).GetField("forceMomentary", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(HandHold).FullName, "forceMomentary");
    private List<GorillaGrabber>? grabbers;
    private LobbyMonitor? lobby;
    private Collider? surface;
    public void Initialize(LobbyMonitor monitor, Collider collider)
    {
        lobby = monitor;
        surface = collider;
        grabbers = (List<GorillaGrabber>)GrabbersField.GetValue(this);
        MomentaryField.SetValue(this, false);
    }
    public override bool CanBeGrabbed(GorillaGrabber grabber) => isActiveAndEnabled && surface != null && surface.enabled &&
        lobby != null && lobby.PhysicsAllowed && grabber != null && grabber.Player != null;

    public void ReleaseAll()
    {
        if (grabbers == null) return;
        // The native release callback removes an entry. Iterate backwards so two hands are both released.
        for (int i = grabbers.Count - 1; i >= 0; i--)
        {
            var grabber = grabbers[i];
            if (grabber != null && grabber.Player != null) grabber.Inject(null, Vector3.zero);
            else grabbers.RemoveAt(i);
        }
    }
    public new void OnDisable() => ReleaseAll();
}
