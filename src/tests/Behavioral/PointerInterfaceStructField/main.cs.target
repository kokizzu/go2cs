namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial interface Describer {
    @string Describe();
}

[GoType] partial struct Setting {
    internal @string name;
    internal nint value;
}

[GoRecv] public static @string Describe(this ref Setting s) {
    return fmt.Sprintf("%s=%d"u8, s.name, s.value);
}

[GoType] partial struct holder {
    internal Describer d;
    internal @string label;
}

internal static void assignDescriber(ref holder h, ж<Setting> Ꮡs) {
    h.d = new SettingжDescriber(Ꮡs);
}

internal static ж<Setting> ᏑglobalSetting = new(new Setting(name: "verbosity"u8, value: 3));
internal static ref Setting globalSetting => ref ᏑglobalSetting.Value;

internal static void Main() {
    var h = Ꮡ(new holder(new SettingжDescriber(ᏑglobalSetting), "positional"u8));
    fmt.Println((~h).label, (~h).d.Describe());
    ref var local = ref heap<Setting>(out var Ꮡlocal);
    local = new Setting(name: "count"u8, value: 7);
    var h2 = new holder(d: new SettingжDescriber(Ꮡlocal), label: "keyed"u8);
    fmt.Println(h2.label, h2.d.Describe());
    ref var replacement = ref heap<Setting>(out var Ꮡreplacement);
    replacement = new Setting(name: "assigned"u8, value: 11);
    assignDescriber(ref (h).DerefOrNull(), Ꮡreplacement);
    replacement.value = 12;
    fmt.Println((~h).label, (~h).d.Describe());
}

} // end main_package
