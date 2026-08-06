namespace go.SamePackageImplementNoWitness;

partial class ledger_package {

[GoType] partial interface Metric {
    nint Value();
}

[GoType] partial struct Counter {
    public nint N;
}

public static nint Value(this Counter c) {
    return c.N;
}

[GoType("num:nint")] partial struct Gauge;

public static nint Value(this Gauge g) {
    return (nint)g * 2;
}

public delegate nint Meter();

public static nint Value(this Meter m) {
    return m();
}

[GoType] partial struct Tally {
    public nint N;
}

[GoRecv] public static nint Value(this ref Tally t) {
    return t.N;
}

[GoType] partial struct Box<T> {
    public T V;
}

public static nint Value<T>(this Box<T> b) {
    return 3;
}

[GoType] partial interface probe {
    nint depth();
}

[GoType] partial struct well {
    public nint D;
}

internal static nint depth(this well w) {
    return w.D;
}

public static nint Depth(nint d) {
    return new well(D: d).depth();
}

} // end ledger_package
