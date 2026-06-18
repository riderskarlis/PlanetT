---
name: planet-magnetization-for-spaceship
description: Implements spaceship magnetization to planets with surface-following movement and smooth transition behavior
source: auto-skill
extracted_at: '2026-06-17T17:07:13.784Z'
---

This skill describes how to implement spaceship magnetization to planets in Unity, where the spaceship:
1. Automatically becomes magnetized to planets when approaching them
2. Follows the planet's surface when moving toward targets
3. Properly orients itself to align with planet surfaces
4. Can smoothly detach when moving away from planet influence

The approach involves modifying SpaceshipController.cs to:
- Add magnetization state tracking (isMagnetizedToPlanet, currentPlanet)
- Implement planet detection logic using MovementTargeting.IsNearPlanet()
- Modify movement behavior to project targets onto planet surfaces
- Update orientation calculations to align with planet surface normals
- Handle smooth transitions between orbital and planetary movement modes

Key implementation details:
- When near a planet (within planetRadiusTrigger distance), spaceship becomes magnetized
- Movement targets are projected onto the planet's surface using surface normal
- Spaceship orientation uses the planet's surface normal for proper alignment
- When leaving planet influence, spaceship releases magnetization and returns to normal flight
- Uses existing MovementTargeting logic to detect planets
- Maintains compatibility with existing orbital camera system

Parameters added:
- planetAttractionStrength: Controls magnetization strength
- planetRadiusTrigger: Distance at which magnetization begins
- flatMove flag: Controls space vs planetary movement constraints

The solution provides intuitive gameplay where players can land on planets, move around their surfaces, and leave when desired, creating a more natural space exploration experience.