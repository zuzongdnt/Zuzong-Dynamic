using System.Runtime;

namespace VNotch.Services;

internal static class ZuzongLiteMemory
{
    static long _lastCollection;
    static int _collecting;

    internal static void ReleaseIdleGarbage(Func<bool> stillIdle)
    {
        long now = Environment.TickCount64;
        if (now - Interlocked.Read(ref _lastCollection) < 60000 || Interlocked.Exchange(ref _collecting, 1) != 0) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (!stillIdle()) return;
                Interlocked.Exchange(ref _lastCollection, Environment.TickCount64);
                // Reclaim actual obsolete LOH maps/bitmaps after an idle resize;
                // do not trim the working set or page out live application data.
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
#pragma warning disable S1215
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
#pragma warning restore S1215
            }
            finally { Volatile.Write(ref _collecting, 0); }
        });
    }
}
