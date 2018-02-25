#include "main.h"
/*#include "imgui/imgui.h"
//#include "imgui/imgui.cpp"
#include "imgui/imgui_demo.cpp"
#include "Rendering/Renderer.h"
#include <ctime>
#include "Rendering/Model.h"
#include "Rendering/SkinnedModel.h"

#include "particles/PlayerHealingParticleEmitter.h"
#include "particles/MinionAttackParticleEmitter.h"
#include "particles/DecalParticleEmitter.h"

#include "Rendering/Material.h"
#include "Lobby/Lobby.h"*/
#include <States/LobbyState.h>
#include <States/GameState.h>
#include <States\EditorState.h>
#include <imgui/imgui.h>

#include "particles/FireParticleEmitter.h"
#include "particles/AoeParticleEmitter.h"

int main()
{
	TechDemo::Main* main = new TechDemo::Main();
	int result = main->Start();
	delete main;
	return result;
}

namespace TechDemo
{
	Main::Main() : stateManager(GetStateManager())
	{
		srand((unsigned int)time(nullptr));
		stateManager.AddState("Lobby", new LobbyState());
		stateManager.AddState("Game", new GameState());
		stateManager.AddState("Editor", new EditorState());
	}

	Main::~Main()
	{

	}

	int Main::Start()
	{
		LaunchEngine(glm::ivec2(1366, 768), "Moba Engine");
		return 0;
	}

	void Main::OnStart()
	{
		client = GetClient();
		stateManager.PushState(*this, "Lobby");

		FireParticleEmitter* f = new FireParticleEmitter(glm::vec3(10.0f, 0, 0), this);
		AddParticleEmitter(f);
		AoeParticleEmitter* a = new AoeParticleEmitter(glm::vec3(-10.0f, 0.0f, 0.0f), this);
		AddParticleEmitter(a);
	}

	void Main::OnUpdate(float deltaTime)
	{
		stateManager.Update(*this);
		ImGui::Begin("press to connect", 0, ImVec2(500, 100));
		if (ImGui::Button("Connect to Lobby"))
		{
			client->Connect("127.0.0.1", 7001);
		}
#ifdef TESTMODE
		if (ImGui::Button("Connect to Server"))
		{
			client->Connect("127.0.0.1", 7000);
			stateManager.ChangeState(*this, "Game");
		}
		if(ImGui::Button("Direct Change state to game state"))
		{
			stateManager.ChangeState(*this, "Game");
		}
		
#endif
		if(ImGui::Button("Direct Change state to editor state"))
		{
			stateManager.ChangeState(*this, "Editor");
		}

		/*if (ImGui::Button("Send player test position"))
		{
		glm::vec3 v = input->TempUnProject(input->window_handle_, input->rendererTest->GetViewMatrix(), input_->rendererTest->GetProjectionMatrix());
		client->SendPos(v);

		}*/
		ImGui::End();

		stateManager.Render(*this);
		
	}
}
