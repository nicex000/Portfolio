#pragma once
#include <string>
#include "StateManager.h"
namespace MobaEngine
{
	class State
	{
	public:
		State() = default;
		virtual ~State() = default;
		virtual void Enter(MainLoop& a_Game) {};
		virtual void Update(MainLoop& a_Game) {};
		virtual void Render(MainLoop& a_Game) {};
		virtual void Exit(MainLoop& a_Game) {};
		virtual void Pause() {};
		virtual void Resume() {};

		void ChangeState(MainLoop& a_Game, std::string a_StateName, StateManager* stateManager) {
			stateManager->ChangeState(a_Game, a_StateName); // used to change state to a new state (since the state changing only happens from inside one state)
		}
		bool isPaused = false;
		bool isRenderOn = true;
	};
}

