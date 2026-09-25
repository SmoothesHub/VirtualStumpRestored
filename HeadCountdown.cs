using System;

namespace VirtualStumpRestored.Core;

internal sealed class HeadCountdown
{
    public int Number { get; private set; }
    public bool AwaitingExit { get; private set; }
    private double enteredAt = double.NaN;
    private double lastTick = double.NaN;

    public bool Tick(double now, bool inside, bool allowed)
    {
        if (!inside)
        {
            Reset();
            return false;
        }
        // Permission changes and completed attempts require a fresh head entry.
        if (!allowed || AwaitingExit)
        {
            CancelUntilExit();
            return false;
        }
        if (!double.IsNaN(lastTick) && (now < lastTick || now - lastTick > 0.5))
            enteredAt = double.NaN;
        lastTick = now;
        if (double.IsNaN(enteredAt)) enteredAt = now;
        double elapsed = now - enteredAt;
        if (elapsed >= 3)
        {
            CancelUntilExit();
            return true;
        }
        Number = Math.Max(1, (int)Math.Ceiling(3 - elapsed));
        return false;
    }

    public void CancelUntilExit()
    {
        enteredAt = lastTick = double.NaN;
        Number = 0;
        AwaitingExit = true;
    }

    public void Reset()
    {
        enteredAt = lastTick = double.NaN;
        Number = 0;
        AwaitingExit = false;
    }
}
