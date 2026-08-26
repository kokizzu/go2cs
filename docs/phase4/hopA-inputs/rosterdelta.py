import re, subprocess, sys

# Rebank staging, read-only: which roster rows' banked arithmetic moved at 1.23.12?
# For each roster row, compare its Tests/Disclosed columns against the proof page's
# "**N matched · D disclosed**" header — reading the page from a given git REF (so the two
# bank branches can be measured without touching the working tree).
#   usage: rosterdelta.py <repo> <ref-for-pages> [<ref2> ...]
# Later refs win per page (banks are disjoint, so at most one ref rewrites a given page).
repo = sys.argv[1]
refs = sys.argv[2:]

def show(ref, path):
    r = subprocess.run(["git", "-C", repo, "show", f"{ref}:{path}"],
                       capture_output=True)
    return r.stdout.decode("utf-8", errors="replace") if r.returncode == 0 else None

roster = show("HEAD", "docs/ValidatedTestPackages.md")
rowpat = re.compile(r"^\| \[`([^`]+)`\]\([^)]*\) \| (\d+) \|\s*(\d*)\s*\|")
hdrpat = re.compile(r"\*\*(\d+) matched (?:·|\xb7|&middot;|\.) (\d+) disclosed\*\*")

moved, missing = [], []
parsed = 0
for line in roster.splitlines():
    m = rowpat.match(line)
    if not m:
        continue
    parsed += 1
    pkg, tests, disc = m.group(1), int(m.group(2)), int(m.group(3) or 0)
    dot = pkg.replace("/", ".")
    page = None
    src = "HEAD"
    for ref in refs:
        p = show(ref, f"docs/validation/current/{dot}.md")
        if p is not None:
            # a ref only counts if it CHANGED the page vs HEAD
            base = show("HEAD", f"docs/validation/current/{dot}.md")
            if p != base:
                page, src = p, ref
    if page is None:
        page = show("HEAD", f"docs/validation/current/{dot}.md")
    if page is None:
        missing.append(pkg)
        continue
    h = hdrpat.search(page)
    if not h:
        missing.append(pkg + " (no header match)")
        continue
    nm, nd = int(h.group(1)), int(h.group(2))
    if (nm, nd) != (tests, disc):
        moved.append((pkg, tests, disc, nm, nd, src))

print(f"parsed {parsed} roster rows (control: must be 162)")
print(f"rows moved: {len(moved)}")
for pkg, t, d, nm, nd, src in moved:
    print(f"  {pkg:35} {t}+{d} -> {nm}+{nd}   [{src[:20]}]")
if missing:
    print(f"pages missing/unparsed: {len(missing)}")
    for p in missing[:10]:
        print("  " + p)
