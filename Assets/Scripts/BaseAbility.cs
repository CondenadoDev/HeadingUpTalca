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
        public float cooldownTime = 3f; // Fixed cooldown duration
        private float cooldownTimer = 0f; // Timer that counts down

        public bool IsOnCooldown { get; private set; } = false;

        public void ActivateAbility(Putter Putter)
        {
            Debug.Log("Ability activated! Preparing to execute...");
            putter = Putter;

            if (IsOnCooldown)
            {
                Debug.Log($"{abilityName} is on cooldown! Time left: {cooldownTimer:F2}s");
                return;
            }

            var rb = putter.rb; // Get the rigid body of the putter
            Vector3 worldUp = Vector3.up; // To keep the direction relative to the world
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Reset vertical velocity
            rb.AddForce(worldUp * jumpForce, ForceMode.VelocityChange);

            cooldownTimer = cooldownTime; // Set cooldown
            IsOnCooldown = true;
            Debug.Log($"{abilityName} activated: Jumping!");
        }


        public void TickCooldown()
        {
            if (IsOnCooldown)
            {
                cooldownTimer -= Time.fixedDeltaTime; // Reduce cooldown over time

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



    /*public abstract class BaseAbility : MonoBehaviour
    {
        public string abilityName;
        public string abilityDescription;

        public abstract void ActivateAbility(GameObject user);
    }*/
}