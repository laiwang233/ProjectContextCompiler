import type {
  Artifact,
  Claim,
  ContentBlock,
  ExportBundle,
  ModelRun,
  OrchestrationTask,
  Project,
  Requirement,
  ReviewDecision,
  TaskDependency
} from "../types";

const jsonHeaders = { "Content-Type": "application/json" };

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
  projects: () => request<Project[]>("/api/projects"),
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
  artifacts: (projectId: string) => request<Artifact[]>(`/api/projects/${projectId}/artifacts`),
  readArtifact: (artifactId: string) =>
    request<ContentBlock[]>(`/api/artifacts/${artifactId}/read`, { method: "POST" }),
  contentBlocks: (projectId: string) =>
    request<ContentBlock[]>(`/api/projects/${projectId}/content-blocks`),
  extractClaims: (projectId: string) =>
    request<Claim[]>(`/api/projects/${projectId}/claims/extract`, { method: "POST" }),
  claims: (projectId: string) => request<Claim[]>(`/api/projects/${projectId}/claims`),
  ignoreClaim: (claimId: string) =>
    request<Claim>(`/api/claims/${claimId}/ignore`, { method: "PUT" }),
  restoreClaim: (claimId: string) =>
    request<Claim>(`/api/claims/${claimId}/restore`, { method: "PUT" }),
  synthesizeRequirements: (projectId: string) =>
    request<Requirement[]>(`/api/projects/${projectId}/requirements/synthesize`, { method: "POST" }),
  requirements: (projectId: string) =>
    request<Requirement[]>(`/api/projects/${projectId}/requirements`),
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
  tasks: (projectId: string) => request<OrchestrationTask[]>(`/api/projects/${projectId}/tasks`),
  dependencies: (projectId: string) =>
    request<TaskDependency[]>(`/api/projects/${projectId}/task-dependencies`),
  exportMarkdown: (projectId: string) =>
    request<ExportBundle>(`/api/projects/${projectId}/exports/symphony-markdown`, { method: "POST" }),
  exports: (projectId: string) => request<ExportBundle[]>(`/api/projects/${projectId}/exports`),
  modelRuns: (projectId: string) => request<ModelRun[]>(`/api/projects/${projectId}/model-runs`)
};
