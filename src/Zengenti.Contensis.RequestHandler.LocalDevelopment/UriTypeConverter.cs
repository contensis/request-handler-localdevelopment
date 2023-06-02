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

    public object? ReadYaml(IParser parser, Type type)
    {
        var uri = parser.Consume<Scalar>();
        if (uri != null)
        {
            return new Uri(uri.Value);
        }

        return null;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
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