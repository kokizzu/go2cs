[assembly: go.GoPositionMap("lib.go", "lib.cs", "")]

namespace go.ForeignPointerImplementSuppression;

partial class shade_package {

[GoType] partial interface Level {
    nint Tone();
    @string Name();
    void Set(nint d);
}

} // end shade_package
