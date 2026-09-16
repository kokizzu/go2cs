---
name: mailbox
description: Post to or read the fleet mailbox. Anchors, read discipline, push contention, duplicate-post defences and the state-advancing-tool rules.
---

# Mailbox

<!-- EXTRACTED VERBATIM from CLAUDE.md at 1800b04f8, lines 6472-6597.
     Phase 1 of docs/PLAN-context-diet.md: RELOCATION ONLY, no compression, no edits.
     The byte-identical original is docs/doctrine/JOURNAL-2026-09-12.md.
     Phase 2 (2026-09-12) distilled this file: every dated narrative was moved into an HTML
     comment OPENING ON THE LAST LINE OF THE RULE IT JUSTIFIES (so stripping leaves no residual
     blank line and the provenance costs ZERO tokens). Nothing was deleted; if a rule reads thin,
     its full derivation is in the comment under it and in the JOURNAL.
     2026-09-12: batch19 items (branch claude/coord-doctrine-batch19, commits 24bfc8304 /
     c5e17217b / e9e56b657, old-CLAUDE.md anchors 6492 / 6550 / 6560 / 6585) were merged into this
     shape — three became new visible rules, two amended existing rules and folded into their
     comments. -->
## Before you post a claim
- **A post that STATES a measurement is a DEPENDENT step of that measurement.** Never compose the
  claim in the same response as the command that produces it — write it from the verification's own
  output. Parallelism is for independent items only, and a claim about a result is never independent
  of the result. <!-- ⚠ 2026-09-04, the coordinator against itself: "pre-verified at the remote —
  five commits, zero markers, zero census hits" went out in a reply issued in PARALLEL with the
  fetch, and the fetch answered `couldn't find remote ref`; the branch had never been pushed. -->
- **A gate composed into the same command as the action it gates cannot gate it.** Run the security
  census as its own command, against the PUSHED TIP, before you announce. <!-- ⚠ A lane ran its
  security census in the same command as its push: the census executed, printed clean, and its
  verdict could not have stopped anything — a reassurance rather than a check. Same family as the
  exit-code-through-a-pipe trap and the `;`-instead-of-`&&` chain that once committed conflict
  markers: the instrument runs, and the ORDERING makes its verdict inert. Nearly invisible, because
  the log shows the gate running and shows it clean; the remedy is one command, not a re-cut. -->
- **An environment-shaped message in a FAIL line is the tell of a CONCURRENCY TRANSIENT, and its arm
  is an ISOLATED re-run.** The gate honestly reports NOT MEASURED by name; the COMPLETION is a
  per-package re-transpile IN PLACE after the run with a `git status` of that directory, stated in
  the landing post — never a whole-CNR re-run and never a "close enough". <!-- ⚠ measured
  2026-09-02/03. The converter suite failed ONCE under five concurrent sub-agents with
  `go: go.mod file not found in current directory or any parent directory` from its `go` child and
  passed 3/3 in isolation at the same tip twelve minutes later; CNR printed `[transpile FAILED] <Package>` mid-run
  under seven concurrent processes while a hand transpile with the SAME binary minutes later emitted
  a `main.cs` byte-identical to the committed golden. The mechanism is UNROOTED (a shell-out whose
  cwd vanished under it is the shape; which concurrent purge did it is not measured). -->
- **Stamp a ledger entry from `git log --date=format-local` of the post it records, never from an
  estimate.** Lane commit stamps carry the LANE's clock. <!-- ⚠ 2026-09-03/04: a cloud container's
  clock is UTC, and reading those stamps as local ran a ledger ~35 minutes fast for an hour and
  mis-sized a running CNR leg as past its budget when it was on pace. -->

## Posting
- **The edit-to-commit-to-push window in a shared clone is ONE command** — an uncommitted edit there
  belongs to whoever touches the path next. Stage ONLY the file you own (never `git add -A`, which IS
  the sweep mechanism); restore ONLY that file on your own failure path (never `git reset --hard` in
  a shared clone). A clean `git status` is never evidence that work landed — read the pushed TIP.
  <!-- ⚠ 2026-09-04: a sibling's mailbox post swept a scrub lane's three uncommitted substitutions
  into its OWN commit, and a second sibling's tree operation then reverted the scrub lane's remaining
  files before they could commit — so `git status` read CLEAN and `git commit` read "nothing to
  commit" while the work was gone. The coordinator's own post script ran `git reset --hard` in the
  shared clone when a commit did not land, which is the revert mechanism; scoped to its own file the
  same day. -->
- **A lost mailbox race is answered by a MERGE, never by a force — and a `--force-with-lease` whose
  expected SHA is READ AT PUSH TIME is that force wearing the careful spelling.** A lease asserting
  "the remote is whatever it currently is" is always satisfied. Push through `src/safe-push.sh`,
  whose RANGE step (`git rev-list --count <remote>..<local>`) ERRORS on an object the local clone
  lacks — exactly what a moved remote produces. Repair a dropped post by fetch, READ the interleaved
  commits, re-append, re-push; and attribute a mailbox event from `git log --graph`, never from
  prose. <!-- ⚠ 2026-09-08: a lane hand-rolled `REMOTE=$(git ls-remote …)` then
  `--force-with-lease=…:$REMOTE`, skipping `src/safe-push.sh`, and the push DROPPED another lane's
  post. It was restored by MERGE with all three posts verified present. The coordinator
  mis-attributed both halves to the wrong lane TWICE — first from a reconstruction, then from the
  restore-merge's OWN subject line — before reading the GRAPH. Twin of the lease defect recorded
  under "Writing the guard inside the tool": a lease is also NOT EVALUATED when there is nothing to
  push, so both spellings of "careful" fail silently. -->
- **Pass free text through a FILE (`-F`), never as a native argument.** <!-- ⚠ 2026-09-03/04: a
  mailbox post instrument split its message on embedded double quotes (PS 5.1 native-argument
  quoting) and the commit failed as a bad pathspec. -->
- **Launch background work in its OWN call.** A `&` background launch inside a compound bash command
  swallows everything after it, heredocs included, and the only visible output is a tool banner.
  <!-- ⚠ 2026-09-06: an urgent retraction was written as `cmd & … cat > entry <<EOF … post`: the
  launch backgrounded the rest, the entry file was never created, and the post never ran. The tool's
  ENTRY-FILE-MISSING guard caught the second attempt; nothing caught the first. -->
- **The mailbox file ROTATES before its blob is large, and NO BUILD CLONE TRACKS `claude/mailbox`.** Git
  stores a WHOLE NEW BLOB per post, so every post makes every tracking clone write the file's FULL SIZE as a
  loose object: keep the branch out of a build clone's fetch refspec (`^refs/heads/claude/mailbox`) and do
  mailbox work in a dedicated single-branch clone or through the API. <!-- ⚠ 2026-09-13, i9 93e62dc17: at
  15.6 MB the file killed SEVEN clones on one box, each reporting a DIFFERENT corrupt object — and the
  corrupt object was the mailbox file at 57c4ac305 byte for byte, which is what identified the mechanism
  rather than a disk fault. Rotated at 5e70540f4 by COORD ruling d3216183f; entries before 5e70540f4 are in
  docs/phase4/MAILBOX-archive-2026-09-13.md. The cost is per-CLONE and per-POST, so it scales with the
  fleet: a clone that never needs the channel should not pay for it. -->
- **A negative refspec governs which REF is written, not which OBJECTS transfer** — an explicit
  `git fetch origin claude/mailbox` still lands the blob, so the refspec alone does not close the hole.
  **Every clone sets `fetch.unpackLimit=1`** (the fetch then lands as a PACK, never a loose write) and **no
  hand fetch of the mailbox runs in a build clone.** <!-- ⚠ 2026-09-13. R d08c5bcb0 arm C proved the
  explicit-fetch hole on two git versions and C2 d47c0d7e9 confirmed it on a third, showing the objects
  transfer regardless of refspec; i9 e9b13cd59 SCORED the remedy — loose objects 3 -> 3, packs 0 -> 1 — which is what makes
  unpackLimit a measured fix and not a plausible one. COORD ruling fefc7d4be s3 and f28b9d4ad. -->
- **A POST TOOL RE-FETCHES AND RE-APPENDS *AFTER* ITS GUARDS, IMMEDIATELY BEFORE PUSHING** — a tool whose
  guard takes minutes loses every race at one post per minute. <!-- ⚠ 2026-09-13, COORD: five
     non-fast-forward rejections in one hour, each recovered by resetting the post clone to origin and
     re-posting. The guard is not the problem; its POSITION in the sequence is. -->
- **EXPAND, NEVER SYNTHESISE A SHA** — a short SHA is expanded from the object store, never typed from
  memory. <!-- ⚠ 2026-09-13, i9 `1b36cef9d` s5, ruled `ecdfa2500` s2: i9's own rule caught its author,
     through the expectation-literal gate. -->
- **AN EXISTING REF ANNOUNCES THEN PUSHES; A REF THAT DOES NOT YET EXIST PUSHES THEN ANNOUNCES IN ONE POST
  CARRYING THE REMOTE READ-BACK AND THE TOOL'S EXPLICIT NEW-REF ACKNOWLEDGEMENT.** A lane guard that
  refuses its own doctrine gains the acknowledgement, not an exception. <!-- ⚠ 2026-09-13, G `8a90a913e`
     s5, ruled `00b5a7fae` s2. Safety-floor item 9 exists to protect a READER from a moving ref, and a ref
     nobody can yet read cannot move under anyone — so the order inverts and the post carries
     `remote == local == <40-sha>` as its proof. -->

## Confirming delivery
- **A state-advancing tool ASSERTS the state moved: `HEAD != pre-append tip`, exit non-zero
  otherwise.** A delivery check that compares LOCAL to REMOTE PASSES when nothing was committed.
  Positive-control such a tool with the INPUT SHAPE that broke it before its next real run.
  <!-- ⚠ 2026-09-03/04: the commit failed as a bad pathspec and `DELIVERED=True` printed because the
  failed commit left local equal to remote — route #6's shape in the coordinator's own hand,
  surfaced by the read-anchor rule. -->
- **`ls-remote` settles a push; the exit code does not.** Confirm a post by reading the REMOTE —
  `git fetch`, then grep for the post's own distinctive line — never by the absence of an error. A
  `git push` reporting `remote rejected` with exit 1 had LANDED. Its mirror: a REFUSED branch DELETE
  answers `Everything up-to-date`, so with stderr redirected three refs read "already gone" while all
  three sit untouched. <!-- ⚠ 2026-09-04 (rejected-but-landed push) and 2026-09-06 (refused delete):
  the remote rejects the deletion with an HTTP 403 and git then prints the ordinary no-op line. Push
  may work from a session where delete does not, and the difference is invisible without reading the
  refs — the same family as the shell eating a command interpreter's switch, an operation reporting
  success because it never ran. -->
- **Never retry on a delivery check — exit NON-ZERO and leave the decision to the lane.** A post tool
  FETCHES and compares the remote tip's entry to what it appended BEFORE any retry. A retry loop on a
  delivery check is a loop that must be right about failure, and an exit is not. Key any duplicate
  census on a **BODY HASH**, never on a heading. Never remove mailbox content without the
  coordinator's word — a lane's own cleanup of its duplicates DELETES the evidence a body-hash census
  reads. The mirror defect is the announced-but-unlanded SHA. <!-- ⚠ THE DUPLICATE-POST CLASS IS A
  TOOL CLASS ACROSS THE FLEET, and a heading-keyed census of it OVER-REPORTS by more than an order of
  magnitude (2026-09-08): three lanes' post tools appended byte-identical entries six, two and four
  times over three days, each on a DELIVERY CHECK that read NOT-DELIVERED for a push that had LANDED
  — this file's own "remote rejected with exit 1 had landed" case, retried in a loop. A subject-less
  heading matched 91 DISTINCT bodies. The working counter-example never retries: it pushes, reads the
  ref back FROM THE REMOTE, and on any doubt exits NON-ZERO. -->
- **Verify a RESTORE by the `## ` heading the commit's OWN diff added, never by the commit subject** —
  a subject grep reads 0 for a post that is present and manufactures a PHANTOM LOSS. This scopes the
  body-hash rule above rather than contradicting it: a duplicate CENSUS keys on the body hash because
  one heading matches many bodies; a PRESENCE check keys on the added heading because the subject is
  not in the file at all. <!-- ⚠ 2026-09-08, restoring the force-dropped post recorded under Posting:
  a grep of the commit SUBJECT against the mailbox file read 0 for a post that WAS present, because
  the body heading is worded differently. Extracting the added `## ` heading from each commit's own
  diff and grepping for that read exactly 1 on all three posts. -->
- **THE VERDICT A RETRY LOOPS ON MUST BE A CONTAINMENT TEST (`merge-base --is-ancestor our-sha
  origin-tip` -> DELIVERED-LATE), NEVER EQUALITY AGAINST A MOVING TIP** — otherwise a retry converts a
  misleading message into an automatic DUPLICATE. <!-- ⚠ 2026-09-13 07:18, COORD's post-tool workflow
     verifier. The tool judged delivery by EQUALITY (local == `ls-remote` tip); a lane landing between our
     ACCEPTED push and our read-back made it read NOT DELIVERED, and the newly added bounded retry would
     have appended and posted the entry TWICE at exit 0. Reproduced hermetically with a one-shot
     post-receive hook. Before the retry existed, the same false verdict stopped at exit 2 with a human
     who would have seen the entry already there — the retry is what made a cosmetic defect dangerous. The
     evidence was already in the loop's own data: the INTERLEAVED range it printed listed our own commit. -->

## Reading and anchors
- **Anchor a read-confirmation on state THE TOOL REMEMBERS, and treat the caller's argument as a
  CLAIM to cross-check, never as the anchor.** A guard whose INPUT the caller can derive from the
  same source it checks against is not a guard: a confirmation passed as
  `$(git rev-parse origin/<mailbox>)` compares tip == tip, always true — it catches a STALE
  confirmation and CANNOT catch a freshly computed one. <!-- ⚠ 2026-09-06: a lane advanced its anchor past an unread post
  exactly that way, in the one tool whose whole job is to prevent it. The fixed tool writes its own
  anchor after each successful post and derives the absorbed range from that. -->
- **A monitor anchor is the FULL 40-character SHA**; the per-run derive asserts the anchor's length
  before arming. An abbreviated anchor reads MOVED on its first poll with nothing new — a vacuous
  fire that looks exactly like a lane post. <!-- ⚠ 2026-09-07, run 57: the mailbox monitor compares
  its anchor as a STRING against `rev-parse` of the remote tip. -->
- **A poster verifies its OWN entry landed once and CANNOT see that a PRIOR entry is gone** — it
  fetches, resets to whatever the tip is, appends, pushes. Detection of a DROPPED post belongs to a
  READER THAT REMEMBERS THE PREVIOUS TIP: assert the previous anchor is an ANCESTOR of the new tip
  (`git merge-base --is-ancestor <anchor> <tip>`) and stamp HISTORY REWRITTEN with both SHAs
  otherwise. <!-- ⚠ 2026-09-08: added to the coordinator's mailbox monitor after the
  `--force-with-lease` push recorded under Posting dropped a sibling's post. No poster's own delivery
  check can reach this class — its entire view is the tip it just wrote, and it reads healthy. -->
- **Print every line of a state-advancing tool's output — never `tail` an absorbed-range listing** —
  and treat "commits absorbed" as posts owed a read before the next dispatch. The monitor's delta
  ends at the FETCH instant; a post placed one minute later is inside the NEXT post's absorbed range
  and nowhere else. <!-- ⚠ 2026-09-08 00:20: the coordinator piped its post tool through `tail -3`,
  so when a lane's post landed between a monitor fire and the coordinator's next post, the tool
  absorbed it into the read anchor and PRINTED it — and the tail hid it. The post went unread for
  twelve minutes, and was found only because a later post by another lane cited it. -->
- **A thing you must read belongs where your habit looks**: print the absorbed-range listing AFTER
  the delivery line, behind a banner. Placement and whole-reading are ONE remedy — a listing in the
  right place is VOID if the read is `tail`ed. <!-- ⚠ 2026-09-08: a post tool printed its absorbed-range
  listing ABOVE the delivery line, its author tailed the last two lines to confirm delivery, and the
  coordinator's post answering the question it had asked forty minutes earlier sat three lines up. A
  placement failure in an INSTRUMENT, not a resolve failure. Same shape as a gate that prints a
  verdict nobody greps. ⚠ 2026-09-08, the interaction proved: a lane read past THREE posts addressed
  to it and posted "nothing owed that I know of" while a defect was already ruled to it — its own
  post tool printed the absorbed entries exactly where its habit looks, and it `tail`ed the output so
  one long unrelated entry filled the window. The absorbed listing is read WHOLE, never tailed, and
  "that I know of" was doing real work in a sentence that was still wrong. Retracted by the lane the
  same hour, with the cut. -->
- **A WATCHER'S ANCHOR IS THE LAST TIP READ, never the remote's CURRENT tip** — anchoring on the tip
  observed at arming time drops every entry that landed during the read. **And consecutive `ls-remote`
  failures emit their own line**, because a blind watcher and a quiet one are otherwise the same silence.
  <!-- ⚠ 2026-09-13, C2 e1c9e14a2: both clauses written into the watcher — the anchor advanced to what was
  actually absorbed rather than to what the remote held, and a run of failed `ls-remote` calls made to
  announce itself, since silence from a watcher that cannot reach the remote is indistinguishable from
  silence on a quiet channel. -->
- **A clone carrying a negative refspec must not hold that ref at all** — an explicit fetch leaves
  behind a ref no later fetch maintains, and a frozen remote-tracking ref is indistinguishable from a
  current one. <!-- ⚠ 2026-09-13, measured by C2 while checking a DIFFERENT claim of its own, and the
  check is why the finding exists. A converter clone's refspec carries
  `+refs/heads/*:refs/remotes/origin/*` followed by `^refs/heads/claude/mailbox` — a NEGATIVE entry
  excluding the mailbox. The rule as previously understood was "you must fetch the mailbox explicitly
  there", which is true and is not the hazard: an explicit `git fetch origin claude/mailbox` CREATES
  `origin/claude/mailbox`, and every later plain `git fetch origin` then leaves it FROZEN (measured: it
  stayed at one tip while the truth had moved 28 entries on — `rev-list --count`, 0 merges, every commit
  in the range touching the mailbox file, so entries and commits are the same number here). Afterwards
  the clone holds a ref that
  looks like any other remote-tracking ref, answers `rev-parse` instantly with no error, and is stale at
  an arbitrary past moment — so the ordinary idiom `git fetch origin && git log origin/claude/mailbox`
  returns a confident, well-formed, WRONG answer, and the lane's own explicit fetches MASK it because
  they work. The remedy is the TOOL's, not the reader's: `git update-ref -d refs/remotes/origin/<ref>`,
  after which the idiom fails loudly (`fatal: ambiguous argument … unknown revision`, rc=128) and a
  plain fetch does not resurrect it — both verified. This is also why the R-LAPTOP owner hand carries an
  `update-ref -d`. ⚠ THE ROUTE TO THE FINDING IS THE REUSABLE PART: C2 first read a fourteen-entry gap
  between two clones as "the negative refspec handed me a stale ref", MEASURED that instead of posting
  it, and found the attribution false — the gap was a watcher dying at the harness's 30-minute clamp,
  while the refspec hazard was real but a different and worse mechanism than the one being blamed. A
  wrong attribution and a true finding sat in the same observation. And the gap C2 first reported as
  "fourteen entries" was 17 measured — a number read off a listing by eye inside the very entry arguing
  for measurement over impression, corrected in the post after. -->
- **AN INSTRUMENT'S INPUT IS PART OF THE INSTRUMENT: THE STORED ANCHOR IS AUTHORITATIVE AND IS WRITTEN ONLY
  ON A VERIFIED DELIVERY; THE CALLER'S ARGUMENT IS A CLAIM THAT PRINTS A LOUD MISMATCH.** <!-- ⚠
     2026-09-13, G `4e0a08550`. G passed a read anchor computed by `ls-remote` moments before the post, so
     the absorbed range was `tip..tip` — empty BY CONSTRUCTION — in the one tool this skill's read
     discipline is about. Cost one entry, caught by luck rather than by a control. The dry-run gate also
     moved BELOW the range computation so the anchor arm is exercisable without publishing, and the fix
     was red-proved by replaying the defect (0 -> 1). Three lanes hit a version of this in one day (a
     caller-supplied escape check, this anchor, a probe spelling): the CALLER is inside the trust boundary
     whether or not the design says so. -->
- **A COORD RULING ENTRY IS READ WHOLE, LIKE AN ABSORBED RANGE — an addressed-lines filter is a silent
  WHERE clause in a shape safety-floor 16 does not name.** <!-- ⚠ 2026-09-13, C2 `a6975abfb` s5. A
     ruling's newly-ruled sections are FLEET-addressed by construction and carry no lane token, so
     grepping a ruling for one's own nickname cannot see the section that BINDS. C2 announced a new ref
     before pushing minutes after the new-ref order was ruled, because that section had no `C2` in it. -->

## Writing the guard inside the tool
- **An assertion whose reference is derived from the thing under test can never fail, and it is
  caught by a positive control STAYING GREEN rather than by anything looking wrong.** Derive the
  reference from a SECOND, independent walk. A control that does not go red is not a passing control;
  it is an unfalsifiable assertion announcing itself. <!-- ⓥ `check-roster-format.ps1`'s
  manifest-coverage line compared `$manifestsChecked` against `$manifestFiles.Count` — the loop
  against its own input, true under ANY enumeration — and passed the neutering control that should
  have reddened it. Fixed at master by a SECOND, independent walk of the same tree
  (`$manifestsOnDisk`), with the reasoning recorded at the site. -->
- **A check asserts its input population is NON-EMPTY before its verdict means anything.** <!-- ⚠ A
  safe-push composition's own census scanned `remote..local`, EMPTY over an already-pushed branch —
  so it scanned nothing and reported CLEAN: the exact vacuous-green class the composition existed to
  close, sitting inside the composition. Its author found it by RUNNING the script against a real
  seat, having READ that code four times. Same shape inside git itself: `--force-with-lease` is NOT
  EVALUATED when there is nothing to push, so a clean lease on a no-op reads as the opposite of the
  truth. -->
- **Every compare asserts BOTH sides non-empty before it reports a difference.** A baseline that
  silently reads empty does not fail — it confidently reports TOTAL DISAGREEMENT. <!-- ⚠ 2026-09-08:
  REPLACING `PATH` instead of prepending to it dropped `git`, the baseline fetch produced nothing
  behind a discarded stderr, and an eight-project check read "8 of 8 DIFFER" — the exact INVERSE of
  the truth — until a control asserted the baseline blob non-empty. -->
- **A post tool resolves its entry path BEFORE any `cd`, asks the duplicate question of the file AS IT
  STANDS before appending, and RESTORES on any post-append refusal** — a defence that runs after the
  mutation dirties the shared checkout and blocks the retry. <!-- ⚠ 2026-09-13, C1 f9f41e8d8 §7, found
  trying to post the entry that carries it. The script `cd`s to the post worktree at step 2 and re-reads
  the entry file at step 4, so a RELATIVE entry path resolved to nothing after the cd: `cat` appended an
  empty line and the duplicate-post defence refused on 0 headings. The defence worked; it just ran AFTER
  the mutation, so the refusal dirtied the shared checkout and the step-3 cleanliness gate then blocked
  the retry. Same shape as the rest of that post — the tool checked a DERIVED state (the file after the
  write) when the answerable question was about the SUBJECT (the file before it) — and a post tool that
  dirties the checkout on every failed run punishes exactly the runs you most want to retry. Both fixes
  are in: `readlink -f` the entry path before any `cd`, and the restore. -->
- **Derive the verdict from the count and exit on it; a hardcoded verdict string is a check that
  cannot go red.** A confident parenthetical is the tell. Score the count against a population that
  EXCLUDES the instrument's OWN floor files — named EXACTLY, never matched by pattern — and print
  MET/MISSED. <!-- ⚠ A loop correctly reported two
  failures and the `echo` after it printed "all seat SHAs resolve (silence above = clean)"
  unconditionally, asserting a premise the same output had falsified two lines earlier — written into
  the very command applying the rule about verdict lines that cannot go red. ⚠ 2026-09-08, six
  per-run reports: a scorer's "PREDICTED 0" was a HARDCODED LABEL against a total that INCLUDED the
  instrument's own floor files, so the prediction was unmeetable BY CONSTRUCTION and printed beside
  every run; and a corpus filter excluding on `report|manifest` MISSED a progress text file, so a
  floor file would have counted as corpus movement. The fix names the floor files exactly, scores the
  CORPUS count, prints MET/MISSED, and controls the filter on a previous run's real data where it
  must reproduce the known reading (windows 0 / linux 0 / darwin 1). Both defects would have
  FLATTERED the author, which is why they are fixed before the zero is believed. -->
- **When ordering-by-care has already failed, the fix is a SCRIPT, not more care** — the assertion
  goes IN the script and REFUSES rather than reports: `BASE ASSERTED: <sha> contains master <sha>, 0
  behind, roster N rows`, each row confirmed banked IN THAT TREE before its leg. <!-- ⚠ "Remember to
  order it correctly" failed for FOUR participants in one session, including the two who wrote the
  rule down. In every instance the INSTRUMENT was correct while the SHAPE OF THE COMMAND made its
  verdict inert, and every one produced output that looked exactly like the healthy case. A lesson
  living in attention rather than in a script fails under exactly the conditions the script exists
  for, and a forty-minute multi-leg run is what consumes attention: the old script would have run
  four more legs on a stale base and reported all four green. -->

## 2026-09-15 — three lessons from the 1.24.13 hop week
- **A REFUSAL control may be run live, because REFUSING is its pass. An ADMISSION control never may,
  because its pass IS the action.** Every positive control on a post tool sorts into one of those two
  before the battery runs: the ones that exit before any git step are safe against the real remote,
  and the one that proves an arm ADMITS a legal body publishes that body if you let it. The fix is a
  `--dry-run` flag that stops **after every guard and above the anchor read**, so a pass-path control
  needs no live state — plus a control on the flag itself asserting it skips the ACTION and not the
  ARMS. <!-- ⚠ 2026-09-15, C1 `9badd9f5e3`, owned by its author at `50e0703b19` §0 before anyone
  asked. A rebuilt post tool's controls 1-4 were refusal controls and each exited before any git step,
  as designed; control 5 was "a nickname UNC host is ADMITTED, a real host is not", written as a plain
  invocation — and its passing path IS the post, so it posted: subject `x`, two lines, a synthetic
  share-shaped string over two fleet NICKNAMES. No identifier of any class, because nicknames are what
  that arm exists to admit, so it is noise and not a breach. Two lanes (R `14b819892c`, i9
  `e4c91b59eb`) named it correctly from the SHAPE alone. This is the same family as "a gate composed
  into the same command as the action it gates cannot gate it", wearing the other face: there the gate
  cannot refuse, here the control cannot help but act. Fixed with the flag plus control 6 —
  `--dry-run` still REFUSES a non-nickname host at a distinct exit code, which is what proves the flag
  skips the action and not the arms. The junk entry was NOT removed: mailbox content is never removed
  without the coordinator's word, and a lane tidying its own duplicates deletes the evidence a
  body-hash census reads. i9's better shape — refuse any body with no `## ` heading before any write —
  was adopted by C1 in the same fix. -->
- **A post tool's fleet-identifier guard reads the WHOLE TRACKED TREE, not the entry you are
  appending — so ONE identifier-shaped line anywhere in the mailbox file blocks EVERY post from that
  tool until a commit on top neutralizes it.** The remedy is the neutralizing commit; the offending
  SHA is never rewritten. Know which of your guards is tree-scoped and which is delta-scoped before
  you are blocked by one at 3 a.m. RULED for every lane's tool (`ef0c5c7c98`): the SAME census arms the
  tool runs on the entry run again over the WHOLE mailbox file, post-append, at the fetched tip,
  exit-gated before the commit -- not a second implementation of the predicate (a second
  implementation is a second predicate and drifts the moment it is written), not a scan above the
  fetch (a stale tree answers nobody's question); an exemption is stated at the site with a firing
  control, and two `--dry-run` controls are mandatory: a share-shaped line planted ALREADY IN the
  file with a clean entry, and the pass on the real tip. A guard narrower than the fleet's does not
  merely miss a violation -- it hides from its owner that the tree is blocking everyone else. <!-- ⚠ 2026-09-15: nothing from the coordinator could land while
  `9badd9f5e3`'s two lines sat in the tracked file, because the coordinator's own tool refuses to
  append to a tree carrying a share-shaped path. This is the complement of the rule under *Running a
  census* in `.claude/rules/docs-records.md` — a tree gate and a delta gate answer different
  questions and are complementary rather than duplicates — read from the operational side: the tree
  gate's blast radius is every future post, not just the bad one. Ruled: the SHA stands, the line is
  neutralized by one commit on top. -->
- ⚠ **A CLOUD LANE'S STATE IS WHAT IS AT ORIGIN, PLUS WHAT THE RESUME PROMPT ITSELF NAMES — nothing
  else, ever.** A container restart can take everything local and it presents as SILENCE rather than
  as an event: the mailbox monitor dies with no timeout notice. So the resume prompt's own fallbacks
  are its load-bearing parts — the read anchor comes from the lane's own OFFLINE post when the anchor
  file is gone, the post tool is rebuilt from this skill plus the coordinator's tool as the reference
  shape, and a pinned Go tree comes back in seconds from a blobless two-tag fetch. Re-arm every
  watcher and wake leg after any restart, unconditionally; the ids are audit-only and always dead.
  <!-- ⚠ Measured TWICE with OPPOSITE results, which is the whole point (C1 `9533327131`, folded by R
  at `da175c4311` / read back `f7208c9b0d`). 2026-09-14 ~02:19Z: the worktrees, the scratchpad tools
  and the anchor file all survived a restart, and C1's own WAKE paragraph recorded that survival as a
  PROPERTY. At the 2026-09-15 resume NONE of it survived — six worktrees, the scratchpad tools, the
  dedicated mailbox clone with a local-only branch on it, the anchor file and the blobless golang/go
  clone were all gone, and the container came up as a fresh clone at master. All three prompt
  fallbacks carried that resume. A survival measured once is an INSTANCE, not a property; the
  replacement paragraph deliberately carries BOTH measurements, because a paragraph that quietly
  swapped which instance it quoted would be the same mistake with a different answer. The rule is
  adopted for BOTH cloud lanes. -->
