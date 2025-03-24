<script setup lang="ts">
import WeatherForecast from '@/components/WeatherForecast.vue'
</script>

<template>
  <div class="about">
    <h1>This is an about page</h1>
    <WeatherForecast></WeatherForecast>
  </div>
</template>

<style>
@media (min-width: 1024px) {
  .about {
    min-height: 100vh;
    display: flex;
    align-items: center;
  }
}
</style>
