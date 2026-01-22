# 国际化规范

**🌐 Language**: [English](i18n-standards.md) | [中文](i18n-standards-cn.md)

## 1. 概述

本文档定义了ClashSubManager项目的国际化（i18n）规范，确保所有管理界面的一致多语言支持。

## 2. 支持语言

### 2.1 语言列表
- **英语 (en-US)**: 默认语言，所有缺失翻译的回退语言
- **简体中文 (zh-CN)**: 次要语言，完整UI翻译支持

### 2.2 语言优先级
1. **主要**: 英语 (en-US) - 默认和回退语言
2. **次要**: 简体中文 (zh-CN) - 完整翻译覆盖

## 3. 实施标准

### 3.1 技术栈
- **框架**: ASP.NET Core Localization
- **资源格式**: .resx文件
- **检测方式**: 浏览器Accept-Language头 + Cookie偏好
- **切换方式**: JavaScript + Cookie偏好存储

### 3.2 语言检测机制
```csharp
// 自动浏览器语言检测
var language = Request.GetBrowserLanguage();
if (language.StartsWith("zh")) {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
} else {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
}
```

### 3.3 语言切换界面
- **位置**: 管理员头部导航栏
- **方式**: 下拉切换按钮
- **存储**: 基于Cookie的语言偏好
- **行为**: 立即UI刷新，无需页面重载

## 4. 资源文件结构

### 4.1 目录组织
```
server/Resources/
├── Pages/
│   ├── Admin/
│   │   ├── Index.en.resx
│   │   ├── Index.zh-CN.resx
│   │   ├── Login.en.resx
│   │   ├── Login.zh-CN.resx
│   │   ├── DefaultIPs.en.resx
│   │   ├── DefaultIPs.zh-CN.resx
│   │   ├── ClashTemplate.en.resx
│   │   ├── ClashTemplate.zh-CN.resx
│   │   ├── UserConfig.en.resx
│   │   └── UserConfig.zh-CN.resx
│   └── Shared/
│       ├── _Layout.en.resx
│       ├── _Layout.zh-CN.resx
│       ├── _ValidationScripts.en.resx
│       └── _ValidationScripts.zh-CN.resx
└── Services/
    ├── Validation.en.resx
    └── Validation.zh-CN.resx
```

### 4.2 资源文件命名约定
- **格式**: `[文件名].[语言代码].resx`
- **语言代码**: `en` 表示英语，`zh-CN` 表示简体中文
- **位置**: 与源文件相同的目录结构

## 5. 翻译覆盖要求

### 5.1 完全覆盖区域
- **所有管理界面页面**: 登录、仪表板、IP管理、模板管理、用户配置
- **导航元素**: 菜单项、面包屑、页面标题
- **表单标签和占位符**: 所有输入字段、按钮、验证消息
- **错误消息**: 系统错误、验证错误、操作反馈
- **帮助文本**: 工具提示、描述、说明

### 5.2 部分覆盖区域
- **系统消息**: 日志条目、调试信息（仅英语）
- **代码注释**: 开发者文档（仅英语）
- **API响应**: 技术错误代码（仅英语）

### 5.3 无覆盖区域
- **控制台输出**: 应用程序日志、系统消息
- **配置键**: 环境变量名、设置键
- **数据库架构**: 表名、列名

## 6. 翻译质量标准

### 6.1 翻译原则
- **准确性**: 保持英语原文的准确含义
- **一致性**: 在所有页面使用一致的术语
- **清晰性**: 使用适合技术用户的清晰简单语言
- **文化适应**: 考虑中文翻译的文化背景

### 6.2 术语标准
| 英语术语 | 中文翻译 | 上下文 |
|----------|----------|--------|
| Admin | 管理员 | 用户角色 |
| Configuration | 配置 | 系统设置 |
| Subscription | 订阅 | Clash订阅 |
| Template | 模板 | Clash模板 |
| Optimized IP | 优选IP | 优化IP地址 |
| User ID | 用户ID | 用户标识符 |
| Login | 登录 | 认证操作 |
| Logout | 登出 | 认证操作 |
| Save | 保存 | 文件操作 |
| Delete | 删除 | 文件操作 |
| Edit | 编辑 | 修改操作 |

## 7. 实施指南

### 7.1 资源文件管理
- **同步**: 始终同时更新两种语言文件
- **验证**: 确保所有资源键在两种语言文件中都存在
- **测试**: 每次更新后验证语言切换正常工作

### 7.2 代码集成
```csharp
// 在Razor Pages中使用IStringLocalizer
@inject IStringLocalizer<Pages.Admin.Index> Localizer

<h1>@Localizer["PageTitle"]</h1>
<button>@Localizer["SaveButton"]</button>
```

```csharp
// 在Services中使用IStringLocalizer
public class SubscriptionService 
{
    private readonly IStringLocalizer<SubscriptionService> _localizer;
    
    public async Task<IActionResult> GetSubscription(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(_localizer["UserIdRequired"]);
        }
        // 实现逻辑
    }
}
```

### 7.3 JavaScript语言切换
```javascript
function switchLanguage(lang) {
    // 将语言偏好保存到Cookie
    document.cookie = `language=${lang}; path=/; max-age=31536000`;
    // 重新加载页面以应用语言更改
    location.reload();
}

function getCurrentLanguage() {
    return document.cookie.replace(/(?:(?:^|.*;\s*)language\s*\=\s*([^;]*).*$)|^.*$/, "$1") || 'en';
}
```

## 8. 测试要求

### 8.1 语言切换测试
- **自动检测**: 验证浏览器语言检测正常工作
- **手动切换**: 测试语言切换功能
- **持久性**: 确保语言偏好保存在cookie中
- **回退**: 验证缺失翻译时英语回退正常

### 8.2 翻译质量测试
- **覆盖检查**: 确保所有UI元素都已翻译
- **一致性检查**: 验证跨页面术语一致性
- **布局测试**: 确保翻译文本适合UI布局
- **功能测试**: 验证所有功能在两种语言下正常工作

## 9. 维护程序

### 9.1 更新流程
1. **添加新功能**: 在两种语言文件中创建资源键
2. **更新现有**: 同时修改两种语言文件
3. **移除功能**: 从两种语言文件中移除未使用的键
4. **测试更改**: 验证语言切换和翻译准确性

### 9.2 质量保证
- **代码审查**: 检查资源文件完整性
- **翻译审查**: 验证翻译准确性和一致性
- **用户测试**: 在实际使用场景中测试语言切换

## 10. 文件一致性管理

### 10.1 工作流集成
- **/i18n-management**: 资源文件管理的主要工作流
- **/development-cycle**: 功能开发期间更新资源内容
- **参考文档**: 本文件作为i18n规范标准

### 10.2 版本控制
- **资源文件**: 在版本控制中跟踪所有.resx文件
- **配置文件**: 跟踪本地化配置更改
- **文档**: 标准更改时更新本文档