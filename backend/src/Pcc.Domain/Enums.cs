namespace Pcc.Domain;

public enum ArtifactType
{
    Text,
    Markdown,
    Csv,
    Excel,
    Docx,
    Pdf,
    PastedText,
    AsrTranscript,
    MeetingNotes,
    ChatExport,
    Other
}

public enum ArtifactReadStatus
{
    Uploaded,
    Reading,
    Read,
    Failed,
    Unsupported
}

public enum ContentBlockType
{
    Paragraph,
    Table,
    List,
    ImageText,
    Diagram,
    TranscriptSegment,
    ChatMessage,
    RequirementRow,
    DecisionNote,
    Unknown
}

public enum ClaimType
{
    Requirement,
    BusinessRule,
    Decision,
    ActionItem,
    ChangeRequest,
    Blocker,
    Risk,
    Dependency,
    AcceptanceRule,
    OpenQuestion,
    Constraint,
    DeferredScope,
    OutOfScope,
    ImplementationHint,
    TestRequirement
}

public enum ClaimStatus
{
    Extracted,
    Merged,
    Ignored,
    Superseded
}

public enum RequirementType
{
    Functional,
    NonFunctional,
    Technical,
    BugFix,
    Refactor,
    Research,
    Test,
    Migration,
    Unknown
}

public enum RequirementStatus
{
    Extracted,
    PendingReview,
    NeedsClarification,
    Approved,
    Rejected,
    Deferred,
    Superseded,
    TaskGenerated,
    Exported
}

public enum RequirementReviewDecision
{
    Approve,
    ApproveWithAssumptions,
    RequestClarification,
    Reject,
    Defer
}

public enum ConflictStatus
{
    Open,
    Resolved,
    Ignored
}

public enum OrchestrationTaskType
{
    Clarification,
    Research,
    Design,
    Implementation,
    Test,
    Migration,
    Review,
    FollowUp
}

public enum OrchestrationTaskStatus
{
    Preview,
    ReadyForExport,
    Exported,
    Cancelled
}

public enum ExportFormat
{
    SymphonyMarkdown,
    Json,
    Markdown
}

public enum ModelRunStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    TimedOut,
    SchemaInvalid
}
