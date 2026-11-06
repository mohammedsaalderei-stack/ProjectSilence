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

    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentMP;
    [HideInInspector] public bool isGuarding;

    Animator animator;

    public bool IsDead => currentHP <= 0;

    void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        animator = GetComponentInChildren<Animator>();
    }

    public int TakeDamage(int amount)
    {
        if (isGuarding) amount /= 2;
        amount = Mathf.Max(1, amount);
        currentHP = Mathf.Max(0, currentHP - amount);
        if (IsDead) Die();
        return amount;
    }

    public int Heal(int amount)
    {
        int healed = Mathf.Min(amount, maxHP - currentHP);
        currentHP += healed;
        return healed;
    }

    public void PlayAttack()
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Attack");
    }

    void Die()
    {
        if (isPlayer) transform.Rotate(0f, 0f, 90f);
        else gameObject.SetActive(false);
    }
}
