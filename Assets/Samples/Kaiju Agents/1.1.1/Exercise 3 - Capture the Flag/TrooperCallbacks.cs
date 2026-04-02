using System.Diagnostics.CodeAnalysis;

namespace KaijuSolutions.Agents.Exercises.CTF
{
    /// <summary>
    /// An action for a <see cref="TrooperOld"/>.
    /// </summary>
    /// <param name="trooper">The <see cref="TrooperOld"/>.</param>
    public delegate void TrooperAction([NotNull] TrooperOld trooperOld);
    
    /// <summary>
    /// An action for two <see cref="TrooperOld"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="TrooperOld"/>.</param>
    /// <param name="b">The second <see cref="TrooperOld"/>.</param>
    public delegate void MultiTrooperAction([NotNull] TrooperOld a, [NotNull] TrooperOld b);
    
    /// <summary>
    /// An action for a <see cref="HealthPickup"/>s.
    /// </summary>
    /// <param name="health">The <see cref="HealthPickup"/>.</param>
    public delegate void HealthAction([NotNull] HealthPickup health);
    
    /// <summary>
    /// An action for a <see cref="TrooperOld"/> and a <see cref="HealthPickup"/>s.
    /// </summary>
    /// <param name="trooper">The <see cref="TrooperOld"/>.</param>
    /// <param name="health">The <see cref="HealthPickup"/>.</param>
    public delegate void TrooperHealthAction([NotNull] TrooperOld trooperOld, [NotNull] HealthPickup health);
    
    /// <summary>
    /// An action for a <see cref="AmmoPickup"/>s.
    /// </summary>
    /// <param name="ammo">The <see cref="AmmoPickup"/>.</param>
    public delegate void AmmoAction([NotNull] AmmoPickup ammo);
    
    /// <summary>
    /// An action for a <see cref="TrooperOld"/> and a <see cref="AmmoPickup"/>s.
    /// </summary>
    /// <param name="trooper">The <see cref="TrooperOld"/>.</param>
    /// <param name="ammo">The <see cref="AmmoPickup"/>.</param>
    public delegate void TrooperAmmoAction([NotNull] TrooperOld trooperOld, [NotNull] AmmoPickup ammo);
    
    /// <summary>
    /// An action for a <see cref="FlagOld"/>s.
    /// </summary>
    /// <param name="flagOld">The <see cref="FlagOld"/>.</param>
    public delegate void FlagAction([NotNull] FlagOld flagOld);
    
    /// <summary>
    /// An action for a <see cref="TrooperOld"/> and a <see cref="FlagOld"/>s.
    /// </summary>
    /// <param name="trooper">The <see cref="TrooperOld"/>.</param>
    /// <param name="flagOld">The <see cref="FlagOld"/>.</param>
    public delegate void TrooperFlagAction([NotNull] TrooperOld trooperOld, [NotNull] FlagOld flagOld);
}