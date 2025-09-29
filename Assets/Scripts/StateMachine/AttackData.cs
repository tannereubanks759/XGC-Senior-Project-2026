using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Enemy/AttackData")]
public class AttackData : ScriptableObject
{
    [Tooltip("The name of the attack")]
    public string attackName;           // For reference/debugging
    [Tooltip("The attack animation")]
    public AnimationClip attackClip;    // Reference to the animation
    [Tooltip("Can the enemy move during the attack")]
    public bool canMoveDuringAttack;
    [Tooltip("Can the enemy move backwards during the attack")]
    public bool movesBackward;
    [Tooltip("The amount of damage that this attack does")]
    public int damage;
}
