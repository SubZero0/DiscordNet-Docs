using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordNet
{
    public class RequireTrustedMemberAttribute : PreconditionAttribute
    {
        private readonly List<ulong> _trustedMemberIds = new List<ulong>
        {
            246770367350177793, //Paulo
            66078337084162048,  //Foxbot
            81062087257755648,  //Monica/Finite
            168693960628371456, //Still
            121164164956684288, //Joe4evr
            89613772372574208,  //AntiTcb
        };

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
            => Task.FromResult(_trustedMemberIds.Contains(context.User.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Only trusted members can use this command."));
    }
}
