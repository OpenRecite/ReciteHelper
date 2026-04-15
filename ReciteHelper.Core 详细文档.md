# ReciteHelper.Core 详细文档

## 目录

1. [概述](#1-概述)
2. [项目结构](#2-项目结构)
3. [Aggregates（聚合根）](#3-aggregates聚合根)
4. [Entities（实体）](#4-entities实体)
5. [ValueObjects（值对象）](#5-valueobjects值对象)
6. [Enums（枚举）](#6-enums枚举)
7. [Interfaces（接口）](#7-interfaces接口)
8. [Exceptions（异常）](#8-exceptions异常)
9. [EventArgs（事件参数）](#9-eventargs事件参数)
10. [Configuration（配置）](#10-configuration配置)
11. [DDD 设计原则](#11-ddd-设计原则)

---

## 1. 概述

`ReciteHelper.Core` 是项目的**领域核心层**，采用**领域驱动设计（DDD）**架构。该层包含业务领域的核心概念、规则和逻辑，是整个应用程序的业务核心。

### 1.1 核心职责

- 定义领域模型（聚合根、实体、值对象）
- 实现领域特定的业务规则
- 维护领域的一致性和完整性
- 提供领域服务接口

### 1.2 设计原则

- **高内聚低耦合**：领域逻辑集中，不依赖外部实现
- **贫血模型避免**：实体包含业务方法，不只是数据容器
- **不可变性**：值对象一旦创建不可修改
- **领域事件**：支持领域事件的发布和订阅

---

## 2. 项目结构

```
ReciteHelper.Core/
├── Aggregates/          # 聚合根
│   └── Project.cs
├── Entities/            # 实体
│   ├── Chapter.cs
│   ├── Question.cs
│   └── ReviewTag.cs
├── ValueObjects/        # 值对象
│   ├── ChapterCluster.cs
│   ├── Chunk.cs
│   ├── ExamSettings.cs
│   ├── KnowledgePoint.cs
│   ├── LatestBuffer.cs
│   ├── Manifest.cs
│   ├── MergeFile.cs
│   ├── RecentProject.cs
│   └── Replay.cs
├── Enums/               # 枚举
│   ├── AnswerStatus.cs
│   ├── ExamAnswerStatus.cs
│   ├── FileClusterType.cs
│   ├── MissingStrategy.cs
│   └── ProjectTemplateType.cs
├── Interfaces/          # 接口
│   └── Services/
│       ├── IAnswerJudge.cs
│       ├── IPhonkService.cs
│       └── ISupermemoSerivce.cs
├── Exceptions/          # 异常
│   ├── ConfigurationException.cs
│   ├── DomainException.cs
│   └── ValidationException.cs
├── EventArgs/           # 事件参数
│   └── PhonkEventArgs.cs
└── Configuration/       # 配置
    ├── ConfigOptions.cs
    └── PhonkOptions.cs
```

---

## 3. Aggregates（聚合根）

### 3.1 Project

**文件路径**：`Aggregates/Project.cs`

**职责**：
- 项目的聚合根，协调项目内所有实体的行为
- 管理项目的生命周期
- 维护项目的一致性和完整性

**核心属性**：

| 属性名 | 类型 | 说明 |
|-------|------|------|
| `ProjectName` | `string?` | 项目名称 |
| `StoragePath` | `string?` | 存储路径 |
| `QuestionBankPath` | `string?` | 题库文件路径 |
| `Chapters` | `List<Chapter>?` | 章节列表 |
| `LastAccessed` | `DateTime` | 最后访问时间（私有 setter） |

**核心方法**：

```csharp
// 导出所有问题
public List<Question> ExportQuestions()

// 更新最后访问时间
public void UpdateLastAccessed()
```

**设计特点**：
- 继承自 `AggregateRoot` 基类
- `LastAccessed` 使用私有 setter，只能通过方法修改
- 包含业务方法 `UpdateLastAccessed()`

---

## 4. Entities（实体）

### 4.1 Question（问题实体）

**文件路径**：`Entities/Question.cs`

**职责**：
- 表示学习项目中的一个问题
- 管理问题的状态、内容和复习标签
- 维护 EF 值（记忆难度因子）

**核心属性**：

| 属性名 | 类型 | 说明 |
|-------|------|------|
| `Status` | `bool?` | 问题状态（正确/错误/未回答） |
| `Text` | `string?` | 问题文本 |
| `ReviewTag` | `List<ReviewTag>` | 复习标签列表（私有 setter） |
| `CorrectAnswer` | `string?` | 正确答案 |
| `EFValue` | `double` | 记忆难度因子，默认 2.5 |

**设计特点**：
- 继承自 `Entity` 基类，具有唯一标识
- `ReviewTag` 使用私有 setter，通过方法管理
- 默认 EF 值为 2.5，符合 SuperMemo 算法

### 4.2 Chapter（章节实体）

**文件路径**：`Entities/Chapter.cs`

**职责**：
- 表示项目中的一个章节
- 管理章节内的问题列表
- 维护章节的元数据

### 4.3 ReviewTag（复习标签实体）

**文件路径**：`Entities/ReviewTag.cs`

**职责**：
- 表示问题的复习标签
- 用于分类和组织复习内容

---

## 5. ValueObjects（值对象）

### 5.1 KnowledgePoint（知识点）

**文件路径**：`ValueObjects/KnowledgePoint.cs`

**职责**：
- 表示一个知识点
- 管理知识点的内容和掌握状态

**核心属性**：

| 属性名 | 类型 | 说明 |
|-------|------|------|
| `Name` | `string?` | 知识点名称（私有 setter） |
| `ContentMarkdown` | `string?` | 内容（Markdown 格式，私有 setter） |
| `IsMastered` | `bool` | 是否已掌握，默认 false（私有 setter） |

**核心方法**：

```csharp
// 修改掌握状态（返回新的值对象）
public KnowledgePoint ModifyMasteredStatus(bool newStatus)

// 克隆值对象
public override T Clone<T>()
```

**设计特点**：
- 继承自 `ValueObject` 基类
- 所有属性使用私有 setter，确保不可变性
- 修改状态返回新的值对象，遵循值对象不可变原则

### 5.2 ExamSettings（考试设置）

**文件路径**：`ValueObjects/ExamSettings.cs`

**职责**：
- 封装考试的相关设置
- 包括考试时间、题目数量等

### 5.3 ChapterCluster（章节聚类）

**文件路径**：`ValueObjects/ChapterCluster.cs`

**职责**：
- 表示章节的聚类信息
- 用于文件组织和分类

### 5.4 其他值对象

| 值对象 | 说明 |
|-------|------|
| `Chunk` | 文本块，用于内容分块处理 |
| `LatestBuffer` | 最新缓冲区，用于临时数据存储 |
| `Manifest` | 清单文件，描述项目结构 |
| `MergeFile` | 合并文件信息 |
| `RecentProject` | 最近项目信息 |
| `Replay` | 回放信息 |

---

## 6. Enums（枚举）

### 6.1 AnswerStatus（答案状态）

**文件路径**：`Enums/AnswerStatus.cs`

**值**：
- `Correct` - 正确
- `Incorrect` - 错误
- `Partial` - 部分正确
- `Unanswered` - 未回答

### 6.2 ExamAnswerStatus（考试答案状态）

**文件路径**：`Enums/ExamAnswerStatus.cs`

**值**：
- `Correct` - 正确
- `Incorrect` - 错误
- `Pending` - 待评分

### 6.3 FileClusterType（文件聚类类型）

**文件路径**：`Enums/FileClusterType.cs`

**说明**：定义文件的不同聚类类型，用于文件组织和管理。

### 6.4 MissingStrategy（缺失策略）

**文件路径**：`Enums/MissingStrategy.cs`

**说明**：定义数据缺失时的处理策略。

### 6.5 ProjectTemplateType（项目模板类型）

**文件路径**：`Enums/ProjectTemplateType.cs`

**说明**：定义不同类型的项目模板。

---

## 7. Interfaces（接口）

### 7.1 ISuperMemoService（超级记忆服务）

**文件路径**：`Interfaces/Services/ISupermemoSerivce.cs`

**职责**：
- 提供 SuperMemo 算法的实现
- 计算 EF 值和预测回答质量

**方法**：

```csharp
// 计算 EF 值
double CalculateEFValue(double currentEF, int quality);

// 预测回答质量
Task<int> PredictQualityAsync(double relativeRate, double similarity);
```

### 7.2 IPhonkService（Phonk 音效服务）

**文件路径**：`Interfaces/Services/IPhonkService.cs`

**职责**：
- 提供 Phonk 音效的播放功能
- 管理音效的状态和事件

**成员**：

```csharp
// 是否启用
bool IsEnabled { get; }

// 播放随机 Phonk 音效
Task PlayRandomPhonkAsync();

// Phonk 触发事件
event EventHandler<PhonkEventArgs>? PhonkTriggered;
```

### 7.3 IAnswerJudge（答案判断服务）

**文件路径**：`Interfaces/Services/IAnswerJudge.cs`

**职责**：
- 判断用户答案的正确性
- 计算答案相似度

**方法**：

```csharp
// 判断答案
Task<bool> JudgeAsync(string? userAnswer, string? correctAnswer);

// 计算相似度
Task<double> CalculateSimilarityAsync(string userAnswer, string correctAnswer);
```

---

## 8. Exceptions（异常）

### 8.1 DomainException（领域异常）

**文件路径**：`Exceptions/DomainException.cs`

**职责**：
- 领域层的基础异常类
- 用于表示领域特定的错误

### 8.2 ValidationException（验证异常）

**文件路径**：`Exceptions/ValidationException.cs`

**职责**：
- 表示验证失败的异常
- 包含验证错误的详细信息

### 8.3 ConfigurationException（配置异常）

**文件路径**：`Exceptions/ConfigurationException.cs`

**职责**：
- 表示配置错误的异常
- 用于处理配置相关的问题

---

## 9. EventArgs（事件参数）

### 9.1 PhonkEventArgs（Phonk 事件参数）

**文件路径**：`EventArgs/PhonkEventArgs.cs`

**职责**：
- 为 Phonk 相关事件提供数据
- 包含音效的元信息

---

## 10. Configuration（配置）

### 10.1 ConfigOptions（配置选项）

**文件路径**：`Configuration/ConfigOptions.cs`

**职责**：
- 定义应用程序的配置选项
- 支持配置的序列化和反序列化

### 10.2 PhonkOptions（Phonk 配置选项）

**文件路径**：`Configuration/PhonkOptions.cs`

**职责**：
- 定义 Phonk 音效的特定配置
- 包括启用状态、音量等设置

---

## 11. DDD 设计原则

### 11.1 聚合根（Aggregate Root）

**定义**：聚合是一组相关对象的集合，聚合根是聚合的入口点。

**在本项目中的体现**：
- `Project` 是聚合根，管理 `Chapter` 和 `Question`
- 外部只能通过 `Project` 访问聚合内部的对象
- 聚合根负责维护聚合内对象的一致性

### 11.2 实体（Entity）

**定义**：具有唯一标识的对象，标识在生命周期内保持不变。

**在本项目中的体现**：
- `Question`、`Chapter`、`ReviewTag` 都是实体
- 继承自 `Entity` 基类，具有 `Id` 属性
- 通过标识进行比较，而不是属性值

### 11.3 值对象（Value Object）

**定义**：通过属性值定义的对象，没有唯一标识，不可变。

**在本项目中的体现**：
- `KnowledgePoint`、`ExamSettings` 等是值对象
- 继承自 `ValueObject` 基类
- 修改时返回新的实例，保持不可变性

### 11.4 领域服务（Domain Service）

**定义**：封装不属于任何实体或值对象的业务逻辑。

**在本项目中的体现**：
- `ISuperMemoService` - SuperMemo 算法
- `IPhonkService` - Phonk 音效管理
- `IAnswerJudge` - 答案判断逻辑

### 11.5 依赖关系

```
ReciteHelper.Core
    ↓ 依赖
ReciteHelper.SharedKernel
```

**原则**：
- 领域核心层只依赖共享内核
- 不依赖基础设施层或表示层
- 通过接口定义服务契约，由基础设施层实现

---

## 12. 使用示例

### 12.1 创建项目

```csharp
var project = new Project
{
    ProjectName = "英语学习",
    StoragePath = "C:\\Projects\\English",
    Chapters = new List<Chapter>()
};

project.UpdateLastAccessed();
```

### 12.2 使用领域服务

```csharp
// 注入服务
public class QuizService
{
    private readonly ISuperMemoService _superMemoService;
    
    public QuizService(ISuperMemoService superMemoService)
    {
        _superMemoService = superMemoService;
    }
    
    public async Task ProcessAnswer(Question question, int quality)
    {
        // 计算新的 EF 值
        var newEF = _superMemoService.CalculateEFValue(question.EFValue, quality);
        question.EFValue = newEF;
    }
}
```

### 12.3 使用值对象

```csharp
// 创建知识点
var knowledgePoint = new KnowledgePoint
{
    Name = "过去时态",
    ContentMarkdown = "## 过去时态\n用于描述过去发生的动作..."
};

// 修改掌握状态（返回新的值对象）
var updatedPoint = knowledgePoint.ModifyMasteredStatus(true);
```

---

## 13. 总结

`ReciteHelper.Core` 作为领域核心层，遵循 DDD 设计原则，将业务逻辑集中在领域模型中。通过聚合根、实体、值对象和领域服务的合理划分，实现了高内聚低耦合的架构设计。

**关键特点**：
- 清晰的领域模型划分
- 严格的不可变性保证
- 完善的领域服务接口
- 统一的异常处理机制

这种设计为应用程序提供了坚实的业务基础，便于后续的扩展和维护。