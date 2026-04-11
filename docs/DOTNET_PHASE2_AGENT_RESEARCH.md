# Phase 2 .NET Agent — Microsoft Agent Framework Compatibility Research

_Research snapshot. Last verified 2026-04-07 against MS Agent Framework 1.0.0._

Phase 2 of the Restaurant Demo port replaces the Python `restaurant_finder`
ADK agent with a .NET equivalent built on Microsoft Agent Framework + Ollama.
This document is the upfront research that will inform that implementation.
It was extracted from `RESTAURANT_DEMO_PORT_PLAN.md` during the 2026-04-11
docs cleanup so the parent plan could shrink to a status tracker.

**Status:** Research only. Phase 2 has not started. All package versions
should be re-verified at implementation time — preview packages in particular
will have moved.

---

## 1. Microsoft Agent Framework 1.0 — key findings

- **Released:** 2026-04-03 — GA 1.0.0 for core packages; preview for A2A/AG-UI hosting
- **A2A support:** YES — preview packages `Microsoft.Agents.AI.A2A` and `Microsoft.Agents.AI.Hosting.A2A.AspNetCore`
  - Server: `app.MapA2A(agent, "/a2a")` exposes an agent as an A2A endpoint with an agent card
  - Client: A2A client support available
  - `DataPart` conversion resolved (Dec 2025) — `DataPart` for JSON data works
- **AG-UI support:** YES — preview package `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`
  - Emits standard AG-UI SSE events (`RUN_STARTED`, `TEXT_MESSAGE_CONTENT`, `TOOL_CALL_*`, etc.)
  - Compatible with this repo's `AgUi.Protocol` C# SDK
- **A2UI support:** NONE — MS Agent Framework has no awareness of the A2UI UI protocol
- **LLM providers:** Azure OpenAI, OpenAI, Anthropic, Gemini, **Ollama** (local)
- **Target frameworks:** .NET 8.0, 9.0, 10.0

---

## 2. Framework capabilities vs. what we need

| Capability | MS Agent Framework | This repo | Gap? |
|---|---|---|---|
| Agent definition + tools | `AIAgent` + `AIFunctionFactory` | — | No gap |
| A2A server hosting | `MapA2A()` | — | No gap |
| AG-UI SSE streaming | `MapAGUI()` | `AgUi.Protocol` (client-side) | No gap |
| LLM integration (Ollama) | Built-in multi-provider | — | No gap |
| A2UI schema injection into prompts | — | `A2Ui.Core` has message model | **Gap — need prompt builder** |
| A2UI JSON parsing from LLM output | — | — | **Gap — need `A2uiStreamParser` equivalent** |
| A2UI `DataPart` creation | — | `A2Ui.Core.Messages` | Provided by this repo |
| A2UI A2A extension negotiation | — | — | **Gap — need A2A extension helpers** |

---

## 3. Target architecture for the .NET agent

```
Microsoft.Agents.AI                         — agent + tool definitions
Microsoft.Agents.AI.OpenAI (or Ollama)      — LLM provider (Ollama for local dev)
Microsoft.Agents.AI.Hosting.A2A.AspNetCore  — A2A endpoint hosting
A2Ui.Core                                   — A2UI message model, validation
A2Ui.Agent (new — or inline in the sample)  — schema injection, response parsing, extension negotiation
ASP.NET Core                                — HTTP hosting, static files (restaurant images)
```

---

## 4. Three gaps to fill for the .NET agent

1. **A2UI prompt builder (C#).** Equivalent of Python `A2uiSchemaManager.generate_system_prompt()`. Injects component catalog JSON schema + examples into the LLM system prompt. Input: catalog JSON files from `specification/v0_9/`. Output: system-prompt string with embedded schema.

2. **A2UI response parser (C#).** Equivalent of Python `parse_response()` / `A2uiStreamParser`. Extracts A2UI JSON blocks delimited by `<a2ui>...</a2ui>` tags from LLM text output.

3. **A2UI A2A extension helpers (C#).** Equivalent of Python `a2ui.a2a`:
   - `GetA2UiAgentExtension()` — builds extension metadata for the agent card
   - `TryActivateA2UiExtension()` — negotiates the version from client headers
   - `CreateA2UiPart()` — wraps A2UI JSON as an A2A `DataPart` with MIME type `application/json+a2ui`

---

## 5. Risk assessment

- **Core packages (1.0.0 GA):** Stable — safe to depend on.
- **A2A hosting (preview):** API may change — acceptable for samples; pin exact preview versions in `Directory.Packages.props` at implementation time.
- **Ollama integration:** Confirmed working, no API key required for local development.
- **A2UI integration is custom regardless:** The bridge code is ours to maintain in either language.
- **Mitigation for preview packages:** Pin exact versions; document a GA-only fallback subset in case preview breaks.

---

## 6. Blockers before starting Phase 2

Phase 2 is gated on Phase 1 manual verification (the Restaurant Shell round-trip
run against the existing Python agent). Do not start Phase 2 until:

- The 8-step test matrix in `VERIFICATION_STEPS_PLAN.md` passes end-to-end against `restaurant_finder`
- The Phase 1 / Phase 2 telemetry (see `docs/DOTNET_TELEMETRY_REFERENCE.md`) confirms the Shell's `A2A.SendMessage` Activity, `Surface.<Op>` child spans, and log sequence match the Python reference log on the dimensions that matter
