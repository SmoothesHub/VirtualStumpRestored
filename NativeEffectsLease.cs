using System;
using System.Reflection;

namespace VirtualStumpRestored;

internal sealed class NativeEffectsLease : IDisposable
{
    private static readonly string[] Names = { "netSerializer", "teleportToVStumpVFX", "returnFromVStumpVFX" };
    private readonly VirtualStumpTeleporter teleporter;
    private readonly FieldInfo[] fields;
    private readonly object?[] original;

    public NativeEffectsLease(VirtualStumpTeleporter source)
    {
        teleporter = source;
        fields = new FieldInfo[Names.Length];
        original = new object?[Names.Length];
        for (int i = 0; i < Names.Length; i++)
        {
            fields[i] = typeof(VirtualStumpTeleporter).GetField(Names[i], BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(VirtualStumpTeleporter).FullName, Names[i]);
            original[i] = fields[i].GetValue(source);
        }
        // This retained entrance is inactive: do not send RPCs from its dormant Photon view
        // or play particle systems on inactive objects. The game's fade and local audio still run.
        for (int i = 0; i < fields.Length; i++) fields[i].SetValue(source, null);
    }

    public void Dispose()
    {
        if (teleporter == null) return;
        for (int i = 0; i < fields.Length; i++) fields[i].SetValue(teleporter, original[i]);
    }
}
