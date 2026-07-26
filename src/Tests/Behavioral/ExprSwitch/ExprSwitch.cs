namespace go;

using fmt = fmt_package;
using time = time_package;
using ꓸꓸꓸany = Span<any>;

partial class main_package {

internal static nint x = 1;

internal static int32 getNext() {
    x++;
    return (int32)x;
}

internal static @string getStr(@string test) {
    return "string"u8 + test;
}

internal static @string getStr2(any test1, @string test2) {
    return test1._<@string>() + test2;
}

internal static @string getStr3(@string format, params ꓸꓸꓸany aʗp) {
    var a = aʗp.slice();

    return fmt.Sprintf(format, a.ꓸꓸꓸ);
}

public static nint Foo(nint n) {
    fmt.Println(n);
    return n;
}

internal static void Main() {
    fmt.Println(getStr("test"u8));
    fmt.Println(getStr2((@string)"hello, "u8, "world"u8));
    fmt.Println(getStr3("hello, %s"u8, (@string)"world"u8));
    nint i = 2;
    fmt.Print((@string)"Write "u8, i, (@string)" as "u8);
    switch (i) {
    case 1: {
        fmt.Println((@string)"one"u8);
        break;
    }
    case 2: {
        fmt.Println((@string)"two"u8);
        break;
    }
    case 3: {
        {
            fmt.Println((@string)"three"u8);
        }
        break;
    }
    case 4 or 5 or 6: {
        fmt.Println((@string)"four, five or siz"u8);
        break;
    }
    default: {
        fmt.Println((@string)"unknown"u8);
        break;
    }}

    nint x = 5;
    fmt.Println(x);
    {
        nint xΔ1 = 6;
        fmt.Println(xΔ1);
    }
    fmt.Println(x);
    var exprᴛ1 = time.Now().Weekday();
    if (exprᴛ1 == time.Saturday || exprᴛ1 == time.Sunday) {
        fmt.Println((@string)"It's the weekend"u8);
    }
    else if (exprᴛ1 == time.Monday) {
        fmt.Println((@string)"Ugh, it's Monday"u8);
    }
    else { /* default: */
        fmt.Println((@string)"It's a weekday"u8);
    }

    var t = time.Now();
    switch (ᐧ) {
    case {} when t.Hour() is < 12: {
        fmt.Println((@string)"It's before noon"u8);
        break;
    }
    default: {
        fmt.Println((@string)"It's after noon"u8);
        break;
    }}

    nint hour = 1;
    nint hour1 = time.Now().Hour();
    {
        nint hourΔ1 = time.Now().Hour();
        switch (ᐧ) {
        case {} when hourΔ1 is 1 or < 12 or 2: {
            fmt.Println((@string)"Good morning!"u8);
            break;
        }
        case {} when (hourΔ1 == 1) || (hourΔ1 < 12) || (hourΔ1 == 2 || hour1 == 4): {
            fmt.Println((@string)"Good morning (opt 2)!"u8);
            break;
        }
        case {} when hourΔ1 is < 17: {
            fmt.Println((@string)"Good afternoon!"u8);
            break;
        }
        case {} when hourΔ1 is 0: {
            fmt.Println((@string)"Midnight!"u8);
            break;
        }
        case {} when hourΔ1 == 0 && hour1 == 1: {
            fmt.Println((@string)"Midnight (opt 2)!"u8);
            break;
        }
        default: {
            fmt.Println((@string)"Good evening!"u8);
            break;
        }}
    }

    fmt.Println(hour);
    var c = (rune)'\r';
    switch (c) {
    case (rune)' ' or (rune)'\t' or (rune)'\n' or (rune)'\f' or (rune)'\r': {
        fmt.Println((@string)"whitespace"u8);
        break;
    }}

    fmt.Printf("i before = %d\n"u8, i);
    {
        nint iΔ1 = 1;
        var exprᴛ2 = getNext();
        var matchᴛ1 = false;
        if (exprᴛ2 == -1) { matchᴛ1 = true;
            fmt.Println((@string)"negative"u8);
        }
        else if (exprᴛ2 is 0) { matchᴛ1 = true;
            fmt.Println((@string)"zero"u8);
        }
        else if (exprᴛ2 is 1 or 2) { matchᴛ1 = true;
            fmt.Println((@string)"one or two"u8);
            fallthrough = true;
        }
        if (fallthrough || !matchᴛ1 && exprᴛ2 is 3) { matchᴛ1 = true;
            fmt.Printf("three, but x=%d "u8, x);
            fmt.Printf("and i now = %d\n"u8, iΔ1);
            fallthrough = true;
        }
        if (fallthrough || !matchᴛ1) { /* default: */
            fmt.Println((@string)"plus, always a default here because of fallthrough"u8);
        }
    }

    fmt.Printf("i after = %d\n"u8, i);
    {
        var next = getNext();
        var matchᴛ2 = false;
        if (next <= -1) { matchᴛ2 = true;
            fmt.Println((@string)"negative"u8);
            var exprᴛ4 = getNext();
            var matchᴛ3 = false;
            if (exprᴛ4 is 1 or 2) { matchᴛ3 = true;
                fmt.Println((@string)"sub0 one or two"u8);
            }
            else if (exprᴛ4 is 3) { matchᴛ3 = true;
                fmt.Println((@string)"sub0 three"u8);
                fallthrough = true;
            }
            if (fallthrough || !matchᴛ3) { /* default: */
                fmt.Println((@string)"sub0 default"u8);
            }

        }
        else if (next is 0) { matchᴛ2 = true;
            fmt.Println((@string)"zero"u8);
            {
                var nextΔ2 = getNext();
                var matchᴛ4 = false;
                if (nextΔ2 is 1 or <= 2) { matchᴛ4 = true;
                    fmt.Println((@string)"sub1 one or two"u8);
                }
                else if (nextΔ2 is 3) { matchᴛ4 = true;
                    fmt.Println((@string)"sub1 three"u8);
                    fallthrough = true;
                }
                if (fallthrough || !matchᴛ4) { /* default: */
                    fmt.Println((@string)"sub1 default"u8);
                }
            }

        }
        else if (next is 1 or 2) { matchᴛ2 = true;
            fmt.Println((@string)"one or two"u8);
            switch (next) {
            case 1 or 2: {
                fmt.Println((@string)"sub2 one or two"u8);
                break;
            }
            case 3: {
                fmt.Println((@string)"sub2 three"u8);
                break;
            }
            default: {
                fmt.Println((@string)"sub2 default"u8);
                break;
            }}

            fallthrough = true;
        }
        if (fallthrough || !matchᴛ2 && (next >= 3 && next < 100)) { matchᴛ2 = true;
            fmt.Printf("three or greater < 100: %d\n"u8, x);
            fallthrough = true;
        }
        if (fallthrough || !matchᴛ2) { /* default: */
            fmt.Println((@string)"plus, always a default here because of fallthrough"u8);
        }
    }

    var exprᴛ6 = Foo(2);
    var matchᴛ5 = false;
    if (exprᴛ6 == Foo(1) || exprᴛ6 == Foo(2) || exprᴛ6 == Foo(3)) { matchᴛ5 = true;
        fmt.Println((@string)"First case"u8);
        fallthrough = true;
    }
    if (fallthrough || !matchᴛ5 && exprᴛ6 == Foo(4)) {
        fmt.Println((@string)"Second case"u8);
    }
    else if (!matchᴛ5) { /* default: */
        fmt.Println((@string)"Default case"u8);
    }

    nint v = 3;
    switch (ᐧ) {
    case {} when ᐧᐧ: {
        fmt.Println((@string)"strict checks disabled"u8);
        break;
    }
    case {} when v is > 2: {
        fmt.Println((@string)"unreachable but compiled"u8);
        break;
    }}

    pace dur = 5;
    switch (ᐧ) {
    case {} when dur >= 6: {
        fmt.Println((@string)"fast"u8);
        break;
    }
    case {} when dur == 5: {
        fmt.Println((@string)"steady"u8);
        break;
    }
    default: {
        fmt.Println((@string)"slow"u8);
        break;
    }}

}

[GoType("num:int64")] partial struct pace;

} // end main_package
