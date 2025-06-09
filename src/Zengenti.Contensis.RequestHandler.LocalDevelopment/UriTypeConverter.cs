using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment;

public sealed class UriTypeConverter : IYamlTypeConverter
{
    private static readonly Type UriType = typeof(Uri);

    public bool Accepts(Type type)
    {
        return type == UriType;
    }

    // ReSharper disable once ReturnTypeCanBeNotNullable
    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var uri = parser.Consume<Scalar>();
        return new Uri(uri.Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        // NOTE: Not needed for us, but leaving this as an example
        //var uri = (Uri)value;
        //if (uri != null)
        //{
        //    emitter.Emit(new Scalar(null, "Name"));
        //    emitter.Emit(new Scalar(null, uri.ToString()));
        //}
    }
}