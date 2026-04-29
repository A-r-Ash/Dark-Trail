using UnityEngine;

public class SmallCreature : CreatureBase
{
    // SmallCreature uses all default behavior from CreatureBase
    // It's afraid of light and flees when hit by flashlight

    // All settings are inherited and configurable in Inspector:
    // - Chase Speed
    // - Retreat Speed  
    // - Detection Range
    // - Kill Range
    // - Afraid Of Light (true by default)
    // - Flee Range
}