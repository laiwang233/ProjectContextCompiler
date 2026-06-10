import { useEffect, useMemo, useState } from "react";
import { Link, Navigate, Route, Routes, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import ReactFlow, { Background, Controls, Edge, Node } from "reactflow";
import {
  Alert,
  Breadcrumb,
  Button,
  Card,
  ConfigProvider,
  Descriptions,
  Drawer,
  Empty,
  Flex,
  Form,
  Input,
  Layout,
  Menu,
  Modal,
  Progress,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
  Upload,
  message,
  theme
} from "antd";
import type { TablePaginationConfig, TableProps } from "antd";
import zhCN from "antd/locale/zh_CN";
import {
  ArrowLeft,
  Check,
  ClipboardCheck,
  Database,
  Download,
  FileText,
  GitBranch,
  Play,
  Plus,
  RefreshCw,
  SearchCheck,
  ShieldCheck,
  Upload as UploadIcon
} from "lucide-react";
import { api } from "./api/client";
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
} from "./types";

const { Header, Sider, Content } = Layout;
const { Title, Text, Paragraph } = Typography;

const demoText = `今天需求评审讨论成员邀请功能。第一版只做管理员通过邮箱邀请成员。普通成员不能邀请。真实邮件服务暂时不接，先 mock。重复邀请时不要创建重复记录。邀请是否计入席位数还没确定。需要补充非管理员不能邀请的 e2e 测试。`;

type StageKey = "artifacts" | "evidence" | "claims" | "requirements" | "tasks" | "exports";

type QueryState = ListQuery & {
  pageNumber: number;
  pageSize: number;
};

const defaultQuery: QueryState = { pageNumber: 1, pageSize: 20 };

const designTokens = {
  primary: "#1f5f8b",
  bgLayout: "#f3f4f6",
  bgElevated: "#fff",
  tableHeader: "#f8fafc",
  tableHover: "#f5f7fa",
  border: "#d0d7de",
  text: "#1f2937",
  radius: 6,
  fontFamily: 'Aptos, "Segoe UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", sans-serif'
} as const;

const stages: Array<{ key: StageKey; label: string; icon: typeof FileText }> = [
  { key: "artifacts", label: "资料", icon: FileText },
  { key: "evidence", label: "证据核验", icon: ClipboardCheck },
  { key: "claims", label: "Claim", icon: SearchCheck },
  { key: "requirements", label: "需求审核", icon: ShieldCheck },
  { key: "tasks", label: "任务", icon: GitBranch },
  { key: "exports", label: "导出", icon: Download }
];

const statusLabels: Record<string, string> = {
  Uploaded: "已上传",
  Reading: "读取中",
  Read: "已读取",
  Failed: "失败",
  Unsupported: "不支持",
  Extracted: "已抽取",
  Merged: "已合并",
  Ignored: "已忽略",
  Confirmed: "已确认",
  Superseded: "已替换",
  PendingReview: "待审核",
  NeedsClarification: "需澄清",
  Approved: "已批准",
  Rejected: "已拒绝",
  Deferred: "已延期",
  TaskGenerated: "已生成任务",
  Exported: "已导出",
  Preview: "预览",
  ReadyForExport: "可导出",
  Cancelled: "已取消",
  Pending: "等待中",
  Running: "运行中",
  Succeeded: "成功",
  TimedOut: "超时",
  SchemaInvalid: "Schema 无效"
};

const typeLabels: Record<string, string> = {
  Text: "文本",
  Markdown: "Markdown",
  Csv: "CSV",
  Excel: "Excel",
  Docx: "Word",
  Pdf: "PDF",
  PastedText: "粘贴文本",
  AsrTranscript: "转写稿",
  MeetingNotes: "会议记录",
  ChatExport: "聊天导出",
  Other: "其他",
  Paragraph: "段落",
  Table: "表格",
  List: "列表",
  ImageText: "图片文本",
  Diagram: "图示",
  TranscriptSegment: "转写片段",
  ChatMessage: "聊天消息",
  RequirementRow: "需求行",
  DecisionNote: "决策记录",
  Requirement: "需求",
  BusinessRule: "业务规则",
  Decision: "决策",
  ActionItem: "行动项",
  ChangeRequest: "变更请求",
  Blocker: "阻塞",
  Risk: "风险",
  Dependency: "依赖",
  AcceptanceRule: "验收规则",
  OpenQuestion: "未决问题",
  Constraint: "约束",
  DeferredScope: "延期范围",
  OutOfScope: "不在范围",
  ImplementationHint: "实现提示",
  TestRequirement: "测试要求",
  Functional: "功能",
  NonFunctional: "非功能",
  Technical: "技术",
  BugFix: "缺陷修复",
  Refactor: "重构",
  Research: "调研",
  Test: "测试",
  Migration: "迁移",
  Unknown: "未知",
  Clarification: "澄清",
  Design: "设计",
  Implementation: "实现",
  Review: "评审",
  FollowUp: "跟进",
  SymphonyMarkdown: "Symphony Markdown",
  Json: "JSON"
};

const decisionLabels: Record<ReviewDecision, string> = {
  Approve: "批准",
  ApproveWithAssumptions: "带假设批准",
  RequestClarification: "请求澄清",
  Reject: "拒绝",
  Defer: "延期"
};

const statusOptions = {
  artifact: ["Uploaded", "Reading", "Read", "Failed", "Unsupported"],
  evidence: ["Pending", "Confirmed", "Ignored"],
  claim: ["Extracted", "Merged", "Ignored", "Superseded"],
  requirement: ["PendingReview", "NeedsClarification", "Approved", "Rejected", "Deferred", "Superseded", "TaskGenerated", "Exported"],
  task: ["Preview", "ReadyForExport", "Exported", "Cancelled"],
  modelRun: ["Pending", "Running", "Succeeded", "Failed", "TimedOut", "SchemaInvalid"]
};

const typeOptions = {
  artifact: ["Text", "Markdown", "Csv", "Excel", "Docx", "Pdf", "PastedText", "AsrTranscript", "MeetingNotes", "ChatExport", "Other"],
  evidence: ["Paragraph", "Table", "List", "ImageText", "Diagram", "TranscriptSegment", "ChatMessage", "RequirementRow", "DecisionNote", "Unknown"],
  claim: ["Requirement", "BusinessRule", "Decision", "ActionItem", "ChangeRequest", "Blocker", "Risk", "Dependency", "AcceptanceRule", "OpenQuestion", "Constraint", "DeferredScope", "OutOfScope", "ImplementationHint", "TestRequirement"],
  requirement: ["Functional", "NonFunctional", "Technical", "BugFix", "Refactor", "Research", "Test", "Migration", "Unknown"],
  task: ["Clarification", "Research", "Design", "Implementation", "Test", "Migration", "Review", "FollowUp"],
  export: ["SymphonyMarkdown", "Json", "Markdown"]
};

export default function App() {
  return (
    <ConfigProvider
      locale={zhCN}
      theme={{
        algorithm: theme.compactAlgorithm,
        token: {
          colorPrimary: designTokens.primary,
          colorBgLayout: designTokens.bgLayout,
          borderRadius: designTokens.radius,
          fontFamily: designTokens.fontFamily
        },
        components: {
          Card: { borderRadiusLG: designTokens.radius },
          Button: { borderRadius: designTokens.radius, controlHeight: 34 },
          Input: { controlHeight: 34 },
          Select: { controlHeight: 34 },
          Table: { headerBg: designTokens.tableHeader, rowHoverBg: designTokens.tableHover }
        }
      }}
    >
      <Routes>
        <Route path="/" element={<Navigate to="/projects" replace />} />
        <Route path="/projects" element={<ProjectIndex />} />
        <Route path="/projects/:projectId/workflow/:stageKey" element={<WorkflowPage />} />
        <Route path="*" element={<Navigate to="/projects" replace />} />
      </Routes>
    </ConfigProvider>
  );
}

function ProjectIndex() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [query, setQuery] = useState<QueryState>({ ...defaultQuery, sortBy: "updatedAt", sortDirection: "desc" });
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm<{ name: string; description?: string }>();
  const projects = useQuery({ queryKey: ["projects", query], queryFn: () => api.projects(query) });
  const createProject = useMutation({
    mutationFn: api.createProject,
    onSuccess: (project) => {
      message.success("项目已创建");
      queryClient.invalidateQueries({ queryKey: ["projects"] });
      setOpen(false);
      form.resetFields();
      navigate(`/projects/${project.id}/workflow/artifacts`);
    },
    onError: (error) => message.error(errorMessage(error))
  });

  const columns: TableProps<Project>["columns"] = [
    {
      title: "项目",
      dataIndex: "name",
      key: "name",
      sorter: true,
      render: (_, project) => (
        <Space orientation="vertical" size={0}>
          <Link to={`/projects/${project.id}/workflow/artifacts`}>{project.name}</Link>
          <Text type="secondary">{project.description || "暂无描述"}</Text>
        </Space>
      )
    },
    {
      title: "待审核",
      dataIndex: ["stats", "pendingReviewRequirementCount"],
      width: 96,
      responsive: ["md"],
      render: (value: number) => <Tag color={value > 0 ? "gold" : "default"}>{value}</Tag>
    },
    {
      title: "任务",
      dataIndex: ["stats", "taskCount"],
      width: 88,
      responsive: ["md"]
    },
    {
      title: "最近更新",
      dataIndex: "updatedAt",
      key: "updatedAt",
      sorter: true,
      width: 180,
      responsive: ["md"],
      render: formatDate
    },
    {
      title: "操作",
      width: 110,
      responsive: ["md"],
      render: (_, project) => (
        <Button size="small" onClick={() => navigate(`/projects/${project.id}/workflow/artifacts`)}>
          打开
        </Button>
      )
    }
  ];

  return (
    <Layout className="app-layout">
      <Header className="app-header">
        <div>
          <Title level={4}>Project Context Compiler</Title>
          <Text type="secondary">项目上下文编译与人工审核工作台</Text>
        </div>
        <Button type="primary" icon={<Plus size={15} />} onClick={() => setOpen(true)}>
          新建项目
        </Button>
      </Header>
      <Content className="page-content">
        <Card className="page-card" title="项目列表">
          <Flex className="table-toolbar" justify="space-between" gap={12} wrap="wrap">
            <Input.Search
              allowClear
              placeholder="搜索项目名称或描述"
              onSearch={(value) => setQuery((current) => ({ ...current, q: value || undefined, pageNumber: 1 }))}
              className="toolbar-search"
            />
            <Text type="secondary">共 {projects.data?.totalCount ?? 0} 个项目</Text>
          </Flex>
          <Table<Project>
            rowKey="id"
            columns={columns}
            dataSource={projects.data?.items ?? []}
            loading={projects.isLoading}
            pagination={pagination(projects.data)}
            onChange={tableChange(setQuery)}
            scroll={{ x: 680 }}
            locale={{ emptyText: <Empty description="暂无项目" /> }}
          />
        </Card>
      </Content>
      <Modal
        title="新建项目"
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createProject.isPending}
        okText="创建"
        cancelText="取消"
      >
        <Form form={form} layout="vertical" onFinish={(values) => createProject.mutate(values)}>
          <Form.Item label="项目名称" name="name" rules={[{ required: true, message: "请输入项目名称" }]}>
            <Input placeholder="例如：邀请功能编译" />
          </Form.Item>
          <Form.Item label="项目描述" name="description">
            <Input.TextArea rows={3} placeholder="描述这个项目的上下文来源和目标" />
          </Form.Item>
        </Form>
      </Modal>
    </Layout>
  );
}

function WorkflowPage() {
  const { projectId, stageKey } = useParams();
  const navigate = useNavigate();
  const currentStage = stages.find((stage) => stage.key === stageKey)?.key ?? "artifacts";
  const project = useQuery({
    queryKey: ["project", projectId],
    queryFn: () => api.project(projectId!),
    enabled: Boolean(projectId)
  });

  if (!projectId) {
    return <Navigate to="/projects" replace />;
  }

  if (!stages.some((stage) => stage.key === stageKey)) {
    return <Navigate to={`/projects/${projectId}/workflow/artifacts`} replace />;
  }

  return (
    <Layout className="app-layout">
      <ProjectSider projectId={projectId} stageKey={currentStage} />
      <Layout>
        <Header className="workflow-header">
          <Flex align="center" justify="space-between" gap={16}>
            <Space orientation="vertical" size={2}>
              <Breadcrumb
                items={[
                  { title: <Link to="/projects">项目</Link> },
                  { title: project.data?.name ?? "加载中" },
                  { title: "流程工作台" }
                ]}
              />
              <Title level={4}>{project.data?.name ?? "流程工作台"}</Title>
              <Text type="secondary">{project.data?.description || "资料到可审核需求和任务导出的端到端流程"}</Text>
            </Space>
            <Button icon={<ArrowLeft size={15} />} onClick={() => navigate("/projects")}>
              返回项目列表
            </Button>
          </Flex>
        </Header>
        <Content className="workflow-content">
          {project.isError ? (
            <Alert type="error" title="项目加载失败" description={(project.error as Error).message} showIcon />
          ) : (
            project.data && <WorkflowWorkbench project={project.data} stageKey={currentStage} />
          )}
        </Content>
      </Layout>
    </Layout>
  );
}

function ProjectSider({ projectId, stageKey }: { projectId: string; stageKey: StageKey }) {
  const projects = useQuery({
    queryKey: ["projects", "sider"],
    queryFn: () => api.projects({ pageNumber: 1, pageSize: 20, sortBy: "updatedAt", sortDirection: "desc" })
  });

  return (
    <Sider width={260} theme="light" className="app-sider">
      <div className="sider-brand">
        <Database size={18} />
        <div>
          <strong>PCC</strong>
          <span>项目工作台</span>
        </div>
      </div>
      <Menu
        className="project-menu"
        mode="inline"
        selectedKeys={[projectId]}
        items={(projects.data?.items ?? []).map((project) => ({
          key: project.id,
          label: (
            <Link to={`/projects/${project.id}/workflow/${stageKey}`}>
              <Space orientation="vertical" size={0}>
                <span>{project.name}</span>
                <Text type="secondary">{project.stats.pendingReviewRequirementCount} 待审 / {project.stats.taskCount} 任务</Text>
              </Space>
            </Link>
          )
        }))}
      />
    </Sider>
  );
}

function WorkflowWorkbench({ project, stageKey }: { project: Project; stageKey: StageKey }) {
  const navigate = useNavigate();
  const currentIndex = stages.findIndex((stage) => stage.key === stageKey);
  const activeStage = stages[currentIndex];
  const notice = stageNotice(project, activeStage.key);

  return (
    <Space orientation="vertical" size={16} className="full-width">
      <Card className="page-card workflow-steps-card">
        <WorkflowStageNav
          project={project}
          activeStageKey={stageKey}
          onStageChange={(nextStage) => navigate(`/projects/${project.id}/workflow/${nextStage}`)}
        />
      </Card>
      <Select
        className="mobile-stage-select"
        value={stageKey}
        options={stages.map((stage) => ({ label: stage.label, value: stage.key }))}
        onChange={(value) => navigate(`/projects/${project.id}/workflow/${value}`)}
      />
      {notice && <Alert type="info" showIcon title={notice} />}
      {stageKey === "artifacts" && <ArtifactsStage projectId={project.id} />}
      {stageKey === "evidence" && <EvidenceStage projectId={project.id} />}
      {stageKey === "claims" && <ClaimsStage project={project} />}
      {stageKey === "requirements" && <RequirementsStage project={project} />}
      {stageKey === "tasks" && <TasksStage project={project} />}
      {stageKey === "exports" && <ExportsStage project={project} />}
    </Space>
  );
}

function WorkflowStageNav({
  project,
  activeStageKey,
  onStageChange
}: {
  project: Project;
  activeStageKey: StageKey;
  onStageChange: (stage: StageKey) => void;
}) {
  return (
    <div className="workflow-stage-nav" role="tablist" aria-label="流程阶段">
      {stages.map((stage, index) => {
        const Icon = stage.icon;
        const status = stageStatus(project, stage.key);
        const active = stage.key === activeStageKey;
        return (
          <button
            key={stage.key}
            type="button"
            className={`workflow-stage-item workflow-stage-${status}${active ? " workflow-stage-active" : ""}`}
            aria-current={active ? "step" : undefined}
            aria-label={`${index + 1}. ${stage.label}`}
            onClick={() => onStageChange(stage.key)}
          >
            <span className="workflow-stage-index">{index + 1}</span>
            <span className="workflow-stage-icon"><Icon size={15} /></span>
            <span className="workflow-stage-title">{stage.label}</span>
          </button>
        );
      })}
    </div>
  );
}

function ArtifactsStage({ projectId }: { projectId: string }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [query, setQuery] = useState<QueryState>(defaultQuery);
  const [selected, setSelected] = useState<Artifact>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [pasteOpen, setPasteOpen] = useState(false);
  const [form] = Form.useForm<{ title: string; text: string }>();
  const isNarrow = useIsNarrow();
  const artifacts = useQuery({ queryKey: ["artifacts", projectId, query], queryFn: () => api.artifacts(projectId, query) });
  const paste = useMutation({
    mutationFn: (values: { title: string; text: string }) => api.pasteArtifact(projectId, { ...values, type: "MeetingNotes" }),
    onSuccess: () => {
      message.success("资料已新增");
      queryClient.invalidateQueries();
      setPasteOpen(false);
      form.resetFields();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const upload = useMutation({
    mutationFn: (file: File) => api.uploadArtifact(projectId, file),
    onSuccess: () => {
      message.success("资料已上传");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const read = useMutation({
    mutationFn: (artifactId: string) => api.readArtifact(artifactId),
    onSuccess: () => {
      message.success("资料读取完成，请核验证据块");
      queryClient.invalidateQueries();
      navigate(`/projects/${projectId}/workflow/evidence`);
    },
    onError: (error) => message.error(errorMessage(error))
  });

  useAutoSelect(artifacts.data, selected, setSelected);

  const columns: TableProps<Artifact>["columns"] = [
    {
      title: "标题",
      dataIndex: "title",
      key: "title",
      sorter: true,
      ellipsis: true,
      render: (_, artifact) => (
        <RowPrimary
          title={artifact.title}
          meta={<Space size={6} wrap>{tagType(artifact.type)}{statusTag(artifact.readStatus)}</Space>}
          onOpenDetail={() => {
            setSelected(artifact);
            setDrawerOpen(true);
          }}
        />
      )
    },
    { title: "类型", dataIndex: "type", key: "type", width: 130, responsive: ["md"], render: tagType },
    { title: "读取状态", dataIndex: "readStatus", key: "status", width: 120, responsive: ["md"], render: statusTag },
    { title: "创建时间", dataIndex: "createdAt", key: "createdAt", sorter: true, width: 160, responsive: ["md"], render: formatDate },
    {
      title: "操作",
      width: 100,
      responsive: ["md"],
      render: (_, artifact) => (
        <Button size="small" icon={<Play size={14} />} loading={read.isPending} onClick={(event) => {
          event.stopPropagation();
          read.mutate(artifact.id);
        }}>
          读取
        </Button>
      )
    }
  ];

  return (
    <>
      <StageGrid
        toolbar={
          <Toolbar
            searchPlaceholder="搜索资料标题或文件名"
            query={query}
            onQueryChange={setQuery}
            statusValues={statusOptions.artifact}
            typeValues={typeOptions.artifact}
            extra={
              <Space>
                <Button type="primary" icon={<Plus size={15} />} onClick={() => setPasteOpen(true)}>
                  新增资料
                </Button>
                <Upload showUploadList={false} beforeUpload={(file) => {
                  upload.mutate(file);
                  return false;
                }}>
                  <Button icon={<UploadIcon size={15} />} loading={upload.isPending}>
                    上传资料
                  </Button>
                </Upload>
              </Space>
            }
          />
        }
        table={
          <Table<Artifact>
            rowKey="id"
            columns={columns}
            dataSource={artifacts.data?.items ?? []}
          loading={artifacts.isLoading}
          pagination={pagination(artifacts.data)}
          onChange={tableChange(setQuery)}
          rowClassName={selectableRowClass(selected)}
          scroll={{ x: 640 }}
          onRow={(record) => ({
            onClick: () => {
              setSelected(record);
                if (isNarrow) setDrawerOpen(true);
              }
            })}
            locale={{ emptyText: <Empty description="暂无资料" /> }}
          />
        }
        detail={<ArtifactDetail projectId={projectId} artifact={selected} />}
        drawerTitle={selected?.title}
        drawerActions={selected && (
          <Button icon={<Play size={14} />} loading={read.isPending} onClick={() => read.mutate(selected.id)}>
            读取
          </Button>
        )}
        drawerOpen={drawerOpen}
        onDrawerClose={() => setDrawerOpen(false)}
      />
      <Modal
        title="新增资料"
        open={pasteOpen}
        onCancel={() => setPasteOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={paste.isPending}
        okText="保存"
        cancelText="取消"
        footer={(_, { OkBtn, CancelBtn }) => (
          <Space>
            <Button onClick={() => form.setFieldsValue({ title: "会议记录", text: demoText })}>填入示例</Button>
            <CancelBtn />
            <OkBtn />
          </Space>
        )}
      >
        <Form form={form} layout="vertical" onFinish={(values) => paste.mutate(values)} initialValues={{ title: "会议记录" }}>
          <Form.Item label="标题" name="title" rules={[{ required: true, message: "请输入资料标题" }]}>
            <Input />
          </Form.Item>
          <Form.Item label="内容" name="text" rules={[{ required: true, message: "请输入资料内容" }]}>
            <Input.TextArea rows={8} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}

function EvidenceStage({ projectId }: { projectId: string }) {
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const targetBlockId = searchParams.get("blockId");
  const [query, setQuery] = useState<QueryState>({ ...defaultQuery, pageSize: 50, sortBy: "createdAt" });
  const [selected, setSelected] = useState<ContentBlock>();
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const isNarrow = useIsNarrow();
  const blocks = useQuery({ queryKey: ["content-blocks", projectId, query], queryFn: () => api.contentBlocks(projectId, query) });
  const reviewOne = useMutation({
    mutationFn: ({ id, status }: { id: string; status: "Confirmed" | "Ignored" }) => api.reviewContentBlock(id, status),
    onSuccess: () => {
      message.success("证据块状态已更新");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const reviewMany = useMutation({
    mutationFn: (status: "Confirmed" | "Ignored") => api.reviewContentBlocks(projectId, selectedRowKeys.map(String), status),
    onSuccess: () => {
      message.success("已批量更新证据块");
      setSelectedRowKeys([]);
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });

  useEffect(() => {
    if (!blocks.data) return;
    const items = blocks.data.items;
    if (items.length === 0) {
      setSelected(undefined);
      return;
    }

    if (targetBlockId) {
      const targeted = items.find((item) => item.id === targetBlockId);
      if (targeted) {
        setSelected(targeted);
        return;
      }
    }

    if (!selected || !items.some((item) => item.id === selected.id)) {
      setSelected(items[0]);
    }
  }, [blocks.data, selected, targetBlockId]);

  const columns: TableProps<ContentBlock>["columns"] = [
    { title: "序号", dataIndex: "orderIndex", key: "orderIndex", sorter: true, width: 80, responsive: ["md"] },
    {
      title: "位置",
      dataIndex: "locationLabel",
      key: "locationLabel",
      ellipsis: true,
      render: (_, block) => (
        <RowPrimary
          title={block.locationLabel || `#${block.orderIndex}`}
          meta={<Space size={6} wrap>{evidenceStatusTag(block.verificationStatus)}<Text type="secondary">{Math.round(block.confidence * 100)}%</Text></Space>}
          description={block.text}
          onOpenDetail={() => {
            setSelected(block);
            setDrawerOpen(true);
          }}
        />
      )
    },
    { title: "类型", dataIndex: "type", key: "type", width: 120, responsive: ["md"], render: tagType },
    { title: "状态", dataIndex: "verificationStatus", key: "status", sorter: true, width: 110, responsive: ["md"], render: evidenceStatusTag },
    { title: "置信度", dataIndex: "confidence", key: "confidence", sorter: true, width: 110, responsive: ["md"], render: confidencePercent },
    {
      title: "核验",
      width: 170,
      responsive: ["md"],
      render: (_, block) => (
        <Space>
          <Button
            size="small"
            icon={<Check size={14} />}
            disabled={block.verificationStatus === "Confirmed"}
            loading={reviewOne.isPending}
            onClick={(event) => {
              event.stopPropagation();
              reviewOne.mutate({ id: block.id, status: "Confirmed" });
            }}
          >
            确认
          </Button>
          <Button
            size="small"
            danger
            disabled={block.verificationStatus === "Ignored"}
            loading={reviewOne.isPending}
            onClick={(event) => {
              event.stopPropagation();
              reviewOne.mutate({ id: block.id, status: "Ignored" });
            }}
          >
            忽略
          </Button>
        </Space>
      )
    }
  ];

  return (
    <StageGrid
      toolbar={
        <Toolbar
          searchPlaceholder="搜索证据文本或位置"
          query={query}
          onQueryChange={setQuery}
          statusValues={statusOptions.evidence}
          typeValues={typeOptions.evidence}
          extra={
            <Space>
              <Button disabled={selectedRowKeys.length === 0} loading={reviewMany.isPending} onClick={() => reviewMany.mutate("Confirmed")}>
                批量确认
              </Button>
              <Button danger disabled={selectedRowKeys.length === 0} loading={reviewMany.isPending} onClick={() => reviewMany.mutate("Ignored")}>
                批量忽略
              </Button>
            </Space>
          }
        />
      }
      table={
        <Table<ContentBlock>
          rowKey="id"
          rowSelection={{
            selectedRowKeys,
            onChange: setSelectedRowKeys
          }}
          columns={columns}
          dataSource={blocks.data?.items ?? []}
          loading={blocks.isLoading}
          pagination={pagination(blocks.data)}
          onChange={tableChange(setQuery)}
          rowClassName={selectableRowClass(selected)}
          scroll={{ x: 860 }}
          onRow={(record) => ({
            onClick: () => {
              setSelected(record);
              if (isNarrow) setDrawerOpen(true);
            }
          })}
          locale={{ emptyText: <Empty description="暂无证据块" /> }}
        />
      }
      detail={<ContentBlockDetail projectId={projectId} block={selected} />}
      drawerTitle={selected?.locationLabel || "证据块详情"}
      drawerActions={selected && (
        <Space>
          <Button
            icon={<Check size={14} />}
            disabled={selected.verificationStatus === "Confirmed"}
            loading={reviewOne.isPending}
            onClick={() => reviewOne.mutate({ id: selected.id, status: "Confirmed" })}
          >
            确认
          </Button>
          <Button
            danger
            disabled={selected.verificationStatus === "Ignored"}
            loading={reviewOne.isPending}
            onClick={() => reviewOne.mutate({ id: selected.id, status: "Ignored" })}
          >
            忽略
          </Button>
        </Space>
      )}
      drawerOpen={drawerOpen}
      onDrawerClose={() => setDrawerOpen(false)}
    />
  );
}

function ClaimsStage({ project }: { project: Project }) {
  const queryClient = useQueryClient();
  const [query, setQuery] = useState<QueryState>(defaultQuery);
  const [selected, setSelected] = useState<Claim>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const isNarrow = useIsNarrow();
  const projectId = project.id;
  const claims = useQuery({ queryKey: ["claims", projectId, query], queryFn: () => api.claims(projectId, query) });
  const pendingBlocks = useQuery({
    queryKey: ["content-blocks", projectId, "pending-count"],
    queryFn: () => api.contentBlocks(projectId, { pageNumber: 1, pageSize: 1, status: "Pending" })
  });
  const confirmedBlocks = useQuery({
    queryKey: ["content-blocks", projectId, "confirmed-count"],
    queryFn: () => api.contentBlocks(projectId, { pageNumber: 1, pageSize: 1, status: "Confirmed" })
  });
  const extract = useMutation({
    mutationFn: () => api.extractClaims(projectId),
    onSuccess: () => {
      message.success("Claim 抽取完成");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const ignore = useMutation({
    mutationFn: api.ignoreClaim,
    onSuccess: () => {
      message.success("Claim 已忽略");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const restore = useMutation({
    mutationFn: api.restoreClaim,
    onSuccess: () => {
      message.success("Claim 已恢复");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const pendingBlockCount = pendingBlocks.data?.totalCount ?? 0;
  const confirmedBlockCount = confirmedBlocks.data?.totalCount ?? 0;
  const extractDisabled = project.stats.contentBlockCount === 0 || pendingBlockCount > 0 || confirmedBlockCount === 0;
  const extractTooltip = project.stats.contentBlockCount === 0
    ? "需要先读取资料生成证据块"
    : pendingBlockCount > 0
      ? "需要先在证据核验阶段处理全部证据块"
      : confirmedBlockCount === 0
        ? "至少需要一个已确认的证据块"
        : undefined;

  useAutoSelect(claims.data, selected, setSelected);

  const columns: TableProps<Claim>["columns"] = [
    {
      title: "主题",
      dataIndex: "subject",
      key: "subject",
      sorter: true,
      ellipsis: true,
      render: (_, claim) => (
        <RowPrimary
          title={claim.subject}
          meta={<Space size={6} wrap>{tagType(claim.type)}{statusTag(claim.status)}<Text type="secondary">{Math.round(claim.confidence * 100)}%</Text></Space>}
          description={claim.text}
          onOpenDetail={() => {
            setSelected(claim);
            setDrawerOpen(true);
          }}
        />
      )
    },
    { title: "类型", dataIndex: "type", key: "type", width: 130, responsive: ["md"], render: tagType },
    { title: "状态", dataIndex: "status", key: "status", width: 110, responsive: ["md"], render: statusTag },
    { title: "置信度", dataIndex: "confidence", key: "confidence", sorter: true, width: 110, responsive: ["md"], render: confidencePercent },
    {
      title: "操作",
      width: 110,
      responsive: ["md"],
      render: (_, claim) => claim.status === "Ignored" ? (
        <Button size="small" icon={<RefreshCw size={14} />} onClick={(event) => {
          event.stopPropagation();
          restore.mutate(claim.id);
        }}>
          恢复
        </Button>
      ) : (
        <Button size="small" danger onClick={(event) => {
          event.stopPropagation();
          ignore.mutate(claim.id);
        }}>
          忽略
        </Button>
      )
    }
  ];

  return (
    <StageGrid
      toolbar={
        <Toolbar
          searchPlaceholder="搜索 Claim 主题或文本"
          query={query}
          onQueryChange={setQuery}
          statusValues={statusOptions.claim}
          typeValues={typeOptions.claim}
          extra={
            <Tooltip title={extractTooltip}>
              <span>
                <Button type="primary" icon={<SearchCheck size={15} />} disabled={extractDisabled} loading={extract.isPending} onClick={() => extract.mutate()}>
                  抽取 Claim
                </Button>
              </span>
            </Tooltip>
          }
        />
      }
      table={
        <Table<Claim>
          rowKey="id"
          columns={columns}
          dataSource={claims.data?.items ?? []}
          loading={claims.isLoading}
          pagination={pagination(claims.data)}
          onChange={tableChange(setQuery)}
          rowClassName={selectableRowClass(selected)}
          scroll={{ x: 680 }}
          onRow={(record) => ({
            onClick: () => {
              setSelected(record);
              if (isNarrow) setDrawerOpen(true);
            }
          })}
          locale={{ emptyText: <Empty description="暂无 Claim" /> }}
        />
      }
      detail={<ClaimDetail projectId={projectId} claim={selected} />}
      drawerTitle={selected?.subject}
      drawerActions={selected && (selected.status === "Ignored" ? (
        <Button icon={<RefreshCw size={14} />} loading={restore.isPending} onClick={() => restore.mutate(selected.id)}>
          恢复
        </Button>
      ) : (
        <Button danger loading={ignore.isPending} onClick={() => ignore.mutate(selected.id)}>
          忽略
        </Button>
      ))}
      drawerOpen={drawerOpen}
      onDrawerClose={() => setDrawerOpen(false)}
    />
  );
}

function RequirementsStage({ project }: { project: Project }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [query, setQuery] = useState<QueryState>({ ...defaultQuery, status: "PendingReview" });
  const [selected, setSelected] = useState<Requirement>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [reviewDecision, setReviewDecision] = useState<ReviewDecision>();
  const [form] = Form.useForm<{ comment: string }>();
  const isNarrow = useIsNarrow();
  const projectId = project.id;
  const requirements = useQuery({ queryKey: ["requirements", projectId, query], queryFn: () => api.requirements(projectId, query) });
  const synthesize = useMutation({
    mutationFn: () => api.synthesizeRequirements(projectId),
    onSuccess: () => {
      message.success("候选需求已合成");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const review = useMutation({
    mutationFn: ({ id, decision, comment }: { id: string; decision: ReviewDecision; comment: string }) => api.reviewRequirement(id, decision, comment),
    onSuccess: (requirement) => {
      message.success("审核结果已提交");
      queryClient.invalidateQueries();
      setReviewDecision(undefined);
      form.resetFields();
      if (requirement.status === "Approved") {
        navigate(`/projects/${projectId}/workflow/tasks?requirementId=${requirement.id}`);
      }
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const synthesizeDisabled = project.stats.claimCount === 0;

  useAutoSelect(requirements.data, selected, setSelected);

  const columns: TableProps<Requirement>["columns"] = [
    {
      title: "标题",
      dataIndex: "title",
      key: "title",
      sorter: true,
      ellipsis: true,
      render: (_, requirement) => (
        <RowPrimary
          title={requirement.title}
          meta={<Space size={6} wrap>{statusTag(requirement.status)}{tagType(requirement.requirementType)}<Text type="secondary">{Math.round(requirement.confidence * 100)}%</Text></Space>}
          description={requirement.summary}
          onOpenDetail={() => {
            setSelected(requirement);
            setDrawerOpen(true);
          }}
        />
      )
    },
    { title: "模块", dataIndex: "module", width: 120, responsive: ["md"], render: (value?: string) => value || "-" },
    { title: "类型", dataIndex: "requirementType", key: "type", width: 110, responsive: ["md"], render: tagType },
    { title: "状态", dataIndex: "status", key: "status", width: 110, responsive: ["md"], render: statusTag },
    { title: "置信度", dataIndex: "confidence", key: "confidence", sorter: true, width: 110, responsive: ["md"], render: confidencePercent },
    { title: "更新时间", dataIndex: "updatedAt", key: "updatedAt", sorter: true, width: 160, responsive: ["md"], render: formatDate }
  ];

  return (
    <>
      <StageGrid
        toolbar={
          <Toolbar
            searchPlaceholder="搜索需求标题、摘要或模块"
            query={query}
            onQueryChange={setQuery}
            statusValues={statusOptions.requirement}
            typeValues={typeOptions.requirement}
            extra={
              <Tooltip title={synthesizeDisabled ? "需要先抽取 Claim" : undefined}>
                <span>
                  <Button type="primary" icon={<ShieldCheck size={15} />} disabled={synthesizeDisabled} loading={synthesize.isPending} onClick={() => synthesize.mutate()}>
                    合成需求
                  </Button>
                </span>
              </Tooltip>
            }
          />
        }
        table={
          <Table<Requirement>
            rowKey="id"
            columns={columns}
            dataSource={requirements.data?.items ?? []}
          loading={requirements.isLoading}
          pagination={pagination(requirements.data)}
          onChange={tableChange(setQuery)}
          rowClassName={selectableRowClass(selected)}
          scroll={{ x: 760 }}
          onRow={(record) => ({
            onClick: () => {
              setSelected(record);
                if (isNarrow) setDrawerOpen(true);
              }
            })}
            locale={{ emptyText: <Empty description="暂无候选需求" /> }}
          />
        }
        detail={<RequirementDetail projectId={projectId} requirement={selected} onReview={setReviewDecision} />}
        drawerTitle={selected?.title}
        drawerOpen={drawerOpen}
        onDrawerClose={() => setDrawerOpen(false)}
      />
      <Modal
        title={reviewDecision ? decisionLabels[reviewDecision] : "审核需求"}
        open={Boolean(reviewDecision && selected)}
        onCancel={() => setReviewDecision(undefined)}
        onOk={() => form.submit()}
        confirmLoading={review.isPending}
        okText="提交审核"
        cancelText="取消"
      >
        <Alert type="info" showIcon title={selected?.title} className="modal-alert" />
        <Form
          form={form}
          layout="vertical"
          onFinish={({ comment }) => selected && reviewDecision && review.mutate({ id: selected.id, decision: reviewDecision, comment })}
          initialValues={{ comment: "确认处理" }}
        >
          <Form.Item label="审核说明" name="comment" rules={[{ required: true, message: "请输入审核说明" }]}>
            <Input.TextArea rows={4} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}

function TasksStage({ project }: { project: Project }) {
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const [query, setQuery] = useState<QueryState>(defaultQuery);
  const [selected, setSelected] = useState<OrchestrationTask>();
  const [selectedRequirementId, setSelectedRequirementId] = useState<string>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const isNarrow = useIsNarrow();
  const projectId = project.id;
  const tasks = useQuery({ queryKey: ["tasks", projectId, query], queryFn: () => api.tasks(projectId, query) });
  const approvedRequirements = useQuery({
    queryKey: ["requirements", projectId, "approved-options"],
    queryFn: () => api.requirements(projectId, { pageNumber: 1, pageSize: 100, status: "Approved", sortBy: "updatedAt", sortDirection: "desc" })
  });
  const selectedRequirement = approvedRequirements.data?.items.find((item) => item.id === selectedRequirementId);
  const generateTasks = useMutation({
    mutationFn: () => selectedRequirementId ? api.generateTasks(selectedRequirementId) : Promise.reject(new Error("需要先选择一个已批准需求")),
    onSuccess: () => {
      message.success("任务已生成");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const generateDisabled = approvedRequirements.isLoading || !selectedRequirementId;

  useEffect(() => {
    if (!approvedRequirements.data) return;
    const requestedRequirementId = searchParams.get("requirementId");
    const items = approvedRequirements.data.items;
    if (requestedRequirementId && items.some((item) => item.id === requestedRequirementId)) {
      setSelectedRequirementId(requestedRequirementId);
      return;
    }

    if (!selectedRequirementId && items.length > 0) {
      setSelectedRequirementId(items[0].id);
    }
  }, [approvedRequirements.data, searchParams, selectedRequirementId]);

  useAutoSelect(tasks.data, selected, setSelected);

  const columns: TableProps<OrchestrationTask>["columns"] = [
    {
      title: "任务",
      dataIndex: "title",
      key: "title",
      sorter: true,
      ellipsis: true,
      render: (_, task) => (
        <RowPrimary
          title={task.title}
          meta={<Space size={6} wrap>{statusTag(task.status)}{tagType(task.taskType)}<Text type="secondary">{Math.round(task.readinessScore)}%</Text></Space>}
          description={task.description}
          onOpenDetail={() => {
            setSelected(task);
            setDrawerOpen(true);
          }}
        />
      )
    },
    { title: "类型", dataIndex: "taskType", key: "type", width: 110, responsive: ["md"], render: tagType },
    { title: "状态", dataIndex: "status", key: "status", width: 110, responsive: ["md"], render: statusTag },
    { title: "就绪度", dataIndex: "readinessScore", key: "readinessScore", sorter: true, width: 110, responsive: ["md"], render: readinessPercent }
  ];

  return (
    <StageGrid
      toolbar={
        <Toolbar
          searchPlaceholder="搜索任务标题或描述"
          query={query}
          onQueryChange={setQuery}
          statusValues={statusOptions.task}
          typeValues={typeOptions.task}
          extra={
            <Space>
              <Select
                className="requirement-select"
                placeholder="选择已批准需求"
                loading={approvedRequirements.isLoading}
                value={selectedRequirementId}
                options={(approvedRequirements.data?.items ?? []).map((requirement) => ({
                  label: `v${requirement.currentVersion} · ${requirement.title}`,
                  value: requirement.id
                }))}
                onChange={setSelectedRequirementId}
              />
              <Tooltip title={generateDisabled ? "需要先批准并选择一个需求" : undefined}>
                <span>
                  <Button type="primary" icon={<GitBranch size={15} />} disabled={generateDisabled} loading={generateTasks.isPending} onClick={() => generateTasks.mutate()}>
                    为所选需求生成任务
                  </Button>
                </span>
              </Tooltip>
            </Space>
          }
        />
      }
      table={
        <Table<OrchestrationTask>
          rowKey="id"
          columns={columns}
          dataSource={tasks.data?.items ?? []}
          loading={tasks.isLoading}
          pagination={pagination(tasks.data)}
          onChange={tableChange(setQuery)}
          rowClassName={selectableRowClass(selected)}
          scroll={{ x: 620 }}
          onRow={(record) => ({
            onClick: () => {
              setSelected(record);
              if (isNarrow) setDrawerOpen(true);
            }
          })}
          locale={{ emptyText: <Empty description="暂无任务" /> }}
        />
      }
      detail={<TaskDetail projectId={projectId} task={selected} selectedRequirement={selectedRequirement} />}
      drawerTitle={selected?.title}
      drawerOpen={drawerOpen}
      onDrawerClose={() => setDrawerOpen(false)}
    />
  );
}

function ExportsStage({ project }: { project: Project }) {
  const queryClient = useQueryClient();
  const [query, setQuery] = useState<QueryState>({ ...defaultQuery, sortBy: "createdAt", sortDirection: "desc" });
  const [selected, setSelected] = useState<ExportBundle>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const isNarrow = useIsNarrow();
  const projectId = project.id;
  const exports = useQuery({ queryKey: ["exports", projectId, query], queryFn: () => api.exports(projectId, query) });
  const exportMarkdown = useMutation({
    mutationFn: () => api.exportMarkdown(projectId),
    onSuccess: () => {
      message.success("Markdown 已生成");
      queryClient.invalidateQueries();
    },
    onError: (error) => message.error(errorMessage(error))
  });
  const exportDisabled = project.stats.taskCount === 0;

  useAutoSelect(exports.data, selected, setSelected);

  const columns: TableProps<ExportBundle>["columns"] = [
    {
      title: "格式",
      dataIndex: "format",
      key: "format",
      sorter: true,
      render: (_, bundle) => (
        <RowPrimary
          title={labelOf(bundle.format)}
          meta={<Text type="secondary">{formatDate(bundle.createdAt)}</Text>}
          onOpenDetail={() => {
            setSelected(bundle);
            setDrawerOpen(true);
          }}
        />
      )
    },
    { title: "创建时间", dataIndex: "createdAt", key: "createdAt", sorter: true, width: 180, responsive: ["md"], render: formatDate },
    {
      title: "操作",
      width: 150,
      responsive: ["md"],
      render: (_, bundle) => (
        <Space>
          <Button size="small" onClick={(event) => {
            event.stopPropagation();
            navigator.clipboard.writeText(bundle.content);
          }}>
            复制
          </Button>
          <a className="ant-btn ant-btn-sm" download={`symphony-${bundle.id}.md`} href={`data:text/markdown;charset=utf-8,${encodeURIComponent(bundle.content)}`}>
            下载
          </a>
        </Space>
      )
    }
  ];

  return (
    <StageGrid
      toolbar={
        <Toolbar
          searchPlaceholder="搜索导出内容"
          query={query}
          onQueryChange={setQuery}
          typeValues={typeOptions.export}
          extra={
            <Tooltip title={exportDisabled ? "需要先生成任务" : undefined}>
              <span>
                <Button type="primary" icon={<Download size={15} />} disabled={exportDisabled} loading={exportMarkdown.isPending} onClick={() => exportMarkdown.mutate()}>
                  生成 Markdown
                </Button>
              </span>
            </Tooltip>
          }
        />
      }
      table={
        <Table<ExportBundle>
          rowKey="id"
          columns={columns}
          dataSource={exports.data?.items ?? []}
          loading={exports.isLoading}
          pagination={pagination(exports.data)}
          onChange={tableChange(setQuery)}
          rowClassName={selectableRowClass(selected)}
          scroll={{ x: 620 }}
          onRow={(record) => ({
            onClick: () => {
              setSelected(record);
              if (isNarrow) setDrawerOpen(true);
            }
          })}
          locale={{ emptyText: <Empty description="暂无导出记录" /> }}
        />
      }
      detail={<ExportDetail projectId={projectId} exportBundle={selected} />}
      drawerTitle={selected ? labelOf(selected.format) : undefined}
      drawerOpen={drawerOpen}
      onDrawerClose={() => setDrawerOpen(false)}
    />
  );
}

function StageGrid({
  toolbar,
  table,
  detail,
  drawerTitle,
  drawerActions,
  drawerOpen,
  onDrawerClose
}: {
  toolbar: React.ReactNode;
  table: React.ReactNode;
  detail: React.ReactNode;
  drawerTitle?: React.ReactNode;
  drawerActions?: React.ReactNode;
  drawerOpen: boolean;
  onDrawerClose: () => void;
}) {
  return (
    <>
      <div className="stage-grid">
        <Card className="page-card stage-main">
          {toolbar}
          {table}
        </Card>
        <aside className="stage-detail">{detail}</aside>
      </div>
      <Drawer size="min(92vw, 520px)" title={drawerTitle ?? "详情"} open={drawerOpen} onClose={onDrawerClose}>
        {detail}
        {drawerActions && <div className="drawer-actions">{drawerActions}</div>}
      </Drawer>
    </>
  );
}

function Toolbar({
  searchPlaceholder,
  query,
  onQueryChange,
  statusValues,
  typeValues,
  extra
}: {
  searchPlaceholder: string;
  query: QueryState;
  onQueryChange: React.Dispatch<React.SetStateAction<QueryState>>;
  statusValues?: string[];
  typeValues?: string[];
  extra?: React.ReactNode;
}) {
  return (
    <Flex className="table-toolbar" align="center" justify="space-between" gap={12} wrap="wrap">
      <Space className="toolbar-controls" wrap>
        <Input.Search
          allowClear
          placeholder={searchPlaceholder}
          className="toolbar-search"
          onSearch={(value) => onQueryChange((current) => ({ ...current, q: value || undefined, pageNumber: 1 }))}
        />
        {statusValues && (
          <Select
            allowClear
            placeholder="状态"
            value={query.status}
            className="toolbar-select"
            options={statusValues.map((value) => ({ label: labelOf(value), value }))}
            onChange={(value) => onQueryChange((current) => ({ ...current, status: value, pageNumber: 1 }))}
          />
        )}
        {typeValues && (
          <Select
            allowClear
            placeholder="类型"
            value={query.type}
            className="toolbar-select"
            options={typeValues.map((value) => ({ label: labelOf(value), value }))}
            onChange={(value) => onQueryChange((current) => ({ ...current, type: value, pageNumber: 1 }))}
          />
        )}
      </Space>
      {extra && <div className="toolbar-extra">{extra}</div>}
    </Flex>
  );
}

function RowPrimary({
  title,
  meta,
  description,
  onOpenDetail
}: {
  title: React.ReactNode;
  meta?: React.ReactNode;
  description?: React.ReactNode;
  onOpenDetail: () => void;
}) {
  return (
    <div className="row-primary">
      <div className="row-primary-head">
        <span className="row-primary-title">{title}</span>
        <Button
          type="link"
          size="small"
          className="mobile-detail-button"
          onClick={(event) => {
            event.stopPropagation();
            onOpenDetail();
          }}
        >
          查看详情
        </Button>
      </div>
      {meta && <div className="row-primary-meta">{meta}</div>}
      {description && <Text type="secondary" className="row-primary-description">{description}</Text>}
    </div>
  );
}

function DetailBlock({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="detail-block">
      <Text strong className="detail-block-title">{title}</Text>
      {children}
    </section>
  );
}

function ContentBlockDetail({ projectId, block }: { projectId: string; block?: ContentBlock }) {
  if (!block) {
    return <Empty description="选择一条证据块查看详情" />;
  }

  return (
    <DetailTabs
      projectId={projectId}
      resetKey={block.id}
      detail={
        <Space orientation="vertical" size={12} className="full-width">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="位置">{block.locationLabel || "-"}</Descriptions.Item>
            <Descriptions.Item label="类型">{tagType(block.type)}</Descriptions.Item>
            <Descriptions.Item label="状态">{evidenceStatusTag(block.verificationStatus)}</Descriptions.Item>
            <Descriptions.Item label="置信度">{confidencePercent(block.confidence)}</Descriptions.Item>
            <Descriptions.Item label="核验人">{block.reviewedBy || "-"}</Descriptions.Item>
            <Descriptions.Item label="核验时间">{formatDate(block.reviewedAt)}</Descriptions.Item>
          </Descriptions>
          <DetailBlock title="证据文本">
            <Paragraph className="pre-wrap">{block.text}</Paragraph>
          </DetailBlock>
        </Space>
      }
    />
  );
}

function ArtifactDetail({ projectId, artifact }: { projectId: string; artifact?: Artifact }) {
  const blocks = useQuery({
    queryKey: ["artifact-content-blocks", artifact?.id],
    queryFn: () => api.artifactContentBlocks(artifact!.id),
    enabled: Boolean(artifact)
  });

  if (!artifact) {
    return <Empty description="选择一条资料查看详情" />;
  }

  return (
    <DetailTabs
      projectId={projectId}
      resetKey={artifact.id}
      detail={
        <Space orientation="vertical" size={12} className="full-width">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="标题">{artifact.title}</Descriptions.Item>
            <Descriptions.Item label="类型">{tagType(artifact.type)}</Descriptions.Item>
            <Descriptions.Item label="读取状态">{statusTag(artifact.readStatus)}</Descriptions.Item>
            <Descriptions.Item label="创建时间">{formatDate(artifact.createdAt)}</Descriptions.Item>
            {artifact.readError && <Descriptions.Item label="错误">{artifact.readError}</Descriptions.Item>}
          </Descriptions>
          <DetailBlock title="原始内容">
            <Paragraph className="pre-wrap">{artifact.rawText || "暂无文本内容"}</Paragraph>
          </DetailBlock>
          <DetailBlock title="证据块">
            <Table<ContentBlock>
              rowKey="id"
              size="small"
              pagination={false}
              loading={blocks.isLoading}
              dataSource={blocks.data ?? []}
              columns={[
                { title: "序号", dataIndex: "orderIndex", width: 64 },
                { title: "类型", dataIndex: "type", width: 110, render: tagType },
                { title: "文本", dataIndex: "text", ellipsis: true }
              ]}
              locale={{ emptyText: <Empty description="暂无证据块" /> }}
            />
          </DetailBlock>
        </Space>
      }
    />
  );
}

function ClaimDetail({ projectId, claim }: { projectId: string; claim?: Claim }) {
  if (!claim) {
    return <Empty description="选择一条 Claim 查看详情" />;
  }

  return (
    <DetailTabs
      projectId={projectId}
      resetKey={claim.id}
      detail={
        <Space orientation="vertical" size={12} className="full-width">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="主题">{claim.subject}</Descriptions.Item>
            <Descriptions.Item label="类型">{tagType(claim.type)}</Descriptions.Item>
            <Descriptions.Item label="状态">{statusTag(claim.status)}</Descriptions.Item>
            <Descriptions.Item label="置信度">{confidencePercent(claim.confidence)}</Descriptions.Item>
            <Descriptions.Item label="歧义度">{confidencePercent(claim.ambiguityScore)}</Descriptions.Item>
          </Descriptions>
          <DetailBlock title="Claim 文本">
            <Paragraph>{claim.text}</Paragraph>
          </DetailBlock>
          <DetailBlock title="证据">
            <Space orientation="vertical" size={8}>
              {claim.evidence.length === 0 ? <Text type="secondary">暂无证据</Text> : claim.evidence.map((item) => <Text key={item.id}>{item.evidenceText}</Text>)}
            </Space>
          </DetailBlock>
        </Space>
      }
    />
  );
}

function RequirementDetail({ projectId, requirement, onReview }: { projectId: string; requirement?: Requirement; onReview: (decision: ReviewDecision) => void }) {
  const navigate = useNavigate();
  const sourceClaimIds = requirement?.content.sourceClaimIds ?? [];
  const sourceClaims = useQuery({
    queryKey: ["requirement-source-claims", requirement?.id, sourceClaimIds],
    queryFn: () => Promise.all(sourceClaimIds.map((claimId) => api.claim(claimId))),
    enabled: Boolean(requirement && sourceClaimIds.length > 0)
  });
  const evidenceItems = sourceClaims.data?.flatMap((claim) => claim.evidence) ?? [];

  if (!requirement) {
    return <Empty description="选择一条需求查看详情" />;
  }

  const handleReview = (decision: ReviewDecision) => {
    const approving = decision === "Approve" || decision === "ApproveWithAssumptions";
    if (approving && evidenceItems.length === 0) {
      Modal.confirm({
        title: "缺少可见来源证据",
        content: "当前详情没有加载到来源 Claim 证据。仍要批准时，请确认你已通过其他方式核对过原始资料。",
        okText: "仍然批准",
        cancelText: "取消",
        onOk: () => onReview(decision)
      });
      return;
    }

    onReview(decision);
  };

  return (
    <DetailTabs
      projectId={projectId}
      resetKey={requirement.id}
      detail={
        <Space orientation="vertical" size={12} className="full-width">
          <Flex justify="space-between" align="start" gap={12}>
            <Space orientation="vertical" size={2}>
              <Title level={5}>{requirement.title}</Title>
              <Text type="secondary">{requirement.summary}</Text>
            </Space>
            {statusTag(requirement.status)}
          </Flex>
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="模块">{requirement.module || "-"}</Descriptions.Item>
            <Descriptions.Item label="类型">{tagType(requirement.requirementType)}</Descriptions.Item>
            <Descriptions.Item label="版本">v{requirement.currentVersion}</Descriptions.Item>
            <Descriptions.Item label="当前版本">{requirement.currentVersionId || "-"}</Descriptions.Item>
            <Descriptions.Item label="置信度">{confidencePercent(requirement.confidence)}</Descriptions.Item>
          </Descriptions>
          <SectionList title="业务规则" items={requirement.content.businessRules} />
          <SectionList title="验收标准" items={requirement.content.acceptanceCriteria} />
          <SectionList title="未决问题" items={requirement.content.openQuestions} />
          <SectionList title="延期范围" items={requirement.content.deferredScope} />
          <SectionList title="假设" items={requirement.content.assumptions} />
          <DetailBlock title="审核材料">
            <Space orientation="vertical" size={10} className="full-width">
              {sourceClaimIds.length === 0 ? (
                <Text type="secondary">暂无来源 Claim</Text>
              ) : sourceClaims.isLoading ? (
                <Text type="secondary">正在加载来源 Claim</Text>
              ) : (
                (sourceClaims.data ?? []).map((claim) => (
                  <section key={claim.id} className="evidence-source">
                    <Text strong>{claim.subject}</Text>
                    <Text type="secondary">{claim.text}</Text>
                    <Space orientation="vertical" size={6}>
                      {claim.evidence.map((item) => (
                        <Flex key={item.id} justify="space-between" align="start" gap={8}>
                          <Text>{item.evidenceText}</Text>
                          <Button
                            type="link"
                            size="small"
                            onClick={() => navigate(`/projects/${projectId}/workflow/evidence?blockId=${item.contentBlockId}`)}
                          >
                            查看证据块
                          </Button>
                        </Flex>
                      ))}
                    </Space>
                  </section>
                ))
              )}
            </Space>
          </DetailBlock>
          <DetailBlock title="版本记录">
            <Space orientation="vertical" size={6} className="full-width">
              {requirement.versions.map((version) => (
                <Text key={version.id}>v{version.version} · {formatDate(version.createdAt)} · {version.changeReason || version.createdBy}</Text>
              ))}
            </Space>
          </DetailBlock>
          <DetailBlock title="审核记录">
            <Space orientation="vertical" size={6} className="full-width">
              {requirement.reviews.length === 0 ? <Text type="secondary">暂无审核记录</Text> : requirement.reviews.map((review) => (
                <Text key={review.id}>{decisionLabels[review.decision]} · {review.reviewerName} · {formatDate(review.createdAt)}{review.comment ? ` · ${review.comment}` : ""}</Text>
              ))}
            </Space>
          </DetailBlock>
          <DetailBlock title="审核操作">
            <Space wrap>
              {(Object.keys(decisionLabels) as ReviewDecision[]).map((decision) => (
                <Button
                  key={decision}
                  type={decision === "Approve" ? "primary" : "default"}
                  danger={decision === "Reject"}
                  onClick={() => handleReview(decision)}
                >
                  {decisionLabels[decision]}
                </Button>
              ))}
            </Space>
          </DetailBlock>
        </Space>
      }
    />
  );
}

function TaskDetail({ projectId, task, selectedRequirement }: { projectId: string; task?: OrchestrationTask; selectedRequirement?: Requirement }) {
  const dependencies = useQuery({
    queryKey: ["dependencies", projectId, task?.id],
    queryFn: () => api.dependencies(projectId, task?.id),
    enabled: Boolean(task)
  });
  const graph = useMemo(() => taskGraph(task, dependencies.data ?? []), [task, dependencies.data]);

  if (!task) {
    return <Empty description="选择一条任务查看详情" />;
  }

  return (
    <DetailTabs
      projectId={projectId}
      resetKey={task.id}
      detail={
        <Tabs
          size="small"
          items={[
            {
              key: "task-detail",
              label: "任务详情",
              children: (
                <Space orientation="vertical" size={12} className="full-width">
                  <Descriptions size="small" column={1}>
                    <Descriptions.Item label="标题">{task.title}</Descriptions.Item>
                    <Descriptions.Item label="类型">{tagType(task.taskType)}</Descriptions.Item>
                    <Descriptions.Item label="状态">{statusTag(task.status)}</Descriptions.Item>
                    <Descriptions.Item label="就绪度">{readinessPercent(task.readinessScore)}</Descriptions.Item>
                    <Descriptions.Item label="来源需求">{selectedRequirement?.title || task.requirementId}</Descriptions.Item>
                    <Descriptions.Item label="来源版本">{task.requirementVersionId}</Descriptions.Item>
                  </Descriptions>
                  <DetailBlock title="描述">
                    <Paragraph>{task.description}</Paragraph>
                  </DetailBlock>
                  <SectionList title="验收标准" items={task.acceptanceCriteria} />
                  <SectionList title="证据" items={task.evidence} />
                  <SectionList title="缺失上下文" items={task.missingContext} />
                  <SectionList title="假设" items={task.assumptions} />
                  <DetailBlock title="Agent Prompt">
                    <Paragraph className="pre-wrap">{task.agentPrompt}</Paragraph>
                  </DetailBlock>
                </Space>
              )
            },
            {
              key: "graph",
              label: "任务图",
              children: (
                <div className="graph-panel">
                  <ReactFlow nodes={graph.nodes} edges={graph.edges} fitView>
                    <Background />
                    <Controls />
                  </ReactFlow>
                </div>
              )
            }
          ]}
        />
      }
    />
  );
}

function ExportDetail({ projectId, exportBundle }: { projectId: string; exportBundle?: ExportBundle }) {
  if (!exportBundle) {
    return <Empty description="选择一条导出记录查看详情" />;
  }

  return (
    <DetailTabs
      projectId={projectId}
      resetKey={exportBundle.id}
      detail={
        <Space orientation="vertical" size={12} className="full-width">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="格式">{tagType(exportBundle.format)}</Descriptions.Item>
            <Descriptions.Item label="创建时间">{formatDate(exportBundle.createdAt)}</Descriptions.Item>
          </Descriptions>
          <Space>
            <Button onClick={() => navigator.clipboard.writeText(exportBundle.content)}>复制内容</Button>
            <a className="ant-btn" download={`symphony-${exportBundle.id}.md`} href={`data:text/markdown;charset=utf-8,${encodeURIComponent(exportBundle.content)}`}>
              下载文件
            </a>
          </Space>
          <pre className="export-preview">{exportBundle.content}</pre>
        </Space>
      }
    />
  );
}

function DetailTabs({ projectId, resetKey, detail }: { projectId: string; resetKey: string; detail: React.ReactNode }) {
  const [activeKey, setActiveKey] = useState("detail");

  useEffect(() => {
    setActiveKey("detail");
  }, [resetKey]);

  return (
    <Tabs
      size="small"
      activeKey={activeKey}
      onChange={setActiveKey}
      items={[
        { key: "detail", label: "详情", children: detail },
        { key: "diagnostics", label: "项目诊断", children: <DiagnosticsPanel projectId={projectId} /> }
      ]}
    />
  );
}

function DiagnosticsPanel({ projectId }: { projectId: string }) {
  const blocks = useQuery({
    queryKey: ["content-blocks", projectId, "diagnostics"],
    queryFn: () => api.contentBlocks(projectId, { pageNumber: 1, pageSize: 5, sortBy: "createdAt" })
  });
  const runs = useQuery({
    queryKey: ["model-runs", projectId, "diagnostics"],
    queryFn: () => api.modelRuns(projectId, { pageNumber: 1, pageSize: 5, sortBy: "createdAt", sortDirection: "desc" })
  });

  return (
    <Space orientation="vertical" size={12} className="full-width">
      <DetailBlock title="最近证据块">
        <Table<ContentBlock>
          rowKey="id"
          size="small"
          pagination={false}
          loading={blocks.isLoading}
          dataSource={blocks.data?.items ?? []}
          columns={[
            { title: "类型", dataIndex: "type", width: 100, render: tagType },
            { title: "文本", dataIndex: "text", ellipsis: true }
          ]}
          locale={{ emptyText: <Empty description="暂无证据块" /> }}
        />
      </DetailBlock>
      <DetailBlock title="最近模型运行">
        <Table<ModelRun>
          rowKey="id"
          size="small"
          pagination={false}
          loading={runs.isLoading}
          dataSource={runs.data?.items ?? []}
          columns={[
            { title: "用途", dataIndex: "purpose", ellipsis: true },
            { title: "状态", dataIndex: "status", width: 90, render: statusTag }
          ]}
          locale={{ emptyText: <Empty description="暂无运行记录" /> }}
        />
      </DetailBlock>
    </Space>
  );
}

function SectionList({ title, items }: { title: string; items: string[] }) {
  return (
    <section className="detail-section">
      <Text strong className="detail-section-title">{title}</Text>
      {items.length === 0 ? (
        <Text type="secondary">无</Text>
      ) : (
        <Space orientation="vertical" size={6}>
          {items.map((item) => <Text key={item}>{item}</Text>)}
        </Space>
      )}
    </section>
  );
}

function useAutoSelect<T extends { id: string }>(page: PagedResult<T> | undefined, selected: T | undefined, setSelected: (value: T | undefined) => void) {
  useEffect(() => {
    if (!page) return;
    if (page.items.length === 0) {
      setSelected(undefined);
      return;
    }

    if (!selected || !page.items.some((item) => item.id === selected.id)) {
      setSelected(page.items[0]);
    }
  }, [page, selected, setSelected]);
}

function useIsNarrow() {
  const [matches, setMatches] = useState(() => window.matchMedia("(max-width: 980px)").matches);

  useEffect(() => {
    const media = window.matchMedia("(max-width: 980px)");
    const listener = () => setMatches(media.matches);
    media.addEventListener("change", listener);
    return () => media.removeEventListener("change", listener);
  }, []);

  return matches;
}

function selectableRowClass<T extends { id: string }>(selected?: T) {
  return (record: T) => record.id === selected?.id ? "stage-row stage-row-selected" : "stage-row";
}

function tableChange<T>(setQuery: React.Dispatch<React.SetStateAction<QueryState>>): TableProps<T>["onChange"] {
  return (page, _filters, sorter) => {
    const activeSorter = Array.isArray(sorter) ? sorter[0] : sorter;
    const sortKey = typeof activeSorter?.columnKey === "string" ? activeSorter.columnKey : undefined;
    setQuery((current) => ({
      ...current,
      pageNumber: page.current ?? current.pageNumber,
      pageSize: page.pageSize ?? current.pageSize,
      sortBy: activeSorter?.order ? sortKey : current.sortBy,
      sortDirection: activeSorter?.order === "ascend" ? "asc" : activeSorter?.order === "descend" ? "desc" : current.sortDirection
    }));
  };
}

function pagination<T>(page?: PagedResult<T>): TablePaginationConfig {
  return {
    current: page?.pageNumber ?? 1,
    pageSize: page?.pageSize ?? 20,
    total: page?.totalCount ?? 0,
    showSizeChanger: true,
    pageSizeOptions: [10, 20, 50, 100],
    showTotal: (total) => `共 ${total} 条`
  };
}

function stageStatus(project: Project, stage: StageKey): "wait" | "process" | "finish" {
  if (stage === "artifacts") return project.stats.artifactCount > 0 ? "finish" : "process";
  if (stage === "evidence") return project.stats.claimCount > 0 ? "finish" : project.stats.contentBlockCount > 0 ? "process" : "wait";
  if (stage === "claims") return project.stats.claimCount > 0 ? "finish" : project.stats.contentBlockCount > 0 ? "process" : "wait";
  if (stage === "requirements") {
    return project.stats.pendingReviewRequirementCount + project.stats.approvedRequirementCount > 0
      ? "finish"
      : project.stats.claimCount > 0
        ? "process"
        : "wait";
  }
  if (stage === "tasks") return project.stats.taskCount > 0 ? "finish" : project.stats.approvedRequirementCount > 0 ? "process" : "wait";
  return project.stats.lastExportedAt ? "finish" : project.stats.taskCount > 0 ? "process" : "wait";
}

function stageNotice(project: Project, stage: StageKey) {
  if (stage === "artifacts") {
    if (project.stats.artifactCount === 0) return "新增或上传资料，然后读取证据块。";
    if (project.stats.contentBlockCount === 0) return "读取资料生成证据块。";
    return undefined;
  }

  if (stage === "evidence") {
    if (project.stats.contentBlockCount === 0) return "先读取资料生成证据块。";
    if (project.stats.claimCount === 0) return "处理全部证据块，至少确认一个证据块后抽取 Claim。";
    return undefined;
  }

  if (stage === "claims") {
    if (project.stats.contentBlockCount === 0) return "先读取并核验证据块。";
    if (project.stats.claimCount === 0) return "从已确认的证据块抽取 Claim。";
    return undefined;
  }

  if (stage === "requirements") {
    if (project.stats.claimCount === 0) return "先抽取 Claim，再合成候选需求。";
    if (project.stats.pendingReviewRequirementCount === 0 && project.stats.approvedRequirementCount === 0) return "合成候选需求后再审核。";
    return undefined;
  }

  if (stage === "tasks") {
    if (project.stats.approvedRequirementCount === 0) return "先批准需求，再生成任务。";
    if (project.stats.taskCount === 0) return "选择已批准需求生成任务。";
    return undefined;
  }

  return project.stats.taskCount === 0 ? "生成任务后再导出。" : undefined;
}

function labelOf(value: string) {
  return statusLabels[value] ?? typeLabels[value] ?? value;
}

function statusTag(value: string) {
  const color = value.includes("Failed") || value.includes("Rejected") || value.includes("Invalid") || value.includes("Cancelled")
    ? "red"
    : value.includes("Pending") || value.includes("Running") || value.includes("Uploaded") || value.includes("Preview")
      ? "gold"
      : value.includes("Approved") || value.includes("Succeeded") || value.includes("Read") || value.includes("Ready")
        ? "green"
        : "default";
  return <Tag color={color}>{labelOf(value)}</Tag>;
}

function evidenceStatusTag(value?: string) {
  const status = value ?? "Pending";
  const labels: Record<string, string> = {
    Pending: "待核验",
    Confirmed: "已确认",
    Ignored: "已忽略"
  };
  const color = status === "Confirmed" ? "green" : status === "Ignored" ? "default" : "gold";
  return <Tag color={color}>{labels[status] ?? status}</Tag>;
}

function tagType(value: string) {
  return <Tag>{labelOf(value)}</Tag>;
}

function confidencePercent(value: number) {
  return <Progress percent={Math.round(value * 100)} size="small" />;
}

function readinessPercent(value: number) {
  return <Progress percent={Math.round(value)} size="small" />;
}

function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : "操作失败";
}

function formatDate(value?: string) {
  return value ? new Date(value).toLocaleString("zh-CN") : "-";
}

function taskGraph(task: OrchestrationTask | undefined, dependencies: TaskDependency[]): { nodes: Node[]; edges: Edge[] } {
  if (!task) {
    return { nodes: [], edges: [] };
  }

  const relatedIds = new Set<string>([task.id]);
  dependencies.slice(0, 24).forEach((dependency) => {
    relatedIds.add(dependency.upstreamTaskId);
    relatedIds.add(dependency.downstreamTaskId);
  });
  const nodes = Array.from(relatedIds).map((id, index) => ({
    id,
    data: { label: id === task.id ? task.title : `任务 ${id.slice(0, 8)}` },
    position: { x: (index % 3) * 220, y: Math.floor(index / 3) * 120 },
    style: {
      border: id === task.id ? `2px solid ${designTokens.primary}` : `1px solid ${designTokens.border}`,
      borderRadius: designTokens.radius,
      color: designTokens.text,
      background: designTokens.bgElevated,
      width: 180,
      fontSize: 12
    }
  }));
  const edges: Edge[] = dependencies.slice(0, 24).map((dependency) => ({
    id: dependency.id,
    source: dependency.upstreamTaskId,
    target: dependency.downstreamTaskId,
    animated: false
  }));
  return { nodes, edges };
}
