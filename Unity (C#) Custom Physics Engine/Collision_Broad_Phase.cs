using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace Assets.Scripts
{
    public class Collision_Broad_Phase
    {
        Collision_Narrow_Phase np = new Collision_Narrow_Phase();
        private CollisionData data = null;
        List<Body> bodies = new List<Body>(); 
        public void BroadPhase(ref List<Body> bs, ref List<Contact> contacts)
        {
            data = new CollisionData(ref contacts, 1000);
            bodies = bs;
            int i = 0, j;
            bool[][] pairs = new bool[bodies.Count][];
            for (; i < bodies.Count; i++)
            {
                pairs[i] = new bool[bodies.Count];
                for (j = 0; j < bodies.Count; j++)
                {
                    pairs[i][j] = false;
                }
            }
            for (i=0; i < bodies.Count; i++)
            {
                for (j=0; j < bodies.Count; j++)
                {
                    if (!pairs[i][j] && !pairs[j][i] && i!=j)
                    {
                        pairs[i][j] = pairs[j][i] = true;
                        if (bodies[i].shape[0].isCircle)
                        {
                            if (bodies[j].shape[0].isCircle)
                            {
                                if (bodies[i].shape[0].cirCollider.collideCircle(bodies[j].shape[0].cirCollider))
                                    PassCC(i, j);
                            }
                            else
                            {
                                if (bodies[i].shape[0].cirCollider.collideAABB(bodies[j].shape[0]))
                                    PassCB(i, j);
                            }
                        }
                        else
                        {
                            if (bodies[j].shape[0].isCircle)
                            {
                                if (bodies[j].shape[0].cirCollider.collideAABB(bodies[i].shape[0]))
                                    PassCB(j, i);
                            }
                        }
                    }
                }
            }
           


           
        }

        public void PassCC(int i, int j)
        {
            NarrowCircle c1 = new NarrowCircle(bodies[i]);
            NarrowCircle c2 = new NarrowCircle(bodies[j]);
            //it will return 1 if it actually generates a contact
            np.circleAndCircle(c1, c2, ref data);
            // data.setToWorld(ref contacts);
        }
        public void PassCB(int i, int j)
        {
            NarrowCircle c = new NarrowCircle(bodies[i]);
            NarrowBox b = new NarrowBox(bodies[j]);
            //it will return 1 if it actually generates a contact
            np.circleAndBox(c, b, ref data);
            // data.setToWorld(ref contacts);
        }
        public void PassBB(int i, int j)
        {
            NarrowBox b1 = new NarrowBox(bodies[i]);
            NarrowBox b2 = new NarrowBox(bodies[j]);
            //it will return 1 if it actually generates a contact
            np.boxAndBox(b1, b2, ref data);
            // data.setToWorld(ref contacts);
        }
    }
}