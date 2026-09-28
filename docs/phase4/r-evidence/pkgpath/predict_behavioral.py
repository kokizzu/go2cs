"""Predict option A's CNR footprint over the behavioral corpus.

For each package_info.cs under src/tests/Behavioral, the Go import path is the nearest go.mod's
module path + the directory below it (Go's own rule); a `package main` package's reflect path is
"main". Decode what golib's decoders decode (namespace tail + '/' + [GoPackage] name) and compare.
"""
import os, re, sys

root = sys.argv[1]
ns_re = re.compile(r"^namespace\s+([\w.@]+)\s*;", re.M)
pkg_re = re.compile(r'^\[GoPackage\("([^"]*)"')
mod_re = re.compile(r"^module\s+(\S+)", re.M)

def module_of(d):
    cur = d
    while True:
        gm = os.path.join(cur, "go.mod")
        if os.path.exists(gm):
            m = mod_re.search(open(gm, encoding="utf-8").read())
            return cur, (m.group(1) if m else None)
        parent = os.path.dirname(cur)
        if parent == cur or not parent.startswith(root):
            return None, None
        cur = parent

rows = []
for dirpath, dirnames, filenames in os.walk(root):
    dirnames[:] = [d for d in dirnames if d not in ("bin", "obj", "Generated")]
    if "package_info.cs" not in filenames:
        continue
    text = open(os.path.join(dirpath, "package_info.cs"), encoding="utf-8-sig").read()
    ns = ns_re.search(text)
    names = [m.group(1) for m in (pkg_re.match(l) for l in text.splitlines()) if m]
    if not ns or not names:
        continue
    name = names[0]
    moddir, module = module_of(dirpath)
    if module is None:
        rows.append((dirpath, name, "?", "?", "NO-GO.MOD"))
        continue
    sub = os.path.relpath(dirpath, moddir).replace("\\", "/")
    import_path = "main" if name == "main" else (module if sub == "." else module + "/" + sub)
    namespace = ns.group(1).replace("@", "")
    derived = name if namespace == "go" else namespace[len("go."):].replace(".", "/") + "/" + name
    rows.append((os.path.relpath(dirpath, root).replace("\\", "/"), name, import_path, derived, derived != import_path))

stamped = [r for r in rows if r[4] is True]
odd = [r for r in rows if r[4] not in (True, False)]
print(f"behavioral package_info.cs scanned: {len(rows)}; would be stamped: {len(stamped)}; unresolved: {len(odd)}")
for r in sorted(stamped):
    print(f"  {r[0]:50} name={r[1]} go-path={r[2]} decodes-as={r[3]}")
for r in odd:
    print("  UNRESOLVED", r)
