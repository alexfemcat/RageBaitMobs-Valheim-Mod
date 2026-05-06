# Phase 1 Setup Complete - Next Steps

## What's Been Created

✅ **Folder Structure**
- `src/` - Source code
- `bin/` - Build output
- `obj/` - Build artifacts
- `RagebateMobs.csproj` - Project configuration
- `Plugin.cs` - BepInEx plugin stub

## Before Building

The project is configured to reference BepInEx assemblies locally. You need to do ONE of these:

### Option A: Extract from PufferPanel Server (Recommended)

1. **Copy BepInEx core DLLs** from your Valheim server to a local `lib/` folder:
   ```bash
   mkdir -p lib/BepInEx/core
   # Copy from PufferPanel server or local Valheim installation:
   cp /path/to/server/BepInEx/core/*.dll lib/BepInEx/core/
   ```

2. **Required DLLs in lib/BepInEx/core/**:
   - `BepInEx.dll`
   - `BepInEx.Harmony.dll`
   - `0Harmony.dll`
   - (Optional: `UnityEngine.dll` and Valheim assemblies if you need them)

### Option B: Download BepInEx Unix Build Manually

1. Download BepInEx Unix build from [GitHub releases](https://github.com/BepInEx/BepInEx/releases)
2. Extract and copy the `core/` folder as shown above

## Build & Test

Once you have the BepInEx DLLs in place:

```bash
# Restore and build
dotnet restore
dotnet build -c Release

# Output will be in: bin/Release/net472/RagebateMobs.dll
```

## Next Steps (Phase 2)

- Create `src/Services/LLMService.cs` - LM Studio API integration
- Create `src/Services/PromptBuilder.cs` - Dynamic prompt construction
- Implement async HTTP calls to Gemma-3 1B model

