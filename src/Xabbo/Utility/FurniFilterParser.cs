using System.Globalization;
using Xabbo.Core;
using Xabbo.ViewModels;

namespace Xabbo.Utility;

public static class FurniFilterParser
{
    private static readonly IReadOnlyList<PropertyDescriptor> SupportedProperties = [
        PropertyDescriptor.Number("id", vm => vm.Id),
        PropertyDescriptor.String("type", vm => vm.Type switch
        {
            ItemType.Floor => "floor",
            ItemType.Wall => "wall",
            _ => vm.Type.ToString()
        }),
        PropertyDescriptor.Number("kind", vm => vm.Kind),
        PropertyDescriptor.String("identifier", vm => vm.Identifier),
        PropertyDescriptor.String("variant", vm => vm.Variant),
        PropertyDescriptor.String("name", vm => vm.Name),
        PropertyDescriptor.String("description", vm => vm.Description),
        PropertyDescriptor.String("owner", vm => vm.Owner),
        PropertyDescriptor.Number("ownerId", vm => vm.OwnerId),
        PropertyDescriptor.Number("state", vm => vm.State),
        PropertyDescriptor.Boolean("hidden", vm => vm.Hidden),
        PropertyDescriptor.Boolean("isHidden", vm => vm.IsHidden),
        PropertyDescriptor.Boolean("isFloorItem", vm => vm.IsFloorItem),
        PropertyDescriptor.Boolean("isWallItem", vm => vm.IsWallItem),
        PropertyDescriptor.Number("x", vm => vm.X),
        PropertyDescriptor.Number("y", vm => vm.Y),
        PropertyDescriptor.Number("z", vm => vm.Z),
        PropertyDescriptor.Number("dir", vm => vm.Dir),
        PropertyDescriptor.Number("ltd", vm => vm.LTD),
        PropertyDescriptor.Number("wx", vm => vm.WX),
        PropertyDescriptor.Number("wy", vm => vm.WY),
        PropertyDescriptor.Number("lx", vm => vm.LX),
        PropertyDescriptor.Number("ly", vm => vm.LY),
        PropertyDescriptor.Boolean("isLeft", vm => vm.IsLeft),
        PropertyDescriptor.Boolean("isRight", vm => vm.IsRight),
        PropertyDescriptor.String("data", vm => vm.Data),
        PropertyDescriptor.Number("count", vm => vm.Count)
    ];

    private static readonly IReadOnlyDictionary<string, PropertyDescriptor> PropertyMap =
        SupportedProperties.ToDictionary(x => NormalizeIdentifier(x.Name), StringComparer.OrdinalIgnoreCase);

    public static string HelpText =>
        "Use where: with safe comparisons like kind == 123, owner contains \"bob\", hidden == false, x >= 4 && y <= 6.";

    public static Func<FurniViewModel, bool> Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        return new Parser(expression).Parse();
    }

    private static string NormalizeIdentifier(string identifier)
        => identifier.Replace("_", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Trim();

    private static string CreateUnknownPropertyMessage(string propertyName)
        => $"Unknown property '{propertyName}'. Supported properties: {string.Join(", ", SupportedProperties.Select(x => x.Name))}.";

    private static string CreateInvalidExpressionMessage(string message) => $"{message} {HelpText}";

    private static bool EvaluateBool(object? value) => value is bool b && b;

    private static string? EvaluateString(object? value) => value as string;

    private static double? EvaluateNumber(object? value) => value switch
    {
        null => null,
        byte x => x,
        sbyte x => x,
        short x => x,
        ushort x => x,
        int x => x,
        uint x => x,
        long x => x,
        ulong x => x,
        float x => x,
        double x => x,
        decimal x => (double)x,
        _ => null
    };

    private sealed class Parser(string expression)
    {
        private readonly Tokenizer _tokenizer = new(expression);
        private Token _current = default;

        public Func<FurniViewModel, bool> Parse()
        {
            _current = _tokenizer.Next();
            Func<FurniViewModel, bool> filter = ParseOr();
            Expect(TokenType.End);
            return filter;
        }

        private Func<FurniViewModel, bool> ParseOr()
        {
            Func<FurniViewModel, bool> left = ParseAnd();
            while (Match(TokenType.Or))
            {
                Func<FurniViewModel, bool> right = ParseAnd();
                Func<FurniViewModel, bool> currentLeft = left;
                left = vm => currentLeft(vm) || right(vm);
            }
            return left;
        }

        private Func<FurniViewModel, bool> ParseAnd()
        {
            Func<FurniViewModel, bool> left = ParseUnary();
            while (Match(TokenType.And))
            {
                Func<FurniViewModel, bool> right = ParseUnary();
                Func<FurniViewModel, bool> currentLeft = left;
                left = vm => currentLeft(vm) && right(vm);
            }
            return left;
        }

        private Func<FurniViewModel, bool> ParseUnary()
        {
            if (Match(TokenType.Not))
            {
                Func<FurniViewModel, bool> operand = ParseUnary();
                return vm => !operand(vm);
            }

            return ParsePrimary();
        }

        private Func<FurniViewModel, bool> ParsePrimary()
        {
            if (Match(TokenType.LeftParen))
            {
                Func<FurniViewModel, bool> expression = ParseOr();
                Expect(TokenType.RightParen);
                return expression;
            }

            return ParseCondition();
        }

        private Func<FurniViewModel, bool> ParseCondition()
        {
            Token propertyToken = Expect(TokenType.Identifier);
            string propertyName = propertyToken.Value ?? "";

            if (!PropertyMap.TryGetValue(NormalizeIdentifier(propertyName), out PropertyDescriptor? property))
                throw new FurniFilterParseException(CreateUnknownPropertyMessage(propertyName));

            if (TryParseComparisonOperator(out ComparisonOperator op))
            {
                FilterLiteral literal = ParseLiteral();
                ValidateComparison(property, op, literal);
                return vm => Compare(property, vm, op, literal);
            }

            if (property.Kind != PropertyKind.Boolean)
                throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Expected an operator after '{property.Name}'."));

            return vm => EvaluateBool(property.Getter(vm));
        }

        private bool TryParseComparisonOperator(out ComparisonOperator op)
        {
            switch (_current.Type)
            {
                case TokenType.Equal:
                    op = ComparisonOperator.Equal;
                    Advance();
                    return true;
                case TokenType.NotEqual:
                    op = ComparisonOperator.NotEqual;
                    Advance();
                    return true;
                case TokenType.Greater:
                    op = ComparisonOperator.Greater;
                    Advance();
                    return true;
                case TokenType.GreaterOrEqual:
                    op = ComparisonOperator.GreaterOrEqual;
                    Advance();
                    return true;
                case TokenType.Less:
                    op = ComparisonOperator.Less;
                    Advance();
                    return true;
                case TokenType.LessOrEqual:
                    op = ComparisonOperator.LessOrEqual;
                    Advance();
                    return true;
                case TokenType.Contains:
                    op = ComparisonOperator.Contains;
                    Advance();
                    return true;
                case TokenType.StartsWith:
                    op = ComparisonOperator.StartsWith;
                    Advance();
                    return true;
                case TokenType.EndsWith:
                    op = ComparisonOperator.EndsWith;
                    Advance();
                    return true;
                default:
                    op = default;
                    return false;
            }
        }

        private FilterLiteral ParseLiteral()
        {
            Token token = _current;
            Advance();

            return token.Type switch
            {
                TokenType.String => FilterLiteral.String(token.Value ?? ""),
                TokenType.Number => FilterLiteral.Number(double.Parse(token.Value ?? "0", CultureInfo.InvariantCulture)),
                TokenType.Boolean => FilterLiteral.Boolean(bool.Parse(token.Value ?? "false")),
                TokenType.Null => FilterLiteral.Null(),
                TokenType.Identifier => FilterLiteral.String(token.Value ?? ""),
                _ => throw new FurniFilterParseException(CreateInvalidExpressionMessage("Expected a value after the operator."))
            };
        }

        private void ValidateComparison(PropertyDescriptor property, ComparisonOperator op, FilterLiteral literal)
        {
            switch (property.Kind)
            {
                case PropertyKind.Number:
                    if (op is ComparisonOperator.Contains or ComparisonOperator.StartsWith or ComparisonOperator.EndsWith)
                        throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Operator '{ToDisplayString(op)}' is not supported for numeric property '{property.Name}'."));
                    if (!literal.IsNull && literal.Kind != LiteralKind.Number)
                        throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Property '{property.Name}' expects a numeric value."));
                    break;
                case PropertyKind.Boolean:
                    if (op is not ComparisonOperator.Equal and not ComparisonOperator.NotEqual)
                        throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Property '{property.Name}' only supports == and != comparisons."));
                    if (!literal.IsNull && literal.Kind != LiteralKind.Boolean)
                        throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Property '{property.Name}' expects true, false or null."));
                    break;
                case PropertyKind.String:
                    if (op is ComparisonOperator.Greater or ComparisonOperator.GreaterOrEqual or ComparisonOperator.Less or ComparisonOperator.LessOrEqual)
                        throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Operator '{ToDisplayString(op)}' is not supported for string property '{property.Name}'."));
                    if (!literal.IsNull && literal.Kind != LiteralKind.String)
                        throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Property '{property.Name}' expects a text value."));
                    break;
            }
        }

        private bool Compare(PropertyDescriptor property, FurniViewModel vm, ComparisonOperator op, FilterLiteral literal)
        {
            object? left = property.Getter(vm);

            return property.Kind switch
            {
                PropertyKind.Number => CompareNumbers(EvaluateNumber(left), op, literal),
                PropertyKind.Boolean => CompareBooleans(left, op, literal),
                PropertyKind.String => CompareStrings(EvaluateString(left), op, literal),
                _ => false
            };
        }

        private static bool CompareNumbers(double? left, ComparisonOperator op, FilterLiteral literal)
        {
            if (literal.IsNull)
            {
                return op switch
                {
                    ComparisonOperator.Equal => left is null,
                    ComparisonOperator.NotEqual => left is not null,
                    _ => false
                };
            }

            if (left is null || literal.NumberValue is null)
                return false;

            return op switch
            {
                ComparisonOperator.Equal => left.Value == literal.NumberValue.Value,
                ComparisonOperator.NotEqual => left.Value != literal.NumberValue.Value,
                ComparisonOperator.Greater => left.Value > literal.NumberValue.Value,
                ComparisonOperator.GreaterOrEqual => left.Value >= literal.NumberValue.Value,
                ComparisonOperator.Less => left.Value < literal.NumberValue.Value,
                ComparisonOperator.LessOrEqual => left.Value <= literal.NumberValue.Value,
                _ => false
            };
        }

        private static bool CompareBooleans(object? left, ComparisonOperator op, FilterLiteral literal)
        {
            bool? leftValue = left switch
            {
                bool value => value,
                _ => null
            };

            if (literal.IsNull)
            {
                return op switch
                {
                    ComparisonOperator.Equal => leftValue is null,
                    ComparisonOperator.NotEqual => leftValue is not null,
                    _ => false
                };
            }

            if (literal.BooleanValue is null)
                return false;

            return op switch
            {
                ComparisonOperator.Equal => leftValue == literal.BooleanValue.Value,
                ComparisonOperator.NotEqual => leftValue != literal.BooleanValue.Value,
                _ => false
            };
        }

        private static bool CompareStrings(string? left, ComparisonOperator op, FilterLiteral literal)
        {
            if (literal.IsNull)
            {
                return op switch
                {
                    ComparisonOperator.Equal => left is null,
                    ComparisonOperator.NotEqual => left is not null,
                    _ => false
                };
            }

            string right = literal.StringValue ?? "";
            if (left is null)
                return false;

            return op switch
            {
                ComparisonOperator.Equal => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.NotEqual => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.StartsWith => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.EndsWith => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private bool Match(TokenType type)
        {
            if (_current.Type != type)
                return false;

            Advance();
            return true;
        }

        private Token Expect(TokenType type)
        {
            if (_current.Type != type)
                throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Unexpected token '{_current.Value ?? _current.Type.ToString()}'."));

            Token token = _current;
            Advance();
            return token;
        }

        private void Advance() => _current = _tokenizer.Next();
    }

    private sealed class Tokenizer(string text)
    {
        private int _position;

        public Token Next()
        {
            SkipWhitespace();

            if (_position >= text.Length)
                return new Token(TokenType.End, null);

            char c = text[_position];

            if (c == '(')
            {
                _position++;
                return new Token(TokenType.LeftParen, "(");
            }
            if (c == ')')
            {
                _position++;
                return new Token(TokenType.RightParen, ")");
            }
            if (c == '!' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.NotEqual, "!=");
            }
            if (c == '!' )
            {
                _position++;
                return new Token(TokenType.Not, "!");
            }
            if (c == '>' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.GreaterOrEqual, ">=");
            }
            if (c == '<' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.LessOrEqual, "<=");
            }
            if (c == '>' )
            {
                _position++;
                return new Token(TokenType.Greater, ">");
            }
            if (c == '<' )
            {
                _position++;
                return new Token(TokenType.Less, "<");
            }
            if (c == '&' && Peek(1) == '&')
            {
                _position += 2;
                return new Token(TokenType.And, "&&");
            }
            if (c == '|' && Peek(1) == '|')
            {
                _position += 2;
                return new Token(TokenType.Or, "||");
            }
            if (c == '=' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.Equal, "==");
            }
            if (c == '=')
            {
                _position++;
                return new Token(TokenType.Equal, "=");
            }
            if (c == '~' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.Contains, "~=");
            }
            if (c == '^' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.StartsWith, "^=");
            }
            if (c == '$' && Peek(1) == '=')
            {
                _position += 2;
                return new Token(TokenType.EndsWith, "$=");
            }
            if (c is '"' or '\'')
                return ReadString(c);
            if (char.IsDigit(c) || (c == '-' && char.IsDigit(Peek(1))))
                return ReadNumber();
            if (char.IsLetter(c) || c == '_')
                return ReadIdentifier();

            throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Unexpected character '{c}'."));
        }

        private Token ReadString(char quote)
        {
            _position++;
            int start = _position;
            var value = new System.Text.StringBuilder();

            while (_position < text.Length)
            {
                char c = text[_position++];
                if (c == '\\' && _position < text.Length)
                {
                    char escaped = text[_position++];
                    value.Append(escaped switch
                    {
                        '\\' => '\\',
                        '\'' => '\'',
                        '"' => '"',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped
                    });
                    continue;
                }

                if (c == quote)
                    return new Token(TokenType.String, value.ToString());

                value.Append(c);
            }

            throw new FurniFilterParseException(CreateInvalidExpressionMessage($"Unterminated string starting at position {start}."));
        }

        private Token ReadNumber()
        {
            int start = _position;
            if (text[_position] == '-')
                _position++;

            while (_position < text.Length && char.IsDigit(text[_position]))
                _position++;

            if (_position < text.Length && text[_position] == '.')
            {
                _position++;
                while (_position < text.Length && char.IsDigit(text[_position]))
                    _position++;
            }

            return new Token(TokenType.Number, text[start.._position]);
        }

        private Token ReadIdentifier()
        {
            int start = _position;
            while (_position < text.Length && (char.IsLetterOrDigit(text[_position]) || text[_position] == '_' || text[_position] == '-'))
                _position++;

            string value = text[start.._position];
            return value.ToLowerInvariant() switch
            {
                "and" => new Token(TokenType.And, value),
                "or" => new Token(TokenType.Or, value),
                "not" => new Token(TokenType.Not, value),
                "contains" => new Token(TokenType.Contains, value),
                "startswith" => new Token(TokenType.StartsWith, value),
                "endswith" => new Token(TokenType.EndsWith, value),
                "true" => new Token(TokenType.Boolean, "true"),
                "false" => new Token(TokenType.Boolean, "false"),
                "null" => new Token(TokenType.Null, "null"),
                _ => new Token(TokenType.Identifier, value)
            };
        }

        private void SkipWhitespace()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position]))
                _position++;
        }

        private char Peek(int offset) => _position + offset < text.Length ? text[_position + offset] : '\0';
    }

    private sealed record PropertyDescriptor(string Name, PropertyKind Kind, Func<FurniViewModel, object?> Getter)
    {
        public static PropertyDescriptor String(string name, Func<FurniViewModel, object?> getter)
            => new(name, PropertyKind.String, getter);

        public static PropertyDescriptor Number(string name, Func<FurniViewModel, object?> getter)
            => new(name, PropertyKind.Number, getter);

        public static PropertyDescriptor Boolean(string name, Func<FurniViewModel, object?> getter)
            => new(name, PropertyKind.Boolean, getter);
    }

    private readonly record struct FilterLiteral(LiteralKind Kind, object? Value)
    {
        public bool IsNull => Kind == LiteralKind.Null;
        public string? StringValue => Value as string;
        public double? NumberValue => Value is double number ? number : null;
        public bool? BooleanValue => Value is bool boolean ? boolean : null;

        public static FilterLiteral String(string value) => new(LiteralKind.String, value);
        public static FilterLiteral Number(double value) => new(LiteralKind.Number, value);
        public static FilterLiteral Boolean(bool value) => new(LiteralKind.Boolean, value);
        public static FilterLiteral Null() => new(LiteralKind.Null, null);
    }

    public sealed class FurniFilterParseException(string message) : Exception(message);

    private readonly record struct Token(TokenType Type, string? Value);

    private enum PropertyKind
    {
        String,
        Number,
        Boolean
    }

    private enum LiteralKind
    {
        String,
        Number,
        Boolean,
        Null
    }

    private enum ComparisonOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Contains,
        StartsWith,
        EndsWith
    }

    private enum TokenType
    {
        Identifier,
        String,
        Number,
        Boolean,
        Null,
        LeftParen,
        RightParen,
        And,
        Or,
        Not,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        End
    }

    private static string ToDisplayString(ComparisonOperator op) => op switch
    {
        ComparisonOperator.Equal => "==",
        ComparisonOperator.NotEqual => "!=",
        ComparisonOperator.Greater => ">",
        ComparisonOperator.GreaterOrEqual => ">=",
        ComparisonOperator.Less => "<",
        ComparisonOperator.LessOrEqual => "<=",
        ComparisonOperator.Contains => "contains",
        ComparisonOperator.StartsWith => "startsWith",
        ComparisonOperator.EndsWith => "endsWith",
        _ => op.ToString()
    };
}
