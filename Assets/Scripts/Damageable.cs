using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum DamageType
{
    Default,
    Bullet,
    Explosive,
    Melee
}

public class Damage
{
    public float value;
    public Vector3? Origin;
    public DamageType? damageType;
    public GameObject? damageSource;

    public Damage(float v, Vector3? o, DamageType? t, GameObject? d)
    {
        value = v;
        Origin = o;
        damageType = t;
        damageSource = d;
    }
}

public class Damageable : MonoBehaviour
{
    public float HP;
    public UnityEvent OnDamageTaken;
    public bool IsDead { get; }

    public virtual void TakenDamage(Damage damage)
    {
        HP -= damage.value;
        OnDamageTaken.Invoke();
    }

    public float GetHP()
    {
        return HP;
    }
}
