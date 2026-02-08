public interface IIncomingDamageModifier
{
    // Return a multiplier for incoming damage. 1.0 = no change, 0.5 = 50% damage, etc.
    float GetIncomingDamageMultiplier();
}
