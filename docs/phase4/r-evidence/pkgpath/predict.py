"""Predict option A's corpus footprint: which committed package_info.cs files would gain an ImportPath.

For each production package_info.cs under src/core, decode exactly what golib's decoders do --
namespace tail (drop the 'go' root, strip '@' escapes, '.' -> '/') + '/' + the [GoPackage] name --
and compare it with the package's Go import path, which for the stdlib corpus is the package's
directory relative to src/core (the per-GOOS folder of an L3 package is NOT part of the path).
"""
import os, re, sys

core = sys.argv[1]
goos_dirs = {"windows", "linux", "darwin"}
ns_re = re.compile(r"^namespace\s+([\w.@]+)\s*;", re.M)
pkg_re = re.compile(r'^\[GoPackage\("([^"]*)"')

rows = []
for dirpath, dirnames, filenames in os.walk(core):
    dirnames[:] = [d for d in dirnames if d not in ("bin", "obj", "Generated")]
    if "package_info.cs" not in filenames:
        continue
    text = open(os.path.join(dirpath, "package_info.cs"), encoding="utf-8-sig").read()
    ns = ns_re.search(text)
    names = [m.group(1) for m in (pkg_re.match(l) for l in text.splitlines()) if m]
    if not ns or not names:
        continue
    rel = os.path.relpath(dirpath, core).replace("\\", "/")
    parts = rel.split("/")
    goos = None
    if parts[-1] in goos_dirs and len(parts) > 1:
        goos = parts[-1]
        parts = parts[:-1]
    import_path = "/".join(parts)
    name = names[0]
    namespace = ns.group(1).replace("@", "")
    derived = name if namespace == "go" else namespace[len("go."):].replace(".", "/") + "/" + name
    rows.append((import_path, goos or "flat", namespace, name, derived, derived != import_path))

stamped = [r for r in rows if r[5]]
print(f"package_info.cs files scanned: {len(rows)}; would be stamped: {len(stamped)}")
for r in sorted(stamped):
    print(f"  {r[0]:55} [{r[1]}] ns={r[2]} name={r[3]} decodes-as={r[4]}")
