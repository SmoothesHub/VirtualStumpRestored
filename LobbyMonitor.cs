using System;
using VirtualStumpRestored.Core;

namespace VirtualStumpRestored;

internal sealed class LobbyMonitor : IDisposable
{
    private NetworkSystem? network;
    private bool leaving;
    private string roomName = "";
    public LobbyKind Kind { get; private set; }
    public bool PhysicsAllowed => LobbyPolicy.AllowsPhysics(Kind);
    public event Action? Changed;

    public void Refresh()
    {
        var current = NetworkSystem.Instance;
        if (network != current)
        {
            Detach();
            network = current;
            leaving = false;
            if (network != null)
            {
                network.OnJoinedRoomEvent += RoomSettled;
                network.OnReturnedToSinglePlayer += RoomSettled;
                network.OnPreLeavingRoom += BeforeLeave;
            }
        }
        if (network == null) { Set(LobbyKind.Unavailable, ""); return; }
        bool inRoom = network.InRoom;
        if (leaving && !inRoom && network.netState == NetSystemState.Idle) leaving = false;
        bool stable = !leaving && (network.netState == NetSystemState.Idle || network.netState == NetSystemState.InGame);
        Set(LobbyPolicy.Classify(true, stable, inRoom, stable && inRoom && network.SessionIsPrivate), inRoom ? network.RoomName : "");
    }

    private void BeforeLeave() { leaving = true; Set(LobbyKind.Unavailable, ""); }
    private void RoomSettled() { leaving = false; Refresh(); Changed?.Invoke(); }
    private void Set(LobbyKind kind, string name)
    {
        if (Kind == kind && roomName == name) return;
        Kind = kind;
        roomName = name;
        Changed?.Invoke();
    }
    private void Detach()
    {
        if (network == null) return;
        network.OnJoinedRoomEvent -= RoomSettled;
        network.OnReturnedToSinglePlayer -= RoomSettled;
        network.OnPreLeavingRoom -= BeforeLeave;
    }
    public void Dispose() { Detach(); network = null; }
}
