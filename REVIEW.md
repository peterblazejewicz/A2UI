# Review: `A2Ui.Core` and `AgUi.Protocol`

The two libraries are a **strong protocol core but an incomplete SDK layer**. `A2Ui.Core` is mostly faithful to A2UI v0.9, `AgUi.Protocol` broadly matches the published AG-UI event model, but the main weaknesses are **scope-aware binding semantics**, **partial schema coverage and validation**, **one stale AG-UI request-shape gap**, and **limited .NET consumption ergonomics**.

## Executive summary

The implementation is generally well engineered: message envelopes are modeled cleanly, event coverage is strong, data-model mutation logic is careful, and diagnostics are better than average. The main concern is not excessive strictness in runtime behavior; it is that the public surface is still **wire-model-first** rather than **.NET-SDK-first**.

## Version and conformance context

| Source | Version posture | Evidence | Note |
| --- | --- | --- | --- |
| A2UI spec | **v0.9** | `specification\v0_9\docs\a2ui_protocol.md:173-249`, `specification\v0_9\json\server_to_client.json:14-129`, `client_to_server.json:8-103` | Authoritative target |
| `A2Ui.Core` | **v0.9-pure** | `A2Ui.Core\Messages\A2UiMessage.cs:11-29`, `ClientToServerMessage.cs:12-22` | Correct |
| Lit shell | **still v0.8-shaped on client actions** | `samples\client\lit\shell\client.ts:47`, `app.ts:466-499` | External interop risk |
| Python `restaurant_finder` | **mixed / incomplete migration** | `samples\agent\adk\restaurant_finder\agent_executor.py:75-90` | Looks for `userAction` and `actionName` |
| Python `contact_lookup` | **mixed bridge code** | `samples\agent\adk\contact_lookup\agent_executor.py:72-90` | Looks for `userAction`, inner field `name` |

## What is already strong

1. **Envelope modeling is good.** `A2UiMessage.Validate()` correctly enforces v0.9 plus exactly one top-level operation (`A2UiMessage.cs:31-60`).
2. **AG-UI event coverage is good.** `BaseEvent` tracks the published event set (`AgUi.Protocol\Events\BaseEvent.cs:10-38`), and `EventTypeCoverageTests` are a strong anti-drift guard (`AgUi.Protocol.Tests\Events\EventTypeCoverageTests.cs:11-104`).
3. **Data-model mutation is carefully implemented.** `DataModel` handles RFC 6901 escaping, array indices, deletes, and traverse-through-scalar rejection well (`A2Ui.Core\Surfaces\DataModel.cs:29-76`, `104-158`, `216-265`).
4. **Separating `AgUi.Protocol` from `A2Ui.Core` is the right architectural split.** AG-UI and A2UI are adjacent protocols, not the same protocol.

## Findings

| Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- |
| **High** | `A2Ui.Core` | **Relative-path / child-scope evaluation is not modeled in the core data API.** The spec explicitly allows relative paths inside template scopes; `DataModel.Resolve()` is root-only. | Spec: `a2ui_protocol.md:391-417`, example `434-458`; code: `DataModel.cs:170-194` | Add a scope-aware API that can resolve against an evaluation base path or child context. |
| **High** | `AgUi.Protocol` | **`RunAgentInput` is behind the current AG-UI shape because it omits `parentRunId`.** | AG-UI docs: `docs/sdk/js/core/types.mdx` (`threadId`, `runId`, `parentRunId`); local code: `RunAgentInput.cs:10-39` | Add `ParentRunId` and a round-trip test. |
| **Medium** | `A2Ui.Core` | **Some valid v0.9 dynamic fields were flattened to plain strings.** That loses binding and function-call scenarios that the schema allows. | `TabDefinition.cs:6-14` vs `basic_catalog.json:417-420`; `ChoiceOption.cs:6-14` vs `basic_catalog.json:638-641` | Use `DynamicValue` or dynamic-string equivalents for schema-dynamic fields. |
| **Medium** | `A2Ui.Core` | **Validation is envelope-level, not schema-level, and some invalid shapes degrade silently.** | `server_to_client.json:63-67`; `ComponentAction.cs:6-15` vs `common_types.json:271-312`; `DynamicValueConverter.cs:10-19,22-37`; `ChildListConverter.cs:8-31` | Add explicit semantic or schema validation and prefer diagnostics over null-collapse. |
| **Medium** | `A2Ui.Core` | **Runtime processing is lenient where the spec uses MUST.** Duplicate creates, unknown surfaces, missing root, and malformed model writes are warnings or no-ops. | `SurfaceManager.cs:223-250`, `255-259`, `270-281`; tests: `TelemetryScenarioTests.cs:187-252`; logs: `SurfaceManagerLog.cs:43-82` | Keep lenient behavior if desired, but expose it as a policy or option instead of baking it in. |
| **Medium** | .NET ergonomics | **The libraries are hard to consume in typical .NET host applications.** No DI registration helpers, no options surface, and low-level constructors only. | `SurfaceManager.cs:26-29`, `ToolCallArgsAccumulator.cs:29-32`, `SseEventParser.cs:14-18`; no `IServiceCollection` extensions present | Add `AddA2UiCore()` and `AddAgUiProtocol()`, options classes, and a few convenience helpers or builders. |
| **Low** | `A2Ui.Core` | **Spec-core and project-specific extensions are mixed together.** `Table` and `Surface` are not in the v0.9 basic catalog but are part of the base polymorphic model. | `A2UiComponent.cs:29-30`; spec component list in `a2ui_protocol.md:663-683` | Move extensions to a separate namespace or assembly, or document them explicitly as non-core catalog extensions. |
| **Low** | `AgUi.Protocol` | **A few event payloads are broader than the published AG-UI docs.** This may be intentional, but it should be explicit. | AG-UI docs show `ToolCallResultEvent.content: string`; local code uses `JsonElement` in `ToolCallResultEvent.cs:17-19` | Either align to the published docs or document the local superset behavior. |

## Commentary on the main design question

### Are you focusing too much on architectural strictness?

**No, not in runtime behavior.** If anything, `SurfaceManager` is **more forgiving than the spec**: duplicate creates, updates for unknown surfaces, missing `root`, and malformed data-model writes are handled as warnings or ignored rather than hard failures.

The more important issue is different: the code is **too protocol-shaped and too low-level for a polished .NET SDK**. The models are fine, but there is not enough SDK layer on top of them.

### Are the APIs idiomatic for .NET?

**Partly.** Internally, yes: records, nullable awareness, source-generated logging, explicit thread-safety notes, and focused types are all good. Public consumption, no: there is very little DI, options, or extensibility surface, and callers are forced down a `JsonElement`-heavy, manual-construction path.

### Are the names aligned with .NET and C# practice?

**Mostly yes for protocol models.** Names like `A2UiMessage`, `CreateSurface`, `UpdateDataModel`, and `RunAgentInput` are acceptable because these are wire and protocol types. I would **not** rename `AgUi.Protocol` to `A2Ui.*`; the split is conceptually correct.

The naming issue is not protocol terminology. The issue is that the packages stop at the protocol layer and do not provide enough **.NET-friendly entry points** around it.

## Recommendations in priority order

1. **Add scope-aware binding support to `A2Ui.Core`.** This is the most important real protocol gap.
2. **Add `ParentRunId` to `AgUi.Protocol.RunAgentInput`.** This is the clearest AG-UI conformance miss.
3. **Do a targeted schema audit for dynamic fields.** At minimum fix `TabDefinition.Title` and `ChoiceOption.Label`.
4. **Add explicit semantic validation APIs.** Especially for `ComponentAction`, `components` minimum shape, and converter failures.
5. **Introduce host-friendly .NET entry points.** Add `IServiceCollection` extensions, options classes, and a small set of convenience helpers or builders.
6. **Separate or clearly document extensions.** Keep v0.9 spec-core types distinct from renderer or app-specific additions.
7. **Do not push v0.8 compatibility into `A2Ui.Core`.** Keep the core v0.9-pure and put mixed-version handling into adapters or sample-specific serializers.

## Bottom line

**Good protocol core, incomplete SDK layer.** The implementation quality is real, and most of the important modeling decisions are sound. The next step is not to rewrite the architecture; it is to add the missing semantic coverage and the missing .NET usability layer around the existing core.
