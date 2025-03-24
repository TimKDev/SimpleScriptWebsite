## Debug
Start backend with `docker compose up` or using Rider. 

The frontend is not automatically reloaded when changes. Use `npm run dev` for running the frontend. For API calls to work CORS must be enabled in the api in dev mode.

## Release
Rebuild images such that frontend is updated: `docker compose build --no-cache`

Start full application with `docker compose up`. No CORS needed anymore.

