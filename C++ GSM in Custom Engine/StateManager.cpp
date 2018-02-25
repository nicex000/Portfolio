#include "GameState/StateManager.h"
#include "GameState/State.h"
namespace MobaEngine
{
	StateManager::StateManager()
	{
	}

	StateManager::~StateManager()
	{
		for (auto s : m_States)
		{
			delete s.second;
		}
	}

	void StateManager::AddState(const std::string& a_StateName, State* a_State)
	{
		m_States[a_StateName] = a_State;
	}

	void StateManager::ChangeState(MobaEngine::MainLoop& a_Game, std::string a_StateName)
	{
		// cleanup the current state
		if (!m_CurrentStates.empty()) {
			m_CurrentStates.back()->Exit(a_Game);
			m_CurrentStates.pop_back();
		}

		// store and init the new state
		m_CurrentStates.push_back(m_States[a_StateName]);
		m_CurrentStates.back()->Enter(a_Game);
	}

	void StateManager::ChangeState(MainLoop& a_Game, std::string a_StateName, bool delay)
	{
		m_NextStates.push_back(new DelayedState{ a_StateName, CHANGE });
	}

	void StateManager::PushState(MobaEngine::MainLoop& a_Game, std::string a_StateName)
	{
		// pause current state
		/*if (!m_CurrentStates.empty()) {
			m_CurrentStates.back()->Pause();
		}*/

		// store and init the new state
		m_CurrentStates.push_back(m_States[a_StateName]);
		m_CurrentStates.back()->Enter(a_Game);//is enter the right thing? YouLostMe
	}

	void StateManager::PushState(MainLoop& a_Game, std::string a_StateName, bool delay)
	{
		m_NextStates.push_back(new DelayedState{ a_StateName, PUSH });
	}

	void StateManager::PopState(MobaEngine::MainLoop& a_Game, std::string a_StateName)
	{
		// cleanup the current state
		if (!m_CurrentStates.empty()) {
			m_CurrentStates.back()->Exit(a_Game);
			m_CurrentStates.pop_back();
		}

		// resume previous state
		if (!m_CurrentStates.empty()) {
			m_CurrentStates.back()->Resume();
		}
	}

	void StateManager::PopState(MainLoop& a_Game, std::string a_StateName, bool delay)
	{
		m_NextStates.push_back(new DelayedState{ a_StateName, POP });
	}

	void StateManager::Update(MobaEngine::MainLoop& a_Game)
	{
		for(auto nextState : m_NextStates)
		{
			switch (nextState->delayType)
			{
			case CHANGE:
				ChangeState(a_Game, nextState->stateName);
				break;
			case PUSH: 
				PushState(a_Game, nextState->stateName);
				break;
			case POP: 
				PopState(a_Game, nextState->stateName);
				break;
			default: break;
			}
		}
		m_NextStates.clear();

		//back or all?
		//printf("manager\n");
		if (!m_CurrentStates.empty()) {
			m_CurrentStates.back()->Update(a_Game);
		}
	}

	void StateManager::Render(MobaEngine::MainLoop& a_Game)
	{
		for (auto state : m_CurrentStates)
		{
			if (state->isRenderOn) state->Render(a_Game);
		}
	}
}