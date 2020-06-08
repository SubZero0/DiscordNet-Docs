using Discord;
using System.Collections.Generic;

namespace DiscordNet
{
    class PaginatorBuilder : EmbedBuilder
    {
        public IEnumerable<string> Pages { get; set; }
    }
}
