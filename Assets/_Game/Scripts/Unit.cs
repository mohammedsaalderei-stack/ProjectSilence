using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public bool isPlayer;
    public int maxHP = 100;
    public int maxMP = 0;
    public int attack = 10;
    public int defense = 5;
    public int speed = 10;

    [Header("Magic (added with AI help)")]
    [Tooltip("Spell power. Magic damage = magic x 2 - half the target's defense.")]
    public int magic = 12;
    [Tooltip("Colour of this unit's spells.")]
    public Color spellColor = new Color(1f, 0.8f, 0.3f);

    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentMP;
    [HideInInspector] public bool isGuarding;

    Animator animator;
    UnitMotion motion;   // Added with AI help: moves and animates this unit in battle (see UnitMotion.cs)

    public bool IsDead => currentHP <= 0;
    public UnitMotion Motion => motion;

    void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        animator = GetComponentInChildren<Animator>();

        // Added with AI help: use the UnitMotion on this object, or add one (so the scene needs no changes).
        motion = GetComponent<UnitMotion>();
        if (motion == null) motion = gameObject.AddComponent<UnitMotion>();
    }

    public int TakeDamage(int amount)
    {
        if (isGuarding) amount /= 2;
        amount = Mathf.Max(1, amount);
        currentHP = Mathf.Max(0, currentHP - amount);
        // Added with AI help: the number pops up and you hear the hit (a guarded hit sounds like a block).
        FloatingText.Show(motion.HeadPoint, amount.ToString(), isPlayer ? new Color(1f, 0.55f, 0.5f) : Color.white, isGuarding ? 0.9f : 1.15f);
        GameAudio.Play(isGuarding ? ProjectSilence.Synth.Block : ProjectSilence.Synth.Hit, 0.8f);
        if (IsDead) Die();
        else motion.PlayHit();   // Added with AI help: flinch when hit
        return amount;
    }

    public int Heal(int amount)
    {
        int healed = Mathf.Min(amount, maxHP - currentHP);
        currentHP += healed;
        FloatingText.Show(motion.HeadPoint, "+" + healed, new Color(0.5f, 1f, 0.55f));   // AI help
        GameAudio.Play(ProjectSilence.Synth.Heal, 0.7f);                                  // AI help
        return healed;
    }

    public void PlayAttack()
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Attack");
    }

    // Added with AI help: plays the "Cast" animation if the Animator has a Cast trigger,
    // otherwise the normal attack animation.
    public void PlayCast()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == "Cast")
            {
                animator.SetTrigger("Cast");
                return;
            }
        }
        animator.SetTrigger("Attack");
    }

    void Die()
    {
        // Changed with AI help. Before: heroes snapped onto their side and enemies vanished at once.
        // Now UnitMotion animates it: heroes fall over, enemies sink into the ground and vanish.
        motion.PlayDeath(isPlayer);
    }
}
