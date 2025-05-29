# Architecture and Workflow Diagrams

## System Architecture

```mermaid
graph TB
    Client[Client Application] --> |HTTP Request| APIG[API Gateway]
    APIG --> |Invoke| Lambda[AWS Lambda]
    Lambda --> |Query| DynamoDB[(DynamoDB)]
    Lambda --> |Cache| MemoryCache[(Memory Cache)]
    Lambda --> |Metrics| CloudWatch[CloudWatch]
    Lambda --> |Traces| XRay[X-Ray]
    Lambda --> |Logs| CloudWatchLogs[CloudWatch Logs]
    
    subgraph AWS Cloud
        APIG
        Lambda
        DynamoDB
        MemoryCache
        CloudWatch
        XRay
        CloudWatchLogs
    end
```

## Request Flow

```mermaid
sequenceDiagram
    participant Client
    participant APIG as API Gateway
    participant Lambda
    participant Cache
    participant DynamoDB
    participant CloudWatch

    Client->>APIG: HTTP Request
    APIG->>Lambda: Invoke Function
    Lambda->>Cache: Check Cache
    alt Cache Hit
        Cache-->>Lambda: Return Cached Data
    else Cache Miss
        Lambda->>DynamoDB: Query Records
        DynamoDB-->>Lambda: Return Records
        Lambda->>Cache: Update Cache
    end
    Lambda->>CloudWatch: Record Metrics
    Lambda-->>APIG: Return Response
    APIG-->>Client: HTTP Response
```

## Monitoring Architecture

```mermaid
graph TB
    subgraph Application
        Metrics[Metrics Service]
        Logs[Logging Service]
        Traces[Tracing Service]
    end

    subgraph AWS Services
        CloudWatch[CloudWatch]
        XRay[X-Ray]
        SNS[SNS]
        Dashboard[CloudWatch Dashboard]
    end

    Metrics --> |PutMetricData| CloudWatch
    Logs --> |WriteLogs| CloudWatch
    Traces --> |SendTraces| XRay
    CloudWatch --> |Alarms| SNS
    CloudWatch --> |Visualization| Dashboard
```

## Testing Architecture

```mermaid
graph TB
    subgraph Test Types
        Unit[Unit Tests]
        Integration[Integration Tests]
        Performance[Performance Tests]
    end

    subgraph Test Components
        Mock[Mock Services]
        TestDB[Test DynamoDB]
        TestCache[Test Cache]
    end

    subgraph Test Execution
        Local[Local Execution]
        CI[CI/CD Pipeline]
        Reports[Test Reports]
    end

    Unit --> Mock
    Integration --> TestDB
    Integration --> TestCache
    Performance --> TestDB
    Performance --> TestCache

    Unit --> Local
    Integration --> Local
    Performance --> Local
    Unit --> CI
    Integration --> CI
    Performance --> CI
    Local --> Reports
    CI --> Reports
```

## Backup and Recovery Flow

```mermaid
graph TB
    subgraph Backup Process
        PITR[Point-in-Time Recovery]
        OnDemand[On-Demand Backup]
        Config[Config Backup]
        Logs[Log Backup]
    end

    subgraph Recovery Process
        RestorePITR[Restore from PITR]
        RestoreBackup[Restore from Backup]
        RestoreConfig[Restore Config]
        RestoreLogs[Restore Logs]
    end

    subgraph Monitoring
        BackupMonitor[Backup Monitoring]
        HealthCheck[Health Checks]
        Alert[Alerts]
    end

    PITR --> BackupMonitor
    OnDemand --> BackupMonitor
    Config --> BackupMonitor
    Logs --> BackupMonitor

    BackupMonitor --> HealthCheck
    HealthCheck --> Alert

    RestorePITR --> HealthCheck
    RestoreBackup --> HealthCheck
    RestoreConfig --> HealthCheck
    RestoreLogs --> HealthCheck
```

## Security Architecture

```mermaid
graph TB
    subgraph Authentication
        APIKey[API Key Auth]
        IAM[IAM Auth]
        Cognito[Cognito Auth]
    end

    subgraph Authorization
        Policies[IAM Policies]
        Roles[IAM Roles]
        Permissions[Resource Permissions]
    end

    subgraph Data Protection
        Encryption[Data Encryption]
        SSL[SSL/TLS]
        Headers[Security Headers]
    end

    subgraph Monitoring
        CloudTrail[CloudTrail]
        GuardDuty[GuardDuty]
        WAF[WAF]
    end

    APIKey --> Policies
    IAM --> Policies
    Cognito --> Policies
    Policies --> Roles
    Roles --> Permissions

    Encryption --> Data Protection
    SSL --> Data Protection
    Headers --> Data Protection

    CloudTrail --> Monitoring
    GuardDuty --> Monitoring
    WAF --> Monitoring
```

## Development Workflow

```mermaid
graph LR
    subgraph Development
        LocalDev[Local Development]
        Testing[Testing]
        CodeReview[Code Review]
    end

    subgraph CI/CD
        Build[Build]
        Test[Automated Tests]
        Deploy[Deploy]
    end

    subgraph Monitoring
        Metrics[Metrics]
        Logs[Logs]
        Alerts[Alerts]
    end

    LocalDev --> Testing
    Testing --> CodeReview
    CodeReview --> Build
    Build --> Test
    Test --> Deploy
    Deploy --> Metrics
    Metrics --> Logs
    Logs --> Alerts
```

## Cache Architecture

```mermaid
graph TB
    subgraph Cache Layer
        MemoryCache[Memory Cache]
        CachePolicy[Cache Policy]
        CacheMetrics[Cache Metrics]
    end

    subgraph Cache Operations
        Get[Get from Cache]
        Set[Set in Cache]
        Invalidate[Invalidate Cache]
    end

    subgraph Cache Monitoring
        HitRate[Cache Hit Rate]
        MissRate[Cache Miss Rate]
        Size[Cache Size]
    end

    MemoryCache --> Get
    MemoryCache --> Set
    MemoryCache --> Invalidate
    CachePolicy --> Get
    CachePolicy --> Set
    CachePolicy --> Invalidate
    Get --> CacheMetrics
    Set --> CacheMetrics
    Invalidate --> CacheMetrics
    CacheMetrics --> HitRate
    CacheMetrics --> MissRate
    CacheMetrics --> Size
```

## Error Handling Flow

```mermaid
graph TB
    subgraph Error Detection
        Validation[Input Validation]
        Exception[Exception Handling]
        Timeout[Timeout Handling]
    end

    subgraph Error Processing
        Log[Error Logging]
        Metrics[Error Metrics]
        Alert[Error Alerting]
    end

    subgraph Error Recovery
        Retry[Retry Logic]
        Fallback[Fallback Strategy]
        CircuitBreaker[Circuit Breaker]
    end

    Validation --> Exception
    Exception --> Timeout
    Timeout --> Log
    Log --> Metrics
    Metrics --> Alert
    Alert --> Retry
    Retry --> Fallback
    Fallback --> CircuitBreaker
```

## Deployment Architecture

```mermaid
graph TB
    subgraph Source Control
        GitHub[GitHub Repository]
        Branch[Feature Branch]
        Main[Main Branch]
    end

    subgraph CI/CD Pipeline
        Build[Build Process]
        Test[Test Process]
        Deploy[Deploy Process]
    end

    subgraph Environments
        Dev[Development]
        Staging[Staging]
        Prod[Production]
    end

    GitHub --> Branch
    Branch --> Main
    Main --> Build
    Build --> Test
    Test --> Deploy
    Deploy --> Dev
    Dev --> Staging
    Staging --> Prod
```

## Query Request Handling

```mermaid
flowchart TB
    Start([Start]) --> Validate[Validate Request]
    Validate --> CheckFields{Check Fields}
    
    CheckFields -->|userId only| UserQuery[Query by UserId]
    CheckFields -->|systemId only| SystemQuery[Query by SystemId]
    CheckFields -->|both fields| CombinedQuery[Query by Both]
    CheckFields -->|neither field| Error[Return Error]
    
    UserQuery --> BuildUserQuery[Build UserId Query]
    SystemQuery --> BuildSystemQuery[Build SystemId Query]
    CombinedQuery --> BuildCombinedQuery[Build Combined Query]
    
    BuildUserQuery --> ExecuteQuery[Execute DynamoDB Query]
    BuildSystemQuery --> ExecuteQuery
    BuildCombinedQuery --> ExecuteQuery
    
    ExecuteQuery --> CheckCache{Cache Check}
    CheckCache -->|Cache Hit| ReturnCached[Return Cached Data]
    CheckCache -->|Cache Miss| QueryDynamoDB[Query DynamoDB]
    
    QueryDynamoDB --> UpdateCache[Update Cache]
    UpdateCache --> ReturnResults[Return Results]
    ReturnCached --> ReturnResults
    
    ReturnResults --> End([End])
    Error --> End

    subgraph Query Building
        BuildUserQuery
        BuildSystemQuery
        BuildCombinedQuery
    end

    subgraph Query Execution
        ExecuteQuery
        CheckCache
        QueryDynamoDB
        UpdateCache
    end
```

## Query Request Sequence

```mermaid
sequenceDiagram
    participant Client
    participant API as API Gateway
    participant Lambda
    participant Validator as Request Validator
    participant QueryBuilder as Query Builder
    participant Cache
    participant DynamoDB

    Client->>API: POST /api/query
    API->>Lambda: Invoke Function
    
    Lambda->>Validator: Validate Request
    Note over Validator: Check for at least one<br/>of userId or systemId
    
    alt Valid Request
        Validator->>QueryBuilder: Build Query
        Note over QueryBuilder: Build query based on<br/>available fields
        
        QueryBuilder->>Cache: Check Cache
        alt Cache Hit
            Cache-->>Lambda: Return Cached Data
        else Cache Miss
            QueryBuilder->>DynamoDB: Execute Query
            DynamoDB-->>QueryBuilder: Return Results
            QueryBuilder->>Cache: Update Cache
        end
        
        Lambda-->>API: Return Response
        API-->>Client: 200 OK
    else Invalid Request
        Validator-->>Lambda: Validation Error
        Lambda-->>API: 400 Bad Request
        API-->>Client: Error Response
    end
```

## Query Field Combinations

```mermaid
graph TB
    subgraph Query Fields
        UserId[User ID]
        SystemId[System ID]
        StartDate[Start Date]
        EndDate[End Date]
        ResourceId[Resource ID]
    end

    subgraph Valid Combinations
        UserOnly[User ID Only]
        SystemOnly[System ID Only]
        UserSystem[User ID + System ID]
        UserDate[User ID + Date Range]
        SystemDate[System ID + Date Range]
        AllFields[All Fields]
    end

    UserId --> UserOnly
    SystemId --> SystemOnly
    UserId --> UserSystem
    SystemId --> UserSystem
    UserId --> UserDate
    StartDate --> UserDate
    EndDate --> UserDate
    SystemId --> SystemDate
    StartDate --> SystemDate
    EndDate --> SystemDate
    UserId --> AllFields
    SystemId --> AllFields
    StartDate --> AllFields
    EndDate --> AllFields
    ResourceId --> AllFields
``` 