// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// package profile -- go2cs converted at 2022 March 13 06:37:00 UTC
// import "cmd/vendor/github.com/google/pprof/profile" ==> using profile = go.cmd.vendor.github.com.google.pprof.profile_package
// Original source: C:\Program Files\Go\src\cmd\vendor\github.com\google\pprof\profile\index.go
namespace go.cmd.vendor.github.com.google.pprof;

using fmt = fmt_package;
using strconv = strconv_package;
using strings = strings_package;


// SampleIndexByName returns the appropriate index for a value of sample index.
// If numeric, it returns the number, otherwise it looks up the text in the
// profile sample types.

public static partial class profile_package {

private static (nint, error) SampleIndexByName(this ptr<Profile> _addr_p, @string sampleIndex) {
    nint _p0 = default;
    error _p0 = default!;
    ref Profile p = ref _addr_p.val;

    if (sampleIndex == "") {
        {
            var dst = p.DefaultSampleType;

            if (dst != "") {
                {
                    var i__prev1 = i;
                    var t__prev1 = t;

                    foreach (var (__i, __t) in sampleTypes(_addr_p)) {
                        i = __i;
                        t = __t;
                        if (t == dst) {
                            return (i, error.As(null!)!);
                        }
                    }
                    i = i__prev1;
                    t = t__prev1;
                }
            }
        } 
        // By default select the last sample value
        return (len(p.SampleType) - 1, error.As(null!)!);
    }
    {
        var i__prev1 = i;

        var (i, err) = strconv.Atoi(sampleIndex);

        if (err == null) {
            if (i < 0 || i >= len(p.SampleType)) {
                return (0, error.As(fmt.Errorf("sample_index %s is outside the range [0..%d]", sampleIndex, len(p.SampleType) - 1))!);
            }
            return (i, error.As(null!)!);
        }
        i = i__prev1;

    } 

    // Remove the inuse_ prefix to support legacy pprof options
    // "inuse_space" and "inuse_objects" for profiles containing types
    // "space" and "objects".
    var noInuse = strings.TrimPrefix(sampleIndex, "inuse_");
    {
        var i__prev1 = i;
        var t__prev1 = t;

        foreach (var (__i, __t) in p.SampleType) {
            i = __i;
            t = __t;
            if (t.Type == sampleIndex || t.Type == noInuse) {
                return (i, error.As(null!)!);
            }
        }
        i = i__prev1;
        t = t__prev1;
    }

    return (0, error.As(fmt.Errorf("sample_index %q must be one of: %v", sampleIndex, sampleTypes(_addr_p)))!);
}

private static slice<@string> sampleTypes(ptr<Profile> _addr_p) {
    ref Profile p = ref _addr_p.val;

    var types = make_slice<@string>(len(p.SampleType));
    foreach (var (i, t) in p.SampleType) {
        types[i] = t.Type;
    }    return types;
}

} // end profile_package
