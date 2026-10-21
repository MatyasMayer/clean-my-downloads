# Glossary

**Subject**
A school course, identified by a **subject code** such as `BI-AG1` appearing in a file's name. Files with the same subject code belong together, whatever their type. A **bare code** (`ag1` without the `BI-`) also counts when exactly one known subject has that code.
_Avoid_: course, class

**Topic**
A group of files without a subject code that clearly belong together because they share a **stem**, e.g. `costy-taxi_task-04.opp`, `costy-taxi_task-05.opp` and `costy-taxi_task-05_cs.pdf` all share the stem `costy-taxi`. Two files are in the same topic when one stem starts the other, ignoring separators (`costytaxi` = `costy-taxi`). A topic needs at least two files; one file alone isn't a topic.
_Avoid_: project, group, category

**Stem**
The leading words of a file's name that say what it's about: language tags like `_cs` are skipped, and the stem ends at the first number or **generic word**.
_Avoid_: prefix, base name

**Generic word**
A word that says what kind of file it is rather than what it's about (`task`, `ukol`, `zadani`, `screenshot`, `img`, …). It ends a stem. The user keeps the list and adds to it over time.

**Type**
The broad kind of a file (video, PDF, image, OpenPonk model, …), judged by its extension. Used for sorting when a file belongs to no subject and no topic.
_Avoid_: format, extension (an extension is how the type is detected, not the type itself)

**Loose file**
A file that belongs to no subject and no topic, e.g. a single `costy-taxi_task-05.opp` with no siblings. What happens to loose files is still undecided.

**Sorting order**
How a file's home is chosen: by **subject** if its name has a subject code, otherwise by **topic** if it has siblings, otherwise by **type**.

**Run**
One pass of the sorter over Downloads, whether started on a schedule or by hand.

**Settled file**
A file that is not in use and has not changed for a set time. Only settled files are sorted; unsettled files wait for a later run.
