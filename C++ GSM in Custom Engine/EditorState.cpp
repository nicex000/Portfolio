#include "States\EditorState.h"
#include "MainLoop.h"
#include "imgui\imgui.h"
#include <unordered_map>
#include <string>
#include <iostream>


namespace TechDemo
{
	void EditorState::Enter(MobaEngine::MainLoop& a_Game)
	{
		champTool_ = new MobaEngine::ChampionTool();
		// Show pathfinding algorithms
		SetupPathfinding(a_Game);
	}
	
	void EditorState::Update(MobaEngine::MainLoop& a_Game)
	{
		champTool_->DisplayTool();
		DisplayPathfinding(a_Game);
	}
	
	void EditorState::Render(MobaEngine::MainLoop& a_Game)
	{

	}
	
	void EditorState::Exit(MobaEngine::MainLoop& a_Game)
	{
		delete champTool_;
	}
	
	void EditorState::Pause()
	{

	}
	
	void EditorState::Resume()
	{

	}

	void EditorState::SetupPathfinding(MobaEngine::MainLoop& a_Game)
	{
		a_Game.GetPathfinder()->collisionGrid_ = new MobaEngine::WeightedGrid(10,10,glm::vec2(10,10));
	}

	void EditorState::DisplayPathfinding(MobaEngine::MainLoop& a_Game)
	{
		const char* pathfindingTypes[] = { "BreadthFirstSearch", "Dijkstra", "AStar" };
		ImGui::Begin("Pathfinding", 0, ImVec2(500, 800));
		
		ImGui::Combo("Algorithm", &selectedAlgorithm, pathfindingTypes, 3);
		
		ImGui::Columns(10);
		int button = 0;
		for (int col = 0; col < 10; col++)
		{
			for (int row = 0; row < 10; row++)
			{
				bool isWaypoint = false;
				for (MobaEngine::Location way : testWaypoints)
				{
					if (way.x_ == row && way.y_ == col)
					{
						isWaypoint = true;
						break;
					}
					else
					{
						isWaypoint = false;
					}
				}
				if (isWaypoint)
				{
					ImGui::PushID(button);
					if (ImGui::ColorButton(ImVec4(0.5, 0, 0.5, 1)))
					{
						a_Game.GetPathfinder()->collisionGrid_->Walls.insert(MobaEngine::Location(row, col));
					}
					ImGui::PopID();
					button++;
				}
				else if (!a_Game.GetPathfinder()->collisionGrid_->Passable(MobaEngine::Location(row, col)))
				{
					ImGui::PushID(button);
					if (ImGui::ColorButton(ImVec4(1, 0, 0, 1)))
					{
						std::cout << row << " , " << col << std::endl;
						a_Game.GetPathfinder()->collisionGrid_->Walls.erase(MobaEngine::Location(row, col));
						
					}
					ImGui::PopID();
					button++;
				}
				else if(a_Game.GetPathfinder()->collisionGrid_->MovementCost(MobaEngine::Location(row, col)) > 1.0)
				{
					ImGui::PushID(button);
					if (ImGui::ColorButton(ImVec4(0, 1, 0, 1)))
					{
						std::cout << row << " , " << col << std::endl;
						a_Game.GetPathfinder()->collisionGrid_->Terrain.erase(MobaEngine::Location(row, col));
						a_Game.GetPathfinder()->collisionGrid_->Walls.insert(MobaEngine::Location(row, col));
					}
					ImGui::PopID();
					button++;
				}
				else
				{
					ImGui::PushID(button);
					if (ImGui::ColorButton(ImVec4(0, 0, 1, 1)))
					{
						std::cout << row << " , " << col << std::endl;
						a_Game.GetPathfinder()->collisionGrid_->Terrain.insert(MobaEngine::Location(row, col));
					}
					ImGui::PopID();
					button++;
				}

			}
			ImGui::NextColumn();
		}

		RunAlgorithm(selectedAlgorithm, a_Game);

		ImGui::Columns(1);
		ImGui::End();
	}

	void EditorState::CalcBreadthFirstSearch(MobaEngine::MainLoop& a_Game)
	{
		testWaypoints.clear();
		testWaypoints = a_Game.GetPathfinder()->GetPath(start, end, a_Game.GetPathfinder()->BreadthFirstSearch(start, end));
	}

	void EditorState::CalcDijkstra(MobaEngine::MainLoop& a_Game)
	{
		testWaypoints.clear();
		std::unordered_map<MobaEngine::Location, MobaEngine::Location, MobaEngine::Location> cameFrom;
		std::unordered_map<MobaEngine::Location, int, MobaEngine::Location> cost;
		a_Game.GetPathfinder()->Dijkstra(a_Game.GetPathfinder()->GetGrid(), start, end, cameFrom, cost);
		testWaypoints = a_Game.GetPathfinder()->GetPath(start, end, cameFrom);
	}

	void EditorState::CalcAStar(MobaEngine::MainLoop& a_Game)
	{
		testWaypoints.clear();
		std::unordered_map<MobaEngine::Location, MobaEngine::Location, MobaEngine::Location> cameFrom;
		std::unordered_map<MobaEngine::Location, int, MobaEngine::Location> cost;
		a_Game.GetPathfinder()->AStar(start, end, cameFrom, cost);
		testWaypoints = a_Game.GetPathfinder()->GetPath(start, end, cameFrom);
	}

	void EditorState::RunAlgorithm(int a, MobaEngine::MainLoop& a_Game)
	{
		switch (a)
		{
		case 0:
			CalcBreadthFirstSearch(a_Game);
			break;
		case 1:
			CalcDijkstra(a_Game);
			break;
		case 2:
			CalcAStar(a_Game);
		default:
			break;
		}
	}
}