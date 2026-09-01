// TestFormat.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace go.testing_runtime;

/// <summary>
/// Diagnostic-grade Go-style formatting for the bootstrap testing shim.
/// </summary>
/// <remarks>
/// The testing runtime must stay fmt-free. It is a FIXED reference of every converted test project,
/// so a core/fmt reference here would put fmt underneath every suite — including fmt's own, where
/// the host would then be reporting on a package it is itself running on, and any other suite that
/// hand-owns or stubs part of the fmt closure would drag a second copy into one build.
/// The host therefore carries this small self-contained formatter instead. Its output feeds
/// t.Log/t.Error diagnostics only — the differential oracle never byte-compares log text
/// (TestingInfrastructureRequirements §7) — so coverage of the common verbs is sufficient, and the
/// converted Go testing package replaces the whole shim at Phase 4D.
/// </remarks>
internal static class TestFormat
{
    /// <summary>
    /// Formats arguments like Go's fmt.Sprintln without the trailing newline — t.Log documents
    /// Println-style default formatting: spaces between all operands.
    /// </summary>
    public static string Sprint(ReadOnlySpan<object> args)
    {
        StringBuilder result = new();

        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0)
                result.Append(' ');
            result.Append(Default(args[i]));
        }

        return result.ToString();
    }

    /// <summary>
    /// Formats a Go format string with common-verb coverage: %v %s %q %d %x %X %o %b %c %t %e %E
    /// %f %F %g %G %T %%. Width/precision are honored for floats, and `#` selects the alternate
    /// form of the integer bases (0x/0X/0b/0); the remaining flags are parsed and ignored.
    /// Unknown verbs, missing arguments, and extra arguments render in Go's disclosure style
    /// (%!x(...), %!v(MISSING), %!(EXTRA ...)) so a formatting gap is visible, never silent.
    /// </summary>
    public static string Sprintf(string format, ReadOnlySpan<object> args)
    {
        StringBuilder result = new(format.Length + 16);
        int argIndex = 0;

        for (int i = 0; i < format.Length; i++)
        {
            char ch = format[i];

            if (ch != '%')
            {
                result.Append(ch);
                continue;
            }

            if (i + 1 >= format.Length)
            {
                result.Append("%!(NOVERB)");
                break;
            }

            // %[flags][width][.precision]verb
            i++;

            // Go's `#` selects the ALTERNATE form, and for the integer bases it is the difference
            // between `deadbeef` and `0xdeadbeef` — the form test messages actually use. The other
            // flags remain parsed-and-ignored (width/padding is a larger surface than a diagnostic
            // shim needs).
            bool alternate = false;

            while (i < format.Length && format[i] is '+' or '-' or '#' or ' ' or '0')
            {
                if (format[i] == '#')
                    alternate = true;

                i++;
            }

            while (i < format.Length && char.IsAsciiDigit(format[i]))
                i++;

            int precision = -1;

            if (i < format.Length && format[i] == '.')
            {
                int digitsStart = ++i;
                while (i < format.Length && char.IsAsciiDigit(format[i]))
                    i++;
                precision = i > digitsStart ? int.Parse(format[digitsStart..i], CultureInfo.InvariantCulture) : 0;
            }

            if (i >= format.Length)
            {
                result.Append("%!(NOVERB)");
                break;
            }

            char verb = format[i];

            if (verb == '%')
            {
                result.Append('%');
                continue;
            }

            if (argIndex >= args.Length)
            {
                result.Append($"%!{verb}(MISSING)");
                continue;
            }

            result.Append(Format(verb, precision, alternate, args[argIndex++]));
        }

        if (argIndex < args.Length)
        {
            result.Append("%!(EXTRA ");

            for (int i = argIndex; i < args.Length; i++)
            {
                if (i > argIndex)
                    result.Append(", ");
                result.Append(Default(args[i]));
            }

            result.Append(')');
        }

        return result.ToString();
    }

    private static string Format(char verb, int precision, bool alternate, object? arg)
    {
        switch (verb)
        {
            case 'v':
            case 'd':
            case 'w':
                return Default(arg);
            case 's':
                return TryGetByteText(arg, out string sText) ? sText : Default(arg);
            case 't':
                return arg is bool boolValue ? boolValue ? "true" : "false" : BadVerb(verb, arg);
            case 'q':
                return Quote(TryGetByteText(arg, out string qText) ? qText : Default(arg));
            case 'c':
                return TryGetInt64(arg, out long rune) ? char.ConvertFromUtf32(checked((int)rune)) : BadVerb(verb, arg);
            case 'x':
            case 'X':
            case 'o':
            case 'b':
                return FormatBase(verb, alternate, arg);
            case 'e':
            case 'E':
            case 'f':
            case 'F':
            case 'g':
            case 'G':
                return FormatFloat(verb, precision, arg);
            case 'T':
                return TrimGoPrefix(builtin.GetGoTypeName(arg));
            case 'p':
                return arg is null ? "<nil>" : $"0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(arg):x}";
            default:
                return BadVerb(verb, arg);
        }
    }

    private static string BadVerb(char verb, object? arg) =>
        $"%!{verb}({TrimGoPrefix(builtin.GetGoTypeName(arg))}={Default(arg)})";

    /// <summary>
    /// Reports the TEXT of a <c>[]byte</c> argument — the rendering Go's string verbs give it.
    /// </summary>
    /// <remarks>
    /// Under <c>%s</c>, <c>%q</c> and <c>%x</c> Go formats a byte slice as the bytes THEMSELVES, and
    /// only <c>%v</c>/<c>%d</c> as the list of numbers: <c>%q</c> of an empty <c>[]byte</c> is
    /// <c>""</c>, not <c>"[]"</c>. Quoting the default rendering instead is what made os's
    /// TestExecutable report a child that produced nothing as <c>Child returned "[]"</c> — a
    /// diagnostic that names the formatter's gap rather than the failure. (core/fmt has always been
    /// right here; this shim is the host's own fmt-free formatter, and only it was wrong.)
    /// </remarks>
    private static bool TryGetByteText(object? arg, out string text)
    {
        if (TryGetBytes(arg, out byte[] bytes))
        {
            text = Encoding.UTF8.GetString(bytes);
            return true;
        }

        text = "";
        return false;
    }

    private static bool TryGetBytes(object? arg, out byte[] bytes)
    {
        if (arg is slice<byte> slice)
        {
            bytes = slice.ꓸꓸꓸ.ToArray();
            return true;
        }

        bytes = [];
        return false;
    }

    /// <summary>
    /// Renders the integer bases -- <c>%x</c>, <c>%X</c>, <c>%o</c>, <c>%b</c> -- the way Go does.
    /// </summary>
    /// <remarks>
    /// Go formats an integer in these bases by MAGNITUDE with a leading minus, never as a two's
    /// complement word: <c>%x</c> of <c>-31</c> is <c>-1f</c>, not <c>ffffffffffffffe1</c>. Going
    /// through an unsigned magnitude also carries the top of the <c>uint64</c> range, which a signed
    /// coercion could not represent at all and so disclosed as a bad verb.
    /// <para>
    /// <c>%x</c>/<c>%X</c> additionally accept a byte slice or a string and render its BYTES. The
    /// other two bases take neither, which matches Go for a STRING -- Go bad-verbs <c>%o</c> on one
    /// itself -- but NOT for a byte slice, where Go renders element-wise (<c>[336 255]</c>) and this
    /// shim discloses a bad verb instead. Named as a known gap rather than implied to be parity:
    /// element-wise rendering applies to every slice, not just bytes, so it belongs with the shim's
    /// other unimplemented breadth rather than being smuggled in behind the integer bases.
    /// </para>
    /// </remarks>
    private static string FormatBase(char verb, bool alternate, object? arg)
    {
        int radix = verb switch { 'o' => 8, 'b' => 2, _ => 16 };
        string digits;

        if (TryGetIntegral(arg, out ulong magnitude, out bool negative))
            digits = Digits(magnitude, radix);
        else if (radix != 16)
            return BadVerb(verb, arg);
        else if (TryGetBytes(arg, out byte[] bytes))
            digits = Convert.ToHexStringLower(bytes); // the RAW bytes, which need not be valid UTF-8
        else if (arg is @string or string)
            digits = Convert.ToHexStringLower(Encoding.UTF8.GetBytes(Default(arg)));
        else
            return BadVerb(verb, arg);

        if (verb == 'X')
            digits = digits.ToUpperInvariant();

        if (alternate)
            digits = verb switch { 'x' => "0x", 'X' => "0X", 'b' => "0b", _ => "0" } + digits;

        // The sign sits OUTSIDE the alternate prefix, as Go writes it: `-0x1f`.
        return negative ? "-" + digits : digits;
    }

    /// <summary>
    /// Coerces any Go integer argument to a sign-and-magnitude pair.
    /// </summary>
    /// <remarks>
    /// Every Go integer kind but one arrives here as a CLR primitive. <c>uintptr</c> is golib's own
    /// STRUCT -- it wraps an <c>nuint</c> so that inference and boxing keep reporting the Go type --
    /// so a switch over primitives alone misses it silently, which is the whole of the defect this
    /// helper was extracted to fix: reflect's <c>TestValuePointerAndUnsafePointer</c> prints both
    /// sides of its comparison with <c>%#x</c>, and the shim answered <c>%!x(uintptr=...)</c> for
    /// each of them, hiding the very values the assert had failed on.
    /// </remarks>
    private static bool TryGetIntegral(object? arg, out ulong magnitude, out bool negative)
    {
        negative = false;

        switch (arg)
        {
            case sbyte or short or int or long:
                return Magnitude(Convert.ToInt64(arg, CultureInfo.InvariantCulture), out magnitude, out negative);
            case nint nativeSigned:
                return Magnitude(nativeSigned, out magnitude, out negative);
            case byte or ushort or uint or ulong:
                magnitude = Convert.ToUInt64(arg, CultureInfo.InvariantCulture);
                return true;
            case nuint nativeUnsigned:
                magnitude = nativeUnsigned;
                return true;
            case uintptr pointer:
                magnitude = pointer.Value;
                return true;
            default:
                magnitude = 0UL;
                return false;
        }
    }

    // Negating long.MinValue overflows the signed domain, so the magnitude is taken in the unsigned
    // one instead -- ~v + 1 is the same two's-complement negation without the trap.
    private static bool Magnitude(long value, out ulong magnitude, out bool negative)
    {
        negative = value < 0L;
        magnitude = negative ? unchecked((ulong)~value) + 1UL : (ulong)value;
        return true;
    }

    /// <summary>
    /// The unsigned magnitude in the given radix, lower-case and without leading zeros.
    /// </summary>
    private static string Digits(ulong magnitude, int radix)
    {
        if (magnitude == 0UL)
            return "0";

        const string Alphabet = "0123456789abcdef";

        // Base 2 is the widest of the three, so 64 digits covers every radix and every value.
        Span<char> buffer = stackalloc char[64];
        int index = buffer.Length;
        ulong divisor = (ulong)radix;

        while (magnitude != 0UL)
        {
            buffer[--index] = Alphabet[(int)(magnitude % divisor)];
            magnitude /= divisor;
        }

        return new string(buffer[index..]);
    }

    private static string FormatFloat(char verb, int precision, object? arg)
    {
        double number;

        if (arg is double doubleValue)
            number = doubleValue;
        else if (arg is float floatValue)
            number = floatValue;
        else if (TryGetInt64(arg, out long integral))
            number = integral;
        else
            return BadVerb(verb, arg);

        switch (char.ToLowerInvariant(verb))
        {
            case 'f':
                return number.ToString("F" + (precision >= 0 ? precision : 6), CultureInfo.InvariantCulture);
            case 'e':
            {
                string text = number.ToString((precision >= 0 ? "0." + new string('0', precision) : "0.000000") + "e+00", CultureInfo.InvariantCulture);
                return verb == 'E' ? text.ToUpperInvariant() : text;
            }
            default: // 'g' / 'G'
                return precision >= 0
                    ? number.ToString("G" + precision, CultureInfo.InvariantCulture)
                    : number.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static string Quote(string value)
    {
        StringBuilder result = new(value.Length + 2);
        result.Append('"');

        foreach (char ch in value)
        {
            switch (ch)
            {
                case '"':
                    result.Append("\\\"");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                default:
                    if (char.IsControl(ch))
                        result.Append($"\\u{(int)ch:x4}");
                    else
                        result.Append(ch);
                    break;
            }
        }

        result.Append('"');
        return result.ToString();
    }

    /// <summary>
    /// Coerces an integer argument to a signed <see cref="long"/>, for the verbs that need a scalar
    /// rather than a magnitude (<c>%c</c>'s rune, <c>%e</c>/<c>%f</c>/<c>%g</c>'s integral operand).
    /// </summary>
    /// <remarks>
    /// It defers to <see cref="TryGetIntegral"/> rather than switching over primitives itself,
    /// because the obvious spelling of that switch is a trap: <c>IntPtr</c>/<c>UIntPtr</c> do NOT
    /// implement <see cref="IConvertible"/>, so a case label listing <c>nint</c> and then calling
    /// <see cref="Convert.ToInt64(object, IFormatProvider)"/> compiles and then THROWS at run time --
    /// and since Go's <c>int</c> converts to <c>nint</c>, that turned a <c>%x</c> on the commonest
    /// integer kind in the corpus into a cast exception that failed the whole test as an
    /// infrastructure error rather than mis-rendering one operand.
    /// A value outside the signed range is still refused, as before.
    /// </remarks>
    private static bool TryGetInt64(object? arg, out long value)
    {
        if (TryGetIntegral(arg, out ulong magnitude, out bool negative))
        {
            // |long.MinValue| is one past long.MaxValue, so the negative bound is the wider one.
            if (negative && magnitude <= (ulong)long.MaxValue + 1UL)
            {
                value = unchecked(-(long)magnitude);
                return true;
            }

            if (!negative && magnitude <= (ulong)long.MaxValue)
            {
                value = (long)magnitude;
                return true;
            }
        }

        value = 0L;
        return false;
    }

    private static string TrimGoPrefix(string goTypeName) =>
        goTypeName.StartsWith("go.", StringComparison.Ordinal) ? goTypeName[3..] : goTypeName;

    internal static string Default(object? arg)
    {
        switch (arg)
        {
            case null:
                return "<nil>";
            case @string goString:
                return goString.ToString();
            case string text:
                return text;
            case bool boolValue:
                return boolValue ? "true" : "false";
            case error err:
                return err.Error();
        }

        // Duck-typed fmt.Stringer: the Stringer interface itself is declared in the fmt package,
        // which this shim deliberately does not reference (mixed-tree ruling in the class remarks).
        MethodInfo? stringMethod = arg.GetType().GetMethod("String", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);

        if (stringMethod is not null && stringMethod.ReturnType == typeof(@string))
            return ((@string)stringMethod.Invoke(arg, null)!).ToString();

        if (arg is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return arg.ToString() ?? "<nil>";
    }
}
