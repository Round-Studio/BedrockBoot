<script setup lang="ts">

</script>

<template>
  <h1>What happen?</h1>
  <p>你来到了一个不存在的页面（滚木）</p>

  <router-link to="/" custom v-slot="{ navigate }">
    <button @click="navigate" style="margin-top: 18px;">
      <p>返回首页</p>
    </button>
  </router-link>
</template>

<style scoped>

</style>