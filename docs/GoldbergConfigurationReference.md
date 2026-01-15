# Goldberg Emulator Configuration Reference

This document provides a comprehensive reference for implementing Goldberg Steam emulator configuration in GoldbergGUI.

## Configuration Files Structure

The Goldberg fork (gbe_fork) uses the following configuration structure:

```
steam_settings/
??? configs.main.ini              # Main emulator settings
??? configs.app.ini               # App-specific settings (DLC, branches, cloud saves)
??? configs.user.ini              # User account settings
??? configs.overlay.ini           # Overlay appearance and behavior
??? achievements.json             # Achievement definitions
??? stats.json                    # Statistics definitions
??? items.json                    # Inventory items (workshop items, cosmetics)
??? branches.json                 # Game branches (beta branches)
??? mods.json                     # Workshop/mod definitions
??? leaderboards.txt              # Leaderboard definitions
??? steam_appid.txt               # Game App ID
??? steam_interfaces.txt          # Steam API interfaces
??? DLC.txt                       # DLC list (legacy format)
??? depots.txt                    # Depot IDs
??? supported_languages.txt       # Game language list
??? subscribed_groups.txt         # Subscribed groups
??? subscribed_groups_clans.txt   # Subscribed clans
??? custom_broadcasts.txt         # Custom broadcast IPs
??? auto_accept_invite.txt        # Auto-accept invite list
??? installed_app_ids.txt         # Additional installed apps
??? account_avatar.jpg            # User avatar image
??? [subdirectories]
    ??? controller.EXAMPLE/       # Controller configurations
    ??? fonts.EXAMPLE/            # Custom fonts for overlay
    ??? http.EXAMPLE/             # Downloaded HTTP requests
    ??? mod_images.EXAMPLE/       # Workshop mod preview images
    ??? mods.EXAMPLE/             # Workshop mod files
    ??? sounds.EXAMPLE/           # Custom overlay sounds
```

---

## 1. Main Configuration (`configs.main.ini`)

### Authentication & Tickets
Controls how the emulator generates authentication tokens.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `new_app_ticket` | int | 0 | Generate new ticket format for `GetAuthSessionTicket()` and related APIs |
| `gc_token` | int | 0 | Generate GC (Game Coordinator) token along with new ticket |

### Playtime Tracking
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `record_playtime` | int | 0 | Track game playtime, updated every minute, persists across launches |

### Connectivity Settings
Controls network behavior and connectivity options.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `disable_lan_only` | int | 0 | Prevent hooking OS networking APIs, allow external requests (experimental Windows only) |
| `disable_networking` | int | 0 | Disable all steam networking interface functionality |
| `listen_port` | int | 47584 | UDP/TCP port the emulator listens on (must match across network) |
| `offline` | int | 0 | Pretend Steam is running in offline mode, affects `ISteamUser::BLoggedOn()` |
| `disable_sharing_stats_with_gameserver` | int | 0 | Prevent sharing stats/achievements with game servers, disables `ISteamGameServerStats` |
| `disable_source_query` | int | 0 | Don't send server details to server browser (game servers only) |
| `share_leaderboards_over_network` | int | 0 | Share leaderboard scores with players on same network (experimental) |
| `disable_lobby_creation` | int | 0 | Prevent lobby creation in steam matchmaking interface |
| `download_steamhttp_requests` | int | 0 | Download external HTTP(S) requests to `steam_settings/http/` (requires `disable_lan_only=1` and `disable_networking=0`) |
| `old_p2p_packet_sharing_mode` | int | 0 | Sharing mode for ISteamNetworking: 0=share unreliable only, 1=always share, 2=never share |

### Matchmaking Servers
**Note: These features are currently broken**

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `matchmaking_server_list_actual_type` | int | 0 | Return proper server type instead of always returning LAN servers (broken) |
| `matchmaking_server_details_via_source_query` | int | 0 | Retrieve actual server info via source query (broken) |

### Misc Workarounds
Game-specific workarounds and compatibility settings.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `achievements_bypass` | int | 0 | Force `ISteamUserStats::SetAchievement()` to always return true |
| `force_steamhttp_success` | int | 0 | Force `Steam_HTTP::SendHTTPRequest()` to always succeed |
| `disable_steamoverlaygameid_env_var` | int | 0 | Don't set `SteamOverlayGameId` env var (fixes Steam Input for non-steam games) |
| `enable_steam_preowned_ids` | int | 0 | Add many Steam apps to owned DLCs list (useful for Source-based games) |
| `steam_game_stats_reports_dir` | string | empty | Path to save `ISteamGameStats` reports (must be writable) |
| `free_weekend` | int | 0 | Pretend user is playing during free weekend |

---

## 2. App Configuration (`configs.app.ini`)

### General Settings
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `is_beta_branch` | int | 0 | Make the game think it's running on a beta branch |
| `branch_name` | string | public | Name of current branch (must exist in `branches.json`) |

### DLC Configuration
Controls which DLCs are reported as owned.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `unlock_all` | int | 1 | Report all DLCs as unlocked (0=only report mentioned DLCs) |

**DLC Format:**
```ini
[app::dlcs]
{AppID}={DLC Name}
```

**Note:** Some games check for "hidden" DLCs (set `unlock_all=1`), while others detect emulators by querying fake DLCs (set `unlock_all=0`).

### App Paths
Provides paths to where apps/DLCs are installed.

**Format:**
```ini
[app::paths]
{AppID}={absolute or relative path}
{AppID}=                    # Deliberately empty path
```

**Path Examples:**
- `556760=../DLCRoot0`
- `1234=./folder_where_steam_api_is`
- `3456=../folder_one_level_above`
- `5678=../../folder_two_levels_above`
- `1337=` (empty path for games that expect empty paths)

### Cloud Save Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `create_default_dir` | int | 1 | Create default directory `[Steam Install]/userdata/{Steam3AccountID}/{AppID}/` |
| `create_specific_dirs` | int | 1 | Create OS-specific directories on startup |

**Directory Identifiers (wrap with double colons `::`)**

General identifiers:
- `{::Steam3AccountID::}` - Current account ID in Steam3 format
- `{::64BitSteamID::}` - Current account ID in Steam64 format
- `{::gameinstall::}` - `[Steam Install]\SteamApps\common\[Game Folder]\`
- `{::EmuSteamInstall::}` - Emulator install path (checks environment variables)

Windows identifiers:
- `{::WinMyDocuments::}` - `%USERPROFILE%\My Documents\`
- `{::WinAppDataLocal::}` - `%USERPROFILE%\AppData\Local\`
- `{::WinAppDataLocalLow::}` - `%USERPROFILE%\AppData\LocalLow\`
- `{::WinAppDataRoaming::}` - `%USERPROFILE%\AppData\Roaming\`
- `{::WinSavedGames::}` - `%USERPROFILE%\Saved Games\`

Linux identifiers:
- `{::LinuxHome::}` - `~/`
- `{::LinuxXdgDataHome::}` - `$XDG_DATA_HOME/` or `$HOME/.local/share`
- `{::SteamCloudDocuments::}` - `~/.SteamCloud/[username]/[Game Folder]/`

**Example:**
```ini
[app::cloud_save::win]
dir1={::WinAppDataRoaming::}/publisher_name/game_name
dir2={::WinMyDocuments::}/publisher_name/game_name/{::Steam3AccountID::}

[app::cloud_save::linux]
dir1={::LinuxXdgDataHome::}/publisher_name/game_name
dir2={::LinuxHome::}/publisher_name/game_name/{::64BitSteamID::}
```

---

## 3. User Configuration (`configs.user.ini`)

### General Settings
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `account_name` | string | gse orca | User account name displayed in-game |
| `account_steamid` | int64 | random | Account ID in Steam64 format (76561197960287930 - 76561202255233023) |
| `ticket` | string | empty | Base64-encoded encrypted app ticket |
| `alt_steamid` | int64 | 0 | Alternative Steam ID for encrypted savegames |
| `alt_steamid_count` | int | 5 | Number of calls before swapping to `alt_steamid` |
| `language` | string | english | Language code (must exist in `supported_languages.txt`) |
| `ip_country` | string | US | ISO 3166-1-alpha-2 country code |

### Save Settings
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `local_save_path` | string | empty | Force portable save location (absolute or relative to DLL) |
| `saves_folder_name` | string | GSE Saves | Base folder name for saves (only used if `local_save_path` is empty) |

---

## 4. Overlay Configuration (`configs.overlay.ini`)

### Feature Toggles
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `disable_overlay` | int | 0 | Completely disable the overlay |
| `hook_delay_sec` | float | 0.0 | Delay before hooking renderer (seconds) |
| `disable_hook_d3d9` | int | 0 | Disable Direct3D 9 hook |
| `disable_hook_d3d10` | int | 0 | Disable Direct3D 10 hook |
| `disable_hook_d3d11` | int | 0 | Disable Direct3D 11 hook |
| `disable_hook_d3d12` | int | 0 | Disable Direct3D 12 hook |
| `disable_hook_opengl` | int | 0 | Disable OpenGL hook |
| `disable_hook_vulkan` | int | 0 | Disable Vulkan hook |
| `disable_all_renderers` | int | 0 | Disable all renderer hooks |

### Warning Toggles
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `disable_warning_any` | int | 0 | Disable all overlay warnings |
| `disable_warning_bad_appid` | int | 0 | Disable bad App ID warning |
| `disable_warning_local_save` | int | 0 | Disable local save path warning |

### Performance
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `upload_achievements_icons_to_gpu` | int | 1 | Upload achievement icons to GPU (disable if causing FPS drops) |
| `fps_averaging_window` | int | 10 | Frames to accumulate for FPS calculation (1=instantaneous, higher=stable) |

### Always Show Options
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `overlay_always_show_user_info` | int | 0 | Always display user information |
| `overlay_always_show_fps` | int | 0 | Always display FPS counter |
| `overlay_always_show_frametime` | int | 0 | Always display frametime |
| `overlay_always_show_playtime` | int | 0 | Always display playtime |

### Appearance Settings

**Font Settings:**
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Font_Override` | string | empty | Path to custom TrueType font (relative to `steam_settings/fonts/` or global fonts folder) |
| `Font_Size` | float | 16.0 | Global font size (multiples of 16 recommended for built-in font) |
| `Font_Glyph_Extra_Spacing_x` | float | 1.0 | Horizontal character spacing |
| `Font_Glyph_Extra_Spacing_y` | float | 0.0 | Vertical character spacing |

**Size Settings:**
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Icon_Size` | float | 64.0 | Achievement icon size in pixels |

**Notification Colors (RGBA 0.0-1.0):**
- `Notification_R/G/B/A` - Background color for all notifications
- `Background_R/G/B/A` - Main background (Shift+Tab menu)
- `Element_R/G/B/A` - UI element color
- `ElementHovered_R/G/B/A` - Hovered element color
- `ElementActive_R/G/B/A` - Active element color (-1.0 to use hovered color)

**Notification Positioning:**
| Setting | Type | Values | Description |
|---------|------|--------|-------------|
| `PosAchievement` | string | see below | Achievement notification position |
| `PosInvitation` | string | see below | Invitation notification position |
| `PosChatMsg` | string | see below | Chat message position |

**Valid Position Values:**
- `top_left`, `top_center`, `top_right`
- `bot_left`, `bot_center`, `bot_right`

**Notification Timing (seconds):**
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Notification_Animation` | float | 0.35 | Duration of notification animation (0=disable) |
| `Notification_Rounding` | float | 10.0 | Notification corner roundness |
| `Notification_Margin_x` | float | 5.0 | Horizontal margin |
| `Notification_Margin_y` | float | 5.0 | Vertical margin |
| `Notification_Duration_Progress` | float | 6.0 | Achievement progress duration |
| `Notification_Duration_Achievement` | float | 7.0 | Achievement unlock duration |
| `Notification_Duration_Invitation` | float | 8.0 | Friend invitation duration |
| `Notification_Duration_Chat` | float | 4.0 | Chat message duration |

**Stats Display (FPS Counter):**
- `Stats_Background_R/G/B/A` - Background color
- `Stats_Text_R/G/B/A` - Text color
- `Stats_Pos_x` - Horizontal position (0.0=left, 1.0=right)
- `Stats_Pos_y` - Vertical position (0.0=top, 1.0=bottom)

**Date/Time Format:**
| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Achievement_Unlock_Datetime_Format` | string | %Y/%m/%d - %H:%M:%S | strftime format (max 79 chars) |

---

## 5. Data Files

### achievements.json
Array of achievement objects.

**Structure:**
```json
[
  {
    "name": "internal_achievement_name",
    "displayName": "Achievement Title",
    "description": "Achievement description (optional)",
    "hidden": "0",
    "icon": "images/achievement_icon.jpg",
    "icongray": "images/achievement_icon_gray.jpg"
  }
]
```

**Fields:**
- `name` (required) - Internal identifier used by the game
- `displayName` (required) - Display name shown to user
- `description` (optional) - Description text (can be empty string)
- `hidden` (required) - "0" for visible, non-zero for hidden
- `icon` (required) - Path to colored icon (relative to steam_settings)
- `icongray` (required) - Path to grayscale icon

---

### stats.json
Array of statistic definitions.

**Structure:**
```json
[
  {
    "name": "stat_internal_name",
    "type": "int|float|avgrate",
    "default": "0",
    "global": "0"
  }
]
```

**Fields:**
- `name` (required) - Internal stat identifier
- `type` (required) - Data type: `int`, `float`, or `avgrate`
- `default` (required) - Default value as string
- `global` (required) - Global stat value as string

---

### items.json
Dictionary of inventory/workshop items.

**Structure:**
```json
{
  "item_id": {
    "Timestamp": "2018-01-09T19:30:03Z",
    "modified": "20180109T193003Z",
    "date_created": "20180109T193003Z",
    "type": "bundle|item",
    "display_type": "Bundle|Item",
    "name": "Item Name",
    "bundle": "item1_id x quantity;item2_id x quantity",
    "description": "Item description",
    "background_color": "000000",
    "icon_url": "http://example.com/icon.png",
    "icon_url_large": "http://example.com/icon_large.png",
    "name_color": "7a0000",
    "tradable": "true|false",
    "marketable": "true|false",
    "commodity": "true|false",
    "drop_interval": "0",
    "drop_max_per_window": "0",
    "workshopid": "0",
    "item_quality": "0"
  }
}
```

**Common Fields:**
- `name` (required) - Item display name
- `type` (required) - Item type (bundle, item, etc.)
- `description` (optional) - Item description
- `tradable` (optional) - Can be traded (true/false string)
- `marketable` (optional) - Can be sold on market (true/false string)
- `icon_url` (optional) - Icon URL or path
- `workshopid` (optional) - Workshop item ID

---

### branches.json
Array of game branch definitions.

**Structure:**
```json
[
  {
    "name": "branch_name",
    "description": "Branch description",
    "protected": false,
    "build_id": 12345,
    "time_updated": 1719527543
  }
]
```

**Fields:**
- `name` (required) - Branch identifier (e.g., "public", "beta")
- `description` (optional) - Human-readable description
- `protected` (required) - Whether branch requires password
- `build_id` (required) - Build number
- `time_updated` (required) - Unix timestamp of last update

---

### mods.json
Dictionary of workshop/mod items.

**Structure:**
```json
{
  "workshop_id": {
    "title": "Mod Title",
    "description": "Mod description",
    "primary_filename": "mod_file.dat",
    "preview_filename": "preview.png",
    "steam_id_owner": 76561197960287930,
    "time_created": 1554997000,
    "time_updated": 1554997000,
    "time_added": 1554997000,
    "tags": "tag1,tag2,tag3",
    "primary_filesize": 1000000,
    "preview_filesize": 1000000,
    "total_files_sizes": 1000000,
    "min_game_branch": "1.0.0",
    "max_game_branch": "2.0.0",
    "workshop_item_url": "https://steamcommunity.com/sharedfiles/filedetails/?id=123",
    "upvotes": 10,
    "downvotes": 1,
    "num_children": 0,
    "path": "path/to/mod/folder",
    "preview_url": "file:///path/to/preview.jpg",
    "score": 0.9,
    "metadata": "optional metadata string"
  }
}
```

**Required Fields (minimum):**
- `title` - Mod display name

**Optional Fields:**
- `description` - Mod description
- `primary_filename` - Main mod file (must exist in `steam_settings/mods/{workshop_id}/`)
- `preview_filename` - Preview image (must exist in `steam_settings/mod_images/{workshop_id}/`)
- `steam_id_owner` - Owner's Steam ID
- `time_created/updated/added` - Unix timestamps
- `tags` - Comma-separated tag list
- `*_filesize` - File sizes in bytes
- `path` - Custom mod folder path
- `preview_url` - Preview image URL (file:// or http://)
- `score` - Rating score (0.0-1.0)

---

### leaderboards.txt
Text file defining leaderboards.

**Format:**
```
LEADERBOARD_NAME=sort_method=display_type
```

**Values:**
- `sort_method`: 0=none, 1=ascending, 2=descending
- `display_type`: 0=none, 1=numeric, 2=time_seconds, 3=time_milliseconds

**Example:**
```
GLOBAL_HIGHSCORE=2=1
FASTEST_TIME=1=2
LEVEL_COMPLETION=0=0
```

---

### Simple Text Files

**steam_appid.txt**
- Single line containing the game's App ID
- Example: `730`

**DLC.txt (Legacy Format)**
- One DLC per line in format: `{AppID}={DLC Name}`
- Example:
```
1234=Example DLC Name
5678=Another DLC
```

**depots.txt**
- One depot ID per line
- Example:
```
440
441
442
```

**supported_languages.txt**
- One API language code per line
- Must match Steam's API language codes
- Example:
```
english
french
german
spanish
```

**custom_broadcasts.txt**
- One IP address or domain per line
- Used for custom LAN broadcast addresses
- Example:
```
192.168.1.255
10.0.0.255
custom.domain.com
```

**auto_accept_invite.txt**
- Empty file: Accept all invites (same as overlay disabled)
- Non-empty: One Steam64 ID per line (only accept from these users)
- Example:
```
76561197960287930
76561197960287931
```

**subscribed_groups.txt**
- One Steam group ID per line
- Format: Group ID only

**subscribed_groups_clans.txt**
- One Steam clan ID per line
- Format: Clan ID only

**installed_app_ids.txt**
- One App ID per line
- Lists additional installed applications
- Example:
```
480
570
730
```

**steam_interfaces.txt**
- Lists Steam API interface versions used by the game
- Generated by GoldbergGUI's "Generate steam_interfaces.txt" feature
- One interface per line
- Example:
```
SteamClient017
SteamUser019
SteamFriends015
```

**account_avatar.jpg**
- User's avatar image
- JPEG format
- Displayed in overlay

---

## 6. Experimental Build Features

The experimental build includes additional features:

### LAN-Only Network Filtering
- Blocks all non-LAN IP addresses by default
- Allowed IP ranges:
  - 10.0.0.0 - 10.255.255.255
  - 127.0.0.0 - 127.255.255.255
  - 169.254.0.0 - 169.254.255.255
  - 172.16.0.0 - 172.31.255.255
  - 192.168.0.0 - 192.168.255.255
  - 224.0.0.0 - 255.255.255.255
- Disable with: `configs.main.ini` ? `disable_lan_only=1`

### CPY Crack Support
- Supports CPY-style cracks that patch the executable
- Rename crack DLL to `cracksteam_api.dll` or `cracksteam_api64.dll`
- Replace `steamclient(64).dll` with experimental version

### Extra DLL Loading
- Automatically loads DLLs from `steam_settings/load_dlls/`
- Uses `LoadLibraryW()` function
- Useful for loading additional mods or patches

---

## 7. Valid Language Codes

Source: https://partner.steamgames.com/doc/store/localization/languages

**Available Languages:**
- `arabic` - ???????
- `bulgarian` - ????????? ????
- `schinese` - ????
- `tchinese` - ????
- `czech` - ?eština
- `danish` - Dansk
- `dutch` - Nederlands
- `english` - English
- `finnish` - Suomi
- `french` - Français
- `german` - Deutsch
- `greek` - ????????
- `hungarian` - Magyar
- `italian` - Italiano
- `japanese` - ???
- `koreana` - ???
- `norwegian` - Norsk
- `polish` - Polski
- `portuguese` - Português
- `brazilian` - Português-Brasil
- `romanian` - Român?
- `russian` - ???????
- `spanish` - Español-España
- `latam` - Español-Latinoamérica
- `swedish` - Svenska
- `thai` - ???
- `turkish` - Türkçe
- `ukrainian` - ??????????
- `vietnamese` - Ti?ng Vi?t

---

## 8. Implementation Status in GoldbergGUI

### ? Already Implemented
- `achievements.json` - Achievement definitions
- `DLC.txt` - DLC list (legacy format)
- `steam_appid.txt` - Game App ID
- `configs.main.ini::offline` - Offline mode
- `configs.main.ini::disable_networking` - Disable networking
- `configs.main.ini::disable_overlay` - Disable overlay
- `configs.user.ini::account_name` - Account name
- `configs.user.ini::account_steamid` - Steam ID
- `configs.user.ini::language` - Language setting

### ?? Recommended Implementation Priority

**Phase 1 - Core Settings:**
1. `configs.main.ini` - Complete connectivity settings
2. `configs.app.ini` - DLC `unlock_all`, basic branch support
3. `configs.user.ini` - Complete user settings (ip_country, local_save_path)
4. `supported_languages.txt` - Language list validation

**Phase 2 - Data Files:**
5. `stats.json` - Statistics definitions
6. `branches.json` - Beta branch support
7. `leaderboards.txt` - Leaderboard definitions
8. `depots.txt` - Depot management

**Phase 3 - Advanced Features:**
9. `items.json` - Inventory/workshop items
10. `mods.json` - Workshop mod support
11. `configs.overlay.ini` - Overlay customization
12. `configs.app.ini::cloud_save` - Cloud save directory management

**Phase 4 - Social Features:**
13. `subscribed_groups.txt` - Group subscriptions
14. `auto_accept_invite.txt` - Invite management
15. `custom_broadcasts.txt` - Custom LAN broadcasts
16. `installed_app_ids.txt` - Additional app IDs

---

## 9. Notes for Implementation

### File Creation Guidelines
- Only create files that have user-configured values
- Don't create empty or default-only configuration files
- Use `.EXAMPLE` suffix for template files in documentation

### Path Handling
- Support both absolute and relative paths
- Relative paths should be relative to steam_api DLL location
- Use forward slashes for cross-platform compatibility
- Trim leading/trailing whitespaces from paths

### Validation
- Validate Steam IDs are in proper range (76561197960265729 - 76561202255233023)
- Validate language codes against supported list
- Validate country codes against ISO 3166-1-alpha-2
- Validate JSON structure before saving

### Backward Compatibility
- Continue supporting `DLC.txt` legacy format
- Migrate settings to new format when detected
- Preserve user's existing configurations

### UI/UX Considerations
- Group related settings in logical sections
- Provide tooltips for complex settings
- Show warnings for settings that may cause compatibility issues
- Validate input before saving

---

## 10. References

- **Goldberg Fork Repository:** https://github.com/Detanup01/gbe_fork
- **Steam Partner Documentation:** https://partner.steamgames.com/doc/
- **Language Codes:** https://partner.steamgames.com/doc/store/localization/languages
- **Country Codes:** https://www.iban.com/country-codes
- **strftime Format:** https://en.cppreference.com/w/cpp/chrono/c/strftime
