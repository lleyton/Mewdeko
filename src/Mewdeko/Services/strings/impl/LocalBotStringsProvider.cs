namespace Mewdeko.Services.strings.impl;

public class LocalBotStringsProvider : IBotStringsProvider
{
    private readonly IStringsSource _source;
    private IReadOnlyDictionary<string, Dictionary<string, CommandStrings>> commandStrings;
    private IReadOnlyDictionary<string, Dictionary<string, string>> responseStrings;

    public LocalBotStringsProvider(IStringsSource source)
    {
        _source = source;
        Reload();
    }

    public string? GetText(string localeName, string? key)
    {
        if (responseStrings.TryGetValue(localeName, out var langStrings)
            && langStrings.TryGetValue(key, out var text))
        {
            return text;
        }

        return null;
    }

    public void Reload()
    {
        responseStrings = _source.GetResponseStrings();
        commandStrings = _source.GetCommandStrings();
    }

    public CommandStrings? GetCommandStrings(string localeName, string commandName)
    {
        if (commandStrings.TryGetValue(localeName, out var langStrings)
            && langStrings.TryGetValue(commandName, out var strings))
        {
            return strings;
        }

        return null;
    }
}