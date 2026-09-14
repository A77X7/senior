using System.Collections.Concurrent;

namespace folder_watcher.Services;

public sealed class PublishedFiles
{
    private readonly ConcurrentDictionary<string, byte> _files = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAdd(string path) => _files.TryAdd(path, 0);

    public bool Remove(string path) => _files.TryRemove(path, out _);
}
