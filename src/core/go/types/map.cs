// Code generated by "go test -run=Generate -write=all"; DO NOT EDIT.
// Source: ../../cmd/compile/internal/types2/map.go
// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.go;

partial class types_package {

// A Map represents a map type.
[GoType] partial struct Map {
    internal ΔType key;
    internal ΔType elem;
}

// NewMap returns a new map for the given key and element types.
public static ж<Map> NewMap(ΔType key, ΔType elem) {
    return Ꮡ(new Map(key: key, elem: elem));
}

// Key returns the key type of map m.
[GoRecv] public static ΔType Key(this ref Map m) {
    return m.key;
}

// Elem returns the element type of map m.
[GoRecv] public static ΔType Elem(this ref Map m) {
    return m.elem;
}

[GoRecv("capture")] public static ΔType Underlying(this ref Map t) {
    return ~t;
}

[GoRecv] public static @string String(this ref Map t) {
    return TypeString(~t, default!);
}

} // end types_package
