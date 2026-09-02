global using eface = object;
global using namedIface = go.fmt_package.Stringer;
global using aliasIface = go.fmt_package.Stringer;

namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {
// Descriptor carrier for `eface` — uninhabited; see GoDescriptorTypeAttribute.
[GoLocalName("eface")] internal interface efaceᴅ { }

// Descriptor carrier for `namedIface` — uninhabited; see GoDescriptorTypeAttribute.
[GoLocalName("namedIface")] internal interface namedIfaceᴅ { }


[GoType] public partial interface realIface {
    void Do();
}

[GoType] partial struct holder {
    [GoDescriptorType(Self = typeof(efaceᴅ))]
    public eface E;
    [GoDescriptorType(Self = typeof(namedIfaceᴅ))]
    public namedIface N;
    public realIface R;
    public aliasIface A;
}

internal static void Main() {
    var t = reflect.TypeFor<holder>();
    for (nint i = 0; i < t.NumField(); i++) {
        var f = t.Field(i);
        fmt.Printf("%s Name=%q String=%q PkgPath=%q Kind=%v\n"u8,
            f.Name, f.Type.Name(), f.Type.String(), f.Type.PkgPath(), f.Type.Kind());
    }
}

} // end main_package
