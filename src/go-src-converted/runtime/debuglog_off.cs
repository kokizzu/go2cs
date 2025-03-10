// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build !debuglog
// +build !debuglog

// package runtime -- go2cs converted at 2022 March 13 05:24:23 UTC
// import "runtime" ==> using runtime = go.runtime_package
// Original source: C:\Program Files\Go\src\runtime\debuglog_off.go
namespace go;

public static partial class runtime_package {

private static readonly var dlogEnabled = false;



private partial struct dlogPerM {
}

private static ptr<dlogger> getCachedDlogger() {
    return _addr_null!;
}

private static bool putCachedDlogger(ptr<dlogger> _addr_l) {
    ref dlogger l = ref _addr_l.val;

    return false;
}

} // end runtime_package
