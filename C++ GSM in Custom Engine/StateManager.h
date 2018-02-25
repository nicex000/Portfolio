#pragma once

#include <string>
#include <map>
#include <vector>


namespace MobaEngine {
	enum DelayType
	{
		CHANGE,
		PUSH,
		POP
	};
	class MainLoop;
	class State;
	class StateManager
	{
	public:
		StateManager();
		~StateManager();
		void AddState(const std::string& a_StateName, State* a_State);
		void ChangeState(MainLoop& a_Game, std::string a_StateName);
		void ChangeState(MainLoop& a_Game, std::string a_StateName, bool delay);
		void PushState(MainLoop& a_Game, std::string a_StateName);
		void PushState(MainLoop& a_Game, std::string a_StateName, bool delay);
		void PopState(MainLoop& a_Game, std::string a_StateName);
		void PopState(MainLoop& a_Game, std::string a_StateName, bool delay);
		void Update(MainLoop& a_Game);
		void Render(MainLoop& a_Game);

	private:
		std::vector<State*> m_CurrentStates;

		std::map<std::string, State*> m_States;
		struct DelayedState
		{
			std::string stateName;
			DelayType delayType;
		};
		std::vector<DelayedState*> m_NextStates;
	};
}

