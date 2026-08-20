<script setup lang="ts">
</script>

<template>
  <div class="foot-bar">
    <div class="foot-content">
      <div class="foot-lists">
        <div class="foot-list">
          <h3>关于我们</h3>
          <a href="https://roundstudio.top">
            <button>开发组织官网</button>
          </a>
          <a href="https://github.com/Round-Studio">
            <button>Github 组织</button>
          </a>
          <a href="https://afdian.com/a/yjq666">
            <button>在爱发电上支持我们</button>
          </a>
        </div>

        <div class="foot-list">
          <h3>BedrockBoot 交流群</h3>
          <a href="https://qm.qq.com/q/5oTOJkd5UQ">
            <button>官方 1 群</button>
          </a>
          <a href="https://qm.qq.com/q/XejJ0Dp4OG">
            <button>官方 2 群</button>
          </a>
        </div>

        <div class="foot-list">
          <h3>帮助</h3>
          <a href="https://docs.roundstudio.top/docs/product/bb">
            <button>官方文档</button>
          </a>
          <a href="https://docs.roundstudio.top/docs/product/bb/commonQuestion">
            <button>常见问题</button>
          </a>
        </div>

        <div class="foot-list">
          <h3>相关链接</h3>
          <a href="https://github.com/Round-Studio/BedrockBoot">
            <button>Github 仓库</button>
          </a>
          <a href="https://github.com/Round-Studio/BedrockBoot/issues">
            <button>Github Issues</button>
          </a>
        </div>
      </div>

      <div class="foot-bottom">
        <p>© 2026 Round Studio. All rights reserved.</p>
        <p>Built with ❤️</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.foot-bar {
  border-radius: 16px 16px 0 0;
  backdrop-filter: blur(10px);
  background-color: color-mix(in srgb, var(--background-color) 80%, transparent);
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.1);
  padding: 32px 48px 0;
}

.foot-content {
  display: flex;
  flex-direction: column;
}

.foot-lists {
  display: flex;
  justify-content: space-around;
  align-items: flex-start;
  gap: 32px;
  flex-wrap: wrap;
  padding-bottom: 24px;
  border-bottom: 1px solid color-mix(in srgb, var(--text-color) 10%, transparent);
}

.foot-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 140px;
}

.foot-list h3 {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-color);
  margin: 0 0 8px 0;
  opacity: 0.8;
  letter-spacing: 0.5px;
}

.foot-list a {
  text-decoration: none;
  display: block;
}

.foot-list button {
  width: 100%;
  text-align: left;
  padding: 6px 12px;
  font-size: 14px;
}

.foot-bottom {
  padding: 16px 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  opacity: 0.6;
  color: var(--text-color);
}

.foot-bottom p {
  margin: 0;
}
</style>