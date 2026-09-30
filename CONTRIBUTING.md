# Contributing Guidelines

Thank you for contributing to **FesterUO**! These guidelines explain how to add or modify custom gameplay scripts, engine patches, runtime configurations, and container settings.

---

## How to Contribute

### 1. Engine Patches (`patches/`)

* ServUO core modifications are stored as unified diff patches in `patches/` and applied during the Docker build.
* Always check that existing patches apply cleanly before generating new ones:
  ```bash
  ./manage-patches.sh check
  ```
* Name patches sequentially using two digits (e.g. `30-feature-name.patch`).
* Update [`patches/README.md`](patches/README.md) with the patch purpose, target files, and upstream mechanics modified.

### 2. Custom Gameplay Scripts (`custom-scripts/`)

* Place all new C# scripts in a subsystem directory under `custom-scripts/` (e.g. `custom-scripts/MySystem/`).
* **Attribution**: If integrating community scripts from servuo.dev or GitHub, include original author attribution, release dates, and thread links in a local `README.md`.
* **Configurability**: Avoid hard-coding rates, timers, or values directly in C#. Use `Config.Get(...)` to read from a corresponding configuration file in `Config/`.

### 3. Runtime Configurations (`Config/`)

* Keep shard progression, rates, caps, and options in `.cfg` files within `Config/`.
* Document new settings in `Config/README.md` or the corresponding subsystem README.

### 4. Docker Compose & Container Configuration

* Container specifications must follow the rules in [AGENTS.md](AGENTS.md).
* Keep volume mounts relative (`./Client`, `./Saves`, `./Logs`, `./custom-scripts`, `./Config`).
* Ensure runtime directories (`Client/`, `Saves/`, `Logs/`) and upstream clones (`upstream/`) are never committed to git.

---

## Validation & Testing

Before submitting changes, verify that your changes compile and start cleanly:

1. Validate Compose YAML syntax:
   ```bash
   docker compose config
   ```
2. Verify patches apply cleanly:
   ```bash
   ./manage-patches.sh check
   ```
3. Build and test the container:
   ```bash
   docker compose up -d --build
   ```
4. Check server logs to ensure 0 compilation errors or warnings:
   ```bash
   docker compose logs -f festeruo
   ```

---

## Documentation & Changelog

* Record all patches, script systems, configuration changes, and fixes in [CHANGELOG.md](CHANGELOG.md) adhering to [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
