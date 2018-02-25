using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Assets.Scripts
{
    public class Collision_Narrow_Phase : MonoBehaviour
    {
        private void Thing()
        {}

        public float transfToAxis(NarrowBox transf, Vector3 axis)
        {
            return transf.halfSize.x*Mathf.Abs(v3.v3Mult(axis,(Vector3)(transf.body.transform.localToWorldMatrix.GetRow(0)))) +
                transf.halfSize.y * Mathf.Abs(v3.v3Mult(axis, (Vector3)(transf.body.transform.localToWorldMatrix.GetRow(1))));
        }


        public float axisPen(NarrowBox b1, NarrowBox b2, Vector3 axis, Vector3 toCenter)
        {
            float pJ1 = transfToAxis(b1, axis);
            float pJ2 = transfToAxis(b2, axis);

            float dist = Mathf.Abs(v3.v3Mult(toCenter, axis));

            //return overlap (positive value is overlap, negative is separation)


            return pJ1 + pJ2 - dist;
        }

        public bool tryAxis(NarrowBox b1, NarrowBox b2, Vector3 axis, Vector3 toCenter, int index, ref float smallestPen, ref int smallestCase)
        {
            if (axis.sqrMagnitude < 0.0001) return true;

            axis.Normalize();

            float pen = axisPen(b1, b2, axis, toCenter);

            if (pen < 0) return false;
            
            if (pen < smallestPen)
            {
                smallestPen = pen;
                smallestCase = index;
            }
            return true;
        }

        public int circleAndCircle(NarrowCircle c1, NarrowCircle c2, ref CollisionData data)
        {

            if (data.contactsLeft <= 0) return 0;

            Vector3 distance = c1.pos - c2.pos;
            float size = distance.magnitude;
           
            if (size <= 0.0f || size >= c1.radius + c2.radius) return 0;

            if (c1.body.triggersScript)
            {
                if(TriggerScript(c1, c2) == 0)
                    return 0;
                
            }

            if (c2.body.triggersScript)
            {
                if(TriggerScript(c2, c1) == 0)
                    return 0;
            }

            Vector3 normal = distance*(1.0f/size);
            Contact contact = new Contact();
            contact.contactNormal = normal;
            contact.contactPoint = c1.pos + distance*0.5f;
            contact.pen = (c1.radius + c2.radius - size);
            contact.SetBodyData(c1.body, c2.body, 0.0f, Mathf.Min(c1.body.material.restitution, c2.body.material.restitution));
            data.contacts.Add(contact);

            return 1;
        }

        public int circleAndBox(NarrowCircle c, NarrowBox b, ref CollisionData data)
        {
            Vector3 scale = b.body.transform.localScale;
            b.body.transform.localScale = Vector3.one;
            Vector3 relPos = b.body.transform.InverseTransformPoint(c.pos);

            if ((Mathf.Abs(relPos.x) - c.radius > b.halfSize.x) || (Mathf.Abs(relPos.y) - c.radius > b.halfSize.y))
            {
                b.body.transform.localScale = scale;
                return 0;
            }

            Vector3 closestPoint = Vector3.zero;
            float distance;

            //clamp each coordinate to the box
            distance = relPos.x;
            if (distance > b.halfSize.x) distance = b.halfSize.x;
            if (distance < -b.halfSize.x) distance = -b.halfSize.x;
            closestPoint.x = distance;

            distance = relPos.y;
            if (distance > b.halfSize.y) distance = b.halfSize.y;
            if (distance < -b.halfSize.y) distance = -b.halfSize.y;
            closestPoint.y = distance;
            //check if in contact
            distance = (closestPoint - relPos).sqrMagnitude;

            if (distance > c.radius*c.radius)
            {
                b.body.transform.localScale = scale;
                return 0;
            }

            if (b.body.triggersScript)
            {
                int i = TriggerScript(b, c);
                if (i == 0)
                    return 0;
                if (i == 2)
                {
                    b.body.transform.localScale = scale;
                    return 0;
                }
            }

            if (c.body.triggersScript)
            {
                int i = TriggerScript(c, b);
                if (i == 0)
                    return 0;
                if (i == 2)
                {
                    b.body.transform.localScale = scale;
                    return 0;
                }
            }

            //create the contact
            Vector3 pointW = b.body.transform.TransformPoint(closestPoint);
            Contact contact = new Contact();
            contact.contactNormal = -(pointW - c.pos);
            contact.contactNormal.Normalize();
            contact.contactPoint = pointW;
            contact.pen = c.radius - Mathf.Sqrt(distance);
            contact.SetBodyData(c.body, b.body, 0.0f, Mathf.Min(c.body.material.restitution, b.body.material.restitution));
            data.contacts.Add(contact);
            b.body.transform.localScale = scale;
            return 1;
        }

        public void pointFace(ref NarrowBox b1, ref NarrowBox b2, Vector3 toCenter, ref CollisionData data, int bes,
            float pen)
        {
            Contact contact = new Contact();
            Vector3 normal = (Vector3) (b1.body.transform.localToWorldMatrix.GetRow(bes));
            if (v3.v3Mult((Vector3) (b1.body.transform.localToWorldMatrix.GetRow(bes)), toCenter) > 0.0f)
            {
                normal = normal*-1.0f;
            }

            Vector3 vertex = b2.halfSize;
            if(v3.v3Mult((Vector3)(b2.body.transform.localToWorldMatrix.GetRow(0)), normal) < 0.0f)
                vertex.x = -vertex.x;
            if (v3.v3Mult((Vector3)(b2.body.transform.localToWorldMatrix.GetRow(1)), normal) < 0.0f)
                vertex.y = -vertex.y;

            contact.contactNormal = normal;
            contact.pen = pen;
            //TODO:contactPoint
            contact.SetBodyData(b1.body, b2.body, 0.0f, Mathf.Min(b1.body.material.restitution, b2.body.material.restitution));
            data.contacts.Add(contact);
        }
        public Vector3 contactPoint(
            Vector3 pOne,
            Vector3 dOne,
            float oneSize,
            Vector3 pTwo,
            Vector3 dTwo,
            float twoSize,
            bool useOne)
        {
            Vector3 toSt, cOne, cTwo;
                float dpStaOne, dpStaTwo, dpOneTwo, smOne, smTwo;
                float denom, mua, mub;

                smOne = dOne.sqrMagnitude;
            smTwo = dTwo.sqrMagnitude;
            dpOneTwo = v3.v3Mult(dTwo, dOne);

                toSt = pOne - pTwo;
            dpStaOne = v3.v3Mult(dOne, toSt);
            dpStaTwo = v3.v3Mult(dTwo, toSt);

            denom = smOne* smTwo - dpOneTwo* dpOneTwo;

            // Zero denominator indicates parrallel lines
            if (Mathf.Abs(denom) < 0.0001f) {
                return useOne? pOne:pTwo;
            }

            mua = (dpOneTwo* dpStaTwo - smTwo* dpStaOne) / denom;
            mub = (smOne* dpStaTwo - dpOneTwo* dpStaOne) / denom;

            // If either of the edges has the nearest point out
            // of bounds, then the edges aren't crossed, we have
            // an edge-face contact. Our point is on the edge, which
            // we know from the useOne parameter.
            if (mua > oneSize ||
                mua< -oneSize ||
                mub> twoSize ||
                mub< -twoSize)
            {
                return useOne? pOne:pTwo;
            }
            else
            {
                cOne = pOne + dOne* mua;
        cTwo = pTwo + dTwo* mub;

                return cOne* 0.5f + cTwo* 0.5f;
            }
        }
        public int boxAndBox(NarrowBox b1, NarrowBox b2, ref CollisionData data)
        {
            Vector3 toCenter = b2.pos - b1.pos;

            float pen = float.MaxValue;
            int bes = Int32.MaxValue;

            tryAxis(b1, b2, (Vector3) (b1.body.transform.localToWorldMatrix.GetRow(0)), toCenter, 0, ref pen, ref bes);
            tryAxis(b1, b2, (Vector3)(b1.body.transform.localToWorldMatrix.GetRow(1)), toCenter, 1, ref pen, ref bes);

            tryAxis(b1, b2, (Vector3)(b2.body.transform.localToWorldMatrix.GetRow(0)), toCenter, 2, ref pen, ref bes);
            tryAxis(b1, b2, (Vector3)(b2.body.transform.localToWorldMatrix.GetRow(1)), toCenter, 3, ref pen, ref bes);

            int besAxis = bes;

            tryAxis(b1, b2, v3.v3Prod((Vector3)(b1.body.transform.localToWorldMatrix.GetRow(0)), (Vector3)(b2.body.transform.localToWorldMatrix.GetRow(0))), toCenter, 6, ref pen, ref bes);
            tryAxis(b1, b2, v3.v3Prod((Vector3)(b1.body.transform.localToWorldMatrix.GetRow(0)), (Vector3)(b2.body.transform.localToWorldMatrix.GetRow(1))), toCenter, 7, ref pen, ref bes);
            tryAxis(b1, b2, v3.v3Prod((Vector3)(b1.body.transform.localToWorldMatrix.GetRow(1)), (Vector3)(b2.body.transform.localToWorldMatrix.GetRow(0))), toCenter, 9, ref pen, ref bes);
            tryAxis(b1, b2, v3.v3Prod((Vector3)(b1.body.transform.localToWorldMatrix.GetRow(1)), (Vector3)(b2.body.transform.localToWorldMatrix.GetRow(1))), toCenter, 10, ref pen, ref bes);

            Assert.IsTrue(bes != Int32.MaxValue);
            print(bes);

            if (bes < 2)
            {
                pointFace(ref b1, ref b2, toCenter, ref data, bes, pen);
                return 1;
            }
            else if (bes < 4)
            {
                pointFace(ref b1, ref b2, -toCenter, ref data, bes, pen);
                return 1;
            }
            else
            {
                bes -= 4;
                int axi1 = bes/3;
                int axi2 = bes%3;
                Vector3 ax1 = (Vector3) (b1.body.transform.localToWorldMatrix.GetRow(axi1));
                Vector3 ax2 = (Vector3) (b2.body.transform.localToWorldMatrix.GetRow(axi2));
                Vector3 axis = v3.v3Prod(ax1, ax2);
                axis.Normalize();

                if (v3.v3Mult(axis, toCenter) > 0.0f)
                {
                    axis = axis*-1.0f;
                }

                Vector3 pt1 = b1.halfSize;
                Vector3 pt2 = b2.halfSize;
                for (int i = 0; i < 3; i++)
                {
                    if (i == axi1) pt1[i] = 0.0f;
                    else if (v3.v3Mult((Vector3) (b1.body.transform.localToWorldMatrix.GetRow(i)), axis) > 0.0f)
                        pt1[i] = -pt1[i];

                    if (i == axi2) pt2[i] = 0.0f;
                    else if (v3.v3Mult((Vector3)(b2.body.transform.localToWorldMatrix.GetRow(i)), axis) < 0.0f)
                        pt2[i] = -pt2[i];
                }

                Matrix4x4 m = b1.body.transform.localToWorldMatrix;
                pt1 = new Vector3(pt1.x* m[0] + pt1.y * m[1] + pt1.z * m[2] + m[3], 
                    pt1.x * m[4] + pt1.y * m[5] + pt1.z * m[6] + m[7], 
                    pt1.x * m[8] + pt1.y * m[9] + pt1.z * m[10] + m[11]);

                m = b2.body.transform.localToWorldMatrix;
                pt1 = new Vector3(pt2.x * m[0] + pt2.y * m[1] + pt2.z * m[2] + m[3],
                    pt2.x * m[4] + pt2.y * m[5] + pt2.z * m[6] + m[7],
                    pt2.x * m[8] + pt2.y * m[9] + pt2.z * m[10] + m[11]);

                Vector3 vertex = contactPoint(pt1, ax1, b1.halfSize[axi1], pt2, ax2, b2.halfSize[axi2], besAxis > 2);

                Contact contact = new Contact();

                contact.pen = pen;
                contact.contactNormal = axis;
                //TODO RIGIDBODY
                contact.SetBodyData(b1.body, b2.body, 0.0f, Mathf.Min(b1.body.material.restitution, b2.body.material.restitution));
                data.contacts.Add(contact);
                return 1;
            }
        }

        public int TriggerScript(Primitive trigger, Primitive other)
        {
            if (trigger.body.transform.gameObject.GetComponent<Object_Trigger>())
            {
                return trigger.body.transform.gameObject.GetComponent<Object_Trigger>().StartTrigger(trigger, other);
            }
            return 1;
        }
    }

   public class CollisionData
    {
        public List<Contact> contacts;
        public int contactsLeft;

       public CollisionData(ref List<Contact> cs, int left)
       {
           contacts = cs;
           contactsLeft = left;
       }

       public void setToWorld(ref List<Contact> cs)
       {
           cs = contacts;
       }
    }

    public class Primitive
    {
        public Body body;
        public Vector3 pos;

       
    }

    public class NarrowBox : Primitive
    {
        public Vector3 halfSize;

        public NarrowBox(Body b)
        {
            body = b;
            halfSize = b.shape[0].halfScale;
            pos = body.shape[0].cirCollider.center;
        }
    }

    public class NarrowCircle : Primitive
    {
        public float radius;

        public NarrowCircle(Body b)
        {
            body = b;
            pos = body.shape[0].cirCollider.center;
            radius = body.shape[0].cirCollider.r;
        }
    }

}

