# clean-my-downloads: spec

A personal Windows 11 tool that sorts `C:\Users\ASUS\Downloads` by **subject → topic → type**. Every behaviour below was decided on the [planning map](https://github.com/MatyasMayer/clean-my-downloads/issues/1); each section links the ticket that holds the reasoning. Terms in **bold** are defined in [GLOSSARY.md](GLOSSARY.md).

## 1. Layout

Sorted files stay inside Downloads. ([Where do sorted files end up?](https://github.com/MatyasMayer/clean-my-downloads/issues/2))

```
Downloads\
├── School\
│   └── BI-AG1\        subject folders: code in upper case, flat (no subfolders)
├── Topics\
│   └── costy-taxi\    topic folders, flat
├── Pictures\          type folders at the top level
├── Videos\
└── …
```

- Folder names are English and capitalised.
- A folder is created only when the first file goes into it.

## 2. What a run looks at

([What does a second run touch?](https://github.com/MatyasMayer/clean-my-downloads/issues/10))

- Files at the top level of Downloads.
- **Sorted files** inside the sorter's own folders (`School\`, `Topics\`, type folders): files the move log says the sorter put there, still at that path under the same name. Editing a file in place keeps it a sorted file.
- **Never touched, and never counted as siblings:** files the user moved or renamed (including by renaming a folder), and folders the sorter didn't create (e.g. an extracted zip) along with everything inside them.
- **Always ignored:** in-progress downloads (`.crdownload`, `.part`, `.partial`, `.download`, `.tmp`), `desktop.ini`, hidden or system files. ([What safety net does a sort need?](https://github.com/MatyasMayer/clean-my-downloads/issues/4))

## 3. Classifying a file

Only the file name is used. The first rule that applies wins.

### 3.1 Subject

([What do your subject codes look like?](https://github.com/MatyasMayer/clean-my-downloads/issues/8), [Pattern alone, or checked against a list of real codes?](https://github.com/MatyasMayer/clean-my-downloads/issues/9))

A name containing a FIT subject code matches this pattern (case-insensitive, whole file name):

```regex
(?<![a-z0-9])(anie|ani|qnie|qni|bie|bik|bi|nie|ni|mie|mi)[-_ ]([a-z][a-z0-9]{2}[0-9]?|[0-9][a-z]{2})(?:\.([0-9]{1,2}))?(?![a-z0-9])
```

- **Folder name:** `<PREFIX>-<BODY>` in upper case.
  - The version suffix is dropped: `BI-AG1.21` → `BI-AG1`.
  - The English/part-time letter is dropped from the prefix: `BIE-`, `BIK-` → `BI-`; `NIE-` → `NI-`; `MIE-` → `MI-`; `ANIE-` → `ANI-`; `QNIE-` → `QNI-`.
  - `MI-` stays separate from `NI-`.
- **"Not a subject" list:** a code on the user's list in settings (e.g. `MI-FIT`) is treated as if it weren't a code. No catalogue is used and no network is needed.
- **Bare code:** a name with a whole word equal to the body of exactly one *known* subject goes to that subject (`ag1-notes.pdf` → `School\BI-AG1\`). Known means an existing `School\<CODE>\` folder, or a full code seen in the same run. ([What counts as the same topic?](https://github.com/MatyasMayer/clean-my-downloads/issues/5))

### 3.2 Topic

([What counts as the same topic?](https://github.com/MatyasMayer/clean-my-downloads/issues/5), logic prototype on branch [`prototype/topic-grouping`](https://github.com/MatyasMayer/clean-my-downloads/blob/prototype/topic-grouping/prototypes/topic-grouping.PROTOTYPE.html))

The **stem** is computed as follows:
1. Take the name without its extension.
2. Split it into words at whitespace, `-`, `_`, `.`, `(`, `)`, `[`, `]`, `,`, `+` and lower→upper CamelCase boundaries.
3. Lower-case every word.
4. Skip language tags (`cs`, `cz`, `en`, `sk`, `eng`).
5. Stop at the first number-ish word (`05`, `v2`, `2026`, `3a`) or the first **generic word**.
6. The stem is the words kept so far. A stem shorter than 3 characters is no stem.

Two stems are the **same topic** when one, ignoring separators, equals the other's first *n* words joined. So `costytaxi` = `costy-taxi`, and `linear-algebra` matches `linear-algebra-exam`. But `linear-regression` doesn't match `linear-algebra`, and `cat` doesn't match `catalog`.

- A new topic needs at least 2 files. It is named after its shortest stem, in that stem's most common spelling.
- An **existing** `Topics\<name>\` folder takes in any new file whose stem matches it, even alone. Topic folders are never renamed.
- Default generic words: task, ukol/úkol, cviceni/cvičení, cv, lecture, prednaska/přednáška, slides, final, copy, kopie, zadani/zadání, reseni/řešení, solution, homework, hw, notes, poznamky/poznámky, img, image, screenshot, snimek/snímek, received, download, file, new, untitled, document, dokument, scan, part, win, x64, setup, installer. The user extends the list in settings.
- Expected results: two `faktura-2026-09`/`-10` invoices form a topic. Screenshots, `IMG_…` photos and `zadani (1)` copies have no stem, so they are loose.

### 3.3 Type (loose files)

([Which type folders exist, and which extensions go in each?](https://github.com/MatyasMayer/clean-my-downloads/issues/7))

| Folder | Extensions (case-insensitive) |
|---|---|
| `Pictures` | png, jpg, jpeg, gif, webp, heic, bmp, svg |
| `Videos` | mp4, mov, mkv, avi, webm |
| `Music` | mp3, wav, flac, m4a, ogg |
| `Documents` | pdf, doc, docx, odt, rtf, txt, md, ppt, pptx, odp, xls, xlsx, ods, csv |
| `Archives` | zip, rar, 7z, tar, gz |
| `Installers` | exe, msi |
| `OpenPonk` | opp |
| `Other` | anything else, including no extension |

The mapping is editable in settings.

## 4. When a file moves

([When does the sorter run?](https://github.com/MatyasMayer/clean-my-downloads/issues/3), [What happens to a loose file?](https://github.com/MatyasMayer/clean-my-downloads/issues/6))

Time is measured from the **later of the created and modified times**.

| File | Moves when |
|---|---|
| Belongs to a subject or topic | **settled**: not in use and ≥ 3 h (`.opp`: ≥ 24 h) |
| Loose, has a stem | not in use and ≥ 1 day (also for `.opp`) |
| Loose, no stem | settled |

- A file in use is skipped and retried next run.
- `.opp` gets 24 h because OpenPonk's file lock is unreliable.
- A waiting file is left exactly as it is: no renaming and no staging folder.

## 5. Re-sorting

([What does a second run touch?](https://github.com/MatyasMayer/clean-my-downloads/issues/10))

- **Pull-back:** a sorted file in a type folder that gains a sibling moves into the new `Topics\<stem>\` together with it.
- **Topic → subject:** when a topic's bare code becomes a known subject, the sorted files in that topic move to `School\<CODE>\`.
- **Empty folders:** a sorter-created folder left empty is removed. That is the only deletion the sorter ever performs, and it is never a file.

## 6. Safety

([What safety net does a sort need?](https://github.com/MatyasMayer/clean-my-downloads/issues/4))

- **Move only:** the sorter never overwrites or edits a file. It deletes only by moving duplicates to its own Trash (section 6.1) and purging that Trash after 30 days. ([ADR-0001](docs/adr/0001-duplicates-go-to-sorter-trash.md))
- **Collision, different content:** the moved file gets a Windows-style suffix, `zadani (2).pdf`.
- **Collision, byte-identical content:** the incoming file is a **duplicate** and goes to the Trash (section 6.1).
- **A failed move** (in use, permission denied, disk error) is skipped and logged, the run carries on, and the move is retried next run.
- **The move log** is in `%LOCALAPPDATA%\clean-my-downloads\`. It's human-readable, one line per move, and also records skips, failures and duplicates:
  `2026-10-21 14:00  Downloads\bi-ag1-cv03.pdf → Downloads\School\BI-AG1\bi-ag1-cv03.pdf`
- **Undo last run** moves everything from the most recent run back, including files it sent to the Trash. It skips and reports files moved or deleted since then. Older runs can be undone by hand from the log.
- **Preview sort** lists what would move, what would be recycled (*would recycle X, keeping Y*), and which files are *waiting until <date>*, without changing anything.
- Normal runs never ask for confirmation.

### 6.1 Duplicates

([ADR-0001](docs/adr/0001-duplicates-go-to-sorter-trash.md))

Dedupe is a step of every **run**, before any file is moved. It is on unless `dedupe` is `false` in settings.

- **Candidates:** two files are compared only when they have the same extension and either
  - the same **stem** (topic and loose files), or
  - the same name apart from a copy suffix: Windows `(1)`, browser `-1`, or ` - Copy`. This is the only rule for files with a subject code, which have no stem.
- **Where it looks:** the same files as section 2 (top-level Downloads and **sorted files**). Both the file to recycle and the file to keep must be **settled**; otherwise the pair waits for a later run.
- **Match test:** equal size, then equal SHA-256 of the contents. A name alone never makes a duplicate. Empty (0-byte) files are never duplicates.
- **Survivor:** among identical files, keep the one in a subject or topic folder; then the oldest by created time; then the shorter name. With three or more copies, all but the survivor are recycled.
- **Recycling:** a duplicate is moved to `%LOCALAPPDATA%\clean-my-downloads\Trash\<runId>\`, keeping its name (suffixed on a clash inside the Trash). Trash runs older than 30 days are purged at the start of a run; that is the only permanent deletion the sorter does.
- **Log:** `2026-10-21 14:00  RECYCLED  Downloads\zadani (1).pdf  (same content as Downloads\School\BI-AG1\zadani.pdf)`. It replaces the old `DUPLICATE` line.
- **Undo:** Undo last run moves recycled files back to their original path, with the usual collision rules (suffix if the content differs, skip if identical).

## 7. Runs and notifications

([When does the sorter run?](https://github.com/MatyasMayer/clean-my-downloads/issues/3))

- **Hourly** via Task Scheduler. Missed runs are caught up as soon as possible, and runs happen on battery too. The sorter starts, sorts and exits, so nothing stays resident.
- **Manual run:** the **Sort now** shortcut starts the same run.
- **No window ever.** A Windows notification appears only when a run moved or recycled 3 or more files in total, or when the settings file is invalid.

## 8. Settings

([What is the sorter built with?](https://github.com/MatyasMayer/clean-my-downloads/issues/11))

`%LOCALAPPDATA%\clean-my-downloads\settings.json` holds:
- the generic-word list,
- the language tags,
- the extension → type-folder mapping,
- the "not a subject" list,
- the wait times (3 h, 24 h for `.opp`, 1 day for loose files),
- `dedupe` (default `true`),
- the number of days the Trash is kept (default 30).

Install writes the defaults above. If the file is invalid, the run is skipped and a notification says what's wrong; the sorter never silently falls back to defaults.

## 9. Build and install

([What is the sorter built with?](https://github.com/MatyasMayer/clean-my-downloads/issues/11))

- **Build:** C# on **.NET 10** (LTS), as a `WinExe` (no console window) with native Windows notifications. It is framework-dependent on the installed .NET 10 runtime.
  - Dev setup: `winget install Microsoft.DotNet.SDK.10`.
- **Install script:**
  - copies the sorter to `%LOCALAPPDATA%\clean-my-downloads\`,
  - registers the hourly scheduled task,
  - writes default settings,
  - creates five Start-menu shortcuts: **Sort now**, **Preview sort**, **Undo last run**, **Show sort log**, **Edit settings** (opens settings in Notepad).
- **Uninstall script:** removes all of the above, but never the Downloads folders or the move log.

## Out of scope

- Classifying by file contents or AI: only file names are used.
- Other operating systems, other users, folders other than Downloads.
