# Duplicates go to the sorter's own Trash

Status: accepted (2026-10-21)

The sorter used to promise it never deletes anything. Dedupe needs to remove byte-identical files, and the user wants that to run in every normal run and be undoable by "Undo last run".

**Decision:** duplicates are moved to `%LOCALAPPDATA%\clean-my-downloads\Trash\<runId>\` and purged after 30 days. The only permanent deletion is that purge, and it only touches the sorter's own Trash.

**Why not the Windows Recycle Bin:** code has no dependable way to restore a specific item, and Undo would fail if the Bin was emptied. A plain move into a folder we control keeps Undo reliable and the move log the single record.

**Why not permanent delete:** unrecoverable, and the check is only name similarity plus a hash; a safety net is worth 30 days of disk space.

**Consequence:** SPEC section 6 "never deletes" is relaxed accordingly. Restoring by hand means opening the Trash folder, not Explorer's Recycle Bin.
