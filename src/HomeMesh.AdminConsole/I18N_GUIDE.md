# 前端国际化 (i18n) 实现指南

## 概述

本项目已实现完整的国际化(i18n)支持，采用 `vue-i18n` 库，支持根据浏览器默认语言自动选择语言，默认为英文。所有界面文本已从代码中抽取到资源文件中，统一采用 UTF-8 编码，避免中文乱码问题。

## 支持的语言

- **中文简体** (zh-CN)
- **英文** (en-US)

## 文件结构

```
src/
  ├── i18n.ts                          # i18n 初始化配置
  ├── locales/                         # 语言资源文件
  │   ├── en-US.json                  # 英文资源
  │   └── zh-CN.json                  # 中文资源
  ├── features/admin-console/
  │   ├── composables/
  │   │   ├── useAdminConsole.ts       # 业务逻辑 composable
  │   │   └── useI18n.ts              # i18n 辅助 composable
  │   ├── constants.ts                # 常量文件（已更新为使用 i18n 键）
  │   └── components/                 # 各个组件（已更新使用 $t() 或 useI18n()）
  └── main.ts                         # 主入口（已集成 i18n）
```

## 工作原理

### 1. 语言检测

系统启动时，`src/i18n.ts` 会自动检测浏览器的语言偏好：

```typescript
function getDefaultLocale(): string {
  const browserLocale = navigator.language;
  
  // 支持的语言列表
  const supportedLocales: Record<string, string> = {
    'zh': 'zh-CN',
    'zh-CN': 'zh-CN',
    'en': 'en-US',
    'en-US': 'en-US',
    // ...
  };

  // 先精确匹配，再前缀匹配，最后使用英文作为默认
  return supportedLocales[browserLocale] || 'en-US';
}
```

**优先级**：
1. 精确匹配（如 `zh-CN`）
2. 语言前缀匹配（如 `zh-*` 匹配到 `zh-CN`）
3. 默认为英文 (`en-US`)

### 2. 在组件中使用 i18n

#### 方法 1：使用 `$t()` 全局方法（推荐用于模板）

```vue
<template>
  <h1>{{ $t('dashboard.title') }}</h1>
  <p>{{ $t('dashboard.description') }}</p>
</template>
```

#### 方法 2：使用 `useI18n()` composable（推荐用于脚本）

```vue
<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const title = t('dashboard.title');
</script>
```

#### 方法 3：使用自定义 i18n composables（用于复杂场景）

项目提供了 `src/features/admin-console/composables/useI18n.ts`，包含：

- `useLocalizedNavItems()` - 获取本地化的导航项
- `useLocalizedAccessExpiryOptions()` - 获取本地化的访问过期选项
- `useLocalizedPlaceholderTitles()` - 获取本地化的占位符标题

```vue
<script setup lang="ts">
import { useLocalizedNavItems } from './composables/useI18n';

const { localizedNavItems } = useLocalizedNavItems();
</script>

<template>
  <div v-for="item in localizedNavItems" :key="item.key">
    {{ item.label }}
  </div>
</template>
```

## 资源文件结构

资源文件采用 JSON 格式，使用嵌套对象组织相关的键值对：

```json
{
  "nav": {
    "dashboard": "Dashboard",
    "network": "Home Network",
    "access": "Device Access",
    "providers": "Protocol Providers"
  },
  "dashboard": {
    "title": "Dashboard",
    "description": "Network, devices, gateways and sync status overview in one screen.",
    "metrics": {
      "networks": "Networks",
      "networks_meta": "Created networks"
    }
  },
  "buttons": {
    "refresh": "Refresh",
    "logout": "Logout"
  }
}
```

### 命名规范

- 使用小写字母和下划线分隔
- 按功能模块组织（如 `nav.*`, `dashboard.*`, `auth.*`）
- 描述性标签和元信息分开（如 `label` 和 `meta`）

## 已更新的组件列表

以下组件已更新为使用 i18n：

### 核心组件
- ✅ `AdminHero.vue` - 顶部导航栏
- ✅ `AdminAuthScreen.vue` - 认证页面
- ✅ `WorkspaceRail.vue` - 侧边栏导航
- ✅ `CreateNetworkModal.vue` - 创建网络对话框
- ✅ `DashboardSection.vue` - 控制台总览页面

### 常量
- ✅ `constants.ts` - 导航项和选项现在使用 i18n 键

## 添加新的国际化文本

### 步骤 1：在资源文件中添加键值

编辑 `src/locales/en-US.json` 和 `src/locales/zh-CN.json`：

```json
{
  "newFeature": {
    "title": "New Feature Title",
    "description": "Feature description here"
  }
}
```

### 步骤 2：在组件中使用

```vue
<template>
  <h1>{{ $t('newFeature.title') }}</h1>
  <p>{{ $t('newFeature.description') }}</p>
</template>
```

## 参数化翻译

对于包含动态值的文本，使用参数化：

### 资源文件
```json
{
  "message": "You have {count} messages"
}
```

### 使用
```vue
<template>
  <p>{{ $t('message', { count: 5 }) }}</p>
</template>
```

## 编码保证

✅ **UTF-8 编码**
- 所有资源文件使用 UTF-8 编码
- 支持包括中文在内的所有 Unicode 字符
- 避免了中文乱码问题

✅ **一致性**
- 所有界面文本统一从资源文件中获取
- 无硬编码的语言特定文本
- 易于维护和更新

## 语言切换（可选功能）

虽然系统目前根据浏览器语言自动选择，但可以添加手动语言切换功能：

```typescript
import { useI18n } from 'vue-i18n';

const { locale } = useI18n();

// 切换到中文
locale.value = 'zh-CN';

// 切换到英文
locale.value = 'en-US';
```

## 最佳实践

1. **始终使用 i18n 键** - 避免在代码中硬编码任何用户可见的文本
2. **组织相关的键** - 使用嵌套对象将相关的翻译分组
3. **保持简洁** - 使用简短而描述性的键名
4. **统一格式** - 遵循现有的命名规范
5. **提前翻译** - 在功能完成前就添加所有需要的翻译

## 故障排除

### 问题：显示 `[object Object]` 或键名本身

**原因**：未使用 `$t()` 或 `useI18n()` 来获取翻译文本

**解决方案**：确保使用正确的方式调用翻译函数

### 问题：某些文本未翻译

**原因**：忘记在资源文件中添加相应的键

**解决方案**：检查 `en-US.json` 和 `zh-CN.json` 中是否都有该键

### 问题：中文显示乱码

**原因**：文件编码不是 UTF-8

**解决方案**：确保所有 JSON 资源文件都使用 UTF-8 编码保存

## 相关文档

- [Vue I18n 官方文档](https://vue-i18n.intlify.dev/)
- [项目国际化最佳实践](https://www.w3.org/International/questions/qa-i18n)
