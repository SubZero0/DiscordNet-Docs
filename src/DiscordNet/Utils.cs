using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordNet
{
    public static class Utils
    {
        public static IEnumerable<T> RandomShuffle<T>(this IEnumerable<T> source)
        {
            return source.Select(t => new {Index = Guid.NewGuid(), Value = t}).OrderBy(p => p.Index).Select(p => p.Value);
        }
    }
}
