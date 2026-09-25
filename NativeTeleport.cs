using System;
using System.Collections.Generic;
using System.Reflection;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

namespace VirtualStumpRestored;

internal sealed class NativeTeleport
{
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly Action<VirtualStumpTeleporter, Action<bool>> enter;
    private readonly FieldInfo managerField;
    private readonly FieldInfo destinationsField;
    private readonly Func<bool> hasNoMapAccess;
    private CustomMapManager? requestManager;
    private int requestVersion;
    public bool InFlight { get; private set; }

    public NativeTeleport()
    {
        var type = typeof(CustomMapManager);
        var method = type.GetMethod("TeleportToVirtualStump", StaticPrivate, null,
            new[] { typeof(VirtualStumpTeleporter), typeof(Action<bool>) }, null)
            ?? throw new MissingMethodException(type.FullName, "TeleportToVirtualStump");
        enter = (Action<VirtualStumpTeleporter, Action<bool>>)Delegate.CreateDelegate(typeof(Action<VirtualStumpTeleporter, Action<bool>>), method);
        managerField = type.GetField("instance", StaticPrivate) ?? throw new MissingFieldException(type.FullName, "instance");
        destinationsField = type.GetField("virtualStumpTeleportLocations", InstancePrivate) ?? throw new MissingFieldException(type.FullName, "virtualStumpTeleportLocations");
        var permissions = type.Assembly.GetType("UGCPermissionManager", true)!;
        var getter = permissions.GetProperty("HasNoMapAccess", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(true)
            ?? throw new MissingMemberException(permissions.FullName, "HasNoMapAccess");
        hasNoMapAccess = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), getter);
    }

    public bool CanEnter => !InFlight && !hasNoMapAccess() &&
        GorillaComputer.instance != null && !GorillaComputer.instance.IsPlayerInVirtualStump() &&
        !CustomMapManager.WaitingForRoomJoin && !CustomMapManager.WaitingForDisconnect;

    public bool TryEnter(VirtualStumpTeleporter teleporter, Action<bool> completed)
    {
        if (!CanEnter || teleporter == null || teleporter.GetReturnTransform() == null) return false;
        var manager = managerField.GetValue(null) as CustomMapManager;
        if (manager == null || !(destinationsField.GetValue(manager) is List<Transform> destinations) ||
            destinations.Count == 0 || destinations.Exists(t => t == null)) return false;
        InFlight = true;
        requestManager = manager;
        int version = ++requestVersion;
        try
        {
            enter(teleporter, success =>
            {
                if (version != requestVersion) return;
                InFlight = false;
                requestManager = null;
                completed(success);
            });
            return true;
        }
        catch
        {
            InFlight = false;
            requestManager = null;
            throw;
        }
    }

    public void ObserveCompletion()
    {
        // The game callback is authoritative. Do not time out and start a second transition.
        if (InFlight && requestManager == null)
        {
            requestVersion++;
            InFlight = false;
        }
    }
}
