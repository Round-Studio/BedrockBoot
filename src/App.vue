<script setup lang="ts">
import TopBar from "./components/bar/TopBar.vue";
import FootBar from "./components/bar/FootBar.vue";
</script>

<template>
  <div class="page-container">
    <top-bar />
    <main class="main-content">
      <router-view />
    </main>
    <foot-bar />
  </div>
</template>

<style>
html,
body {
  margin: 0;
  padding: 0;
  height: 100%;
}

.page-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  width: 100%;
}

.main-content {
  flex: 1 0 auto;
  width: 100%;
  padding-top: 128px;
  padding-bottom: 96px;
  box-sizing: border-box;
}
</style>