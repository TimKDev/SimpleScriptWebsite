export interface WeatherForecast {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string
}

// Base API URL
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:10000'

// Helper function for API requests
async function apiRequest<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`

  const defaultOptions: RequestInit = {
    headers: {
      'Content-Type': 'application/json',
    },
  }

  const response = await fetch(url, { ...defaultOptions, ...options })

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status} ${response.statusText}`)
  }

  return (await response.json()) as T
}

export const weatherService = {
  async getForecasts(): Promise<WeatherForecast[]> {
    try {
      return await apiRequest<WeatherForecast[]>('/weatherforecast')
    } catch (error) {
      console.error('Error fetching weather forecasts:', error)
      throw error
    }
  },
}
