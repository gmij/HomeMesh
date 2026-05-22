import { useI18n } from 'vue-i18n';
import { computed } from 'vue';
import { prototypeNavItems, accessExpiryOptions, placeholderTitles } from '../constants';
import type { SectionNavItem } from '../types';

/**
 * 获取本地化的导航项（带实际翻译的标签）
 */
export function useLocalizedNavItems() {
  const { t } = useI18n();

  const localizedNavItems = computed<SectionNavItem[]>(() => {
    return prototypeNavItems.map((item) => ({
      ...item,
      label: t(item.label)
    }));
  });

  return { localizedNavItems };
}

/**
 * 获取本地化的访问过期选项
 */
export function useLocalizedAccessExpiryOptions() {
  const { t } = useI18n();

  const localizedOptions = computed(() => {
    return accessExpiryOptions.map((option) => ({
      ...option,
      label: t(option.label)
    }));
  });

  return { localizedOptions };
}

/**
 * 获取本地化的占位符标题
 */
export function useLocalizedPlaceholderTitles() {
  const { t } = useI18n();

  const localizedTitles = computed(() => {
    const result: Record<string, string> = {};
    for (const [key, i18nKey] of Object.entries(placeholderTitles)) {
      result[key] = t(i18nKey);
    }
    return result;
  });

  return { localizedTitles };
}
