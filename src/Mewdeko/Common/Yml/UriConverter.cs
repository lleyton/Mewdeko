﻿using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Mewdeko.Common.Yml;

public class UriConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Uri);

    public object ReadYaml(IParser parser, Type type)
    {
        var scalar = parser.Consume<Scalar>();
        return new Uri(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var uri = (Uri)value;
        emitter.Emit(new Scalar(uri.ToString()));
    }
}