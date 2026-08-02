# Eduflex Git Workflow — Command Flow & Lessons Learned

Reference sheet built from a real incident: a Visual Studio UI merge silently
deleted core frontend files during a modify/delete conflict, broke the build,
and took a full command-line recovery to fix. This doc is the corrected,
repeatable flow going forward.

---

## Branch structure

- `main`   — released/stable code. Updated via GitHub PRs.
- `dev`    — integration branch. Feature branches merge into this.
- feature branches — one per piece of work, branched off `dev`,
  e.g. `enrolment-and-finance`, `Build-Feedback-Course-Promo`.

---

## 1. Starting new work

```bash
# Always start from an up-to-date dev — local branches don't
# auto-update from GitHub, so fetch/pull first.
git checkout dev
git pull origin dev

# Create and switch to a new feature branch in one step.
git checkout -b <feature-branch-name>
```

---

## 2. Working & committing on a feature branch

```bash
# Stage everything (or name specific files instead of . if you
# want to be selective).
git add .

# Commit with a clear message.
git commit -m "describe the change"

# First push of a NEW branch needs --set-upstream (or -u) so git
# knows which remote branch to link to. Only needed once per branch.
git push --set-upstream origin <feature-branch-name>

# Every push after that, plain push works:
git push origin <feature-branch-name>
```

---

## 3. Merging a feature branch back into dev

```bash
# Get on dev and make sure it's current first.
git checkout dev
git pull origin dev

# Merge the feature branch in.
git merge <feature-branch-name>

# If it's a clean fast-forward, you'll see "Fast-forward" and you're done.
# If it opens an editor (Vim) asking for a merge commit message, see
# the "Stuck in Vim" section below — or just avoid it entirely with:
#   git commit -m "Merge branch '<feature-branch-name>' into dev"

# Push the updated dev.
git push origin dev
```

---

## 4. Syncing dev after a PR merges into main (GitHub)

**Rule: once a PR merges into `main` on GitHub, `main` is the source of
truth — merge FROM `main`, not from the old feature branch again.**
Re-merging the raw feature branch locally re-runs conflict resolution
from scratch and can silently diverge from what GitHub already resolved.

```bash
# Always fetch first — local main/dev refs are stale until you do.
git fetch origin

# See what's coming in before touching anything.
git log --oneline dev..origin/main
git diff dev origin/main --stat

# Do the actual merge FROM the remote-tracking ref (note the slash,
# not a space — "git merge origin main" is NOT the same command).
git checkout dev
git merge origin/main

# If git reports "Updating X..Y" — that's a fast-forward, no conflicts
# possible, nothing to resolve.
#
# If it reports "Automatic merge failed" — real conflicts. Open each
# file listed under "both modified" in git status, resolve the
# <<<<<<< / ======= / >>>>>>> markers by hand, then:
#   git add <resolved-file>
#   git commit -m "Resolve merge conflicts from origin/main"

# BUILD BEFORE PUSHING. Every time. No exceptions.
cd eduflex-Angular && npm run build
cd ../Eduflex/Eduflex && dotnet build

# Only push once both builds are clean.
git push origin dev
```

---

## 5. Pulling dev changes back into a feature branch

```bash
git checkout <feature-branch-name>
git pull origin <feature-branch-name>   # sync with its own remote first
git merge dev                            # bring dev's changes in
git push origin <feature-branch-name>
```

---

## Golden rules (from what went wrong)

1. **Never trust a UI merge on a large/long-diverged branch without
   building immediately after.** The VS Git UI silently deleted ~10
   actively-imported files during a modify/delete conflict. Nothing
   in the UI clearly flagged it. A `npm run build` right after would
   have caught it in seconds instead of hours later.

2. **`git status` is ground truth — a UI banner is not.** VS showed
   "merge completed with conflicts, resolve and commit" while
   `git status` showed no unmerged paths at all. When they disagree,
   believe `git status`.

3. **Fetch before checking anything against `main`/`dev`.** Local
   branch refs (`main`, `dev`) do not update themselves — only
   `origin/main` / `origin/dev` are current after a `git fetch`.

4. **After a PR merges into `main`, sync FROM `main`, not the old
   feature branch.** The feature branch is stale history at that point.

5. **`git merge origin/main` ≠ `git merge origin main`.** The slash
   matters — one merges the remote-tracking branch, the other tries
   to merge two unrelated refs named "origin" and "main".

6. **A new local branch needs `--set-upstream` on its first push.**
   Expected, not an error — just run the command git suggests.

7. **A hard reset that rewrites a branch's history needs
   `--force-with-lease` to push**, since it's not a fast-forward from
   the remote's point of view. Use `--force-with-lease`, never plain
   `--force`, so a push gets rejected instead of silently clobbering
   someone else's work.

---

## Troubleshooting quick reference

**"File exists" errors on specific files during `git merge` (Windows only)**
NTFS is case-insensitive; a path that differs only by casing between two
commits can make git's delete-then-recreate sequence fail with a
misleading "File exists" error. If it's a pure fast-forward (check for
"Updating X..Y" in the merge output), skip the merge machinery entirely:
```bash
git reset --hard origin/<branch>
```
This is safe ONLY when your branch has zero commits of its own beyond
what's already in the target — confirm with `git log --oneline
origin/<branch>..<branch>` (empty output = safe).

**Stuck in Vim's commit message editor and Esc doesn't work**
```bash
# Try in this order: click into the terminal window first, then:
#   Esc
#   Ctrl+[         (identical signal to Esc)
#   Ctrl+C
# If truly stuck, close the terminal window entirely — the merge's
# file-level work is already done, only the commit is pending. Open a
# fresh terminal and finish it without ever opening the editor:
git status                      # confirms "All conflicts fixed, still merging"
git commit -m "Merge branch 'X' into Y"

**Before deleting anything during cleanup, dry-run first**
```bash
git clean -ndf      # PREVIEW only, deletes nothing — always run this first
git clean -fd        # actually deletes, only after reviewing the preview
```

**Confirming nothing unique is lost before a destructive reset**
```bash
git ls-tree -r origin/main --name-only | findstr <filename>
# If it prints the path, that file is safely preserved on the target
# branch/commit and safe to discard locally.
```
