﻿using Mewdeko.Services.Settings;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;

namespace Mewdeko.Services.Impl;

public class Localization : ILocalization
{
    private static readonly Dictionary<string, CommandData> _commandData =
        JsonConvert.DeserializeObject<Dictionary<string, CommandData>>(
            File.ReadAllText("./data/strings/commands/commands.en-US.json"));

    private readonly BotConfigService _bss;
    private readonly DbService _db;

    public Localization(BotConfigService bss, DiscordSocketClient client, DbService db)
    {
        _bss = bss;
        _db = db;
        using var uow = db.GetDbContext();
        var gc = uow.GuildConfigs.All().Where(x => client.Guilds.Select(socketGuild => socketGuild.Id).Contains(x.GuildId));
        var cultureInfoNames = gc
            .ToDictionary(x => x.GuildId, x => x.Locale);

        GuildCultureInfos = new ConcurrentDictionary<ulong, CultureInfo?>(cultureInfoNames.ToDictionary(x => x.Key,
            x =>
            {
                var cultureInfo = new CultureInfo("en-US");
                try
                {
                    switch (x.Value)
                    {
                        case null:
                            return null;
                        case "english":
                            cultureInfo = new CultureInfo("en-US");
                            break;
                        default:
                            try
                            {
                                cultureInfo = new CultureInfo(x.Value);
                            }
                            catch
                            {
                                cultureInfo = new CultureInfo("en-US");
                            }
                            break;
                    }
                }
                catch
                {
                    // ignored
                }

                return cultureInfo;
            }).Where(x => x.Value != null));
    }

    public ConcurrentDictionary<ulong, CultureInfo?> GuildCultureInfos { get; }
    public CultureInfo? DefaultCultureInfo => _bss.Data.DefaultLocale;

    public void SetGuildCulture(IGuild guild, CultureInfo? ci) => SetGuildCulture(guild.Id, ci);

    public async void SetGuildCulture(ulong guildId, CultureInfo? ci)
    {
        if (ci?.Name == _bss.Data.DefaultLocale?.Name)
        {
            RemoveGuildCulture(guildId);
            return;
        }

        await using (var uow = _db.GetDbContext())
        {
            var gc = await uow.ForGuildId(guildId, set => set);
            gc.Locale = ci.Name;
            await uow.SaveChangesAsync().ConfigureAwait(false);
        }

        GuildCultureInfos.AddOrUpdate(guildId, ci, (_, _) => ci);
    }

    public void RemoveGuildCulture(IGuild guild) => RemoveGuildCulture(guild.Id);

    public async void RemoveGuildCulture(ulong guildId)
    {
        if (!GuildCultureInfos.TryRemove(guildId, out _)) return;
        await using var uow = _db.GetDbContext();
        var gc = await uow.ForGuildId(guildId, set => set);
        gc.Locale = null;
        await uow.SaveChangesAsync().ConfigureAwait(false);
    }

    public void SetDefaultCulture(CultureInfo? ci) => _bss.ModifyConfig(bs => bs.DefaultLocale = ci);

    public void ResetDefaultCulture() => SetDefaultCulture(CultureInfo.CurrentCulture);

    public CultureInfo? GetCultureInfo(IGuild? guild) => GetCultureInfo(guild?.Id);

    public CultureInfo? GetCultureInfo(ulong? guildId)
    {
        if (guildId is null || !GuildCultureInfos.TryGetValue(guildId.Value, out var info) || info is null)
            return _bss.Data.DefaultLocale;

        return info;
    }

    public static CommandData LoadCommand(string key)
    {
        _commandData.TryGetValue(key, out var toReturn);

        if (toReturn == null)
        {
            return new CommandData
            {
                Cmd = key,
                Desc = key,
                Usage = new[] { key }
            };
        }

        return toReturn;
    }
}