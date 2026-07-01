# ReciteHelper

<p align="center">
  <img src="docs/Resources/Dev/photo.png" alt="ReciteHelper" />
</p>

**让课程资料从“存着以后看”变成真正可以练习、检索和复习的学习项目。**

ReciteHelper 是一款面向考试复习、课程学习与知识整理的开源 Windows 桌面应用。它能够读取学习资料，借助 AI 提取知识点并生成选择、填空、名词解释与解答题，也能从 PDF/TXT/HTML/MHTML 识别包含判断题在内的整套试卷，再通过本地文件知识库、智能判分和个性化复习，把一次性的资料处理变成持续可用的学习闭环。

无感化AI技术的应用，决定了它从不是在学习软件里简单塞入一个聊天框，而是让 AI 参与资料解析、题目生成、错题辅助与复习调度。每个项目的章节、题库、学习记录和知识库都围绕 项目文件组织；知识库采用项目目录内的文件存储，无需额外部署数据库或向量服务。

ReciteHelper 使用 C#、.NET 10 与 WPF 开发，并承诺保持开源、免费。如果这个项目帮助到了你，欢迎点亮 Star。

<div style="display: flex; align-items: center; justify-content: center;">
  <img src="docs/Resources/Logos/clublogo.png" style="height: 50px; width: auto;" />
  <span style="margin: 0 15px;">&nbsp;</span>
  <img src="docs/Resources/Logos/caylogo.png" style="height: 50px; width: auto;" />
</div>


---

## 核心能力

- **从资料自动生成学习项目**：读取文字型 PDF，或先将 DOCX、PPTX、PDF、TXT 等资料合并为 `.meg` 文件；自动完成文本读取、知识提取、章节聚类和向量生成。
- **五类题型独立交互**：支持选择、填空、判断、名词解释和解答题。选择与判断直接点选，填空按空位填写，名词解释与解答题使用语义相似度判定。
- **项目级本地知识库**：创建项目时同步构建文件型向量知识库，并随项目一同加载、导入和导出。资料规模较小时无需安装额外服务，也不依赖常驻后端。
- **错题检索与 AI 解析**：答错后可检索最相关的 3 个知识点，自动高亮与题目、答案重合的内容；需要时再将题目与检索结果交给 DeepSeek，生成有资料依据的针对性解析。
- **个性化智能复习**：根据答题表现筛选薄弱内容，并使用项目内的记忆模型安排后续练习，让复习重点随真实学习行为变化。
- **整卷导入与完整考试流程**：DeepSeek 可从 PDF/TXT/HTML/MHTML 中识别并分割多套试卷，保留答案、解析和自定义大小标题；也支持按章节权重自动组卷、限时考试、自动评分与错题回顾。
- **知识点学习与进度保存**：按章节浏览聚类后的知识点、标记掌握状态，并将做题记录和项目进度持续保存到本地。
- **学习内容游戏化**：可将现有 `.rhproj` 题库生成为视觉小说项目，用另一种方式回顾熟悉的知识内容（预览功能，6.0版本及以后可能会稳定）。

---

## 快速开始

### 普通用户
1. 前往 [Releases](https://github.com/ArabidopsisDev/ReciteHelper/releases) 下载最新版本。
2. 解压并运行 `ReciteHelper.exe`。
3. 在 `Config.xml` 中配置 DeepSeek API Key；使用知识库向量检索还需配置 Qwen API Key。两者均支持从环境变量读取。
4. 导入文字型 PDF，或使用文件合并工具整理多份资料，创建 `.rhproj` 学习项目。

详细的配置和使用方法请参阅[中文用户手册](docs/manual-cn.md)。

### 开发者构建
1. 安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 和支持 WPF 开发的 Visual Studio 或 JetBrains Rider。
2. 本项目包含 Git 子模块，请递归克隆仓库：

   ```bash
   git clone --recurse-submodules https://github.com/ArabidopsisDev/ReciteHelper.git
   ```

   已经克隆仓库时，可补充初始化子模块：

   ```bash
   git submodule update --init --recursive
   ```
3. 使用 IDE 打开 `src/ReciteHelper.slnx`，或通过命令行运行新架构下的 WPF 启动项目：

   ```bash
   dotnet restore src/ReciteHelper.slnx
   dotnet run --project ReciteHelper.Wpf/ReciteHelper.Wpf.csproj
   ```

---

## 如何贡献

非常欢迎社区贡献者参与！您的贡献将被永久记录在项目 README 文件的特别感谢一栏中。只需要：

1. Fork 本仓库，创建新分支进行开发；
2. 遵循现有代码风格，建议任何改动都配上适当的注释说明；
3. 代码提交前请确保能正常运行并通过基本测试；
4. 提 PR 前，请尽量关联 issue 或附带改动说明及截图（如有 UI 变更）；
5. 对于文档、翻译、测试用例也同样欢迎补充！

开发/讨论可以通过 [Issues](https://github.com/ArabidopsisDev/ReciteHelper/issues) 区或邮箱反馈。

---

## 许可证

本项目采用 **GNU AGPL v3.0** 或更高版本许可证分发。  
任何发布的衍生作品或者基于本项目进行的二次开发、SaaS 部署，均须共享完整源代码，并附带此协议原文说明。  
协议详细内容请参阅 [LICENSE](LICENSE) 文件或访问 [GNU 官网](https://www.gnu.org/licenses/agpl-3.0.html)。

---

## 用户手册

|语言|地址|
|:--:|:--:|
|中文（简体）|[中文用户手册](docs/manual-cn.md)|
|English|[English Manual](docs/manual-en.md)|

---

## 联系与反馈

- GitHub： [https://github.com/ArabidopsisDev/ReciteHelper](https://github.com/ArabidopsisDev/ReciteHelper)
- 邮箱：arab@methodbox.top
- QQ讨论群：1053379975

贡献前，请阅读[行为准则](CODE_OF_CONDUCT.md)。

欢迎提出建议、Bug 反馈或功能需求，PR 与 Issue 都会及时处理！

---

## 星标历史

[![Star History Chart](https://api.star-history.com/svg?repos=ArabidopsisDev/ReciteHelper&type=date&legend=top-left)](https://www.star-history.com/)

---

## 特别鸣谢

衷心感谢以下成员为项目做出的卓越贡献，他们的付出是项目成功的关键：

<div align="center">

| 头像 | 学校或单位 | 昵称 | 贡献内容 |
|:------:|:----------:|:-----:|------------|
| <img src="docs\Resources\Thanks\01.jpg" width="60" height="60" style="border-radius:50%;border:2px solid #4fc3f7"> | 南昌航空航天大学 |  **Sati** | 帮助测试并解决了多PDF的处理问题 |
| <img src="docs\Resources\Thanks\02.jpg" width="60" height="60" style="border-radius:50%;border:2px solid #55b74d"> | 海南大学 | **Mrwhite3142** | 帮助测试发现了文件无法正常处理的问题（暂未能复现） |
| <img src="docs\Resources\Thanks\03.png" width="60" height="60" style="border-radius:50%;border:2px solid #ffb74d"> | / | **oife** | 帮助测试并解决了无法正常加载项目和测试代码未删除的问题 |
| <img src="docs\Resources\Thanks\04.jpg" width="60" height="60" style="border-radius:50%;border:2px solid #aa274d"> | 中国民航大学 | **AcE77505** | 发现并为项目不支持部分旧版本 Windows 的问题提供了解决方案 |


</div>

<br>

> “众人拾柴火焰高”——感谢每一位贡献者的热情参与和无私奉献，你们的每一行代码、每一次测试、每一份文档都让这个项目更加完善。期待未来继续携手同行，创造更多精彩！

<br>
<div align="center">
<p style="color:#666;font-size:0.9em">
感谢阅读 · 持续更新中 · 更多贡献者欢迎加入！
</p>
</div>
