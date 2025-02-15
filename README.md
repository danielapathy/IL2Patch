![Banner Image](assets/sample.png)
# IL2Patch

IL2Patch is a specialized tool for modifying IL2CPP Unity Android applications through binary patching of `libil2cpp.so` libraries. It enables direct modification of game logic and behavior through hex pattern matching and replacement.

## Features

- Multi-architecture support (arm64-v8a, armeabi-v7a, x86, x86_64)
- Automated APK handling (unpack, patch, repack)
- Zipalign optimization
- APK signing
- Debug logging

## How It Works

![Terminal Preview](assets/Animation.gif)

## What is IL2CPP and Why Patch It?

IL2CPP (Intermediate Language to C++) is a Unity scripting backend that converts managed C# code into native machine code. This improves performance and security but also makes modifying the game’s logic more difficult.  

In IL2CPP-based Unity games, **most of the core gameplay logic, scripting, and mechanics are compiled into the `libil2cpp.so` binary**, rather than being stored as human-readable C# code. This means traditional methods of modifying `.NET` assemblies won’t work, and any changes must be made directly at the binary level.  

Patching IL2CPP-based applications involves:  

1. Extracting the APK and locating `libil2cpp.so`  
2. Identifying and modifying relevant hex patterns in the binary  
3. Repacking the APK while maintaining correct compression settings (e.g., `resources.arsc`)  
4. **Aligning the package** to ensure files like `resources.arsc` and native libraries are at proper byte boundaries  
5. Signing the APK to ensure it can be installed  

Because modifying an APK disrupts its structure, alignment must be restored before signing to prevent installation failures or performance issues. This process is tedious and requires multiple test runs when developing patches or cheats. **IL2Patch automates this entire workflow, making IL2CPP modifications faster.**


## Setup & Usage

1. Place target APK in IL2Patch directory
2. Create `patch.xml` with your modifications
3. Run IL2Patch
4. Collect modified APK (`signed.apk` or `aligned.apk`)

### Keystore

A debug keystore is included in the `keystore` folder:
- `debug.keystore`
- `password.txt` (contains: android)

For production use, create your own keystore:
```bash
keytool -genkey -v -keystore android.keystore -alias android -keyalg RSA -keysize 2048 -validity 10000
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

### Supported Hex Format Variations for `Find` and `Replace`

When defining hex patterns, the `Find` and `Replace` fields can support a variety of formats for the byte sequences. The tool will automatically handle common variations, including:

1. **Standard continuous hex string**:  
   - Example: `600080520200001440008052`
   - The pattern is a single string of hex characters with no delimiters.

2. **Space-separated hex values**:  
   - Example: `00 d8 21 5e 00 20 28 1e 60 00 80 52`
   - The hex values are separated by spaces.

3. **Hex values with `0x` prefix**:  
   - Example: `0x00 0xd8 0x21 0x5e 0x00 0x20 0x28 0x1e 0x60 0x00 0x80 0x52`
   - Each byte is prefixed with `0x`, often seen in many disassembly tools.

4. **Comma-separated hex values with `0x` prefix**:  
   - Example: `0x00, 0xd8, 0x21, 0x5e, 0x00, 0x20, 0x28, 0x1e, 0x60, 0x00, 0x80, 0x52`
   - Hex bytes separated by commas, commonly used in code snippets.

## Build Tools Setup

First run will prompt for:
- Android SDK location
- Build Tools version selection
- Zipalign/signing preferences

Configuration saves to `config.xml`

## Disclaimer

This tool is for educational and research purposes only. Users are responsible for compliance with applicable terms of service and local regulations. 