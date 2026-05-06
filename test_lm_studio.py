#!/usr/bin/env python3
"""Test LM Studio API endpoint before deploying mod"""

import requests
import json
import sys

def test_lm_studio(api_url="http://localhost:1234/v1"):
    print(f"Testing LM Studio API at: {api_url}")
    print("=" * 50)
    print()

    try:
        # Test 1: Check if API is reachable
        print("[1] Checking if LM Studio is reachable...")
        models_response = requests.get(f"{api_url}/models", timeout=10)

        if models_response.status_code != 200:
            print(f"❌ FAILED: Got HTTP {models_response.status_code}")
            print(f"   Make sure LM Studio is running at {api_url}")
            return False

        print("✅ LM Studio is reachable\n")

        # Test 2: Check available models
        print("[2] Checking available models...")
        models_data = models_response.json()
        models = models_data.get("data", [])

        if not models:
            print("❌ No models loaded")
            print(f"   Response: {models_data}")
            return False

        for model in models:
            print(f"   📦 {model['id']}")
        print()

        # Test 3: Send a test chat completion
        print("[3] Testing chat completion endpoint...")
        test_payload = {
            "model": "mistralai/ministral-3-3b",
            "messages": [
                {
                    "role": "user",
                    "content": "Greydwarf: A player just hit you. Respond with ONE sentence of brutal trash talk. Be extremely crude, profane, and mean. Make sexual/genital insults. Examples: 'Your dick is small like your skill', 'You play like you got a limp dick', 'Skill issue you suck ass', 'Fuck off dogwater'. Be as offensive as possible."
                }
            ],
            "temperature": 0.95,
            "max_tokens": 50,
            "top_p": 0.95
        }

        chat_response = requests.post(
            f"{api_url}/chat/completions",
            json=test_payload,
            timeout=15
        )

        if chat_response.status_code != 200:
            print(f"❌ FAILED: Got HTTP {chat_response.status_code}")
            print(f"   Error: {chat_response.text}")
            return False

        chat_data = chat_response.json()
        insult = chat_data.get("choices", [{}])[0].get("message", {}).get("content", "").strip()

        if not insult:
            print("❌ Got empty response from model")
            print(f"   Response: {chat_data}")
            return False

        print("✅ Chat completion works!\n")
        print("[TEST RESULT]")
        print("=" * 50)
        print(f"Generated insult: \"{insult}\"")
        print("=" * 50)
        print("\n✅ ALL TESTS PASSED - API is ready for the mod!")
        return True

    except requests.exceptions.ConnectionError:
        print(f"❌ Connection failed")
        print(f"   Make sure LM Studio is running at {api_url}")
        return False
    except requests.exceptions.Timeout:
        print(f"❌ Request timed out")
        print(f"   LM Studio may be slow or unresponsive")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return False

if __name__ == "__main__":
    api_url = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:1234/v1"
    success = test_lm_studio(api_url)
    sys.exit(0 if success else 1)
