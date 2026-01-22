# Internationalization Standards

**🌐 Language**: [English](i18n-standards.md) | [中文](i18n-standards-cn.md)

## 1. Overview

This document defines the internationalization (i18n) standards for ClashSubManager project, ensuring consistent multi-language support across all admin interfaces.

## 2. Supported Languages

### 2.1 Language List
- **English (en-US)**: Default language, fallback for all missing translations
- **Chinese Simplified (zh-CN)**: Secondary language, complete UI translation support

### 2.2 Language Priority
1. **Primary**: English (en-US) - Default and fallback
2. **Secondary**: Chinese Simplified (zh-CN) - Complete translation coverage

## 3. Implementation Standards

### 3.1 Technology Stack
- **Framework**: ASP.NET Core Localization
- **Resource Format**: .resx files
- **Detection Method**: Browser Accept-Language header + Cookie preference
- **Switching Method**: JavaScript + Cookie-based preference storage

### 3.2 Language Detection Mechanism
```csharp
// Automatic browser language detection
var language = Request.GetBrowserLanguage();
if (language.StartsWith("zh")) {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
} else {
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
}
```

### 3.3 Language Switching Interface
- **Location**: Admin header navigation bar
- **Method**: Dropdown toggle button
- **Storage**: Cookie-based language preference
- **Behavior**: Immediate UI refresh without page reload

## 4. Resource File Structure

### 4.1 Directory Organization
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

### 4.2 Resource File Naming Convention
- **Format**: `[FileName].[LanguageCode].resx`
- **Language Codes**: `en` for English, `zh-CN` for Chinese Simplified
- **Location**: Same directory structure as source files

## 5. Translation Coverage Requirements

### 5.1 Full Coverage Areas
- **All admin interface pages**: Login, Dashboard, IP Management, Template Management, User Config
- **Navigation elements**: Menu items, breadcrumbs, page titles
- **Form labels and placeholders**: All input fields, buttons, validation messages
- **Error messages**: System errors, validation errors, operation feedback
- **Help text**: Tooltips, descriptions, instructions

### 5.2 Partial Coverage Areas
- **System messages**: Log entries, debug information (English only)
- **Code comments**: Developer documentation (English only)
- **API responses**: Technical error codes (English only)

### 5.3 No Coverage Areas
- **Console output**: Application logs, system messages
- **Configuration keys**: Environment variable names, setting keys
- **Database schemas**: Table names, column names

## 6. Translation Quality Standards

### 6.1 Translation Principles
- **Accuracy**: Maintain exact meaning of original English text
- **Consistency**: Use consistent terminology across all pages
- **Clarity**: Use clear, simple language appropriate for technical users
- **Cultural Adaptation**: Consider cultural context for Chinese translations

### 6.2 Terminology Standards
| English Term | Chinese Translation | Context |
|--------------|-------------------|---------|
| Admin | 管理员 | User role |
| Configuration | 配置 | System settings |
| Subscription | 订阅 | Clash subscription |
| Template | 模板 | Clash template |
| Optimized IP | 优选IP | Optimized IP addresses |
| User ID | 用户ID | User identifier |
| Login | 登录 | Authentication action |
| Logout | 登出 | Authentication action |
| Save | 保存 | File operation |
| Delete | 删除 | File operation |
| Edit | 编辑 | Modification action |

## 7. Implementation Guidelines

### 7.1 Resource File Management
- **Synchronization**: Always update both language files simultaneously
- **Validation**: Ensure all resource keys exist in both language files
- **Testing**: Verify language switching works correctly after each update

### 7.2 Code Integration
```csharp
// Using IStringLocalizer in Razor Pages
@inject IStringLocalizer<Pages.Admin.Index> Localizer

<h1>@Localizer["PageTitle"]</h1>
<button>@Localizer["SaveButton"]</button>
```

```csharp
// Using IStringLocalizer in Services
public class SubscriptionService 
{
    private readonly IStringLocalizer<SubscriptionService> _localizer;
    
    public async Task<IActionResult> GetSubscription(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(_localizer["UserIdRequired"]);
        }
        // Implementation
    }
}
```

### 7.3 JavaScript Language Switching
```javascript
function switchLanguage(lang) {
    // Save language preference to Cookie
    document.cookie = `language=${lang}; path=/; max-age=31536000`;
    // Reload page to apply language change
    location.reload();
}

function getCurrentLanguage() {
    return document.cookie.replace(/(?:(?:^|.*;\s*)language\s*\=\s*([^;]*).*$)|^.*$/, "$1") || 'en';
}
```

## 8. Testing Requirements

### 8.1 Language Switching Tests
- **Automatic detection**: Verify browser language detection works
- **Manual switching**: Test language toggle functionality
- **Persistence**: Ensure language preference is saved in cookies
- **Fallback**: Verify English fallback for missing translations

### 8.2 Translation Quality Tests
- **Coverage check**: Ensure all UI elements are translated
- **Consistency check**: Verify terminology consistency across pages
- **Layout test**: Ensure translated text fits UI layout properly
- **Functionality test**: Verify all features work in both languages

## 9. Maintenance Procedures

### 9.1 Update Process
1. **Add new feature**: Create resource keys in both language files
2. **Update existing**: Modify both language files simultaneously
3. **Remove feature**: Remove unused keys from both language files
4. **Test changes**: Verify language switching and translation accuracy

### 9.2 Quality Assurance
- **Code review**: Check resource file completeness
- **Translation review**: Verify translation accuracy and consistency
- **User testing**: Test language switching in actual usage scenarios

## 10. File Consistency Management

### 10.1 Workflow Integration
- **/i18n-management**: Primary workflow for resource file management
- **/development-cycle**: Updates resource content during feature development
- **Reference Document**: This file serves as the i18n specification standard

### 10.2 Version Control
- **Resource files**: Track all .resx files in version control
- **Configuration files**: Track localization configuration changes
- **Documentation**: Update this document when standards change

---

## Language Versions
- [English](./i18n-standards.md) | [中文](./i18n-standards-cn.md)

**Document Version**: v1.0  
**Creation Time**: 2026-01-22  
**Responsible Person**: AI Agent Software Engineer  
**Reviewer**: Project Manager
