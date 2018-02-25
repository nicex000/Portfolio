using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Comparers;

namespace Assets.Scripts
{
    public class Box
    {
        public Vector2 min;
        public Vector2 max;
        //private LineRenderer r = new GameObject().AddComponent<LineRenderer>();
        public bool collideAABB(Box col)
        {
            return (min.x <= col.max.x && max.x >= col.min.x) &&
                   (min.y <= col.max.y && max.y >= col.min.y);
        }
        public bool collidePoint(Vector3 pos)
        {
            return (min.x <= pos.x && max.x >= pos.x) &&
                   (min.y <= pos.y && max.y >= pos.y);
        }

      /*  public void showColliders(GameObject gameObject)
        {
            Vector3[] lines = new Vector3[5];
            lines[0] = new Vector3(min.x, min.y, 0.0f);
            lines[4] = lines[0];
            lines[1] = new Vector3(min.x, max.y, 0.0f);
            lines[2] = new Vector3(max.x, max.y, 0.0f);
            lines[3] = new Vector3(max.x, min.y, 0.0f);
            r.useWorldSpace = false;
            r.enabled = true;
            r.SetVertexCount(5);
            r.SetColors(Color.magenta, Color.magenta);
            r.SetWidth(0.1f, 0.1f);
            r.SetPositions(lines);
        }*/
    }

    public class Circle : MonoBehaviour
    {
        public Vector2 center;
        public float r;
        private float distanceX;
        private float distanceY;
        private Vector2 tempCenter, oldCenter;
        private bool retrn;

        public bool collideCircle(Circle col)
        {
            distanceX = col.center.x - center.x;
            distanceY = col.center.y - center.y;

            return
                (Mathf.Sqrt(distanceX*distanceX +
                            distanceY * distanceY) 
                            < (r + col.r));
        }

        public bool collideAABB(Shape col)
        {
            oldCenter = col.cirCollider.center;
            tempCenter = center;
            if (tempCenter.x > col.AABB.max.x)
                tempCenter.x = col.AABB.max.x;
            if (tempCenter.y > col.AABB.max.y)
                tempCenter.y = col.AABB.max.y;
            if (tempCenter.x < col.AABB.min.x)
                tempCenter.x = col.AABB.min.x;
            if (tempCenter.y < col.AABB.min.y)
                tempCenter.y = col.AABB.min.y;

            col.cirCollider.center = tempCenter;

            retrn = collideCircle(col.cirCollider);
            col.cirCollider.center = oldCenter;
            return retrn;
        }
        public bool collideBox(Shape col)
        {
            oldCenter = col.cirCollider.center;
            tempCenter = center;

            if (tempCenter.x > col.collider.max.x)
                tempCenter.x = col.collider.max.x;
            if (tempCenter.y > col.collider.max.y)
                tempCenter.y = col.collider.max.y;
            if (tempCenter.x < col.collider.min.x)
                tempCenter.x = col.collider.min.x;
            if (tempCenter.y < col.collider.min.y)
                tempCenter.y = col.collider.min.y;

            col.cirCollider.center = tempCenter;

            retrn = collideCircle(col.cirCollider);
            col.cirCollider.center = oldCenter;
            return retrn;
        }
    }
    public class Shape
    {
        public Vector3 localPosition;
        public Vector3 worldPosition;
        public Vector3 scale;
        public Vector3 halfScale;
        public bool isCircle = false;
        public float radius;

        public Box collider = new Box();
        public Box AABB = new Box();
        private Vector3 vec, vec2;

        public Circle cirCollider = new Circle();

        public Shape(Vector3 localPos, Vector3 sca, float rad = 0.0f)
        {
            localPosition = localPos;
            scale = sca;
            halfScale = scale/2.0f;
            if (rad != 0)
                isCircle = true;

            radius = rad;
            cirCollider.r = radius;
        }

        public MassData calcMassData(Body_Material mat)
        {
            MassData massData;
            massData.mass = (scale.x* scale.y* scale.z) * mat.density;
            massData.inv_mass = 1/massData.mass;
            massData.inertia = 1.0f;
            massData.inv_inertia = 1.0f;
            return massData;
        }

        public void updateCol(Transform transf)
        {
            if(transf== null && isCircle)
                transf.position = Vector3.forward *1000;
            worldPosition = localPosition + transf.position;
            if (!isCircle)
            {
                collider.min = new Vector2(worldPosition.x - halfScale.x, worldPosition.y - halfScale.y);
                collider.max = new Vector2(collider.min.x + scale.x, collider.min.y + scale.y);

                AABB.min = worldPosition;
                AABB.max = worldPosition;
                for (int i = 0; i < 4; i++)
                {
                    switch (i)
                    {
                        case 0:
                            vec = transf.TransformPoint(-0.5f, -0.5f, 0.0f);
                            break;
                        case 1:
                            vec = transf.TransformPoint(0.5f, -0.5f, 0.0f);
                            break;
                        case 2:
                            vec = transf.TransformPoint(-0.5f, 0.5f, 0.0f);
                            break;
                        case 3:
                        default:
                            vec = transf.TransformPoint(0.5f, 0.5f, 0.0f);
                            break;
                    }
                    if (AABB.min.x > vec.x)
                        AABB.min.x = vec.x;
                    if (AABB.max.x < vec.x)
                        AABB.max.x = vec.x;
                    if (AABB.min.y > vec.y)
                        AABB.min.y = vec.y;
                    if (AABB.max.y < vec.y)
                        AABB.max.y = vec.y;
                }
            }

            cirCollider.center = new Vector2(worldPosition.x, worldPosition.y);
        }
    };

    public struct MassData
    {
        public float mass;
        public float inv_mass;
        public float inertia;
        public float inv_inertia;
    };

    public struct Body_Material
    {
        public float density;
        public float restitution;
        public float staticFriction;
        public float dynamicFriction;
    };

    public class Body : MonoBehaviour
    {
        public List<Shape> shape = new List<Shape>();
        public Transform transform;
        public float rotation;
        public float angAccel;
        public Body_Material material;
        public MassData totalMassData;
        public List<MassData> shapeMassData = new List<MassData>();
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 force;
        public float torque;
        public bool exists;
        public bool triggersScript;
        private Vector3 lastFrameAccel;

        public Body()
        {
            exists = false;
        }

        public Body(List<Shape> shapes, Transform transf, float rot, Body_Material mat, bool tS = false)
        {
            exists = true;
            shape = shapes;
            transform = transf;
            rotation = rot;
            material = mat;
            triggersScript = tS;

            velocity = new Vector3(0.0f, 0.0f, 0.0f);
            acceleration = new Vector3(0.0f, 0.0f, 0.0f);
            force = new Vector3(0.0f, 0.0f, 0.0f);
            torque = 0.0f;
            rotation = 0.0f;

            totalMassData.mass = 0.0f;
            totalMassData.inertia = 0.0f;

            for (int i = 0; i < shapes.Count; i++)
            {
                shape[i].updateCol(transform);
                shapeMassData.Add(shape[i].calcMassData(material));
                totalMassData.mass += shapeMassData[i].mass;
            }
            if (totalMassData.mass == 0.0f)
            {
                totalMassData.inv_mass = 0.0f;
                totalMassData.inertia = 0.0f;
            }
            else
            {
                totalMassData.inv_mass = 1 / totalMassData.mass;
                if (shape[0].isCircle)
                {
                    totalMassData.inertia = (totalMassData.mass*(shape[0].radius*shape[0].radius))*0.5f;
                }
                else
                {
                    totalMassData.inertia = (totalMassData.mass/12.0f)*
                                            ((shape[0].scale.x*shape[0].scale.x) + (shape[0].scale.y*shape[0].scale.y));
                }
            }
            if (totalMassData.inertia == 0.0f)
            {
                totalMassData.inv_inertia = 0.0f;
            }
            else
            {
                totalMassData.inv_inertia = 1/totalMassData.inertia;
            }
        }

        public void Integrate(float deltaT)
        {
            //make sure infinite mass is not itegrated
            if (totalMassData.inv_mass <= 0.0f)
            {
                for (int i = 0; i < shape.Count; i++)
                {
                    shape[i].updateCol(transform);
                }
                return;
            }
            Assert.IsTrue(deltaT > 0.0f, "last frame had no time");
            
            //update pos
            transform.position += velocity*deltaT;
            transform.Rotate(0.0f, 0.0f, rotation * deltaT);
            transform.position = new Vector3(transform.position.x, transform.position.y, 0.0f);
            for (int i = 0; i < shape.Count; i++)
            {
                shape[i].updateCol(transform);
            }
            //get acceleration from force
            Vector3 newAcc = acceleration;
            newAcc += force*totalMassData.inv_mass;
            float newAngAccel = angAccel;
            newAngAccel += torque*totalMassData.inv_inertia;
            //update velocity
            velocity += newAcc*deltaT;
            rotation += newAngAccel*deltaT;
            


            //clear forces
            ClearForces();
        }

        public void SimulateIntegration(float deltaT, Transform transf)
        {
            //make sure infinite mass is not itegrated
            if (totalMassData.inv_mass <= 0.0f)
            {
                return;
            }
            Assert.IsTrue(deltaT > 0.0f, "last frame had no time");
            //update pos
            transf.position += velocity * deltaT;
            transf.position = new Vector3(transf.position.x, transf.position.y, 0.0f);
            //get acceleration from force
            Vector3 newAcc = acceleration;
            newAcc += force * totalMassData.inv_mass;
            //update velocity
            velocity += newAcc * deltaT;

            //print(velocity);

            //clear forces
            ClearForces();
        }

        public void SetAcceleration(float x, float y)
        {
            acceleration.x = x;
            acceleration.y = y;
            acceleration.z = 0.0f;
        }

        public void SetVelocity(float x, float y)
        {
            velocity.x = x;
            velocity.y = y;
            velocity.z = 0.0f;
        }

        public void ClearForces()
        {
            force = new Vector3(0.0f, 0.0f, 0.0f);
            torque = 0.0f;
        }

        public void AddForce(Vector3 f)
        {
            force += f;
        }

        public void AddTorque(float t)
        {
            torque += t;
        }

        public void AddForceAtBodyPoint(Vector3 f, Vector3 p)
        {
            Vector3 sca = transform.localScale;
            transform.localScale = Vector3.one;
            p = transform.TransformPoint(p);
            transform.localScale = sca;
            AddForceAtPoint(f, p);
        }

        public void AddForceAtPoint(Vector3 f, Vector3 p)
        {
            p -= transform.position;
            force += f;
            torque += v3.v3Mult(p, f);
        }

        public bool HasFiniteMass()
        {
            return totalMassData.inv_mass > 0;
        }

    };
 
}
