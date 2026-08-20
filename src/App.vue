<script setup lang="ts">
import TopBar from "./components/bar/TopBar.vue";
</script>

<template>
  <top-bar class="fixed-topbar"></top-bar>
  <main>
    <router-view />
  </main>
</template>

<style>
.fixed-topbar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
}
</style>