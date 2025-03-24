<script setup lang="ts">
import { weatherService, type WeatherForecast } from '@/api'
import { ref, onMounted } from 'vue'

const forecasts = ref<WeatherForecast[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const fetchWeatherData = async () => {
  loading.value = true
  error.value = null

  try {
    forecasts.value = await weatherService.getForecasts()
  } catch (err) {
    error.value = 'Failed to load weather forecast data. Please try again later.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchWeatherData()
})
</script>

<template>
  <div class="weather-container">
    <h2>Weather Forecast</h2>

    <div v-if="loading" class="loading">Loading weather data...</div>

    <div v-else-if="error" class="error">
      {{ error }}
      <button @click="fetchWeatherData">Try Again</button>
    </div>

    <div v-else-if="forecasts.length === 0" class="no-data">
      No weather forecast data available.
    </div>

    <div v-else class="forecast-list">
      <div v-for="(forecast, index) in forecasts" :key="index" class="forecast-item">
        <div class="date">{{ new Date(forecast.date).toLocaleDateString() }}</div>
        <div class="temperature">
          <span class="temp-c">{{ forecast.temperatureC }}°C</span>
          <span class="temp-f">({{ forecast.temperatureF }}°F)</span>
        </div>
        <div class="summary">{{ forecast.summary }}</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.weather-container {
  background-color: #f5f5f5;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 2rem;
}

h2 {
  margin-top: 0;
  color: #2c3e50;
}

.loading,
.error,
.no-data {
  padding: 1rem;
  text-align: center;
}

.error {
  color: #d63031;
}

button {
  margin-top: 0.5rem;
  padding: 0.5rem 1rem;
  background-color: #3498db;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

button:hover {
  background-color: #2980b9;
}

.forecast-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.forecast-item {
  background-color: white;
  border-radius: 6px;
  padding: 1rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.date {
  font-weight: bold;
  margin-bottom: 0.5rem;
}

.temperature {
  margin-bottom: 0.5rem;
}

.temp-f {
  color: #7f8c8d;
  margin-left: 0.5rem;
  font-size: 0.9em;
}

.summary {
  color: #2c3e50;
}
</style>
