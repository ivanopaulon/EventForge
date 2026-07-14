# FluentValidation Generator

A one-off / repeatable CLI tool that generates skeleton [FluentValidation](https://docs.fluentvalidation.net/)
validator classes for `Prym.DTOs` request DTOs, translating the `DataAnnotations` attributes already present
on each DTO into the equivalent FluentValidation rules.

## Why

`EventForge.Server` registers FluentValidation globally (`FluentValidationFilter` +
`AddValidatorsFromAssemblyContaining<Program>()`), but for a long time only 2 DTOs had a validator
(`LoginRequestDto`, `ChangePasswordRequestDto`). This tool bootstraps a validator for every `[FromBody]`
DTO used by a controller action, based on the `DataAnnotations` attributes already on the DTO
(`[Required]`, `[MaxLength]`, `[StringLength]`, `[MinLength]`, `[Range]`, `[EmailAddress]`,
`[RegularExpression]`, `[Compare]`). DataAnnotations are **not removed** — they stay for OpenAPI/Swagger
schema generation. FluentValidation intercepts requests first via the global filter.

## Usage

1. Build (or update) a manifest JSON mapping DTO type name -> path (relative to the repo root) of the
   `.cs` file where the type is declared, e.g.:

   ```json
   {
     "CreateBrandDto": "Prym.DTOs/Products/CreateBrandDto.cs",
     "UpdateTeamDto": "Prym.DTOs/Teams/TeamDtos.cs"
   }
   ```

   `scripts/FluentValidationGenerator/dto-manifest.json` contains the manifest used for the initial
   rollout (all `[FromBody]` DTOs found in `EventForge.Server/Controllers/**/*.cs`, excluding
   `LoginRequestDto`/`ChangePasswordRequestDto` which already had hand-written validators, and excluding
   the `Controllers/BoldReports/` viewer/designer proxy endpoints which only accept a raw
   `Dictionary<string, object>`).

2. Run the tool:

   ```bash
   dotnet run --project scripts/FluentValidationGenerator -- <repoRoot> <manifestJsonPath>
   ```

   Example:

   ```bash
   dotnet run --project scripts/FluentValidationGenerator -- . scripts/FluentValidationGenerator/dto-manifest.json
   ```

3. The tool writes `EventForge.Server/Validators/<SameSubfolderAsInPrym.DTOs>/<DtoName>Validator.cs` for
   every DTO that has at least one attribute it can map to a FluentValidation rule. DTOs with no mappable
   attributes are skipped and logged at the end of the run ("DTO senza regole di validazione rilevate").

4. Attributes it cannot map (custom project attributes, business-rule attributes, etc.) are preserved as
   a `// TODO-REVIEW: attributo/i non mappato/i automaticamente: [AttributeName]` comment above the
   generated `RuleFor` (or as a standalone comment if the property has no other mappable attribute) —
   review these by hand and either add the equivalent FluentValidation rule or leave an explicit note if
   the rule requires a business decision.

   Purely informational/metadata attributes (`[Display]`, `[Timestamp]`, `[Obsolete]`, `[JsonPropertyName]`,
   `[JsonIgnore]`, `[DataType]`, `[Key]`, `[Column]`, `[NotMapped]`, `[DefaultValue]`, `[Description]`, ...)
   are silently ignored since they carry no validation semantics.

## Mapping table

| DataAnnotation | FluentValidation rule | Notes |
|---|---|---|
| `[Required]` on `string` | `.NotEmpty()` | |
| `[Required]` on nullable value type (`Guid?`, `int?`, ...) | `.NotEmpty()` | |
| `[Required]` on a known non-nullable scalar value type (`Guid`, `int`, `bool`, `decimal`, `DateTime`, ...) | *(no rule — the type itself guarantees presence)* | |
| `[Required]` on an `enum` | `.IsInEnum()` | `[Required]` alone validates nothing for a non-nullable enum (it can never be `null`); `.IsInEnum()` is the real, useful check — it catches an out-of-range integer silently cast to the enum by the JSON binder, which neither `[Required]` nor its absence ever intercepted. The tool recognizes enum types by scanning every `enum` declaration under `Prym.DTOs` up front. |
| `[Required]` on a nested complex type (a class/record property, not a collection) | `.NotNull()`, plus `.SetValidator(new <Type>Validator())` if that nested type is itself in the manifest | Composes the nested type's own generated validator instead of duplicating its rules in the parent (see FluentValidation docs, "Collections"/"Creating your first validator"). If the nested type has no validator of its own in the manifest, only `.NotNull()` is generated. |
| `[MaxLength(n)]` / `[StringLength(n)]` | `.MaximumLength(n)` | On collection properties, becomes a `.Must(...)` count check instead |
| `[StringLength(n, MinimumLength = m)]` | `.MinimumLength(m).MaximumLength(n)` | |
| `[MinLength(n)]` | `.MinimumLength(n)` | On collection properties, becomes a `.Must(...)` count check instead |
| `[Range(min, max)]` | `.InclusiveBetween(min, max)` | Bounds are cast to the property's numeric type to avoid `double`/`decimal` literal conversion errors (e.g. `double.MaxValue` on a `decimal` property becomes `decimal.MaxValue`) |
| `[EmailAddress]` | `.EmailAddress()` | |
| `[RegularExpression(pattern)]` | `.Matches(pattern)` | If the pattern argument is a `const` field reference instead of a literal, it is qualified with the DTO's own type name |
| `[Compare(nameof(Other))]` | `.Equal(x => x.Other)` | |
| anything else | *(not generated — `TODO-REVIEW` comment)* | |

## Scope limitations (by design)

- Only DTOs used as `[FromBody]` parameters on `HttpPost`/`HttpPut`/`HttpPatch` controller actions are in
  scope — response-only DTOs don't need input validation. The one exception: a complex type that is
  never itself `[FromBody]` but is referenced by a `[Required]` property of an in-scope DTO (e.g.
  `NotificationPayloadDto` nested inside `CreateNotificationDto`) should also be added to the manifest so
  the parent's `.SetValidator(...)` composition has a validator to point at.
- No validator generated by this tool (or added manually as part of the rollout) queries the database
  (e.g. uniqueness checks). Async validators with DB side effects are a separate, larger architectural
  change and are out of scope here.
- Generated rules are purely structural (length/range/format/required/cross-field-within-the-same-DTO).

## Regenerating / extending

Keep this tool around for future DTOs: add the new DTO to a manifest JSON (or reuse/extend
`dto-manifest.json`) and re-run the generator to get a validator skeleton instead of writing one from
scratch. Re-running on a DTO that already has attributes fully covered is safe, but note the tool
**overwrites** any existing generated file at the target path — don't run it against a validator you've
already hand-edited unless you intend to regenerate it from the DTO's current DataAnnotations and re-apply
your manual review afterwards.
