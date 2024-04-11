using System.Diagnostics;
using System.Net.Http.Headers;
using Zengenti.Contensis.RequestHandler.Domain.Common;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

[DebuggerDisplay("{_values}")]
public sealed class Headers
{
    public readonly string[] CaseInsensitiveHeaderNames =
    {
        "host"
    };

    private readonly Dictionary<string, IEnumerable<string>> _values;

    public IReadOnlyDictionary<string, IEnumerable<string>> Values => _values.ToReadOnlyDictionary();

    public Headers()
    {
        _values = new Dictionary<string, IEnumerable<string>>();
    }

    public Headers(Dictionary<string, IEnumerable<string>>? dictionary)
    {
        _values = dictionary ?? new Dictionary<string, IEnumerable<string>>();
    }

    public Headers(IHeaderDictionary headerDictionary)
    {
        _values = headerDictionary.ToDictionary(
            h =>
                h.Key,
            h => h.Value.AsEnumerable());
    }

    public Headers(Dictionary<string, string>? headers)
    {
        _values =
            _values = headers is null
                ? new Dictionary<string, IEnumerable<string>>()
                : headers.ToDictionary(
                    x => x.Key,
                    x => new[]
                    {
                        x.Value
                    }.Select(header => header));
    }

    public Headers(HttpResponseHeaders? headers)
    {
        _values = headers is null
            ? new Dictionary<string, IEnumerable<string>>()
            : headers.ToDictionary(h => h.Key, h => h.Value);
    }

    public string? SiteType => GetFirstValueIfExists(Constants.Headers.ServerType);

    public string? HidePreviewToolbar => GetFirstValueIfExists(Constants.Headers.HidePreviewToolbar);

    public string? EntryVersionStatus => GetFirstValueIfExists(Constants.Headers.EntryVersionStatus);

    public bool Debug => GetFirstValueIfExists(Constants.Headers.Debug) == "true";

    public static implicit operator Headers(Dictionary<string, IEnumerable<string>> dictionary) => new(dictionary);

    public static implicit operator Dictionary<string, IEnumerable<string>>(Headers headers) => new(headers._values);

    public string? this[string name]
    {
        set =>
            SetValue(
                name,
                new[]
                {
                    value
                }!);
    }

    public bool HasKey(string key)
    {
        return _values.ContainsKey(key);
    }

    public void Add(string key, IEnumerable<string> value)
    {
        SetValue(key, value);
    }

    public Headers Merge(Headers headers)
    {
        foreach (var headerKvp in headers._values)
        {
            SetValue(headerKvp.Key, headerKvp.Value);
        }

        return this;
    }

    private void SetValue(string key, IEnumerable<string> value)
    {
        if (_values.ContainsKey(key))
        {
            _values[key] = value;
        }
        else
        {
            if (CaseInsensitiveHeaderNames.ContainsCaseInsensitive(key))
            {
                var storedKey = _values.Keys.SingleOrDefault(k => k.EqualsCaseInsensitive(key));
                key = storedKey ?? key;
            }

            _values[key] = value;
        }
    }

    public string? GetFirstValueIfExists(string headerName)
    {
        var key = CaseInsensitiveHeaderNames.ContainsCaseInsensitive(headerName)
            ? _values.Keys.SingleOrDefault(k => k.EqualsCaseInsensitive(headerName))
            : headerName;

        if (key is null)
        {
            return null;
        }

        if (_values.TryGetValue(key, out var value))
        {
            return value.FirstOrDefault();
        }

        return null;
    }
}