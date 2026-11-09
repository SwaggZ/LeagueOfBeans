## Summary

Upgrades the project to Unity 6, fixes a set of loose ends, restyles the UI, and adds two new champions (Jinx and Swain).

### Unity 6 upgrade and cleanup
- Project upgraded to Unity 6000.3.8f1 (URP 17.3, FishNet 4); compile errors fixed and deprecated APIs updated.
- Removed the leftover `NetworkSessionBridge`; champ-select selection/ready now use FishNet broadcasts. Stale docs rewritten.

### Champ select and HUD fixes
- Champ-select previews matched the wrong champion (Ashe showed Aphelios, Galio showed Ashe). Entry ids now come from the gameplay prefab, so preview and spawn always agree.
- Broken cooldowns fixed (Galio R, Lux E double throw, Aphelios weapon-switch spam, dash).
- The cooldown HUD is one global HUD that survives death/respawn and swaps loadouts; each slot shows its key (LMB / RMB / Q / E / CTRL).
- Restyled HUD and champ select (gold/slate theme, ability row with key badges).

### Main menu
- Rotating champion splash showcase, new logo, PLAY / SETTINGS / QUIT.
- Server browser as host / join cards with LAN list and direct connect.
- Working Settings page (volume, mouse sensitivity, fullscreen, graphics quality) saved to PlayerPrefs.
- Applied at runtime by `MainMenuStyler`, so the generated menu prefab and its wiring are untouched.

### New champions
- **Jinx**: hold-to-fire minigun that spins up / splash rocket launcher (LMB), weapon swap (RMB), three thrown stun mines (Q), loaded mega rocket that scales with distance and missing health (E), speed boost on kills.
- **Swain**: flat lightning cone (LMB), grasping hand that stuns and can be recast to pull (RMB), eye that bursts and drains health to heal (Q), demon-form aura that drains and heals, ending after 5s without hits or 20s max (E).
- `ChampionScaffold` (Tools → &lt;Champion&gt; → Build Champion) builds a champion from the bean rig and registers it everywhere: gameplay + preview prefabs, material, champ-select entry and button, and the `NetworkGameSession` spawn lists.
- `AbilityHits`: shared casts, damage/heal, stun and pull helpers for future champions.

## Testing
Tested in Play Mode as host: every champ-select button shows the right preview and spawns the right champion; the HUD persists through death and respawn; all Jinx and Swain abilities were exercised against the training dummies (damage, splash, stun, pull, heal, demon-form timeouts, kill speed boost); the main menu panels and hosting were checked.

## Notes for reviewers
- **Not tested with two players.** Like the existing champions, ability effects run locally on the owner.
- New champions must also be in `NetworkGameSession.characterPrefabs` (Menu and selection scenes) or the server spawns the first champion instead; the scaffold handles this.
- Bean textures with a centered face need material Offset X = 0.25 (the bean's front is at u = 0.25).
- Known issues left as-is: broken VFX graph ("Invalid VFX Particle System"), `selection` scene disabled in Build Settings but loaded by name on single-player death, unfinished `LobbyReadyPanel`.
- Splash art in `Assets/SplashArts` is Riot's; fine for a free fan project, would need replacing for anything commercial.
