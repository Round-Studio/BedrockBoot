<script setup lang="ts">
import GithubRelease from "../controls/GithubRelease.vue";
import CodeBlock from "../controls/CodeBlock.vue";
</script>

<template>
  <div>
    <div class="header">
      <h1>获取 BedrockBoot</h1>
    </div>
    <p>部分网络环境下载速度较慢，可尝试使用 <a href="https://github.akams.cn/"><button class="border-btn">Github 加速站点</button></a> 对文件进行加速。</p>

    <div class="header">
      <h1>正式版</h1>
      <p>更新速度慢，稳定。适合日常使用</p>
    </div>
    <h2>官方源</h2>
    <GithubRelease></GithubRelease>

    <h2>AUR 源 (Linux)</h2>
    <CodeBlock code="paru -S bedrockboot" />

    <div class="header">
      <h1>预览版</h1>
      <p>更新速度快，部分功能不稳定。适合尝鲜</p>
    </div>
    <h2>官方源</h2>
    <GithubRelease :prerelease="true"></GithubRelease>
  </div>
</template>

<style scoped>
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}

@media (max-width: 600px) {
  .header {
    flex-direction: column;
    align-items: flex-start;
  }
}

.header h1 {
  margin: 0;
}

.header p {
  margin: 0;
  opacity: 0.7;
  font-weight: 400;
  font-size: 16px;
}

h2 {
  margin-top: 32px;
  margin-bottom: 12px;
  font-size: 20px;
  font-weight: 600;
}
</style>