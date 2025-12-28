using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Combat.Weapons
{
    public interface IDamageable
    {
        void TakeDamage(int damageAmount);
        void TakeDamageAtPoint(int damageAmount, Vector3 hitPoint); // ADD THIS
    }
}
