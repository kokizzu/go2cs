// Copyright 2015 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package main -- go2cs converted at 2022 March 13 06:28:35 UTC
// Original source: C:\Program Files\Go\src\cmd\compile\internal\test\testdata\gen\zeroGen.go
namespace go;

using bytes = bytes_package;
using fmt = fmt_package;
using format = go.format_package;
using ioutil = io.ioutil_package;
using log = log_package;


// This program generates tests to verify that zeroing operations
// zero the data they are supposed to and clobber no adjacent values.

// run as `go run zeroGen.go`.  A file called zero.go
// will be written into the parent directory containing the tests.

public static partial class main_package {

private static array<nint> sizes = new array<nint>(new nint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 16, 17, 23, 24, 25, 31, 32, 33, 63, 64, 65, 1023, 1024, 1025 });
private static array<nint> usizes = new array<nint>(new nint[] { 8, 16, 24, 32, 64, 256 });

private static void Main() => func((_, panic, _) => {
    ptr<object> w = @new<bytes.Buffer>();
    fmt.Fprintf(w, "// Code generated by gen/zeroGen.go. DO NOT EDIT.\n\n");
    fmt.Fprintf(w, "package main\n");
    fmt.Fprintf(w, "import \"testing\"\n");

    {
        var s__prev1 = s;

        foreach (var (_, __s) in sizes) {
            s = __s; 
            // type for test
            fmt.Fprintf(w, "type Z%d struct {\n", s);
            fmt.Fprintf(w, "  pre [8]byte\n");
            fmt.Fprintf(w, "  mid [%d]byte\n", s);
            fmt.Fprintf(w, "  post [8]byte\n");
            fmt.Fprintf(w, "}\n"); 

            // function being tested
            fmt.Fprintf(w, "//go:noinline\n");
            fmt.Fprintf(w, "func zero%d_ssa(x *[%d]byte) {\n", s, s);
            fmt.Fprintf(w, "  *x = [%d]byte{}\n", s);
            fmt.Fprintf(w, "}\n"); 

            // testing harness
            fmt.Fprintf(w, "func testZero%d(t *testing.T) {\n", s);
            fmt.Fprintf(w, "  a := Z%d{[8]byte{255,255,255,255,255,255,255,255},[%d]byte{", s, s);
            {
                nint i__prev2 = i;

                for (nint i = 0; i < s; i++) {
                    fmt.Fprintf(w, "255,");
                }


                i = i__prev2;
            }
            fmt.Fprintf(w, "},[8]byte{255,255,255,255,255,255,255,255}}\n");
            fmt.Fprintf(w, "  zero%d_ssa(&a.mid)\n", s);
            fmt.Fprintf(w, "  want := Z%d{[8]byte{255,255,255,255,255,255,255,255},[%d]byte{", s, s);
            {
                nint i__prev2 = i;

                for (i = 0; i < s; i++) {
                    fmt.Fprintf(w, "0,");
                }


                i = i__prev2;
            }
            fmt.Fprintf(w, "},[8]byte{255,255,255,255,255,255,255,255}}\n");
            fmt.Fprintf(w, "  if a != want {\n");
            fmt.Fprintf(w, "    t.Errorf(\"zero%d got=%%v, want %%v\\n\", a, want)\n", s);
            fmt.Fprintf(w, "  }\n");
            fmt.Fprintf(w, "}\n");
        }
        s = s__prev1;
    }

    {
        var s__prev1 = s;

        foreach (var (_, __s) in usizes) {
            s = __s; 
            // type for test
            fmt.Fprintf(w, "type Z%du1 struct {\n", s);
            fmt.Fprintf(w, "  b   bool\n");
            fmt.Fprintf(w, "  val [%d]byte\n", s);
            fmt.Fprintf(w, "}\n");

            fmt.Fprintf(w, "type Z%du2 struct {\n", s);
            fmt.Fprintf(w, "  i   uint16\n");
            fmt.Fprintf(w, "  val [%d]byte\n", s);
            fmt.Fprintf(w, "}\n"); 

            // function being tested
            fmt.Fprintf(w, "//go:noinline\n");
            fmt.Fprintf(w, "func zero%du1_ssa(t *Z%du1) {\n", s, s);
            fmt.Fprintf(w, "  t.val = [%d]byte{}\n", s);
            fmt.Fprintf(w, "}\n"); 

            // function being tested
            fmt.Fprintf(w, "//go:noinline\n");
            fmt.Fprintf(w, "func zero%du2_ssa(t *Z%du2) {\n", s, s);
            fmt.Fprintf(w, "  t.val = [%d]byte{}\n", s);
            fmt.Fprintf(w, "}\n"); 

            // testing harness
            fmt.Fprintf(w, "func testZero%du(t *testing.T) {\n", s);
            fmt.Fprintf(w, "  a := Z%du1{false, [%d]byte{", s, s);
            {
                nint i__prev2 = i;

                for (i = 0; i < s; i++) {
                    fmt.Fprintf(w, "255,");
                }


                i = i__prev2;
            }
            fmt.Fprintf(w, "}}\n");
            fmt.Fprintf(w, "  zero%du1_ssa(&a)\n", s);
            fmt.Fprintf(w, "  want := Z%du1{false, [%d]byte{", s, s);
            {
                nint i__prev2 = i;

                for (i = 0; i < s; i++) {
                    fmt.Fprintf(w, "0,");
                }


                i = i__prev2;
            }
            fmt.Fprintf(w, "}}\n");
            fmt.Fprintf(w, "  if a != want {\n");
            fmt.Fprintf(w, "    t.Errorf(\"zero%du2 got=%%v, want %%v\\n\", a, want)\n", s);
            fmt.Fprintf(w, "  }\n");
            fmt.Fprintf(w, "  b := Z%du2{15, [%d]byte{", s, s);
            {
                nint i__prev2 = i;

                for (i = 0; i < s; i++) {
                    fmt.Fprintf(w, "255,");
                }


                i = i__prev2;
            }
            fmt.Fprintf(w, "}}\n");
            fmt.Fprintf(w, "  zero%du2_ssa(&b)\n", s);
            fmt.Fprintf(w, "  wantb := Z%du2{15, [%d]byte{", s, s);
            {
                nint i__prev2 = i;

                for (i = 0; i < s; i++) {
                    fmt.Fprintf(w, "0,");
                }


                i = i__prev2;
            }
            fmt.Fprintf(w, "}}\n");
            fmt.Fprintf(w, "  if b != wantb {\n");
            fmt.Fprintf(w, "    t.Errorf(\"zero%du2 got=%%v, want %%v\\n\", b, wantb)\n", s);
            fmt.Fprintf(w, "  }\n");
            fmt.Fprintf(w, "}\n");
        }
        s = s__prev1;
    }

    fmt.Fprintf(w, "func TestZero(t *testing.T) {\n");
    {
        var s__prev1 = s;

        foreach (var (_, __s) in sizes) {
            s = __s;
            fmt.Fprintf(w, "  testZero%d(t)\n", s);
        }
        s = s__prev1;
    }

    {
        var s__prev1 = s;

        foreach (var (_, __s) in usizes) {
            s = __s;
            fmt.Fprintf(w, "  testZero%du(t)\n", s);
        }
        s = s__prev1;
    }

    fmt.Fprintf(w, "}\n"); 

    // gofmt result
    var b = w.Bytes();
    var (src, err) = format.Source(b);
    if (err != null) {
        fmt.Printf("%s\n", b);
        panic(err);
    }
    err = ioutil.WriteFile("../zero_test.go", src, 0666);
    if (err != null) {
        log.Fatalf("can't write output: %v\n", err);
    }
});

} // end main_package
