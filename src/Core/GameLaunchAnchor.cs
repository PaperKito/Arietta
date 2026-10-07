using System.Collections.Concurrent;

namespace Arietta.Core;















internal static class GameLaunchAnchor
{
    private static readonly ConcurrentDictionary<GameBiz, int> _anchors = new();

    public static void Set(GameBiz biz, int pid) => _anchors[biz] = pid;

    public static int Get(GameBiz biz) => _anchors.TryGetValue(biz, out int pid) ? pid : 0;

    public static void Clear(GameBiz biz) => _anchors.TryRemove(biz, out _);
}
