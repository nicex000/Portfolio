using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Proxies;
using JetBrains.Annotations;

namespace Assets.Scripts
{
    public class v3
    {
        public static float v3Mult(Vector3 v1, Vector3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static Vector3 v3Prod(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.y*v2.z - v1.z*v2.y,
                v1.z*v2.x - v1.x*v2.z,
                v1.x*v2.y - v1.y*v2.x);
        }

        public static Vector3 Perpendicular(Vector3 v)
        {
            return new Vector3(-v.y, v.x, v.z);
        }
    }
    public class Contact
    {
        public Body[] particle =
            {new Body(), new Body()};

        public float restitution;

        public float friction;

        public Vector3 contactNormal;

        public Vector3 contactPoint;

        public float pen;

        public Vector3[] particleMov = new Vector3[2];

        public void resolve(float deltaT)
        {
            resolveInterPen(deltaT);
            resolveVelocity(deltaT);
        }

        public float calculateSepVelocity(Vector3 aRelPos, Vector3 bRelPos)
        {
            Vector3 relVelocity = particle[0].velocity + Mathf.Deg2Rad * particle[0].rotation * aRelPos;
            if (particle[1].exists) relVelocity -= particle[1].velocity + Mathf.Deg2Rad * particle[1].rotation * bRelPos;

            return v3.v3Mult(relVelocity, contactNormal);
        }

        public float calculateSepVelocity()
        {
            Vector3 relVelocity = particle[0].velocity;
            if (particle[1].exists) relVelocity -= particle[1].velocity;

            return v3.v3Mult(relVelocity, contactNormal);
        }

        private void resolveVelocity(float deltaT)
        {
            Vector3 aRelPos = v3.Perpendicular(contactPoint - particle[0].transform.position);
            Vector3 bRelPos = v3.Perpendicular(contactPoint - particle[1].transform.position);
            //find velocity in direction of contact
            float sepVelocity = calculateSepVelocity(aRelPos, bRelPos);

            //if they are separating then return
            if(sepVelocity>0) return;

            //calculate new separating velocity
            float newSepVel = -sepVelocity*restitution;

            //this part is used for microcollisions. it will use only the relative velocity and not the absolute one
            //check the velocity build-up due to acceleration only
            //TODO it seems to not work
            Vector3 accelCausedVel = particle[0].acceleration;
            if (particle[1].exists) accelCausedVel -= particle[1].acceleration;
            float accelCausedSepVel = v3.v3Mult(accelCausedVel, contactNormal)*deltaT;
            //check if velocity is closing due to accel build up
            if (accelCausedSepVel <0)
            {
                //if so remove it
                newSepVel += restitution*accelCausedSepVel;

                //make sure it didnt remove too much
                if (newSepVel < 0) newSepVel = 0;
            }
            float deltaVel = newSepVel - sepVelocity;

            //get inverse mass
            float invMass = particle[0].totalMassData.inv_mass;
            if (particle[1].exists) invMass += particle[1].totalMassData.inv_mass;

            //if all (both) have infinite mass, exit
            if (invMass <= 0) return;

            //calculate impulse
            float den = invMass + Mathf.Pow(v3.v3Mult(aRelPos, contactNormal), 2)*particle[0].totalMassData.inv_inertia *
                        Mathf.Pow(v3.v3Mult(bRelPos, contactNormal), 2)*particle[1].totalMassData.inv_inertia;
            float impulse = deltaVel/den;

            //find impulse per unit of inv mass
            Vector3 impPerInvMass = contactNormal*impulse;

            
            //apply impulse: the impulse is applied in the direction of the contact, and is proportional to the inverse mass
            particle[0].velocity = particle[0].velocity + impPerInvMass * particle[0].totalMassData.inv_mass;
            if (particle[1].exists)
                particle[1].velocity = particle[1].velocity + impPerInvMass * -particle[1].totalMassData.inv_mass;

            particle[0].rotation = particle[0].rotation + Mathf.Rad2Deg * v3.v3Mult(aRelPos,impPerInvMass)*particle[0].totalMassData.inv_inertia;
            if (particle[1].exists)
                particle[1].rotation = particle[1].rotation + Mathf.Rad2Deg * v3.v3Mult(bRelPos, impPerInvMass) * -particle[1].totalMassData.inv_inertia;





            Vector3 relVel = particle[1].velocity - particle[0].velocity;

            Vector3 tangent = (relVel - calculateSepVelocity(aRelPos, bRelPos) * contactNormal).normalized;


            float jt = -v3.v3Mult(relVel, tangent);

            den = invMass + Mathf.Pow(v3.v3Mult(aRelPos, tangent), 2) * particle[0].totalMassData.inv_inertia *
                        Mathf.Pow(v3.v3Mult(bRelPos, tangent), 2) * particle[1].totalMassData.inv_inertia;

            jt = jt / den;

            float mu =
                Mathf.Sqrt(Mathf.Pow(particle[0].material.staticFriction, 2) +
                           Mathf.Pow(particle[1].material.staticFriction, 2));

            Vector3 frictionImpulse;

            if (Mathf.Abs(jt) < impulse*mu)
            {
                frictionImpulse = jt*tangent;
                
            }
            else
            {
                mu = Mathf.Sqrt(Mathf.Pow(particle[0].material.dynamicFriction, 2) +
                           Mathf.Pow(particle[1].material.dynamicFriction, 2));
                frictionImpulse = -impulse * tangent * mu;
            }

            particle[0].velocity = particle[0].velocity + frictionImpulse * -particle[0].totalMassData.inv_mass;
            if (particle[1].exists)
                particle[1].velocity = particle[1].velocity + frictionImpulse * particle[1].totalMassData.inv_mass;

            particle[0].rotation = particle[0].rotation + Mathf.Rad2Deg * v3.v3Mult(aRelPos, frictionImpulse) * -particle[0].totalMassData.inv_inertia;
            if (particle[1].exists)
                particle[1].rotation = particle[1].rotation + Mathf.Rad2Deg * v3.v3Mult(bRelPos, frictionImpulse) * particle[1].totalMassData.inv_inertia;


            if (particle[0].velocity == Vector3.zero) particle[0].rotation *= 0.85f;
            if (Mathf.Abs(particle[0].rotation) < 0.1f) particle[0].rotation = 0.0f;

        }

        //this is what brian was saying about moving the earth
        private void resolveInterPen(float deltaT)
        {
            //if there is no penetration between the two objects, exit
            if (pen <= 0) return;

            //get inverse mass
            float invMass = particle[0].totalMassData.inv_mass;
            if (particle[1].exists) invMass += particle[1].totalMassData.inv_mass;

            //if all (both) have infinite mass, exit
            if (invMass <= 0) return;

            //find penetration per unit of inv mass
            Vector3 penPerInvMass = contactNormal*(pen/invMass);

            //calculate the amount of movement
            particleMov[0] = penPerInvMass*particle[0].totalMassData.inv_mass;
            if (particle[1].exists)
            {
                particleMov[1] = penPerInvMass*-particle[1].totalMassData.inv_mass;
            }
            else
            {
                particleMov[1] = Vector3.zero;
            }

            //apply penetration resolution
            particle[0].transform.position += particleMov[0];
            if (particle[1].exists)
                particle[1].transform.position += particleMov[1];
        }

        public void SetBodyData(Body b1, Body b2, float fric, float rest)
        {
            particle[0] = b1;
            particle[1] = b2;
            friction = fric;
            restitution = rest;
        }
    }

    public class ContactResolver
    {
        protected int iterations;

        protected int usedIterations;

        public ContactResolver(int iters)
        {
            iterations = iters;
        }

        public void setIterations(int iters)
        {
            iterations = iters;
        }

        public void resolveContacts(ref List<Contact> contactArray, int numContacts, float deltaT)
        {
            int i;
            usedIterations = 0;
            while (usedIterations < iterations)
            {
                //find the contact with the largest closing velocity, maxI will stay at numContacts if there is nothing worth resolving
                float max = float.MaxValue;
                int maxI = numContacts;
                for(i = 0; i < numContacts; i++)
                {
                    float sepVal = contactArray[i].calculateSepVelocity();
                    if (sepVal < max && (sepVal < 0 || contactArray[i].pen > 0))
                    {
                        max = sepVal;
                        maxI = i;
                    }

                }

                //checks if the maxI is still numContacts, if so exit
                if (maxI == numContacts) return;

                //resolve the contact
                contactArray[maxI].resolve(deltaT);

                //TODO: update interpenetration for all particles


                usedIterations++;
            }
        }
    }
} 
