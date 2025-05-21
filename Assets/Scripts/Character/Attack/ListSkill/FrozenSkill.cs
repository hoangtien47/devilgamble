using MasterStylizedProjectile;
using System.Collections;
using System.Linq;
using UnityEngine;

public class FrozenSkill: BaseSkill
{
    public override void UseSkill()
    {
        base.UseSkill();
    }

    public void Shoot()
    {
        StartCoroutine(ShootIE());
    }
    public IEnumerator ShootIE()
    {
        yield return Charge();
        DoShoot();
    }
    public IEnumerator Charge()
    {
        if (CurEffect.ChargeParticles != null)
        {
            var ChargePar = Instantiate(CurEffect.ChargeParticles, StartNodeTrans.position, Quaternion.identity);
            //var onStart = gameObject.AddComponent<AudioTrigger>();
            //if (CurEffect.ChargeClip != null)
            //{
            //    onStart.onClip = CurEffect.startClip;
            //}


            if (CurEffect.ChargeClip != null)
            {
                GameObject AudioObj = new GameObject();
                var audiosource = AudioObj.AddComponent<AudioSource>();
                audiosource.clip = CurEffect.ChargeClip;
                audiosource.Play();
            }
            yield return new WaitForSeconds(CurEffect.ChargeParticleTime);
            Destroy(ChargePar.gameObject);
        }

    }
    public void DoShoot()
    {
        var targetPos = EndNodeTrans.position;
        var targetDir = targetPos - StartNodeTrans.position;
        targetDir = targetDir.normalized;
        if (CurEffect.StartParticles != null)
        {
            var StartPar = Instantiate(CurEffect.StartParticles, StartNodeTrans.position, Quaternion.identity);
            StartPar.transform.forward = targetDir;

            var onStart = StartPar.gameObject.AddComponent<AudioTrigger>();
            if (CurEffect.startClip != null)
            {
                onStart.onClip = CurEffect.startClip;
            }

        }
        if (CurEffect.BulletParticles != null)
        {
            var bulletObj = Instantiate(CurEffect.BulletParticles, StartNodeTrans.position, Quaternion.identity);
            bulletObj.transform.forward = targetDir;

            var bullet = bulletObj.gameObject.AddComponent<Bullet>();
            bullet.OnHitEffect = CurEffect.HitParticles;
            bullet.Speed = CurEffect.Speed;
            bullet.isTargeting = CurEffect.isTargeting;
            if (CurEffect.isTargeting)
            {
                var target = FindNearestTarget("Respawn");
                if (target != null)
                {
                    bullet.rotSpeed = CurEffect.RotSpeed;
                    bullet.target = target.transform;
                }
            }


            if (CurEffect.hitClip != null)
            {
                bullet.onHitClip = CurEffect.hitClip;
            }
            if (CurEffect.bulletClip != null)
            {
                bullet.bulletClip = CurEffect.bulletClip;
            }


            var collider = bulletObj.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.6f;
        }

    }

    public GameObject FindNearestTarget(string tag)
    {
        var gameObjects = GameObject.FindGameObjectsWithTag(tag).ToList().OrderBy(
            (x) => Vector3.Distance(transform.position, x.transform.position));
        return gameObjects.FirstOrDefault();
    }
}
