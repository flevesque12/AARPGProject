// Enums partagés du système de spell crafting v4.0 (voir CLAUDE.md, "Spell Crafting System").
// Nommés avec le préfixe "Spell"/"Rune" pour rester découplés de SkillSchool/SkillType
// (Skills/SkillData.cs, v3.1 — encore actif en parallèle jusqu'à son archivage, voir
// CLAUDE.md table "EXISTING DATA ASSETS"), même si les 7 écoles sont les mêmes valeurs.

public enum SpellBaseForm { Projectile, Zone, Aura, Impact }

public enum SpellSchool { Ignis, Aqua, Terra, Ventus, Lux, Umbra, Ferrum }

public enum RuneCategory { Trajectory, Shape, Time, Interaction }

// Types de terrain laissés au sol par les sorts Zone (voir CLAUDE.md, "Environmental
// synergies" — Fire/Water/Wind/Shadow/LooseEarth correspondent aux 5 écoles qui marquent le
// terrain dans le tableau de synergies ; Lux/Ferrum n'en laissent pas). La détection de
// synergie elle-même (Ignis+eau=Vapeur, etc.) est l'item "Environmental synergies" de la
// Phase 8 — pas géré ici, cet enum ne sert pour l'instant qu'à EnvironmentState.
public enum TerrainType { Fire, Water, Wind, Shadow, LooseEarth }
