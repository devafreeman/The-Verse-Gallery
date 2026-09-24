# The Verse Gallery

A first-person 3D gallery built in Unity 6 (URP). The player walks through a gallery of book podiums. Walking up to a podium opens a panel showing that book's title, author, description, screenshots, and a **Visit Website** button. The scene has looping background music, a short voice-over intro, and a slowly rotating skybox.

The playable scene is **`Assets/Scenes/my scene.unity`**.

---

## Requirements

| | |
|---|---|
| Unity version | **6000.0.73f1** (Unity 6). Any Unity 6000.0.x should work; install this exact version to avoid upgrade prompts. |
| Render pipeline | Universal Render Pipeline (URP) 17.0.4 |
| Tools | [Unity Hub](https://unity.com/download) and [Git](https://git-scm.com/downloads) (or GitHub Desktop) |

All packages (URP, Input System, Cinemachine, AI Navigation, etc.) are listed in `Packages/manifest.json` and download automatically when the project is first opened.

---

## How to play the scene

### 1. Download the project

```bash
git clone https://github.com/devafreeman/The-Verse-Gallery.git
```

Or on GitHub click **Code → Download ZIP** and unzip it somewhere with a short path (for example `C:\Projects\The-Verse-Gallery`).

### 2. Open it in Unity

1. Open **Unity Hub**.
2. Click **Add → Add project from disk** and select the `The-Verse-Gallery` folder (the one containing `Assets`, `Packages`, and `ProjectSettings`).
3. If Unity Hub says the editor version is missing, click **Install** for **6000.0.73f1**.
4. Open the project. **The first launch takes several minutes** while Unity re-imports all the assets and rebuilds its `Library` cache. This only happens once.

### 3. Open the scene

In the **Project** window, go to `Assets/Scenes` and double-click **`my scene`**.

> If Unity opens an empty "Untitled" scene or the Starter Assets demo, that is normal. Just open `my scene` as above.

### 4. Press Play

Click the **Play** button at the top of the editor. Click inside the **Game** view once so it captures the mouse.

---

## Controls

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Move | `W` `A` `S` `D` | Left stick |
| Look around | Mouse | Right stick |
| Sprint | `Left Shift` | Left stick press |
| Jump | `Space` | South button (A / Cross) |
| Close a book panel | `Q` or `Esc` | — |
| Open the book's website | Click **Visit Website** on the panel | — |

---

## What happens when you play

- **Background music** starts immediately and loops for the whole session.
- **Voice-over intro** plays about 2 seconds after Play starts.
- **The skybox** slowly rotates.
- **Book podiums (10):** walk up to any podium and its book panel opens automatically, the mouse cursor unlocks, and you can click the buttons. Walk away or press `Q` / `Esc` to close it. The mouse re-locks to camera look.

---

## Building a standalone version

The scene is already set as the only scene in the build list.

1. **File → Build Profiles** (or **Build Settings**).
2. Confirm `Scenes/my scene` is checked in the scene list.
3. Select your platform (Windows, macOS, etc.) and click **Build**.

---

## Bringing the scene into another Unity project

To merge this gallery into a larger project:

1. In **this** project, right-click `Assets/Scenes/my scene.unity` → **Export Package…**
2. Leave **Include dependencies** checked and click **Export**. This creates a `.unitypackage` containing the scene plus every script, material, texture, model, and audio file it uses.
3. In the **target** project, go to **Assets → Import Package → Custom Package…** and select that file.

The target project also needs:

- **URP** as its render pipeline (otherwise materials show up pink).
- These packages from the Package Manager: **Input System**, **Cinemachine**, **AI Navigation**.
- **Player Settings → Other Settings → Active Input Handling** set to **Input System Package (New)** or **Both**.
- The **Staggart Skyboxes** package: copy the `Packages/xyz.staggartcreations.skyboxes` folder into the target project's `Packages` folder.
- A **"Player"** tag on the player object (the podium triggers look for it).

---

## Troubleshooting

| Problem | Fix |
|---|---|
| Everything is pink / magenta | The project isn't using URP. Check **Edit → Project Settings → Graphics** and make sure a URP asset is assigned. |
| Can't move or look around | Click inside the Game view. Also check that **Active Input Handling** is set to the new Input System (or Both). |
| Book panel doesn't open | Make sure the player object has the **Player** tag and a collider / CharacterController. |
| No sound | Check that the Game view's **Mute Audio** toggle is off. |
| Lighting looks flat or dark | Open **Window → Rendering → Lighting** and click **Generate Lighting**. |
| Errors about missing packages on first open | Wait for the import to finish, then open **Window → Package Manager**; it resolves packages from `Packages/manifest.json` automatically. |

---

## Project structure

```
Assets/
  Scenes/my scene.unity        <- the playable gallery scene (+ baked lighting)
  Gallery/
    Scripts/                   <- gallery gameplay scripts
      GalleryBook.cs           <- podium trigger + book data + website link
      BookInteractionUI.cs     <- per-podium book panel (text, screenshots, buttons)
      BackgroundMusic.cs       <- looping background music
      VoiceOverIntro.cs        <- delayed voice-over at start
      SkyboxRotator.cs         <- slow skybox rotation
    Materials/                 <- floor, granite, leather, parchment, poster materials
    Screenshots/               <- images shown on the book panels
    Audio/                     <- voice-over intro
  StarterAssets/               <- Unity first-person controller + input actions
  Barking_Dog/                 <- 3D Free Modular Kit (building pieces)
  grass texture/               <- ground textures
  Timeless European Strings.../<- background music pack
Packages/
  manifest.json                <- Unity package list
  xyz.staggartcreations.skyboxes/ <- skybox package
ProjectSettings/               <- input, graphics, tags/layers, build settings
```

---

## Third-party assets

This project uses free assets from the Unity Asset Store: Unity **Starter Assets – First Person**, **Barking Dog 3D Free Modular Kit**, **Timeless European Strings** music pack, a grass texture pack, and **Staggart Creations Skyboxes**. These remain under their original Asset Store licenses.
