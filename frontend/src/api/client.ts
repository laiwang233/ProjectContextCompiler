import type {
  Artifact,
  Claim,
  ContentBlock,
  ExportBundle,
  ListQuery,
  ModelRun,
  OrchestrationTask,
  PagedResult,
  Project,
  Requirement,
  ReviewDecision,
  TaskDependency
} from "../types";

const jsonHeaders = { "Content-Type": "application/json" };

function withQuery(path: string, query?: ListQuery): string {
  const params = new URLSearchParams();
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") {
        params.set(key, String(value));
      }
    }
  }

  return params.size === 0 ? path : `${path}?${params.toString()}`;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, init);
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const message = body?.message ?? body?.error ?? response.statusText;
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  projects: (query?: ListQuery) => request<PagedResult<Project>>(withQuery("/api/projects", query)),
  project: (projectId: string) => request<Project>(`/api/projects/${projectId}`),
  createProject: (payload: { name: string; description?: string }) =>
    request<Project>("/api/projects", {
      method: "POST",
      headers: jsonHeaders,
      body: JSON.stringify(payload)
    }),
  pasteArtifact: (projectId: string, payload: { title: string; text: string; type: string }) =>
    request<Artifact>(`/api/projects/${projectId}/artifacts/paste`, {
      method: "POST",
      headers: jsonHeaders,
      body: JSON.stringify(payload)
    }),
  uploadArtifact: (projectId: string, file: File) => {
    const data = new FormData();
    data.append("file", file);
    return request<Artifact>(`/api/projects/${projectId}/artifacts/upload`, {
      method: "POST",
      body: data
    });
  },
  artifacts: (projectId: string, query?: ListQuery) =>
    request<PagedResult<Artifact>>(withQuery(`/api/projects/${projectId}/artifacts`, query)),
  readArtifact: (artifactId: string) =>
    request<ContentBlock[]>(`/api/artifacts/${artifactId}/read`, { method: "POST" }),
  contentBlocks: (projectId: string, query?: ListQuery) =>
    request<PagedResult<ContentBlock>>(withQuery(`/api/projects/${projectId}/content-blocks`, query)),
  artifactContentBlocks: (artifactId: string) =>
    request<ContentBlock[]>(`/api/artifacts/${artifactId}/content-blocks`),
  reviewContentBlock: (contentBlockId: string, verificationStatus: "Confirmed" | "Ignored") =>
    request<ContentBlock>(`/api/content-blocks/${contentBlockId}/review`, {
      method: "PUT",
      headers: jsonHeaders,
      body: JSON.stringify({ verificationStatus, reviewerName: "PM" })
    }),
  reviewContentBlocks: (projectId: string, contentBlockIds: string[], verificationStatus: "Confirmed" | "Ignored") =>
    request<ContentBlock[]>(`/api/projects/${projectId}/content-blocks/review`, {
      method: "PUT",
      headers: jsonHeaders,
      body: JSON.stringify({ contentBlockIds, verificationStatus, reviewerName: "PM" })
    }),
  extractClaims: (projectId: string) =>
    request<Claim[]>(`/api/projects/${projectId}/claims/extract`, { method: "POST" }),
  claims: (projectId: string, query?: ListQuery) =>
    request<PagedResult<Claim>>(withQuery(`/api/projects/${projectId}/claims`, query)),
  claim: (claimId: string) =>
    request<Claim>(`/api/claims/${claimId}`),
  ignoreClaim: (claimId: string) =>
    request<Claim>(`/api/claims/${claimId}/ignore`, { method: "PUT" }),
  restoreClaim: (claimId: string) =>
    request<Claim>(`/api/claims/${claimId}/restore`, { method: "PUT" }),
  synthesizeRequirements: (projectId: string) =>
    request<Requirement[]>(`/api/projects/${projectId}/requirements/synthesize`, { method: "POST" }),
  requirements: (projectId: string, query?: ListQuery) =>
    request<PagedResult<Requirement>>(withQuery(`/api/projects/${projectId}/requirements`, query)),
  reviewRequirement: (requirementId: string, decision: ReviewDecision, comment: string) =>
    request<Requirement>(`/api/requirements/${requirementId}/review`, {
      method: "POST",
      headers: jsonHeaders,
      body: JSON.stringify({ decision, reviewerName: "PM", comment })
    }),
  generateTasks: (requirementId: string) =>
    request<OrchestrationTask[]>(`/api/requirements/${requirementId}/tasks/generate`, {
      method: "POST"
    }),
  tasks: (projectId: string, query?: ListQuery) =>
    request<PagedResult<OrchestrationTask>>(withQuery(`/api/projects/${projectId}/tasks`, query)),
  dependencies: (projectId: string, taskId?: string) =>
    request<TaskDependency[]>(
      taskId
        ? `/api/projects/${projectId}/task-dependencies?taskId=${encodeURIComponent(taskId)}`
        : `/api/projects/${projectId}/task-dependencies`
    ),
  exportMarkdown: (projectId: string) =>
    request<ExportBundle>(`/api/projects/${projectId}/exports/symphony-markdown`, { method: "POST" }),
  exports: (projectId: string, query?: ListQuery) =>
    request<PagedResult<ExportBundle>>(withQuery(`/api/projects/${projectId}/exports`, query)),
  modelRuns: (projectId: string, query?: ListQuery) =>
    request<PagedResult<ModelRun>>(withQuery(`/api/projects/${projectId}/model-runs`, query))
};
