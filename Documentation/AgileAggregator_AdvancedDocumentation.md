
# 📘 AgileAggregator (Advanced Version) – Class Documentation

## ✅ Overview

This advanced version of AgileAggregator introduces a flexible and scalable architecture based on a `GenericApiService` capable of dynamically consuming third-party APIs using configuration and optional post-processing logic. It supports:

- Runtime configuration from a Microsoft Access database.
- Dynamic JSON parsing using path templates.
- Optional per-service `PostProcessor` logic via a plugin-style registry.
- Built-in support for API Key and Client Credentials authentication.
- Decoupled services and extensibility for future integrations.

## 📂 Key Classes

### 🔹 `GenericApiService`
**Namespace**: `AgileActors.Services`  
**Purpose**: Fetches, authenticates, and parses responses from third-party APIs using dynamic configuration.

- Reads API query template and paths (e.g., `TitlePath`, `ItemPath`) from `ApiServiceDefinition`.
- Applies authentication using headers (API key or OAuth2).
- Supports JSON traversal and extraction via dot/bracket path syntax.
- Invokes optional `PostProcess` method if defined for result enhancement.

### 🔹 `ApiServiceDefinition`
**Namespace**: `AgileActors.Models`  
**Purpose**: Holds all configuration data for an API.

Includes:
- `BaseUrl`, `QueryTemplate`, authentication fields
- JSON path settings: `ItemPath`, `TitlePath`, `ImagePath`, etc.
- `PostProcessor`: string name of a class implementing `IApiPostProcessor`
- `PostProcess`: resolved delegate to be called by `GenericApiService`

### 🔹 `IApiPostProcessor`
**Namespace**: `AgileActors.Services`  
**Purpose**: Interface for optional per-service processors.
```csharp
AggregatedResult Process(AggregatedResult result, JsonElement item, string keyword);
```

### 🔹 `ApiPostProcessorRegistry`
**Namespace**: `AgileActors.Services`  
**Purpose**: Central registry for post-processors.

- Maps processor names (e.g., `"WeatherPostProcessor"`) to concrete classes.
- Case-insensitive lookup.
- Keeps post-processors discoverable and centralized.

### 🔹 `WeatherPostProcessor`
**Namespace**: `AgileActors.PostProcessors`  
**Purpose**: Adds special formatting logic for OpenWeatherMap results.

- Converts temperature to localized format (e.g., `17,4°C`)
- Appends weather description and image
- Constructs external URL for city search

### 🔹 `ApiServiceConfigLoader`
**Namespace**: `AgileActors.Services`  
**Purpose**: Loads configuration for all services from an Access database.

- Wires up appropriate postprocessor using `ApiPostProcessorRegistry`
- Supports fallback and null safety

## 🧪 Unit Testing

**File**: `GenericApiServiceTests.cs`

Covers:

- ✅ Valid API response parsing
- ✅ Fallback behavior on HTTP errors
- ✅ Post-processor invocation
- ✅ JSON parsing using path selectors

> Uses `Moq` to mock `HttpClient` and inject fake JSON responses.

## 🧩 Database Fields (MS Access)

| Column            | Description                                      |
|-------------------|--------------------------------------------------|
| `ServiceName`     | Logical name of the API                          |
| `BaseUrl`         | Root URL for the API                             |
| `QueryTemplate`   | Template with placeholders (`{keyword}`)         |
| `AuthType`        | Either `ApiKey` or `ClientCredentials`           |
| `ItemPath`        | JSON path to array/object of results             |
| `TitlePath`       | JSON path to title field                         |
| `DescriptionPath` | JSON path to description                         |
| `ImagePath`       | JSON path to image/icon                          |
| `DatePath`        | JSON path to date field                          |
| `PostProcessor`   | Class name for custom logic (optional)           |
