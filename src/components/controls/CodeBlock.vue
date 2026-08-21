<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  code: string
}>()

const copied = ref(false)
let timeoutId: ReturnType<typeof setTimeout> | null = null

const copyCode = async () => {
  try {
    await navigator.clipboard.writeText(props.code)
    copied.value = true
    if (timeoutId) clearTimeout(timeoutId)
    timeoutId = setTimeout(() => {
      copied.value = false
    }, 2000)
  } catch {
    copied.value = false
  }
}
</script>

<template>
  <div class="code-block-wrapper">
    <div class="code-block">
      <code>{{ code }}</code>
    </div>
    <button class="copy-btn" @click="copyCode">
      {{ copied ? '已复制' : '复制' }}
    </button>
  </div>
</template>

<style scoped>
.code-block-wrapper {
  position: relative;
  margin: 8px 0 16px 0;
}

.code-block {
  backdrop-filter: blur(10px);
  background-color: color-mix(in srgb, var(--background-color) 80%, transparent);
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  padding: 16px 80px 16px 20px;
  overflow-x: auto;
}

.code-block code {
  font-family: monospace;
  font-size: 15px;
  color: var(--text-color);
}

.copy-btn {
  position: absolute;
  top: 8px;
  right: 8px;
  padding: 8px 12px;
  font-size: 13px;
  border-radius: 4px;
  border: 1px solid var(--hover-color);
  background: transparent;
  color: var(--text-color);
  cursor: pointer;
  transition: all 0.16s ease-in-out;
  opacity: 0.6;
}

.copy-btn:hover {
  background-color: var(--hover-color);
  opacity: 1;
}

.copy-btn:active {
  background-color: color-mix(in srgb, var(--hover-color) 80%, transparent);
}
</style>