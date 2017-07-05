using Discord;
using System.Collections.Generic;

namespace DiscordNet.EmbedExtension
{
    class PaginatorBuilder : EmbedBuilder
    {
        public IEnumerable<string> Pages { get; set; }
    }
}
