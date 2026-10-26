using System.Collections.Generic;

namespace CurioCatalogue.Core;

public static class Multiset
{
    /// <summary>
    /// Merges entries of the same item into one, summing counts. Keeps the order in which items first
    /// appear and the first entry's sample.
    /// </summary>
    public static List<Entry> Sum(IEnumerable<Entry> entries)
    {
        var result = new List<Entry>();
        var index = new Dictionary<ItemKey, int>();
        foreach (var entry in entries)
        {
            if (entry.Count <= 0) continue;
            if (index.TryGetValue(entry.Key, out int i))
            {
                result[i] = result[i].WithCount(result[i].Count + entry.Count);
            }
            else
            {
                index[entry.Key] = result.Count;
                result.Add(entry);
            }
        }
        return result;
    }
}
