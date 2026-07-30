// Enums partagés du système de spell crafting v4.0 (voir CLAUDE.md, "Spell Crafting System").
// Nommés avec le préfixe "Spell"/"Rune" pour rester découplés de SkillSchool/SkillType
// (Skills/SkillData.cs, v3.1 — encore actif en parallèle jusqu'à son archivage, voir
// CLAUDE.md table "EXISTING DATA ASSETS"), même si les 7 écoles sont les mêmes valeurs.

public enum SpellBaseForm { Projectile, Zone, Aura, Impact }

public enum SpellSchool { Ignis, Aqua, Terra, Ventus, Lux, Umbra, Ferrum }

public enum RuneCategory { Trajectory, Shape, Time, Interaction }
