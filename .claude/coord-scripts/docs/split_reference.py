#!/usr/bin/env python3
"""Split docs/ConversionStrategies-Reference.md into docs/ConversionStrategies-Reference/<topic>.md.

Deterministic and re-runnable: it reads the monolith from the given repo root, writes one file per
level-2 section plus a README.md index, replaces the monolith with a stub that keeps every old
anchor alive, and rewrites inbound links in the live documents named in LIVE_INBOUND. It verifies
itself: lossless content (modulo heading promotion and link targets), every anchor link resolves,
every relative path exists. Exit 0 only when every check passes.

usage: split_reference.py <repo-root> [--check-only]
"""
import os, re, sys, unicodedata, json

ROOT = sys.argv[1]
CHECK_ONLY = '--check-only' in sys.argv
DOCS = os.path.join(ROOT, 'docs')
MONO_REL = 'ConversionStrategies-Reference.md'
MONO = os.path.join(DOCS, MONO_REL)
FOLDER_REL = 'ConversionStrategies-Reference'
FOLDER = os.path.join(DOCS, FOLDER_REL)
SUMMARY_REL = 'ConversionStrategies.md'

# section title (exact text after '## ') -> file stem
FILE_NAMES = {
    'Package Conversion': 'package-conversion',
    'Package-Level Variable Initialization Order': 'variable-initialization-order',
    'Compiled Library versus Source Code': 'compiled-library-vs-source',
    'Constant Values': 'constants',
    'Native and Narrow Integer Types': 'native-and-narrow-integers',
    'Named Numeric Types and Constant Contexts': 'named-numeric-types',
    'Floating-Point Formatting': 'floating-point-formatting',
    'Nil and Zero Values': 'nil-and-zero-values',
    'Empty Interface (`any`)': 'empty-interface',
    'Multi-Assignment and Evaluation Order': 'multi-assignment',
    'Short Variable Redeclaration (Shadowing)': 'shadowing',
    'Multi-Result Values and Comma-Ok Forms': 'multi-result-and-comma-ok',
    'Slices and Arrays': 'slices-and-arrays',
    'Strings (`@string` and `sstring`)': 'strings',
    'Maps and Channels': 'maps-and-channels',
    'Generic Constraints': 'generic-constraints',
    'Type Aliasing': 'type-aliasing',
    'Delegates to Value Receiver Instances': 'value-receiver-delegates',
    'Defer / Panic / Recover': 'defer-panic-recover',
    'Expression Switch Statements': 'expression-switch',
    'Type Switch Statements': 'type-switch',
    'Struct Types': 'struct-types',
    'Struct Type Embedding': 'struct-embedding',
    'Interfaces': 'interfaces',
    'Pointers': 'pointers',
    'Implicit Pointer Dereferencing': 'implicit-dereferencing',
    'Labeled Control Flow and Loop Variables': 'labels-and-loop-variables',
    'The `go.golib` support namespace': 'golib-namespace',
    'Source Generators': 'source-generators',
    'The standard-library conversion applies `-tags purego`': 'purego',
    'Manually-Converted Declarations': 'manual-conversions',
    'Comments': 'comments',
    'Deterministic Output': 'deterministic-output',
    'Packages That Do Not Type-Check': 'packages-that-do-not-type-check',
}

# Live documents whose links into the monolith are rewritten to the new files. Everything else
# (dated records, manifests, code comments, proof pages) keeps its link, which lands on the stub.
LIVE_INBOUND = [
    'docs/ConversionStrategies.md',
    'docs/Glossary.md',
    'docs/README.md',
    'docs/Architecture.md',
    'docs/Background.md',
    'docs/Roadmap.md',
    'CLAUDE.md',
    '.claude/rules/docs-records.md',
    '.claude/rules/harness-gates.md',
    '.claude/rules/converter.md',
    '.claude/rules/corpus.md',
    '.claude/rules/golib-gen.md',
    '.claude/skills/validation-bank/SKILL.md',
]

RAW_OPEN = '<!-- {% raw %} — Jekyll/Liquid guard: this page contains Go composite-literal and template syntax ({{ … }}) that Liquid would otherwise parse; the HTML comment hides the tag on GitHub. -->'
RAW_CLOSE = '<!-- {% endraw %} -->'

errors = []
def err(msg):
    errors.append(msg)

# ---------------------------------------------------------------------------------------------
# Markdown scanning helpers

FENCE_RE = re.compile(r'^(\s*)(`{3,}|~{3,})(.*)$')

def fence_mask(lines):
    """True for every line that is inside (or delimits) a fenced code block."""
    mask = [False] * len(lines)
    open_ch, open_len = None, 0
    for i, l in enumerate(lines):
        m = FENCE_RE.match(l)
        if open_ch is None:
            if m:
                open_ch, open_len = m.group(2)[0], len(m.group(2))
                mask[i] = True
        else:
            mask[i] = True
            if m and m.group(2)[0] == open_ch and len(m.group(2)) >= open_len and m.group(3).strip() == '':
                open_ch = None
    return mask

HEADING_RE = re.compile(r'^(#{1,6})\s+(.*?)\s*#*\s*$')

def strip_inline(text):
    """Rendered text of a heading, approximately as GitHub computes it for the slug."""
    t = re.sub(r'!\[([^\]]*)\]\([^)]*\)', r'\1', text)
    t = re.sub(r'\[([^\]]*)\]\([^)]*\)', r'\1', t)
    t = re.sub(r'<[^>]+>', '', t)
    t = t.replace('`', '')
    t = re.sub(r'\\(.)', r'\1', t)
    t = re.sub(r'(\*\*|__)(.+?)\1', r'\2', t)
    t = re.sub(r'(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])', r'\1', t)
    return t

def slug(text):
    out = []
    for ch in strip_inline(text).lower():
        cat = unicodedata.category(ch)
        if cat[0] in 'LMN' or cat == 'Pc' or ch in '- ':
            out.append(ch)
    return ''.join(out).replace(' ', '-')

def github_slugs(headings):
    """github-slugger semantics: first occurrence 'x', then 'x-1', 'x-2', ... skipping taken."""
    occ = {}
    taken = set()
    res = []
    for h in headings:
        base = slug(h)
        s = base
        if s in taken:
            n = occ.get(base, 0)
            while True:
                n += 1
                s = f'{base}-{n}'
                if s not in taken:
                    break
            occ[base] = n
        taken.add(s)
        res.append(s)
    return res

# inline code spans are protected from link rewriting
CODE_SPAN_RE = re.compile(r'(`+)(.+?)\1')
LINK_RE = re.compile(r'(\]\()([^()\s]*(?:\([^()\s]*\)[^()\s]*)*)((?:\s+"[^"]*")?\))')
REFDEF_RE = re.compile(r'^(\s*\[[^\]]+\]:\s*)(\S+)(.*)$')

def rewrite_links_in_line(line, fn):
    """Apply fn(target)->target to every inline link target outside code spans."""
    parts = []
    pos = 0
    for m in CODE_SPAN_RE.finditer(line):
        parts.append(('t', line[pos:m.start()]))
        parts.append(('c', m.group(0)))
        pos = m.end()
    parts.append(('t', line[pos:]))
    out = []
    for kind, s in parts:
        if kind == 'c':
            out.append(s)
        else:
            out.append(LINK_RE.sub(lambda m: m.group(1) + fn(m.group(2)) + m.group(3), s))
    return ''.join(out)

def rewrite_lines(lines, fn):
    mask = fence_mask(lines)
    out = []
    for i, l in enumerate(lines):
        if mask[i]:
            out.append(l)
            continue
        m = REFDEF_RE.match(l)
        if m and not l.lstrip().startswith('[^'):
            out.append(m.group(1) + fn(m.group(2)) + m.group(3))
            continue
        out.append(rewrite_links_in_line(l, fn))
    return out

def collect_targets(lines):
    found = []
    rewrite_lines(lines, lambda t: (found.append(t), t)[1])
    return found

# ---------------------------------------------------------------------------------------------
# Parse the monolith

with open(MONO, encoding='utf-8', newline='') as f:
    mono_text = f.read()
if mono_text.startswith('> **This reference has moved'):
    print('monolith is already a stub; nothing to split')
    sys.exit(2)
NL = '\r\n' if '\r\n' in mono_text[:4000] else '\n'
mono_lines = mono_text.split(NL)
if mono_lines and mono_lines[-1] == '':
    mono_lines = mono_lines[:-1]
mask = fence_mask(mono_lines)

heads = []   # (line index, level, text)
for i, l in enumerate(mono_lines):
    if mask[i]:
        continue
    m = HEADING_RE.match(l)
    if m:
        heads.append((i, len(m.group(1)), m.group(2)))

mono_slugs = github_slugs([h[2] for h in heads])

h2 = [(i, t) for (i, lv, t) in heads if lv == 2]
sections = []  # dicts
for k, (i, t) in enumerate(h2):
    end = h2[k + 1][0] if k + 1 < len(h2) else len(mono_lines)
    sections.append({'title': t, 'start': i, 'end': end})

topics = [s for s in sections if s['title'] == 'Topics']
if len(topics) != 1:
    err(f'expected exactly one Topics section, found {len(topics)}')
body_sections = [s for s in sections if s['title'] != 'Topics']
for s in body_sections:
    if s['title'] not in FILE_NAMES:
        err(f'no file name for section {s["title"]!r}')
    s['stem'] = FILE_NAMES.get(s['title'], slug(s['title']))
if set(FILE_NAMES) - {s['title'] for s in body_sections}:
    err(f'file names for missing sections: {set(FILE_NAMES) - {s["title"] for s in body_sections}}')

# trailing endraw guard belongs to the last section's range; strip it from content
def strip_guards(lines):
    return [l for l in lines if l.strip() not in (RAW_CLOSE,) and not l.startswith('<!-- {% raw %}')]

# global anchor map: monolith slug -> (stem or None for preamble/topics, per-file slug)
anchor_map = {}
file_heads = {}
for s in body_sections:
    hs = [(i, lv, t) for (i, lv, t) in heads if s['start'] <= i < s['end']]
    fslugs = github_slugs([t for (_, _, t) in hs])
    file_heads[s['stem']] = list(zip(hs, fslugs))
    for (i, lv, t), fs in zip(hs, fslugs):
        ms = mono_slugs[heads.index((i, lv, t))]
        anchor_map[ms] = (s['stem'], fs)

# pre-existing anchor links that do not resolve in the monolith (reported, never "fixed")
mono_targets = collect_targets(mono_lines)
mono_slug_set = set(mono_slugs)
pre_broken = sorted({t for t in mono_targets if t.startswith('#') and t[1:] not in mono_slug_set})

# ---------------------------------------------------------------------------------------------
# Link rewriting for content that moves one directory deeper

def is_external(t):
    return re.match(r'^[a-zA-Z][a-zA-Z0-9+.-]*:', t) is not None or t.startswith('//')

def map_anchor(anchor, cur_stem):
    if anchor not in anchor_map:
        return None
    stem, fs = anchor_map[anchor]
    if stem == cur_stem:
        return '#' + fs
    return f'{stem}.md#{fs}'

def moved_link_fn(cur_stem, unresolved):
    def fn(t):
        if not t or is_external(t):
            return t
        if t.startswith('#'):
            r = map_anchor(t[1:], cur_stem)
            if r is None:
                unresolved.append(t)
                return t
            return r
        path, _, frag = t.partition('#')
        if path in (MONO_REL, './' + MONO_REL):
            if not frag:
                return 'README.md'
            r = map_anchor(frag, cur_stem)
            if r is None:
                unresolved.append(t)
                return t
            return r
        if path.startswith('/'):
            return t
        return '../' + t
    return fn

# ---------------------------------------------------------------------------------------------
# Build the section files

outputs = {}  # relative path under docs -> text
stems = [s['stem'] for s in body_sections]
titles = {s['stem']: s['title'] for s in body_sections}
summary_text = open(os.path.join(DOCS, SUMMARY_REL), encoding='utf-8').read()
summary_lines = summary_text.replace('\r\n', '\n').split('\n')
smask = fence_mask(summary_lines)
summary_heads = [HEADING_RE.match(l).group(2) for i, l in enumerate(summary_lines)
                 if not smask[i] and HEADING_RE.match(l)]
summary_slugs = set(github_slugs(summary_heads))

unresolved_all = {}
core_lines = {}
for k, s in enumerate(body_sections):
    content = strip_guards(mono_lines[s['start']:s['end']])
    while content and content[-1].strip() == '':
        content.pop()
    cmask = fence_mask(content)
    promoted = []
    for i, l in enumerate(content):
        if not cmask[i] and HEADING_RE.match(l) and l.startswith('##'):
            promoted.append(l[1:])
        else:
            promoted.append(l)
    unresolved = []
    promoted = rewrite_lines(promoted, moved_link_fn(s['stem'], unresolved))
    if unresolved:
        unresolved_all[s['stem']] = unresolved
    core_lines[s['stem']] = promoted
    title_line = promoted[0]
    rest = promoted[1:]
    ss = slug(s['title'])
    summary_link = f'../{SUMMARY_REL}#{ss}' if ss in summary_slugs else f'../{SUMMARY_REL}'
    prev_s = body_sections[k - 1] if k > 0 else None
    next_s = body_sections[k + 1] if k + 1 < len(body_sections) else None
    crumb = f'[Reference index](README.md) · [Summary of this topic]({summary_link})'
    nav = []
    if prev_s:
        nav.append(f'[← {prev_s["title"]}]({prev_s["stem"]}.md)')
    nav.append('[Index](README.md)')
    if next_s:
        nav.append(f'[{next_s["title"]} →]({next_s["stem"]}.md)')
    body = [title_line, '', crumb] + rest + ['', '---', '', ' · '.join(nav)]
    needs_raw = any(('{{' in l or '{%' in l) for l in body)
    if needs_raw:
        body = [RAW_OPEN] + body + [RAW_CLOSE]
    outputs[f'{FOLDER_REL}/{s["stem"]}.md'] = NL.join(body) + NL

# ---------------------------------------------------------------------------------------------
# The index (README.md): the monolith's own preamble, re-pointed, then a two-level contents list

pre_end = topics[0]['start'] if topics else body_sections[0]['start']
preamble = strip_guards(mono_lines[:pre_end])
while preamble and preamble[-1].strip() == '':
    preamble.pop()
unresolved = []
preamble = rewrite_lines(preamble, moved_link_fn(None, unresolved))
if unresolved:
    unresolved_all['README'] = unresolved
idx = list(preamble)
idx += ['', '## Contents', '',
        'Each topic is its own page. The summary, [Conversion Strategies](../ConversionStrategies.md), '
        'gives the short, example-driven version of each one.', '']
for s in body_sections:
    idx.append(f'- **[{s["title"]}]({s["stem"]}.md)**')
    for ((i, lv, t), fs) in file_heads[s['stem']]:
        if lv == 3:
            idx.append(f'  - [{t}]({s["stem"]}.md#{fs})')
needs_raw = any(('{{' in l or '{%' in l) for l in idx)
if needs_raw:
    idx = [RAW_OPEN] + idx + [RAW_CLOSE]
outputs[f'{FOLDER_REL}/README.md'] = NL.join(idx) + NL

# ---------------------------------------------------------------------------------------------
# The stub at the old path: every old anchor stays a live target and points at its new home

stub = ['> **This reference has moved** to [`ConversionStrategies-Reference/`](ConversionStrategies-Reference/README.md),',
        '> one page per topic. The short, example-driven summary is [Conversion Strategies](ConversionStrategies.md).',
        '',
        '# Conversion Strategies — Technical Reference (moved)',
        '',
        'Links written against the single-page reference still land here. Each line below keeps one old',
        'anchor and links to the same heading in its new page.',
        '']
for (i, lv, t), ms in zip(heads, mono_slugs):
    if ms in anchor_map:
        stem, fs = anchor_map[ms]
        target = f'{FOLDER_REL}/{stem}.md#{fs}'
    else:
        target = f'{FOLDER_REL}/README.md'
    indent = '  ' * max(0, lv - 2)
    label = strip_inline(t).replace('[', '').replace(']', '')
    stub.append(f'{indent}- <a id="{ms}"></a>[{label}]({target})')
needs_raw = any(('{{' in l or '{%' in l) for l in stub)
if needs_raw:
    stub = [RAW_OPEN] + stub + [RAW_CLOSE]
outputs[MONO_REL] = NL.join(stub) + NL

# ---------------------------------------------------------------------------------------------
# Inbound links in live documents

def inbound_fn(doc_rel, unresolved):
    doc_dir = os.path.dirname(doc_rel)
    def fn(t):
        if not t or is_external(t):
            return t
        path, hash_, frag = t.partition('#')
        norm = os.path.normpath(os.path.join(os.path.dirname(os.path.join(ROOT, doc_rel)), path)) if path else None
        if norm != os.path.normpath(MONO):
            return t
        rel_folder = os.path.relpath(FOLDER, os.path.dirname(os.path.join(ROOT, doc_rel))).replace(os.sep, '/')
        if not frag:
            return f'{rel_folder}/README.md'
        if frag not in anchor_map:
            unresolved.append(t)
            return t
        stem, fs = anchor_map[frag]
        return f'{rel_folder}/{stem}.md#{fs}'
    return fn

inbound_changes = {}
for rel in LIVE_INBOUND:
    p = os.path.join(ROOT, rel)
    if not os.path.exists(p):
        continue
    raw = open(p, encoding='utf-8', newline='').read()
    nl = '\r\n' if '\r\n' in raw else '\n'
    ls = raw.split(nl)
    unresolved = []
    new = rewrite_lines(ls, inbound_fn(rel, unresolved))
    if unresolved:
        unresolved_all['inbound:' + rel] = unresolved
    if new != ls:
        n = sum(1 for a, b in zip(ls, new) if a != b)
        inbound_changes[rel] = (nl.join(new), n)

# ---------------------------------------------------------------------------------------------
# Verification

# (1) lossless: every non-heading, non-link character of the body sections survives
def normalize(lines):
    m = fence_mask(lines)
    out = []
    for i, l in enumerate(lines):
        if not m[i]:
            hm = HEADING_RE.match(l)
            if hm:
                l = '#' + hm.group(2)
            l = rewrite_links_in_line(l, lambda t: '')
            rd = REFDEF_RE.match(l)
            if rd:
                l = rd.group(1)
        out.append(l.rstrip())
    while out and out[-1] == '':
        out.pop()
    return out

orig_norm = []
new_norm = []
for s in body_sections:
    seg = strip_guards(mono_lines[s['start']:s['end']])
    orig_norm += normalize(seg)
    new_norm += normalize(core_lines[s['stem']])
    # and the page as written is exactly crumb + core + nav, nothing else
    page = outputs[f'{FOLDER_REL}/{s["stem"]}.md'].split(NL)
    page = [l for l in page if l not in (RAW_OPEN, RAW_CLOSE)]
    core = core_lines[s['stem']]
    if page[0] != core[0] or page[3:3 + len(core) - 1] != core[1:]:
        err(f'page assembly mismatch in {s["stem"]}')
if orig_norm != new_norm:
    for a, (x, y) in enumerate(zip(orig_norm, new_norm)):
        if x != y:
            err(f'LOSSLESS FAIL at normalized line {a}:\n  orig: {x[:160]}\n  new:  {y[:160]}')
            break
    else:
        err(f'LOSSLESS FAIL: lengths differ {len(orig_norm)} vs {len(new_norm)}')

# (2) every anchor link in the new files resolves against the new files' own slugs
new_slugs = {}
for rel, text in outputs.items():
    ls = text.split(NL)
    m = fence_mask(ls)
    hs = [HEADING_RE.match(l).group(2) for i, l in enumerate(ls) if not m[i] and HEADING_RE.match(l)]
    sl = set(github_slugs(hs))
    if rel == MONO_REL:
        sl |= set(mono_slugs)  # the stub's explicit <a id> anchors
    new_slugs[rel] = sl

def check_links(rel, text):
    ls = text.split(NL) if NL in text else text.split('\n')
    base = os.path.dirname(os.path.join(DOCS, rel)) if not rel.startswith('..') else None
    bad = []
    for t in collect_targets(ls):
        if not t or is_external(t):
            continue
        path, _, frag = t.partition('#')
        if not path:
            if frag and frag not in new_slugs.get(rel, set()):
                bad.append(t)
            continue
        full = os.path.normpath(os.path.join(base, path))
        target_rel = os.path.relpath(full, DOCS).replace(os.sep, '/')
        if target_rel in outputs:
            if frag and frag not in new_slugs[target_rel]:
                bad.append(t)
        elif not os.path.exists(full):
            bad.append(t)
    return bad

link_report = {}
for rel, text in outputs.items():
    bad = check_links(rel, text)
    if bad:
        link_report[rel] = bad

# pre-existing breakage is carried, not introduced: compare against the monolith's own broken set
introduced = {}
for rel, bad in link_report.items():
    keep = [b for b in bad if not (b.startswith('#') and b in pre_broken)]
    # a relative path that did not exist before the move either
    keep2 = []
    for b in keep:
        orig = b[3:] if b.startswith('../') else b
        p = orig.partition('#')[0]
        if p and not os.path.exists(os.path.normpath(os.path.join(DOCS, p))) and not p.endswith('.md#'):
            if any(orig == t for t in mono_targets):
                continue
        keep2.append(b)
    if keep2:
        introduced[rel] = keep2

report = {
    'sections': len(body_sections),
    'headings': len(heads),
    'pre_existing_broken_anchor_links': pre_broken,
    'unresolved_during_rewrite': unresolved_all,
    'broken_links_in_outputs': link_report,
    'introduced_breakage': introduced,
    'inbound_changes': {k: v[1] for k, v in inbound_changes.items()},
    'sizes_kb': {k: round(len(v.encode('utf-8')) / 1024) for k, v in outputs.items()},
    'errors': errors,
}
print(json.dumps(report, indent=1, ensure_ascii=False))

if errors or introduced:
    print('FAIL', file=sys.stderr)
    sys.exit(1)
if CHECK_ONLY:
    print('CHECK-ONLY: nothing written')
    sys.exit(0)

os.makedirs(FOLDER, exist_ok=True)
for rel, text in outputs.items():
    with open(os.path.join(DOCS, rel), 'w', encoding='utf-8', newline='') as f:
        f.write(text)
for rel, (text, n) in inbound_changes.items():
    with open(os.path.join(ROOT, rel), 'w', encoding='utf-8', newline='') as f:
        f.write(text)
print('WRITTEN')
