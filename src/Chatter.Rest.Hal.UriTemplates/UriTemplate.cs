namespace Chatter.Rest.Hal.UriTemplates;

public sealed class UriTemplate
{
    public UriTemplate(string template) => throw new NotImplementedException();

    public string Expand(IDictionary<string, string> variables) => throw new NotImplementedException();

    public string Expand(params (string Key, string Value)[] variables) => throw new NotImplementedException();

    public IReadOnlyList<string> GetVariables() => throw new NotImplementedException();
}
