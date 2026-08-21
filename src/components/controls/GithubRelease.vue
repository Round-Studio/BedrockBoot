<script setup lang="ts">
import { ref, onMounted } from 'vue'

const props = defineProps<{
  prerelease?: boolean
}>()

interface Release {
  name: string
  tag_name: string
  published_at: string
  body: string
  html_url: string
  assets: {
    name: string
    browser_download_url: string
    size: number
  }[]
}

const release = ref<Release | null>(null)
const loading = ref(true)
const error = ref(false)

const formatDate = (dateStr: string) => {
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

const formatSize = (bytes: number) => {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

onMounted(async () => {
  try {
    const response = await fetch('https://api.github.com/repos/Round-Studio/BedrockBoot/releases')
    if (!response.ok) throw new Error()
    const data = await response.json()

    let filtered = props.prerelease
        ? data.filter((r: any) => r.prerelease === true)
        : data.filter((r: any) => r.prerelease === false)

    filtered.sort((a: any, b: any) =>
        new Date(b.published_at).getTime() - new Date(a.published_at).getTime()
    )

    release.value = filtered[0] || null
    loading.value = false
  } catch {
    error.value = true
    loading.value = false
  }
})
</script>

<template>
  <div class="release-container">
    <div v-if="loading" class="release-loading">
      <div class="loading-spinner"></div>
      <p>加载中...</p>
    </div>

    <div v-else-if="error" class="release-error">
      <p>获取版本信息失败</p>
      <button @click="onMounted">重试</button>
    </div>

    <div v-else-if="!release" class="release-empty">
      <p>暂无版本</p>
    </div>

    <div v-else class="release-item">
      <div class="release-header">
        <div class="release-title">
          <h2>{{ release.name || release.tag_name }}</h2>
          <span class="release-tag">{{ release.tag_name }}</span>
        </div>
        <span class="release-date">{{ formatDate(release.published_at) }}</span>
      </div>

      <div class="release-assets">
        <h3>下载</h3>
        <div class="asset-list">
          <a
              v-for="asset in release.assets"
              :key="asset.name"
              :href="asset.browser_download_url"
              class="asset-item"
              download
          >
            <span class="asset-name">{{ asset.name }}</span>
            <span class="asset-size">{{ formatSize(asset.size) }}</span>
          </a>
        </div>
      </div>

      <a :href="release.html_url" target="_blank" class="release-link">
        查看完整发布说明
      </a>
    </div>
  </div>
</template>

<style scoped>
.release-container {
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px 0;
}

.release-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  gap: 16px;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--hover-color);
  border-top-color: var(--text-color);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.release-loading p {
  margin: 0;
  font-size: 16px;
  font-weight: 400;
}

.release-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 60px 20px;
  gap: 16px;
}

.release-error p {
  margin: 0;
  font-size: 16px;
  font-weight: 400;
}

.release-error button {
  padding: 8px 24px;
  border: 1px solid var(--text-color);
  border-radius: 4px;
  font-size: 14px;
  color: var(--text-color);
  background: transparent;
}

.release-empty {
  text-align: center;
  padding: 60px 20px;
}

.release-empty p {
  margin: 0;
  font-size: 16px;
  font-weight: 400;
  opacity: 0.6;
}

.release-item {
  backdrop-filter: blur(10px);
  background-color: color-mix(in srgb, var(--background-color) 80%, transparent);
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  padding: 24px;
  transition: all 0.16s ease-in-out;
}

.release-item:hover {
  border-color: color-mix(in srgb, var(--hover-color) 80%, transparent);
}

.release-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 16px;
}

.release-title {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.release-title h2 {
  margin: 0;
  font-size: 22px;
  font-weight: 600;
}

.release-tag {
  font-size: 13px;
  padding: 2px 10px;
  border-radius: 12px;
  background-color: var(--hover-color);
  font-weight: 500;
  font-family: monospace;
}

.release-date {
  font-size: 14px;
  opacity: 0.6;
  white-space: nowrap;
}

.release-assets {
  margin: 16px 0 12px;
}

.release-assets h3 {
  font-size: 15px;
  font-weight: 600;
  margin: 0 0 10px 0;
  opacity: 0.8;
}

.asset-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.asset-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  border: 1px solid var(--hover-color);
  border-radius: 4px;
  font-size: 14px;
  color: var(--text-color);
  text-decoration: none;
  transition: all 0.16s ease-in-out;
}

.asset-item:hover {
  background-color: var(--hover-color);
  border-color: color-mix(in srgb, var(--hover-color) 80%, transparent);
}

.asset-name {
  font-weight: 500;
}

.asset-size {
  font-size: 12px;
  opacity: 0.6;
  font-family: monospace;
}

.release-link {
  display: inline-block;
  margin-top: 4px;
  font-size: 14px;
  color: var(--text-color);
  opacity: 0.7;
  text-decoration: none;
  transition: all 0.16s ease-in-out;
}

.release-link:hover {
  opacity: 1;
  text-decoration: underline;
}

@media (max-width: 768px) {
  .release-item {
    padding: 16px;
  }

  .release-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .release-title h2 {
    font-size: 18px;
  }

  .release-date {
    font-size: 13px;
  }

  .asset-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
  }
}
</style>