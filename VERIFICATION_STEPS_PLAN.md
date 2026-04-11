# Restaurant Shell — Functional Verification Plan

**Companion to:** `RESTAURANT_DEMO_PORT_PLAN.md`
**Scope:** Phase 1 functional verification of `samples/client/avalonia/Shell/` against the Python `restaurant_finder` ADK agent.
**Branch:** `feature/restaurant-demo-shell`
**Created:** 2026-04-10
**Status update 2026-04-11:** Phase 1 + Phase 2 telemetry is now shipped
(`docs/DOTNET_TELEMETRY_REFERENCE.md`). The verification run below now has a
structured-log contract to compare against `sample-restaurant-find-log.txt`;
look for the `RequestSummary` Information line per round-trip, the
`Surface.CreateSurface`/`UpdateComponents`/`UpdateDataModel` spans, and the
expected `SurfaceManagerLog` EventIds (9, 1, 3, 4, 8). Any warning-level entry
(EventIds 5/6/7/10 in `SurfaceManagerLog`, 2 in `CatalogRegistryLog`, 12 in
`RendererLog`, or 4/5/9 in `A2AAgentClientLog`) is a failed verification.

---

## 1. Purpose

Prove that the Avalonia Shell round-trips correctly with a real A2UI agent over A2A/HTTP — user query in, multi-surface UI out, user action in, new surface out. Do this with two independent LLM backends so we can tell a *Shell bug* from a *model-capability limitation*:

- **Run 1:** Anthropic Claude Sonnet 4.6 — low-friction baseline. If this fails, the Shell has a bug.
- **Run 2:** Gemma 4 31B on local DGX Spark via Ollama — validates the fully-local path. If Run 1 passes and Run 2 fails, the problem is model-side (prompt adherence, tool-call format, schema conformance).

Nemotron 3 Super 120B is explicitly **deferred** — see Section 10 for rationale.

---

## 2. Scope

**In scope:**
- Surface creation, update, and replacement (`createSurface`, `updateComponents`, `updateDataModel`)
- Two-step action round-trip (restaurant list → booking form → confirmation card)
- `DateTimeInput` with both `enableDate` and `enableTime` active
- v0.8 `userAction` outbound envelope reaching the Python agent's parser correctly
- `SurfaceManager.Clear()` between prompts and between actions

**Out of scope (for this verification pass):**
- .NET agent (Phase 2) — still unstarted
- Second agent / contact_lookup (Phase 3)
- Performance measurement beyond qualitative "feels responsive"
- Accessibility, localization, error telemetry

---

## 3. Test Matrix

Execute **both runs** against this matrix. Each step either passes or fails; record the outcome in Section 9.

| # | Step | Expected | Notes |
|---|---|---|---|
| 1 | Python agent starts on `localhost:10002` | `uvicorn running on http://localhost:10002` in agent logs, no startup errors | Run-specific env vars set (Sections 6/7) |
| 2 | Agent card fetch from Shell | Shell status bar shows `"Restaurant Agent"` (not `"Agent (offline)"`) | Exercises `A2AAgentClient.GetAgentNameAsync` + DI header wiring |
| 3 | Text query round-trip | Type `"Top 5 Chinese restaurants in New York"` → Shell renders a surface with a restaurant list (cards with images, ratings, cuisine tags, "Book Now" buttons) | Exercises `createSurface`, `updateComponents`, `updateDataModel`, v0.9 extension negotiation inbound |
| 4 | Second query clears prior surfaces | Type `"Italian restaurants in Seattle"` → prior list disappears before new list renders | Exercises `SurfaceManager.Clear()` from `ShellViewModel.SendAsync` |
| 5 | Booking form renders with date **and** time pickers | Click `"Book Now"` on any restaurant → booking form surface replaces list; form shows both a `CalendarDatePicker` and a `TimePicker` | Exercises Fix #3 (DateTimeInput enableDate/enableTime). If only a date picker appears, Fix #3 regressed |
| 6 | Booking form fields accept input | Fill party size (TextField), pick a future date, pick a time | Exercises input catalog entries, data model updates via `NotifyValueChanged` |
| 7 | Form submit round-trip | Click submit button → confirmation card surface replaces form | Exercises second action round-trip; hardest path — resolved data model values must reach the agent |
| 8 | Outbound payload contract | From Python agent logs, verify the received action envelope matches `{ "userAction": { "name": "book_restaurant", "surfaceId": "...", "sourceComponentId": "...", "timestamp": "...", "context": { "restaurantName": "...", ... } } }` | Exercises `UserActionSerializer` + Fix #1. `name` (not `actionName`) per Lit shell convention |

A run is considered **green** only if all 8 steps pass consecutively without retries, restarts, or manual fudging.

---

## 4. Model Decisions

### Chosen: Claude Sonnet 4.6 (Run 1)
- Current-generation Sonnet (Sonnet 4.5 is now in Anthropic's "Legacy models" table)
- Strong native function-calling and near-perfect JSON schema adherence
- Expected retries in `agent.py:205` validation loop: **zero**
- **Pricing: $3 / MTok input, $15 / MTok output** — identical to Sonnet 4.5, no cost delta for upgrading
- **Context window: 1M tokens** (vs. 200k on 4.5) — vast headroom for the A2UI schema + examples injection
- Knowledge cutoff: Aug 2025 (reliable), Jan 2026 (training data)
- Adaptive thinking support (4.5 did not have this)
- API ID: `claude-sonnet-4-6` — no dated snapshot form needed
- Trade-off: cloud API, costs per call (~$0.003 per query), requires `ANTHROPIC_API_KEY`
- Purpose: establish Shell correctness as a clean baseline before testing on-prem hardware

### Chosen: Gemma 4 31B (Run 2)
- 30.7B dense, 20GB on disk (Ollama Q4_K_M default), 256K context
- Native function-calling explicitly advertised
- Strong structured-output benchmarks (LiveCodeBench v6 80.0, GPQA Diamond 84.3)
- Runs locally on DGX Spark (GB10, 128GB unified LPDDR5X, ~273 GB/s)
- Expected retries in validation loop: **0–1 per request** after sampling override
- Trade-off: needs sampling parameters tuned (Gemma 4 recommends `temperature=1.0`, which is too hot for schema-constrained output; we override to `0.2`)
- Purpose: prove the fully-local path works end-to-end with zero cloud dependencies

### Deferred: Nemotron 3 Super 120B
See Section 10. Short version: reasoning-mode overhead and 87GB memory footprint make it a poor fit for a snappy interactive demo, despite its very strong benchmarks on harder agentic tasks.

### Sampling override decision
The `agent.py:165` `LiteLlm(...)` call currently takes only the model string. For Gemma 4 we need to pass `temperature=0.2, top_p=0.9` to overcome its creative-tuned sampling defaults and force deterministic schema output. See Section 5 Patch B.

For Claude (Run 1) the sampling override is **not required** — Sonnet 4.6's default sampling is already well-calibrated for structured JSON output. Patch B is applied *only* for Run 2.

---

## 5. Required Agent Patches

Both patches target files under `samples/agent/adk/restaurant_finder/`. **These patches are kept local and uncommitted** during verification. If they prove valuable we can decide whether to upstream later; the git history should not carry temporary verification scaffolding.

### Patch A — Bypass Gemini API key pre-flight check

**File:** `samples/agent/adk/restaurant_finder/__main__.py`
**Why:** Lines 42–49 hard-require `GEMINI_API_KEY` unless Vertex AI is active. For both Claude and Gemma runs, we're not using Gemini at all, but LiteLLM's provider prefix (`anthropic/`, `ollama_chat/`) routes to the correct backend.

**Minimal workaround (preferred — no code edit):** set `GEMINI_API_KEY=unused` in `.env`. The check passes, LiteLLM ignores the value because the model prefix routes elsewhere.

**Cleaner alternative (optional, more invasive):** make the check provider-aware. Replace lines 42–49 with:

```python
litellm_model = os.getenv("LITELLM_MODEL", "gemini/gemini-2.5-flash")
if litellm_model.startswith("gemini/") and not os.getenv("GOOGLE_GENAI_USE_VERTEXAI") == "TRUE":
    if not os.getenv("GEMINI_API_KEY"):
        raise MissingAPIKeyError(
            "GEMINI_API_KEY environment variable not set and GOOGLE_GENAI_USE_VERTEXAI"
            " is not TRUE."
        )
```

Use the minimal workaround for Runs 1 and 2. Only apply the cleaner alternative if we decide to upstream it after verification is done.

### Patch B — Sampling override for Gemma 4

**File:** `samples/agent/adk/restaurant_finder/agent.py`
**Why:** Gemma 4's recommended `temperature=1.0, top_p=0.95, top_k=64` is tuned for creative/reasoning tasks and produces unreliable JSON schema conformance. For the A2UI validation loop we need deterministic structured output.

**Required for Run 2 only.** Replace `agent.py:164-170` with:

```python
    return LlmAgent(
        model=LiteLlm(
            model=LITELLM_MODEL,
            # Override Gemma 4's recommended temp=1.0 — too hot for JSON schema
            # conformance. Claude and Gemini's defaults are already fine, so this
            # override is safe across providers. See VERIFICATION_STEPS_PLAN.md §4.
            temperature=0.2,
            top_p=0.9,
        ),
        name="restaurant_agent",
        description="An agent that finds restaurants and helps book tables.",
        instruction=instruction,
        tools=[get_restaurants],
    )
```

`LiteLlm(...)` passes extra kwargs through to `litellm.completion()`, which propagates them to Ollama's chat API, which relays them to Gemma 4's sampler.

**Run 1 (Claude):** applying Patch B is **optional but harmless** — Claude's default sampling is already conservative, and explicit `temperature=0.2` won't hurt quality. If convenient, apply Patch B once and leave it in place for both runs; if not, skip for Run 1.

---

## 6. Run 1 — Claude Sonnet 4.6 (Today)

### 6.1 Prerequisites

- Anthropic API key with Sonnet 4.6 access
- Python 3.13+ with `uv` installed
- `.env` file in `samples/agent/adk/restaurant_finder/` (do not commit)
- Patch A: **minimal workaround** (dummy env var, no code edit)
- Patch B: **optional** (safe to apply or skip; either works for Claude)

### 6.2 Agent setup

Create `samples/agent/adk/restaurant_finder/.env`:

```bash
# Satisfy __main__.py pre-flight check; LiteLLM ignores this because
# LITELLM_MODEL's prefix routes to Anthropic.
GEMINI_API_KEY=unused

# Route LiteLLM to Claude Sonnet 4.6 — current-generation Sonnet, 1M context,
# same $3/$15 per MTok pricing as legacy 4.5, no dated snapshot suffix needed.
LITELLM_MODEL=anthropic/claude-sonnet-4-6
ANTHROPIC_API_KEY=sk-ant-...
```

Confirm `.env` is gitignored:

```bash
grep -n '^\.env$\|/\.env$' samples/agent/adk/restaurant_finder/.gitignore
```

If nothing is returned, add `.env` to that agent's `.gitignore` before proceeding.

### 6.3 Start the agent

```bash
cd samples/agent/adk/restaurant_finder
uv run .
```

**Expected:**
- `uvicorn running on http://localhost:10002` within ~5 seconds
- No `MissingAPIKeyError`
- No `anthropic.AuthenticationError`
- Agent logs on first query: `"--- RestaurantAgent.stream: Response is valid. Sending final response (Attempt 1). ---"` — single-attempt success

### 6.4 Start the Shell

In a separate terminal at the repo root:

```bash
dotnet run --project samples/client/avalonia/Shell/
```

**Expected:**
- Window opens with title `"Restaurant Finder"`
- Status bar shows `"Restaurant Agent"` (fetched from `GetAgentNameAsync`)
- Prompt input focused and ready

### 6.5 Execute test matrix

Walk through steps 1–8 from Section 3. Claude should pass all eight in a single cold run.

### 6.6 Expected run characteristics

- Each text query: ~3–6 seconds first-token, ~5–15 seconds total
- No validation retries visible in agent logs
- No errors visible in Shell logs (`samples/client/avalonia/Shell/bin/.../logs/shell-*.log`)
- Outbound action payload (visible via agent logs on Step 8) contains `"userAction"` at top level with `name`, `surfaceId`, `sourceComponentId`, `timestamp`, `context` fields

---

## 7. Run 2 — Gemma 4 31B on DGX Spark (Tomorrow)

### 7.1 Prerequisites

- DGX Spark reachable from the Windows dev box over the local network
- Ollama running on DGX Spark, listening on `0.0.0.0:11434` (not just `127.0.0.1` — otherwise remote access fails silently)
- Firewall rule on DGX Spark allowing inbound TCP/11434 from the dev box
- `.env` updated (see Section 7.3)
- Patch A: minimal workaround
- Patch B: **required** (sampling override)

### 7.2 Pull the model on DGX Spark

Over SSH or directly on the DGX Spark console:

```bash
ollama pull gemma4:31b
# ~20GB download; verify
ollama list | grep gemma4
```

Quick smoke test on the DGX Spark host itself (before the Windows dev box gets involved):

```bash
ollama run gemma4:31b "Say hello in one sentence."
```

Confirm first-token latency feels reasonable (< ~2 seconds) and total response lands in a few seconds. If it doesn't, investigate Ollama GPU configuration on DGX Spark before proceeding — no point debugging the A2UI layer if the model itself is mis-configured.

### 7.3 Confirm remote reachability from the Windows dev box

From the dev box:

```bash
curl http://<dgx-spark-hostname>:11434/api/tags
```

Expected: JSON list of models including `gemma4:31b`. If the request hangs or times out, fix Ollama's `OLLAMA_HOST` (set to `0.0.0.0:11434` on the DGX Spark host) and firewall before continuing.

### 7.4 Apply Patch B

Edit `samples/agent/adk/restaurant_finder/agent.py` per Section 5 Patch B. Do **not** commit.

### 7.5 Update .env

Replace `samples/agent/adk/restaurant_finder/.env`:

```bash
GEMINI_API_KEY=unused

# Route LiteLLM to Ollama on DGX Spark
LITELLM_MODEL=ollama_chat/gemma4:31b
OLLAMA_API_BASE=http://<dgx-spark-hostname>:11434
```

Note: use `ollama_chat/` (not `ollama/`) — the `_chat` variant routes through Ollama's `/api/chat` endpoint which has more reliable tool-call format handling than the legacy `/api/generate` path.

### 7.6 Start the agent

```bash
cd samples/agent/adk/restaurant_finder
uv run .
```

**Expected:**
- Same startup as Run 1 (no missing key errors)
- First query: noticeably higher first-token latency than Claude because the model has to load into Ollama's GPU memory on DGX Spark. Subsequent queries are fast.
- Agent logs may show occasional `"A2UI validation failed"` warnings followed by `"Retrying..."` — this is normal on Gemma and the retry loop should recover on the second attempt

### 7.7 Start the Shell

Same command as Run 1:

```bash
dotnet run --project samples/client/avalonia/Shell/
```

### 7.8 Execute test matrix

Walk through steps 1–8 from Section 3.

### 7.9 Expected run characteristics

- Each text query: ~5–15 seconds first-token (first query slower due to model load), ~15–30 seconds total response
- **0–1 validation retries per request** is acceptable; **2+ is a warning sign** — see Section 9 triage
- Booking form (Step 5) is the hardest case for Gemma because it combines tool-call output, schema conformance, *and* the new DateTimeInput with both pickers. Watch agent logs closely on this step.

### 7.10 Known risks

- **Tool-call format drift:** LiteLLM's `ollama_chat` adapter translates function-call schemas between OpenAI/Anthropic tool format and Ollama's native format. If Gemma 4 emits tool calls in an unexpected shape, LiteLLM may fail to parse them. Surfaces as `"get_restaurants"` never being called and the agent returning a plain text response.
- **Schema conformance cliff:** If the retry loop fails twice in a row, you'll see `"Max retries exhausted"` in agent logs and a generic error response in the Shell. Drop temperature further (0.1, then 0.05) in Patch B before concluding the model can't do it.
- **Memory pressure on DGX Spark:** If Ollama reports OOM or reverts to CPU inference, reduce concurrent model loads. Gemma 4 31B at Q4 should comfortably fit in 20GB, leaving 100+GB headroom on a 128GB DGX Spark.
- **Network latency:** DGX Spark over wired LAN adds ~1–5ms per request. Over WiFi, variance dominates. Wire the dev box in if possible.

---

## 8. Success Criteria

A run is **verified green** when:

1. All 8 matrix steps pass in a single consecutive session (no agent restart, no Shell restart between steps)
2. No uncaught exceptions in either process's logs
3. Outbound action payload on Step 8 matches the expected shape exactly (not just "something with a restaurant in it")
4. Surfaces replace cleanly on each transition — no stale content, no overlapping cards, no orphaned controls
5. DateTimeInput (Step 5) shows **both** a date picker and a time picker side-by-side, and the submitted time value round-trips as a non-empty ISO 8601 string in the context

Phase 1 is considered complete when **both** Run 1 and Run 2 are green.

If only Run 1 is green, Phase 1 is conditionally complete for the Claude path — we document Run 2's model-side issues and proceed to Phase 2 (.NET agent with MS Agent Framework + Ollama), which bypasses the Python adapter entirely and gives us another data point on local-model viability.

---

## 9. Observation, Triage, and Artifacts

### 9.1 Record per run

For each run, create a short notes file: `VERIFICATION_RUN_1_CLAUDE.md` and `VERIFICATION_RUN_2_GEMMA.md` (local, uncommitted). Capture:

- Date, time, duration
- Pass/fail per matrix step
- Agent log excerpts for any validation retries
- Outbound action payload (copy-paste from agent logs or Shell logs)
- Screenshots of: the restaurant list (Step 3), the booking form showing both pickers (Step 5), the confirmation card (Step 7)
- Any anomalies — unexpected spinners, flickers, layout glitches

Keep these notes **local and uncommitted**. If the run passes cleanly we roll the observations into `RESTAURANT_DEMO_PORT_PLAN.md`'s status table; if it fails we open issues / update the plan's "Known renderer bugs" list.

### 9.2 Triage decision tree

| Symptom | Likely cause | Next step |
|---|---|---|
| Shell window never shows agent name, status stuck on `"Connecting..."` or `"Agent (offline)"` | Agent not running, port mismatch, firewall | Check `curl http://localhost:10002/.well-known/agent-card.json` from Windows host |
| Agent starts but first query returns empty text in Shell | LiteLLM returned nothing or hit an auth error | Check agent logs for LiteLLM errors; verify `ANTHROPIC_API_KEY` (Run 1) or `OLLAMA_API_BASE` (Run 2) |
| Restaurant list renders but images are broken | Agent's static file mount at `/static` not serving; image URL paths in response use wrong base_url | Check agent logs, `images/` directory, `app.mount("/static", ...)` in `__main__.py:76` |
| "Book Now" click fires but nothing happens | Action wire-up broken; `A2UiRenderer.UserActionFired` not subscribed in Shell | Check `ShellViewModel.HandleUserActionAsync` is actually invoked (add breakpoint or log) |
| Booking form shows only date picker, no time picker | Fix #3 regression | Check `InputCatalogEntries.cs:CreateDateTimePicker` path is being taken; confirm `c.EnableTime` is `true` in the component JSON |
| Booking form time-value round-trips as empty string in context | `NotifyCombined` in `CreateDateTimePicker` not firing, or data model path resolution broken | Check `NotifyValueChanged` calls and `bindingPath` resolution |
| Form submit reaches agent but `context.restaurantName` is missing | Action context gathering broken — data model values not resolved at dispatch time | Check `UserActionEventArgs.Payload` at the moment of firing; this is the hardest bug class to diagnose |
| Agent logs show `"A2UI validation failed"` once, then success | Normal for Gemma, retry loop handled it | No action; record as expected behavior for Run 2 |
| Agent logs show `"Max retries exhausted"` | Model cannot produce valid A2UI schema output | **Run 2 only:** lower `temperature` in Patch B to 0.1, retry. If still failing, document and fall back to Claude for sign-off |
| Agent logs show tool not being called (LLM just chats) | Tool-call format drift between LiteLLM and Ollama | **Run 2 only:** try switching `ollama_chat/` → `ollama/`, or update LiteLLM, or switch model. Document |
| Shell crashes with exception | Genuine bug | Capture stack trace from `shell-*.log` and from Avalonia's debug output; **this is the signal to halt verification and fix before continuing** |

### 9.3 Isolation principle

**Do not debug Shell bugs and model bugs simultaneously.** If Run 1 (Claude) fails at step N, halt — fix the Shell, re-run from step 1. Only once Run 1 is fully green do we start Run 2. If Run 2 then fails at step N, we *know* it's model-side because the Shell is proven by Run 1. This lets us document the model issue and move on without chasing phantom Shell bugs.

---

## 10. Deferred — Nemotron 3 Super 120B

Explicitly **not used** for Phase 1 verification.

### Rationale

- **Memory footprint:** 87GB on disk. Fits in DGX Spark's 128GB unified memory but leaves only ~40GB headroom for KV cache and other workloads. Gemma 4 31B (20GB) leaves 100+GB headroom.
- **MoE memory pattern:** 120B total / 12B active sounds fast, but every decoded token still has to touch the full 87GB tensor for router selection. On DGX Spark's LPDDR5X (~273 GB/s — slow compared to HBM), this is memory-bandwidth bound at a lower ceiling than the "12B active" number would suggest.
- **Reasoning mode overhead:** Nemotron generates hidden reasoning traces (often 1000–3000+ tokens) before every user-visible response. For a snappy interactive demo like restaurant booking, this translates into 30–90 second round-trips instead of 5–15.
- **No native JSON mode advertised:** We'd be entirely reliant on prompt-based schema adherence plus the retry loop, with no safety net.
- **Tool-calling not explicitly native:** Tools appear in benchmarks but without the "native function-calling" callout that Gemma 4 has. LiteLLM's tool-format bridge would be less battle-tested on this path.
- **Overkill for the task:** Nemotron's strength is complex multi-step agentic reasoning (SWE-Bench OpenHands 60.47). The restaurant demo's hardest step is "call `get_restaurants`, then emit a valid JSON blob matching a schema" — a single-hop structured-output task that any competent mid-size model handles.

### When to revisit

Nemotron 3 Super 120B is an **excellent candidate** for later phases that genuinely stress multi-step reasoning:

- **Phase 4 (orchestrator):** routing between multiple sub-agents, deciding which sub-agent owns a surface, resolving conflicts — this is the kind of problem Nemotron is built for
- **Any future "agentic" demo** that needs long-horizon planning, code generation, or cross-tool reasoning
- **Benchmark comparison runs** where we want to characterize A2UI protocol load across very different model architectures

Keep it pulled on DGX Spark; don't uninstall. Just don't use it for this verification.

---

## 11. Post-Verification Follow-ups

After both runs are green:

1. **Update `RESTAURANT_DEMO_PORT_PLAN.md`** status table: Phase 1 functional verification → ✅ Done, with links to the run notes and date.
2. **Update `MEMORY.md` project entry** (auto-memory): Phase 1 complete, next milestone = Phase 2 (.NET agent).
3. **Decide whether to upstream the two agent patches.** Patch A (provider-aware key check) is clearly upstream-worthy. Patch B (sampling override) is more nuanced — arguably should be a config option rather than a hard-coded override. Open a conversation with upstream if we want to contribute either back.
4. **Optional cleanup:** delete `VERIFICATION_RUN_1_CLAUDE.md` and `VERIFICATION_RUN_2_GEMMA.md` (or fold their content into a "Phase 1 retrospective" section of this document), then delete this document itself if it has served its purpose. Verification plans are disposable; design plans are not.
5. **Start Phase 2 planning:** MS Agent Framework 1.0 + Ollama Gemma 4 31B for a pure-.NET end-to-end stack. Run 2's results will inform what to watch out for when building the .NET A2UI bridge.

If only Run 1 is green (Run 2 failed on model-side issues):

1. Document Gemma 4's specific failure modes with log excerpts
2. Update this plan with "Known Run 2 limitations" so the next person trying doesn't re-discover them
3. Still proceed to Phase 2 — a native .NET A2UI parser may handle edge cases LiteLLM doesn't

---

## 12. Quick Reference — Commands

```bash
# Both runs
cd samples/agent/adk/restaurant_finder
uv run .
# (in another terminal, repo root)
dotnet run --project samples/client/avalonia/Shell/

# Run 1 only — Claude Sonnet 4.6
# Set in .env:
#   GEMINI_API_KEY=unused
#   LITELLM_MODEL=anthropic/claude-sonnet-4-6
#   ANTHROPIC_API_KEY=sk-ant-...

# Run 2 only — Gemma 4 31B on DGX Spark
# On DGX Spark:
ollama pull gemma4:31b
# From dev box, verify reachability:
curl http://<dgx-spark-hostname>:11434/api/tags
# Apply Patch B to agent.py (see §5)
# Set in .env:
#   GEMINI_API_KEY=unused
#   LITELLM_MODEL=ollama_chat/gemma4:31b
#   OLLAMA_API_BASE=http://<dgx-spark-hostname>:11434

# Solution build + test sanity check (before each run)
dotnet build A2Ui.slnx --configuration Release
dotnet test A2Ui.slnx --configuration Release --no-build
```
