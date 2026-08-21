// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("bytes/export_test.go", "export_test.cs", "")]

namespace go;

using static go.bytes_package;

partial class bytes_internal_test_package {

// Export func for testing
public static Func<slice<byte>, byte, nint> IndexBytePortable = indexBytePortable;

} // end bytes_internal_test_package
