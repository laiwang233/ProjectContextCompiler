import { useMemo, useState } from "react";
import { Link, Navigate, Route, Routes, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import ReactFlow, { Background, Controls, Edge, Node } from "reactflow";
import {
  Check,
  ClipboardCheck,
  Download,
  FileText,
  GitBranch,
  Layers3,
  Play,
  Plus,
  RefreshCw,
  SearchCheck,
  ShieldCheck,
  Upload
} from "lucide-react";
import { api } from "./api/client";
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
} from "./types";

const demoText = `今天需求评审讨论成员邀请功能。
第一版只做管理员通过邮箱邀请成员。
普通成员不能邀请。
真实邮件服务暂时不接，先 mock。
重复邀请时不要创建重复记录。
邀请是否计入席位数还没确定。
需要补充非管理员不能邀请的 e2e 测试。`;

const sections = [
  ["artifacts", "资料", FileText],
  ["content-blocks", "证据块", Layers3],
  ["claims", "Claim", SearchCheck],
  ["requirements", "审核", ShieldCheck],
  ["tasks", "任务", GitBranch],
  ["export", "导出", Download],
  ["model-runs", "运行", ClipboardCheck]
] as const;

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/projects" replace />} />
      <Route path="/projects" element={<Workspace />} />
      <Route path="/projects/:projectId" element={<Workspace />} />
      <Route path="/projects/:projectId/:section" element={<Workspace />} />
      <Route path="*" element={<Navigate to="/projects" replace />} />
    </Routes>
  );
}

function Workspace() {
  const navigate = useNavigate();
  const { projectId, section = "artifacts" } = useParams();
  const queryClient = useQueryClient();
  const projectsQuery = useQuery({ queryKey: ["projects"], queryFn: api.projects });
  const projects = projectsQuery.data ?? [];
  const selected = projects.find((project) => project.id === projectId) ?? projects[0];

  const createProject = useMutation({
    mutationFn: api.createProject,
    onSuccess: (project) => {
      queryClient.invalidateQueries({ queryKey: ["projects"] });
      navigate(`/projects/${project.id}/artifacts`);
    }
  });

  const [name, setName] = useState("邀请功能编译");
  const [description, setDescription] = useState("从会议记录编译可审核需求");

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">PCC</span>
          <div>
            <strong>Project Context Compiler</strong>
            <span>LLM-first requirement compiler</span>
          </div>
        </div>

        <form
          className="create-form"
          onSubmit={(event) => {
            event.preventDefault();
            createProject.mutate({ name, description });
          }}
        >
          <input value={name} onChange={(event) => setName(event.target.value)} aria-label="项目名称" />
          <input value={description} onChange={(event) => setDescription(event.target.value)} aria-label="项目描述" />
          <button type="submit">
            <Plus size={16} /> 新建
          </button>
        </form>

        <nav className="project-list">
          {projects.map((project) => (
            <Link
              key={project.id}
              className={project.id === selected?.id ? "project-link active" : "project-link"}
              to={`/projects/${project.id}/${section}`}
            >
              <strong>{project.name}</strong>
              <span>{project.stats.pendingReviewRequirementCount} 待审 / {project.stats.taskCount} 任务</span>
            </Link>
          ))}
        </nav>
      </aside>

      <main className="workspace">
        {selected ? (
          <ProjectWorkbench project={selected} section={section} />
        ) : (
          <section className="empty-state">
            <h1>创建第一个项目</h1>
          </section>
        )}
      </main>
    </div>
  );
}

function ProjectWorkbench({ project, section }: { project: Project; section: string }) {
  const navigate = useNavigate();
  return (
    <>
      <header className="work-header">
        <div>
          <h1>{project.name}</h1>
          <p>{project.description}</p>
        </div>
        <StatStrip project={project} />
      </header>

      <div className="stage-tabs">
        {sections.map(([key, label, Icon]) => (
          <button
            key={key}
            className={section === key ? "active" : ""}
            onClick={() => navigate(`/projects/${project.id}/${key}`)}
            title={label}
          >
            <Icon size={16} />
            {label}
          </button>
        ))}
      </div>

      <FlowCommands projectId={project.id} />

      {section === "artifacts" && <ArtifactsPanel projectId={project.id} />}
      {section === "content-blocks" && <ContentBlocksPanel projectId={project.id} />}
      {section === "claims" && <ClaimsPanel projectId={project.id} />}
      {section === "requirements" && <RequirementsPanel projectId={project.id} />}
      {section === "tasks" && <TasksPanel projectId={project.id} />}
      {section === "export" && <ExportPanel projectId={project.id} />}
      {section === "model-runs" && <ModelRunsPanel projectId={project.id} />}
    </>
  );
}

function StatStrip({ project }: { project: Project }) {
  const stats = [
    ["Artifact", project.stats.artifactCount],
    ["ContentBlock", project.stats.contentBlockCount],
    ["Claim", project.stats.claimCount],
    ["Pending", project.stats.pendingReviewRequirementCount],
    ["Approved", project.stats.approvedRequirementCount],
    ["Task", project.stats.taskCount]
  ];
  return (
    <div className="stat-strip">
      {stats.map(([label, value]) => (
        <div key={label}>
          <span>{label}</span>
          <strong>{value}</strong>
        </div>
      ))}
    </div>
  );
}

function FlowCommands({ projectId }: { projectId: string }) {
  const queryClient = useQueryClient();
  const artifacts = useQuery({ queryKey: ["artifacts", projectId], queryFn: () => api.artifacts(projectId) });
  const requirements = useQuery({ queryKey: ["requirements", projectId], queryFn: () => api.requirements(projectId) });
  const firstArtifact = artifacts.data?.[0];
  const pending = requirements.data?.find((requirement) => requirement.status === "PendingReview");
  const approved = requirements.data?.find((requirement) => requirement.status === "Approved");
  const invalidateAll = () => {
    for (const key of ["projects", "artifacts", "content-blocks", "claims", "requirements", "tasks", "dependencies", "exports", "model-runs"]) {
      queryClient.invalidateQueries({ queryKey: [key] });
    }
  };
  const paste = useMutation({ mutationFn: () => api.pasteArtifact(projectId, { title: "会议记录", text: demoText, type: "MeetingNotes" }), onSuccess: invalidateAll });
  const read = useMutation({ mutationFn: () => api.readArtifact(firstArtifact!.id), onSuccess: invalidateAll });
  const extract = useMutation({ mutationFn: () => api.extractClaims(projectId), onSuccess: invalidateAll });
  const synthesize = useMutation({ mutationFn: () => api.synthesizeRequirements(projectId), onSuccess: invalidateAll });
  const approve = useMutation({ mutationFn: () => api.reviewRequirement(pending!.id, "Approve", "确认进入开发"), onSuccess: invalidateAll });
  const tasks = useMutation({ mutationFn: () => api.generateTasks(approved!.id), onSuccess: invalidateAll });
  const exportMutation = useMutation({ mutationFn: () => api.exportMarkdown(projectId), onSuccess: invalidateAll });

  const buttons = [
    { label: "粘贴示例", icon: Plus, action: () => paste.mutate(), disabled: paste.isPending },
    { label: "读取资料", icon: Layers3, action: () => read.mutate(), disabled: !firstArtifact || read.isPending },
    { label: "抽取 Claim", icon: SearchCheck, action: () => extract.mutate(), disabled: extract.isPending },
    { label: "合成需求", icon: ShieldCheck, action: () => synthesize.mutate(), disabled: synthesize.isPending },
    { label: "批准", icon: Check, action: () => approve.mutate(), disabled: !pending || approve.isPending },
    { label: "生成任务", icon: GitBranch, action: () => tasks.mutate(), disabled: !approved || tasks.isPending },
    { label: "导出", icon: Download, action: () => exportMutation.mutate(), disabled: exportMutation.isPending }
  ];

  return (
    <div className="command-bar">
      {buttons.map(({ label, icon: Icon, action, disabled }) => (
        <button key={label} onClick={action} disabled={disabled}>
          <Icon size={16} /> {label}
        </button>
      ))}
    </div>
  );
}

function ArtifactsPanel({ projectId }: { projectId: string }) {
  const queryClient = useQueryClient();
  const [text, setText] = useState(demoText);
  const [title, setTitle] = useState("会议记录");
  const artifacts = useQuery({ queryKey: ["artifacts", projectId], queryFn: () => api.artifacts(projectId) });
  const paste = useMutation({
    mutationFn: () => api.pasteArtifact(projectId, { title, text, type: "MeetingNotes" }),
    onSuccess: () => queryClient.invalidateQueries()
  });
  const upload = useMutation({
    mutationFn: (file: File) => api.uploadArtifact(projectId, file),
    onSuccess: () => queryClient.invalidateQueries()
  });
  const read = useMutation({
    mutationFn: (artifactId: string) => api.readArtifact(artifactId),
    onSuccess: () => queryClient.invalidateQueries()
  });

  return (
    <section className="panel-grid">
      <div className="tool-panel">
        <div className="panel-title">Paste</div>
        <input value={title} onChange={(event) => setTitle(event.target.value)} />
        <textarea value={text} onChange={(event) => setText(event.target.value)} />
        <button onClick={() => paste.mutate()}><Plus size={16} /> 保存文本</button>
      </div>
      <div className="tool-panel">
        <div className="panel-title">Upload</div>
        <label className="upload-box">
          <Upload size={20} />
          <input type="file" onChange={(event) => event.target.files?.[0] && upload.mutate(event.target.files[0])} />
        </label>
      </div>
      <DataTable
        title="Artifacts"
        rows={artifacts.data ?? []}
        render={(artifact: Artifact) => (
          <tr key={artifact.id}>
            <td>{artifact.title}</td>
            <td>{artifact.type}</td>
            <td><Status value={artifact.readStatus} /></td>
            <td><button onClick={() => read.mutate(artifact.id)}><Play size={14} /> Read</button></td>
          </tr>
        )}
      />
    </section>
  );
}

function ContentBlocksPanel({ projectId }: { projectId: string }) {
  const blocks = useQuery({ queryKey: ["content-blocks", projectId], queryFn: () => api.contentBlocks(projectId) });
  return (
    <DataTable
      title="ContentBlocks"
      rows={blocks.data ?? []}
      render={(block: ContentBlock) => (
        <tr key={block.id}>
          <td>{block.type}</td>
          <td>{block.text}</td>
          <td>{block.locationLabel}</td>
          <td>{Math.round(block.confidence * 100)}%</td>
        </tr>
      )}
    />
  );
}

function ClaimsPanel({ projectId }: { projectId: string }) {
  const queryClient = useQueryClient();
  const claims = useQuery({ queryKey: ["claims", projectId], queryFn: () => api.claims(projectId) });
  const ignore = useMutation({ mutationFn: api.ignoreClaim, onSuccess: () => queryClient.invalidateQueries() });
  const restore = useMutation({ mutationFn: api.restoreClaim, onSuccess: () => queryClient.invalidateQueries() });
  return (
    <DataTable
      title="Claims"
      rows={claims.data ?? []}
      render={(claim: Claim) => (
        <tr key={claim.id}>
          <td>{claim.type}</td>
          <td>{claim.subject}</td>
          <td>{claim.text}</td>
          <td>{claim.evidence.length}</td>
          <td><Status value={claim.status} /></td>
          <td>
            {claim.status === "Ignored" ? (
              <button onClick={() => restore.mutate(claim.id)}><RefreshCw size={14} /> Restore</button>
            ) : (
              <button onClick={() => ignore.mutate(claim.id)}>Ignore</button>
            )}
          </td>
        </tr>
      )}
    />
  );
}

function RequirementsPanel({ projectId }: { projectId: string }) {
  const queryClient = useQueryClient();
  const requirements = useQuery({ queryKey: ["requirements", projectId], queryFn: () => api.requirements(projectId) });
  const review = useMutation({
    mutationFn: ({ id, decision }: { id: string; decision: ReviewDecision }) =>
      api.reviewRequirement(id, decision, decision),
    onSuccess: () => queryClient.invalidateQueries()
  });
  return (
    <div className="requirements">
      {(requirements.data ?? []).map((requirement) => (
        <article className="requirement-card" key={requirement.id}>
          <header>
            <div>
              <h2>{requirement.title}</h2>
              <p>{requirement.summary}</p>
            </div>
            <Status value={requirement.status} />
          </header>
          <div className="requirement-columns">
            <List title="业务规则" items={requirement.content.businessRules} />
            <List title="验收标准" items={requirement.content.acceptanceCriteria} />
            <List title="未决问题" items={requirement.content.openQuestions} />
            <List title="延期范围" items={requirement.content.deferredScope} />
          </div>
          <div className="review-actions">
            {(["Approve", "ApproveWithAssumptions", "RequestClarification", "Reject", "Defer"] as ReviewDecision[]).map((decision) => (
              <button key={decision} onClick={() => review.mutate({ id: requirement.id, decision })}>
                {decision}
              </button>
            ))}
          </div>
        </article>
      ))}
    </div>
  );
}

function TasksPanel({ projectId }: { projectId: string }) {
  const tasks = useQuery({ queryKey: ["tasks", projectId], queryFn: () => api.tasks(projectId) });
  const dependencies = useQuery({ queryKey: ["dependencies", projectId], queryFn: () => api.dependencies(projectId) });
  const graph = useMemo(() => toGraph(tasks.data ?? [], dependencies.data ?? []), [tasks.data, dependencies.data]);
  return (
    <section className="task-layout">
      <div className="graph-panel">
        <ReactFlow nodes={graph.nodes} edges={graph.edges} fitView>
          <Background />
          <Controls />
        </ReactFlow>
      </div>
      <DataTable
        title="Tasks"
        rows={tasks.data ?? []}
        render={(task: OrchestrationTask) => (
          <tr key={task.id}>
            <td>{task.title}</td>
            <td>{task.taskType}</td>
            <td>{task.readinessScore}</td>
            <td><Status value={task.status} /></td>
          </tr>
        )}
      />
    </section>
  );
}

function ExportPanel({ projectId }: { projectId: string }) {
  const queryClient = useQueryClient();
  const exports = useQuery({ queryKey: ["exports", projectId], queryFn: () => api.exports(projectId) });
  const exportMutation = useMutation({ mutationFn: () => api.exportMarkdown(projectId), onSuccess: () => queryClient.invalidateQueries() });
  const latest = exports.data?.[0];
  return (
    <section className="export-panel">
      <div className="export-actions">
        <button onClick={() => exportMutation.mutate()}><Download size={16} /> 生成 Markdown</button>
        {latest && <button onClick={() => navigator.clipboard.writeText(latest.content)}>复制</button>}
        {latest && <DownloadLink exportBundle={latest} />}
      </div>
      <pre>{latest?.content ?? ""}</pre>
    </section>
  );
}

function ModelRunsPanel({ projectId }: { projectId: string }) {
  const runs = useQuery({ queryKey: ["model-runs", projectId], queryFn: () => api.modelRuns(projectId) });
  return (
    <DataTable
      title="ModelRuns"
      rows={runs.data ?? []}
      render={(run: ModelRun) => (
        <tr key={run.id}>
          <td>{run.purpose}</td>
          <td>{run.providerName}</td>
          <td>{run.modelName}</td>
          <td><Status value={run.status} /></td>
          <td>{run.error}</td>
        </tr>
      )}
    />
  );
}

function DataTable<T>({ title, rows, render }: { title: string; rows: T[]; render: (row: T) => React.ReactNode }) {
  return (
    <div className="data-table">
      <div className="panel-title">{title}</div>
      <table>
        <tbody>{rows.map(render)}</tbody>
      </table>
    </div>
  );
}

function List({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="list-block">
      <strong>{title}</strong>
      {items.length === 0 ? <span>无</span> : items.map((item) => <span key={item}>{item}</span>)}
    </div>
  );
}

function Status({ value }: { value: string }) {
  return <span className={`status status-${value.toLowerCase()}`}>{value}</span>;
}

function DownloadLink({ exportBundle }: { exportBundle: ExportBundle }) {
  const href = URL.createObjectURL(new Blob([exportBundle.content], { type: "text/markdown" }));
  return <a className="button-link" download={`symphony-${exportBundle.id}.md`} href={href}>下载</a>;
}

function toGraph(tasks: OrchestrationTask[], dependencies: TaskDependency[]): { nodes: Node[]; edges: Edge[] } {
  const nodes = tasks.map((task, index) => ({
    id: task.id,
    data: { label: task.title },
    position: { x: (index % 3) * 260, y: Math.floor(index / 3) * 150 },
    style: {
      border: "1px solid #375a7f",
      borderRadius: 6,
      color: "#1c1e21",
      background: "#fffdf8",
      width: 210,
      fontSize: 13
    }
  }));
  const edges = dependencies.map((dependency) => ({
    id: dependency.id,
    source: dependency.upstreamTaskId,
    target: dependency.downstreamTaskId,
    animated: false
  }));
  return { nodes, edges };
}
