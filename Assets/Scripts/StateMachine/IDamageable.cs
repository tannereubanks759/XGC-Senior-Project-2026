/*
 * IDamageable.cs
 * 
 * This is an interface to call a function on another script without haveing a hard reference to that script (No GetComponent)
 * 
 * By: Matthew Bolger
 */
public interface IDamageable
{
    void TakeDamage(int amount);
}