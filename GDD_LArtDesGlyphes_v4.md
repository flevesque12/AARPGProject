# GDD — L'Art des Glyphes (v4.0)
### Document de Game Design consolidé — Juillet 2026
**Moteur :** Unity 6 (URP) · **Genre :** Action-aventure / Spell-crafting RPG · **Vue :** 3e personne
**Style visuel :** Toon fantasy · Low-poly stylisé · Cel-shaded
**Développeur :** Frédéric Lévesque — Solo dev

> **Note de version v4.0** — Ce document remplace le GDD v3.1.
> Pivot majeur : abandon du modèle ARPG sombre (PoE 2 / Sekiro / Grim Dawn).
> Le jeu adopte une direction toon fantasy colorée, une caméra 3e personne,
> un système de spell crafting modulaire, et un monde semi-ouvert centré sur
> l'apprentissage magique et l'aide aux villageois.
> Tout le contenu des versions précédentes lié au combat hardcore (posture/stagger,
> riposte Sekiro, Tissage Arcanique proc-slots, Ordres Chevalier-Mage, endgame PoE-style)
> est retiré.

---

# TABLE DES MATIÈRES

1. Vision & fondations
2. Piliers de design
3. Le Spell Crafting — cœur du jeu
4. Les 7 écoles de magie
5. Runes modifieurs & crafting avancé
6. Le Grimoire
7. Progression & apprentissage
8. Le combat
9. Le monde — le Village & les Zones
10. Les Librairies des Anciens
11. Histoire & narrative
12. Direction artistique
13. Architecture technique
14. Roadmap de développement
15. Questions ouvertes

---

# PARTIE 1 — VISION & FONDATIONS

## 1.1 Pitch

Un RPG d'action-aventure en 3e personne dans un monde toon fantasy coloré où le joueur incarne un jeune mage qui apprend l'art de la magie en combinant des runes pour crafter ses propres sorts. La magie n'est pas juste un outil de combat — c'est un outil de vie. On aide les villageois, on résout des problèmes environnementaux, on explore des librairies anciennes remplies de savoirs perdus, et on découvre des combinaisons de sorts que personne n'avait imaginées.

## 1.2 Expérience centrale

Le joueur ressent la joie de la découverte. Chaque nouvelle rune trouvée ouvre un éventail de possibilités : « Est-ce que je peux combiner ça avec ça ? Et si j'ajoute un modifieur de rebond à mon sort de vent ? » Le moment où un sort crafté résout un puzzle environnemental d'une façon inattendue — c'est le cœur émotionnel du jeu. La magie est un langage, et le joueur apprend à le parler.

## 1.3 Concept unique — trois différenciateurs

1. **Spell Crafting modulaire** — Le joueur ne choisit pas des sorts dans un menu. Il les construit, rune par rune, dans son Grimoire. Chaque sort est une combinaison unique de forme + école + modifieurs. Deux joueurs n'auront jamais exactement le même spellbook.

2. **Magie utilitaire** — Les sorts ne servent pas seulement au combat. Ignis allume un feu pour réchauffer un villageois en hiver. Aqua irrigue les champs du fermier. Terra répare le pont cassé. Ventus disperse le brouillard toxique. La magie est au service de la communauté autant que du combat.

3. **Monde qui réagit** — Le village évolue visuellement selon l'aide apportée. Les zones se débloquent par la magie apprise. L'environnement est un puzzle géant où chaque nouvelle capacité ouvre des chemins et des opportunités.

## 1.4 Références

| Référence | Ce qu'on prend | Ce qu'on laisse |
|-----------|----------------|-----------------|
| **Mages of Mystralia** | Spell crafting modulaire, ton aventure coloré, mage en apprentissage, 3e personne | La relative simplicité du crafting (on va plus loin) |
| **Magicka 2** | Combinaisons élémentaires dynamiques, humour, chaos créatif, effets environnementaux | Le co-op comme pilier central, l'isométrique |
| **Magic and Mayhem** | Grimoire comme outil central, Portmanteau (craft pré-combat), exploration mythologique, talismans | Le RTS, la grille tactique |
| **Zelda: Tears of the Kingdom** | Créativité émergente, monde qui récompense l'expérimentation | La physique ultra-complexe, le scope AAA |
| **Ni no Kuni** | Direction artistique toon fantasy, chaleur du monde, lien avec la communauté | Le tour par tour |

## 1.5 Plateformes & technique

- **Moteur :** Unity 6 (URP)
- **Caméra :** 3e personne (over-the-shoulder, similaire à Mystralia)
- **Input :** WASD + souris, manette supportée
- **Style visuel :** Low-poly stylisé, cel-shading, palette vibrante
- **Plateforme cible :** PC (prioritaire), consoles envisagées

---

# PARTIE 2 — PILIERS DE DESIGN

## Pilier 1 : Spell Crafting

Le joueur construit ses sorts. Chaque sort est une recette : une **Forme de base** + une **École élémentaire** + des **Runes modifieurs**. L'expérimentation est récompensée. Les meilleurs sorts sont ceux que le joueur invente, pas ceux qu'on lui donne.

## Pilier 2 : Le Grimoire

Le Grimoire est le compagnon permanent du joueur. Il documente chaque rune trouvée, chaque sort crafté, chaque combinaison découverte, chaque lieu visité, chaque créature rencontrée. C'est à la fois l'interface de crafting, le journal de progression, et l'encyclopédie du monde. Le remplir est un objectif en soi.

## Pilier 3 : Monde vivant

Le village hub est le foyer du joueur. Il y retourne entre les aventures, voit l'impact de ses actions, reçoit de nouvelles quêtes. Les zones du monde se débloquent par la magie : chaque école ouvre des chemins spécifiques. Le monde est un puzzle d'exploration interconnecté.

## Pilier 4 : Toon fantasy

L'esthétique est chaleureuse, colorée, vivante. Pas de corruption sombre, pas de gore, pas de grimdark. Le monde a ses dangers et ses mystères, mais la beauté et l'émerveillement dominent. La palette visuelle inspire la curiosité, pas la peur.

---

# PARTIE 3 — LE SPELL CRAFTING

## 3.1 Architecture d'un sort

Chaque sort est composé de trois couches :

```
[Forme de base] + [École élémentaire] + [Runes modifieurs (0 à 4)]
```

**Exemple :**
- Projectile + Ignis = Boule de feu basique
- Projectile + Ignis + Rebond = Boule de feu qui rebondit sur les murs
- Projectile + Ignis + Rebond + Division = Boule de feu qui rebondit ET se divise en 3
- Zone + Aqua + Persistance = Flaque d'eau persistante au sol

## 3.2 Les 4 Formes de base

Chaque forme détermine *comment* le sort se manifeste dans le monde :

| Forme | Comportement | Icône | Usage typique |
|-------|-------------|-------|---------------|
| **Projectile** | Lance un sort devant le joueur en ligne droite | ➜ | Attaque à distance, activer un mécanisme lointain |
| **Zone** | Place un effet au sol autour du point visé | ◎ | Contrôle de zone, piège, aide environnementale |
| **Aura** | Active un effet sur le joueur lui-même | ◇ | Buff, bouclier, résistance |
| **Impact** | Effet mêlée / courte portée devant le joueur | ✦ | Attaque rapprochée, briser un obstacle, repousser |

Le joueur commence le jeu avec la forme **Impact** (la plus intuitive). Les autres formes sont apprises au fil de l'aventure.

## 3.3 Slots de sorts actifs

Le joueur peut équiper **4 sorts craftés** à la fois (slots liés aux boutons / touches). Changer ses sorts équipés se fait via le Grimoire, hors combat ou dans une zone sûre.

Progression des slots :
- Début du jeu : 2 slots
- Mi-jeu : 3 slots
- Fin de jeu : 4 slots

Cela force le joueur à faire des choix stratégiques selon la zone ou le défi.

## 3.4 Mana

La Mana est la seule ressource de cast.

- Chaque sort a un coût de Mana basé sur sa complexité (forme + nombre de modifieurs)
- La Mana se régénère lentement au fil du temps
- Certaines plantes/sources dans le monde accélèrent la régénération
- Le joueur peut crafter des potions de Mana (via des recettes trouvées)
- Coût de base : Forme seule = peu cher. Chaque modifieur ajouté augmente le coût.

| Complexité du sort | Coût indicatif |
|-------------------|---------------|
| Forme seule | 5-10 Mana |
| Forme + 1 modifieur | 12-18 Mana |
| Forme + 2 modifieurs | 20-30 Mana |
| Forme + 3 modifieurs | 35-50 Mana |
| Forme + 4 modifieurs | 55-75 Mana |

Mana max au début : ~100. Augmente avec la progression (trouver des Fontaines d'Arcanite dans les Librairies).

---

# PARTIE 4 — LES 7 ÉCOLES DE MAGIE

Les 7 écoles déterminent *quel type d'effet* le sort produit. Chaque école a son identité visuelle, son utilité au combat ET dans le monde.

## 4.1 Ignis — Le Feu 🔥

- **Effet principal :** Dégâts de feu, brûlure, chaleur
- **Couleur :** Rouge-orangé
- **Combat :** Dégâts directs élevés, DoT (brûlure), AoE
- **Utilité monde :** Allumer des torches/feux de camp, faire fondre la glace, réchauffer des villageois en hiver, cuire des matériaux, brûler des obstacles végétaux
- **Déblocage de zones :** Chemins bloqués par la glace ou la végétation dense

## 4.2 Aqua — L'Eau 💧

- **Effet principal :** Dégâts d'eau, ralentissement, gel (à haut niveau)
- **Couleur :** Bleu
- **Combat :** Ralentissement, contrôle, gel des ennemis
- **Utilité monde :** Irriguer les champs, éteindre les incendies, remplir des réservoirs, nettoyer la pollution, créer des ponts de glace
- **Déblocage de zones :** Rivières à traverser, mécanismes hydrauliques

## 4.3 Terra — La Terre 🪨

- **Effet principal :** Dégâts physiques lourds, murs, protection
- **Couleur :** Brun-vert
- **Combat :** Dégâts lourds lents, murs de protection, zone denial
- **Utilité monde :** Réparer des structures, créer des plateformes, déplacer des rochers, labourer la terre, stabiliser des éboulements
- **Déblocage de zones :** Éboulements à dégager, falaises à escalader (plateformes)

## 4.4 Ventus — Le Vent 🌬️

- **Effet principal :** Poussée, vitesse, effets de zone aériens
- **Couleur :** Cyan-blanc
- **Combat :** Repousser les ennemis, projectiles rapides, esquive améliorée
- **Utilité monde :** Disperser le brouillard/gaz, activer des moulins, sécher des zones humides, porter des objets légers, nettoyer la poussière pour révéler des inscriptions
- **Déblocage de zones :** Gouffres à traverser (saut assisté), brouillard bloquant

## 4.5 Lux — La Lumière ✨

- **Effet principal :** Soin, purification, révélation
- **Couleur :** Doré-blanc
- **Combat :** Soins, boucliers, aveuglement des ennemis, buff d'alliés/invocations
- **Utilité monde :** Soigner les villageois/animaux malades, purifier l'eau, révéler des inscriptions cachées, illuminer les zones sombres, faire pousser des plantes
- **Déblocage de zones :** Zones plongées dans le noir magique, portes nécessitant une clé de lumière

## 4.6 Umbra — L'Ombre 🌑

- **Effet principal :** Invisibilité, confusion, dégâts sournois
- **Couleur :** Violet sombre
- **Combat :** Invisibilité temporaire, confusion ennemie, dégâts bonus depuis les ombres, zones d'obscurité
- **Utilité monde :** Se cacher pour observer, créer des ombres pour protéger les plantes fragiles du soleil, éteindre les lumières, espionner, distraire des créatures
- **Déblocage de zones :** Passages cachés révélés uniquement dans l'ombre, gardes à éviter

## 4.7 Ferrum — Le Métal ⚒️

- **Effet principal :** Renforcement physique, armes magiques, construction
- **Couleur :** Gris acier-cuivré
- **Combat :** Arme magique temporaire, armure renforcée, projectiles métalliques
- **Utilité monde :** Forger/réparer des outils et structures métalliques, créer des leviers, magnétiser des objets, renforcer les murs du village
- **Déblocage de zones :** Portails métalliques rouillés, mécanismes à activer

---

# PARTIE 5 — RUNES MODIFIEURS & CRAFTING AVANCÉ

## 5.1 Les catégories de runes modifieurs

Les runes modifieurs changent le *comportement* d'un sort. Elles sont organisées en 4 catégories :

### Runes de Trajectoire (comment le sort se déplace)

| Rune | Effet | Exemple |
|------|-------|---------|
| **Rebond** | Le sort rebondit sur les surfaces | Boule de feu qui rebondit dans une grotte |
| **Poursuite** | Le sort suit la cible la plus proche | Projectile d'eau à tête chercheuse |
| **Orbite** | Le sort orbite autour du joueur | Bouclier de roches orbitales |
| **Arc** | Le sort décrit un arc de cercle | Tir d'ombre par-dessus un obstacle |

### Runes de Forme (la géométrie de l'effet)

| Rune | Effet | Exemple |
|------|-------|---------|
| **Division** | Le sort se divise en copies multiples (2-3) | Triple boule de feu |
| **Expansion** | L'effet grandit en taille | Grande zone de gel |
| **Chaîne** | L'effet saute d'une cible à la suivante | Éclair qui chaîne entre ennemis |
| **Mur** | L'effet prend la forme d'un mur | Mur de flammes |

### Runes de Temps (la durée / le timing)

| Rune | Effet | Exemple |
|------|-------|---------|
| **Persistance** | L'effet dure plus longtemps | Zone de vent persistante |
| **Retard** | Le sort se déclenche après un délai | Mine de terre retardée |
| **Répétition** | L'effet se répète à intervalles | Pluie de projectiles lumineux |
| **Instantané** | Le sort est plus rapide mais plus faible | Flash de lumière rapide |

### Runes d'Interaction (comment le sort interagit avec le monde)

| Rune | Effet | Exemple |
|------|-------|---------|
| **Absorption** | Le sort absorbe l'élément opposé pour se renforcer | Sort de feu qui absorbe l'eau |
| **Transfert** | L'effet passe du joueur à un objet/allié | Enchanter une torche avec Lux |
| **Attraction** | Le sort attire les objets/ennemis vers lui | Vortex de vent |
| **Imprégnation** | Le sort imprègne le terrain durablement | Sol devenu fertile (Terra) |

## 5.2 Progression des runes

Le joueur ne commence pas avec toutes les runes. Elles sont découvertes à travers le jeu :

| Source | Type de runes | Fréquence |
|--------|--------------|-----------|
| **Quêtes villageoises** | Runes de base (Rebond, Division, Persistance, Expansion) | Régulier |
| **Librairies des Anciens** | Runes avancées (Poursuite, Chaîne, Absorption, Transfert) | Peu fréquent |
| **Expérimentation au Grimoire** | Runes secrètes (combinaisons spécifiques qui débloquent une rune cachée) | Rare |
| **PNJ spéciaux / maîtres** | Runes de maîtrise (versions améliorées) | Très rare |

## 5.3 Synergies cross-école

Quand un sort utilise une école qui interagit avec un effet déjà présent dans l'environnement, une **Synergie** se déclenche :

| École A | + École B (dans l'environnement) | Synergie |
|---------|----------------------------------|----------|
| Ignis | Aqua (zone d'eau au sol) | **Vapeur** — brouillard chaud, zone d'aveuglement |
| Ignis | Ventus (zone de vent) | **Tempête de feu** — flammes propagées, dégâts étendus |
| Aqua | Ventus (zone de vent) | **Tempête de grêle** — projectiles glacés, ralentissement AoE |
| Aqua | Terra (zone de terre meuble) | **Boue** — ralentissement fort, piège |
| Terra | Ignis (zone de feu au sol) | **Magma** — dégâts massifs persistants |
| Ventus | Umbra (zone d'ombre) | **Brume noire** — invisibilité de zone pour le joueur |
| Lux | Umbra (zone d'ombre) | **Éclipse** — explosion de dégâts purs |
| Lux | Aqua (zone d'eau) | **Prisme** — réflexion de tous les projectiles dans la zone |
| Ferrum | Ignis (zone de feu) | **Forge** — renforce l'arme/armure temporairement |
| Ferrum | Terra (zone de terre) | **Rempart** — mur ultra-résistant |

Les synergies se découvrent naturellement en jouant et sont documentées dans le Grimoire quand activées pour la première fois. C'est un des moments « eurêka » du jeu.

## 5.4 Limite de crafting

Un sort peut contenir **au maximum 4 runes modifieurs**. Cette limite empêche les sorts surpuissants et force le joueur à faire des choix intéressants.

Certaines runes sont **incompatibles** entre elles :
- Poursuite + Arc (contradictoire — le sort suit ou fait un arc)
- Instantané + Persistance (contradictoire — rapide ou durable)
- Expansion + Mur (contradictoire — zone ou ligne)

Ces incompatibilités sont affichées clairement dans le Grimoire.

---

# PARTIE 6 — LE GRIMOIRE

## 6.1 Interface

Le Grimoire est un livre interactif que le joueur ouvre (touche dédiée / bouton). Il a plusieurs onglets / sections :

### Atelier de Craft
- Interface visuelle de type node-graph (inspirée de Mystralia)
- Au centre : la forme de base choisie
- Autour : les slots pour les runes modifieurs (0 à 4)
- Prévisualisation en temps réel de l'effet du sort
- Indicateur de coût de Mana
- Bouton « Tester » (lance le sort sur un mannequin d'entraînement dans le village)

### Spellbook (sorts sauvegardés)
- Liste de tous les sorts craftés par le joueur
- Possibilité de nommer ses sorts
- Drag & drop vers les 4 slots actifs
- Filtre par école / par forme

### Encyclopédie des Runes
- Catalogue de toutes les runes découvertes
- Description, source, effets
- Runes manquantes affichées en silhouette (motivation à explorer)

### Encyclopédie des Synergies
- Chaque synergie découverte est documentée avec illustration
- Synergies non-découvertes affichées comme « ??? » (motivation)

### Journal du monde
- Lore des lieux visités
- PNJ rencontrés
- Créatures documentées (bestiaire)

## 6.2 Philosophie UX du Grimoire

Le Grimoire doit être **plaisant à utiliser**. Ce n'est pas un menu, c'est un objet dans le monde. Le joueur doit avoir envie de l'ouvrir, de feuilleter, de tester des trucs. L'animation d'ouverture/fermeture, le bruit des pages, les illustrations qui apparaissent — tout contribue au plaisir.

Références UX : le Sheikah Slate dans Zelda BotW (un outil qu'on a envie de sortir), le spellbook de Mystralia (crafting visuel intuitif).

---

# PARTIE 7 — PROGRESSION & APPRENTISSAGE

## 7.1 Les trois sources de savoir

Le joueur progresse en acquérant du **Savoir Magique**, pas des niveaux numériques traditionnels. Trois sources :

### Source 1 : Aider les villageois
Les villageois du hub ont des problèmes concrets. En les aidant, le joueur apprend.

| Quête exemple | Récompense |
|--------------|-----------|
| Le fermier a besoin d'eau pour ses champs | Rune de base Aqua + Forme Zone |
| La forge est éteinte | Rune de base Ignis + Forme Impact |
| Le pont est cassé | Rune de base Terra + concept de Persistance |
| Le guetteur ne voit rien dans le brouillard | Rune de base Ventus + Forme Projectile |
| L'herboriste a une plante malade | Rune de base Lux + concept de Transfert |

### Source 2 : Explorer les Librairies des Anciens
Les Librairies sont les « donjons » du jeu (voir Partie 10). Chaque Librairie contient :
- Des **Pages Perdues** (runes modifieurs avancées)
- Du **Lore** (enrichit le Grimoire, donne des indices sur les synergies)
- Des **Épreuves de Savoir** (puzzles qui enseignent une mécanique de crafting)
- Un **Gardien** (boss qui teste la maîtrise du joueur)

### Source 3 : L'expérimentation libre
Le Grimoire récompense la curiosité :
- Crafter un sort avec une combinaison inédite → entrée dans le Grimoire
- Découvrir une synergie environnementale → page de synergie débloquée
- Utiliser un sort d'une façon créative pour résoudre un problème → bonus de Savoir

## 7.2 La jauge de Savoir

Le Savoir Magique est une valeur qui augmente globalement. À certains paliers :
- Nouveaux slots de sorts disponibles
- Mana max augmentée
- Capacité à supporter plus de modifieurs par sort
- Formes de base supplémentaires débloquées
- Accès à de nouvelles zones et Librairies

Ce n'est pas un « Level 1-50 ». C'est une progression organique liée à ce que le joueur a réellement appris et découvert.

## 7.3 Arbre de Spécialisation (optionnel, mi-jeu)

À mi-jeu, le joueur peut choisir **2 écoles principales** sur 7 (comme dans le design original). Cela ne bloque pas les autres écoles mais confère des bonus passifs aux deux écoles choisies :
- Coût de Mana réduit pour ces écoles
- Runes modifieurs de maîtrise débloquées (propres à chaque école)
- Synergies exclusives entre les deux écoles choisies

Ce choix est ré-assignable au village (pour ne pas punir l'expérimentation).

---

# PARTIE 8 — LE COMBAT

## 8.1 Philosophie

Le combat est un **puzzle créatif**, pas un test de réflexes hardcore. Le joueur gagne en utilisant le bon sort au bon moment, en exploitant les synergies environnementales, et en craftant des sorts adaptés à la situation. C'est plus proche de Magicka et Mystralia que de Dark Souls.

## 8.2 Mouvements du joueur

| Action | Input | Détail |
|--------|-------|--------|
| **Déplacement** | WASD / stick gauche | Mouvement libre en 3e personne |
| **Caméra** | Souris / stick droit | Rotation caméra over-the-shoulder |
| **Esquive** | Espace / bouton | Roulade rapide avec courtes i-frames (~0.2s) |
| **Sprint** | Shift / bouton | Course, consomme de l'endurance (régénération rapide) |
| **Sort 1-4** | Clic / 1-2-3-4 / gâchettes | Lance le sort équipé dans le slot |
| **Grimoire** | Tab / bouton | Ouvre le Grimoire (pause le jeu en solo) |
| **Interaction** | E / bouton | Parler, ramasser, activer |

## 8.3 Design des ennemis

Les ennemis ne sont pas des « sacs à PV ». Chaque type d'ennemi a des **faiblesses élémentaires** et des **comportements** qui encouragent l'utilisation créative de sorts :

| Type | Comportement | Contre-stratégie |
|------|-------------|-----------------|
| **Golem de Pierre** | Lent, très résistant, charge | Aqua pour ramollir → Impact pour briser |
| **Feu Follet** | Rapide, volant, brûlure | Aqua pour éteindre, Ventus pour repousser |
| **Lierre Animé** | Enracine le joueur, multiplication | Ignis pour brûler, Ferrum pour couper |
| **Spectre** | Intangible, apparaît/disparaît | Lux pour révéler, Umbra pour piéger |
| **Automate Rouillé** | Mécanique, prévisible, résistant | Aqua pour rouiller, Ignis pour souder |
| **Brume Vivante** | Zone de confusion, insaisissable | Ventus pour disperser, Lux pour purifier |

### Boss — Les Gardiens de Librairie
Chaque Librairie a un Gardien : un boss qui teste la maîtrise d'une école (ou d'une synergie). Le Gardien n'est pas un test de DPS — c'est un puzzle de combat :
- Le Gardien a des phases avec des mécaniques spécifiques
- Chaque phase nécessite une école ou une synergie particulière
- Le joueur doit adapter ses sorts (retour au Grimoire entre les phases si nécessaire)

## 8.4 Dégâts & santé

- Le joueur a des **Points de Vie** (PV) qui augmentent avec le Savoir
- PV récupérés via : sorts Lux (soin), potions craftées, sources de soin dans le monde
- Pas de potions achetées infiniment — les ingrédients sont récoltés et les potions craftées
- Mort = retour au dernier point de sauvegarde (statues dans le monde / lit au village)
- Pas de punition sévère — le jeu est challenging mais pas punitif

## 8.5 Effets environnementaux en combat

Le terrain de combat compte. Les sorts laissent des traces :

| Sort utilisé | Effet sur le terrain | Durée |
|-------------|---------------------|-------|
| Ignis (zone) | Sol en feu — dégâts aux ennemis qui marchent dessus | 6s |
| Aqua (zone) | Flaque d'eau — ralentissement | 8s |
| Terra (zone) | Terrain surélevé — couverture / obstacle | 12s |
| Ventus (zone) | Courant d'air — pousse les projectiles dans une direction | 5s |
| Umbra (zone) | Zone sombre — le joueur est invisible dedans | 7s |
| Lux (zone) | Zone lumineuse — buff de régénération dedans | 8s |
| Ferrum (zone) | Sol métallique — reflète certains sorts | 10s |

Combiner des effets de terrain = synergies (voir §5.3). C'est le cœur du combat avancé.

---

# PARTIE 9 — LE MONDE

## 9.1 Structure : Hub + Zones

Le monde est **semi-ouvert** : un village central (hub) avec des chemins menant à différentes zones. Certains chemins sont bloqués et nécessitent des sorts spécifiques pour s'ouvrir.

### Le Village — Havrevent

**Havrevent** est un petit village de montagne, construit autour d'une ancienne fontaine magique asséchée. C'est le foyer du joueur.

**Lieux dans le village :**

| Lieu | Fonction |
|------|----------|
| **Maison du joueur** | Sauvegarde, repos, stockage |
| **Place centrale (Fontaine)** | Point de fast-travel, hub social |
| **Atelier du forgeron** | Craft d'équipement basique, réparation |
| **Ferme de Liora** | Quêtes d'aide, ingrédients pour potions |
| **Bibliothèque du village** | Lore de base, indices vers les Librairies |
| **Tour du guetteur** | Vue sur le monde, révèle les zones accessibles |
| **Auberge** | PNJ voyageurs avec quêtes secondaires, rumeurs |

**Évolution du village :**
Le village change visuellement selon les quêtes complétées :
- Aider le fermier → les champs verdissent
- Réparer le pont → les marchands arrivent
- Restaurer la fontaine → la magie revient au village (milestone majeur)
- Aider l'herboriste → un jardin médicinal apparaît
- Renforcer les murs → le village résiste à un événement narratif

## 9.2 Les Zones du monde

Chaque zone a un biome distinct, un thème élémentaire dominant, et au moins une Librairie des Anciens.

| Zone | Biome | École dominante | Déblocage requis | Contenu clé |
|------|-------|----------------|-----------------|-------------|
| **Forêt d'Émeraude** | Forêt tempérée dense | Terra / Lux | Début du jeu (accessible) | Zone tutoriel, première Librairie |
| **Lacs Jumeaux** | Lacs et marais | Aqua | Sort Aqua (traverser l'eau) | Village de pêcheurs, brouillard, Librairie submergée |
| **Pics de Cendre** | Montagnes volcaniques | Ignis | Sort Terra (créer un chemin) + Ignis (résister à la chaleur) | Forgeron légendaire, Librairie dans le cratère |
| **Plaines de Zéphyr** | Prairies ventées, canyons | Ventus | Sort Ventus (saut assisté) | Nomades du vent, Librairie flottante |
| **Bosquet Crépusculaire** | Forêt sombre, champignons lumineux | Umbra / Lux | Sort Lux (lumière) + Umbra (voir dans le noir) | Créatures nocturnes, Librairie cachée dans l'ombre |
| **Citadelle de Rouille** | Ruines mécaniques, engrenages | Ferrum | Sort Ferrum (activer mécanismes) | Automates, puzzles mécaniques, Librairie-forge |
| **Le Sanctuaire** | Zone finale, convergence | Toutes | Toutes les écoles maîtrisées | Zone de climax narratif, Librairie Primordiale |

---

# PARTIE 10 — LES LIBRAIRIES DES ANCIENS

## 10.1 Concept

Les Librairies des Anciens sont les « donjons » du jeu. Ce ne sont pas des lieux sombres et dangereux — ce sont des **sanctuaires de savoir** protégés par des épreuves magiques. Chaque Librairie teste les compétences du joueur dans une école spécifique.

## 10.2 Structure d'une Librairie

Chaque Librairie suit un format en 4 étages :

1. **Le Vestibule** — Introduction, lore, puzzle d'entrée simple
2. **Les Salles d'Étude** — 2-3 salles de puzzles combinant la magie et l'environnement
3. **La Salle d'Épreuve** — Un défi de combat qui requiert la maîtrise de l'école de la Librairie
4. **La Bibliothèque Intérieure** — Récompense : Pages Perdues (runes avancées), lore profond, augmentation de Savoir

## 10.3 Types d'épreuves dans les Librairies

| Type | Description | Exemple |
|------|-------------|---------|
| **Puzzle environnemental** | Utiliser la magie pour modifier le terrain et atteindre un objectif | Rediriger un cours d'eau avec Terra pour activer un mécanisme Aqua |
| **Épreuve de précision** | Toucher des cibles dans un ordre ou un timing | Allumer des torches avec un projectile Ignis rebondissant |
| **Épreuve de créativité** | Plusieurs solutions possibles, récompense la créativité | Traverser un gouffre — Ventus (saut) ? Terra (pont) ? Ferrum (grappin) ? |
| **Combat de Gardien** | Boss-puzzle qui teste la maîtrise d'une école | Le Gardien de Glace (faible à Ignis, mais protégé par un bouclier Aqua qu'il faut d'abord contourner) |
| **Épreuve de synergie** | Combiner deux écoles pour résoudre | Créer de la Vapeur (Ignis + Aqua) pour activer un mécanisme à pression |

## 10.4 Les 7 Librairies

| Librairie | Zone | École testée | Gardien | Récompense clé |
|-----------|------|-------------|---------|----------------|
| **Librairie de l'Aube** | Forêt d'Émeraude | Terra / bases | Golem de Mousse | Forme Projectile, runes de base |
| **Librairie des Profondeurs** | Lacs Jumeaux | Aqua | Léviathan de Cristal | Rune Chaîne, lore sur les synergies |
| **Librairie du Cratère** | Pics de Cendre | Ignis | Phénix de Pierre | Rune Expansion, sorts de forge |
| **Librairie des Courants** | Plaines de Zéphyr | Ventus | Serpent d'Air | Rune Orbite, sorts de vol assisté |
| **Librairie de l'Éclipse** | Bosquet Crépusculaire | Umbra + Lux | Le Gardien Dédoublé | Rune Absorption, synergie Éclipse |
| **Librairie-Forge** | Citadelle de Rouille | Ferrum | Titan de Métal | Rune Imprégnation, sorts de construction |
| **Librairie Primordiale** | Le Sanctuaire | Toutes | L'Archiviste | Rune de Maîtrise ultime, fin de l'histoire |

---

# PARTIE 11 — HISTOIRE & NARRATIVE

## 11.1 Prémisse

Le joueur est un jeune habitant de Havrevent, un village isolé dont la fontaine magique — source de toute la magie de la région — s'est asséchée il y a des années. La magie a presque disparu du monde. Le joueur découvre un vieux Grimoire dans le grenier de sa maison et réalise qu'il a une affinité naturelle avec la magie.

Guidé par **Maeva**, une vieille herboriste du village qui connaît les légendes des Anciens, le joueur part restaurer les 7 Librairies des Anciens pour comprendre pourquoi la magie s'éteint — et comment la ramener.

## 11.2 Arc narratif principal

| Acte | Contenu | Thème |
|------|---------|-------|
| **Acte 1 : L'Éveil** | Le joueur découvre le Grimoire, apprend les bases en aidant Havrevent. Première Librairie (Forêt d'Émeraude). | Découverte, apprentissage |
| **Acte 2 : L'Exploration** | Le joueur explore 3-4 zones, restaure les Librairies, rencontre d'autres communautés. Il apprend que les Librairies sont connectées par un réseau magique. | Croissance, communauté |
| **Acte 3 : La Vérité** | Le joueur découvre pourquoi la magie s'éteint : un Ancien a verrouillé la magie pour protéger le monde d'une force dangereuse. Mais le verrouillage tue lentement le monde aussi. | Dilemme moral, nuance |
| **Acte 4 : Le Choix** | Le joueur doit choisir : restaurer pleinement la magie (risquer le danger), la laisser s'éteindre (sauver le monde mais perdre la magie), ou trouver un équilibre (nécessite la maîtrise des 7 écoles). | Responsabilité, sagesse |

## 11.3 Ton narratif

- **Chaleureux** — les PNJ sont attachants, le village est un foyer
- **Curieux** — les mystères donnent envie d'explorer, pas peur d'avancer
- **Nuancé** — pas de méchant manichéen. L'Ancien qui a verrouillé la magie avait de bonnes raisons
- **Humoristique (par touches)** — certains PNJ sont drôles, certaines situations absurdes, sans que ça tourne à la parodie

---

# PARTIE 12 — DIRECTION ARTISTIQUE

## 12.1 Style visuel

**Toon fantasy** — Un monde qui ressemble à un conte de fées interactif.

Inspirations visuelles :
- **Mages of Mystralia** — Coloré, stylisé, chaleureux
- **Ni no Kuni** — Cel-shaded expressif
- **A Short Hike** — Low-poly vibrant, nature accueillante
- **Okami** — Monde peint, magie visible et belle
- **Genshin Impact** (esthétique, pas le gacha) — Cel-shaded en monde ouvert

## 12.2 Palette de couleurs par zone

| Zone | Couleurs dominantes | Ambiance |
|------|-------------------|---------|
| Havrevent | Ocre, vert, bois naturel | Chaleur de foyer |
| Forêt d'Émeraude | Vert profond, doré | Exploration paisible |
| Lacs Jumeaux | Bleu, argent, brume blanche | Mystère tranquille |
| Pics de Cendre | Rouge, orange, noir volcanique | Défi intense mais beau |
| Plaines de Zéphyr | Cyan, blanc, herbe dorée | Liberté, espace |
| Bosquet Crépusculaire | Violet, bioluminescence | Émerveillement nocturne |
| Citadelle de Rouille | Cuivre, bronze, vert-de-gris | Nostalgie mécanique |
| Le Sanctuaire | Arc-en-ciel, blanc pur | Majesté, culmination |

## 12.3 VFX magiques

Chaque école a son **langage visuel** distinct :
- **Ignis** — Particules chaudes, traînées lumineuses orange
- **Aqua** — Flux liquides, gouttes, éclats de cristal de glace
- **Terra** — Roches qui craquent, poussière, géométrie angulaire
- **Ventus** — Lignes de flux, feuilles emportées, traînées de vitesse
- **Lux** — Rayons dorés, étoiles, lueur douce
- **Umbra** — Fumée violette, particules sombres, distorsion visuelle
- **Ferrum** — Étincelles, éclats métalliques, géométrie précise

Les sorts craftés combinent visuellement les VFX de leur école + leurs modifieurs. Un sort Ignis + Rebond laisse des traînées de feu à chaque rebond. Un sort Aqua + Division montre trois jets d'eau qui se séparent.

---

# PARTIE 13 — ARCHITECTURE TECHNIQUE

## 13.1 Stack technologique

- **Moteur :** Unity 6 (Universal Render Pipeline)
- **Langage :** C#
- **Rendering :** URP + cel-shader custom
- **Input :** New Input System (gamepad + clavier/souris)
- **Pathfinding :** NavMeshAgent (ennemis uniquement)
- **Joueur :** CharacterController (mouvement direct WASD)
- **Caméra :** Cinemachine (3e personne, over-the-shoulder)

## 13.2 Architecture Spell Crafting

Le système de sorts est basé sur des **ScriptableObjects** empilables :

```
SpellRecipe (ScriptableObject)
├── BaseForm (enum: Projectile, Zone, Aura, Impact)
├── School (enum: Ignis, Aqua, Terra, Ventus, Lux, Umbra, Ferrum)
├── ModifierRunes[] (array de RuneModifier SO, max 4)
├── ManaCost (calculé dynamiquement)
├── CooldownTime (calculé dynamiquement)
└── VFXProfile (généré selon School + Modifiers)

RuneModifier (ScriptableObject)
├── RuneType (enum: Trajectory, Shape, Time, Interaction)
├── RuneName (string)
├── ManaCostMultiplier (float)
├── IncompatibleWith[] (list de RuneModifier)
└── BehaviorData (paramètres spécifiques au type)
```

## 13.3 Structure des dossiers

```
Assets/Scripts/
├── Core/
│   ├── GameInput.cs              # Abstraction New Input System
│   ├── HealthSystem.cs           # PV joueur + ennemis
│   ├── ManaSystem.cs             # Gestion Mana
│   ├── SavoirSystem.cs           # Progression Savoir Magique
│   └── SaveManager.cs            # Sauvegarde / chargement
├── Player/
│   ├── PlayerController.cs       # Mouvement 3e personne (CharacterController)
│   ├── PlayerCombat.cs           # Gestion des 4 slots, lancement de sorts
│   ├── DodgeRoll.cs              # Esquive avec i-frames
│   ├── SprintController.cs       # Sprint
│   └── InteractionController.cs  # Interaction PNJ / objets
├── SpellCraft/
│   ├── Data/
│   │   ├── SpellRecipe.cs        # SO — recette de sort complète
│   │   ├── BaseFormData.cs       # SO — données par forme de base
│   │   ├── SchoolData.cs         # SO — données par école
│   │   └── RuneModifier.cs       # SO — données par rune modifieur
│   ├── Runtime/
│   │   ├── SpellCaster.cs        # Instancie et lance un sort en jeu
│   │   ├── SpellFactory.cs       # Construit le sort selon la recette
│   │   ├── ProjectileSpell.cs    # Comportement projectile
│   │   ├── ZoneSpell.cs          # Comportement zone
│   │   ├── AuraSpell.cs          # Comportement aura
│   │   ├── ImpactSpell.cs        # Comportement impact
│   │   └── ModifierProcessor.cs  # Applique les modifieurs à un sort
│   └── Synergies/
│       ├── SynergyData.cs        # SO — définition d'une synergie
│       ├── SynergyDetector.cs    # Détecte les conditions de synergie
│       └── EnvironmentState.cs   # Track les effets de terrain actifs
├── Grimoire/
│   ├── GrimoireUI.cs             # Interface principale du Grimoire
│   ├── CraftingPanel.cs          # Panel de craft (node-graph)
│   ├── SpellbookPanel.cs         # Liste des sorts sauvegardés
│   ├── RuneEncyclopedia.cs       # Catalogue des runes
│   ├── SynergyEncyclopedia.cs    # Catalogue des synergies
│   └── JournalPanel.cs           # Journal du monde
├── Enemy/
│   ├── EnemyAI.cs                # FSM basique (Idle, Patrol, Chase, Attack, Hurt, Death)
│   ├── EnemySpawner.cs           # Gestion des spawns
│   ├── ElementalWeakness.cs      # Composant faiblesses élémentaires
│   └── GuardianBoss.cs           # Logique boss des Librairies
├── World/
│   ├── VillageEvolution.cs       # Système d'évolution du village
│   ├── ZoneUnlocker.cs           # Déblocage de zones par sorts
│   ├── EnvironmentEffect.cs      # Effets des sorts sur le terrain
│   ├── NPCDialogue.cs            # Système de dialogue
│   └── LibraryManager.cs         # Gestion des Librairies (puzzles, progression)
├── Camera/
│   ├── ThirdPersonCamera.cs      # Cinemachine wrapper
│   └── CameraEffects.cs          # Effets caméra (shake léger, zoom sur craft)
└── UI/
    ├── PlayerHUD.cs              # Barre de vie, mana, slots de sorts
    ├── DamageNumbers.cs          # Affichage dégâts stylisé
    ├── QuestTracker.cs           # Suivi de quêtes
    └── DialogueUI.cs             # Interface de dialogue
```

## 13.4 Conventions de code

- PascalCase classes/méthodes, camelCase variables
- Commentaires en anglais (code), UI en français
- `[SerializeField]` pour l'Inspector, jamais public sauf si API
- `[Header]` et `[Tooltip]` sur les valeurs de balance
- Events C# (`event Action<T>`) pour la communication inter-systèmes
- ScriptableObjects pour toutes les données de design
- Pas de singletons — injection de dépendances via l'Inspector
- Un script = une responsabilité

---

# PARTIE 14 — ROADMAP DE DÉVELOPPEMENT

## Phase 1 — Core Loop (fondation)
- [ ] PlayerController 3e personne (CharacterController + Cinemachine)
- [ ] Mouvement WASD + caméra souris
- [ ] Esquive (DodgeRoll)
- [ ] Sort basique (1 forme Impact + 1 école Ignis)
- [ ] Ennemi basique avec FSM (Idle → Chase → Attack → Death)
- [ ] HealthSystem + ManaSystem
- [ ] HUD minimal (PV, Mana, 1 slot)

## Phase 2 — Spell Crafting de base
- [ ] SpellRecipe ScriptableObject
- [ ] Les 4 formes de base fonctionnelles
- [ ] 3 écoles jouables (Ignis, Aqua, Terra)
- [ ] Grimoire UI — atelier de craft basique
- [ ] 4 runes modifieurs de base (Rebond, Division, Persistance, Expansion)
- [ ] Slots de sorts (2 slots)
- [ ] Terrain effects basiques (feu au sol, flaque d'eau)

## Phase 3 — Le Village & la première Librairie
- [ ] Havrevent — layout du village, 4-5 PNJ avec dialogues
- [ ] 3 quêtes villageoises tutorielles
- [ ] Forêt d'Émeraude — première zone explorable
- [ ] Librairie de l'Aube — 4 salles, puzzles, Gardien
- [ ] VillageEvolution — premier changement visible
- [ ] Sauvegarde/chargement

## Phase 4 — Spell Crafting complet
- [ ] Les 7 écoles fonctionnelles
- [ ] 16 runes modifieurs (toutes catégories)
- [ ] Synergies environnementales (10 combinaisons)
- [ ] Grimoire complet (craft, spellbook, encyclopédie, journal)
- [ ] 3 slots de sorts
- [ ] Incompatibilités de runes

## Phase 5 — Monde étendu
- [ ] 3 zones supplémentaires (Lacs, Pics, Plaines)
- [ ] 3 Librairies supplémentaires avec Gardiens
- [ ] Zone unlocking via sorts
- [ ] PNJ secondaires, quêtes de zone
- [ ] Spécialisation d'école (choix de 2 écoles)

## Phase 6 — Contenu complet
- [ ] Toutes les zones (7 + village)
- [ ] Toutes les Librairies (7)
- [ ] Tous les Gardiens
- [ ] Arc narratif complet (4 actes)
- [ ] Village entièrement évolutif
- [ ] 4 slots de sorts

## Phase 7 — Polish & lancement
- [ ] Cel-shader et VFX finaux
- [ ] Musique et sound design
- [ ] Balancing complet
- [ ] Tutoriel et onboarding
- [ ] Accessibilité (remapping, sous-titres, options de difficulté)
- [ ] Build de lancement

---

# PARTIE 15 — QUESTIONS OUVERTES

1. **Nom du monde.** « Havrevent » pour le village est un placeholder — valider si ça sonne bien ou trouver mieux. Le monde lui-même n'a pas encore de nom (l'ancien « Cairn » est retiré car c'est le monde de Grim Dawn).

2. **Compagnon.** Est-ce que le joueur a un compagnon (familier magique, créature, PNJ) ? Magicka 2 est en co-op, Mystralia est solo. Un familier pourrait ajouter de la personnalité et servir de guide narratif.

3. **Craft d'équipement.** Au-delà des sorts, est-ce que le joueur peut crafter de l'équipement (robes, bâtons) qui modifient ses stats ou ses affinités d'école ? Ça ajouterait de la profondeur au mid/late game.

4. **Multijoueur.** Le jeu est conçu solo pour le moment. Un co-op local (2 joueurs, style Magicka) serait un excellent ajout futur mais représente un scope énorme.

5. **Monstres invocables.** Magic and Mayhem permet d'invoquer des créatures. Est-ce qu'une 5e forme de sort « Invocation » (temporaire, liée à une école) serait intéressante ? Ça ajouterait une couche mais aussi de la complexité.

6. **Système de quêtes.** Formaliser le système de quêtes villageoises — tableau de quêtes dans l'auberge ? PNJ avec icônes ? Quêtes qui apparaissent organiquement en explorant le village ?

7. **Jour/Nuit.** Un cycle jour/nuit ajouterait de l'atmosphère et pourrait affecter Lux/Umbra (Lux plus fort le jour, Umbra plus fort la nuit). Mais c'est du scope en plus.

---

*Fin du GDD v4.0 — L'Art des Glyphes*
*Ce document est un point de départ vivant. Chaque section sera enrichie au fil du développement.*
