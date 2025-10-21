# 12. Workflow Integration trong OrchardCore
## (Viết khi cần automation, business processes, event-driven systems)

> **Khi nào sử dụng**: Viết **KHI CẦN** automation workflows, business process management, event-driven systems, approval processes

---

## 🎯 **TỔNG QUAN WORKFLOW INTEGRATION**

Workflow Integration trong OrchardCore là hệ thống mạnh mẽ cho phép:
- **Business Process Automation**: Tự động hóa quy trình kinh doanh
- **Event-Driven Architecture**: Xử lý events và triggers
- **Custom Activities**: Tạo activities tùy chỉnh cho business logic
- **Approval Workflows**: Quy trình phê duyệt và review
- **Integration Workflows**: Kết nối với external systems

---

## 🏗️ **CORE CONCEPTS**

### **1. Workflow Architecture**

#### **Workflow Components**
```csharp
// Workflow Type - Template cho workflows
public class WorkflowType
{
    public string WorkflowTypeId { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSingleton { get; set; }
    public bool DeleteFinishedWorkflows { get; set; }
    public IList<ActivityRecord> Activities { get; set; }
    public IList<Transition> Transitions { get; set; }
}

// Workflow Instance - Thực thể workflow đang chạy
public class Workflow
{
    public string WorkflowId { get; set; }
    public string WorkflowTypeId { get; set; }
    public WorkflowStatus Status { get; set; }
    public JsonObject State { get; set; }
    public string CorrelationId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
}

// Workflow Execution Context
public class WorkflowExecutionContext
{
    public Workflow Workflow { get; set; }
    public WorkflowType WorkflowType { get; set; }
    public IDictionary<string, object> Input { get; set; }
    public IDictionary<string, object> Output { get; set; }
    public IDictionary<string, object> Properties { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
```

### **2. Activity System**

#### **Base Activity Pattern**
```csharp
// Abstract Activity Base Class
public abstract class Activity : Entity, IActivity
{
    public abstract string Name { get; }
    public abstract LocalizedString DisplayText { get; }
    public abstract LocalizedString Category { get; }
    public virtual bool HasEditor => true;

    // Định nghĩa possible outcomes
    public virtual IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return [];
    }

    // Kiểm tra có thể execute không
    public virtual Task<bool> CanExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Task.FromResult(true);
    }

    // Execute activity logic
    public abstract Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext);

    // Resume từ blocking state
    public virtual Task<ActivityExecutionResult> ResumeAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Task.FromResult(ActivityExecutionResult.Empty);
    }

    // Workflow lifecycle hooks
    public virtual Task OnWorkflowStartingAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnWorkflowStartedAsync(WorkflowExecutionContext context)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnActivityExecutingAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnActivityExecutedAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        return Task.CompletedTask;
    }
}
```

#### **Task Activity Pattern**
```csharp
// Task Activity - Thực hiện action cụ thể
public abstract class TaskActivity : Activity, ITask
{
    // Task activities thường có outcomes cụ thể
    protected static ActivityExecutionResult Outcomes(params string[] names)
    {
        return new ActivityExecutionResult(names);
    }

    protected static ActivityExecutionResult Halt()
    {
        return ActivityExecutionResult.Halted;
    }

    protected static ActivityExecutionResult Noop()
    {
        return ActivityExecutionResult.Empty;
    }
}

// Generic Task Activity
public abstract class TaskActivity<TActivity> : TaskActivity where TActivity : ITask
{
    public override string Name => typeof(TActivity).Name;
}
```

#### **Event Activity Pattern**
```csharp
// Event Activity - Lắng nghe events
public abstract class EventActivity : Activity, IEvent
{
    public override ActivityExecutionResult Execute(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        // Events halt workflow để chờ event xảy ra
        return Halt();
    }

    // Event activities thường override ResumeAsync
    public override async Task<ActivityExecutionResult> ResumeAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        // Process event data và continue workflow
        return await ProcessEventAsync(workflowContext, activityContext);
    }

    protected abstract Task<ActivityExecutionResult> ProcessEventAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext);
}
```

---

## 🔧 **CUSTOM ACTIVITIES DEVELOPMENT**

### **1. Business Logic Task Activities**

#### **Email Notification Task**
```csharp
public class SendEmailTask : TaskActivity<SendEmailTask>
{
    private readonly IEmailService _emailService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public SendEmailTask(
        IEmailService emailService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<SendEmailTask> localizer)
    {
        _emailService = emailService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Send Email Task"];
    public override LocalizedString Category => S["Messaging"];

    // Properties cho activity
    public WorkflowExpression<string> Recipients
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Subject
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Body
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public bool IsHtml
    {
        get => GetProperty(() => false);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Sent"], S["Failed"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            // Evaluate expressions
            var recipients = await _expressionEvaluator.EvaluateAsync(Recipients, workflowContext, null);
            var subject = await _expressionEvaluator.EvaluateAsync(Subject, workflowContext, null);
            var body = await _expressionEvaluator.EvaluateAsync(Body, workflowContext, null);

            // Parse recipients
            var recipientList = recipients.Split(',', ';')
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            // Send email
            var emailMessage = new EmailMessage
            {
                To = recipientList,
                Subject = subject,
                Body = body,
                IsHtml = IsHtml
            };

            await _emailService.SendAsync(emailMessage);

            // Store result in workflow properties
            workflowContext.Properties["EmailSent"] = true;
            workflowContext.Properties["EmailSentAt"] = DateTime.UtcNow;
            workflowContext.Properties["EmailRecipients"] = recipientList;

            return Outcomes("Sent");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["EmailError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}
```

#### **Database Operation Task**
```csharp
public class ExecuteQueryTask : TaskActivity<ExecuteQueryTask>
{
    private readonly ISession _session;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public ExecuteQueryTask(
        ISession session,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<ExecuteQueryTask> localizer)
    {
        _session = session;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Execute Query Task"];
    public override LocalizedString Category => S["Database"];

    public WorkflowExpression<string> Query
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public string OutputVariable
    {
        get => GetProperty(() => "QueryResult");
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Success"], S["Error"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var query = await _expressionEvaluator.EvaluateAsync(Query, workflowContext, null);
            
            // Execute query (simplified - add proper SQL injection protection)
            var results = await _session.Query<object>(query).ListAsync();
            
            // Store results in workflow output
            workflowContext.Output[OutputVariable] = results;
            workflowContext.Properties["QueryExecutedAt"] = DateTime.UtcNow;
            workflowContext.Properties["RecordCount"] = results.Count;

            return Outcomes("Success");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["QueryError"] = ex.Message;
            return Outcomes("Error");
        }
    }
}
```

#### **HTTP Request Task**
```csharp
public class HttpRequestTask : TaskActivity<HttpRequestTask>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public HttpRequestTask(
        IHttpClientFactory httpClientFactory,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<HttpRequestTask> localizer)
    {
        _httpClientFactory = httpClientFactory;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["HTTP Request Task"];
    public override LocalizedString Category => S["Integration"];

    public WorkflowExpression<string> Url
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public string Method
    {
        get => GetProperty(() => "GET");
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Headers
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Body
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public string ContentType
    {
        get => GetProperty(() => "application/json");
        set => SetProperty(value);
    }

    public int TimeoutSeconds
    {
        get => GetProperty(() => 30);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Success"], S["Failed"], S["Timeout"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var url = await _expressionEvaluator.EvaluateAsync(Url, workflowContext, null);
            var headers = await _expressionEvaluator.EvaluateAsync(Headers, workflowContext, null);
            var body = await _expressionEvaluator.EvaluateAsync(Body, workflowContext, null);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            // Parse và add headers
            if (!string.IsNullOrEmpty(headers))
            {
                var headerDict = JsonSerializer.Deserialize<Dictionary<string, string>>(headers);
                foreach (var header in headerDict)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            HttpResponseMessage response;
            
            switch (Method.ToUpper())
            {
                case "GET":
                    response = await httpClient.GetAsync(url);
                    break;
                case "POST":
                    var postContent = new StringContent(body ?? "", Encoding.UTF8, ContentType);
                    response = await httpClient.PostAsync(url, postContent);
                    break;
                case "PUT":
                    var putContent = new StringContent(body ?? "", Encoding.UTF8, ContentType);
                    response = await httpClient.PutAsync(url, putContent);
                    break;
                case "DELETE":
                    response = await httpClient.DeleteAsync(url);
                    break;
                default:
                    throw new NotSupportedException($"HTTP method {Method} is not supported");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Store response in workflow properties
            workflowContext.Properties["HttpStatusCode"] = (int)response.StatusCode;
            workflowContext.Properties["HttpResponseContent"] = responseContent;
            workflowContext.Properties["HttpResponseHeaders"] = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            workflowContext.Properties["HttpRequestedAt"] = DateTime.UtcNow;

            return response.IsSuccessStatusCode ? Outcomes("Success") : Outcomes("Failed");
        }
        catch (TaskCanceledException)
        {
            workflowContext.Properties["HttpError"] = "Request timeout";
            return Outcomes("Timeout");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["HttpError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}
```

### **2. Event Activities**

#### **Content Published Event**
```csharp
public class ContentPublishedEvent : EventActivity
{
    private readonly IStringLocalizer S;

    public ContentPublishedEvent(IStringLocalizer<ContentPublishedEvent> localizer)
    {
        S = localizer;
    }

    public override string Name => nameof(ContentPublishedEvent);
    public override LocalizedString DisplayText => S["Content Published Event"];
    public override LocalizedString Category => S["Content"];

    public string ContentTypes
    {
        get => GetProperty(() => "");
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Published"]);
    }

    protected override async Task<ActivityExecutionResult> ProcessEventAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        // Event data được pass qua workflowContext.Input
        var contentItem = workflowContext.Input["ContentItem"] as ContentItem;
        
        if (contentItem == null)
            return Noop();

        // Filter theo content types nếu được specify
        if (!string.IsNullOrEmpty(ContentTypes))
        {
            var allowedTypes = ContentTypes.Split(',').Select(t => t.Trim()).ToList();
            if (!allowedTypes.Contains(contentItem.ContentType))
                return Noop();
        }

        // Store content item data trong workflow properties
        workflowContext.Properties["PublishedContentItem"] = contentItem;
        workflowContext.Properties["PublishedContentType"] = contentItem.ContentType;
        workflowContext.Properties["PublishedContentId"] = contentItem.ContentItemId;
        workflowContext.Properties["PublishedAt"] = contentItem.PublishedUtc;

        return Outcomes("Published");
    }
}
```

#### **Timer Event**
```csharp
public class TimerEvent : EventActivity
{
    private readonly IStringLocalizer S;

    public TimerEvent(IStringLocalizer<TimerEvent> localizer)
    {
        S = localizer;
    }

    public override string Name => nameof(TimerEvent);
    public override LocalizedString DisplayText => S["Timer Event"];
    public override LocalizedString Category => S["Timer"];

    public WorkflowExpression<DateTime> StartAt
    {
        get => GetProperty(() => new WorkflowExpression<DateTime>());
        set => SetProperty(value);
    }

    public WorkflowExpression<TimeSpan> Interval
    {
        get => GetProperty(() => new WorkflowExpression<TimeSpan>());
        set => SetProperty(value);
    }

    public bool IsRecurring
    {
        get => GetProperty(() => false);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Timer"]);
    }

    protected override async Task<ActivityExecutionResult> ProcessEventAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        // Timer event được trigger bởi background service
        workflowContext.Properties["TimerTriggeredAt"] = DateTime.UtcNow;
        
        // Nếu là recurring timer, schedule next execution
        if (IsRecurring)
        {
            var interval = await EvaluateAsync(Interval, workflowContext);
            var nextExecution = DateTime.UtcNow.Add(interval);
            workflowContext.Properties["NextTimerExecution"] = nextExecution;
        }

        return Outcomes("Timer");
    }

    private async Task<T> EvaluateAsync<T>(WorkflowExpression<T> expression, WorkflowExecutionContext context)
    {
        // Simplified expression evaluation
        return expression.Expression != null ? 
            await Task.FromResult(expression.Expression) : 
            default(T);
    }
}
```

### **3. Control Flow Activities**

#### **Parallel Execution Task**
```csharp
public class ParallelTask : TaskActivity<ParallelTask>
{
    private readonly IStringLocalizer S;

    public ParallelTask(IStringLocalizer<ParallelTask> localizer)
    {
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Parallel Task"];
    public override LocalizedString Category => S["Control Flow"];

    public int BranchCount
    {
        get => GetProperty(() => 2);
        set => SetProperty(value);
    }

    public bool WaitForAll
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        var outcomes = new List<string>();
        
        for (int i = 1; i <= BranchCount; i++)
        {
            outcomes.Add($"Branch{i}");
        }

        if (WaitForAll)
        {
            outcomes.Add("AllCompleted");
        }

        return Outcomes(outcomes.ToArray());
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        // Initialize parallel execution state
        var parallelState = new ParallelExecutionState
        {
            TotalBranches = BranchCount,
            CompletedBranches = 0,
            WaitForAll = WaitForAll,
            StartedAt = DateTime.UtcNow
        };

        workflowContext.Properties["ParallelState"] = parallelState;

        // Start all branches simultaneously
        var branchOutcomes = new List<string>();
        for (int i = 1; i <= BranchCount; i++)
        {
            branchOutcomes.Add($"Branch{i}");
        }

        return Outcomes(branchOutcomes.ToArray());
    }

    public override async Task<ActivityExecutionResult> ResumeAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        var parallelState = workflowContext.Properties["ParallelState"] as ParallelExecutionState;
        
        if (parallelState == null)
            return Noop();

        parallelState.CompletedBranches++;

        if (WaitForAll && parallelState.CompletedBranches >= parallelState.TotalBranches)
        {
            workflowContext.Properties["ParallelCompletedAt"] = DateTime.UtcNow;
            return Outcomes("AllCompleted");
        }
        else if (!WaitForAll)
        {
            // Continue với first completed branch
            return Outcomes("AllCompleted");
        }

        // Still waiting for more branches
        return Halt();
    }
}

public class ParallelExecutionState
{
    public int TotalBranches { get; set; }
    public int CompletedBranches { get; set; }
    public bool WaitForAll { get; set; }
    public DateTime StartedAt { get; set; }
}
```

---

## 🎛️ **WORKFLOW MANAGEMENT**

### **1. Workflow Manager Service**
```csharp
public class CustomWorkflowManager : IWorkflowManager
{
    private readonly IWorkflowStore _workflowStore;
    private readonly IWorkflowTypeStore _workflowTypeStore;
    private readonly IActivityLibrary _activityLibrary;
    private readonly ILogger<CustomWorkflowManager> _logger;

    public CustomWorkflowManager(
        IWorkflowStore workflowStore,
        IWorkflowTypeStore workflowTypeStore,
        IActivityLibrary activityLibrary,
        ILogger<CustomWorkflowManager> logger)
    {
        _workflowStore = workflowStore;
        _workflowTypeStore = workflowTypeStore;
        _activityLibrary = activityLibrary;
        _logger = logger;
    }

    public async Task<WorkflowExecutionContext> StartWorkflowAsync(
        string workflowTypeId, 
        IDictionary<string, object> input = null,
        string correlationId = null)
    {
        var workflowType = await _workflowTypeStore.GetAsync(workflowTypeId);
        if (workflowType == null)
            throw new ArgumentException($"Workflow type {workflowTypeId} not found");

        // Create new workflow instance
        var workflow = NewWorkflow(workflowType, correlationId);
        
        // Create execution context
        var executionContext = await CreateWorkflowExecutionContextAsync(workflowType, workflow, input);

        // Start workflow execution
        await ExecuteWorkflowAsync(executionContext);

        return executionContext;
    }

    public async Task<WorkflowExecutionContext> ResumeWorkflowAsync(
        string workflowId, 
        string activityId, 
        IDictionary<string, object> input = null)
    {
        var workflow = await _workflowStore.GetAsync(workflowId);
        if (workflow == null)
            throw new ArgumentException($"Workflow {workflowId} not found");

        var workflowType = await _workflowTypeStore.GetAsync(workflow.WorkflowTypeId);
        if (workflowType == null)
            throw new ArgumentException($"Workflow type {workflow.WorkflowTypeId} not found");

        // Create execution context
        var executionContext = await CreateWorkflowExecutionContextAsync(workflowType, workflow, input);

        // Resume from specific activity
        await ResumeWorkflowAsync(executionContext, activityId);

        return executionContext;
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsByCorrelationIdAsync(string correlationId)
    {
        return await _workflowStore.ListAsync(w => w.CorrelationId == correlationId);
    }

    public async Task<IEnumerable<Workflow>> GetActiveWorkflowsAsync()
    {
        return await _workflowStore.ListAsync(w => w.Status == WorkflowStatus.Executing || w.Status == WorkflowStatus.Halted);
    }

    public async Task CancelWorkflowAsync(string workflowId, string reason = null)
    {
        var workflow = await _workflowStore.GetAsync(workflowId);
        if (workflow != null && workflow.Status != WorkflowStatus.Finished)
        {
            workflow.Status = WorkflowStatus.Aborted;
            workflow.FinishedUtc = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(reason))
            {
                var state = workflow.State.ToObject<WorkflowState>();
                state.Properties["CancellationReason"] = reason;
                workflow.State = JObject.FromObject(state);
            }

            await _workflowStore.SaveAsync(workflow);
            _logger.LogInformation("Workflow {WorkflowId} cancelled: {Reason}", workflowId, reason);
        }
    }

    private Workflow NewWorkflow(WorkflowType workflowType, string correlationId = null)
    {
        return new Workflow
        {
            WorkflowId = Guid.NewGuid().ToString(),
            WorkflowTypeId = workflowType.WorkflowTypeId,
            Status = WorkflowStatus.Idle,
            CorrelationId = correlationId,
            CreatedUtc = DateTime.UtcNow,
            State = JObject.FromObject(new WorkflowState
            {
                ActivityStates = workflowType.Activities.ToDictionary(x => x.ActivityId, x => x.Properties)
            })
        };
    }

    private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(
        WorkflowType workflowType, 
        Workflow workflow, 
        IDictionary<string, object> input = null)
    {
        var context = new WorkflowExecutionContext
        {
            Workflow = workflow,
            WorkflowType = workflowType,
            Input = input ?? new Dictionary<string, object>(),
            Output = new Dictionary<string, object>(),
            Properties = new Dictionary<string, object>()
        };

        return context;
    }

    private async Task ExecuteWorkflowAsync(WorkflowExecutionContext context)
    {
        // Implementation của workflow execution logic
        // Simplified version - actual implementation is more complex
        context.Workflow.Status = WorkflowStatus.Executing;
        await _workflowStore.SaveAsync(context.Workflow);

        // Execute starting activities
        var startActivities = context.WorkflowType.Activities.Where(a => IsStartActivity(a));
        
        foreach (var activity in startActivities)
        {
            await ExecuteActivityAsync(context, activity);
        }
    }

    private async Task ResumeWorkflowAsync(WorkflowExecutionContext context, string activityId)
    {
        var activity = context.WorkflowType.Activities.FirstOrDefault(a => a.ActivityId == activityId);
        if (activity != null)
        {
            await ResumeActivityAsync(context, activity);
        }
    }

    private async Task ExecuteActivityAsync(WorkflowExecutionContext context, ActivityRecord activityRecord)
    {
        var activity = await _activityLibrary.InstantiateActivityAsync(activityRecord);
        var activityContext = new ActivityContext
        {
            ActivityRecord = activityRecord,
            Activity = activity
        };

        try
        {
            var result = await activity.ExecuteAsync(context, activityContext);
            await ProcessActivityResultAsync(context, activityContext, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing activity {ActivityId} in workflow {WorkflowId}", 
                activityRecord.ActivityId, context.Workflow.WorkflowId);
            
            context.Workflow.Status = WorkflowStatus.Faulted;
            await _workflowStore.SaveAsync(context.Workflow);
        }
    }

    private async Task ResumeActivityAsync(WorkflowExecutionContext context, ActivityRecord activityRecord)
    {
        var activity = await _activityLibrary.InstantiateActivityAsync(activityRecord);
        var activityContext = new ActivityContext
        {
            ActivityRecord = activityRecord,
            Activity = activity
        };

        try
        {
            var result = await activity.ResumeAsync(context, activityContext);
            await ProcessActivityResultAsync(context, activityContext, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming activity {ActivityId} in workflow {WorkflowId}", 
                activityRecord.ActivityId, context.Workflow.WorkflowId);
        }
    }

    private async Task ProcessActivityResultAsync(
        WorkflowExecutionContext context, 
        ActivityContext activityContext, 
        ActivityExecutionResult result)
    {
        if (result.IsHalted)
        {
            context.Workflow.Status = WorkflowStatus.Halted;
            await _workflowStore.SaveAsync(context.Workflow);
        }
        else if (result.Outcomes.Any())
        {
            // Process transitions based on outcomes
            await ProcessTransitionsAsync(context, activityContext.ActivityRecord, result.Outcomes);
        }
    }

    private async Task ProcessTransitionsAsync(
        WorkflowExecutionContext context, 
        ActivityRecord sourceActivity, 
        IEnumerable<string> outcomes)
    {
        var transitions = context.WorkflowType.Transitions
            .Where(t => t.SourceActivityId == sourceActivity.ActivityId && outcomes.Contains(t.SourceOutcomeName));

        foreach (var transition in transitions)
        {
            var targetActivity = context.WorkflowType.Activities
                .FirstOrDefault(a => a.ActivityId == transition.DestinationActivityId);
                
            if (targetActivity != null)
            {
                await ExecuteActivityAsync(context, targetActivity);
            }
        }
    }

    private bool IsStartActivity(ActivityRecord activity)
    {
        // Simplified - check if activity has no incoming transitions
        return true; // Implementation depends on workflow structure
    }
}
```

### **2. Event Handling System**
```csharp
public class WorkflowEventHandler : IContentHandler
{
    private readonly IWorkflowManager _workflowManager;
    private readonly IWorkflowTypeStore _workflowTypeStore;
    private readonly ILogger<WorkflowEventHandler> _logger;

    public WorkflowEventHandler(
        IWorkflowManager workflowManager,
        IWorkflowTypeStore workflowTypeStore,
        ILogger<WorkflowEventHandler> logger)
    {
        _workflowManager = workflowManager;
        _workflowTypeStore = workflowTypeStore;
        _logger = logger;
    }

    public async Task PublishedAsync(PublishContentContext context)
    {
        // Trigger workflows listening for ContentPublishedEvent
        await TriggerWorkflowsAsync("ContentPublishedEvent", new Dictionary<string, object>
        {
            ["ContentItem"] = context.ContentItem,
            ["PublishContext"] = context
        });
    }

    public async Task CreatedAsync(CreateContentContext context)
    {
        await TriggerWorkflowsAsync("ContentCreatedEvent", new Dictionary<string, object>
        {
            ["ContentItem"] = context.ContentItem,
            ["CreateContext"] = context
        });
    }

    public async Task UpdatedAsync(UpdateContentContext context)
    {
        await TriggerWorkflowsAsync("ContentUpdatedEvent", new Dictionary<string, object>
        {
            ["ContentItem"] = context.ContentItem,
            ["UpdateContext"] = context
        });
    }

    private async Task TriggerWorkflowsAsync(string eventName, IDictionary<string, object> input)
    {
        try
        {
            // Find workflow types that have activities listening for this event
            var workflowTypes = await _workflowTypeStore.ListAsync();
            var relevantWorkflowTypes = workflowTypes.Where(wt => 
                wt.IsEnabled && 
                wt.Activities.Any(a => a.Name == eventName));

            foreach (var workflowType in relevantWorkflowTypes)
            {
                // Check if this is a singleton workflow
                if (workflowType.IsSingleton)
                {
                    var existingWorkflows = await _workflowManager.GetWorkflowsByTypeAsync(workflowType.WorkflowTypeId);
                    if (existingWorkflows.Any(w => w.Status == WorkflowStatus.Executing || w.Status == WorkflowStatus.Halted))
                    {
                        // Resume existing workflow instead of creating new one
                        var existingWorkflow = existingWorkflows.First();
                        await _workflowManager.ResumeWorkflowAsync(existingWorkflow.WorkflowId, eventName, input);
                        continue;
                    }
                }

                // Start new workflow
                await _workflowManager.StartWorkflowAsync(workflowType.WorkflowTypeId, input);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering workflows for event {EventName}", eventName);
        }
    }
}
```

---

## 🔄 **WORKFLOW PATTERNS**

### **1. Approval Workflow Pattern**
```csharp
public class ApprovalWorkflowBuilder
{
    public static WorkflowType BuildContentApprovalWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "ContentApprovalWorkflow",
            Name = "Content Approval Workflow",
            IsEnabled = true,
            IsSingleton = false,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Start Event - Content Submitted for Approval
        var startEvent = new ActivityRecord
        {
            ActivityId = "start",
            Name = "ContentSubmittedEvent",
            Properties = JObject.FromObject(new
            {
                ContentTypes = "BlogPost,Article"
            })
        };

        // 2. Notify Reviewers Task
        var notifyReviewers = new ActivityRecord
        {
            ActivityId = "notify-reviewers",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "reviewers@company.com",
                Subject = "Content Review Required: {{ ContentItem.DisplayText }}",
                Body = "A new content item requires your review: {{ ContentItem.DisplayText }}"
            })
        };

        // 3. Wait for Approval Event
        var waitForApproval = new ActivityRecord
        {
            ActivityId = "wait-approval",
            Name = "UserTaskEvent",
            Properties = JObject.FromObject(new
            {
                Title = "Review Content",
                Instructions = "Please review and approve or reject this content",
                PossibleActions = new[] { "Approve", "Reject", "RequestChanges" }
            })
        };

        // 4. Conditional Logic
        var approvalDecision = new ActivityRecord
        {
            ActivityId = "approval-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ UserTask.Action == 'Approve' }}"
            })
        };

        // 5. Publish Content Task
        var publishContent = new ActivityRecord
        {
            ActivityId = "publish-content",
            Name = "PublishContentTask",
            Properties = JObject.FromObject(new
            {
                ContentItemId = "{{ ContentItem.ContentItemId }}"
            })
        };

        // 6. Notify Author - Approved
        var notifyApproved = new ActivityRecord
        {
            ActivityId = "notify-approved",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ ContentItem.Author.Email }}",
                Subject = "Content Approved: {{ ContentItem.DisplayText }}",
                Body = "Your content has been approved and published."
            })
        };

        // 7. Notify Author - Rejected
        var notifyRejected = new ActivityRecord
        {
            ActivityId = "notify-rejected",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ ContentItem.Author.Email }}",
                Subject = "Content Rejected: {{ ContentItem.DisplayText }}",
                Body = "Your content has been rejected. Reason: {{ UserTask.Comments }}"
            })
        };

        // Add activities
        workflowType.Activities.AddRange(new[]
        {
            startEvent, notifyReviewers, waitForApproval, approvalDecision,
            publishContent, notifyApproved, notifyRejected
        });

        // Define transitions
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "start", SourceOutcomeName = "Submitted", DestinationActivityId = "notify-reviewers" },
            new Transition { SourceActivityId = "notify-reviewers", SourceOutcomeName = "Sent", DestinationActivityId = "wait-approval" },
            new Transition { SourceActivityId = "wait-approval", SourceOutcomeName = "Completed", DestinationActivityId = "approval-decision" },
            new Transition { SourceActivityId = "approval-decision", SourceOutcomeName = "True", DestinationActivityId = "publish-content" },
            new Transition { SourceActivityId = "approval-decision", SourceOutcomeName = "False", DestinationActivityId = "notify-rejected" },
            new Transition { SourceActivityId = "publish-content", SourceOutcomeName = "Published", DestinationActivityId = "notify-approved" }
        });

        return workflowType;
    }
}
```

### **2. E-commerce Order Processing Workflow**
```csharp
public class OrderProcessingWorkflowBuilder
{
    public static WorkflowType BuildOrderProcessingWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "OrderProcessingWorkflow",
            Name = "E-commerce Order Processing",
            IsEnabled = true,
            IsSingleton = false,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Order Placed Event
        var orderPlaced = new ActivityRecord
        {
            ActivityId = "order-placed",
            Name = "OrderPlacedEvent"
        };

        // 2. Validate Order
        var validateOrder = new ActivityRecord
        {
            ActivityId = "validate-order",
            Name = "ValidateOrderTask",
            Properties = JObject.FromObject(new
            {
                CheckInventory = true,
                ValidatePayment = true,
                CheckShippingAddress = true
            })
        };

        // 3. Process Payment
        var processPayment = new ActivityRecord
        {
            ActivityId = "process-payment",
            Name = "ProcessPaymentTask",
            Properties = JObject.FromObject(new
            {
                PaymentGateway = "Stripe",
                Amount = "{{ Order.TotalAmount }}",
                Currency = "{{ Order.Currency }}"
            })
        };

        // 4. Reserve Inventory
        var reserveInventory = new ActivityRecord
        {
            ActivityId = "reserve-inventory",
            Name = "ReserveInventoryTask",
            Properties = JObject.FromObject(new
            {
                OrderItems = "{{ Order.Items }}"
            })
        };

        // 5. Parallel: Send Confirmation & Create Shipment
        var parallelTask = new ActivityRecord
        {
            ActivityId = "parallel-processing",
            Name = "ParallelTask",
            Properties = JObject.FromObject(new
            {
                BranchCount = 2,
                WaitForAll = false
            })
        };

        // 6a. Send Order Confirmation
        var sendConfirmation = new ActivityRecord
        {
            ActivityId = "send-confirmation",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Order.CustomerEmail }}",
                Subject = "Order Confirmation #{{ Order.OrderNumber }}",
                Body = "Thank you for your order. We'll process it shortly."
            })
        };

        // 6b. Create Shipment
        var createShipment = new ActivityRecord
        {
            ActivityId = "create-shipment",
            Name = "CreateShipmentTask",
            Properties = JObject.FromObject(new
            {
                OrderId = "{{ Order.OrderId }}",
                ShippingMethod = "{{ Order.ShippingMethod }}"
            })
        };

        // 7. Wait for Shipment
        var waitShipment = new ActivityRecord
        {
            ActivityId = "wait-shipment",
            Name = "ShipmentReadyEvent"
        };

        // 8. Send Tracking Info
        var sendTracking = new ActivityRecord
        {
            ActivityId = "send-tracking",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Order.CustomerEmail }}",
                Subject = "Your Order Has Shipped #{{ Order.OrderNumber }}",
                Body = "Your order is on its way! Tracking: {{ Shipment.TrackingNumber }}"
            })
        };

        // 9. Wait for Delivery
        var waitDelivery = new ActivityRecord
        {
            ActivityId = "wait-delivery",
            Name = "DeliveryConfirmedEvent"
        };

        // 10. Complete Order
        var completeOrder = new ActivityRecord
        {
            ActivityId = "complete-order",
            Name = "CompleteOrderTask"
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            orderPlaced, validateOrder, processPayment, reserveInventory,
            parallelTask, sendConfirmation, createShipment, waitShipment,
            sendTracking, waitDelivery, completeOrder
        });

        // Define transitions
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "order-placed", SourceOutcomeName = "Placed", DestinationActivityId = "validate-order" },
            new Transition { SourceActivityId = "validate-order", SourceOutcomeName = "Valid", DestinationActivityId = "process-payment" },
            new Transition { SourceActivityId = "process-payment", SourceOutcomeName = "Success", DestinationActivityId = "reserve-inventory" },
            new Transition { SourceActivityId = "reserve-inventory", SourceOutcomeName = "Reserved", DestinationActivityId = "parallel-processing" },
            new Transition { SourceActivityId = "parallel-processing", SourceOutcomeName = "Branch1", DestinationActivityId = "send-confirmation" },
            new Transition { SourceActivityId = "parallel-processing", SourceOutcomeName = "Branch2", DestinationActivityId = "create-shipment" },
            new Transition { SourceActivityId = "create-shipment", SourceOutcomeName = "Created", DestinationActivityId = "wait-shipment" },
            new Transition { SourceActivityId = "wait-shipment", SourceOutcomeName = "Ready", DestinationActivityId = "send-tracking" },
            new Transition { SourceActivityId = "send-tracking", SourceOutcomeName = "Sent", DestinationActivityId = "wait-delivery" },
            new Transition { SourceActivityId = "wait-delivery", SourceOutcomeName = "Delivered", DestinationActivityId = "complete-order" }
        });

        return workflowType;
    }
}
```

---

## 🎛️ **ACTIVITY DISPLAY DRIVERS**

### **1. Custom Activity Display Driver**
```csharp
public class SendEmailTaskDisplayDriver : ActivityDisplayDriver<SendEmailTask, SendEmailTaskViewModel>
{
    protected readonly IStringLocalizer S;

    public SendEmailTaskDisplayDriver(IStringLocalizer<SendEmailTaskDisplayDriver> localizer)
    {
        S = localizer;
    }

    public override IDisplayResult Display(SendEmailTask activity, IUpdateModel updater)
    {
        return Combine(
            Shape("SendEmailTask_Fields_Thumbnail", new SendEmailTaskViewModel(activity))
                .Location("Thumbnail", "Content"),
            Shape("SendEmailTask_Fields_Design", new SendEmailTaskViewModel(activity))
                .Location("Design", "Content")
        );
    }

    public override IDisplayResult Edit(SendEmailTask activity, IUpdateModel updater)
    {
        return Initialize<SendEmailTaskViewModel>("SendEmailTask_Fields_Edit", model =>
        {
            model.Recipients = activity.Recipients.Expression;
            model.Subject = activity.Subject.Expression;
            model.Body = activity.Body.Expression;
            model.IsHtml = activity.IsHtml;
        }).Location("Content");
    }

    public override async Task<IDisplayResult> UpdateAsync(SendEmailTask activity, IUpdateModel updater)
    {
        var model = new SendEmailTaskViewModel();
        
        if (await updater.TryUpdateModelAsync(model, Prefix))
        {
            activity.Recipients = new WorkflowExpression<string>(model.Recipients);
            activity.Subject = new WorkflowExpression<string>(model.Subject);
            activity.Body = new WorkflowExpression<string>(model.Body);
            activity.IsHtml = model.IsHtml;
        }

        return Edit(activity, updater);
    }
}

public class SendEmailTaskViewModel
{
    public SendEmailTaskViewModel()
    {
    }

    public SendEmailTaskViewModel(SendEmailTask activity)
    {
        Recipients = activity.Recipients.Expression;
        Subject = activity.Subject.Expression;
        Body = activity.Body.Expression;
        IsHtml = activity.IsHtml;
    }

    public string Recipients { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }
}
```

---

## 📊 **WORKFLOW MONITORING & ANALYTICS**

### **1. Workflow Analytics Service**
```csharp
public class WorkflowAnalyticsService
{
    private readonly IWorkflowStore _workflowStore;
    private readonly ISession _session;
    private readonly ILogger<WorkflowAnalyticsService> _logger;

    public WorkflowAnalyticsService(
        IWorkflowStore workflowStore,
        ISession session,
        ILogger<WorkflowAnalyticsService> logger)
    {
        _workflowStore = workflowStore;
        _session = session;
        _logger = logger;
    }

    public async Task<WorkflowAnalytics> GetAnalyticsAsync(DateTime fromDate, DateTime toDate)
    {
        var workflows = await _workflowStore.ListAsync(w => 
            w.CreatedUtc >= fromDate && w.CreatedUtc <= toDate);

        var analytics = new WorkflowAnalytics
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalWorkflows = workflows.Count(),
            CompletedWorkflows = workflows.Count(w => w.Status == WorkflowStatus.Finished),
            FailedWorkflows = workflows.Count(w => w.Status == WorkflowStatus.Faulted),
            ActiveWorkflows = workflows.Count(w => w.Status == WorkflowStatus.Executing || w.Status == WorkflowStatus.Halted),
            AbortedWorkflows = workflows.Count(w => w.Status == WorkflowStatus.Aborted)
        };

        // Calculate success rate
        analytics.SuccessRate = analytics.TotalWorkflows > 0 
            ? (double)analytics.CompletedWorkflows / analytics.TotalWorkflows * 100 
            : 0;

        // Average execution time
        var completedWorkflows = workflows.Where(w => w.Status == WorkflowStatus.Finished && w.FinishedUtc.HasValue);
        if (completedWorkflows.Any())
        {
            analytics.AverageExecutionTime = completedWorkflows
                .Average(w => (w.FinishedUtc.Value - w.CreatedUtc).TotalMinutes);
        }

        // Workflow type statistics
        analytics.WorkflowTypeStats = workflows
            .GroupBy(w => w.WorkflowTypeId)
            .Select(g => new WorkflowTypeStats
            {
                WorkflowTypeId = g.Key,
                Count = g.Count(),
                SuccessCount = g.Count(w => w.Status == WorkflowStatus.Finished),
                FailureCount = g.Count(w => w.Status == WorkflowStatus.Faulted)
            })
            .ToList();

        // Daily statistics
        analytics.DailyStats = workflows
            .GroupBy(w => w.CreatedUtc.Date)
            .Select(g => new DailyWorkflowStats
            {
                Date = g.Key,
                Count = g.Count(),
                CompletedCount = g.Count(w => w.Status == WorkflowStatus.Finished),
                FailedCount = g.Count(w => w.Status == WorkflowStatus.Faulted)
            })
            .OrderBy(s => s.Date)
            .ToList();

        return analytics;
    }

    public async Task<IEnumerable<WorkflowPerformanceMetric>> GetPerformanceMetricsAsync()
    {
        // Query for performance metrics using raw SQL for better performance
        var sql = @"
            SELECT 
                WorkflowTypeId,
                COUNT(*) as TotalExecutions,
                AVG(DATEDIFF(minute, CreatedUtc, FinishedUtc)) as AvgExecutionTimeMinutes,
                MIN(DATEDIFF(minute, CreatedUtc, FinishedUtc)) as MinExecutionTimeMinutes,
                MAX(DATEDIFF(minute, CreatedUtc, FinishedUtc)) as MaxExecutionTimeMinutes,
                COUNT(CASE WHEN Status = 'Finished' THEN 1 END) as SuccessCount,
                COUNT(CASE WHEN Status = 'Faulted' THEN 1 END) as FailureCount
            FROM Workflows 
            WHERE FinishedUtc IS NOT NULL 
                AND CreatedUtc >= DATEADD(day, -30, GETDATE())
            GROUP BY WorkflowTypeId
            ORDER BY TotalExecutions DESC";

        var results = await _session.CreateSQLQuery(sql)
            .SetResultTransformer(Transformers.AliasToBean<WorkflowPerformanceMetric>())
            .ListAsync<WorkflowPerformanceMetric>();

        return results;
    }

    public async Task<IEnumerable<ActivityPerformanceMetric>> GetActivityPerformanceAsync(string workflowTypeId)
    {
        // This would require storing activity execution times
        // Implementation depends on how you track activity performance
        var workflows = await _workflowStore.ListAsync(w => 
            w.WorkflowTypeId == workflowTypeId && 
            w.Status == WorkflowStatus.Finished);

        // Analyze activity performance from workflow execution logs
        var metrics = new List<ActivityPerformanceMetric>();
        
        foreach (var workflow in workflows)
        {
            var state = workflow.State.ToObject<WorkflowState>();
            // Extract activity performance data from state
            // This is a simplified example
        }

        return metrics;
    }
}

public class WorkflowAnalytics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalWorkflows { get; set; }
    public int CompletedWorkflows { get; set; }
    public int FailedWorkflows { get; set; }
    public int ActiveWorkflows { get; set; }
    public int AbortedWorkflows { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public List<WorkflowTypeStats> WorkflowTypeStats { get; set; } = new();
    public List<DailyWorkflowStats> DailyStats { get; set; } = new();
}

public class WorkflowTypeStats
{
    public string WorkflowTypeId { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class DailyWorkflowStats
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
}

public class WorkflowPerformanceMetric
{
    public string WorkflowTypeId { get; set; }
    public int TotalExecutions { get; set; }
    public double AvgExecutionTimeMinutes { get; set; }
    public double MinExecutionTimeMinutes { get; set; }
    public double MaxExecutionTimeMinutes { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class ActivityPerformanceMetric
{
    public string ActivityId { get; set; }
    public string ActivityName { get; set; }
    public double AvgExecutionTimeMs { get; set; }
    public int ExecutionCount { get; set; }
    public int FailureCount { get; set; }
}
```

---

## 🔄 **BEST PRACTICES**

### **1. Activity Development**
- **Single Responsibility**: Mỗi activity chỉ làm một việc cụ thể
- **Idempotent**: Activities nên có thể chạy lại mà không gây side effects
- **Error Handling**: Luôn handle exceptions và return appropriate outcomes
- **Logging**: Log chi tiết để debug và monitor
- **Performance**: Avoid long-running operations trong activities

### **2. Workflow Design**
- **Modular**: Chia workflows thành các sub-workflows nhỏ
- **Testable**: Design workflows để dễ test từng phần
- **Recoverable**: Handle failures và có recovery mechanisms
- **Scalable**: Consider performance với large volumes
- **Maintainable**: Document workflows và business logic

### **3. Error Handling**
- **Graceful Degradation**: Workflows nên handle partial failures
- **Retry Logic**: Implement retry cho transient failures
- **Dead Letter Queue**: Handle permanently failed workflows
- **Alerting**: Monitor và alert khi workflows fail
- **Rollback**: Implement compensation activities khi cần

### **4. Performance Optimization**
- **Async Operations**: Use async/await throughout
- **Batch Processing**: Group similar operations
- **Caching**: Cache frequently accessed data
- **Database Optimization**: Optimize workflow storage queries
- **Resource Management**: Properly dispose resources

---

## 🔄 **TODO & FUTURE ENHANCEMENTS**

### **Planned Features:**
- [ ] Visual workflow designer
- [ ] Workflow versioning system
- [ ] Advanced scheduling capabilities
- [ ] Workflow templates marketplace
- [ ] Real-time workflow monitoring
- [ ] Machine learning for workflow optimization
- [ ] Integration with external workflow engines
- [ ] Advanced debugging tools

---

## 🏢 **ỨNG DỤNG THỰC TẾ TRONG CÁC DỰ ÁN**

### **1. 🏥 Hospital Patient Management System**

#### **Tình huống:**
Xây dựng **hệ thống quản lý bệnh nhân** với quy trình từ nhập viện đến xuất viện, bao gồm các bước: đăng ký, khám bệnh, xét nghiệm, điều trị, xuất viện.

#### **Ứng dụng Workflow Integration:**

```csharp
// Patient Admission Workflow
public class PatientAdmissionWorkflow
{
    public static WorkflowType BuildPatientAdmissionWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "PatientAdmissionWorkflow",
            Name = "Patient Admission Process",
            IsEnabled = true,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Patient Registration Event
        var patientRegistered = new ActivityRecord
        {
            ActivityId = "patient-registered",
            Name = "PatientRegisteredEvent",
            Properties = JObject.FromObject(new
            {
                TriggerConditions = "PatientType == 'Inpatient'"
            })
        };

        // 2. Assign Room Task
        var assignRoom = new ActivityRecord
        {
            ActivityId = "assign-room",
            Name = "AssignRoomTask",
            Properties = JObject.FromObject(new
            {
                RoomType = "{{ Patient.RequiredRoomType }}",
                Department = "{{ Patient.Department }}",
                Priority = "{{ Patient.Priority }}"
            })
        };

        // 3. Create Medical Record
        var createMedicalRecord = new ActivityRecord
        {
            ActivityId = "create-medical-record",
            Name = "CreateMedicalRecordTask",
            Properties = JObject.FromObject(new
            {
                PatientId = "{{ Patient.PatientId }}",
                AdmissionDate = "{{ Patient.AdmissionDate }}",
                AttendingPhysician = "{{ Patient.AttendingPhysician }}"
            })
        };

        // 4. Notify Medical Staff
        var notifyStaff = new ActivityRecord
        {
            ActivityId = "notify-medical-staff",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Patient.AttendingPhysician.Email }}, nursing-station@hospital.com",
                Subject = "New Patient Admission: {{ Patient.FullName }}",
                Body = @"
                    New patient admitted:
                    Name: {{ Patient.FullName }}
                    Room: {{ AssignedRoom.RoomNumber }}
                    Condition: {{ Patient.Condition }}
                    Special Instructions: {{ Patient.SpecialInstructions }}
                "
            })
        };

        // 5. Schedule Initial Assessment
        var scheduleAssessment = new ActivityRecord
        {
            ActivityId = "schedule-assessment",
            Name = "ScheduleAppointmentTask",
            Properties = JObject.FromObject(new
            {
                AppointmentType = "Initial Assessment",
                ScheduledFor = "{{ Patient.AdmissionDate.AddHours(2) }}",
                Duration = 60,
                AssignedStaff = "{{ Patient.AttendingPhysician }}"
            })
        };

        // 6. Wait for Assessment Completion
        var waitAssessment = new ActivityRecord
        {
            ActivityId = "wait-assessment",
            Name = "AssessmentCompletedEvent"
        };

        // 7. Create Treatment Plan
        var createTreatmentPlan = new ActivityRecord
        {
            ActivityId = "create-treatment-plan",
            Name = "CreateTreatmentPlanTask",
            Properties = JObject.FromObject(new
            {
                PatientId = "{{ Patient.PatientId }}",
                Diagnosis = "{{ Assessment.Diagnosis }}",
                TreatmentProtocol = "{{ Assessment.RecommendedTreatment }}"
            })
        };

        // 8. Parallel: Schedule Tests & Medications
        var parallelScheduling = new ActivityRecord
        {
            ActivityId = "parallel-scheduling",
            Name = "ParallelTask",
            Properties = JObject.FromObject(new
            {
                BranchCount = 2,
                WaitForAll = true
            })
        };

        // 8a. Schedule Lab Tests
        var scheduleTests = new ActivityRecord
        {
            ActivityId = "schedule-tests",
            Name = "ScheduleLabTestsTask",
            Properties = JObject.FromObject(new
            {
                PatientId = "{{ Patient.PatientId }}",
                RequiredTests = "{{ TreatmentPlan.RequiredTests }}",
                Priority = "{{ Patient.Priority }}"
            })
        };

        // 8b. Setup Medication Schedule
        var setupMedications = new ActivityRecord
        {
            ActivityId = "setup-medications",
            Name = "SetupMedicationScheduleTask",
            Properties = JObject.FromObject(new
            {
                PatientId = "{{ Patient.PatientId }}",
                Medications = "{{ TreatmentPlan.Medications }}",
                StartDate = "{{ DateTime.UtcNow }}"
            })
        };

        // 9. Monitor Patient Status (Timer Event)
        var monitorPatient = new ActivityRecord
        {
            ActivityId = "monitor-patient",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                Interval = "PT4H", // Every 4 hours
                IsRecurring = true
            })
        };

        // 10. Check Discharge Criteria
        var checkDischarge = new ActivityRecord
        {
            ActivityId = "check-discharge",
            Name = "CheckDischargeCriteriaTask",
            Properties = JObject.FromObject(new
            {
                PatientId = "{{ Patient.PatientId }}",
                DischargeCriteria = "{{ TreatmentPlan.DischargeCriteria }}"
            })
        };

        // 11. Discharge Process
        var dischargePatient = new ActivityRecord
        {
            ActivityId = "discharge-patient",
            Name = "DischargePatientTask",
            Properties = JObject.FromObject(new
            {
                PatientId = "{{ Patient.PatientId }}",
                DischargeInstructions = "{{ TreatmentPlan.DischargeInstructions }}"
            })
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            patientRegistered, assignRoom, createMedicalRecord, notifyStaff,
            scheduleAssessment, waitAssessment, createTreatmentPlan,
            parallelScheduling, scheduleTests, setupMedications,
            monitorPatient, checkDischarge, dischargePatient
        });

        // Define transitions
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "patient-registered", SourceOutcomeName = "Registered", DestinationActivityId = "assign-room" },
            new Transition { SourceActivityId = "assign-room", SourceOutcomeName = "Assigned", DestinationActivityId = "create-medical-record" },
            new Transition { SourceActivityId = "create-medical-record", SourceOutcomeName = "Created", DestinationActivityId = "notify-staff" },
            new Transition { SourceActivityId = "notify-staff", SourceOutcomeName = "Sent", DestinationActivityId = "schedule-assessment" },
            new Transition { SourceActivityId = "schedule-assessment", SourceOutcomeName = "Scheduled", DestinationActivityId = "wait-assessment" },
            new Transition { SourceActivityId = "wait-assessment", SourceOutcomeName = "Completed", DestinationActivityId = "create-treatment-plan" },
            new Transition { SourceActivityId = "create-treatment-plan", SourceOutcomeName = "Created", DestinationActivityId = "parallel-scheduling" },
            new Transition { SourceActivityId = "parallel-scheduling", SourceOutcomeName = "Branch1", DestinationActivityId = "schedule-tests" },
            new Transition { SourceActivityId = "parallel-scheduling", SourceOutcomeName = "Branch2", DestinationActivityId = "setup-medications" },
            new Transition { SourceActivityId = "parallel-scheduling", SourceOutcomeName = "AllCompleted", DestinationActivityId = "monitor-patient" },
            new Transition { SourceActivityId = "monitor-patient", SourceOutcomeName = "Timer", DestinationActivityId = "check-discharge" },
            new Transition { SourceActivityId = "check-discharge", SourceOutcomeName = "ReadyForDischarge", DestinationActivityId = "discharge-patient" },
            new Transition { SourceActivityId = "check-discharge", SourceOutcomeName = "ContinueTreatment", DestinationActivityId = "monitor-patient" }
        });

        return workflowType;
    }
}

// Custom Activities cho Hospital System
public class AssignRoomTask : TaskActivity<AssignRoomTask>
{
    private readonly IRoomManagementService _roomService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public AssignRoomTask(
        IRoomManagementService roomService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<AssignRoomTask> localizer)
    {
        _roomService = roomService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Assign Room Task"];
    public override LocalizedString Category => S["Hospital Management"];

    public WorkflowExpression<string> RoomType
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Department
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Priority
    {
        get => GetProperty(() => new WorkflowExpression<string>("Normal"));
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Assigned"], S["NoRoomAvailable"], S["WaitingList"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var roomType = await _expressionEvaluator.EvaluateAsync(RoomType, workflowContext, null);
            var department = await _expressionEvaluator.EvaluateAsync(Department, workflowContext, null);
            var priority = await _expressionEvaluator.EvaluateAsync(Priority, workflowContext, null);

            var roomRequest = new RoomAssignmentRequest
            {
                RoomType = roomType,
                Department = department,
                Priority = priority,
                PatientId = workflowContext.Input["PatientId"]?.ToString()
            };

            var assignmentResult = await _roomService.AssignRoomAsync(roomRequest);

            if (assignmentResult.Success)
            {
                workflowContext.Properties["AssignedRoom"] = assignmentResult.AssignedRoom;
                workflowContext.Properties["RoomAssignedAt"] = DateTime.UtcNow;
                return Outcomes("Assigned");
            }
            else if (assignmentResult.CanWaitlist)
            {
                workflowContext.Properties["WaitlistPosition"] = assignmentResult.WaitlistPosition;
                return Outcomes("WaitingList");
            }
            else
            {
                workflowContext.Properties["RoomAssignmentError"] = assignmentResult.ErrorMessage;
                return Outcomes("NoRoomAvailable");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["RoomAssignmentError"] = ex.Message;
            return Outcomes("NoRoomAvailable");
        }
    }
}

public class CheckDischargeCriteriaTask : TaskActivity<CheckDischargeCriteriaTask>
{
    private readonly IPatientService _patientService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public CheckDischargeCriteriaTask(
        IPatientService patientService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<CheckDischargeCriteriaTask> localizer)
    {
        _patientService = patientService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Check Discharge Criteria Task"];
    public override LocalizedString Category => S["Hospital Management"];

    public WorkflowExpression<string> PatientId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["ReadyForDischarge"], S["ContinueTreatment"], S["RequiresReview"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var patientId = await _expressionEvaluator.EvaluateAsync(PatientId, workflowContext, null);
            var patient = await _patientService.GetPatientAsync(patientId);

            if (patient == null)
            {
                workflowContext.Properties["DischargeCheckError"] = "Patient not found";
                return Outcomes("RequiresReview");
            }

            // Check discharge criteria
            var dischargeEvaluation = await _patientService.EvaluateDischargeReadinessAsync(patientId);

            workflowContext.Properties["DischargeEvaluation"] = dischargeEvaluation;
            workflowContext.Properties["DischargeCheckPerformedAt"] = DateTime.UtcNow;

            if (dischargeEvaluation.IsReadyForDischarge)
            {
                workflowContext.Properties["EstimatedDischargeTime"] = dischargeEvaluation.EstimatedDischargeTime;
                return Outcomes("ReadyForDischarge");
            }
            else if (dischargeEvaluation.RequiresPhysicianReview)
            {
                workflowContext.Properties["ReviewReason"] = dischargeEvaluation.ReviewReason;
                return Outcomes("RequiresReview");
            }
            else
            {
                workflowContext.Properties["ContinueTreatmentReason"] = dischargeEvaluation.ContinueTreatmentReason;
                workflowContext.Properties["NextEvaluationTime"] = dischargeEvaluation.NextEvaluationTime;
                return Outcomes("ContinueTreatment");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["DischargeCheckError"] = ex.Message;
            return Outcomes("RequiresReview");
        }
    }
}
```

#### **Kết quả:**
- ✅ **Tự động hóa quy trình**: Từ nhập viện đến xuất viện
- ✅ **Theo dõi real-time**: Monitor tình trạng bệnh nhân 24/7
- ✅ **Thông báo tự động**: Alert medical staff khi cần
- ✅ **Tích hợp hệ thống**: Kết nối với HIS, LIS, RIS
- ✅ **Audit trail**: Lưu lại toàn bộ quá trình điều trị

### **2. 🏭 Manufacturing Quality Control System**

#### **Tình huống:**
Xây dựng **hệ thống kiểm soát chất lượng sản xuất** với quy trình: nhận nguyên liệu, kiểm tra chất lượng, sản xuất, kiểm tra thành phẩm, đóng gói, xuất kho.

#### **Ứng dụng Workflow Integration:**

```csharp
// Quality Control Workflow
public class QualityControlWorkflow
{
    public static WorkflowType BuildQualityControlWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "QualityControlWorkflow",
            Name = "Manufacturing Quality Control Process",
            IsEnabled = true,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Raw Material Received Event
        var materialReceived = new ActivityRecord
        {
            ActivityId = "material-received",
            Name = "MaterialReceivedEvent",
            Properties = JObject.FromObject(new
            {
                MaterialTypes = "RawMaterial,Component"
            })
        };

        // 2. Quality Inspection Task
        var qualityInspection = new ActivityRecord
        {
            ActivityId = "quality-inspection",
            Name = "QualityInspectionTask",
            Properties = JObject.FromObject(new
            {
                InspectionType = "Incoming",
                MaterialBatch = "{{ Material.BatchNumber }}",
                QualityStandards = "{{ Material.QualityStandards }}",
                SamplingRate = "{{ Material.SamplingRate }}"
            })
        };

        // 3. Quality Decision
        var qualityDecision = new ActivityRecord
        {
            ActivityId = "quality-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ QualityInspection.Result == 'Pass' }}"
            })
        };

        // 4a. Accept Material
        var acceptMaterial = new ActivityRecord
        {
            ActivityId = "accept-material",
            Name = "AcceptMaterialTask",
            Properties = JObject.FromObject(new
            {
                MaterialBatch = "{{ Material.BatchNumber }}",
                StorageLocation = "{{ Material.StorageLocation }}",
                ExpiryDate = "{{ Material.ExpiryDate }}"
            })
        };

        // 4b. Reject Material
        var rejectMaterial = new ActivityRecord
        {
            ActivityId = "reject-material",
            Name = "RejectMaterialTask",
            Properties = JObject.FromObject(new
            {
                MaterialBatch = "{{ Material.BatchNumber }}",
                RejectionReason = "{{ QualityInspection.FailureReason }}",
                SupplierNotification = true
            })
        };

        // 5. Production Order Event
        var productionOrder = new ActivityRecord
        {
            ActivityId = "production-order",
            Name = "ProductionOrderEvent"
        };

        // 6. Pre-Production Check
        var preProductionCheck = new ActivityRecord
        {
            ActivityId = "pre-production-check",
            Name = "PreProductionCheckTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                RequiredMaterials = "{{ ProductionOrder.RequiredMaterials }}",
                EquipmentCheck = true,
                EnvironmentalCheck = true
            })
        };

        // 7. Start Production
        var startProduction = new ActivityRecord
        {
            ActivityId = "start-production",
            Name = "StartProductionTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                ProductionLine = "{{ ProductionOrder.ProductionLine }}",
                EstimatedDuration = "{{ ProductionOrder.EstimatedDuration }}"
            })
        };

        // 8. In-Process Quality Checks (Timer)
        var inProcessChecks = new ActivityRecord
        {
            ActivityId = "in-process-checks",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                Interval = "PT1H", // Every hour
                IsRecurring = true
            })
        };

        // 9. In-Process Quality Test
        var inProcessTest = new ActivityRecord
        {
            ActivityId = "in-process-test",
            Name = "InProcessQualityTestTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                TestParameters = "{{ ProductionOrder.QualityParameters }}",
                SampleSize = "{{ ProductionOrder.SampleSize }}"
            })
        };

        // 10. Process Adjustment Decision
        var processAdjustment = new ActivityRecord
        {
            ActivityId = "process-adjustment",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ InProcessTest.Result == 'OutOfSpec' }}"
            })
        };

        // 11. Adjust Process
        var adjustProcess = new ActivityRecord
        {
            ActivityId = "adjust-process",
            Name = "AdjustProcessTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                AdjustmentType = "{{ InProcessTest.RecommendedAdjustment }}",
                NotifyOperator = true
            })
        };

        // 12. Production Completed Event
        var productionCompleted = new ActivityRecord
        {
            ActivityId = "production-completed",
            Name = "ProductionCompletedEvent"
        };

        // 13. Final Quality Inspection
        var finalInspection = new ActivityRecord
        {
            ActivityId = "final-inspection",
            Name = "FinalQualityInspectionTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                InspectionType = "Final",
                TestingProtocol = "{{ Product.TestingProtocol }}",
                CertificationRequired = "{{ Product.CertificationRequired }}"
            })
        };

        // 14. Final Quality Decision
        var finalDecision = new ActivityRecord
        {
            ActivityId = "final-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ FinalInspection.Result == 'Pass' }}"
            })
        };

        // 15a. Release Product
        var releaseProduct = new ActivityRecord
        {
            ActivityId = "release-product",
            Name = "ReleaseProductTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                QualityCertificate = "{{ FinalInspection.CertificateNumber }}",
                ReleaseDate = "{{ DateTime.UtcNow }}"
            })
        };

        // 15b. Quarantine Product
        var quarantineProduct = new ActivityRecord
        {
            ActivityId = "quarantine-product",
            Name = "QuarantineProductTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                QuarantineReason = "{{ FinalInspection.FailureReason }}",
                ReviewRequired = true
            })
        };

        // 16. Generate Quality Report
        var generateReport = new ActivityRecord
        {
            ActivityId = "generate-report",
            Name = "GenerateQualityReportTask",
            Properties = JObject.FromObject(new
            {
                ProductionOrderId = "{{ ProductionOrder.OrderId }}",
                IncludeTestResults = true,
                IncludeStatistics = true,
                ReportFormat = "PDF"
            })
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            materialReceived, qualityInspection, qualityDecision,
            acceptMaterial, rejectMaterial, productionOrder,
            preProductionCheck, startProduction, inProcessChecks,
            inProcessTest, processAdjustment, adjustProcess,
            productionCompleted, finalInspection, finalDecision,
            releaseProduct, quarantineProduct, generateReport
        });

        // Define transitions (simplified for brevity)
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "material-received", SourceOutcomeName = "Received", DestinationActivityId = "quality-inspection" },
            new Transition { SourceActivityId = "quality-inspection", SourceOutcomeName = "Completed", DestinationActivityId = "quality-decision" },
            new Transition { SourceActivityId = "quality-decision", SourceOutcomeName = "True", DestinationActivityId = "accept-material" },
            new Transition { SourceActivityId = "quality-decision", SourceOutcomeName = "False", DestinationActivityId = "reject-material" },
            new Transition { SourceActivityId = "accept-material", SourceOutcomeName = "Accepted", DestinationActivityId = "production-order" },
            new Transition { SourceActivityId = "production-order", SourceOutcomeName = "Created", DestinationActivityId = "pre-production-check" },
            new Transition { SourceActivityId = "pre-production-check", SourceOutcomeName = "Passed", DestinationActivityId = "start-production" },
            new Transition { SourceActivityId = "start-production", SourceOutcomeName = "Started", DestinationActivityId = "in-process-checks" },
            new Transition { SourceActivityId = "in-process-checks", SourceOutcomeName = "Timer", DestinationActivityId = "in-process-test" },
            new Transition { SourceActivityId = "in-process-test", SourceOutcomeName = "Completed", DestinationActivityId = "process-adjustment" },
            new Transition { SourceActivityId = "process-adjustment", SourceOutcomeName = "True", DestinationActivityId = "adjust-process" },
            new Transition { SourceActivityId = "process-adjustment", SourceOutcomeName = "False", DestinationActivityId = "in-process-checks" },
            new Transition { SourceActivityId = "adjust-process", SourceOutcomeName = "Adjusted", DestinationActivityId = "in-process-checks" },
            new Transition { SourceActivityId = "production-completed", SourceOutcomeName = "Completed", DestinationActivityId = "final-inspection" },
            new Transition { SourceActivityId = "final-inspection", SourceOutcomeName = "Completed", DestinationActivityId = "final-decision" },
            new Transition { SourceActivityId = "final-decision", SourceOutcomeName = "True", DestinationActivityId = "release-product" },
            new Transition { SourceActivityId = "final-decision", SourceOutcomeName = "False", DestinationActivityId = "quarantine-product" },
            new Transition { SourceActivityId = "release-product", SourceOutcomeName = "Released", DestinationActivityId = "generate-report" },
            new Transition { SourceActivityId = "quarantine-product", SourceOutcomeName = "Quarantined", DestinationActivityId = "generate-report" }
        });

        return workflowType;
    }
}

// Custom Quality Control Activities
public class QualityInspectionTask : TaskActivity<QualityInspectionTask>
{
    private readonly IQualityControlService _qualityService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public QualityInspectionTask(
        IQualityControlService qualityService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<QualityInspectionTask> localizer)
    {
        _qualityService = qualityService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Quality Inspection Task"];
    public override LocalizedString Category => S["Quality Control"];

    public WorkflowExpression<string> InspectionType
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> MaterialBatch
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> QualityStandards
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Completed"], S["Failed"], S["RequiresRetest"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var inspectionType = await _expressionEvaluator.EvaluateAsync(InspectionType, workflowContext, null);
            var materialBatch = await _expressionEvaluator.EvaluateAsync(MaterialBatch, workflowContext, null);
            var qualityStandards = await _expressionEvaluator.EvaluateAsync(QualityStandards, workflowContext, null);

            var inspectionRequest = new QualityInspectionRequest
            {
                InspectionType = inspectionType,
                MaterialBatch = materialBatch,
                QualityStandards = qualityStandards,
                InspectionDate = DateTime.UtcNow
            };

            var inspectionResult = await _qualityService.PerformInspectionAsync(inspectionRequest);

            // Store inspection results
            workflowContext.Properties["QualityInspection"] = inspectionResult;
            workflowContext.Properties["InspectionPerformedAt"] = DateTime.UtcNow;
            workflowContext.Properties["InspectionId"] = inspectionResult.InspectionId;

            if (inspectionResult.Status == QualityInspectionStatus.Passed)
            {
                return Outcomes("Completed");
            }
            else if (inspectionResult.Status == QualityInspectionStatus.Failed)
            {
                workflowContext.Properties["FailureReason"] = inspectionResult.FailureReason;
                return Outcomes("Failed");
            }
            else
            {
                workflowContext.Properties["RetestReason"] = inspectionResult.RetestReason;
                return Outcomes("RequiresRetest");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["InspectionError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}

public class AdjustProcessTask : TaskActivity<AdjustProcessTask>
{
    private readonly IProductionControlService _productionService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public AdjustProcessTask(
        IProductionControlService productionService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<AdjustProcessTask> localizer)
    {
        _productionService = productionService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Adjust Process Task"];
    public override LocalizedString Category => S["Production Control"];

    public WorkflowExpression<string> ProductionOrderId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> AdjustmentType
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public bool NotifyOperator
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Adjusted"], S["AdjustmentFailed"], S["RequiresManualIntervention"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var productionOrderId = await _expressionEvaluator.EvaluateAsync(ProductionOrderId, workflowContext, null);
            var adjustmentType = await _expressionEvaluator.EvaluateAsync(AdjustmentType, workflowContext, null);

            var adjustmentRequest = new ProcessAdjustmentRequest
            {
                ProductionOrderId = productionOrderId,
                AdjustmentType = adjustmentType,
                RequestedBy = "QualityControlWorkflow",
                RequestedAt = DateTime.UtcNow
            };

            var adjustmentResult = await _productionService.AdjustProcessAsync(adjustmentRequest);

            workflowContext.Properties["ProcessAdjustment"] = adjustmentResult;
            workflowContext.Properties["AdjustmentPerformedAt"] = DateTime.UtcNow;

            if (NotifyOperator && adjustmentResult.RequiresOperatorAttention)
            {
                // Send notification to operator
                await NotifyOperatorAsync(productionOrderId, adjustmentType, adjustmentResult);
            }

            switch (adjustmentResult.Status)
            {
                case ProcessAdjustmentStatus.Success:
                    return Outcomes("Adjusted");
                case ProcessAdjustmentStatus.Failed:
                    workflowContext.Properties["AdjustmentError"] = adjustmentResult.ErrorMessage;
                    return Outcomes("AdjustmentFailed");
                case ProcessAdjustmentStatus.RequiresManualIntervention:
                    workflowContext.Properties["ManualInterventionReason"] = adjustmentResult.ManualInterventionReason;
                    return Outcomes("RequiresManualIntervention");
                default:
                    return Outcomes("AdjustmentFailed");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["AdjustmentError"] = ex.Message;
            return Outcomes("AdjustmentFailed");
        }
    }

    private async Task NotifyOperatorAsync(string productionOrderId, string adjustmentType, ProcessAdjustmentResult result)
    {
        // Implementation to notify production operator
        // Could send email, SMS, or push notification to production dashboard
        await Task.CompletedTask;
    }
}
```

#### **Kết quả:**
- ✅ **Kiểm soát chất lượng tự động**: Từ nguyên liệu đến thành phẩm
- ✅ **Real-time monitoring**: Theo dõi quá trình sản xuất liên tục
- ✅ **Tự động điều chỉnh**: Process adjustment khi phát hiện lỗi
- ✅ **Traceability**: Theo dõi được toàn bộ quá trình sản xuất
- ✅ **Compliance**: Đáp ứng các tiêu chuẩn chất lượng ISO, FDA

### **3. 🏦 Banking Loan Approval System**

#### **Tình huống:**
Xây dựng **hệ thống phê duyệt khoản vay ngân hàng** với quy trình: nộp đơn, xác minh thông tin, đánh giá tín dụng, phê duyệt, giải ngân.

#### **Ứng dụng Workflow Integration:**

```csharp
// Loan Approval Workflow
public class LoanApprovalWorkflow
{
    public static WorkflowType BuildLoanApprovalWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "LoanApprovalWorkflow",
            Name = "Bank Loan Approval Process",
            IsEnabled = true,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Loan Application Submitted
        var applicationSubmitted = new ActivityRecord
        {
            ActivityId = "application-submitted",
            Name = "LoanApplicationSubmittedEvent"
        };

        // 2. Initial Validation
        var initialValidation = new ActivityRecord
        {
            ActivityId = "initial-validation",
            Name = "ValidateLoanApplicationTask",
            Properties = JObject.FromObject(new
            {
                RequiredDocuments = "ID,IncomeProof,BankStatements",
                MinimumIncome = "{{ LoanType.MinimumIncome }}",
                MaximumLoanAmount = "{{ LoanType.MaximumAmount }}"
            })
        };

        // 3. Document Verification
        var documentVerification = new ActivityRecord
        {
            ActivityId = "document-verification",
            Name = "VerifyDocumentsTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                VerificationLevel = "Standard",
                ExternalVerification = true
            })
        };

        // 4. Credit Score Check
        var creditScoreCheck = new ActivityRecord
        {
            ActivityId = "credit-score-check",
            Name = "CheckCreditScoreTask",
            Properties = JObject.FromObject(new
            {
                ApplicantId = "{{ LoanApplication.ApplicantId }}",
                CreditBureaus = "Experian,Equifax,TransUnion",
                IncludeHistory = true
            })
        };

        // 5. Risk Assessment
        var riskAssessment = new ActivityRecord
        {
            ActivityId = "risk-assessment",
            Name = "PerformRiskAssessmentTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                CreditScore = "{{ CreditCheck.Score }}",
                IncomeToDebtRatio = "{{ LoanApplication.IncomeToDebtRatio }}",
                LoanAmount = "{{ LoanApplication.RequestedAmount }}"
            })
        };

        // 6. Automated Decision
        var automatedDecision = new ActivityRecord
        {
            ActivityId = "automated-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = @"
                    {{ CreditCheck.Score >= 700 && 
                       RiskAssessment.RiskLevel == 'Low' && 
                       LoanApplication.RequestedAmount <= 50000 }}"
            })
        };

        // 7a. Auto Approve
        var autoApprove = new ActivityRecord
        {
            ActivityId = "auto-approve",
            Name = "ApproveLoanTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                ApprovalType = "Automated",
                InterestRate = "{{ RiskAssessment.RecommendedRate }}",
                Terms = "{{ LoanApplication.RequestedTerms }}"
            })
        };

        // 7b. Manual Review Required
        var manualReview = new ActivityRecord
        {
            ActivityId = "manual-review",
            Name = "AssignToUnderwriterTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                Priority = "{{ RiskAssessment.RiskLevel }}",
                ReviewType = "Standard"
            })
        };

        // 8. Wait for Underwriter Decision
        var waitUnderwriterDecision = new ActivityRecord
        {
            ActivityId = "wait-underwriter-decision",
            Name = "UnderwriterDecisionEvent"
        };

        // 9. Underwriter Decision Logic
        var underwriterDecision = new ActivityRecord
        {
            ActivityId = "underwriter-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ UnderwriterDecision.Decision == 'Approved' }}"
            })
        };

        // 10a. Manual Approve
        var manualApprove = new ActivityRecord
        {
            ActivityId = "manual-approve",
            Name = "ApproveLoanTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                ApprovalType = "Manual",
                InterestRate = "{{ UnderwriterDecision.InterestRate }}",
                Terms = "{{ UnderwriterDecision.Terms }}",
                Conditions = "{{ UnderwriterDecision.Conditions }}"
            })
        };

        // 10b. Reject Application
        var rejectApplication = new ActivityRecord
        {
            ActivityId = "reject-application",
            Name = "RejectLoanTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                RejectionReason = "{{ UnderwriterDecision.RejectionReason }}",
                AppealProcess = true
            })
        };

        // 11. Generate Loan Agreement
        var generateAgreement = new ActivityRecord
        {
            ActivityId = "generate-agreement",
            Name = "GenerateLoanAgreementTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                LoanTerms = "{{ LoanApproval.Terms }}",
                InterestRate = "{{ LoanApproval.InterestRate }}",
                Template = "StandardLoanAgreement"
            })
        };

        // 12. Send for Customer Signature
        var sendForSignature = new ActivityRecord
        {
            ActivityId = "send-for-signature",
            Name = "SendForDigitalSignatureTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                DocumentId = "{{ LoanAgreement.DocumentId }}",
                SignerEmail = "{{ LoanApplication.ApplicantEmail }}",
                ExpirationDays = 7
            })
        };

        // 13. Wait for Customer Signature
        var waitSignature = new ActivityRecord
        {
            ActivityId = "wait-signature",
            Name = "DocumentSignedEvent"
        };

        // 14. Final Verification
        var finalVerification = new ActivityRecord
        {
            ActivityId = "final-verification",
            Name = "FinalVerificationTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                VerifyEmployment = true,
                VerifyBankAccount = true,
                VerifyInsurance = "{{ LoanType.RequiresInsurance }}"
            })
        };

        // 15. Disburse Loan
        var disburseLoan = new ActivityRecord
        {
            ActivityId = "disburse-loan",
            Name = "DisburseLoanTask",
            Properties = JObject.FromObject(new
            {
                ApplicationId = "{{ LoanApplication.ApplicationId }}",
                Amount = "{{ LoanApproval.ApprovedAmount }}",
                DestinationAccount = "{{ LoanApplication.DisbursementAccount }}",
                DisbursementMethod = "{{ LoanApplication.DisbursementMethod }}"
            })
        };

        // 16. Setup Loan Servicing
        var setupServicing = new ActivityRecord
        {
            ActivityId = "setup-servicing",
            Name = "SetupLoanServicingTask",
            Properties = JObject.FromObject(new
            {
                LoanId = "{{ Loan.LoanId }}",
                PaymentSchedule = "{{ LoanApproval.PaymentSchedule }}",
                AutoPaySetup = "{{ LoanApplication.AutoPayRequested }}"
            })
        };

        // 17. Send Welcome Package
        var sendWelcomePackage = new ActivityRecord
        {
            ActivityId = "send-welcome-package",
            Name = "SendWelcomePackageTask",
            Properties = JObject.FromObject(new
            {
                LoanId = "{{ Loan.LoanId }}",
                CustomerEmail = "{{ LoanApplication.ApplicantEmail }}",
                IncludePaymentInfo = true,
                IncludeOnlineAccess = true
            })
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            applicationSubmitted, initialValidation, documentVerification,
            creditScoreCheck, riskAssessment, automatedDecision,
            autoApprove, manualReview, waitUnderwriterDecision,
            underwriterDecision, manualApprove, rejectApplication,
            generateAgreement, sendForSignature, waitSignature,
            finalVerification, disburseLoan, setupServicing, sendWelcomePackage
        });

        // Define transitions (simplified)
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "application-submitted", SourceOutcomeName = "Submitted", DestinationActivityId = "initial-validation" },
            new Transition { SourceActivityId = "initial-validation", SourceOutcomeName = "Valid", DestinationActivityId = "document-verification" },
            new Transition { SourceActivityId = "document-verification", SourceOutcomeName = "Verified", DestinationActivityId = "credit-score-check" },
            new Transition { SourceActivityId = "credit-score-check", SourceOutcomeName = "Completed", DestinationActivityId = "risk-assessment" },
            new Transition { SourceActivityId = "risk-assessment", SourceOutcomeName = "Completed", DestinationActivityId = "automated-decision" },
            new Transition { SourceActivityId = "automated-decision", SourceOutcomeName = "True", DestinationActivityId = "auto-approve" },
            new Transition { SourceActivityId = "automated-decision", SourceOutcomeName = "False", DestinationActivityId = "manual-review" },
            new Transition { SourceActivityId = "manual-review", SourceOutcomeName = "Assigned", DestinationActivityId = "wait-underwriter-decision" },
            new Transition { SourceActivityId = "wait-underwriter-decision", SourceOutcomeName = "Decided", DestinationActivityId = "underwriter-decision" },
            new Transition { SourceActivityId = "underwriter-decision", SourceOutcomeName = "True", DestinationActivityId = "manual-approve" },
            new Transition { SourceActivityId = "underwriter-decision", SourceOutcomeName = "False", DestinationActivityId = "reject-application" },
            new Transition { SourceActivityId = "auto-approve", SourceOutcomeName = "Approved", DestinationActivityId = "generate-agreement" },
            new Transition { SourceActivityId = "manual-approve", SourceOutcomeName = "Approved", DestinationActivityId = "generate-agreement" },
            new Transition { SourceActivityId = "generate-agreement", SourceOutcomeName = "Generated", DestinationActivityId = "send-for-signature" },
            new Transition { SourceActivityId = "send-for-signature", SourceOutcomeName = "Sent", DestinationActivityId = "wait-signature" },
            new Transition { SourceActivityId = "wait-signature", SourceOutcomeName = "Signed", DestinationActivityId = "final-verification" },
            new Transition { SourceActivityId = "final-verification", SourceOutcomeName = "Verified", DestinationActivityId = "disburse-loan" },
            new Transition { SourceActivityId = "disburse-loan", SourceOutcomeName = "Disbursed", DestinationActivityId = "setup-servicing" },
            new Transition { SourceActivityId = "setup-servicing", SourceOutcomeName = "Setup", DestinationActivityId = "send-welcome-package" }
        });

        return workflowType;
    }
}

// Custom Banking Activities
public class CheckCreditScoreTask : TaskActivity<CheckCreditScoreTask>
{
    private readonly ICreditBureauService _creditBureauService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public CheckCreditScoreTask(
        ICreditBureauService creditBureauService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<CheckCreditScoreTask> localizer)
    {
        _creditBureauService = creditBureauService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Check Credit Score Task"];
    public override LocalizedString Category => S["Credit Assessment"];

    public WorkflowExpression<string> ApplicantId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> CreditBureaus
    {
        get => GetProperty(() => new WorkflowExpression<string>("Experian,Equifax,TransUnion"));
        set => SetProperty(value);
    }

    public bool IncludeHistory
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Completed"], S["Failed"], S["InsufficientData"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var applicantId = await _expressionEvaluator.EvaluateAsync(ApplicantId, workflowContext, null);
            var creditBureaus = await _expressionEvaluator.EvaluateAsync(CreditBureaus, workflowContext, null);

            var bureauList = creditBureaus.Split(',').Select(b => b.Trim()).ToList();

            var creditCheckRequest = new CreditCheckRequest
            {
                ApplicantId = applicantId,
                CreditBureaus = bureauList,
                IncludeHistory = IncludeHistory,
                RequestedBy = "LoanApprovalWorkflow",
                RequestDate = DateTime.UtcNow
            };

            var creditCheckResult = await _creditBureauService.CheckCreditAsync(creditCheckRequest);

            // Store credit check results
            workflowContext.Properties["CreditCheck"] = creditCheckResult;
            workflowContext.Properties["CreditCheckPerformedAt"] = DateTime.UtcNow;

            if (creditCheckResult.Success)
            {
                workflowContext.Properties["CreditScore"] = creditCheckResult.Score;
                workflowContext.Properties["CreditRating"] = creditCheckResult.Rating;
                workflowContext.Properties["CreditHistory"] = creditCheckResult.History;
                return Outcomes("Completed");
            }
            else if (creditCheckResult.InsufficientData)
            {
                workflowContext.Properties["CreditCheckIssue"] = "Insufficient credit data available";
                return Outcomes("InsufficientData");
            }
            else
            {
                workflowContext.Properties["CreditCheckError"] = creditCheckResult.ErrorMessage;
                return Outcomes("Failed");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["CreditCheckError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}

public class DisburseLoanTask : TaskActivity<DisburseLoanTask>
{
    private readonly ILoanDisbursementService _disbursementService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public DisburseLoanTask(
        ILoanDisbursementService disbursementService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<DisburseLoanTask> localizer)
    {
        _disbursementService = disbursementService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Disburse Loan Task"];
    public override LocalizedString Category => S["Loan Processing"];

    public WorkflowExpression<string> ApplicationId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<decimal> Amount
    {
        get => GetProperty(() => new WorkflowExpression<decimal>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> DestinationAccount
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> DisbursementMethod
    {
        get => GetProperty(() => new WorkflowExpression<string>("ACH"));
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Disbursed"], S["Failed"], S["Pending"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var applicationId = await _expressionEvaluator.EvaluateAsync(ApplicationId, workflowContext, null);
            var amount = await _expressionEvaluator.EvaluateAsync(Amount, workflowContext, null);
            var destinationAccount = await _expressionEvaluator.EvaluateAsync(DestinationAccount, workflowContext, null);
            var disbursementMethod = await _expressionEvaluator.EvaluateAsync(DisbursementMethod, workflowContext, null);

            var disbursementRequest = new LoanDisbursementRequest
            {
                ApplicationId = applicationId,
                Amount = amount,
                DestinationAccount = destinationAccount,
                DisbursementMethod = disbursementMethod,
                RequestedBy = "LoanApprovalWorkflow",
                RequestDate = DateTime.UtcNow
            };

            var disbursementResult = await _disbursementService.DisburseLoanAsync(disbursementRequest);

            // Store disbursement results
            workflowContext.Properties["LoanDisbursement"] = disbursementResult;
            workflowContext.Properties["DisbursementRequestedAt"] = DateTime.UtcNow;

            if (disbursementResult.Success)
            {
                workflowContext.Properties["TransactionId"] = disbursementResult.TransactionId;
                workflowContext.Properties["DisbursedAmount"] = disbursementResult.DisbursedAmount;
                workflowContext.Properties["DisbursementDate"] = disbursementResult.DisbursementDate;
                return Outcomes("Disbursed");
            }
            else if (disbursementResult.IsPending)
            {
                workflowContext.Properties["PendingReason"] = disbursementResult.PendingReason;
                workflowContext.Properties["ExpectedCompletionDate"] = disbursementResult.ExpectedCompletionDate;
                return Outcomes("Pending");
            }
            else
            {
                workflowContext.Properties["DisbursementError"] = disbursementResult.ErrorMessage;
                return Outcomes("Failed");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["DisbursementError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}
```

#### **Kết quả:**
- ✅ **Tự động hóa quy trình**: Từ nộp đơn đến giải ngân
- ✅ **Đánh giá rủi ro tự động**: Credit scoring và risk assessment
- ✅ **Phê duyệt nhanh**: Auto-approval cho các case đơn giản
- ✅ **Tuân thủ quy định**: Compliance với banking regulations
- ✅ **Audit trail**: Lưu lại toàn bộ quá trình phê duyệt

---

## 🎯 **KHI NÀO CẦN SỬ DỤNG WORKFLOW INTEGRATION?**

### **✅ NÊN DÙNG KHI:**

#### **1. 🔄 Business Process Automation**
- **Complex approval processes** với nhiều bước
- **Multi-step workflows** cần coordination
- **Event-driven processes** dựa trên triggers
- **Long-running processes** có thể kéo dài nhiều ngày/tuần

#### **2. 🏢 Enterprise Integration**
- **System integration** giữa multiple services
- **Data synchronization** across systems
- **External API integration** với third-party services
- **Legacy system modernization**

#### **3. 📋 Compliance & Audit**
- **Regulatory compliance** cần audit trail
- **Quality control processes** với checkpoints
- **Document approval workflows**
- **Risk management processes**

#### **4. 🚀 Scalable Operations**
- **High-volume processing** cần automation
- **Parallel processing** của multiple tasks
- **Error handling và retry logic**
- **Performance monitoring và optimization**

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Simple Operations**
- **Single-step processes** không cần orchestration
- **Synchronous operations** hoàn thành ngay lập tức
- **Static workflows** không thay đổi logic
- **Low-volume operations** không cần automation

#### **2. 🚀 Real-time Requirements**
- **Ultra-low latency** applications
- **Real-time streaming** data processing
- **Simple CRUD operations**
- **Direct API calls** đủ đáp ứng

---

## 💡 **KINH NGHIỆM THỰC TẾ**

### **🔥 Best Practices:**

#### **1. Workflow Design**
```csharp
// Modular workflow design
public class ModularWorkflowBuilder
{
    // Tách workflow thành sub-workflows nhỏ
    public static WorkflowType BuildOrderProcessingWorkflow()
    {
        return new WorkflowType
        {
            // Main workflow chỉ orchestrate sub-workflows
            Activities = new[]
            {
                CreateSubWorkflowActivity("PaymentProcessing"),
                CreateSubWorkflowActivity("InventoryManagement"), 
                CreateSubWorkflowActivity("ShippingProcess"),
                CreateSubWorkflowActivity("CustomerNotification")
            }
        };
    }
}
```

#### **2. Error Handling Strategy**
```csharp
public class RobustWorkflowActivity : TaskActivity<RobustWorkflowActivity>
{
    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        var retryCount = 0;
        var maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                var result = await PerformBusinessLogicAsync();
                return Outcomes("Success");
            }
            catch (TransientException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    workflowContext.Properties["FinalError"] = ex.Message;
                    return Outcomes("Failed");
                }
                
                // Exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (PermanentException ex)
            {
                workflowContext.Properties["PermanentError"] = ex.Message;
                return Outcomes("Failed");
            }
        }
        
        return Outcomes("Failed");
    }
}
```

#### **3. Performance Monitoring**
```csharp
public class MonitoredWorkflowActivity : TaskActivity<MonitoredWorkflowActivity>
{
    private readonly IMetrics _metrics;
    
    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        using var timer = _metrics.Measure.Timer.Time("workflow.activity.execution_time", 
            new MetricTags("activity", Name));
            
        try
        {
            var result = await PerformActivityLogicAsync();
            _metrics.Measure.Counter.Increment("workflow.activity.success", 
                new MetricTags("activity", Name));
            return result;
        }
        catch (Exception ex)
        {
            _metrics.Measure.Counter.Increment("workflow.activity.failure", 
                new MetricTags("activity", Name, "error", ex.GetType().Name));
            throw;
        }
    }
}
```

### **🚀 Tips Triển Khai:**

#### **1. Start Simple, Scale Up**
```csharp
// Phase 1: Basic workflow
public class SimpleApprovalWorkflow
{
    // Submit -> Review -> Approve/Reject
}

// Phase 2: Add complexity
public class EnhancedApprovalWorkflow  
{
    // Submit -> Validate -> Review -> Approve/Reject -> Notify
}

// Phase 3: Add advanced features
public class AdvancedApprovalWorkflow
{
    // Multi-level approval, parallel reviews, escalation, SLA monitoring
}
```

#### **2. Testing Strategy**
```csharp
[Test]
public async Task LoanApprovalWorkflow_Should_AutoApprove_HighCreditScore()
{
    // Arrange
    var workflowType = LoanApprovalWorkflow.BuildLoanApprovalWorkflow();
    var input = new Dictionary<string, object>
    {
        ["LoanApplication"] = new LoanApplication 
        { 
            RequestedAmount = 25000,
            ApplicantId = "test-applicant"
        }
    };
    
    // Mock credit score service to return high score
    _creditService.Setup(x => x.CheckCreditAsync(It.IsAny<CreditCheckRequest>()))
        .ReturnsAsync(new CreditCheckResult { Score = 750, Success = true });
    
    // Act
    var context = await _workflowManager.StartWorkflowAsync(workflowType.WorkflowTypeId, input);
    
    // Assert
    Assert.That(context.Workflow.Status, Is.EqualTo(WorkflowStatus.Finished));
    Assert.That(context.Properties["LoanApproval"], Is.Not.Null);
}
```

### **💡 KẾT LUẬN**

#### **Use cases chính cần Workflow Integration:**
- ✅ **Healthcare**: Patient care workflows, treatment protocols
- ✅ **Manufacturing**: Quality control, production processes
- ✅ **Banking**: Loan approval, compliance processes
- ✅ **E-commerce**: Order processing, fulfillment
- ✅ **HR**: Employee onboarding, performance reviews

#### **Lợi ích:**
- 🎯 **Automation**: Giảm manual work, tăng efficiency
- 🔄 **Consistency**: Standardize business processes
- 📊 **Visibility**: Monitor và track process execution
- ⚡ **Scalability**: Handle high-volume operations
- 🔧 **Flexibility**: Easy to modify và extend workflows

**Workflow Integration là core component cho các hệ thống enterprise cần automation và process management!**

---

## 🏢 **ỨNG DỤNG THỰC TẾ TRONG CÁC DỰ ÁN NÂNG CAO**

### **1. 📚 E-learning Platform - Student Enrollment & Progress Tracking**

#### **Tình huống:**
Xây dựng **nền tảng học online** cần tự động hóa quy trình: đăng ký khóa học, thanh toán, cấp quyền truy cập, theo dõi tiến độ, cấp chứng chỉ.

#### **Ứng dụng Workflow Integration:**

```csharp
// Student Enrollment Workflow
public class StudentEnrollmentWorkflow
{
    public static WorkflowType BuildStudentEnrollmentWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "StudentEnrollmentWorkflow",
            Name = "Student Course Enrollment Process",
            IsEnabled = true,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Student Registration Event
        var studentRegistered = new ActivityRecord
        {
            ActivityId = "student-registered",
            Name = "StudentRegisteredEvent",
            Properties = JObject.FromObject(new
            {
                CourseTypes = "Premium,Standard,Free"
            })
        };

        // 2. Check Course Availability
        var checkAvailability = new ActivityRecord
        {
            ActivityId = "check-availability",
            Name = "CheckCourseAvailabilityTask",
            Properties = JObject.FromObject(new
            {
                CourseId = "{{ Enrollment.CourseId }}",
                RequestedStartDate = "{{ Enrollment.RequestedStartDate }}",
                CheckPrerequisites = true
            })
        };

        // 3. Payment Processing (for paid courses)
        var processPayment = new ActivityRecord
        {
            ActivityId = "process-payment",
            Name = "ProcessPaymentTask",
            Properties = JObject.FromObject(new
            {
                Amount = "{{ Course.Price }}",
                Currency = "{{ Course.Currency }}",
                PaymentMethod = "{{ Enrollment.PaymentMethod }}",
                StudentId = "{{ Student.StudentId }}"
            })
        };

        // 4. Grant Course Access
        var grantAccess = new ActivityRecord
        {
            ActivityId = "grant-access",
            Name = "GrantCourseAccessTask",
            Properties = JObject.FromObject(new
            {
                StudentId = "{{ Student.StudentId }}",
                CourseId = "{{ Course.CourseId }}",
                AccessLevel = "{{ Course.AccessLevel }}",
                ExpiryDate = "{{ Course.AccessDuration }}"
            })
        };

        // 5. Send Welcome Email
        var sendWelcome = new ActivityRecord
        {
            ActivityId = "send-welcome",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Student.Email }}",
                Subject = "Welcome to {{ Course.Title }}!",
                Body = @"
                    Dear {{ Student.FirstName }},
                    
                    Welcome to {{ Course.Title }}!
                    
                    Course Details:
                    - Start Date: {{ Course.StartDate }}
                    - Duration: {{ Course.Duration }}
                    - Instructor: {{ Course.Instructor.Name }}
                    
                    Access your course: {{ Course.AccessUrl }}
                    
                    Happy Learning!
                ",
                IsHtml = true
            })
        };

        // 6. Create Learning Path
        var createLearningPath = new ActivityRecord
        {
            ActivityId = "create-learning-path",
            Name = "CreateLearningPathTask",
            Properties = JObject.FromObject(new
            {
                StudentId = "{{ Student.StudentId }}",
                CourseId = "{{ Course.CourseId }}",
                Modules = "{{ Course.Modules }}",
                ProgressTracking = true
            })
        };

        // 7. Schedule Progress Check (Timer Event)
        var scheduleProgressCheck = new ActivityRecord
        {
            ActivityId = "schedule-progress-check",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                Interval = "P7D", // Every 7 days
                IsRecurring = true
            })
        };

        // 8. Check Student Progress
        var checkProgress = new ActivityRecord
        {
            ActivityId = "check-progress",
            Name = "CheckStudentProgressTask",
            Properties = JObject.FromObject(new
            {
                StudentId = "{{ Student.StudentId }}",
                CourseId = "{{ Course.CourseId }}",
                ProgressThreshold = 10 // Minimum 10% progress per week
            })
        };

        // 9. Progress Decision
        var progressDecision = new ActivityRecord
        {
            ActivityId = "progress-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ StudentProgress.WeeklyProgress >= 10 }}"
            })
        };

        // 10a. Send Encouragement Email
        var sendEncouragement = new ActivityRecord
        {
            ActivityId = "send-encouragement",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Student.Email }}",
                Subject = "Great Progress in {{ Course.Title }}!",
                Body = @"
                    Hi {{ Student.FirstName }},
                    
                    You're doing great! You've completed {{ StudentProgress.CompletionPercentage }}% of {{ Course.Title }}.
                    
                    Keep up the excellent work!
                    
                    Next up: {{ StudentProgress.NextModule }}
                "
            })
        };

        // 10b. Send Reminder Email
        var sendReminder = new ActivityRecord
        {
            ActivityId = "send-reminder",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Student.Email }}",
                Subject = "Don't forget about {{ Course.Title }}",
                Body = @"
                    Hi {{ Student.FirstName }},
                    
                    We noticed you haven't made much progress in {{ Course.Title }} lately.
                    
                    Current Progress: {{ StudentProgress.CompletionPercentage }}%
                    Time Remaining: {{ Course.TimeRemaining }}
                    
                    Need help? Contact our support team.
                "
            })
        };

        // 11. Course Completion Check
        var checkCompletion = new ActivityRecord
        {
            ActivityId = "check-completion",
            Name = "CheckCourseCompletionTask",
            Properties = JObject.FromObject(new
            {
                StudentId = "{{ Student.StudentId }}",
                CourseId = "{{ Course.CourseId }}",
                RequiredCompletionRate = 80
            })
        };

        // 12. Generate Certificate
        var generateCertificate = new ActivityRecord
        {
            ActivityId = "generate-certificate",
            Name = "GenerateCertificateTask",
            Properties = JObject.FromObject(new
            {
                StudentId = "{{ Student.StudentId }}",
                CourseId = "{{ Course.CourseId }}",
                CompletionDate = "{{ DateTime.UtcNow }}",
                CertificateTemplate = "{{ Course.CertificateTemplate }}"
            })
        };

        // 13. Send Certificate
        var sendCertificate = new ActivityRecord
        {
            ActivityId = "send-certificate",
            Name = "SendEmailTask",
            Properties = JObject.FromObject(new
            {
                Recipients = "{{ Student.Email }}",
                Subject = "🎉 Congratulations! You've completed {{ Course.Title }}",
                Body = @"
                    Congratulations {{ Student.FirstName }}!
                    
                    You have successfully completed {{ Course.Title }}.
                    
                    Your certificate is attached to this email.
                    
                    Final Score: {{ StudentProgress.FinalScore }}%
                    Completion Date: {{ Certificate.CompletionDate }}
                ",
                Attachments = "{{ Certificate.FilePath }}"
            })
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            studentRegistered, checkAvailability, processPayment, grantAccess,
            sendWelcome, createLearningPath, scheduleProgressCheck, checkProgress,
            progressDecision, sendEncouragement, sendReminder, checkCompletion,
            generateCertificate, sendCertificate
        });

        // Define transitions
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "student-registered", SourceOutcomeName = "Registered", DestinationActivityId = "check-availability" },
            new Transition { SourceActivityId = "check-availability", SourceOutcomeName = "Available", DestinationActivityId = "process-payment" },
            new Transition { SourceActivityId = "process-payment", SourceOutcomeName = "Success", DestinationActivityId = "grant-access" },
            new Transition { SourceActivityId = "grant-access", SourceOutcomeName = "Granted", DestinationActivityId = "send-welcome" },
            new Transition { SourceActivityId = "send-welcome", SourceOutcomeName = "Sent", DestinationActivityId = "create-learning-path" },
            new Transition { SourceActivityId = "create-learning-path", SourceOutcomeName = "Created", DestinationActivityId = "schedule-progress-check" },
            new Transition { SourceActivityId = "schedule-progress-check", SourceOutcomeName = "Timer", DestinationActivityId = "check-progress" },
            new Transition { SourceActivityId = "check-progress", SourceOutcomeName = "Checked", DestinationActivityId = "progress-decision" },
            new Transition { SourceActivityId = "progress-decision", SourceOutcomeName = "True", DestinationActivityId = "send-encouragement" },
            new Transition { SourceActivityId = "progress-decision", SourceOutcomeName = "False", DestinationActivityId = "send-reminder" },
            new Transition { SourceActivityId = "send-encouragement", SourceOutcomeName = "Sent", DestinationActivityId = "check-completion" },
            new Transition { SourceActivityId = "send-reminder", SourceOutcomeName = "Sent", DestinationActivityId = "schedule-progress-check" },
            new Transition { SourceActivityId = "check-completion", SourceOutcomeName = "Completed", DestinationActivityId = "generate-certificate" },
            new Transition { SourceActivityId = "check-completion", SourceOutcomeName = "InProgress", DestinationActivityId = "schedule-progress-check" },
            new Transition { SourceActivityId = "generate-certificate", SourceOutcomeName = "Generated", DestinationActivityId = "send-certificate" }
        });

        return workflowType;
    }
}

// Custom Activities cho E-learning
public class CheckStudentProgressTask : TaskActivity<CheckStudentProgressTask>
{
    private readonly ILearningProgressService _progressService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public CheckStudentProgressTask(
        ILearningProgressService progressService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<CheckStudentProgressTask> localizer)
    {
        _progressService = progressService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Check Student Progress Task"];
    public override LocalizedString Category => S["E-Learning"];

    public WorkflowExpression<string> StudentId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> CourseId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public int ProgressThreshold
    {
        get => GetProperty(() => 10);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Checked"], S["NoProgress"], S["Error"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var studentId = await _expressionEvaluator.EvaluateAsync(StudentId, workflowContext, null);
            var courseId = await _expressionEvaluator.EvaluateAsync(CourseId, workflowContext, null);

            var progressData = await _progressService.GetStudentProgressAsync(studentId, courseId);

            if (progressData == null)
            {
                workflowContext.Properties["ProgressError"] = "Student progress data not found";
                return Outcomes("Error");
            }

            // Calculate weekly progress
            var lastWeekProgress = await _progressService.GetProgressSinceAsync(studentId, courseId, DateTime.UtcNow.AddDays(-7));
            var weeklyProgress = progressData.CompletionPercentage - lastWeekProgress.CompletionPercentage;

            // Store progress data
            workflowContext.Properties["StudentProgress"] = new
            {
                CompletionPercentage = progressData.CompletionPercentage,
                WeeklyProgress = weeklyProgress,
                LastAccessDate = progressData.LastAccessDate,
                TimeSpent = progressData.TotalTimeSpent,
                CompletedModules = progressData.CompletedModules,
                NextModule = progressData.NextModule,
                FinalScore = progressData.FinalScore
            };

            workflowContext.Properties["ProgressCheckedAt"] = DateTime.UtcNow;

            if (weeklyProgress < ProgressThreshold)
            {
                return Outcomes("NoProgress");
            }

            return Outcomes("Checked");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["ProgressError"] = ex.Message;
            return Outcomes("Error");
        }
    }
}

public class GenerateCertificateTask : TaskActivity<GenerateCertificateTask>
{
    private readonly ICertificateService _certificateService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public GenerateCertificateTask(
        ICertificateService certificateService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<GenerateCertificateTask> localizer)
    {
        _certificateService = certificateService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Generate Certificate Task"];
    public override LocalizedString Category => S["E-Learning"];

    public WorkflowExpression<string> StudentId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> CourseId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<DateTime> CompletionDate
    {
        get => GetProperty(() => new WorkflowExpression<DateTime>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> CertificateTemplate
    {
        get => GetProperty(() => new WorkflowExpression<string>("DefaultTemplate"));
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Generated"], S["Failed"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var studentId = await _expressionEvaluator.EvaluateAsync(StudentId, workflowContext, null);
            var courseId = await _expressionEvaluator.EvaluateAsync(CourseId, workflowContext, null);
            var completionDate = await _expressionEvaluator.EvaluateAsync(CompletionDate, workflowContext, null);
            var template = await _expressionEvaluator.EvaluateAsync(CertificateTemplate, workflowContext, null);

            var certificateRequest = new CertificateGenerationRequest
            {
                StudentId = studentId,
                CourseId = courseId,
                CompletionDate = completionDate,
                Template = template,
                IncludeQRCode = true,
                IncludeGrades = true
            };

            var certificate = await _certificateService.GenerateCertificateAsync(certificateRequest);

            // Store certificate data
            workflowContext.Properties["Certificate"] = new
            {
                CertificateId = certificate.CertificateId,
                FilePath = certificate.FilePath,
                DownloadUrl = certificate.DownloadUrl,
                CompletionDate = certificate.CompletionDate,
                CertificateNumber = certificate.CertificateNumber,
                VerificationUrl = certificate.VerificationUrl
            };

            workflowContext.Properties["CertificateGeneratedAt"] = DateTime.UtcNow;

            return Outcomes("Generated");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["CertificateError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}
```

#### **Kết quả:**
- ✅ **Tự động hóa enrollment**: Từ đăng ký đến cấp chứng chỉ
- ✅ **Theo dõi tiến độ**: Weekly progress monitoring
- ✅ **Personalized communication**: Encouragement/reminder emails
- ✅ **Certificate automation**: Tự động tạo và gửi chứng chỉ
- ✅ **Student retention**: Proactive engagement strategies

### **2. 🏪 Retail Inventory Management System**

#### **Tình huống:**
Xây dựng **hệ thống quản lý kho bán lẻ** cần tự động hóa: nhập kho, kiểm kê, cảnh báo hết hàng, đặt hàng tự động, phân phối đến các cửa hàng.

#### **Ứng dụng Workflow Integration:**

```csharp
// Inventory Management Workflow
public class InventoryManagementWorkflow
{
    public static WorkflowType BuildInventoryManagementWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "InventoryManagementWorkflow",
            Name = "Retail Inventory Management Process",
            IsEnabled = true,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. Daily Inventory Check (Timer Event)
        var dailyInventoryCheck = new ActivityRecord
        {
            ActivityId = "daily-inventory-check",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                StartAt = "06:00:00", // 6 AM daily
                Interval = "P1D", // Every day
                IsRecurring = true
            })
        };

        // 2. Check Stock Levels
        var checkStockLevels = new ActivityRecord
        {
            ActivityId = "check-stock-levels",
            Name = "CheckStockLevelsTask",
            Properties = JObject.FromObject(new
            {
                WarehouseId = "{{ Warehouse.WarehouseId }}",
                CheckAllProducts = true,
                IncludePendingOrders = true
            })
        };

        // 3. Identify Low Stock Items
        var identifyLowStock = new ActivityRecord
        {
            ActivityId = "identify-low-stock",
            Name = "IdentifyLowStockTask",
            Properties = JObject.FromObject(new
            {
                LowStockThreshold = "{{ Product.ReorderPoint }}",
                CriticalStockThreshold = "{{ Product.MinimumStock }}",
                SeasonalAdjustment = true
            })
        };

        // 4. Low Stock Decision
        var lowStockDecision = new ActivityRecord
        {
            ActivityId = "low-stock-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ LowStockItems.Count > 0 }}"
            })
        };

        // 5. Generate Purchase Orders
        var generatePurchaseOrders = new ActivityRecord
        {
            ActivityId = "generate-purchase-orders",
            Name = "GeneratePurchaseOrdersTask",
            Properties = JObject.FromObject(new
            {
                LowStockItems = "{{ LowStockItems }}",
                AutoApproveLimit = 10000, // Auto-approve orders under $10,000
                PreferredSuppliers = "{{ Product.PreferredSuppliers }}",
                DeliveryTimeframe = "{{ Product.LeadTime }}"
            })
        };

        // 6. Purchase Order Approval
        var purchaseOrderApproval = new ActivityRecord
        {
            ActivityId = "purchase-order-approval",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ PurchaseOrder.TotalAmount <= 10000 }}"
            })
        };

        // 7a. Auto Approve Small Orders
        var autoApproveOrder = new ActivityRecord
        {
            ActivityId = "auto-approve-order",
            Name = "ApprovePurchaseOrderTask",
            Properties = JObject.FromObject(new
            {
                PurchaseOrderId = "{{ PurchaseOrder.OrderId }}",
                ApprovalType = "Automatic",
                ApprovedBy = "InventorySystem"
            })
        };

        // 7b. Send for Manual Approval
        var sendForApproval = new ActivityRecord
        {
            ActivityId = "send-for-approval",
            Name = "SendForApprovalTask",
            Properties = JObject.FromObject(new
            {
                PurchaseOrderId = "{{ PurchaseOrder.OrderId }}",
                ApproverRole = "PurchaseManager",
                Priority = "{{ PurchaseOrder.Priority }}",
                DueDate = "{{ DateTime.UtcNow.AddDays(2) }}"
            })
        };

        // 8. Wait for Manual Approval
        var waitApproval = new ActivityRecord
        {
            ActivityId = "wait-approval",
            Name = "ApprovalReceivedEvent"
        };

        // 9. Send Purchase Order to Supplier
        var sendToSupplier = new ActivityRecord
        {
            ActivityId = "send-to-supplier",
            Name = "SendPurchaseOrderTask",
            Properties = JObject.FromObject(new
            {
                PurchaseOrderId = "{{ PurchaseOrder.OrderId }}",
                SupplierId = "{{ PurchaseOrder.SupplierId }}",
                DeliveryMethod = "{{ Supplier.PreferredDeliveryMethod }}",
                PaymentTerms = "{{ Supplier.PaymentTerms }}"
            })
        };

        // 10. Track Delivery Status
        var trackDelivery = new ActivityRecord
        {
            ActivityId = "track-delivery",
            Name = "TrackDeliveryTask",
            Properties = JObject.FromObject(new
            {
                PurchaseOrderId = "{{ PurchaseOrder.OrderId }}",
                TrackingEnabled = true,
                NotifyOnDelay = true,
                ExpectedDeliveryDate = "{{ PurchaseOrder.ExpectedDeliveryDate }}"
            })
        };

        // 11. Goods Received Event
        var goodsReceived = new ActivityRecord
        {
            ActivityId = "goods-received",
            Name = "GoodsReceivedEvent"
        };

        // 12. Quality Inspection
        var qualityInspection = new ActivityRecord
        {
            ActivityId = "quality-inspection",
            Name = "QualityInspectionTask",
            Properties = JObject.FromObject(new
            {
                DeliveryId = "{{ Delivery.DeliveryId }}",
                InspectionType = "Incoming",
                SamplingRate = "{{ Product.InspectionSamplingRate }}",
                QualityStandards = "{{ Product.QualityStandards }}"
            })
        };

        // 13. Quality Decision
        var qualityDecision = new ActivityRecord
        {
            ActivityId = "quality-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ QualityInspection.Result == 'Pass' }}"
            })
        };

        // 14a. Accept Goods
        var acceptGoods = new ActivityRecord
        {
            ActivityId = "accept-goods",
            Name = "AcceptGoodsTask",
            Properties = JObject.FromObject(new
            {
                DeliveryId = "{{ Delivery.DeliveryId }}",
                UpdateInventory = true,
                GenerateReceiptNote = true,
                NotifyAccounting = true
            })
        };

        // 14b. Reject Goods
        var rejectGoods = new ActivityRecord
        {
            ActivityId = "reject-goods",
            Name = "RejectGoodsTask",
            Properties = JObject.FromObject(new
            {
                DeliveryId = "{{ Delivery.DeliveryId }}",
                RejectionReason = "{{ QualityInspection.FailureReason }}",
                NotifySupplier = true,
                RequestReplacement = true
            })
        };

        // 15. Update Inventory Levels
        var updateInventory = new ActivityRecord
        {
            ActivityId = "update-inventory",
            Name = "UpdateInventoryTask",
            Properties = JObject.FromObject(new
            {
                DeliveryId = "{{ Delivery.DeliveryId }}",
                ReceivedItems = "{{ Delivery.ReceivedItems }}",
                UpdateStockLevels = true,
                UpdateCosts = true,
                GenerateStockMovement = true
            })
        };

        // 16. Distribute to Stores
        var distributeToStores = new ActivityRecord
        {
            ActivityId = "distribute-to-stores",
            Name = "DistributeToStoresTask",
            Properties = JObject.FromObject(new
            {
                ReceivedItems = "{{ Delivery.ReceivedItems }}",
                DistributionStrategy = "DemandBased",
                PriorityStores = "{{ Product.PriorityStores }}",
                MinimumStoreStock = "{{ Product.MinimumStoreStock }}"
            })
        };

        // 17. Generate Reports
        var generateReports = new ActivityRecord
        {
            ActivityId = "generate-reports",
            Name = "GenerateInventoryReportsTask",
            Properties = JObject.FromObject(new
            {
                ReportTypes = "StockLevels,PurchaseOrders,Deliveries,QualityIssues",
                Recipients = "inventory-manager@company.com,operations@company.com",
                IncludeCharts = true,
                ScheduleDaily = true
            })
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            dailyInventoryCheck, checkStockLevels, identifyLowStock, lowStockDecision,
            generatePurchaseOrders, purchaseOrderApproval, autoApproveOrder, sendForApproval,
            waitApproval, sendToSupplier, trackDelivery, goodsReceived,
            qualityInspection, qualityDecision, acceptGoods, rejectGoods,
            updateInventory, distributeToStores, generateReports
        });

        // Define transitions (simplified)
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "daily-inventory-check", SourceOutcomeName = "Timer", DestinationActivityId = "check-stock-levels" },
            new Transition { SourceActivityId = "check-stock-levels", SourceOutcomeName = "Checked", DestinationActivityId = "identify-low-stock" },
            new Transition { SourceActivityId = "identify-low-stock", SourceOutcomeName = "Identified", DestinationActivityId = "low-stock-decision" },
            new Transition { SourceActivityId = "low-stock-decision", SourceOutcomeName = "True", DestinationActivityId = "generate-purchase-orders" },
            new Transition { SourceActivityId = "generate-purchase-orders", SourceOutcomeName = "Generated", DestinationActivityId = "purchase-order-approval" },
            new Transition { SourceActivityId = "purchase-order-approval", SourceOutcomeName = "True", DestinationActivityId = "auto-approve-order" },
            new Transition { SourceActivityId = "purchase-order-approval", SourceOutcomeName = "False", DestinationActivityId = "send-for-approval" },
            new Transition { SourceActivityId = "auto-approve-order", SourceOutcomeName = "Approved", DestinationActivityId = "send-to-supplier" },
            new Transition { SourceActivityId = "send-for-approval", SourceOutcomeName = "Sent", DestinationActivityId = "wait-approval" },
            new Transition { SourceActivityId = "wait-approval", SourceOutcomeName = "Approved", DestinationActivityId = "send-to-supplier" },
            new Transition { SourceActivityId = "send-to-supplier", SourceOutcomeName = "Sent", DestinationActivityId = "track-delivery" },
            new Transition { SourceActivityId = "track-delivery", SourceOutcomeName = "Tracking", DestinationActivityId = "goods-received" },
            new Transition { SourceActivityId = "goods-received", SourceOutcomeName = "Received", DestinationActivityId = "quality-inspection" },
            new Transition { SourceActivityId = "quality-inspection", SourceOutcomeName = "Completed", DestinationActivityId = "quality-decision" },
            new Transition { SourceActivityId = "quality-decision", SourceOutcomeName = "True", DestinationActivityId = "accept-goods" },
            new Transition { SourceActivityId = "quality-decision", SourceOutcomeName = "False", DestinationActivityId = "reject-goods" },
            new Transition { SourceActivityId = "accept-goods", SourceOutcomeName = "Accepted", DestinationActivityId = "update-inventory" },
            new Transition { SourceActivityId = "update-inventory", SourceOutcomeName = "Updated", DestinationActivityId = "distribute-to-stores" },
            new Transition { SourceActivityId = "distribute-to-stores", SourceOutcomeName = "Distributed", DestinationActivityId = "generate-reports" }
        });

        return workflowType;
    }
}

// Custom Inventory Activities
public class CheckStockLevelsTask : TaskActivity<CheckStockLevelsTask>
{
    private readonly IInventoryService _inventoryService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public CheckStockLevelsTask(
        IInventoryService inventoryService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<CheckStockLevelsTask> localizer)
    {
        _inventoryService = inventoryService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Check Stock Levels Task"];
    public override LocalizedString Category => S["Inventory Management"];

    public WorkflowExpression<string> WarehouseId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public bool CheckAllProducts
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public bool IncludePendingOrders
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Checked"], S["Error"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var warehouseId = await _expressionEvaluator.EvaluateAsync(WarehouseId, workflowContext, null);

            var stockCheckRequest = new StockLevelCheckRequest
            {
                WarehouseId = warehouseId,
                CheckAllProducts = CheckAllProducts,
                IncludePendingOrders = IncludePendingOrders,
                CheckDate = DateTime.UtcNow
            };

            var stockLevels = await _inventoryService.CheckStockLevelsAsync(stockCheckRequest);

            // Store stock level data
            workflowContext.Properties["StockLevels"] = stockLevels;
            workflowContext.Properties["TotalProducts"] = stockLevels.Count;
            workflowContext.Properties["StockCheckPerformedAt"] = DateTime.UtcNow;

            // Calculate summary statistics
            var totalValue = stockLevels.Sum(s => s.CurrentStock * s.UnitCost);
            var lowStockCount = stockLevels.Count(s => s.CurrentStock <= s.ReorderPoint);
            var outOfStockCount = stockLevels.Count(s => s.CurrentStock <= 0);

            workflowContext.Properties["InventorySummary"] = new
            {
                TotalValue = totalValue,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                HealthyStockCount = stockLevels.Count - lowStockCount - outOfStockCount
            };

            return Outcomes("Checked");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["StockCheckError"] = ex.Message;
            return Outcomes("Error");
        }
    }
}

public class DistributeToStoresTask : TaskActivity<DistributeToStoresTask>
{
    private readonly IDistributionService _distributionService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public DistributeToStoresTask(
        IDistributionService distributionService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<DistributeToStoresTask> localizer)
    {
        _distributionService = distributionService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Distribute to Stores Task"];
    public override LocalizedString Category => S["Distribution"];

    public WorkflowExpression<string> ReceivedItems
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public string DistributionStrategy
    {
        get => GetProperty(() => "DemandBased");
        set => SetProperty(value);
    }

    public WorkflowExpression<string> PriorityStores
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Distributed"], S["PartiallyDistributed"], S["Failed"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var receivedItemsJson = await _expressionEvaluator.EvaluateAsync(ReceivedItems, workflowContext, null);
            var priorityStoresJson = await _expressionEvaluator.EvaluateAsync(PriorityStores, workflowContext, null);

            var receivedItems = JsonSerializer.Deserialize<List<ReceivedItem>>(receivedItemsJson);
            var priorityStores = JsonSerializer.Deserialize<List<string>>(priorityStoresJson ?? "[]");

            var distributionRequest = new DistributionRequest
            {
                ReceivedItems = receivedItems,
                DistributionStrategy = DistributionStrategy,
                PriorityStores = priorityStores,
                RequestDate = DateTime.UtcNow
            };

            var distributionResult = await _distributionService.DistributeItemsAsync(distributionRequest);

            // Store distribution results
            workflowContext.Properties["Distribution"] = distributionResult;
            workflowContext.Properties["DistributionPerformedAt"] = DateTime.UtcNow;

            if (distributionResult.FullyDistributed)
            {
                workflowContext.Properties["DistributedStores"] = distributionResult.DistributedStores;
                workflowContext.Properties["TotalItemsDistributed"] = distributionResult.TotalItemsDistributed;
                return Outcomes("Distributed");
            }
            else if (distributionResult.PartiallyDistributed)
            {
                workflowContext.Properties["RemainingItems"] = distributionResult.RemainingItems;
                workflowContext.Properties["DistributionIssues"] = distributionResult.Issues;
                return Outcomes("PartiallyDistributed");
            }
            else
            {
                workflowContext.Properties["DistributionError"] = distributionResult.ErrorMessage;
                return Outcomes("Failed");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["DistributionError"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}
```

#### **Kết quả:**
- ✅ **Tự động quản lý kho**: Daily stock monitoring và reordering
- ✅ **Tối ưu hóa mua hàng**: Auto-approval cho orders nhỏ
- ✅ **Kiểm soát chất lượng**: Quality inspection workflow
- ✅ **Phân phối thông minh**: Demand-based distribution
- ✅ **Báo cáo tự động**: Daily inventory reports

### **3. 🏢 HR Employee Onboarding System**

#### **Tình huống:**
Xây dựng **hệ thống onboarding nhân viên** cần tự động hóa: tạo tài khoản, cấp quyền, training, paperwork, equipment setup, buddy assignment.

#### **Ứng dụng Workflow Integration:**

```csharp
// Employee Onboarding Workflow
public class EmployeeOnboardingWorkflow
{
    public static WorkflowType BuildEmployeeOnboardingWorkflow()
    {
        var workflowType = new WorkflowType
        {
            WorkflowTypeId = "EmployeeOnboardingWorkflow",
            Name = "Employee Onboarding Process",
            IsEnabled = true,
            Activities = new List<ActivityRecord>(),
            Transitions = new List<Transition>()
        };

        // 1. New Employee Hired Event
        var employeeHired = new ActivityRecord
        {
            ActivityId = "employee-hired",
            Name = "EmployeeHiredEvent"
        };

        // 2. Create Employee Profile
        var createProfile = new ActivityRecord
        {
            ActivityId = "create-profile",
            Name = "CreateEmployeeProfileTask",
            Properties = JObject.FromObject(new
            {
                EmployeeData = "{{ NewEmployee }}",
                GenerateEmployeeId = true,
                CreatePhotoPlaceholder = true
            })
        };

        // 3. Setup IT Accounts
        var setupITAccounts = new ActivityRecord
        {
            ActivityId = "setup-it-accounts",
            Name = "SetupITAccountsTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                Department = "{{ Employee.Department }}",
                Role = "{{ Employee.Role }}",
                CreateEmailAccount = true,
                SetupActiveDirectory = true,
                AssignSoftwareLicenses = true
            })
        };

        // 4. Assign Equipment
        var assignEquipment = new ActivityRecord
        {
            ActivityId = "assign-equipment",
            Name = "AssignEquipmentTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                Department = "{{ Employee.Department }}",
                Role = "{{ Employee.Role }}",
                EquipmentList = "{{ Role.RequiredEquipment }}",
                DeliveryLocation = "{{ Employee.WorkLocation }}"
            })
        };

        // 5. Assign Buddy/Mentor
        var assignBuddy = new ActivityRecord
        {
            ActivityId = "assign-buddy",
            Name = "AssignBuddyTask",
            Properties = JObject.FromObject(new
            {
                NewEmployeeId = "{{ Employee.EmployeeId }}",
                Department = "{{ Employee.Department }}",
                PreferSameDepartment = true,
                ExcludeManagers = true,
                BuddyDuration = "P90D" // 90 days
            })
        };

        // 6. Parallel: Send Welcome Package & Schedule Orientation
        var parallelWelcome = new ActivityRecord
        {
            ActivityId = "parallel-welcome",
            Name = "ParallelTask",
            Properties = JObject.FromObject(new
            {
                BranchCount = 2,
                WaitForAll = false
            })
        };

        // 6a. Send Welcome Package
        var sendWelcomePackage = new ActivityRecord
        {
            ActivityId = "send-welcome-package",
            Name = "SendWelcomePackageTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                IncludeHandbook = true,
                IncludeCompanySwag = true,
                IncludeITInstructions = true,
                DeliveryMethod = "Email"
            })
        };

        // 6b. Schedule Orientation
        var scheduleOrientation = new ActivityRecord
        {
            ActivityId = "schedule-orientation",
            Name = "ScheduleOrientationTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                StartDate = "{{ Employee.StartDate }}",
                OrientationType = "{{ Employee.OrientationType }}",
                Duration = "P5D" // 5 days
            })
        };

        // 7. First Day Activities
        var firstDayActivities = new ActivityRecord
        {
            ActivityId = "first-day-activities",
            Name = "FirstDayActivitiesTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                Activities = "BadgePhoto,OfficeToour,MeetTeam,ITSetup,Paperwork",
                BuddyIntroduction = true,
                ManagerMeeting = true
            })
        };

        // 8. Training Schedule
        var scheduleTraining = new ActivityRecord
        {
            ActivityId = "schedule-training",
            Name = "ScheduleTrainingTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                Department = "{{ Employee.Department }}",
                Role = "{{ Employee.Role }}",
                TrainingPrograms = "{{ Role.RequiredTraining }}",
                TrainingDuration = "P30D" // 30 days
            })
        };

        // 9. Monitor Training Progress (Timer)
        var monitorTraining = new ActivityRecord
        {
            ActivityId = "monitor-training",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                Interval = "P7D", // Weekly check
                IsRecurring = true
            })
        };

        // 10. Check Training Progress
        var checkTrainingProgress = new ActivityRecord
        {
            ActivityId = "check-training-progress",
            Name = "CheckTrainingProgressTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                ExpectedProgressRate = 25, // 25% per week
                SendReminders = true
            })
        };

        // 11. 30-Day Check-in
        var thirtyDayCheckin = new ActivityRecord
        {
            ActivityId = "thirty-day-checkin",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                StartAt = "{{ Employee.StartDate.AddDays(30) }}",
                IsRecurring = false
            })
        };

        // 12. Conduct 30-Day Review
        var conductReview = new ActivityRecord
        {
            ActivityId = "conduct-review",
            Name = "Conduct30DayReviewTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                ReviewType = "30DayCheckIn",
                IncludeBuddyFeedback = true,
                IncludeManagerFeedback = true,
                IncludeSelfAssessment = true
            })
        };

        // 13. Review Decision
        var reviewDecision = new ActivityRecord
        {
            ActivityId = "review-decision",
            Name = "IfElseTask",
            Properties = JObject.FromObject(new
            {
                Condition = "{{ Review.OverallRating >= 3 }}" // 3 out of 5
            })
        };

        // 14a. Continue Onboarding
        var continueOnboarding = new ActivityRecord
        {
            ActivityId = "continue-onboarding",
            Name = "ContinueOnboardingTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                ExtendBuddyProgram = "{{ Review.ExtendBuddyProgram }}",
                AdditionalTraining = "{{ Review.AdditionalTraining }}",
                NextReviewDate = "{{ Employee.StartDate.AddDays(90) }}"
            })
        };

        // 14b. Performance Improvement Plan
        var performanceImprovement = new ActivityRecord
        {
            ActivityId = "performance-improvement",
            Name = "CreatePerformanceImprovementPlanTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                IssuesIdentified = "{{ Review.IssuesIdentified }}",
                ImprovementActions = "{{ Review.ImprovementActions }}",
                ReviewFrequency = "P14D" // Every 2 weeks
            })
        };

        // 15. 90-Day Final Review
        var ninetyDayReview = new ActivityRecord
        {
            ActivityId = "ninety-day-review",
            Name = "TimerEvent",
            Properties = JObject.FromObject(new
            {
                StartAt = "{{ Employee.StartDate.AddDays(90) }}",
                IsRecurring = false
            })
        };

        // 16. Complete Onboarding
        var completeOnboarding = new ActivityRecord
        {
            ActivityId = "complete-onboarding",
            Name = "CompleteOnboardingTask",
            Properties = JObject.FromObject(new
            {
                EmployeeId = "{{ Employee.EmployeeId }}",
                OnboardingSuccess = "{{ FinalReview.Success }}",
                GenerateCompletionCertificate = true,
                UpdateEmployeeStatus = "Active",
                EndBuddyProgram = true
            })
        };

        // Add all activities
        workflowType.Activities.AddRange(new[]
        {
            employeeHired, createProfile, setupITAccounts, assignEquipment,
            assignBuddy, parallelWelcome, sendWelcomePackage, scheduleOrientation,
            firstDayActivities, scheduleTraining, monitorTraining, checkTrainingProgress,
            thirtyDayCheckin, conductReview, reviewDecision, continueOnboarding,
            performanceImprovement, ninetyDayReview, completeOnboarding
        });

        // Define transitions (simplified)
        workflowType.Transitions.AddRange(new[]
        {
            new Transition { SourceActivityId = "employee-hired", SourceOutcomeName = "Hired", DestinationActivityId = "create-profile" },
            new Transition { SourceActivityId = "create-profile", SourceOutcomeName = "Created", DestinationActivityId = "setup-it-accounts" },
            new Transition { SourceActivityId = "setup-it-accounts", SourceOutcomeName = "Setup", DestinationActivityId = "assign-equipment" },
            new Transition { SourceActivityId = "assign-equipment", SourceOutcomeName = "Assigned", DestinationActivityId = "assign-buddy" },
            new Transition { SourceActivityId = "assign-buddy", SourceOutcomeName = "Assigned", DestinationActivityId = "parallel-welcome" },
            new Transition { SourceActivityId = "parallel-welcome", SourceOutcomeName = "Branch1", DestinationActivityId = "send-welcome-package" },
            new Transition { SourceActivityId = "parallel-welcome", SourceOutcomeName = "Branch2", DestinationActivityId = "schedule-orientation" },
            new Transition { SourceActivityId = "parallel-welcome", SourceOutcomeName = "AllCompleted", DestinationActivityId = "first-day-activities" },
            new Transition { SourceActivityId = "first-day-activities", SourceOutcomeName = "Completed", DestinationActivityId = "schedule-training" },
            new Transition { SourceActivityId = "schedule-training", SourceOutcomeName = "Scheduled", DestinationActivityId = "monitor-training" },
            new Transition { SourceActivityId = "monitor-training", SourceOutcomeName = "Timer", DestinationActivityId = "check-training-progress" },
            new Transition { SourceActivityId = "check-training-progress", SourceOutcomeName = "Checked", DestinationActivityId = "thirty-day-checkin" },
            new Transition { SourceActivityId = "thirty-day-checkin", SourceOutcomeName = "Timer", DestinationActivityId = "conduct-review" },
            new Transition { SourceActivityId = "conduct-review", SourceOutcomeName = "Completed", DestinationActivityId = "review-decision" },
            new Transition { SourceActivityId = "review-decision", SourceOutcomeName = "True", DestinationActivityId = "continue-onboarding" },
            new Transition { SourceActivityId = "review-decision", SourceOutcomeName = "False", DestinationActivityId = "performance-improvement" },
            new Transition { SourceActivityId = "continue-onboarding", SourceOutcomeName = "Continued", DestinationActivityId = "ninety-day-review" },
            new Transition { SourceActivityId = "performance-improvement", SourceOutcomeName = "Created", DestinationActivityId = "ninety-day-review" },
            new Transition { SourceActivityId = "ninety-day-review", SourceOutcomeName = "Timer", DestinationActivityId = "complete-onboarding" }
        });

        return workflowType;
    }
}

// Custom HR Activities
public class AssignBuddyTask : TaskActivity<AssignBuddyTask>
{
    private readonly IBuddyAssignmentService _buddyService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public AssignBuddyTask(
        IBuddyAssignmentService buddyService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<AssignBuddyTask> localizer)
    {
        _buddyService = buddyService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Assign Buddy Task"];
    public override LocalizedString Category => S["HR Management"];

    public WorkflowExpression<string> NewEmployeeId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Department
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public bool PreferSameDepartment
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public bool ExcludeManagers
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public string BuddyDuration
    {
        get => GetProperty(() => "P90D");
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Assigned"], S["NoBuddyAvailable"], S["Error"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var newEmployeeId = await _expressionEvaluator.EvaluateAsync(NewEmployeeId, workflowContext, null);
            var department = await _expressionEvaluator.EvaluateAsync(Department, workflowContext, null);

            var buddyRequest = new BuddyAssignmentRequest
            {
                NewEmployeeId = newEmployeeId,
                Department = department,
                PreferSameDepartment = PreferSameDepartment,
                ExcludeManagers = ExcludeManagers,
                Duration = TimeSpan.Parse(BuddyDuration),
                RequestDate = DateTime.UtcNow
            };

            var buddyAssignment = await _buddyService.AssignBuddyAsync(buddyRequest);

            if (buddyAssignment.Success)
            {
                // Store buddy assignment data
                workflowContext.Properties["BuddyAssignment"] = new
                {
                    BuddyId = buddyAssignment.BuddyId,
                    BuddyName = buddyAssignment.BuddyName,
                    BuddyEmail = buddyAssignment.BuddyEmail,
                    BuddyDepartment = buddyAssignment.BuddyDepartment,
                    AssignmentDate = buddyAssignment.AssignmentDate,
                    EndDate = buddyAssignment.EndDate
                };

                workflowContext.Properties["BuddyAssignedAt"] = DateTime.UtcNow;

                // Send notification to buddy
                await _buddyService.NotifyBuddyAsync(buddyAssignment.BuddyId, newEmployeeId);

                return Outcomes("Assigned");
            }
            else if (buddyAssignment.NoBuddyAvailable)
            {
                workflowContext.Properties["BuddyAssignmentIssue"] = "No suitable buddy available";
                return Outcomes("NoBuddyAvailable");
            }
            else
            {
                workflowContext.Properties["BuddyAssignmentError"] = buddyAssignment.ErrorMessage;
                return Outcomes("Error");
            }
        }
        catch (Exception ex)
        {
            workflowContext.Properties["BuddyAssignmentError"] = ex.Message;
            return Outcomes("Error");
        }
    }
}

public class CheckTrainingProgressTask : TaskActivity<CheckTrainingProgressTask>
{
    private readonly ITrainingService _trainingService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    private readonly IStringLocalizer S;

    public CheckTrainingProgressTask(
        ITrainingService trainingService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<CheckTrainingProgressTask> localizer)
    {
        _trainingService = trainingService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
    }

    public override LocalizedString DisplayText => S["Check Training Progress Task"];
    public override LocalizedString Category => S["Training Management"];

    public WorkflowExpression<string> EmployeeId
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public int ExpectedProgressRate
    {
        get => GetProperty(() => 25);
        set => SetProperty(value);
    }

    public bool SendReminders
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        return Outcomes(S["Checked"], S["BehindSchedule"], S["Error"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        try
        {
            var employeeId = await _expressionEvaluator.EvaluateAsync(EmployeeId, workflowContext, null);

            var trainingProgress = await _trainingService.GetTrainingProgressAsync(employeeId);

            if (trainingProgress == null)
            {
                workflowContext.Properties["TrainingProgressError"] = "Training progress data not found";
                return Outcomes("Error");
            }

            // Calculate expected vs actual progress
            var startDate = trainingProgress.StartDate;
            var weeksSinceStart = (DateTime.UtcNow - startDate).Days / 7.0;
            var expectedProgress = Math.Min(100, weeksSinceStart * ExpectedProgressRate);

            // Store progress data
            workflowContext.Properties["TrainingProgress"] = new
            {
                CompletionPercentage = trainingProgress.CompletionPercentage,
                ExpectedProgress = expectedProgress,
                WeeksSinceStart = weeksSinceStart,
                CompletedModules = trainingProgress.CompletedModules,
                TotalModules = trainingProgress.TotalModules,
                LastAccessDate = trainingProgress.LastAccessDate,
                TimeSpent = trainingProgress.TotalTimeSpent
            };

            workflowContext.Properties["TrainingProgressCheckedAt"] = DateTime.UtcNow;

            if (trainingProgress.CompletionPercentage < expectedProgress)
            {
                if (SendReminders)
                {
                    await _trainingService.SendTrainingReminderAsync(employeeId, trainingProgress);
                }

                workflowContext.Properties["ProgressGap"] = expectedProgress - trainingProgress.CompletionPercentage;
                return Outcomes("BehindSchedule");
            }

            return Outcomes("Checked");
        }
        catch (Exception ex)
        {
            workflowContext.Properties["TrainingProgressError"] = ex.Message;
            return Outcomes("Error");
        }
    }
}
```

#### **Kết quả:**
- ✅ **Tự động hóa onboarding**: Từ hire đến 90-day review
- ✅ **IT setup tự động**: Accounts, equipment, software licenses
- ✅ **Buddy program**: Automatic buddy assignment và management
- ✅ **Training tracking**: Progress monitoring với reminders
- ✅ **Performance reviews**: 30-day và 90-day check-ins

---

## 🎯 **KHI NÀO CẦN SỬ DỤNG WORKFLOW INTEGRATION?**

### **✅ NÊN DÙNG KHI:**

#### **1. 🔄 Long-running Business Processes**
- **Multi-step processes** kéo dài nhiều ngày/tuần/tháng
- **Human involvement** cần approval, review, decision making
- **Time-based triggers** như reminders, deadlines, recurring tasks
- **Complex business rules** với nhiều conditions và branches

#### **2. 🏢 Cross-system Integration**
- **Multiple systems** cần coordinate với nhau
- **External API calls** với error handling và retry logic
- **Data synchronization** across different platforms
- **Event-driven architecture** với loose coupling

#### **3. 📋 Compliance & Audit Requirements**
- **Audit trail** cần track toàn bộ process
- **Regulatory compliance** với specific workflows
- **Document approval** với proper authorization
- **Quality control** với checkpoints và validations

#### **4. 🚀 Scalable Automation**
- **High-volume processing** cần automation
- **Parallel execution** của multiple tasks
- **Resource optimization** với intelligent scheduling
- **Performance monitoring** và optimization

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Simple Synchronous Operations**
- **Single-step processes** hoàn thành ngay lập tức
- **Direct API calls** đủ đáp ứng requirements
- **Simple CRUD operations** không cần orchestration
- **Static workflows** không thay đổi logic

#### **2. 🚀 Real-time Requirements**
- **Ultra-low latency** applications
- **Real-time streaming** data processing
- **Simple event handling** không cần persistence
- **Immediate response** requirements

---

## 💡 **KINH NGHIỆM THỰC TẾ**

### **🔥 Best Practices:**

#### **1. Workflow Design Principles**
```csharp
// Modular và reusable workflows
public class ModularWorkflowDesign
{
    // Tách thành sub-workflows nhỏ
    public static WorkflowType BuildMainWorkflow()
    {
        return new WorkflowType
        {
            Activities = new[]
            {
                // Main workflow chỉ orchestrate
                CreateSubWorkflowActivity("UserRegistration"),
                CreateSubWorkflowActivity("EmailVerification"),
                CreateSubWorkflowActivity("ProfileSetup"),
                CreateSubWorkflowActivity("WelcomeSequence")
            }
        };
    }
    
    // Mỗi sub-workflow có thể reuse
    public static WorkflowType BuildEmailVerificationWorkflow()
    {
        // Có thể dùng cho registration, password reset, etc.
        return new WorkflowType { /* ... */ };
    }
}
```

#### **2. Error Handling Strategy**
```csharp
public class RobustWorkflowActivity : TaskActivity<RobustWorkflowActivity>
{
    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        var retryCount = 0;
        var maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                var result = await PerformBusinessLogicAsync();
                
                // Log success
                workflowContext.Properties["LastSuccessAt"] = DateTime.UtcNow;
                return Outcomes("Success");
            }
            catch (TransientException ex)
            {
                retryCount++;
                workflowContext.Properties[$"RetryAttempt{retryCount}"] = new
                {
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
                
                if (retryCount >= maxRetries)
                {
                    // Escalate to manual intervention
                    return Outcomes("RequiresManualIntervention");
                }
                
                // Exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
            catch (PermanentException ex)
            {
                // No point retrying
                workflowContext.Properties["PermanentError"] = ex.Message;
                return Outcomes("Failed");
            }
        }
        
        return Outcomes("Failed");
    }
}
```

#### **3. Performance Monitoring**
```csharp
public class MonitoredWorkflowActivity : TaskActivity<MonitoredWorkflowActivity>
{
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;
    
    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext workflowContext, 
        ActivityContext activityContext)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await PerformActivityLogicAsync();
            
            stopwatch.Stop();
            
            // Record metrics
            _metrics.Measure.Timer.Time("workflow.activity.execution_time", stopwatch.Elapsed,
                new MetricTags("activity", Name, "outcome", result.Outcomes.FirstOrDefault()));
                
            _metrics.Measure.Counter.Increment("workflow.activity.success",
                new MetricTags("activity", Name));
                
            // Log performance
            if (stopwatch.ElapsedMilliseconds > 5000) // > 5 seconds
            {
                _logger.LogWarning("Slow workflow activity: {Activity} took {ElapsedMs}ms", 
                    Name, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _metrics.Measure.Counter.Increment("workflow.activity.failure",
                new MetricTags("activity", Name, "error", ex.GetType().Name));
                
            _logger.LogError(ex, "Workflow activity failed: {Activity}", Name);
            throw;
        }
    }
}
```

### **🚀 Tips Triển Khai:**

#### **1. Start Simple, Scale Gradually**
```csharp
// Phase 1: Basic workflow
public class SimpleWorkflow
{
    // Start -> Process -> End
}

// Phase 2: Add error handling
public class RobustWorkflow  
{
    // Start -> Validate -> Process -> Retry on Error -> End
}

// Phase 3: Add monitoring
public class MonitoredWorkflow
{
    // Add metrics, logging, alerts
}

// Phase 4: Add advanced features
public class AdvancedWorkflow
{
    // Parallel execution, conditional logic, sub-workflows
}
```

#### **2. Testing Strategy**
```csharp
[Test]
public async Task StudentEnrollmentWorkflow_Should_SendWelcomeEmail_WhenPaymentSucceeds()
{
    // Arrange
    var workflowType = StudentEnrollmentWorkflow.BuildStudentEnrollmentWorkflow();
    var input = new Dictionary<string, object>
    {
        ["Student"] = new Student { Email = "test@example.com" },
        ["Course"] = new Course { Price = 99.99m, Title = "Test Course" }
    };
    
    // Mock payment service
    _paymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
        .ReturnsAsync(new PaymentResult { Success = true });
    
    // Mock email service
    _emailService.Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
        .Returns(Task.CompletedTask);
    
    // Act
    var context = await _workflowManager.StartWorkflowAsync(workflowType.WorkflowTypeId, input);
    
    // Assert
    Assert.That(context.Workflow.Status, Is.EqualTo(WorkflowStatus.Executing));
    _emailService.Verify(x => x.SendAsync(It.Is<EmailMessage>(e => 
        e.Subject.Contains("Welcome to Test Course"))), Times.Once);
}
```

### **💡 KẾT LUẬN**

#### **Use cases chính cần Workflow Integration:**
- ✅ **E-learning**: Student enrollment, progress tracking, certification
- ✅ **Retail**: Inventory management, purchase orders, distribution
- ✅ **HR**: Employee onboarding, performance reviews, training
- ✅ **Healthcare**: Patient care workflows, treatment protocols
- ✅ **Finance**: Loan approval, compliance processes, audit trails

#### **Lợi ích:**
- 🎯 **Automation**: Giảm manual work, tăng consistency
- 🔄 **Orchestration**: Coordinate multiple systems và processes
- 📊 **Visibility**: Monitor và track process execution
- ⚡ **Scalability**: Handle high-volume operations efficiently
- 🔧 **Flexibility**: Easy to modify và extend workflows

#### **Key Success Factors:**
- **Start simple** và gradually add complexity
- **Design for failure** với proper error handling
- **Monitor performance** và optimize bottlenecks
- **Test thoroughly** với realistic scenarios
- **Document workflows** cho maintenance

**Workflow Integration là game-changer cho các hệ thống enterprise cần automation và process orchestration! 🚀**

---

## 🎯 **KHI NÀO CẦN WORKFLOW INTEGRATION - VÍ DỤ THỰC TẾ**

### **1. 📋 Content Publishing Workflow - Website Tin Tức**

#### **Tình huống:**
Anh quản lý **website tin tức** với đội ngũ: phóng viên, biên tập viên, tổng biên tập. Cần quy trình phê duyệt bài viết trước khi đăng.

#### **❌ TRƯỚC KHI DÙNG WORKFLOW:**
```csharp
// Code thủ công - dễ sai sót
public async Task<IActionResult> PublishArticle(int articleId)
{
    var article = await _contentManager.GetAsync(articleId);
    
    // Phóng viên tự publish - không có kiểm duyệt
    article.Published = true;
    await _contentManager.UpdateAsync(article);
    
    // Quên gửi email thông báo
    // Không có audit trail
    // Không kiểm tra chất lượng
    
    return Ok();
}
```

#### **✅ SAU KHI DÙNG WORKFLOW:**
```csharp
// Workflow tự động - chuẩn chỉnh
public class ArticlePublishingWorkflow
{
    public static WorkflowType BuildWorkflow()
    {
        return new WorkflowType
        {
            WorkflowTypeId = "ArticlePublishingWorkflow",
            Name = "Article Publishing Process",
            Activities = new[]
            {
                // 1. Phóng viên submit bài
                new ActivityRecord
                {
                    ActivityId = "article-submitted",
                    Name = "ArticleSubmittedEvent"
                },
                
                // 2. Kiểm tra chất lượng tự động
                new ActivityRecord
                {
                    ActivityId = "quality-check",
                    Name = "ArticleQualityCheckTask",
                    Properties = JObject.FromObject(new
                    {
                        MinWordCount = 300,
                        CheckSpelling = true,
                        CheckPlagiarism = true,
                        RequiredSections = "Title,Summary,Content,Tags"
                    })
                },
                
                // 3. Gửi cho biên tập viên
                new ActivityRecord
                {
                    ActivityId = "assign-editor",
                    Name = "AssignToEditorTask",
                    Properties = JObject.FromObject(new
                    {
                        EditorRole = "Editor",
                        Priority = "{{ Article.Priority }}",
                        Deadline = "{{ Article.PublishDate.AddDays(-1) }}"
                    })
                },
                
                // 4. Chờ biên tập viên review
                new ActivityRecord
                {
                    ActivityId = "wait-editor-review",
                    Name = "EditorReviewEvent"
                },
                
                // 5. Quyết định của biên tập viên
                new ActivityRecord
                {
                    ActivityId = "editor-decision",
                    Name = "IfElseTask",
                    Properties = JObject.FromObject(new
                    {
                        Condition = "{{ EditorReview.Decision == 'Approved' }}"
                    })
                },
                
                // 6a. Nếu approved -> Gửi tổng biên tập (bài quan trọng)
                new ActivityRecord
                {
                    ActivityId = "check-importance",
                    Name = "IfElseTask",
                    Properties = JObject.FromObject(new
                    {
                        Condition = "{{ Article.Category == 'Breaking' || Article.Category == 'Politics' }}"
                    })
                },
                
                // 6b. Tổng biên tập review
                new ActivityRecord
                {
                    ActivityId = "chief-editor-review",
                    Name = "ChiefEditorReviewEvent"
                },
                
                // 7. Publish bài viết
                new ActivityRecord
                {
                    ActivityId = "publish-article",
                    Name = "PublishArticleTask",
                    Properties = JObject.FromObject(new
                    {
                        ArticleId = "{{ Article.ArticleId }}",
                        PublishDate = "{{ Article.ScheduledPublishDate }}",
                        NotifySubscribers = true,
                        PostToSocialMedia = "{{ Article.SocialMediaEnabled }}"
                    })
                },
                
                // 8. Thông báo hoàn thành
                new ActivityRecord
                {
                    ActivityId = "notify-completion",
                    Name = "SendEmailTask",
                    Properties = JObject.FromObject(new
                    {
                        Recipients = "{{ Article.Author.Email }}, editors@newspaper.com",
                        Subject = "Article Published: {{ Article.Title }}",
                        Body = @"
                            Your article has been published successfully!
                            
                            Title: {{ Article.Title }}
                            URL: {{ Article.PublicUrl }}
                            Published: {{ Article.PublishedDate }}
                            
                            Analytics will be available in 24 hours.
                        "
                    })
                },
                
                // 9. Lên lịch báo cáo analytics
                new ActivityRecord
                {
                    ActivityId = "schedule-analytics",
                    Name = "TimerEvent",
                    Properties = JObject.FromObject(new
                    {
                        StartAt = "{{ Article.PublishedDate.AddDays(1) }}",
                        IsRecurring = false
                    })
                },
                
                // 10. Gửi báo cáo analytics
                new ActivityRecord
                {
                    ActivityId = "send-analytics",
                    Name = "SendAnalyticsReportTask",
                    Properties = JObject.FromObject(new
                    {
                        ArticleId = "{{ Article.ArticleId }}",
                        Recipients = "{{ Article.Author.Email }}",
                        IncludeMetrics = "Views,Shares,Comments,ReadTime"
                    })
                }
            }
        };
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Workflow** | **Sau Workflow** |
|-------------------|------------------|
| ❌ Phóng viên tự publish | ✅ Quy trình phê duyệt 3 cấp |
| ❌ Không kiểm tra chất lượng | ✅ Tự động check: từ vựng, chính tả, đạo văn |
| ❌ Quên thông báo | ✅ Tự động email tất cả stakeholders |
| ❌ Không có audit trail | ✅ Lưu lại toàn bộ quá trình |
| ❌ Không theo dõi hiệu quả | ✅ Báo cáo analytics tự động |
| ❌ Xử lý thủ công, dễ sai | ✅ Tự động 100%, nhất quán |

---

### **2. 🛒 E-commerce Customer Service Workflow**

#### **Tình huống:**
Anh điều hành **shop online** với nhiều đơn hàng. Khách hàng thường gặp vấn đề: đổi trả, hoàn tiền, khiếu nại. Cần quy trình xử lý tự động.

#### **❌ TRƯỚC KHI DÙNG WORKFLOW:**
```csharp
// Xử lý thủ công - không nhất quán
public async Task<IActionResult> HandleCustomerComplaint(CustomerComplaint complaint)
{
    // Nhân viên tự quyết định - không có quy trình
    if (complaint.Amount < 100)
    {
        // Tự động refund - rủi ro cao
        await ProcessRefund(complaint.OrderId, complaint.Amount);
    }
    else
    {
        // Gửi email cho manager - có thể quên
        await SendEmail("manager@shop.com", "Customer complaint", complaint.Description);
    }
    
    // Không theo dõi thời gian xử lý
    // Không thông báo khách hàng về tiến độ
    // Không có escalation khi quá hạn
    
    return Ok();
}
```

#### **✅ SAU KHI DÙNG WORKFLOW:**
```csharp
// Customer Service Workflow - Chuyên nghiệp
public class CustomerServiceWorkflow
{
    public static WorkflowType BuildWorkflow()
    {
        return new WorkflowType
        {
            WorkflowTypeId = "CustomerServiceWorkflow",
            Name = "Customer Service Process",
            Activities = new[]
            {
                // 1. Khách hàng gửi yêu cầu
                new ActivityRecord
                {
                    ActivityId = "request-received",
                    Name = "CustomerRequestEvent",
                    Properties = JObject.FromObject(new
                    {
                        RequestTypes = "Refund,Exchange,Complaint,Question"
                    })
                },
                
                // 2. Tự động phân loại yêu cầu
                new ActivityRecord
                {
                    ActivityId = "classify-request",
                    Name = "ClassifyRequestTask",
                    Properties = JObject.FromObject(new
                    {
                        UseAI = true,
                        Categories = "Refund,Exchange,Complaint,Technical,General",
                        ConfidenceThreshold = 0.8
                    })
                },
                
                // 3. Gửi email xác nhận cho khách hàng
                new ActivityRecord
                {
                    ActivityId = "send-acknowledgment",
                    Name = "SendEmailTask",
                    Properties = JObject.FromObject(new
                    {
                        Recipients = "{{ CustomerRequest.Email }}",
                        Subject = "Đã nhận yêu cầu #{{ CustomerRequest.TicketNumber }}",
                        Body = @"
                            Chào {{ CustomerRequest.CustomerName }},
                            
                            Chúng tôi đã nhận được yêu cầu của bạn:
                            - Mã số: #{{ CustomerRequest.TicketNumber }}
                            - Loại: {{ Classification.Category }}
                            - Thời gian xử lý dự kiến: {{ Classification.EstimatedTime }}
                            
                            Chúng tôi sẽ liên hệ trong vòng {{ Classification.ResponseTime }}.
                            
                            Trân trọng,
                            Đội ngũ CSKH
                        "
                    })
                },
                
                // 4. Quyết định luồng xử lý
                new ActivityRecord
                {
                    ActivityId = "routing-decision",
                    Name = "IfElseTask",
                    Properties = JObject.FromObject(new
                    {
                        Condition = @"
                            {{ Classification.Category == 'Refund' && 
                               CustomerRequest.Amount <= 500000 && 
                               CustomerRequest.OrderAge <= 7 }}"
                    })
                },
                
                // 5a. Tự động xử lý (đơn giản)
                new ActivityRecord
                {
                    ActivityId = "auto-process",
                    Name = "AutoProcessRequestTask",
                    Properties = JObject.FromObject(new
                    {
                        RequestType = "{{ Classification.Category }}",
                        OrderId = "{{ CustomerRequest.OrderId }}",
                        Amount = "{{ CustomerRequest.Amount }}",
                        AutoApprovalLimit = 500000
                    })
                },
                
                // 5b. Giao cho nhân viên (phức tạp)
                new ActivityRecord
                {
                    ActivityId = "assign-to-agent",
                    Name = "AssignToAgentTask",
                    Properties = JObject.FromObject(new
                    {
                        Department = "{{ Classification.Department }}",
                        Priority = "{{ Classification.Priority }}",
                        Skills = "{{ Classification.RequiredSkills }}",
                        SLA = "{{ Classification.SLA }}"
                    })
                }
            }
        };
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Workflow** | **Sau Workflow** |
|-------------------|------------------|
| ❌ Xử lý thủ công, không nhất quán | ✅ Quy trình chuẩn, tự động phân loại |
| ❌ Không thông báo khách hàng | ✅ Email xác nhận + cập nhật tiến độ |
| ❌ Không theo dõi SLA | ✅ Tự động escalate khi quá hạn |
| ❌ Không có feedback loop | ✅ Survey khách hàng + phân tích |
| ❌ Nhân viên tự quyết định | ✅ Auto-process đơn giản, manual review phức tạp |
| ❌ Không có audit trail | ✅ Lưu lại toàn bộ quá trình xử lý |

---

## 💡 **TÓM TẮT KHI NÀO CẦN WORKFLOW INTEGRATION**

### **✅ CẦN DÙNG KHI:**

#### **1. 🔄 Quy trình có nhiều bước tuần tự**
- **Ví dụ**: Phê duyệt bài viết (Submit → Review → Approve → Publish)
- **Lợi ích**: Đảm bảo không bỏ sót bước nào, có audit trail

#### **2. ⏰ Cần theo dõi thời gian và deadline**
- **Ví dụ**: SLA customer service, deadline nộp học phí
- **Lợi ích**: Tự động nhắc nhở, escalation khi quá hạn

#### **3. 🤖 Kết hợp xử lý tự động và thủ công**
- **Ví dụ**: Auto-process đơn giản, manual review phức tạp
- **Lợi ích**: Tăng hiệu quả, giảm workload cho nhân viên

#### **4. 📧 Cần thông báo nhiều bên liên quan**
- **Ví dụ**: Thông báo tác giả, biên tập viên, độc giả
- **Lợi ích**: Không ai bị bỏ sót, thông tin nhất quán

#### **5. 🔀 Logic phức tạp với nhiều điều kiện**
- **Ví dụ**: Phân loại yêu cầu, routing theo skills
- **Lợi ích**: Logic rõ ràng, dễ maintain và modify

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Quy trình đơn giản 1-2 bước**
- **Ví dụ**: Đăng ký newsletter, reset password
- **Lý do**: Overhead không cần thiết

#### **2. ⚡ Cần response time thấp**
- **Ví dụ**: Real-time chat, gaming
- **Lý do**: Workflow engine tạo latency

#### **3. 🔒 Quy trình ít thay đổi**
- **Ví dụ**: Static approval, fixed process
- **Lý do**: Simple code đủ rồi

### **🎯 KẾT LUẬN:**
**Workflow Integration phù hợp nhất cho các hệ thống enterprise với quy trình phức tạp, nhiều stakeholders, cần automation và audit trail!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*