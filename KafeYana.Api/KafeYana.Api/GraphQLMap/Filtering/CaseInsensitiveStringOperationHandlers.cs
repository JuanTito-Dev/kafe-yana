using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;

namespace KafeYana.Api.GraphQLMap.Filtering;

internal sealed class CaseInsensitiveStringEqualsHandler(InputParser inputParser)
    : CaseInsensitiveStringOperationHandler(inputParser, DefaultFilterOperations.Equals)
{
    protected override Expression BuildComparison(Expression property, string value) =>
        Expression.Equal(
            Expression.Call(property, ToLowerMethod),
            Expression.Constant(value.ToLowerInvariant()));
}

internal sealed class CaseInsensitiveStringContainsHandler(InputParser inputParser)
    : CaseInsensitiveStringOperationHandler(inputParser, DefaultFilterOperations.Contains)
{
    protected override Expression BuildComparison(Expression property, string value) =>
        Expression.Call(
            Expression.Call(property, ToLowerMethod),
            ContainsMethod,
            Expression.Constant(value.ToLowerInvariant()));
}

internal sealed class CaseInsensitiveStringStartsWithHandler(InputParser inputParser)
    : CaseInsensitiveStringOperationHandler(inputParser, DefaultFilterOperations.StartsWith)
{
    private static readonly MethodInfo StartsWithMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    protected override Expression BuildComparison(Expression property, string value) =>
        Expression.Call(
            Expression.Call(property, ToLowerMethod),
            StartsWithMethod,
            Expression.Constant(value.ToLowerInvariant()));
}

internal sealed class CaseInsensitiveStringEndsWithHandler(InputParser inputParser)
    : CaseInsensitiveStringOperationHandler(inputParser, DefaultFilterOperations.EndsWith)
{
    private static readonly MethodInfo EndsWithMethod =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

    protected override Expression BuildComparison(Expression property, string value) =>
        Expression.Call(
            Expression.Call(property, ToLowerMethod),
            EndsWithMethod,
            Expression.Constant(value.ToLowerInvariant()));
}

internal abstract class CaseInsensitiveStringOperationHandler(InputParser inputParser, int operation)
    : QueryableStringOperationHandler(inputParser)
{
    protected static readonly MethodInfo ToLowerMethod = typeof(string)
        .GetMethods()
        .Single(m => m.Name == nameof(string.ToLower) && m.GetParameters().Length == 0);

    protected static readonly MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    protected override int Operation => operation;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();

        if (parsedValue is string str)
            return BuildComparison(property, str);

        throw new InvalidOperationException();
    }

    protected abstract Expression BuildComparison(Expression property, string value);
}

internal sealed class CaseInsensitiveFilteringConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.Provider(new QueryableFilterProvider(x =>
            x.AddFieldHandler<CaseInsensitiveStringEqualsHandler>()
             .AddFieldHandler<CaseInsensitiveStringContainsHandler>()
             .AddFieldHandler<CaseInsensitiveStringStartsWithHandler>()
             .AddFieldHandler<CaseInsensitiveStringEndsWithHandler>()
             .AddDefaultFieldHandlers()));
    }
}
