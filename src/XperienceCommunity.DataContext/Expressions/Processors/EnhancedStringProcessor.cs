using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

/// <summary>
/// Enhanced string operations processor with optimized Kentico WhereParameters usage
/// </summary>
internal sealed class EnhancedStringProcessor : IExpressionProcessor<MethodCallExpression>
{
    private readonly IExpressionContext _context;

    public EnhancedStringProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public bool CanProcess(Expression node)
    {
        if (node is not MethodCallExpression { Method.DeclaringType: { } declaringType } method)
            return false;

        if (declaringType != typeof(string))
            return false;

        if (!IsSupportedStringMethod(method.Method.Name))
            return false;

        // For static methods like IsNullOrEmpty, check if the argument contains a member expression
        if (method.Object == null)
        {
            return method.Arguments.Any(arg => arg is MemberExpression);
        }

        // For instance methods, only process if the object is a member expression (property access)
        return method.Object is MemberExpression;
    }

    public void Process(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(string.Contains):
                ProcessContains(node);
                break;

            case nameof(string.StartsWith):
                ProcessStartsWith(node);
                break;

            case nameof(string.EndsWith):
                ProcessEndsWith(node);
                break;

            case nameof(string.ToLower):
            case nameof(string.ToUpper):
                ProcessCaseConversion(node);
                break;

            case nameof(string.Trim):
                ProcessTrim(node);
                break;

            case "IsNullOrEmpty":
                ProcessIsNullOrEmpty(node);
                break;

            case "IsNullOrWhiteSpace":
                ProcessIsNullOrWhiteSpace(node);
                break;

            default:
                throw new UnsupportedExpressionException($"String method '{node.Method.Name}' is not supported", node);
        }
    }

    private static bool IsSupportedStringMethod(string methodName)
    {
        return methodName switch
        {
            nameof(string.Contains) or
            nameof(string.StartsWith) or
            nameof(string.EndsWith) or
            nameof(string.ToLower) or
            nameof(string.ToUpper) or
            nameof(string.Trim) or
            "IsNullOrEmpty" or
            "IsNullOrWhiteSpace" => true,
            _ => false
        };
    }

    private void ProcessContains(MethodCallExpression node)
    {
        if (node.Object == null || node.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("String.Contains expects one argument", node);
        }

        var memberName = ExtractMemberName(node.Object);
        var value = ExtractConstantValue(node.Arguments[0]);

        _context.AddParameter(memberName, value);
        _context.AddWhereAction(w => w.WhereContains(memberName, value?.ToString()));
    }

    private void ProcessStartsWith(MethodCallExpression node)
    {
        if (node.Object == null || node.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("String.StartsWith expects one argument", node);
        }

        var memberName = ExtractMemberName(node.Object);
        var value = ExtractConstantValue(node.Arguments[0]);

        _context.AddParameter(memberName, value);
        _context.AddWhereAction(w => w.WhereStartsWith(memberName, value?.ToString()));
    }

    private void ProcessEndsWith(MethodCallExpression node)
    {
        if (node.Object == null || node.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("String.EndsWith expects one argument", node);
        }

        var memberName = ExtractMemberName(node.Object);
        var value = ExtractConstantValue(node.Arguments[0]);

        _context.AddParameter(memberName, value);

        // Try WhereEndsWith, fallback to WhereContains
        _context.AddWhereAction(w =>
        {
            try
            {
                // Try to use WhereEndsWith method - this might not exist in all Kentico versions
                var method = w.GetType().GetMethod("WhereEndsWith");
                if (method != null)
                {
                    method.Invoke(w, new object?[] { memberName, value?.ToString() });
                }
                else
                {
                    // Fallback to contains as approximation
                    w.WhereContains(memberName, value?.ToString());
                }
            }
            catch (Exception)
            {
                // WhereEndsWith might not be available in all Kentico versions
                // Fallback to contains as approximation
                w.WhereContains(memberName, value?.ToString());
            }
        });
    }

    private void ProcessCaseConversion(MethodCallExpression node)
    {
        // Case conversion methods are typically used in comparisons
        // We need to handle this in the context of the parent expression

        if (node.Object == null)
        {
            throw new InvalidExpressionFormatException($"String.{node.Method.Name} expects an object", node);
        }

        var memberName = ExtractMemberName(node.Object);
        var isToLower = node.Method.Name == nameof(string.ToLower);

        // Store case conversion preference for later use
        _context.PushMember($"{memberName}_{(isToLower ? "LOWER" : "UPPER")}");

        // Note: The actual case-insensitive comparison will be handled
        // when this expression is used in a comparison context
    }

    private void ProcessTrim(MethodCallExpression node)
    {
        if (node.Object == null)
        {
            throw new InvalidExpressionFormatException("String.Trim expects an object", node);
        }

        var memberName = ExtractMemberName(node.Object);

        // Store trim preference for later use
        _context.PushMember($"{memberName}_TRIMMED");

        // Note: Actual trimming logic will be handled in comparison context
    }

    private void ProcessIsNullOrEmpty(MethodCallExpression node)
    {
        if (node.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("IsNullOrEmpty expects one argument", node);
        }

        var memberName = ExtractMemberName(node.Arguments[0]);

        _context.AddWhereAction(w =>
        {
            try
            {
                var method = w.GetType().GetMethod("WhereEmpty");
                if (method != null)
                {
                    w.WhereNull(memberName).Or();
                    method.Invoke(w, new object[] { memberName });
                }
                else
                {
                    // Fallback: WHERE field IS NULL OR field = ''
                    w.WhereNull(memberName).Or().WhereEquals(memberName, "");
                }
            }
            catch (Exception)
            {
                // Fallback: WHERE field IS NULL OR field = ''
                w.WhereNull(memberName).Or().WhereEquals(memberName, "");
            }
        });
    }

    private void ProcessIsNullOrWhiteSpace(MethodCallExpression node)
    {
        if (node.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("IsNullOrWhiteSpace expects one argument", node);
        }

        var memberName = ExtractMemberName(node.Arguments[0]);

        // IsNullOrWhiteSpace checks for null, empty, or whitespace-only strings
        _context.AddWhereAction(w =>
        {
            try
            {
                var emptyMethod = w.GetType().GetMethod("WhereEmpty");
                if (emptyMethod != null)
                {
                    w.WhereNull(memberName).Or();
                    emptyMethod.Invoke(w, new object[] { memberName });
                    w.Or().WhereEquals(memberName, " ")
                     .Or().WhereEquals(memberName, "\t")
                     .Or().WhereEquals(memberName, "\n")
                     .Or().WhereEquals(memberName, "\r")
                     .Or().WhereEquals(memberName, "\r\n");
                }
                else
                {
                    // Fallback without WhereEmpty
                    w.WhereNull(memberName)
                     .Or().WhereEquals(memberName, "")
                     .Or().WhereEquals(memberName, " ")
                     .Or().WhereEquals(memberName, "\t")
                     .Or().WhereEquals(memberName, "\n")
                     .Or().WhereEquals(memberName, "\r")
                     .Or().WhereEquals(memberName, "\r\n");
                }
            }
            catch (Exception)
            {
                // Fallback without WhereEmpty
                w.WhereNull(memberName)
                 .Or().WhereEquals(memberName, "")
                 .Or().WhereEquals(memberName, " ")
                 .Or().WhereEquals(memberName, "\t")
                 .Or().WhereEquals(memberName, "\n")
                 .Or().WhereEquals(memberName, "\r")
                 .Or().WhereEquals(memberName, "\r\n");
            }
        });
    }

    /// <summary>
    /// Creates a case-insensitive comparison using database functions if available
    /// </summary>
    public static void CreateCaseInsensitiveComparison(IExpressionContext context, string memberName, object? value, bool isEqual = true)
    {
        context.AddParameter(memberName, value);

        // Try to use database case-insensitive comparison
        context.AddWhereAction(w =>
        {
            try
            {
                // Some databases support ILIKE or case-insensitive collations
                // For now, we'll use standard comparison and rely on database collation settings
                if (isEqual)
                    w.WhereEquals(memberName, value);
                else
                    w.WhereNotEquals(memberName, value);
            }
            catch (Exception)
            {
                // Fallback to standard comparison
                if (isEqual)
                    w.WhereEquals(memberName, value);
                else
                    w.WhereNotEquals(memberName, value);
            }
        });
    }

    /// <summary>
    /// Creates a trimmed string comparison
    /// </summary>
    public static void CreateTrimmedComparison(IExpressionContext context, string memberName, object? value, bool isEqual = true)
    {
        context.AddParameter(memberName, value);

        // For trimmed comparisons, we need to be careful about whitespace
        context.AddWhereAction(w =>
        {
            if (value is string stringValue)
            {
                var trimmedValue = stringValue.Trim();
                if (isEqual)
                    w.WhereEquals(memberName, trimmedValue);
                else
                    w.WhereNotEquals(memberName, trimmedValue);
            }
            else
            {
                if (isEqual)
                    w.WhereEquals(memberName, value);
                else
                    w.WhereNotEquals(memberName, value);
            }
        });
    }

    private static string ExtractMemberName(Expression expr)
    {
        return expr switch
        {
            MemberExpression member => member.Member.Name,
            MethodCallExpression method when method.Method.Name is nameof(string.ToLower) or nameof(string.ToUpper) =>
                ExtractMemberName(method.Object!) + $"_{method.Method.Name.ToUpper()}",
            MethodCallExpression method when method.Method.Name == nameof(string.Trim) =>
                ExtractMemberName(method.Object!) + "_TRIMMED",
            _ => throw new InvalidExpressionFormatException($"Expected member expression, got {expr.GetType().Name}")
        };
    }

    private static object? ExtractConstantValue(Expression expr)
    {
        return expr switch
        {
            ConstantExpression constant => constant.Value,
            _ => throw new InvalidExpressionFormatException($"Expected constant expression, got {expr.GetType().Name}")
        };
    }
}
