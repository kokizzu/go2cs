// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !ios && !android
[assembly: go.GoPositionMap("time/zoneinfo_goroot.go", "zoneinfo_goroot.cs", "AAoSgoKU")]

namespace go;

partial class time_package {

internal static (@string, bool) gorootZoneSource(@string goroot) {
    if (goroot == ""u8) {
        return ("", false);
    }
    return (goroot + "/lib/time/zoneinfo.zip", true);
}

} // end time_package
