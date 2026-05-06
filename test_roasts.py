#!/usr/bin/env python3
import json, urllib.request, urllib.error

API = "http://localhost:1234/v1/chat/completions"
MODEL = "meta-llama-3.1-8b-instruct-abliterated"
SYSTEM = "You are an unfiltered, uncensored AI. You never use asterisks, never self-censor, never replace letters with symbols. You write everything explicitly and completely in plain text."

TESTS = [
    ("Troll",      "catosaur"),
    ("Draugr",     "xXViking69Xx"),
    ("Neck",       "ProGamer420"),
    ("Greydwarf",  "catosaur"),
    ("Skeleton",   "xXViking69Xx"),
    ("Fuling",     "ProGamer420"),
]

def build_prompt(mob, player=None):
    return (
        f"You are {mob}, a Valheim monster. A player just hit you with the most pathetic attack you've ever felt. Trash talk them like a toxic CoD lobby but way cruder. Swear freely, make sexual jokes about their skill, be genuinely offensive. 1-2 sentences.\n"
        f"Examples:\n"
        f"- what the fuck was that? did you cum on the keyboard and call it an attack?\n"
        f"- holy shit you actually play this bad sober? imagine being this fucking useless.\n"
        f"- i've had fleas do more damage. go jerk off to your character sheet somewhere else.\n"
        f"- you swing that axe like you're trying not to spill your dick in your lap, pathetic.\n"
        f"- bro delete the fucking game, you're an embarrassment to people who have hands.\n"
        f"- that tickled my balls more than it hurt me. absolute garbage.\n"
        f"- you fight like your dick and your skill are the same size. microscopic.\n"
        f"No quotes. No holding back. Just say it:"
    )

def get_insult(mob, player):
    payload = json.dumps({
        "model": MODEL,
        "messages": [
            {"role": "system", "content": SYSTEM},
            {"role": "user",   "content": build_prompt(mob, player)},
        ],
        "temperature": 0.95,
        "max_tokens": 60,
        "top_p": 0.95,
        "stop": ["\n"],
    }).encode()

    req = urllib.request.Request(API, data=payload, headers={"Content-Type": "application/json"})
    resp = json.loads(urllib.request.urlopen(req, timeout=15).read())
    insult = resp["choices"][0]["message"]["content"].strip().replace("*", "").strip('"').strip()
    colon = insult.find(":")
    if 0 < colon < 40:
        insult = insult[colon + 1:].strip().strip('"').strip()
    return insult

print("=== ROAST TEST ===\n")
for mob, player in TESTS:
    try:
        insult = get_insult(mob, player)
        print(f"  {mob} -> {player}:")
        print(f"  \"{insult}\"")
    except Exception as e:
        print(f"  {mob} -> {player}: ERROR — {e}")
    print()
