using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class BaseAbility : MonoBehaviour
    {
        public string abilityName;
        public string abilityDescription;
        private Putter putter;

        public float jumpForce = 3f;
        public float cooldownTime = 3f;
        private float cooldownTimer = 0f;

        public bool IsOnCooldown { get; private set; } = false;

        // OPTIMIZATION: Cache para componentes
        private Rigidbody cachedRigidbody;

        public void ActivateAbility(Putter Putter)
        {
            Debug.Log("Ability activated! Preparing to execute...");
            putter = Putter;

            if (IsOnCooldown)
            {
                Debug.Log($"{abilityName} is on cooldown! Time left: {cooldownTimer:F2}s");
                return;
            }

            // OPTIMIZATION: Cache rigidbody en primera llamada
            if (cachedRigidbody == null)
                cachedRigidbody = putter.rb;

            Vector3 worldUp = Vector3.up;
            cachedRigidbody.linearVelocity = new Vector3(cachedRigidbody.linearVelocity.x, 0f, cachedRigidbody.linearVelocity.z);
            cachedRigidbody.AddForce(worldUp * jumpForce, ForceMode.VelocityChange);

            cooldownTimer = cooldownTime;
            IsOnCooldown = true;
            Debug.Log($"{abilityName} activated: Jumping!");
        }

        public void TickCooldown()
        {
            if (IsOnCooldown)
            {
                cooldownTimer -= Time.fixedDeltaTime;

                if (cooldownTimer <= 0f)
                {
                    ResetCooldown();
                }
            }
        }

        public void ResetCooldown()
        {
            cooldownTimer = 0f;
            IsOnCooldown = false;
            Debug.Log($"{abilityName} is ready to use again!");
        }
    }
}