using System.Collections.Generic;
using UnityEngine;

public class UnitWithGun : TargetingUtilityAI
{
    public BaseShooting weapon;

    public override void AttackUpdate(Transform target)
    {
        base.AttackUpdate(target);

        if (target.TryGetComponent(out Rigidbody body))
            weapon.transform.LookAt(weapon.PredictMovement(body));
        else
            weapon.transform.LookAt(target.position);

        weapon.Shoot(target.position);
    }

    protected override Tool ToolChosingCheck(Transform target)
    {
        return weapon;
    }

    public override Transform GetRightHandTarget()
    {
        return weapon.transform;
    }

    public override void GivePoints(int points)
    {
        base.GivePoints(points);

        int remaining = points;

        //TODO
    }
}