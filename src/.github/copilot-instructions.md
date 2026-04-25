# Copilot Instructions

## Project Guidelines
- For this project, do not preserve backward compatibility; prefer removing legacy or compatibility-layer code when making changes.

### OpenRouter Catalog Cache
- Treat the OpenRouter catalog cache as a JSON file with a timestamp.
- Refresh the cache every 7 days.
- If the JSON cache is missing, check internet connectivity and create the cache