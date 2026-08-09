/// <reference types="vite/client" />

// Declaring each VITE_* variable gives `import.meta.env.VITE_API_URL` a real
// named property instead of falling through vite/client's string index
// signature. Two things follow from that:
//
//   - noPropertyAccessFromIndexSignature accepts the dot access, so these do
//     not have to be written as import.meta.env["VITE_API_URL"].
//   - Vite keeps statically replacing each one with its literal value at build
//     time. Bracket access defeats that: Vite emits the whole env object and
//     looks the key up at runtime, which is larger and blocks dead-code
//     elimination on env-guarded branches.
//
// All of them are optional: CI supplies them as GitHub Actions vars, and local
// development runs without them (see .env.local.example).
interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_ADS_ENABLED?: string;
  readonly VITE_AMAZON_ASSOCIATE_TAG?: string;
  readonly VITE_BOOKING_AFFILIATE_ID?: string;
  readonly VITE_ADSENSE_PUBLISHER_ID?: string;
  readonly VITE_ADSENSE_AD_UNIT_ID?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
