#include "BattleShips.h"
vector <int> BattleShips::keyDownList = vector<int>();
DebugRenderer* dr = new DebugRenderer;

static void error_callback(int error, const char* description)
{
	fputs(description, stderr);
}

void BattleShips::key_callback(GLFWwindow* window, int key, int scancode, int action, int mods)
{
	if (key == GLFW_KEY_ESCAPE && action == GLFW_PRESS)
	{
		glfwSetWindowShouldClose(window, GL_TRUE);
	}
	else
	{
		keyEvent ev = {
			key, scancode, action, mods
		};
		BattleShips::EventManager(ev);
	}
}

void BattleShips::cursor_position_callback(GLFWwindow* window, double xpos, double ypos)
{
}

void BattleShips::mouse_button_callback(GLFWwindow* window, int button, int action, int mods)
{
	BattleShips::key_callback(window, button, 0, action, mods);
}

void BattleShips::EnableWireframe()
{
	glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
}

void BattleShips::DisableWireframe()
{
	glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
}

void BattleShips::MainBB()
{
	connect = Connect();
	connect.Init();

	glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);		 // yes, 3 and 2!!!
	glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 2);
	glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE); // But also 4 if present
	glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

	GLFWwindow* window;
	glfwSetErrorCallback(error_callback);
	if (!glfwInit())
		exit(EXIT_FAILURE);
	window = glfwCreateWindow(1280, 720, "BattleShips++    -    Itz_Crate.EXE", nullptr, nullptr);

	int major = glfwGetWindowAttrib(window, GLFW_CONTEXT_VERSION_MAJOR);
	int minor = glfwGetWindowAttrib(window, GLFW_CONTEXT_VERSION_MINOR);
	int revision = glfwGetWindowAttrib(window, GLFW_CONTEXT_REVISION);
	cout << "OpenGL Version " << major << "." << minor << "." << revision << endl;

	if (!window)
	{
		glfwTerminate();
		exit(EXIT_FAILURE);
	}
	glfwMakeContextCurrent(window);
	glfwSwapInterval(1);
	glfwSetKeyCallback(window, key_callback);
	glfwSetCursorPosCallback(window, cursor_position_callback);
	glfwSetMouseButtonCallback(window, mouse_button_callback);

	if (!gladLoadGLLoader(GLADloadproc(glfwGetProcAddress)))
	{
		cout << "Failed to initialize OpenGL context" << endl;
		//return -1;
	}

	glfwGetFramebufferSize(window, &width, &height);

	
	dr->Initialize();
	Load();
	cameraPos = Vector3(2.0f, 20.0f, 15.0f);
	cameraTargetPos = Vector3(cameraPos.x, 0.0f, cameraPos.z - 5.0f);
	for (int i = 0; i < 100; i++)
	{
		lineWaveOffset[i] = sin(DegToRad(i * 3.6f));
	}
	boardsOffsetX = 11.0f;
	boardOffsetX = 5.0f;
	boardOffsetZ = 0.0f;
	enemyBoardOffsetZ = 2.0f;
	enemyBoardOffsetY = 0.0f;
	combat_stage = Matrix44::CreateTranslation(8.0f, 5.0f, 16.0f) * Matrix44::CreateRotateY(Pi);

	for (int i = 0; i < 5; i++)
	{
		ships[i] = { EMPTY, 0, 0, 0 };
		templateShip[i] = { static_cast<Board>(static_cast<int>(DD) + i), 0, 0, 1 };
	}
	shipStage = DD;
	grabbed = EMPTY;
	shipsOnHold = false;
	shipPlaceError = 0.0f;

	float startTime = (float)glfwGetTime();
	transition = 0.0f;
	while (!glfwWindowShouldClose(window))
	{
		deltaT = (float)glfwGetTime() - startTime;
		startTime = (float)glfwGetTime();
		glViewport(0, 0, width, height);
		glClearColor(1.0, 1.0, 1.0, 1.0);
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

		connect.Receive();
		mouse.MouseToWorld(window, view, projection, (float)width, (float)height);
		EventManager();
		Update();
		Render();
		
		glfwSwapBuffers(window);
		glfwPollEvents();
	}
}

BattleShips::~BattleShips()
{
	Close();
}

void BattleShips::Load()
{
	
	shader = new Shader("./Assets/Basic.vsh", "./Assets/Basic.fsh");
	waterShader = new Shader("./Assets/Water.vsh", "./Assets/Water.fsh");
	water_texture = new Texture("./Assets/water.jpg", GL_REPEAT, GL_REPEAT);
	empty_texture = new Texture("./Assets/empty.jpg");
	ship_texture = new Texture("./Assets/ship.jpg");
	selected_texture = new Texture("./Assets/selected.jpg");
	sip_texture = new Texture("./Assets/sip.jpg");
	template_texture = new Texture("./Assets/template.jpg");
	template_used_texture = new Texture("./Assets/template used.jpg");
	combat_enemy_texture = new Texture("./Assets/Combat_enemy.jpg");
	combat_friendly_texture = new Texture("./Assets/Combat_friendly.jpg");
	renderer = new Renderer(shader);
	waterRenderer = new Renderer(waterShader);

	diffuse = Color("CCCCCC");
	diffuseError = Color("DD0505");

	float ratio = float(width) / float(height);
	projection = Matrix44::CreatePerspective(DegToRad(60), ratio, 0.01f, 5000.0f);

	crate_mesh = new Mesh("./Assets/box.obj");
	water.GenerateWater();
}

void BattleShips::EventManager(keyEvent keyEV)
{
	if(keyEV.key == GLFW_KEY_Q)
	{
		if (keyEV.action == GLFW_PRESS)
		{
			EnableWireframe();
		}
		else if (keyEV.action == GLFW_RELEASE)
		{
			DisableWireframe();
		}
	}
	else if(keyEV.action == GLFW_PRESS)
	{
		keyDownList.push_back(keyEV.key);
	}
	else if(keyEV.action == GLFW_RELEASE)
	{
		
		for (int i = 0; i < keyDownList.size(); i++)
		{
			if(keyDownList[i] == keyEV.key)
			{
				keyDownList.erase(keyDownList.begin() + i);
				i--;
			}
		}
		if (keyEV.key == GLFW_MOUSE_BUTTON_1) keyDownList.push_back(keyEV.key + 1000);
	}
}


void BattleShips::EventManager()
{
	for (int i = 0; i < keyDownList.size(); i++)
	{
		
		switch (keyDownList[i])
		{
		default: break;
		case GLFW_KEY_A:
		case GLFW_KEY_LEFT:
			selectedX--;

			if(selectedX < 0)
			{
				selectedX = 19;
			}
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			rotation = 0.0f;
			break;
		case GLFW_KEY_D:
		case GLFW_KEY_RIGHT:
			selectedX++;

			if (selectedX > 19)
			{
				selectedX = 0;
			}
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			rotation = 0.0f;
			break;
		case GLFW_KEY_W:
		case GLFW_KEY_UP:
			selectedY--;

			if (selectedY < 0)
			{
				selectedY = 9;
			}
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			rotation = 0.0f;
			break;
		case GLFW_KEY_S:
		case GLFW_KEY_DOWN:
			selectedY++;

			if (selectedY > 9)
			{
				selectedY = 0;
			}
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			rotation = 0.0f;
			break;
		case GLFW_KEY_SPACE:
		case GLFW_KEY_ENTER:
			shipPlacement.PlaceShip();
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			break;
		case GLFW_MOUSE_BUTTON_1:
			MouseLeftClick(i);
			break;
		case GLFW_MOUSE_BUTTON_2:
			if(grabbed >= DD)
			{
				templateShip[static_cast<int>(grabbed) - static_cast<int>(DD)].dir++;
				if(templateShip[static_cast<int>(grabbed) - static_cast<int>(DD)].dir > 3)
				{
					templateShip[static_cast<int>(grabbed) - static_cast<int>(DD)].dir = 0;
				}
			}
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			break;
		case GLFW_MOUSE_BUTTON_1 + 1000:
			if(grabbed != EMPTY)
			{
				shipPlacement.ReleaseShip();
			}
			keyDownList.erase(keyDownList.begin() + i);
			i--;
			break;

		}
	}
}

void BattleShips::MouseLeftClick(int i)
{
	bool inGrid = false;
	if (grabbed == EMPTY)
	{
		for (int k = 0; k < 20; k++)
		{
			for (int j = 0; j < 10; j++)
			{
				//ray/plane intersection
				Vector3 pos;
				if(connect.gameStage < TRANSITION_START)
				{
					pos = boardM[k][j].GetTranslation();
				}
				else
				{
					pos = enemyBoardM[k][j].GetTranslation();
				}
				plane.SetPlane(pos, Vector3(0.0f, 1.0f, 0.0f), 1.0f);
				intersectPoint = plane.Intersect(mouse.pos, cameraPos);
				dr->AddLine(cameraPos, mouse.pos);
				if (pos.x - 1.0f < intersectPoint.x && pos.x + 1.0f > intersectPoint.x && pos.z - 1.0f < intersectPoint.z && pos.z + 1.0f > intersectPoint.z)
				{
					selectedX = k;
					selectedY = j;
					if (connect.gameStage < TRANSITION_START)
					{
						shipPlacement.PlaceShip();
					}
					else
					{
						if(connect.gameStage == SHOOT)
						{
							connect.Send(EMessages_SendShoot, selectedX, selectedY);
						}
						else
						{
							//error here
							shipPlaceError += deltaT;
						}
					}
					inGrid = true;
					keyDownList.erase(keyDownList.begin() + i);
					i--;
					return;
				}
			}
		}
	}
	if (!inGrid && connect.gameStage < TRANSITION_START)
	{
		for (int k = 0; k < 5; k++)
		{
			if (ships[k].ship == EMPTY)
			{
				int maxDistance = 0;
				switch (k)
				{
				case 0: maxDistance = 2;
					break;
				case 1: maxDistance = 3;
					break;
				case 2: maxDistance = 3;
					break;
				case 3: maxDistance = 4;
					break;
				case 4: maxDistance = 5;
					break;
				default: break;
				}

				for (int j = 0; j < maxDistance; j++)
				{
					//ray/plane intersection
					Vector3 pos = templateShipsM[k][j].GetTranslation();
					plane.SetPlane(pos, Vector3(0.0f, 1.0f, 0.0f), 0.7f);
					float y = intersectPoint.y;
					intersectPoint = plane.Intersect(mouse.pos, cameraPos);
					intersectPoint.y = y;
					if ((pos.x - 0.7f < intersectPoint.x && pos.x + 0.7f > intersectPoint.x && pos.z - 0.7f < intersectPoint.z && pos.z + 0.7f > intersectPoint.z && grabbed == EMPTY) || grabbed == static_cast<Board>(k + static_cast<int>(DD)))
					{
						//dr->AddSphere(intersectPoint, 1.0f);
						shipPlacement.GrabShip(static_cast<Board>(k + static_cast<int>(DD)));
					}
				}
			}
		}
	}
}

//crate is love crate is life
void BattleShips::Update()
{
	water.MoveWater();
	for (int i = 0; i < 20; i++)
	{
		for (int j = 0; j < 10; j++)
		{
			if(connect.gameStage < TRANSITION_START)
			{
				boardM[i][j] = Matrix44::CreateTranslation(1.61f * i - boardsOffsetX, water.CrateWaterHeight(i, j, 0.8f, int(-boardsOffsetX)), 1.61f * j) * Matrix44::CreateScale(Vector3(0.8f, 0.8f, 0.8f));
			}
			else
			{
				boardM[i][j] = Matrix44::CreateTranslation(0.6f * i - boardOffsetX, 2.0f, 0.6f * j + boardOffsetZ) * Matrix44::CreateScale(Vector3(0.3f, 0.3f, 0.3f));
				enemyBoardM[i][j] = Matrix44::CreateTranslation(1.61f * i - boardsOffsetX, water.CrateWaterHeight(i, j, 0.8f, int(-boardsOffsetX), int(-enemyBoardOffsetZ)) + (-2.0f + enemyBoardOffsetY), 1.61f * j - enemyBoardOffsetZ)* Matrix44::CreateScale(Vector3(0.8f, 0.8f, 0.8f));
			}

			if (selectedX == i && selectedY == j)
			{
				if(connect.gameStage < TRANSITION_START)
				{
					boardM[i][j] = boardM[i][j] * Matrix44::CreateRotateX(rotation);
				}
				else
				{
					enemyBoardM[i][j] = enemyBoardM[i][j] * Matrix44::CreateRotateX(rotation);
				}
			}
		}
	}



	view = Matrix44(Matrix44::CreateLookAt(
		cameraPos,
		cameraTargetPos,
		Vector3(0.0f, 1.0f, 0.0f)));
	rotation += 0.025f;

	shipPlacement.Update();
	
	if(connect.gameStage == TRANSITION_START)
	{
		if(transition < transition_start_max_time /2)
		{
			boardOffsetZ += 14.0f / ((transition_start_max_time / (deltaT * 2)));
			enemyBoardOffsetY += 2.0f / ((transition_start_max_time / deltaT));

			transition += deltaT;
		}
		else if(transition < transition_start_max_time)
		{
			enemyBoardOffsetY += 2.0f / ((transition_start_max_time / deltaT));
			transition += deltaT;
			boardOffsetZ = 14.0f;
		}
		else
		{
			if(connect.isStarting)
			{
				connect.gameStage = SHOOT;
			}
			else
			{
				connect.gameStage = WAIT_FOR_ENEMY;
			}
			enemyBoardOffsetY = 2.0f;
			transition = 0.0f;
		}
	}
}
void BattleShips::Render()
{
	glEnable(GL_DEPTH_TEST);
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	

	//dr->Clear();

	
	Color rendColor = diffuse;
	if(shipPlaceError >0.0f)
	{
		rendColor = diffuseError;
		shipPlaceError += deltaT;
		if(shipPlaceError>=0.3f)
		{
			shipPlaceError = 0.0f;
		}
	}

	waterRenderer->Begin(view, projection, cameraPos, rendColor, rendColor, 5.0f, 0.5f);
	waterRenderer->Render(Matrix44::CreateIdentity(), water_texture, &water.mesh, Color("000000"), 1.0f);

	waterRenderer->End();

	renderer->Begin(view, projection, cameraPos, rendColor, rendColor, 5.0f, 4.0f);

	for (int i = 0; i < 20; i++)
	{
		for (int j = 0; j < 10; j++)
		{
			if (selectedX == i && selectedY == j)
			{
				if(connect.gameStage < TRANSITION_START)
				{
					if (shipPlaceDown && board[i][j] == shipStage)
					{
						renderer->Render(boardM[i][j], sip_texture, crate_mesh, Color("333333"), 1.0f);
					}
					else
					{
						renderer->Render(boardM[i][j], selected_texture, crate_mesh, Color("333333"), 1.0f);
					}
				}
				else
				{
					if (board[i][j] >= DD)
					{
						renderer->Render(boardM[i][j], ship_texture, crate_mesh, Color("333333"), 1.0f);
					}

					renderer->Render(enemyBoardM[i][j], selected_texture, crate_mesh, Color("333333"), 1.0f);
				}
			}
			else
			{
				if(board[i][j] >= DD)
				{
					if(shipPlaceDown && board[i][j] == shipStage)
					{
						renderer->Render(boardM[i][j], sip_texture, crate_mesh, Color("333333"), 1.0f);
					}
					else
					{
						renderer->Render(boardM[i][j], ship_texture, crate_mesh, Color("333333"), 1.0f);
					}
				}
			}

			

		}
	}

	if(connect.gameStage < TRANSITION_START)
	{
		shipPlacement.Render();
	}
	else if(connect.gameStage == SHOOT)
	{
		renderer->Render(combat_stage, combat_friendly_texture, crate_mesh, Color("000000"), 1.0f);
	}
	else if(connect.gameStage == WAIT_FOR_ENEMY)
	{
		renderer->Render(combat_stage, combat_enemy_texture, crate_mesh, Color("000000"), 1.0f);
	}
	//render transparent grid
	for (int i = 0; i < 20; i++)
	{
		for (int j = 0; j < 10; j++)
		{
			if (!(selectedX == i && selectedY == j))
			{
				if (board[i][j] < DD)
				{
					renderer->Render(boardM[i][j], empty_texture, crate_mesh, Color("000000"), 0.15f);
				}
				if (connect.gameStage >= TRANSITION_START)
				{
					renderer->Render(enemyBoardM[i][j], empty_texture, crate_mesh, Color("000000"), 0.15f);
				}
			}
			else if (connect.gameStage >= TRANSITION_START && board[i][j] < DD)
			{
				renderer->Render(boardM[i][j], empty_texture, crate_mesh, Color("000000"), 0.15f);
			}
		}
	}

	

	renderer->End();
	//dr->AddSphere(cameraPos, 3.0f);
	//Matrix44 tempM = projection * view;
	//dr->Draw(tempM);
}
void BattleShips::Close()
{
	connect.Destroy();
	

	
	delete water_texture;
	delete empty_texture;
	delete ship_texture;
	delete selected_texture;
	delete sip_texture;
	delete template_texture;
	delete template_used_texture;
	delete combat_enemy_texture;
	delete combat_friendly_texture;

	delete crate_mesh;

	delete renderer;
	delete waterRenderer;
	delete shader;
	delete waterShader;
	
	//delete dr;
}

/*
		   ___
      ,.-"',..'"-.
    ,' `.,' : '`'.`.
   ;`-. " `. ``:` `.'.
  ..   '`-.:...-:-` ' .
  ; `.;   .      :   ' .
  |. . ```:---...,;,, .|
  | `:--..:    /   \ ' |
   `.;    :```' "./##\:'
     ``-..:___|  |####|
               `.|##"'              */