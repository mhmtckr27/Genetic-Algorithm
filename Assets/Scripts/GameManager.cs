using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	[Header("Grid Map")]
	[SerializeField] private GameObject gridObject;
	[SerializeField] private GameObject cellPrefab;
	[SerializeField] private int cellRowCount;
	[SerializeField] private int cellColumnCount;
	private GridLayoutGroup gridLayoutGroup;
	private List<List<GameObject>> cells;
	[Space]

	[Header("Drones")]
	[SerializeField] private int droneCount;
	[SerializeField] private Vector2 startFinishPoint;
	private List<List<List<int>>> populations;
	private List<int> moveDirections = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7};
	private List<Vector2> directionCoordinates = new List<Vector2> {new Vector2(-1, -1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, -1),
																	new Vector2(0, 1), new Vector2(1, -1), new Vector2(1, 0), new Vector2(1, 1)};
	private List<int> directionCosts = new List<int> { 2, 1, 2, 3, 3, 4, 5, 4 };
	/// <directions>
	/// 0	1	2
	/// 3	X	4
	/// 5	6	7
	/// </directions>
	private float mutationProbability;
	private List<List<bool>> exploredAreas;
	[Space]

	[SerializeField] private int populationCount;


	private void Start()
	{
		gridLayoutGroup = gridObject.GetComponent<GridLayoutGroup>();
		cells = new List<List<GameObject>>();
		exploredAreas = new List<List<bool>>();
		populations = new List<List<List<int>>>();

		for(int i = 0; i < cellRowCount; i++)
		{
			cells.Add(new List<GameObject>());
			exploredAreas.Add(new List<bool>());
		}
		for(int i = 0; i < droneCount; i++)
		{
			populations.Add(new List<List<int>>());
		}
		for(int i = 0; i < populationCount; i++)
		{
			populations[i].Add(new List<int>());
		}

		InitGridMap();
		InitFirstGeneration();
	}

	private void InitGridMap()
	{
		gridLayoutGroup.constraintCount = cellColumnCount;
		for(int i = 0; i < cellRowCount; i++)
		{
			for(int j = 0; j < cellColumnCount; j++)
			{
				cells[i].Add(Instantiate(cellPrefab, gridObject.transform));
			}
		}
	}

	private void InitFirstGeneration()
	{
		int chromosomeLength = Mathf.CeilToInt(cellRowCount * cellColumnCount - 1);

		for(int i = 0; i < droneCount; i++)
		{
			for(int j = 0; j < populationCount; j++)
			{
				populations[i].Add(GenerateRandomChromosome(chromosomeLength));
			}
		}
	}

	private List<int> GenerateRandomChromosome(int chromosomeLength)
	{
		List<int> chromosome = new List<int>();
		for(int i = 0; i < chromosomeLength; i++)
		{
			chromosome.Add(Random.Range(0, 8));
		}
		return chromosome;
	}

	private bool CanMoveToDirection(Vector2 location, int direction)
	{
		if((location.x + directionCoordinates[direction].x) < 0) { return false; }
		if((location.x + directionCoordinates[direction].x) >= cellColumnCount) { return false; }
		if ((location.y + directionCoordinates[direction].y) < 0) { return false; }
		if ((location.y + directionCoordinates[direction].y) >= cellRowCount) { return false; }

		return true;
	}

	private int CalculateMovementCost(Vector2 location, int direction)
	{
		if(!CanMoveToDirection(location, direction)) { return 1000; }

		return directionCosts[direction];
	}

	private void SelectIndividuals(List<List<int>> population, int selectCount)
	{

	}

	private List<List<int>> Reproduce(List<List<int>> populationToReproduce)
	{
		List<List<int>> newPopulation = new List<List<int>>();
		for(int i = 0; i < populationToReproduce.Count; i++)
		{
			for(int j = 0; j < populationToReproduce.Count; j++)
			{
				newPopulation.Add(ReproduceChromosome(populationToReproduce[i], populationToReproduce[j]))
			}
		}
	}

	private List<int> ReproduceChromosome(List<int> chromosome_1, float fitness_1, List<int> chromosome_2, float fitness_2)
	{
		int newLengthChromosome_1 = Mathf.RoundToInt(chromosome_1.Count * (fitness_1 / (fitness_1 + fitness_2)));
		int newLengthChromosome_2 = chromosome_1.Count - newLengthChromosome_1;

		List<int> newChromosome = new List<int>();
		newChromosome.AddRange(chromosome_1.GetRange(0, newLengthChromosome_1));
		newChromosome.AddRange(chromosome_2.GetRange(newLengthChromosome_1 - 1, newLengthChromosome_2));

		return newChromosome;
	}

	private void MutateChromosome(List<int> chromosome, int mutationCount)
	{
		for (int i = 0; i < mutationCount; i++)
		{
			chromosome[Random.Range(0, chromosome.Count)] = Random.Range(0, 8);
		}
	}

	public struct Individual
	{
		public List<int> path;
		public float fitness;
	}
}