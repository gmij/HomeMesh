import { createI18n } from 'vue-i18n';
import enUS from './locales/en-US.json';
import zhCN from './locales/zh-CN.json';

// 获取浏览器的默认语言
function getDefaultLocale(): string {
  const browserLocale = navigator.language;
  
  // 支持的语言列表
  const supportedLocales: Record<string, string> = {
    'zh': 'zh-CN',
    'zh-CN': 'zh-CN',
    'zh-TW': 'zh-CN', // 繁體中文也使用簡體資源
    'en': 'en-US',
    'en-US': 'en-US',
    'en-GB': 'en-US'
  };

  // 先尝试精确匹配
  if (supportedLocales[browserLocale]) {
    return supportedLocales[browserLocale];
  }

  // 尝试语言前缀匹配
  const languagePrefix = browserLocale.split('-')[0];
  if (supportedLocales[languagePrefix]) {
    return supportedLocales[languagePrefix];
  }

  // 默认为英文
  return 'en-US';
}

const i18n = createI18n({
  legacy: false,
  locale: getDefaultLocale(),
  fallbackLocale: 'en-US',
  messages: {
    'en-US': enUS,
    'zh-CN': zhCN
  },
  globalInjection: true,
  missingWarn: false,
  fallbackWarn: false
});

export default i18n;
