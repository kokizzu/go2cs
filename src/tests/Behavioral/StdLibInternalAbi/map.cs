[assembly: go.GoPositionMap("map.go", "map.cs", "")]

namespace go;

partial class main_package {

public static UntypedInt MapBucketCountBits => 3;
public static UntypedInt MapBucketCount => /* 1 << MapBucketCountBits */ 8;
public static UntypedInt MapMaxKeyBytes => 128;
public static UntypedInt MapMaxElemBytes => 128;

} // end main_package
