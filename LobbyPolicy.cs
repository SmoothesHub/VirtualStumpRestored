namespace VirtualStumpRestored.Core;

internal enum LobbyKind { Unavailable, Offline, Private, Public }

internal static class LobbyPolicy
{
    public static LobbyKind Classify(bool networkAvailable, bool stable, bool inRoom, bool sessionPrivate)
    {
        if (!networkAvailable || !stable) return LobbyKind.Unavailable;
        if (!inRoom) return LobbyKind.Offline;
        return sessionPrivate ? LobbyKind.Private : LobbyKind.Public;
    }

    public static bool AllowsPhysics(LobbyKind kind) => kind == LobbyKind.Offline || kind == LobbyKind.Private;
}
