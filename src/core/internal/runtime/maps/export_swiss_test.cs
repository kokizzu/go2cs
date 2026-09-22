// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build goexperiment.swissmap
namespace go.@internal.runtime;

using abi = go.@internal.abi_package;
using @unsafe = unsafe_package;
using go.@internal;
using static go.@internal.runtime.maps_package;

partial class maps_internal_test_package {

internal static ж<abi.SwissMapType> newTestMapType<K, V>() {
    map<K, V> m = default!;
    var mTyp = abi.TypeOf(m);
    var mt = mTyp.Reinterpret<abi.Type, abi.SwissMapType>();
    return mt;
}

} // end maps_internal_test_package
