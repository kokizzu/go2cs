namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType("bool")] partial struct main_MyBool;

public static void ShowValue(fmt.Stringer val) {
    fmt.Println(val.String());
}

internal static @string String(this main_MyBool b) {
    if (b) {
        return "true-ish"u8;
    }
    return "false-ish"u8;
}

[GoType("bool")] partial struct main_MyBoolᴛ1;

internal static void Main() {
    const bool c = /* 3 < 4 */ true;
    nint x = default!;
    nint y = default!;
    bool b3 = x == y;
    bool b4 = x == y;
    main_MyBoolᴛ1 b5 = x == y;
    fmt.Println(c);
    fmt.Println(b3);
    fmt.Println(b4);
    fmt.Println(b5);
    main_MyBool other = default!;
    ShowValue(other);
}

} // end main_package
