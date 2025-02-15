# IL2Patch

IL2Patch is a specialized tool for modifying IL2CPP Unity Android applications through binary patching of `libil2cpp.so` libraries. It enables direct modification of game logic and behavior through hex pattern matching and replacement.

## Features

- Multi-architecture support (arm64-v8a, armeabi-v7a, x86, x86_64)
- Automated APK handling (unpack, patch, repack)
- Zipalign optimization
- APK signing
- Debug logging

## How It Works

IL2Patch performs the following operations:
1. APK extraction
2. Architecture detection and `libil2cpp.so` modification
3. Binary patching using hex patterns
4. APK repackaging
5. Zipalign optimization
6. APK signing

## Setup & Usage

1. Place target APK in IL2Patch directory
2. Create `patch.xml` with your modifications
3. Run IL2Patch
4. Collect modified APK

### Keystore

A debug keystore is included in the `keystore` folder:
- `debug.keystore`
- `password.txt` (contains: yourpassword)

You can also create your own keystore:
```bash
keytool.exe -genkeypair -v -keystore "IL2Patch\keystore\debug.keystore" -keyalg RSA -keysize 2048 -validity 10000 -alias android -dname "CN=Android, OU=Android, O=Android, L=Android, ST=Android, C=US" -storepass yourpassword -keypass yourpassword
```

## Patch Examples

Patches must be enclosed in proper XML structure:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Patches>
    <!-- Patches go here -->
</Patches>
```

### Example
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Patches>
    <Patch arch="arm64-v8a">
        <Description>No Recoil</Description>
        <Find>1f2003d5202640b9</Find>
        <Replace>1f2003d51f2003d5</Replace>
    </Patch>
    <Patch arch="arm64-v8a">
        <Description>Rapid Fire</Description>
        <Find>00009f1a60008052</Find>
        <Replace>00009f1a20008052</Replace>
    </Patch>
    <Patch arch="arm64-v8a">
        <Description>Infinite Ammo</Description>
        <Find>1f2003d5400080d2</Find>
        <Replace>1f2003d5200080d2</Replace>
    </Patch>
</Patches>
```

## Build Tools Setup

First run will prompt for:
- Android SDK location
- Build Tools version selection
- Zipalign/signing preferences

Configuration saves to `config.xml`

## Disclaimer

This tool is for educational and research purposes only. Users are responsible for compliance with applicable terms of service and local regulations.