#include "MouseRay.h"

void Mouse::MouseToWorld(GLFWwindow* window, Matrix44 view, Matrix44 projection, float width, float height)
{
	//system("CLS");
	double xpos, ypos;
	glfwGetCursorPos(window, &xpos, &ypos);
	xpos = ((xpos * 2.0f) / width) - 1;
	ypos = -((ypos * 2.0f) / height) + 1;
	//printf("Mouse Position\nX =  %f\nY =  %f", xpos, ypos);
	Matrix44 inverseView = view;
	inverseView.Invert();
	Matrix44 inverseProj = projection;
	inverseProj.Invert();
	pos = Vector3(float(xpos), float(ypos), -1.0f);
	pos = inverseProj * pos;
	pos = inverseView * pos;
	//printf("Mouse Position\nX =  %f\t%f\nY =  %f\t%f\nZ =  %f", mouse.x, xpos, mouse.y, ypos, mouse.z);
}

Vector3 Mouse::GetDir(Vector3 camera, float distance)
{
	Vector3 diff = (camera - pos);
	diff.Normalize();
	diff *= -10.0f;

	return camera + diff * distance;
}
