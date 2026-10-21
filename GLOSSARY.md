# Glossary

**Subject**
A school course, identified by a **subject code** such as `BI-AG1` appearing in a file's name. Files with the same subject code belong together, whatever their type. Variants of one course are one subject: `BI-AG1.21`, `BIE-AG1` and `BIK-AG1` are all `BI-AG1`. A **bare code** (`ag1` without the `BI-`) also counts when exactly one known subject has that code.
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
The broad kind of a file (video, PDF, image, OpenPonk model, …), judged by its extension. A file whose extension matches no type has the type **Other**. Used for sorting when a file belongs to no subject and no topic.
_Avoid_: format, extension (an extension is how the type is detected, not the type itself)

**Loose file**
A file that belongs to no subject and no topic, e.g. a single `costy-taxi_task-05.opp` with no siblings. A loose file waits a while in case a sibling arrives, then is sorted by type. It stops being loose as soon as a sibling turns up, even after it has been sorted by type.

**Sorting order**
How a file's home is chosen: by **subject** if its name has a subject code, otherwise by **topic** if it has siblings, otherwise by **type**.

**Run**
One pass of the sorter over Downloads, whether started on a schedule or by hand.

**Sorted file**
A file the sorter moved that is still where it put it, under the same name. Only sorted files are ever moved again. A file the user has moved or renamed is no longer a sorted file: the sorter leaves it alone for good.
_Avoid_: managed file, tracked file

**Settled file**
A file that is not in use and has not changed for a set time. Only settled files are sorted; unsettled files wait for a later run.

**Duplicate**
A settled file with byte-identical content (same SHA-256) to another settled file of the same extension whose name is similar: the same **stem**, or the same name apart from a copy suffix like `(1)`. Of a set of duplicates the sorter keeps one (the **survivor**) and sends the rest to the **Trash**. Empty files are never duplicates.
_Avoid_: copy, dupe

**Trash**
The sorter's own holding folder in `%LOCALAPPDATA%\clean-my-downloads\Trash\`, where duplicates go instead of being deleted. Undo can restore from it; runs older than 30 days are purged.
_Avoid_: Recycle Bin (that is Windows'; the sorter does not use it)
