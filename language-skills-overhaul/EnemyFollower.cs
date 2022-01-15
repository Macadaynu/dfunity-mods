using Assets.Scripts.Game.MacadaynuMods;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.MacadaynuMods
{
    public class EnemyFollower : MonoBehaviour
    {

        #region Properties

        EnemyMotor motor;
        public EnemyEntity entity;
        EnemySenses senses;
        MobileUnit mobile;
        CharacterController controller;
        Renderer renderer;

        public MobileTeams originalTeam;
        Vector3 destination;

        float originalHeight;

        bool obstacleDetected;
        bool fallDetected;
        bool foundUpwardSlope;
        bool foundDoor;

        public bool isFollowing;
        public bool hasChangedName;

        #endregion

        public void SetupEnemy(DaggerfallEntityBehaviour enemy)
        {
            motor = enemy.transform.GetComponent<EnemyMotor>();
            mobile = motor.transform.GetComponentInChildren<MobileUnit>();
            entity = enemy.Entity as EnemyEntity;
            senses = motor.transform.GetComponent<EnemySenses>();
            controller = motor.transform.GetComponent<CharacterController>();
            originalHeight = controller.height;
            renderer = enemy.transform.GetComponentInChildren<Renderer>();

            motor.IsHostile = false;
            isFollowing = true;
            originalTeam = entity.Team;
            entity.Team = MobileTeams.PlayerAlly;
        }

        private void FixedUpdate()
        {
            if (LanguageSkillsOverhaulMod.enemyFollowers.Any(x => x == gameObject) && motor != null && senses != null && !senses.TargetInSight && isFollowing)
            {
                FollowPlayer();
            }
        }

        void FollowPlayer()
        {
            // Move the enemy twice as fast as you walk so you don't lose them
            float moveSpeed = ((entity.Stats.LiveSpeed + PlayerSpeedChanger.dfWalkBase) * MeshReader.GlobalScale) * 1.5f;

            var targetTransform = GetTargetTransform();
            if (targetTransform == null)
            {
                Debug.LogError($"NO QUEUE POSITION FOUND FOR {transform.name}");
            }

            // Get location to move towards.
            GetDestination(targetTransform);

            // Get direction & distance to destination.
            Vector3 direction = (destination - motor.transform.position).normalized;
            var distance = (destination - motor.transform.position).magnitude;

            if (distance >= LanguageSkillsOverhaulMod.stopDistance)
            {
                AttemptMove(direction, moveSpeed, targetTransform);
            }
            else if (!senses.TargetIsWithinYawAngle(22.5f, destination))
            {
                TurnToTarget(direction);
            }
        }

        public static int IndexOf<T>(LinkedList<T> list, T item)
        {
            var count = 0;
            for (var node = list.First; node != null; node = node.Next, count++)
            {
                if (item.Equals(node.Value))
                    return count;
            }
            return -1;
        }

        void GetDestination(Transform targetTransform)
        {
            if (ClearPathToPosition(targetTransform.position, targetTransform.GetComponent<DaggerfallEntityBehaviour>()))
            {
                destination = targetTransform.position;
            }
            // teleport the follower behind the player, if the camera cant see the follower
            else if ((destination - motor.transform.position).magnitude > 30 || !renderer.isVisible)//((targetTransform.position - motor.transform.position).magnitude > teleDistance)
            {
                TeleportBehindPlayer(targetTransform);
            }
        }

        void AttemptMove(Vector3 direction, float moveSpeed, Transform targetTransform)
        {
            if (!senses.TargetIsWithinYawAngle(5.625f, destination))
            {
                TurnToTarget(direction);
            }

            // Move downward some to eliminate bouncing down inclines
            if (!CanFly() && !motor.IsLevitating && controller.isGrounded)
            {
                direction.y = -2f;
            }

            // Stop fliers from moving too near the floor during combat
            if (CanFly() && direction.y < 0 && motor.FindGroundPosition((originalHeight / 2) + 1f) != motor.transform.position)
            {
                direction.y = 0.1f;
            }

            Vector3 motion = direction * moveSpeed;

            // Check if there is something to collide with directly in movement direction, such as upward sloping ground.
            Vector3 direction2d = direction;

            if (!CanFly() && !motor.IsLevitating)
            {
                direction2d.y = 0;
            }

            ObstacleCheck(direction2d);
            FallCheck(direction2d);

            if (fallDetected || obstacleDetected)
            {
                motor.transform.position = targetTransform.position - targetTransform.forward;
            }
            else
            // Clear to move
            {
                controller.Move(motion * Time.deltaTime);
            }
        }

        void TurnToTarget(Vector3 targetDirection)
        {
            const float turnSpeed = 20f;

            motor.transform.forward = Vector3.RotateTowards(motor.transform.forward, targetDirection, turnSpeed * Mathf.Deg2Rad, 0.0f);            
        }

        bool ClearPathToPosition(Vector3 location, DaggerfallEntityBehaviour targetEntityBehaviour, float dist = 30)
        {
            Vector3 sphereCastDir = (location - motor.transform.position).normalized;
            Vector3 sphereCastDir2d = sphereCastDir;
            sphereCastDir2d.y = 0;
            ObstacleCheck(sphereCastDir2d);
            FallCheck(sphereCastDir2d);

            if (obstacleDetected || fallDetected)
                return false;

            RaycastHit hit;
            if (Physics.SphereCast(motor.transform.position, controller.radius / 2, sphereCastDir, out hit, dist, LanguageSkillsOverhaulMod.ignoreMaskForShooting))
            {
                DaggerfallEntityBehaviour hitTarget = hit.transform.GetComponent<DaggerfallEntityBehaviour>();
                return hitTarget == targetEntityBehaviour;
            }

            return true;
        }

        void FallCheck(Vector3 direction)
        {
            if (CanFly() || motor.IsLevitating || obstacleDetected || foundUpwardSlope || foundDoor)
            {
                fallDetected = false;
                return;
            }

            int checkDistance = 1;
            Vector3 rayOrigin = motor.transform.position;

            direction *= checkDistance;
            Ray ray = new Ray(rayOrigin + direction, Vector3.down);
            RaycastHit hit;

            fallDetected = !Physics.Raycast(ray, out hit, (originalHeight * 0.5f) + 1.5f);
        }

        bool CanFly()
        {
            return mobile.Enemy.Behaviour == MobileBehaviour.Flying || mobile.Enemy.Behaviour == MobileBehaviour.Spectral;
        }

        void ObstacleCheck(Vector3 direction)
        {
            obstacleDetected = false;
            float checkDistance = controller.radius / Mathf.Sqrt(2f);
            foundUpwardSlope = false;
            foundDoor = false;

            RaycastHit hit;
            // Climbable/not climbable step for the player seems to be at around a height of 0.65f. The player is 1.8f tall.
            // Using the same ratio to height as these values, set the capsule for the enemy. 
            Vector3 p1 = motor.transform.position + (Vector3.up * -originalHeight * 0.1388F);
            Vector3 p2 = p1 + (Vector3.up * Mathf.Min(originalHeight, LanguageSkillsOverhaulMod.doorCrouchingHeight) / 2);

            if (Physics.CapsuleCast(p1, p2, controller.radius / 2, direction, out hit, checkDistance, LanguageSkillsOverhaulMod.ignoreMaskForObstacles))
            {
                // Debug.DrawRay(transform.position, direction, Color.red, 2.0f);
                obstacleDetected = true;
                DaggerfallEntityBehaviour entityBehaviour2 = hit.transform.GetComponent<DaggerfallEntityBehaviour>();
                DaggerfallActionDoor door = hit.transform.GetComponent<DaggerfallActionDoor>();
                DaggerfallLoot loot = hit.transform.GetComponent<DaggerfallLoot>();

                if (entityBehaviour2)
                {
                    if (entityBehaviour2 == senses.Target)
                        obstacleDetected = false;
                }
                else if (door)
                {
                    obstacleDetected = false;
                    foundDoor = true;
                    if (senses.TargetIsWithinYawAngle(22.5f, door.transform.position))
                    {
                        senses.LastKnownDoor = door;
                        senses.DistanceToDoor = Vector3.Distance(motor.transform.position, door.transform.position);
                    }
                }
                else if (loot)
                {
                    obstacleDetected = false;
                }
                else if (!CanFly() && !motor.IsLevitating)
                {
                    // If an obstacle was hit, check for a climbable upward slope
                    Vector3 checkUp = motor.transform.position + direction;
                    checkUp.y++;

                    direction = (checkUp - motor.transform.position).normalized;
                    p1 = motor.transform.position + (Vector3.up * -originalHeight * 0.25f);
                    p2 = p1 + (Vector3.up * originalHeight * 0.75f);

                    if (!Physics.CapsuleCast(p1, p2, controller.radius / 2, direction, checkDistance))
                    {
                        obstacleDetected = false;
                        foundUpwardSlope = true;
                    }
                }
            }
        }

        public Transform GetTargetTransform()
        {
            var queuePosition = LanguageSkillsOverhaulMod.enemyFollowers.IndexOf(transform.gameObject);
            if (queuePosition == -1)
            {    
                return null;
            }
            else if (queuePosition == 0)
            {
                return GameManager.Instance.PlayerEntityBehaviour.transform;
            }
            else
            {
                return LanguageSkillsOverhaulMod.enemyFollowers.ElementAt(queuePosition - 1).transform;
            }
        }

        public void TeleportBehindPlayer(Transform targetTransform)
        {
            Vector3 positionToMoveTo = targetTransform.position - targetTransform.forward;

            RaycastHit floorHit;
            var ray = new Ray(positionToMoveTo, Vector3.down);
            var rayDown = Physics.Raycast(ray, out floorHit, 2f);

            // Ensure this is open space
            Vector3 testPoint = floorHit.point + Vector3.up * 1.25f;
            Collider[] colliders = Physics.OverlapSphere(testPoint, 0.65f);

            var openSpace = !colliders.Where(x => x.GetType() != typeof(CharacterController)).Any();

            if (rayDown && openSpace)
            {
                motor.transform.position = positionToMoveTo;
                destination = positionToMoveTo;
            }
        }
    }
}
