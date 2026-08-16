namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {

[GoType("[]byte")] partial struct myBytes;

[GoType("[]nint")] partial struct myInts;

[GoType("map[@string, nint]")] partial struct myMap;

[GoType("map[@string, int64]")] partial struct myWideMap;

[GoType("map[nint, nint]")] partial struct myKeyMap;

[GoType("ж<nint>")] partial class myPtr;

[GoType("ж<int64>")] partial class myWidePtr;

[GoType("chan nint")] partial struct myChan;

[GoType("[3]byte")] partial struct myArray;

[GoType("[4]byte")] partial struct myWideArray;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object sliceˢ = (@string)"slice"u8;
private static readonly object mapˢ = (@string)"map"u8;
private static readonly object ptrˢ = (@string)"ptr"u8;
private static readonly object arrayˢ = (@string)"array"u8;
private static readonly object elemˢ = (@string)"elem"u8;

internal static void Main() {
    var bytes = reflect.TypeOf(slice<byte>(default!));
    var named = reflect.TypeOf(((myBytes)default!));
    var ints = reflect.TypeOf(slice<nint>(default!));
    var namedInts = reflect.TypeOf(((myInts)default!));
    fmt.Println(sliceˢ, bytes.ConvertibleTo(named), named.ConvertibleTo(bytes));
    fmt.Println(sliceˢ, named.ConvertibleTo(named), bytes.ConvertibleTo(bytes));
    fmt.Println(sliceˢ, bytes.ConvertibleTo(namedInts), ints.ConvertibleTo(namedInts));
    fmt.Println(sliceˢ, named.ConvertibleTo(ints), ints.ConvertibleTo(bytes));
    map<@string, nint> unnamedMap = default!;
    var plain = reflect.TypeOf(unnamedMap);
    var namedMap = reflect.TypeOf(((myMap)default!));
    var wideMap = reflect.TypeOf(((myWideMap)default!));
    var keyMap = reflect.TypeOf(((myKeyMap)default!));
    fmt.Println(mapˢ, plain.ConvertibleTo(namedMap), namedMap.ConvertibleTo(plain));
    fmt.Println(mapˢ, plain.ConvertibleTo(wideMap), plain.ConvertibleTo(keyMap));
    fmt.Println(mapˢ, wideMap.ConvertibleTo(keyMap), namedMap.ConvertibleTo(namedMap));
    var ptr = reflect.TypeOf(((ж<nint>)nil));
    var namedPtr = reflect.TypeOf(((myPtr)nil));
    var widePtr = reflect.TypeOf(((myWidePtr)nil));
    fmt.Println(ptrˢ, ptr.ConvertibleTo(namedPtr), namedPtr.ConvertibleTo(ptr));
    fmt.Println(ptrˢ, ptr.ConvertibleTo(widePtr), widePtr.ConvertibleTo(namedPtr));
    var arr = reflect.TypeOf(new byte[]{}.array(3));
    var namedArr = reflect.TypeOf(new myArray(new byte[3].array()));
    var wideArr = reflect.TypeOf(new myWideArray(new byte[4].array()));
    fmt.Println(arrayˢ, arr.ConvertibleTo(namedArr), namedArr.ConvertibleTo(arr));
    fmt.Println(arrayˢ, arr.ConvertibleTo(wideArr), wideArr.ConvertibleTo(namedArr));
    fmt.Println(arrayˢ, arr.Len(), namedArr.Len(), wideArr.Len());
    var myChanType = reflect.TypeOf(((myChan)default!));
    fmt.Println(elemˢ, named.Elem().Kind(), namedMap.Key().Kind(), namedMap.Elem().Kind());
    fmt.Println(elemˢ, namedPtr.Elem().Kind(), myChanType.Elem().Kind(), namedArr.Elem().Kind());
}

} // end main_package
