# Project Guidelines for AI Agents

This repository manages **FesterUO**, a containerized Ultima Online server stack running [ServUO](https://github.com/ServUO/ServUO) (Publish 57) with .NET 10, Mono runtime, modular engine patches, parameterized runtime configurations, and custom gameplay systems.

## Architecture & Conventions

### Container & Service Structure in `docker-compose.yaml`

When modifying or extending the service configuration, adhere strictly to the following standards:

1. **Naming & Identity**:
   - Explicitly specify `container_name: festeruo`.
   - Set `restart: unless-stopped`.
   - Multi-stage build context pointing to repository root (`context: .`, `dockerfile: Dockerfile`).
   - Interactive shell support enabled (`stdin_open: true`, `tty: true`).

2. **Networking & DNS**:
   - Parameterize port mappings with fallback defaults:
     ```yaml
     ports:
       - "${PORT_FESTERUO:-2593}:2593"
       - "${PORT_FESTERUO:-2593}:2593/udp"
     ```
   - Explicitly define DNS resolution using environment variables with public fallbacks:
     ```yaml
     dns:
       - "${DNS_PRIMARY:-1.1.1.1}"
       - "${DNS_SECONDARY:-1.0.0.1}"
     ```

3. **Volumes & Storage**:
   - **Relative Bind Mounts Only**: Mount repository-relative paths to server directories:
     ```yaml
     volumes:
       - "./Client:/server/Client:ro"
       - "./Saves:/server/Saves"
       - "./Logs:/server/Logs"
       - "./custom-scripts:/server/Scripts/Custom"
       - "./Config:/server/Config"
     ```
   - Runtime data directories (`Client/`, `Saves/`, `Logs/`) and upstream git clones (`upstream/`) must remain excluded in `.gitignore` and `.dockerignore`.

4. **Environment Variables**:
   - Format as an array of double-quoted strings (`- "KEY=${VAR}"`).
   - Maintain permissions and system variables:
     ```yaml
     environment:
       - "PGID=${PGID}"
       - "PUID=${PUID}"
       - "TZ=${TZ}"
       - "UMASK=${UMASK:-022}"
       - "DOTNET_CLI_HOME=/server/.dotnet"
       - "HOME=/server"
       - "DOTNET_SKIP_WORKLOAD_INTEGRITY_CHECK=true"
     ```

5. **Healthchecks**:
   - Healthcheck verifies Mono/dotnet process execution:
     ```yaml
     healthcheck:
       test: [ "CMD", "sh", "-c", "pidof mono || pidof dotnet || exit 1" ]
       interval: 60s
       retries: 5
       start_period: 60s
       timeout: 10s
     ```

### Engine Patches (`patches/` & `manage-patches.sh`)

- Patches modify the core ServUO engine during Docker image creation without directly checking upstream code into git.
- Number patches sequentially using two digits (e.g. `30-feature-name.patch`).
- Keep patches minimal and decoupled; favor exposing configuration keys or invoking hooks in `custom-scripts/` over baking game logic directly into engine core.
- Verify patches apply cleanly against upstream using `./manage-patches.sh check`.
- Document all new patches in [`patches/README.md`](patches/README.md).

### Custom Gameplay Scripts (`custom-scripts/`)

- Place all custom C# scripts in `custom-scripts/` organized by subsystem (e.g. `custom-scripts/FesterUO/`).
- Document community author attribution, provenance URLs, and installation notes in a dedicated `README.md` within each subsystem directory.
- Avoid hard-coded parameters; read settings from `Config/` via native `Config.Get(...)` calls.

### Runtime Configuration (`Config/`)

- Expose configurable gameplay values via `.cfg` files in `Config/ServUO/` or `Config/FesterUO/`.
- Document all settings in `Config/README.md` or subsystem configuration READMEs.

### Versioning & Changelog

- Document all gameplay systems, patches, configuration changes, and architectural updates in [CHANGELOG.md](CHANGELOG.md).
- Adhere strictly to [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Git Workflow & Commit Rules

- **NO Automated `git add` or `git commit`**: The agent MUST NEVER run `git add`, `git commit`, or `git push` commands, and must NOT prompt or ask the user to run them.
- **User-Managed Commits**: All git staging, committing, and pushing is handled exclusively by the user.
- **Commit Messages**: Do NOT generate or propose commit messages unless the user explicitly requests assistance with one.

### Instruction Synchronization

- Keep [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md) synchronized whenever guidelines or project conventions are updated.
