<script setup lang="ts">
import { ref, onMounted } from 'vue'

const isDark = ref(false)

const toggleTheme = () => {
  isDark.value = !isDark.value
  updateTheme()
}

const updateTheme = () => {
  if (isDark.value) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

onMounted(() => {
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
  isDark.value = prefersDark
  updateTheme()
})
</script>

<template>
  <header class="topbar-wrapper">
    <div class="topbar">
      <div class="topbar-left">
        <router-link to="/" class="logo-btn" custom v-slot="{ navigate }">
          <button @click="navigate">
            <img src="./../../assets/BedrockBoot.Icon.256x.png" alt="logo" style="width: 36px"/>
            <p>BedrockBoot</p>
          </button>
        </router-link>

        <router-link to="/download" class="left-btn" custom v-slot="{ navigate }">
          <button @click="navigate">
            <svg t="1787244542283" class="icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="3915" width="18" height="18"><path d="M896 672c-17.066667 0-32 14.933333-32 32v128c0 6.4-4.266667 10.666667-10.666667 10.666667H170.666667c-6.4 0-10.666667-4.266667-10.666667-10.666667v-128c0-17.066667-14.933333-32-32-32s-32 14.933333-32 32v128c0 40.533333 34.133333 74.666667 74.666667 74.666667h682.666666c40.533333 0 74.666667-34.133333 74.666667-74.666667v-128c0-17.066667-14.933333-32-32-32z" p-id="3916"></path><path d="M488.533333 727.466667c6.4 6.4 14.933333 8.533333 23.466667 8.533333s17.066667-2.133333 23.466667-8.533333l213.333333-213.333334c12.8-12.8 12.8-32 0-44.8-12.8-12.8-32-12.8-44.8 0l-157.866667 157.866667V170.666667c0-17.066667-14.933333-32-32-32s-34.133333 14.933333-34.133333 32v456.533333L322.133333 469.333333c-12.8-12.8-32-12.8-44.8 0-12.8 12.8-12.8 32 0 44.8l211.2 213.333334z" p-id="3917"></path></svg>
            <p>下载</p>
          </button>
        </router-link>

        <a href="https://docs.roundstudio.top/docs/product/bb">
          <button class="left-btn">
            <svg t="1787244730791" class="icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="6971" width="18" height="18"><path d="M512 85.333333c235.648 0 426.666667 191.018667 426.666667 426.666667s-191.018667 426.666667-426.666667 426.666667S85.333333 747.648 85.333333 512 276.352 85.333333 512 85.333333z m0 89.6a337.066667 337.066667 0 1 0 0 674.133334 337.066667 337.066667 0 0 0 0-674.133334z" p-id="6972"></path><path d="M551.381333 627.328v-9.813333c0-16.896 3.541333-31.658667 10.538667-45.738667 6.357333-12.672 15.488-24.618667 28.16-35.157333 33.706667-29.525333 54.144-48.512 60.458667-55.552 16.896-22.528 26.026667-51.370667 26.026666-86.528 0-42.88-14.08-76.629333-42.197333-101.248C606.208 267.946667 568.96 256 523.264 256c-52.053333 0-92.842667 14.762667-123.093333 44.288-30.933333 29.568-45.696 70.357333-45.696 122.410667h80.170666c0-29.568 5.632-52.778667 17.578667-68.949334 13.354667-19.669333 35.157333-28.842667 66.133333-28.842666 23.893333 0 42.88 6.357333 56.234667 19.712 12.672 13.354667 19.712 31.658667 19.712 54.869333 0 17.578667-6.357333 34.432-18.986667 49.92l-8.448 9.813333c-45.738667 40.832-73.173333 70.4-82.304 89.344-9.813333 18.986667-14.08 42.24-14.08 68.949334v9.813333h80.896zM512 768c15.488 0 28.16-4.906667 38.698667-14.762667 10.538667-9.813333 16.170667-23.210667 16.170666-38.698666 0-15.445333-5.632-28.16-15.488-37.973334a55.893333 55.893333 0 0 0-39.381333-14.762666c-15.488 0-28.16 4.906667-38.698667 14.762666-10.538667 9.813333-15.445333 22.528-15.445333 37.973334 0 15.488 4.906667 28.16 15.445333 37.973333 10.581333 9.856 23.210667 15.488 38.698667 15.488z" p-id="6973"></path></svg>
            <p>文档</p>
          </button>
        </a>
      </div>
      <div class="topbar-right">
        <a href="https://github.com/Round-Studio/BedrockBoot">
          <button class="theme-toggle">
            <svg t="1787244314488" class="icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="2932" width="18" height="18"><path d="M512 42.666667A464.64 464.64 0 0 0 42.666667 502.186667 460.373333 460.373333 0 0 0 363.52 938.666667c23.466667 4.266667 32-9.813333 32-22.186667v-78.08c-130.56 27.733333-158.293333-61.44-158.293333-61.44a122.026667 122.026667 0 0 0-52.053334-67.413333c-42.666667-28.16 3.413333-27.733333 3.413334-27.733334a98.56 98.56 0 0 1 71.68 47.36 101.12 101.12 0 0 0 136.533333 37.973334 99.413333 99.413333 0 0 1 29.866667-61.44c-104.106667-11.52-213.333333-50.773333-213.333334-226.986667a177.066667 177.066667 0 0 1 47.36-124.16 161.28 161.28 0 0 1 4.693334-121.173333s39.68-12.373333 128 46.933333a455.68 455.68 0 0 1 234.666666 0c89.6-59.306667 128-46.933333 128-46.933333a161.28 161.28 0 0 1 4.693334 121.173333A177.066667 177.066667 0 0 1 810.666667 477.866667c0 176.64-110.08 215.466667-213.333334 226.986666a106.666667 106.666667 0 0 1 32 85.333334v125.866666c0 14.933333 8.533333 26.88 32 22.186667A460.8 460.8 0 0 0 981.333333 502.186667 464.64 464.64 0 0 0 512 42.666667" p-id="2933"></path></svg>
          </button>
        </a>

        <button @click="toggleTheme" class="theme-toggle">
          <svg t="1787243596398" class="icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="1666" width="16" height="16"><path d="M320 85.333333C243.626667 134.4 192 221.013333 192 320 192 418.986667 243.626667 505.6 321.28 554.666667 190.293333 554.666667 85.333333 449.706667 85.333333 320 85.333333 190.293333 190.293333 85.333333 320 85.333333M813.653333 149.333333 874.666667 210.346667 210.346667 874.666667 149.333333 813.653333 813.653333 149.333333M549.973333 253.013333 486.826667 213.333333 425.386667 256 443.306667 183.466667 384 138.24 458.666667 133.12 483.413333 62.72 512 132.266667 585.813333 133.546667 528.213333 181.76 549.973333 253.013333M409.173333 407.04 359.68 375.893333 311.893333 409.173333 326.4 352.853333 279.893333 317.44 337.92 313.6 357.12 258.56 378.88 312.746667 436.906667 314.026667 392.106667 351.146667 409.173333 407.04M810.666667 576C810.666667 705.706667 705.706667 810.666667 576 810.666667 523.946667 810.666667 475.733333 793.6 436.906667 765.013333L765.013333 436.906667 793.6 475.733333 810.666667 523.946667 810.666667 576M622.933333 856.746667 741.12 807.68 730.88 950.613333 622.933333 856.746667M807.68 741.546667 856.746667 623.36 950.613333 731.733333 807.68 741.546667M856.746667 529.92 808.106667 411.306667 950.613333 421.546667 856.746667 529.92M410.88 807.68 529.066667 856.746667 421.12 950.186667 410.88 807.68Z" p-id="1667"></path></svg>
        </button>
      </div>
    </div>
    <div class="topbar-warring">
      <p>网站正在建设中...</p>
    </div>
  </header>
</template>

<style scoped>
.topbar-wrapper {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
  width: 100%;
}

p {
  font-size: 16px;
  font-weight: 500;
  opacity: 1;
}

.topbar {
  max-height: 64px;
  height: 64px;
  backdrop-filter: blur(10px);
  background-color: color-mix(in srgb, var(--background-color) 80%, transparent);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 8px;
  box-sizing: border-box;
}

.topbar-warring{
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgb(197 0 0 / 0.8);
  color: white;
  font-size: 15px;
}

.topbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.topbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.logo-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
}

.logo-btn p {
  margin: 0;
  font-size: 18px;
  font-weight: 900;
  white-space: nowrap;
}

.theme-toggle {
  font-size: 20px;
  width: 36px;
  height: 36px;
  border-radius: 18px;
}

.theme-toggle svg{
  fill: var(--text-color);
}

.left-btn svg{
  fill: var(--text-color);
}

.left-btn {
  height: 36px;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
}

@media (max-width: 600px) {
  .topbar-left .left-btn p {
    display: none;
  }

  .logo-btn p {
    font-size: 14px;
  }

  .logo-btn {
    padding: 8px 8px;
  }

  .left-btn {
    padding: 8px 12px;
  }
}

@media (max-width: 400px) {
  .topbar-left {
    gap: 4px;
  }

  .topbar-right {
    gap: 4px;
  }

  .logo-btn img {
    width: 28px !important;
  }

  .logo-btn p {
    font-size: 12px;
  }

  .left-btn svg {
    width: 16px;
    height: 16px;
  }

  .theme-toggle {
    width: 32px;
    height: 32px;
  }

  .theme-toggle svg {
    width: 14px;
    height: 14px;
  }
}
</style>