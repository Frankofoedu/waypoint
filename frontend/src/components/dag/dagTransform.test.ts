import { describe, it, expect } from "vitest";
import { transformEventsToGraph } from "./dagTransform";
import type { TraceEvent } from "../../types";

function makeEvent(overrides: Partial<TraceEvent> = {}): TraceEvent {
  return {
    id: crypto.randomUUID(),
    traceId: "trace-1",
    branchName: "main",
    timestamp: new Date().toISOString(),
    eventType: "Prompt",
    stepOrder: 0,
    depth: 0,
    hitlStatus: "None",
    ...overrides,
  };
}

describe("dagTransform", () => {
  it("creates nodes from events", () => {
    const events = [makeEvent({ depth: 0 }), makeEvent({ depth: 1 })];
    const { nodes, edges } = transformEventsToGraph(events);
    expect(nodes).toHaveLength(2);
  });

  it("creates edges from parent_id", () => {
    const parent = makeEvent({ depth: 0 });
    const child = makeEvent({ depth: 1, parentId: parent.id });
    const { edges } = transformEventsToGraph([parent, child]);
    expect(edges).toHaveLength(1);
    expect(edges[0].source).toBe(parent.id);
    expect(edges[0].target).toBe(child.id);
  });

  it("separates branches horizontally", () => {
    const main = makeEvent({ depth: 0, branchName: "main" });
    const retry = makeEvent({ depth: 0, branchName: "retry_1" });
    const { nodes } = transformEventsToGraph([main, retry]);
    expect(nodes[0].position.x).not.toBe(nodes[1].position.x);
  });

  it("marks side-effect events in label", () => {
    const evt = makeEvent({ sideEffects: '[{"type":"email"}]' });
    const { nodes } = transformEventsToGraph([evt]);
    expect(nodes[0].data.label).toContain("⚠️");
  });

  it("marks paused events in label", () => {
    const evt = makeEvent({ hitlStatus: "Paused" });
    const { nodes } = transformEventsToGraph([evt]);
    expect(nodes[0].data.label).toContain("⏸");
  });
});
