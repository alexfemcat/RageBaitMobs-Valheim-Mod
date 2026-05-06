# Test LM Studio API Before Deploying Mod

Before you start the Valheim server, validate that LM Studio is configured correctly with this test script.

## Prerequisites

**Python 3.7+**
```bash
pip install requests
```

OR

**.NET (for C# version)**
```bash
dotnet tool install -g dotnet-script
```

## Usage

### Python (Easiest)
```bash
# Test default endpoint (http://localhost:1234/v1)
python3 test_lm_studio.py

# Test custom endpoint
python3 test_lm_studio.py http://192.168.1.100:1234/v1
```

### C# (if you prefer)
```bash
# Test default endpoint
dotnet script test_lm_studio.cs

# Test custom endpoint
dotnet script test_lm_studio.cs http://192.168.1.100:1234/v1
```

## What It Tests

1. **API Reachability** - Checks if LM Studio is running
2. **Loaded Models** - Lists available models (should include `gemma-3`)
3. **Chat Completion** - Sends a test prompt and gets a response
4. **Response Format** - Validates JSON structure and content

## What Success Looks Like

```
Testing LM Studio API at: http://localhost:1234/v1
==================================================

[1] Checking if LM Studio is reachable...
✅ LM Studio is reachable

[2] Checking available models...
   📦 gemma-3

[3] Testing chat completion endpoint...
✅ Chat completion works!

[TEST RESULT]
==================================================
Generated insult: "You're absolutely dogwater at this game, get rekt noob"
==================================================

✅ ALL TESTS PASSED - API is ready for the mod!
```

## Common Issues

### Connection Refused
- **Problem:** LM Studio isn't running
- **Solution:** Start LM Studio server with `lms server start`

### No Models Loaded
- **Problem:** Gemma-3 1B isn't loaded
- **Solution:** Download and load Gemma-3 1B in LM Studio

### Timeout
- **Problem:** LM Studio is slow or hung
- **Solution:** Restart LM Studio

## Once Tests Pass

You're good to deploy the mod to PufferPanel! The API endpoint is working correctly.
