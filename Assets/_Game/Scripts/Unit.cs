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
    [Tooltip("Enemy spellcaster (Archcantor Vale): attacks with spells from where it stands.")]
    public bool castsSpells;
    [Tooltip("A caster hits the whole party every this many turns (0 = never).")]
    public int chordEvery;

    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentMP;
    [HideInInspector] public bool isGuarding;

    Animator animator;
    UnitMotion motion;

    public bool IsDead => currentHP <= 0;
    public UnitMotion Motion => motion;

    void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        animator = GetComponentInChildren<Animator>();

        motion = GetComponent<UnitMotion>();
        if (motion == null) motion = gameObject.AddComponent<UnitMotion>();
    }

    public int TakeDamage(int amount)
    {
        if (isGuarding) amount /= 2;
        amount = Mathf.Max(1, amount);
        currentHP = Mathf.Max(0, currentHP - amount);
        FloatingText.Show(motion.HeadPoint, amount.ToString(), isPlayer ? new Color(1f, 0.55f, 0.5f) : Color.white, isGuarding ? 0.9f : 1.15f);
        GameAudio.Play(isGuarding ? GameSounds.Slots.block : GameSounds.Slots.hit);
        if (IsDead) Die();
        else motion.PlayHit();
        return amount;
    }

    public int Heal(int amount)
    {
        int healed = Mathf.Min(amount, maxHP - currentHP);
        currentHP += healed;
        FloatingText.Show(motion.HeadPoint, "+" + healed, new Color(0.5f, 1f, 0.55f));
        GameAudio.Play(GameSounds.Slots.heal);
        return healed;
    }

    public void PlayAttack()
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Attack");
    }

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
        motion.PlayDeath(isPlayer);
    }
}
