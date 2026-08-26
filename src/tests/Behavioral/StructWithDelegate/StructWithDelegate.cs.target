namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct Person {
    internal Action work;
    internal @string name;
    internal int32 age;
}

internal static void Main() {
    var person = new Person(work: default!, name: "Michał"u8, age: 29);
    fmt.Println(person);
}

} // end main_package
