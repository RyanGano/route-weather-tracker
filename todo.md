# API Key Todo

The following states require API keys to enable official road condition data.
Without these keys the app still works — NWS weather data is used as a fallback.

Mark a state as **Done** once you have stored the key in Azure Key Vault and
I will run the integration tests and remove the entry.

---

## Colorado: Done

**Why:** CDOT COtrip API provides official road conditions (open/restricted/closed),
traction laws, and chain requirements for all 14 Colorado passes.

**Where to get it:** https://manage-api.cotrip.org/login
(Free — sign in with Google, Twitter, or email; key delivered immediately.)

**What to save:** Store the key in Azure Key Vault as:

```
Secret name: CDOT-ApiKey
```

---

## Virginia: Todo

**Why:** VDOT 511 API provides official road conditions and cameras for
Virginia Appalachian passes (Afton Mountain, Fancy Gap, etc.).

**Where to get it:** https://developer.511Virginia.org
(Free registration required.)

**What to save:** Store the key in Azure Key Vault as:

```
Secret name: VDOT-ApiKey
```

---

## Tennessee (NC/TN): Todo

**Why:** TDOT 511 API provides official conditions for Tennessee mountain passes
(Clinch Mountain, Newfound Gap area from TN side).

**Where to get it:** https://developer.tn511.com/api/
(Free registration required; key delivered by email.)

**What to save:** Store the key in Azure Key Vault as:

```
Secret name: TDOT-ApiKey
```
