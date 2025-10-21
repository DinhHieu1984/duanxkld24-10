# Localization & Internationalization Patterns trong OrchardCore

## 🎯 **MỤC TIÊU**
Tìm hiểu các patterns về Localization và Internationalization để **viết modules OrchardCore đa ngôn ngữ**.

---

## 🌍 **LOCALIZATION ARCHITECTURE TRONG ORCHARDCORE**

### **1. Hai Loại Localization Chính**

#### **A. UI Localization (Interface Translation)**
- **Module**: `OrchardCore.Localization`
- **Mục đích**: Dịch giao diện, messages, labels
- **Công nghệ**: **PO Files** (Portable Object)
- **Scope**: Toàn bộ application UI

#### **B. Content Localization (Content Translation)**
- **Module**: `OrchardCore.ContentLocalization`
- **Mục đích**: Dịch nội dung (articles, pages, etc.)
- **Công nghệ**: **LocalizationPart** + **Culture management**
- **Scope**: Content items cụ thể

---

## 🔧 **CORE LOCALIZATION PATTERNS**

### **1. PO Files Structure**
```po
# Simple translation
msgid "Unknown system error"
msgstr "Error desconegut del sistema"

# With context
msgctxt "OrchardCore.FileSystems.Media.FileSystemStorageProvider"
msgid "File {0} does not exist"
msgstr "Soubor {0} neexistuje"
```

### **2. File Location Hierarchy**
```
1. [Extension]/Localization/[culture].po          ← Module-specific
2. /Localization/[culture].po                     ← Global
3. /App_Data/Sites/[Tenant]/Localization/[culture].po ← Tenant-specific
4. /Localization/[Extension]/[culture].po         ← NuGet package style
5. /Localization/[culture]/[Extension].po         ← Culture folder style
```

### **3. LocalizationService Pattern**
```csharp
public class LocalizationService : ILocalizationService
{
    private LocalizationSettings _localizationSettings;
    private static readonly string _defaultCulture = CultureInfo.InstalledUICulture.Name;
    
    public async Task<string> GetDefaultCultureAsync()
    {
        await InitializeLocalizationSettingsAsync();
        return _localizationSettings.DefaultCulture ?? _defaultCulture;
    }
}
```

### **4. Content Localization Pattern**
```csharp
public class LocalizationPart : ContentPart, ILocalizable
{
    public string LocalizationSet { get; set; }  // Groups related translations
    public string Culture { get; set; }          // Specific culture code
}
```

---

## 🎯 **BEST PRACTICES**

### **✅ ĐÚNG:**
- PO files cho UI translation
- LocalizationPart cho content translation
- Fallback culture strategy
- Tenant-specific localization
- Lazy loading settings
- Culture validation

### **❌ SAI:**
- Hard-coded strings
- No fallback strategy
- Mixed UI and content localization
- Culture without validation

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*