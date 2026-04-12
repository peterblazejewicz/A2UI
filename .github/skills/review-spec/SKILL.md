---
name: review-spec
description: Cross-reference A2UI protocol spec with Lit shell and Python agent implementations for a given topic
disable-model-invocation: true
---

Cross-reference these three sources for the topic the user specifies:

1. **Spec**: `specification/v0_9/docs/a2ui_protocol.md` and relevant JSON schemas in `specification/v0_9/json/`
2. **Lit shell**: `samples/client/lit/shell/` — focus on `client.ts`, `app.ts`, and configs
3. **Python agents**: `samples/agent/adk/restaurant_finder/` and `samples/agent/adk/contact_lookup/` — focus on `agent_executor.py`, `agent.py`

For the requested topic:
- Read the relevant sections from all three sources
- Report any discrepancies (naming, format, version differences)
- Note which version (v0.8 vs v0.9) each source uses
- Flag any known gotchas (e.g., `actionName` vs `name` in action wire format)

Output a comparison table when useful.
