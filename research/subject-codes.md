# What do CTU FIT subject codes look like?

Research for [#8](https://github.com/MatyasMayer/clean-my-downloads/issues/8) (map: [#1](https://github.com/MatyasMayer/clean-my-downloads/issues/1)). Researched 2026-10-20.

## Short answer

A FIT subject code is **`<PREFIX>-<BODY>`**, optionally followed by **`.<version>`**:

- **PREFIX** names the study level and its variant: `BI` (bachelor), `NI` (master since 2020), `MI` (master before that). A suffix letter `E` means the English version, `K` means part-time. A few smaller programmes have their own prefixes (see below).
- **BODY** is almost always 3 characters, usually letters with an optional digit (`AG1`, `PA2`, `DBS`, `3DT`). It is occasionally 4 (`ACM1`, `SCE1`, `ANGK`) or 2 (`SM`, `IB`).
- **`.NN`** is the version of the study plan or the course, not a semester: `.21` is the 2021 accreditation, `.16` the 2016 master plan, `.1`/`.2`/`.3` small revisions, and `.24`/`.25`/`.26` newer ones. `BI-AG1.21` replaced the older `BI-AG1`, and `BI-MA1.21` replaced `BI-ZMA`.

Filenames seen in practice write the code in **lower case with a hyphen** (`bi-zma-01-uvod.pdf`), or **upper case followed by `_` or `-`** (`MI-MZI_skripta.pdf`, `MI-MZI-otazky_2015-16.pdf`). The code usually sits at the start of the name but can appear anywhere. Some course files carry no prefix at all (`lin-cviceni-1.pdf`, `QAP_01.ipynb`), and no detector can catch those.

**Recommended detection** (case-insensitive, run on the whole filename):

```regex
(?<![a-z0-9])(anie|ani|qnie|qni|bie|bik|bi|nie|ni|mie|mi)[-_ ]([a-z][a-z0-9]{2}[0-9]?|[0-9][a-z]{2})(?:\.([0-9]{1,2}))?(?![a-z0-9])
```

Groups: 1 = prefix, 2 = body, 3 = optional version. Details, tests and known limits are in the last section.

## Sources

| # | Source | Trust |
|---|---|---|
| S1 | FIT Bílá kniha (official course catalogue), all courses: <https://bk.fit.cvut.cz/cz/predmety/predmetyAll.html> (fetched 2026-10-20, 701 distinct codes) | Primary, first-party |
| S2 | The same catalogue as archived on 2018-12-23: <https://web.archive.org/web/20181223152433/https://bk.fit.cvut.cz/cz/predmety/predmetyAll.html> (554 codes) | Primary (archived) |
| S3 | Course detail page for BI-AG1.21: <https://bk.fit.cvut.cz/cz/predmety/00/00/00/00/00/00/06/54/69/p6546906.html> | Primary |
| S4 | ČVUT-wide Bílá kniha: <https://bilakniha.cvut.cz/en/predmet6535606.html> (BI-MA1.21; can be satisfied by substitution with BI-ZMA), <https://bilakniha.cvut.cz/en/predmet1432506.html> (MI-PAA / MIE-PAA), group pages titled "version 2021" with `.21` codes, e.g. <https://bilakniha.cvut.cz/en/skupina30021468-BI-PS-TI.21.html> | Primary |
| S5 | Real files hosted by FIT's Department of Applied Mathematics: <https://kam.fit.cvut.cz/deploy/> (directory listings of `bi-zma/`, `bi-lin/`, `mi-mpi/`, `mi-mzi/`, `bi-qap/`, `bi-vmm/`, …) | Primary (actual files students download) |
| S6 | courses.fit.cvut.cz: course pages live at `https://courses.fit.cvut.cz/<CODE>/` (e.g. `/BI-AG1/`, `/NI-MPI/`, `/MI-PAA/`). The code is used **without** the `.21` suffix. Login is required, so only the URL shape was checked. | Primary (URL shape only) |
| S7 | FEL / FBMI course pages: <https://bilakniha.cvut.cz/en/predmet4680606.html> (`B0B01LAG`), <https://predmety.fbmi.cvut.cz/en/17ambltx> (`17AMBLTX`) | Primary |

## Prefixes

Counts of distinct codes per prefix in the catalogue, now (S1) and in 2018 (S2):

| Prefix | Meaning | Now (S1) | 2018 (S2) |
|---|---|---|---|
| `BI` | Bachelor, Czech | 147 | 166 |
| `BIE` | Bachelor, English | 86 | 59 |
| `BIK` | Bachelor, part-time ("kombinovaná") | 61 | 78 |
| `NI` | Master (2020+ programme), Czech | 162 | – |
| `NIE` | Master, English | 76 | – |
| `MI` | Master, **legacy** (until ~2020) | – | 160 |
| `MIE` | Master, English, legacy | – | 82 |
| `ANI` / `ANIE` | Master "Aplikovaná informatika / Applied Informatics" | 52 / 27 | – |
| `QNI` / `QNIE` | Master "Kvantová informatika / Quantum Informatics" | 21 / 23 | – |
| `UNI` | Master "Učitelství informatiky" (teacher training) | 13 | – |
| `FIT` / `FITE` | Faculty-wide extras (e.g. `FIT-ACM1` "Programovací praktika 1", `FIT-SM1`) | 22 / 6 | – |
| `DD` | Doctoral | 5 | – |
| `FI` | Faculty-wide (e.g. `FI-FIL`) | – | 9 |

The study programmes behind ANI, QNI and UNI are listed on the catalogue's front page (<https://bk.fit.cvut.cz/cz/>). The meanings of `E` (English) and `K` (part-time) are visible from the parallel names: `BI-PA1.21`, `BIE-PA1.21` and `BIK-PA1.21` are the same course. FIT does not publish a document that defines the scheme; the meanings above are inferred from the catalogue. In that sense they are reliable but undocumented.

## Body (the part after the hyphen)

Shapes of the body in all catalogue codes (A = letter, 9 = digit, version suffix kept):

| Shape | Now (S1) | 2018 (S2) | Examples |
|---|---|---|---|
| `AAA` | 390 | 356 | `BI-DBS`, `NI-OSY`, `MI-PAA` |
| `AAA.99` | 177 | 74 | `BI-OSY.21`, `BI-DBS.21` |
| `AA9` | 51 | 52 | `ANI-AM1`, `ANI-PG2` |
| `AA9.99` / `AA9.9` | 35 / – | – / 12 | `BI-AG1.21`, `BI-PA1.21`, `BI-SI1.2` |
| `AAA9` | 26 | 9 | `FIT-ACM1`, `NIE-SCE1` |
| `AA99` | 6 | 6 | `BI-ZS10`, `NI-ZS10` |
| `AAA.9` | 6 | 31 | `BI-ADW.1`, `BI-SQL.1` |
| `AAAA` | 5 | 1 | `BI-ANGK`, `BI-ZIVS`, `NI-SCRS`, `UNI-MTUI` (language, PE and similar) |
| `AA` | 3 | 1 | `BI-SM`, `UNI-IB` |
| `A9A` / `A99` | 1 / – | 6 / 2 | `BI-A2L`, `MI-W20` |
| `9AA(.9)` | 1 | 3 | `BI-3DT.1` |

- Bodies are upper case in the catalogue. Letters are ASCII; no diacritics were found.
- **Version suffix**: now `.21` (202), `.26` (8), `.1` (7), `.24`, `.25`; in 2018 `.16` (74), `.1` (28), `.2` (14), `.3`, `.18`. A suffix of `.21` lines up with group titles reading "version 2021" (S4). It is a curriculum version, not a semester. Successors and predecessors are separate catalogue entries (`BI-AG1` → `BI-AG1.21`; `BI-ZMA` → `BI-MA1.21`, S4).
- **Semesters** use a separate code, `B` + two-digit year + `1` (winter) or `2` (summer), e.g. `B181` = winter 2018/19, as in the directory `kam.fit.cvut.cz/deploy/bi-zma/B181/` (S5). It never contains a hyphen and is not a subject code.

## Wider ČVUT (other faculties)

Other faculties use a different, separator-free scheme. FEL codes look like `B0B01LAG`, `B6B01LAG`, `B0B01LAGA` (S7). FBMI codes start with a department number, like `17AMBLTX` (S7). None of these contain a hyphen, and they cannot be told apart from ordinary words or IDs without a catalogue. **Recommendation: support FIT codes only.**

## How codes appear in real filenames

From actual files on kam.fit.cvut.cz (S5):

- `bi-zma-00-info.pdf`, `bi-zma-01-uvod.pdf`, `bi-zma-13-priklady-Landau.pdf`, `bi-zma-slides.zip`, `bi-zma-zk-ukazka-2020.pdf`: lower case, hyphen, code at the start, followed by `-`.
- `bik-zma-2019-2020-test-1-vzorova.pdf`: a part-time variant in the same folder.
- `mi-mpi-cviceni-03-algebra-3-homomorfismy-handout.pdf`: a legacy MI- code.
- `MI-MZI_skripta.pdf`, `MI-MZI-otazky_2015-16.pdf`: upper case, followed by `_` or `-`.
- `bi-vmm-prednaska.pdf`, `bi-pkm-textbook.pdf`, `bi-ma1-2026-02-08.epub`.
- **No prefix**: `lin-cviceni-1.pdf` (in `bi-lin/`), `QAP_01.ipynb` (in `bi-qap/`). The subject is only in the folder or URL, so filename-only detection misses these.
- No downloaded file was seen with the `.21` suffix in its name. It mostly appears in KOS and the catalogue. courses.fit.cvut.cz URLs use the bare code (S6).
- The user's current Downloads (`costy-taxi_task-04.opp`, `costy-taxi_task-05.opp`, `costy-taxi_task-05_cs.pdf`, `openponk-class-editor-win-v5.1.1.zip`, a few GIFs/JPGs) contain no subject codes.

No source showed the code written without a separator (`bipa1`), so the pattern below does not accept that form. Accepting it would match ordinary words such as `bible`, `nikon` and `bike`.

## Recommended detection pattern

Case-insensitive, applied to the whole filename (extension included is fine):

```regex
(?<![a-z0-9])(anie|ani|qnie|qni|bie|bik|bi|nie|ni|mie|mi)[-_ ]([a-z][a-z0-9]{2}[0-9]?|[0-9][a-z]{2})(?:\.([0-9]{1,2}))?(?![a-z0-9])
```

What each part does:

- `(?<![a-z0-9])` … `(?![a-z0-9])`: the code must not touch other letters or digits. `-`, `_`, space, `.` and the start or end of the name all count as boundaries. Do **not** use `\b` here, because it treats `_` as part of a word and would miss `ukol_bi-pa2`.
- The prefix list is the student-facing FIT prefixes, current and legacy. `FIT`, `FITE`, `DD` and `UNI` are left out on purpose: they are rare for a bachelor or master student, and `fit-`/`uni-` collide with ordinary words. They can be added if the user turns out to need them.
- The separator is `-`, `_` or a space. Each match should be normalised to `PREFIX-BODY`, upper case.
- The body is `[a-z][a-z0-9]{2}[0-9]?` or `[0-9][a-z]{2}`. This covers 1,132 of the 1,137 BI/BIE/BIK/NI/NIE/MI/MIE/ANI(E)/QNI(E) codes in S1+S2. Five are not covered on purpose: `BI-ANGK`, `BI-ANGS`, `BI-ZIVS`, `NI-SCRS` (4-letter all-alpha bodies) and `BI-SM` (2 letters). Leaving them out blocks words like `mi-band` and `bi-fold`.
- `(?:\.([0-9]{1,2}))?` captures an optional version (`.21`, `.1`). Whether the sorter treats `BI-AG1` and `BI-AG1.21`, or `BI-` and `BIE-`, as the same subject is a grouping decision outside this ticket.

**Tested** (Python `re`): every covered code from S1 and S2 is detected in each of these forms: `CODE.pdf`, `code-01-uvod.pdf`, `code_skripta.pdf`, `Prednaska_CODE_2024.pdf`, `BI_PA1_hw.zip`, `BI PA1 cviceni.pdf`.

| Filename | Result |
|---|---|
| `bi-zma-00-info.pdf` | `BI-ZMA` |
| `bik-zma-2019-2020-test-1-vzorova.pdf` | `BIK-ZMA` |
| `mi-mpi-cviceni-00-rozcvicka-handout.pdf` | `MI-MPI` |
| `MI-MZI_skripta.pdf` | `MI-MZI` |
| `BI-PA1.21_progtest.zip` | `BI-PA1.21` |
| `bie-ag1_hw3.pdf` | `BIE-AG1` |
| `NI_MPI cviceni 2.pdf` | `NI-MPI` |
| `ukol_bi-pa2.cpp` | `BI-PA2` |
| `B241_bi-ag1.pdf` | `BI-AG1` |
| `ANI-TPA.pdf` | `ANI-TPA` |
| `costy-taxi_task-05_cs.pdf`, `costy-taxi_task-04.opp` | no match |
| `openponk-class-editor-win-v5.1.1.zip`, `togif-2.gif`, `received_906334184082038.gif` | no match |
| `bible.pdf`, `nikon_D750_manual.pdf`, `bike-map.png`, `binary_search.cpp`, `mini-project.zip` | no match |
| `bi-weekly-report.xlsx`, `Mi-Band-8.apk`, `ni-mh_battery.pdf`, `bi-123.txt` | no match |
| `xbi-pa1.pdf`, `BIPA1_notes.pdf` | no match (glued to a letter / no separator) |
| `B0B01LAG_zkouska.pdf`, `17AMBLTX_lecture01.pdf` | no match (other faculties) |
| `lin-cviceni-1.pdf`, `QAP_01.ipynb` | no match (no code in the name) |

**Known residual false positive**: `mi-`, `ni-` or `bi-` followed by a 3-character word, e.g. `mi-fit.apk` → `MI-FIT`. If that matters, the fix is to check each match against a snapshot of real codes taken from S1 (and S2 for legacy MI-). The catalogue is a static HTML page in which each code is the text of a link, so a snapshot is easy to take. The pattern alone is a good default; the catalogue check removes false positives completely.
