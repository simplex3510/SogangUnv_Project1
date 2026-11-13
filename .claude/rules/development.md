# Development Principles & Code Style

## Development Principles
- If the current development level and code quality are "good enough," move on to the next step
- Settle system/architecture-level decisions first, but decide the fine details of the logic at the time of actual implementation
- Propose actual numerical data (constants, tables, etc.) but do not finalize it; recommend that the user handle testing and balance tuning
- Always write comments for logic, but do not overuse them
- Do not write an excessive amount of code at once; proceed in stages
- Write implementations one file at a time (do not implement multiple systems in a single response)
  - Tightly coupled pairs (e.g., an interface and its only implementation, a struct and its dedicated helper) may be written together after the user approves the file list
- Consider extensibility but avoid over-engineering, and aim for an intuitive, non-abbreviated code style

## Definition of "Good Enough"
A step is good enough to move on when all of the following hold:
- It compiles without errors (confirmed by the user in the Unity Editor)
- The intended behavior has been confirmed by the user through a test or Editor check
- It does not violate confirmed decisions
- Known shortcuts are marked with a `TODO` comment, not left silent

Do not polish beyond this unless the user asks.

## Numerical Data & Constants
- Do not scatter magic numbers in logic. Give every gameplay value a named constant or a named data field.
- Keep tunable values in one place (a constants class or ScriptableObject data, as agreed in the design) so the user can find and adjust them.
- Mark proposed, untested values with `// TODO(tuning)`.
- Present proposed values with a short rationale, and state clearly that they are not final.

## Unity & API Usage
- Target Unity 6.6. If unsure whether an API exists or behaves a certain way in Unity 6.x or a package version, say so instead of guessing, and ask the user to verify in the Editor or documentation.
- Do not use deprecated APIs (e.g., `FindObjectOfType`; use the current replacements).
- Check the package versions in `Packages/manifest.json` (read-only) before assuming a package API.

## Testing
- When designing a new system, propose how to test it together with the design (EditMode test first when the logic does not need the Unity runtime).
- Write tests only when the user asks for them, like any other code.

## Code Style
- Follow C# and Unity conventions for naming and notation
  - PascalCase for types, methods, properties, and public members
  - camelCase for locals and parameters; `_camelCase` for private fields
  - Interfaces start with `I`
- Use non-abbreviated, intention-revealing names (e.g., `attackSpeed`, not `atkSpd`)
- Use `[SerializeField] private` for Inspector-exposed fields instead of public fields
- Keep one primary type per file, with the file name matching the type name
- Write comments in Korean, explaining why and the non-obvious flow rather than restating the code
- Do not overuse comments: no XML doc blocks on simple data types, no per-field comments when the field name already says it, no comments restating the next line. One short line per non-obvious reason is the default; a type with an obvious name gets no comment at all
- Avoid cleverness that hurts readability (dense LINQ chains, nested ternaries, unexplained bit tricks)