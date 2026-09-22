// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using abi = @internal.abi_package;
using goarch = @internal.goarch_package;
using reflect = reflect_package;
using testing = testing_package;
using @internal;
using static go.unique_package;
using ꓸꓸꓸuintptr = Span<uintptr>;

partial class unique_internal_test_package {

public static void TestMakeCloneSeq(ж<testing.T> Ꮡt) {
    testCloneSeq<testString, testString>(Ꮡt, cSeq(0));
    testCloneSeq<testIntArray, testIntArray>(Ꮡt, cSeq());
    testCloneSeq<testEface, testEfaceᴅ>(Ꮡt, cSeq());
    testCloneSeq<testStringArray, testStringArray>(Ꮡt, cSeq(0, 2 * goarch.PtrSize, 4 * goarch.PtrSize));
    testCloneSeq<testStringStruct, testStringStruct>(Ꮡt, cSeq(0));
    testCloneSeq<testStringStructArrayStruct, testStringStructArrayStruct>(Ꮡt, cSeq(0, 2 * goarch.PtrSize));
    testCloneSeq<testStruct, testStruct>(Ꮡt, cSeq(8));
}

internal static global::go.unique_package.cloneSeq cSeq(params ꓸꓸꓸuintptr stringOffsetsʗp) {
    var stringOffsets = stringOffsetsʗp.slice();

    return new cloneSeq(stringOffsets: stringOffsets);
}

internal static void testCloneSeq<T, Tᴺ>(ж<testing.T> Ꮡt, global::go.unique_package.cloneSeq want) {
    @string typName = reflect.TypeFor<Tᴺ>().Name();
    var typ = abi.TypeFor<T>();
    var typʗ1 = typ;
    var wantʗ1 = want;
    Ꮡt.Run(typName, (ж<testing.T> tΔ1) => {
        var got = makeCloneSeq(typʗ1);
        if (!reflect.DeepEqual(got, wantʗ1)) {
            tΔ1.Errorf("unexpected cloneSeq for type %s: got %#v, want %#v"u8, typName, got, wantʗ1);
        }
    });
}

} // end unique_internal_test_package
