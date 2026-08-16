# go.crypto.internal.mlkem768

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-not_yet_validated-orange?logo=go)](https://go2cs.net/ValidatedTestPackages.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/crypto/internal/mlkem768@go1.23.1)\
[![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/crypto/internal/mlkem768) [![Source](https://img.shields.io/badge/Source-@1.23.1.6-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.6/src/core/crypto/internal/mlkem768)

Package mlkem768 implements the quantum-resistant key encapsulation method ML-KEM (formerly known as Kyber).

Only the recommended ML-KEM-768 parameter set is provided.

The version currently implemented is the one specified by [NIST FIPS 203 ipd](https://doi.org/10.6028/NIST.FIPS.203.ipd), with the unintentional transposition of the matrix A reverted to match the behavior of [Kyber version 3.0](https://pq-crystals.org/kyber/data/kyber-specification-round3-20210804.pdf). Future versions of this package might introduce backwards incompatible changes to implement changes to FIPS 203.

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
