// Copyright 2015 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using context = context_package;
using Δruntime = runtime_package;
using testing = testing_package;
using static go.net_package;

partial class net_internal_test_package {

internal static void allResolvers(ж<testing.T> Ꮡt, Action<ж<testing.T>> f) {
    Ꮡt.Run(defaultResolverˢ, f);
    Ꮡt.Run(forcedGoResolverˢ, (ж<testing.T> tΔ1) => {
        GoFrame ᒐ = default;
        try {
            // On plan9 the forceGoDNS might not force the go resolver, currently
            // it is only forced when the Resolver.Dial field is populated.
            // See conf.go mustUseGoResolver.
            defer(forceGoDNS(), ref ᒐ);
            f(tΔ1);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    });
    Ꮡt.Run(forcedCgoResolverˢ, (ж<testing.T> tΔ2) => {
        GoFrame ᒐ = default;
        try {
            defer(forceCgoDNS(), ref ᒐ);
            f(tΔ2);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    });
}

// forceGoDNS forces the resolver configuration to use the pure Go resolver
// and returns a fixup function to restore the old settings.
internal static Action forceGoDNS() {
    var c = systemConf();
    var oldGo = c.Value.netGo;
    var oldCgo = c.Value.netCgo;
    var cʗ1 = c;
    var fixup = () => {
        cʗ1.Value.netGo = oldGo;
        cʗ1.Value.netCgo = oldCgo;
    };
    c.Value.netGo = true;
    c.Value.netCgo = false;
    return fixup;
}

// forceCgoDNS forces the resolver configuration to use the cgo resolver
// and returns a fixup function to restore the old settings.
internal static Action forceCgoDNS() {
    var c = systemConf();
    var oldGo = c.Value.netGo;
    var oldCgo = c.Value.netCgo;
    var cʗ1 = c;
    var fixup = () => {
        cʗ1.Value.netGo = oldGo;
        cʗ1.Value.netCgo = oldCgo;
    };
    c.Value.netGo = false;
    c.Value.netCgo = true;
    return fixup;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object cgoResolverNotAvailableˢ = (@string)"cgo resolver not available"u8;
internal static readonly @string goDevˢ = "go.dev"u8;
internal static readonly object mustUseGoResolverTrueˢ = (@string)"mustUseGoResolver = true, want false"u8;

public static void TestForceCgoDNS(ж<testing.T> Ꮡt) {
    GoFrame ᒐ = default;
    try {
        if (!cgoAvailable) {
            Ꮡt.Skip(cgoResolverNotAvailableˢ);
        }
        defer(forceCgoDNS(), ref ᒐ);
        var (order, _) = systemConf().hostLookupOrder(nil, goDevˢ);
        if (order != hostLookupCgo) {
            Ꮡt.Fatalf("hostLookupOrder returned: %v, want cgo"u8, order);
        }
        (order, _) = systemConf().addrLookupOrder(nil, "192.0.2.1"u8);
        if (order != hostLookupCgo) {
            Ꮡt.Fatalf("addrLookupOrder returned: %v, want cgo"u8, order);
        }
        if (systemConf().mustUseGoResolver(nil)) {
            Ꮡt.Fatal(mustUseGoResolverTrueˢ);
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object mustUseGoResolverFalseˢ = (@string)"mustUseGoResolver = false, want true"u8;

public static void TestForceGoDNS(ж<testing.T> Ꮡt) {
    GoFrame ᒐ = default;
    try {
        ж<global::go.net_package.Resolver> resolver = default!;
        if (Δruntime.GOOS == "plan9"u8) {
            resolver = Ꮡ(new Resolver(
                Dial: (context.Context _Δp0, @string _Δp1, @string _Δp2) => {
                    throw panic("unreachable");
                }
            ));
        }
        defer(forceGoDNS(), ref ᒐ);
        var (order, _) = systemConf().hostLookupOrder(resolver, goDevˢ);
        if (order == hostLookupCgo) {
            Ꮡt.Fatalf("hostLookupOrder returned: %v, want go resolver order"u8, order);
        }
        (order, _) = systemConf().addrLookupOrder(resolver, "192.0.2.1"u8);
        if (order == hostLookupCgo) {
            Ꮡt.Fatalf("addrLookupOrder returned: %v, want go resolver order"u8, order);
        }
        if (!systemConf().mustUseGoResolver(resolver)) {
            Ꮡt.Fatal(mustUseGoResolverFalseˢ);
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end net_internal_test_package
