<script setup lang="ts">
import TopBar from "./components/bar/TopBar.vue";
import FootBar from "./components/bar/FootBar.vue";
</script>

<template>
  <top-bar />
  <main class="main-content">
    <router-view />
  </main>
  <foot-bar/>
</template>

<style>
.main-content {
  padding-top: 96px;
  padding-bottom: 96px;
}
</style>