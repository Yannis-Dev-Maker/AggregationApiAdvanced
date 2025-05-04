# 📦 AgileAggregator - Advanced Version

This is the **advanced version** of the AgileAggregator project. It dynamically loads services from a database (`apiservices.accdb`), supports custom authentication and parameterized queries, and extends functionality using **pluggable PostProcessors**.

---

## 🚀 Features

- ✅ **Generic API Service** with dynamic `QueryTemplate`
- 🔑 Supports `ApiKey` and `ClientCredentials` authentication
- 📁 Configuration via MS Access (`apiservices.accdb`)
- 🔄 **Dynamic JSON parsing** via `ItemPath`, `TitlePath`, etc.
- 🧩 Extensible PostProcessor mechanism (`IApiPostProcessor`)
- 🧪 Fully unit tested with `xUnit` + `Moq`

---

## 🧠 Architecture

![Architecture Diagram](documentaion/AgileAggregator_Advanced_Diagram.png)

---

## 🗂️ Project Structure

| File/Folder | Purpose |
|-------------|---------|
| `GenericApiService.cs` | Generic handler for all services using config and templates |
| `ApiServiceDefinition.cs` | POCO loaded from the Access database |
| `ApiServiceConfigLoader.cs` | Loads enabled services from Access (`PostProcessor` optional) |
| `IApiPostProcessor.cs` | Interface for custom data transformation |
| `ApiPostProcessorRegistry.cs` | Maps processor names (e.g., `"WeatherPostProcessor"`) to implementations |
| `PostProcessors/WeatherPostProcessor.cs` | Example: appends temperature, modifies title/description |
| `AggregationController.cs` | API endpoint for frontend queries |
| `apiservices.accdb` | Local database with service definitions |

---

## 🧩 Adding a New API Service

1. Add a new row in `apiservices.accdb`:
   - Set fields like `ServiceName`, `BaseUrl`, `QueryTemplate`, etc.
   - Optionally assign a `PostProcessor` name (e.g., `MyPostProcessor`)

2. Implement the post-processor (optional):
   ```csharp
   public class MyPostProcessor : IApiPostProcessor
   {
       public AggregatedResult Process(AggregatedResult result, JsonElement item, string keyword)
       {
           // transform fields
           return result;
       }
   }
   ```

3. Register it in `ApiPostProcessorRegistry.cs`:
   ```csharp
   { "MyPostProcessor", new MyPostProcessor() }
   ```

---

## 🧪 Running Unit Tests

This project includes unit tests for:
- ✅ `GenericApiService.cs` using mocked `HttpClient`
- ✅ `AggregationController.cs` with mocked services

To run:
```bash
dotnet test
```

---

## 🖥️ Running the API

```bash
dotnet run --project AgileActors
```

Then visit:
```
https://localhost:5001/api/aggregation?keyword=Athens&services=weather,news
```

---

## 📄 License

MIT © 2025 Yannis Thanassekos

---
