namespace Chatter.Rest.UriTemplates;

internal sealed record UriTemplateExpression(
    UriTemplateOperator Operator,
    IReadOnlyList<string> Variables
);
