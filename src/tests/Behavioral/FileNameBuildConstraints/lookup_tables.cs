[assembly: go.GoPositionMap("lookup_tables.go", "lookup_tables.cs", "")]

namespace go;

partial class main_package {

internal static array<nint> lookupTable = new nint[]{1, 4, 9, 16, 25}.array();

internal static readonly @string tableName = "squares"u8;

} // end main_package
