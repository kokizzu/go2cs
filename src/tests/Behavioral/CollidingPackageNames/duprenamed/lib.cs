namespace go.collidea;

partial class dup_package {

[GoType] partial struct Widget {
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string widgetMarkerˢ = "widget-marker"u8;

public static @string Marker(this Widget _) {
    return widgetMarkerˢ;
}

[GoType] partial struct ΔMarker {
    public @string Value;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string helloFromDuprenamedˢ = "hello-from-duprenamed"u8;

public static @string Greeting() {
    return helloFromDuprenamedˢ;
}

} // end dup_package
