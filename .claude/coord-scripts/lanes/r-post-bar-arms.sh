#!/usr/bin/env bash
# Arms for r-post.sh's two live-path guards: the control BAR and the ANCHOR-ADVANCE decision.
#
# Both decisions live on the LIVE path, where proving one HOLDS would otherwise mean making a real
# post to prove it — so each has a door that evaluates it and exits touching nothing: --bar-check
# and --anchor-check. The arms drive the REAL predicate through those doors rather than a copy of it.
#
# THE BAR runs in TWO DIRECTIONS, because a narrowing that only frees things is a hole:
#   FREED   a heading quoting a fixture package name must now PASS   (the false positive)
#   KEPT    every genuine control heading must still REFUSE          (the guard's whole job)
# Its red is this same set against the OLD substring predicate, where the two FREED arms refuse:
#   cp r-post.sh /tmp/oldbar.sh
#   <edit /tmp/oldbar.sh's barmatch back to the bare `probe` substring — and change NOTHING else>
#   R_POST_TOOL=/tmp/oldbar.sh bash r-post-bar-arms.sh        -> the two FREED arms refuse
#
# ⚠ DERIVE that copy from the CURRENT tool, not from an older snapshot. The interlock below refuses a
# copy missing either door, and an older snapshot is missing one — which is how I found that my own
# first old-predicate copy would have driven the anchor arms against a doorless tool.
#
# THE ANCHOR DECISION answers one question: may a POST advance the read anchor? Only when nothing
# landed between the stored anchor and the tip the post appends to. Three lanes shipped tools that
# advanced it unconditionally, so posting recorded unread entries as read.
set -u

TOOL="${R_POST_TOOL:-/c/go2cs-tmp/r-instruments/r-mailbox-post.sh}"

# ⚠⚠ THE INTERLOCK, and it exists because of i9's incident (mailbox 03603d635), not because of one
# here. i9's red arms ran the REAL post tool: the four REDS were safe BECAUSE THEY FAIL — each stops
# at a refusal long before any write — and the CONTROL, the one arm built to pass every gate, ran on
# to the end of the happy path, which on a post tool is a post. A control that is designed to reach
# the end is the last thing to run without a stop.
#
# Every arm below goes through a door that exits before any mutation (--bar-check, --anchor-check),
# so this harness cannot post AS WRITTEN. But `R_POST_TOOL` lets a reader point it at another copy,
# and this file's own header tells them to — at a copy with the OLD predicate, to see the red. A copy
# that predates a door would take the arm's arguments as an ENTRY FILE and a SUBJECT and run LIVE,
# and the arms built to PASS are exactly the ones that would reach the end.
#
# ⚠ Today that case is caught by a DIFFERENT gate (a missing entry file refuses), which is safety by
# accident of another guard rather than by an interlock. So: assert both doors exist in the tool
# under test, BEFORE any arm runs, and refuse by name otherwise.
for door in -- '--bar-check' '--anchor-check'; do
    [ "$door" = "--" ] && continue
    if ! grep -Fq -- "\"$door\"" "$TOOL"; then
        echo "REFUSED: $(basename "$TOOL") carries no $door door."
        echo "  Every arm here drives a LIVE-PATH decision and is safe only because that door exits"
        echo "  before any mutation. Without it the passing arms run the tool for real."
        exit 2
    fi
done

DIR="$(mktemp -d)"
SUBJ="$DIR/subj.txt"
echo "subject line for the arm" > "$SUBJ"

pass=0
fail=0

arm() {
    local want="$1" label="$2" heading="$3" rc
    printf '%s\n\nbody\n' "$heading" > "$DIR/entry.md"
    R_POST_STATE=/c/go2cs-tmp/r-instruments "$TOOL" "$DIR/entry.md" "$SUBJ" --bar-check >/dev/null 2>&1
    rc=$?

    if [ "$rc" -eq "$want" ]; then
        printf '  OK    %-46s rc=%s\n' "$label" "$rc"
        pass=$((pass + 1))
    else
        printf '  FAIL  %-46s rc=%s want=%s\n' "$label" "$rc" "$want"
        fail=$((fail + 1))
    fi
}

echo "== FREED: these must now PASS (rc 0) =="
arm 0 "fixture package name in a quoted error" '## 2026-09-20 — R: CS0266 on go.probe_package.digest'
arm 0 "the sentence describing that refusal"   '## 2026-09-20 — R: the barred word matched probe_package, a package name'
arm 0 "an ordinary seat announcement"          '## 2026-09-20 — R: part (d) is whole, GenTests 47 of 47'

echo "== KEPT: these must still REFUSE (rc 13) =="
arm 13 "a bare probe"                          '## 2026-09-20 — R: a probe for the adapter shape'
arm 13 "probes, plural"                        '## 2026-09-20 — R: two probes over one compilation'
arm 13 "probed, past tense"                    '## 2026-09-20 — R: the junction was probed with the setting'
arm 13 "a planted control"                     '## 2026-09-20 — R: a planted token must make it fire'
arm 13 "the bracket tag"                       '## 2026-09-20 — R: [CTL] an admission arm'
arm 13 "self-test by name"                     '## 2026-09-20 — R: the self-test figure is invariant'
arm 13 "census control by name"                '## 2026-09-20 — R: a census control for the arm'

echo "== THE ANCHOR-ADVANCE DECISION: a post must never claim a read =="
# Driven through --anchor-check, the door that evaluates the branch and exits. The branch lives on
# the live path, and proving it HOLDS must not require making a real post to prove it.
anchor_arm() {
    local want="$1" label="$2" prev="$3" pretip="$4" rc
    R_POST_STATE=/c/go2cs-tmp/r-instruments "$TOOL" --anchor-check "$prev" "$pretip" >/dev/null 2>&1
    rc=$?

    if [ "$rc" -eq "$want" ]; then
        printf '  OK    %-46s rc=%s\n' "$label" "$rc"
        pass=$((pass + 1))
    else
        printf '  FAIL  %-46s rc=%s want=%s\n' "$label" "$rc" "$want"
        fail=$((fail + 1))
    fi
}

anchor_arm 0  "nothing landed since the anchor -> ADVANCE" "aaaaaaa" "aaaaaaa"
anchor_arm 20 "entries landed since the anchor -> HOLD"    "aaaaaaa" "bbbbbbb"
anchor_arm 0  "no prior anchor -> ADVANCE"                 ""        "bbbbbbb"

rm -rf "$DIR"
echo
echo "arms: $pass passed, $fail failed"
[ "$fail" -eq 0 ]
