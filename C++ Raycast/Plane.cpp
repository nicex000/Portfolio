#include "Plane.h"

void Plane::SetPlane(Vector3 pos, Vector3 norm)
{
	point = pos;
	normal = norm;
	d = normal.Dot(point);
}

void Plane::SetPlane(Vector3 pos, Vector3 norm, float yOffset)
{
	point = pos;
	point.y += yOffset;
	normal = norm;
	d = normal.Dot(point);
}

Vector3 Plane::Intersect(Vector3 mouse, Vector3 camera)
{
	top = d - camera.Dot(normal); // d - (planeNormal.x * cameraPos.x + planeNormal.y * cameraPos.y + planeNormal.z * cameraPos.z);
	bottom = normal.Dot(mouse - camera); // planeNormal.x * (mouse.x - cameraPos.x) + planeNormal.y * (mouse.y - cameraPos.y) + planeNormal.z * (mouse.z - cameraPos.z);
	t = top / bottom;

	intersectPoint = (1 - t)*camera + t*mouse; // Vector3((1-t) * cameraPos.x + t * mouse.x, (1-t) * cameraPos.y + t * mouse.y, (1-t) * cameraPos.z + t * mouse.z);
	return intersectPoint;
}