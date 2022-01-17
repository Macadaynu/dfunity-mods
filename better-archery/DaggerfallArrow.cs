using BetterArcheryModMod;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Utility;
using UnityEngine;

namespace Assets.MacadaynuMods.BetterArchery
{
    public class DaggerfallArrow : MonoBehaviour
    {
        public GameObject target;
        bool impactDetected;
        GameObject ArrowMesh;
        DaggerfallAudioSource audioSource;
        bool isArrowSummoned;
        bool arrowFired;
        Vector3 direction;
        public float MovementSpeed = 25.0f;
        public float ColliderRadius = 0.45f;
        public float LifespanInSeconds = 8f;
        float lifespan = 0f;

        void Awake()
        {
            isArrowSummoned = GetComponent<DaggerfallMissile>().IsArrowSummoned;
            Destroy(GetComponent<DaggerfallMissile>());

            GetComponent<MeshCollider>().enabled = false;
            GetComponent<SphereCollider>().enabled = true;
            GetComponent<SphereCollider>().isTrigger = true;

            // Create and orient 3d arrow
            ArrowMesh = GameObjectHelper.CreateDaggerfallMeshGameObject(99800, transform, ignoreCollider: true);

            // Offset up so it comes from same place LOS check is done from
            Vector3 adjust;
            // Offset forward to avoid collision with player
            adjust = GameManager.Instance.MainCamera.transform.forward * 0.6f;
            // Adjust slightly downward to match bow animation
            adjust.y -= 0.11f;
            // Adjust to the right or left to match bow animation
            if (!GameManager.Instance.WeaponManager.ScreenWeapon.FlipHorizontal)
                adjust += GameManager.Instance.MainCamera.transform.right * 0.15f;
            else
                adjust -= GameManager.Instance.MainCamera.transform.right * 0.15f;

            ArrowMesh.transform.localPosition = adjust;
            ArrowMesh.transform.rotation = Quaternion.LookRotation(GameManager.Instance.MainCamera.transform.forward);
            ArrowMesh.layer = gameObject.layer;

            Physics.IgnoreCollision(GameManager.Instance.PlayerEntityBehaviour.GetComponent<Collider>(), this.GetComponent<SphereCollider>());

            audioSource = transform.GetComponent<DaggerfallAudioSource>();
        }

        void DoMissile()
        {
            direction = GameManager.Instance.MainCamera.transform.forward;
            transform.position = GameManager.Instance.MainCamera.transform.position + direction * ColliderRadius;
            arrowFired = true;
        }

        private void Update()
        {
            // Execute based on target type
            if (!arrowFired)
            {
                DoMissile();
            }

            if (!impactDetected)
            {
                // Transform missile along direction vector
                transform.position += (direction * MovementSpeed) * Time.deltaTime;

                lifespan += Time.deltaTime;
                if (lifespan > LifespanInSeconds)
                    Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<DaggerfallEntityBehaviour>() != null
                || other.gameObject == target)
            {
                // Missile collision should only happen once
                if (impactDetected)
                    return;

                impactDetected = true;

                AssignBowDamageToTarget(other);

                // Destroy arrow
                Destroy(gameObject);
            }
        }

        void AssignBowDamageToTarget(Collider arrowHitCollider)
        {
            ArrowMesh = transform.Find("DaggerfallMesh [ID=99800]").gameObject;

            Transform hitTransform = arrowHitCollider.gameObject.transform;

            DaggerfallEntityBehaviour entityBehaviour = hitTransform.GetComponent<DaggerfallEntityBehaviour>();

            int hp = 0;

            if (entityBehaviour)
            {
                hp = entityBehaviour.Entity.CurrentHealth;
            }

            var hitTarget = GameManager.Instance.WeaponManager.WeaponDamage(GameManager.Instance.WeaponManager.LastBowUsed, true, isArrowSummoned, hitTransform, hitTransform.position, ArrowMesh.transform.forward);

            if (hitTarget && entityBehaviour)
            {
                EnemyEntity enemyEntity = entityBehaviour.Entity as EnemyEntity;

                if (entityBehaviour.Entity.CurrentHealth > 0
                    && entityBehaviour.Entity.CurrentHealth == hp
                    && !enemyEntity.MobileEnemy.ParrySounds)
                {
                    // Play a 'miss' sound
                    DaggerfallAudioSource dfAudioSource = GameManager.Instance.PlayerActivate.GetComponent<DaggerfallAudioSource>();
                    if (dfAudioSource != null)
                    {
                        dfAudioSource.PlayOneShot(SoundClips.SwingLowPitch, volumeScale: 0.75f);
                    }
                }
            }

            //DaggerfallUI.AddHUDText($"Hit: {hitTarget}");
        }
    }
}
