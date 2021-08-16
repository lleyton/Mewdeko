using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mewdeko.Common.Attributes;
using Mewdeko.Core.Modules.Gambling.Common;
using Mewdeko.Core.Modules.Gambling.Services;
using Mewdeko.Extensions;
using Mewdeko.Modules.Administration.Services;
using Mewdeko.Modules.Gambling.Services;

namespace Mewdeko.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class PlantPickCommands : GamblingSubmodule<PlantPickService>
        {
            private readonly LogCommandService logService;

            public PlantPickCommands(LogCommandService logService, GamblingConfigService gss) : base(gss)
            {
                this.logService = logService;
            }

            [MewdekoCommand]
            [Usage]
            [Description]
            [Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick(string pass = null)
            {
                if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric()) return;

                var picked = await _service.PickAsync(ctx.Guild.Id, (ITextChannel) ctx.Channel, ctx.User.Id, pass);

                if (picked > 0)
                {
                    var msg = await ReplyConfirmLocalizedAsync("picked", picked + CurrencySign)
                        .ConfigureAwait(false);
                    msg.DeleteAfter(10);
                }

                if (((SocketGuild) ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                    try
                    {
                        logService.AddDeleteIgnore(ctx.Message.Id);
                        await ctx.Message.DeleteAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
            }

            [MewdekoCommand]
            [Usage]
            [Description]
            [Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant(int amount = 1, string pass = null)
            {
                if (amount < 1)
                    return;

                if (!string.IsNullOrWhiteSpace(pass) && !pass.IsAlphaNumeric()) return;

                var success = await _service.PlantAsync(ctx.Guild.Id, ctx.Channel, ctx.User.Id, ctx.User.ToString(),
                    amount, pass);
                if (!success)
                {
                    await ReplyErrorLocalizedAsync("not_enough", CurrencySign).ConfigureAwait(false);
                    return;
                }

                if (((SocketGuild) ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    logService.AddDeleteIgnore(ctx.Message.Id);
                    await ctx.Message.DeleteAsync().ConfigureAwait(false);
                }
            }

            [MewdekoCommand]
            [Usage]
            [Description]
            [Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
#if GLOBAL_Mewdeko
            [OwnerOnly]
#endif
            public async Task GenCurrency()
            {
                var enabled = _service.ToggleCurrencyGeneration(ctx.Guild.Id, ctx.Channel.Id);
                if (enabled)
                    await ReplyConfirmLocalizedAsync("curgen_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("curgen_disabled").ConfigureAwait(false);
            }

            [MewdekoCommand]
            [Usage]
            [Description]
            [Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            [OwnerOnly]
            public Task GenCurList(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                var enabledIn = _service.GetAllGeneratingChannels();

                return ctx.SendPaginatedConfirmAsync(page, cur =>
                {
                    var items = enabledIn.Skip(page * 9).Take(9);

                    if (!items.Any())
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription("-");

                    return items.Aggregate(new EmbedBuilder().WithOkColor(),
                        (eb, i) => eb.AddField(i.GuildId.ToString(), i.ChannelId));
                }, enabledIn.Count(), 9);
            }
        }
    }
}