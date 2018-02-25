using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Assets.Scripts
{
    public class ForceGenerator
    {
        public virtual void updateForce(ref Body particle, float deltaT) { }
    }

    public class ForceGravity : ForceGenerator
    {
        private Vector3 gravity;

        public ForceGravity(Vector3 grav)
        {
            gravity = grav;
        }

        public override void updateForce(ref Body particle, float deltaT)
        {
            if (!particle.HasFiniteMass()) return;
            particle.AddForce(gravity * particle.totalMassData.mass);
        }
    }

    public class ForceGravitationalPull : ForceGenerator
    {
        private float gravity = 0.91f;

        public void updateForce(ref Body b1, ref Body b2, float deltaT)
        {
            float mass1 = 0.0f, mass2 = 0.0f;
            float totalMass;
            if (b1.HasFiniteMass()) mass1 = b1.totalMassData.mass;
            if (b2.HasFiniteMass()) mass2 = b2.totalMassData.mass;
            if (mass1 != 0)
            {
                if (mass2 != 0)
                {
                    totalMass = mass1*mass2;
                }
                else
                {
                    totalMass = mass1;
                }
            }
            else if(mass2 !=0)
            {
                totalMass = mass2;
            }
            else
            {
                return;
            }
            float force = gravity*totalMass/(b1.transform.position - b2.transform.position).sqrMagnitude;
            b1.AddForce(b2.transform.position * force);
            b2.AddForce(b1.transform.position * force);
        }
    }

    class ForceDrag : ForceGenerator
    {
        //velocity drag coefficient
        private float k1;
        //squared velocity drag coefficient
        private float k2;

        public ForceDrag(float v1, float v2)
        {
            k1 = v1;
            k2 = v2;
        }

        public override void updateForce(ref Body particle, float deltaT)
        {
            Vector3 force = particle.velocity;

            float dragCo = force.magnitude;
            dragCo = k1*dragCo + k2*dragCo*dragCo;

            force.Normalize();
            force *= -dragCo;
            particle.AddForce(force);
        }
    }

    public class ForceRegistry
    {
        public class ForceReg
        {
            public Body particle;
            public ForceGenerator generator;
        }

        private List<ForceReg> registrations = new List<ForceReg>();

        public void Add(ref Body particle, ref ForceGenerator gen)
        {
            ForceReg reg = new ForceReg();
            reg.particle = particle;
            reg.generator = gen;
            registrations.Add(reg);
        }

        public bool Remove(ref Body particle, ref ForceGenerator gen)
        {
            ForceReg reg = new ForceReg();
            reg.particle = particle;
            reg.generator = gen;
            bool success = false;
            for (int i = 0; i < registrations.Count; i++)
            {
                if (registrations[i].Equals(reg))
                {
                    registrations.RemoveAt(i);
                    i--;
                    success = true;
                }
            }
            return success;
        }

        public void Clear()
        {
            registrations.Clear();
        }

        public void UpdateForces(float deltaT)
        {
            for(int i=0; i< registrations.Count; i++)
            {
                registrations[i].generator.updateForce(ref registrations[i].particle, deltaT);
            }
        }
    }
}