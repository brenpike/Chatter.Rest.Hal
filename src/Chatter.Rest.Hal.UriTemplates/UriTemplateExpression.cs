namespace Chatter.Rest.Hal.UriTemplates;

internal sealed record UriTemplateExpression(
    UriTemplateOperator Operator,
    IReadOnlyList<string> Variables
);
