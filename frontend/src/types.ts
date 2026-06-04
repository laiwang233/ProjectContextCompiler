export type ArtifactType =
  | "Text"
  | "Markdown"
  | "Csv"
  | "Excel"
  | "Docx"
  | "Pdf"
  | "PastedText"
  | "AsrTranscript"
  | "MeetingNotes"
  | "ChatExport"
  | "Other";

export type RequirementStatus =
  | "Extracted"
  | "PendingReview"
  | "NeedsClarification"
  | "Approved"
  | "Rejected"
  | "Deferred"
  | "Superseded"
  | "TaskGenerated"
  | "Exported";

export type ReviewDecision =
  | "Approve"
  | "ApproveWithAssumptions"
  | "RequestClarification"
  | "Reject"
  | "Defer";

export type RequirementContent = {
  actors: string[];
  preconditions: string[];
  businessRules: string[];
  mainFlow: string[];
  exceptionFlows: string[];
  acceptanceCriteria: string[];
  openQuestions: string[];
  outOfScope: string[];
  deferredScope: string[];
  risks: string[];
  assumptions: string[];
  sourceClaimIds: string[];
};

export type Project = {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  stats: {
    artifactCount: number;
    contentBlockCount: number;
    claimCount: number;
    pendingReviewRequirementCount: number;
    approvedRequirementCount: number;
    taskCount: number;
    lastExportedAt?: string;
  };
};

export type Artifact = {
  id: string;
  projectId: string;
  title: string;
  type: ArtifactType;
  originalFileName?: string;
  rawText?: string;
  readStatus: string;
  readError?: string;
  createdAt: string;
};

export type ContentBlock = {
  id: string;
  projectId: string;
  artifactId: string;
  type: string;
  text: string;
  locationLabel?: string;
  orderIndex: number;
  confidence: number;
  createdAt: string;
};

export type Claim = {
  id: string;
  projectId: string;
  type: string;
  subject: string;
  text: string;
  confidence: number;
  ambiguityScore: number;
  status: string;
  evidence: { id: string; contentBlockId: string; evidenceText: string }[];
};

export type Requirement = {
  id: string;
  projectId: string;
  title: string;
  summary: string;
  module?: string;
  requirementType: string;
  status: RequirementStatus;
  confidence: number;
  currentVersion: number;
  currentVersionId?: string;
  content: RequirementContent;
  versions: { id: string; version: number; content: RequirementContent; createdAt: string }[];
  reviews: { id: string; decision: ReviewDecision; reviewerName: string; comment?: string; createdAt: string }[];
};

export type OrchestrationTask = {
  id: string;
  projectId: string;
  requirementId: string;
  requirementVersionId: string;
  title: string;
  description: string;
  taskType: string;
  status: string;
  readinessScore: number;
  agentPrompt: string;
  acceptanceCriteria: string[];
  evidence: string[];
  missingContext: string[];
  assumptions: string[];
};

export type TaskDependency = {
  id: string;
  upstreamTaskId: string;
  downstreamTaskId: string;
  dependencyType: string;
};

export type ExportBundle = {
  id: string;
  projectId: string;
  format: string;
  content: string;
  createdAt: string;
};

export type ModelRun = {
  id: string;
  projectId: string;
  purpose: string;
  providerName: string;
  modelName?: string;
  status: string;
  error?: string;
  createdAt: string;
  finishedAt?: string;
};
